import {
    app,
    BrowserWindow,
    ipcMain,
    powerMonitor,
    protocol,
    safeStorage,
    shell,
} from "electron";
import { HttpAgent } from "@ag-ui/client";
import type { Message } from "@ag-ui/core";
import { readFile, rename, rm, writeFile } from "node:fs/promises";
import { join } from "node:path";
import type {
    ActiveAgentChatRun,
    AgentAvatarUploadFile,
    AgentAvatarUploadResponse,
    AgentConversation,
    AgentConversationListItem,
    AgentDefinition,
    AgentListItem,
    AgentSkillDefinition,
    AgentSkillUpdateRequest,
    AgentSkillUploadFile,
    AgentToolDefinition,
    AgentUpsertRequest,
    AgentVersion,
    AppMetadata,
    AuthIdentity,
    AuthSnapshot,
    AuthStatus,
    ChangePasswordRequest,
    ChatMessage,
    ChatRequest,
    ChatRunError,
    ChatRunSnapshot,
    ChatTokenUsage,
    CreateAccountRequest,
    IssuedCredential,
    LLMModel,
    LLMModelTestResult,
    LLMProviderManagementInfo,
    LoginRequest,
    LoginResult,
    SkillRunActivity,
    UnlockResult,
    UserListItem,
} from "../src/shared/network/contracts.js";
import {
    addAguiTokenUsage,
    type AguiTokenUsage,
} from "./chat-token-usage.js";
import { toToolRunActivity } from "./chat-tool-activity.js";

declare const __INKWELL_BUILD_NUMBER__: string;
declare const __INKWELL_COMMIT_SHA__: string;

app.setName("Inkwell");

protocol.registerSchemesAsPrivileged([
    {
        scheme: "inkwell",
        privileges: { secure: true, standard: true, supportFetchAPI: true },
    },
]);

const apiBaseUrl = (
    process.env.INKWELL_WEBAPI_URL ?? "http://localhost:6801"
).replace(/\/$/, "");
const authSessionFileName = "auth-session.bin";
const idleLockMilliseconds = 60 * 60 * 1000;
const completedChatRunLimit = 20;
const chatRunBroadcastIntervalMilliseconds = 32;
const applicationIconPath = join(__dirname, "../renderer/logo.png");
let sessionToken: string | null = null;
let authSnapshot: AuthSnapshot = { status: "restoring", identity: null };
let idleLockTimer: NodeJS.Timeout | null = null;
const chatRuns = new Map<
    string,
    {
        agentId: string;
        conversationId: string | null;
        controller: AbortController;
        userMessage: ChatMessage;
        snapshot: ChatRunSnapshot;
    }
>();
const completedChatRunIds: string[] = [];
const pendingChatRunBroadcastTimers = new Map<string, NodeJS.Timeout>();

interface InternalAuthSession extends AuthIdentity {
    sessionToken: string;
}

interface PagedApiResponse<T> {
    items: T[];
    totalCount: number;
}

interface AgentChatMessageApiResponse {
    id: string;
    usage?: ChatTokenUsage | null;
    message: {
        role?: string;
        Role?: string;
        text?: string | null;
        Text?: string | null;
        contents?: AgentChatMessageContentApiResponse[];
        Contents?: AgentChatMessageContentApiResponse[];
    };
}

interface AgentChatMessageContentApiResponse {
    $type?: string;
    text?: string | null;
    Text?: string | null;
    name?: string;
    Name?: string;
    callId?: string;
    CallId?: string;
    arguments?: unknown;
    Arguments?: unknown;
}

class ApiRequestError extends Error {
    public readonly status: number;

    public constructor(status: number, message: string) {
        super(message);
        this.status = status;
    }
}

class ChatRunFailure extends Error {
    public readonly error: ChatRunError;

    public constructor(error: ChatRunError) {
        super(`${error.code}: ${error.reason}`);
        this.error = error;
    }
}

const copyChatRunSnapshot = (snapshot: ChatRunSnapshot): ChatRunSnapshot => ({
    ...snapshot,
    skillActivities: snapshot.skillActivities.map((activity) => ({
        ...activity,
    })),
    ...(snapshot.usage
        ? {
              usage: {
                  ...snapshot.usage,
                  ...(snapshot.usage.additionalCounts
                      ? {
                            additionalCounts: {
                                ...snapshot.usage.additionalCounts,
                            },
                        }
                      : {}),
              },
          }
        : {}),
    ...(snapshot.error ? { error: { ...snapshot.error } } : {}),
});

const copyActiveAgentChatRun = (
    run: ActiveAgentChatRun,
): ActiveAgentChatRun => ({
    ...run,
    userMessage: {
        ...run.userMessage,
        ...(run.userMessage.skillActivities
            ? {
                  skillActivities: run.userMessage.skillActivities.map(
                      (activity) => ({ ...activity }),
                  ),
              }
            : {}),
    },
    snapshot: copyChatRunSnapshot(run.snapshot),
});

const getPersistedSkillActivities = (
    contents: AgentChatMessageContentApiResponse[] | undefined,
): SkillRunActivity[] =>
    (contents ?? [])
        .filter((content) => content.$type === "functionCall")
        .map((content) =>
            toToolRunActivity({
                id: content.callId ?? content.CallId,
                function: {
                    name: content.name ?? content.Name,
                    arguments: JSON.stringify(
                        content.arguments ?? content.Arguments ?? {},
                    ),
                },
            }),
        )
        .filter((activity): activity is SkillRunActivity => activity !== null)
        .map((activity) => ({ ...activity, status: "success" }));

const normalizePersistedChatMessages = (
    items: AgentChatMessageApiResponse[],
): ChatMessage[] => {
    const messages: ChatMessage[] = [];
    let pendingSkillActivities: SkillRunActivity[] = [];
    let pendingMessageId: string | undefined;

    const flushPendingActivities = (): void => {
        if (pendingSkillActivities.length === 0) return;
        messages.push({
            id: pendingMessageId,
            role: "assistant",
            content: "",
            skillActivities: pendingSkillActivities,
        });
        pendingSkillActivities = [];
        pendingMessageId = undefined;
    };

    for (const { id, message, usage } of items) {
        const role = message.role ?? message.Role;
        const contents = message.contents ?? message.Contents ?? [];
        const skillActivities = getPersistedSkillActivities(contents);
        const content =
            message.text ??
            message.Text ??
            contents.map((item) => item.text ?? item.Text ?? "").join("");

        if (skillActivities.length > 0) {
            pendingMessageId ??= id;
            pendingSkillActivities = [
                ...pendingSkillActivities,
                ...skillActivities.filter(
                    (activity) =>
                        !pendingSkillActivities.some(
                            (current) => current.callId === activity.callId,
                        ),
                ),
            ];
        }

        if (role === "user") {
            flushPendingActivities();
            if (content.trim()) messages.push({ id, role, content });
            continue;
        }

        if (role !== "assistant" || !content.trim()) continue;
        messages.push({
            id,
            role,
            content,
            ...(usage ? { usage } : {}),
            ...(pendingSkillActivities.length > 0
                ? { skillActivities: pendingSkillActivities }
                : {}),
        });
        pendingSkillActivities = [];
        pendingMessageId = undefined;
    }

    flushPendingActivities();
    return messages;
};

const sendChatRunSnapshot = (snapshot: ChatRunSnapshot): void => {
    const value = copyChatRunSnapshot(snapshot);
    for (const window of BrowserWindow.getAllWindows()) {
        window.webContents.send("inkwell:chat-run-changed", value);
    }
};

const broadcastChatRun = (
    snapshot: ChatRunSnapshot,
    immediate = false,
): void => {
    const pendingTimer = pendingChatRunBroadcastTimers.get(snapshot.requestId);
    if (immediate || snapshot.status !== "running") {
        if (pendingTimer) {
            clearTimeout(pendingTimer);
            pendingChatRunBroadcastTimers.delete(snapshot.requestId);
        }
        sendChatRunSnapshot(snapshot);
        return;
    }

    if (pendingTimer) return;

    pendingChatRunBroadcastTimers.set(
        snapshot.requestId,
        setTimeout(() => {
            pendingChatRunBroadcastTimers.delete(snapshot.requestId);
            const latestSnapshot = chatRuns.get(snapshot.requestId)?.snapshot;
            if (latestSnapshot) sendChatRunSnapshot(latestSnapshot);
        }, chatRunBroadcastIntervalMilliseconds),
    );
};

const finishChatRun = (
    requestId: string,
    status: Exclude<ChatRunSnapshot["status"], "running">,
    error?: ChatRunError,
    usage?: ChatTokenUsage,
): void => {
    const run = chatRuns.get(requestId);
    if (!run || run.snapshot.status !== "running") return;

    run.snapshot = {
        ...run.snapshot,
        status,
        skillActivities: run.snapshot.skillActivities.map((activity) => ({
            ...activity,
            status:
                status === "completed"
                    ? "success"
                    : status === "stopped"
                      ? "abort"
                      : "error",
            ...(error ? { error: error.reason } : {}),
        })),
        ...(status === "completed" && usage ? { usage } : {}),
        ...(error ? { error } : {}),
    };
    broadcastChatRun(run.snapshot, true);
    completedChatRunIds.push(requestId);
    while (completedChatRunIds.length > completedChatRunLimit) {
        const expiredRequestId = completedChatRunIds.shift();
        if (
            expiredRequestId &&
            chatRuns.get(expiredRequestId)?.snapshot.status !== "running"
        ) {
            chatRuns.delete(expiredRequestId);
        }
    }
};

const getSafeErrorReason = async (response: Response): Promise<string> => {
    const fallback = `Agent request failed with status ${response.status}.`;
    const text = (await response.text()).trim();
    if (!text) return fallback;

    try {
        const problem = JSON.parse(text) as {
            detail?: unknown;
            message?: unknown;
        };
        const reason =
            typeof problem.detail === "string"
                ? problem.detail
                : typeof problem.message === "string"
                  ? problem.message
                  : fallback;
        return reason.slice(0, 240);
    } catch {
        return fallback;
    }
};

const broadcastAuthState = (): void => {
    for (const window of BrowserWindow.getAllWindows()) {
        window.webContents.send("inkwell:auth-state-changed", authSnapshot);
    }
};

const setAuthState = (
    status: AuthStatus,
    identity: AuthIdentity | null,
): void => {
    authSnapshot = { status, identity };
    broadcastAuthState();
};

const getAuthSessionPath = (): string =>
    join(app.getPath("userData"), authSessionFileName);

const deletePersistedToken = async (): Promise<void> => {
    await rm(getAuthSessionPath(), { force: true });
};

const persistToken = async (token: string): Promise<void> => {
    if (!safeStorage.isEncryptionAvailable()) return;

    const targetPath = getAuthSessionPath();
    const temporaryPath = `${targetPath}.tmp`;
    await writeFile(temporaryPath, safeStorage.encryptString(token), {
        mode: 0o600,
    });
    await rename(temporaryPath, targetPath);
};

const readPersistedToken = async (): Promise<string | null> => {
    if (!safeStorage.isEncryptionAvailable()) return null;

    try {
        return safeStorage.decryptString(await readFile(getAuthSessionPath()));
    } catch (reason) {
        const errorCode =
            reason instanceof Error && "code" in reason
                ? reason.code
                : undefined;
        if (errorCode !== "ENOENT") await deletePersistedToken();
        return null;
    }
};

const clearIdleLockTimer = (): void => {
    if (idleLockTimer) clearTimeout(idleLockTimer);
    idleLockTimer = null;
};

const lockAuthentication = (): void => {
    clearIdleLockTimer();
    if (authSnapshot.status === "authenticated") {
        setAuthState("locked", authSnapshot.identity);
    }
};

const scheduleIdleLock = (): void => {
    clearIdleLockTimer();
    if (authSnapshot.status === "authenticated") {
        idleLockTimer = setTimeout(lockAuthentication, idleLockMilliseconds);
    }
};

const clearAuthentication = async (): Promise<void> => {
    clearIdleLockTimer();
    sessionToken = null;
    await deletePersistedToken();
    setAuthState("anonymous", null);
};

const request = async <T>(path: string, init?: RequestInit): Promise<T> => {
    const response = await fetch(`${apiBaseUrl}${path}`, {
        ...init,
        headers: {
            Accept: "application/json",
            ...(init?.body && !(init.body instanceof FormData)
                ? { "Content-Type": "application/json" }
                : {}),
            ...(sessionToken
                ? { Authorization: `Bearer ${sessionToken}` }
                : {}),
            ...init?.headers,
        },
    });

    if (!response.ok) {
        const detail = await response.text();
        if (
            response.status === 401 &&
            path !== "/api/auth/login" &&
            path !== "/api/auth/unlock"
        ) {
            await clearAuthentication();
        }
        throw new ApiRequestError(
            response.status,
            detail ||
                `Inkwell API request failed with status ${response.status}.`,
        );
    }

    return response.status === 204
        ? (undefined as T)
        : (response.json() as Promise<T>);
};

const requestAllPages = async <T>(path: string): Promise<T[]> => {
    const pageSize = 100;
    const items: T[] = [];
    for (let page = 1; ; page++) {
        const result = await request<PagedApiResponse<T>>(
            `${path}?page=${page}&pageSize=${pageSize}`,
        );
        items.push(...result.items);
        if (result.items.length === 0 || items.length >= result.totalCount) {
            return items;
        }
    }
};

const requireAuthenticated = (): void => {
    if (authSnapshot.status !== "authenticated" || !sessionToken) {
        throw new Error(
            authSnapshot.status === "locked"
                ? "Client is locked."
                : "Authentication is required.",
        );
    }

    if (authSnapshot.identity?.mustChangePassword) {
        throw new Error("Password change is required.");
    }
};

const requireAdmin = (): void => {
    requireAuthenticated();
    if (!authSnapshot.identity?.isAdmin) {
        throw new Error("Super user authorization is required.");
    }
};

const restoreAuthentication = async (): Promise<AuthSnapshot> => {
    const persistedToken = await readPersistedToken();
    if (!persistedToken) {
        setAuthState("anonymous", null);
        return authSnapshot;
    }

    sessionToken = persistedToken;
    try {
        const session = await request<InternalAuthSession>("/api/auth/session");
        const identity = toAuthIdentity(session);
        setAuthState("authenticated", identity);
        scheduleIdleLock();
    } catch (reason) {
        if (!(reason instanceof ApiRequestError)) {
            setAuthState("offline", null);
        }
    }

    return authSnapshot;
};

const toAuthIdentity = (session: InternalAuthSession): AuthIdentity => ({
    userId: session.userId,
    username: session.username,
    isAdmin: session.isAdmin,
    mustChangePassword: session.mustChangePassword,
    expiresAt: session.expiresAt,
});

const registerApiHandlers = (): void => {
    ipcMain.handle(
        "inkwell:app-metadata",
        (): AppMetadata => ({
            version: app.getVersion(),
            buildNumber: __INKWELL_BUILD_NUMBER__,
            commit: __INKWELL_COMMIT_SHA__,
        }),
    );
    ipcMain.handle("inkwell:restore-auth", restoreAuthentication);

    ipcMain.handle(
        "inkwell:login",
        async (_event, login: LoginRequest): Promise<LoginResult> => {
            try {
                const session = await request<InternalAuthSession>(
                    "/api/auth/login",
                    {
                        method: "POST",
                        body: JSON.stringify(login),
                    },
                );
                sessionToken = session.sessionToken;
                try {
                    await persistToken(sessionToken);
                } catch (reason) {
                    console.error(
                        "Unable to persist the authentication session; it will remain available for this process only.",
                        reason,
                    );
                }
                const identity = toAuthIdentity(session);
                setAuthState("authenticated", identity);
                scheduleIdleLock();
                return { ok: true, identity };
            } catch (reason) {
                if (reason instanceof ApiRequestError) {
                    const code =
                        reason.status === 401
                            ? "invalid-credentials"
                            : reason.status === 423
                              ? "account-locked"
                              : reason.status === 429
                                ? "rate-limited"
                                : "unknown";
                    return { ok: false, code };
                }

                return { ok: false, code: "offline" };
            }
        },
    );

    ipcMain.handle(
        "inkwell:unlock",
        async (_event, password: string): Promise<UnlockResult> => {
            if (
                authSnapshot.status !== "locked" ||
                !sessionToken ||
                !authSnapshot.identity
            ) {
                throw new Error("Client is not locked.");
            }

            const identity = authSnapshot.identity;

            try {
                await request<void>("/api/auth/unlock", {
                    method: "POST",
                    body: JSON.stringify({ password }),
                });
            } catch (reason) {
                if (reason instanceof ApiRequestError) {
                    if (reason.status === 423) {
                        await clearAuthentication();
                        return { ok: false, code: "account-locked" };
                    }
                    return {
                        ok: false,
                        code:
                            reason.status === 401
                                ? "invalid-password"
                                : "unknown",
                    };
                }

                return { ok: false, code: "offline" };
            }

            setAuthState("authenticated", identity);
            scheduleIdleLock();
            return { ok: true, identity };
        },
    );

    ipcMain.handle("inkwell:logout", async (): Promise<void> => {
        try {
            await request<void>("/api/auth/logout", { method: "POST" });
        } finally {
            await clearAuthentication();
        }
    });

    ipcMain.handle(
        "inkwell:change-password",
        async (_event, input: ChangePasswordRequest): Promise<AuthIdentity> => {
            if (!sessionToken || !authSnapshot.identity) {
                throw new Error("Authentication is required.");
            }

            const session = await request<InternalAuthSession>(
                "/api/auth/password",
                { method: "POST", body: JSON.stringify(input) },
            );
            const identity = toAuthIdentity(session);
            setAuthState("authenticated", identity);
            scheduleIdleLock();
            return identity;
        },
    );

    ipcMain.on("inkwell:activity", scheduleIdleLock);
    ipcMain.handle("inkwell:list-my-agents", () => {
        requireAuthenticated();
        return request<AgentListItem[]>("/api/agents/mine");
    });
    ipcMain.handle("inkwell:list-shared-agents", () => {
        requireAuthenticated();
        return request<AgentListItem[]>("/api/agents/shared");
    });
    ipcMain.handle("inkwell:delete-agent", (_event, agentId: string) => {
        requireAuthenticated();
        return request<void>(`/api/agents/${encodeURIComponent(agentId)}`, {
            method: "DELETE",
        });
    });
    ipcMain.handle("inkwell:share-agent", (_event, agentId: string) => {
        requireAuthenticated();
        return request<void>(
            `/api/agents/${encodeURIComponent(agentId)}/share`,
            { method: "POST" },
        );
    });
    ipcMain.handle("inkwell:unshare-agent", (_event, agentId: string) => {
        requireAuthenticated();
        return request<void>(
            `/api/agents/${encodeURIComponent(agentId)}/share`,
            { method: "DELETE" },
        );
    });
    ipcMain.handle("inkwell:revoke-agent-share", (_event, agentId: string) => {
        requireAdmin();
        return request<void>(
            `/api/agents/${encodeURIComponent(agentId)}/share/revoke`,
            { method: "POST" },
        );
    });
    ipcMain.handle("inkwell:list-tools", () => {
        requireAuthenticated();
        return request<AgentToolDefinition[]>("/api/tools");
    });
    ipcMain.handle("inkwell:list-skills", () => {
        requireAuthenticated();
        return request<AgentSkillDefinition[]>("/api/skills");
    });
    ipcMain.handle(
        "inkwell:upload-skill",
        (_event, file: AgentSkillUploadFile) => {
            requireAuthenticated();
            const body = new FormData();
            body.append(
                "file",
                new Blob([file.bytes], { type: "application/octet-stream" }),
                file.name,
            );
            return request<AgentSkillDefinition>("/api/skills", {
                method: "POST",
                body,
            });
        },
    );
    ipcMain.handle(
        "inkwell:update-skill",
        (_event, skillId: string, input: AgentSkillUpdateRequest) => {
            requireAuthenticated();
            return request<AgentSkillDefinition>(
                `/api/skills/${encodeURIComponent(skillId)}`,
                { method: "PUT", body: JSON.stringify(input) },
            );
        },
    );
    ipcMain.handle("inkwell:delete-skill", (_event, skillId: string) => {
        requireAuthenticated();
        return request<void>(`/api/skills/${encodeURIComponent(skillId)}`, {
            method: "DELETE",
        });
    });
    ipcMain.handle("inkwell:list-models", () => {
        requireAuthenticated();
        return request<LLMModel[]>("/api/models");
    });
    ipcMain.handle("inkwell:model-management-info", () => {
        requireAuthenticated();
        return request<LLMProviderManagementInfo>("/api/models/management");
    });
    ipcMain.handle("inkwell:test-model", (_event, modelId: string) => {
        requireAuthenticated();
        return request<LLMModelTestResult>(
            `/api/models/${encodeURIComponent(modelId)}/test`,
            { method: "POST" },
        );
    });
    ipcMain.handle("inkwell:open-external", async (_event, url: string) => {
        requireAuthenticated();
        const externalUrl = new URL(url);
        if (
            externalUrl.protocol !== "https:" &&
            externalUrl.protocol !== "http:"
        ) {
            throw new Error(
                "Only HTTP and HTTPS URLs can be opened externally.",
            );
        }

        await shell.openExternal(externalUrl.toString());
    });
    ipcMain.handle("inkwell:list-accounts", () => {
        requireAdmin();
        return request<UserListItem[]>("/api/auth/accounts");
    });
    ipcMain.handle(
        "inkwell:create-account",
        (_event, input: CreateAccountRequest) => {
            requireAdmin();
            return request<IssuedCredential>("/api/auth/accounts", {
                method: "POST",
                body: JSON.stringify(input),
            });
        },
    );
    ipcMain.handle("inkwell:unlock-account", (_event, userId: string) => {
        requireAdmin();
        return request<void>(
            `/api/auth/accounts/${encodeURIComponent(userId)}/unlock`,
            { method: "POST" },
        );
    });
    ipcMain.handle("inkwell:disable-account", (_event, userId: string) => {
        requireAdmin();
        return request<void>(
            `/api/auth/accounts/${encodeURIComponent(userId)}/disable`,
            { method: "POST" },
        );
    });
    ipcMain.handle("inkwell:enable-account", (_event, userId: string) => {
        requireAdmin();
        return request<void>(
            `/api/auth/accounts/${encodeURIComponent(userId)}/enable`,
            { method: "POST" },
        );
    });
    ipcMain.handle(
        "inkwell:reset-account-password",
        (_event, userId: string) => {
            requireAdmin();
            return request<IssuedCredential>(
                `/api/auth/accounts/${encodeURIComponent(userId)}/reset-password`,
                { method: "POST" },
            );
        },
    );
    ipcMain.handle(
        "inkwell:get-agent",
        (_event, agentId: string): Promise<AgentDefinition> => {
            requireAuthenticated();
            return request<AgentDefinition>(
                `/api/agents/${encodeURIComponent(agentId)}`,
            );
        },
    );
    ipcMain.handle(
        "inkwell:create-agent",
        (_event, input: AgentUpsertRequest): Promise<AgentDefinition> => {
            requireAuthenticated();
            return request<AgentDefinition>("/api/agents", {
                method: "POST",
                body: JSON.stringify(input),
            });
        },
    );
    ipcMain.handle(
        "inkwell:update-agent",
        (
            _event,
            agentId: string,
            input: AgentUpsertRequest,
        ): Promise<AgentDefinition> => {
            requireAuthenticated();
            return request<AgentDefinition>(
                `/api/agents/${encodeURIComponent(agentId)}`,
                { method: "PUT", body: JSON.stringify(input) },
            );
        },
    );
    ipcMain.handle(
        "inkwell:clone-agent",
        (_event, agentId: string): Promise<AgentDefinition> => {
            requireAuthenticated();
            return request<AgentDefinition>(
                `/api/agents/${encodeURIComponent(agentId)}/clone`,
                { method: "POST" },
            );
        },
    );
    ipcMain.handle(
        "inkwell:upload-agent-avatar",
        (
            _event,
            file: AgentAvatarUploadFile,
        ): Promise<AgentAvatarUploadResponse> => {
            requireAuthenticated();
            const body = new FormData();
            body.append(
                "file",
                new Blob([file.bytes], { type: file.contentType }),
                file.name,
            );
            return request<AgentAvatarUploadResponse>("/api/agents/avatar", {
                method: "POST",
                body,
            });
        },
    );
    ipcMain.handle(
        "inkwell:publish-agent",
        (
            _event,
            agentId: string,
            changeSummary: string | null,
        ): Promise<AgentVersion> => {
            requireAuthenticated();
            return request<AgentVersion>(
                `/api/agents/${encodeURIComponent(agentId)}/publish`,
                {
                    method: "POST",
                    body: JSON.stringify({ changeSummary }),
                },
            );
        },
    );
    ipcMain.handle(
        "inkwell:list-agent-versions",
        (_event, agentId: string): Promise<AgentVersion[]> => {
            requireAuthenticated();
            return request<AgentVersion[]>(
                `/api/agents/${encodeURIComponent(agentId)}/versions`,
            );
        },
    );
    ipcMain.handle(
        "inkwell:rollback-agent-version",
        (
            _event,
            agentId: string,
            versionId: string,
            changeSummary: string | null,
        ): Promise<AgentVersion> => {
            requireAuthenticated();
            return request<AgentVersion>(
                `/api/agents/${encodeURIComponent(agentId)}/versions/${encodeURIComponent(versionId)}/rollback`,
                {
                    method: "POST",
                    body: JSON.stringify({ changeSummary }),
                },
            );
        },
    );
    ipcMain.handle(
        "inkwell:create-agent-conversation",
        (_event, agentId: string): Promise<AgentConversation> => {
            requireAuthenticated();
            return request<AgentConversation>(
                `/api/agents/${encodeURIComponent(agentId)}/conversations`,
                { method: "POST" },
            );
        },
    );
    ipcMain.handle(
        "inkwell:list-agent-conversations",
        async (
            _event,
            agentId: string,
        ): Promise<AgentConversationListItem[]> => {
            requireAuthenticated();
            return requestAllPages<AgentConversationListItem>(
                `/api/agents/${encodeURIComponent(agentId)}/conversations`,
            );
        },
    );
    ipcMain.handle(
        "inkwell:get-agent-conversation-messages",
        async (
            _event,
            agentId: string,
            conversationId: string,
        ): Promise<ChatMessage[]> => {
            requireAuthenticated();
            const items = await requestAllPages<AgentChatMessageApiResponse>(
                `/api/agents/${encodeURIComponent(agentId)}/conversations/${encodeURIComponent(conversationId)}/messages`,
            );
            return normalizePersistedChatMessages(items);
        },
    );
    ipcMain.handle(
        "inkwell:delete-agent-conversation-message",
        (
            _event,
            agentId: string,
            conversationId: string,
            messageId: string,
        ): Promise<void> => {
            requireAuthenticated();
            return request<void>(
                `/api/agents/${encodeURIComponent(agentId)}/conversations/${encodeURIComponent(conversationId)}/messages/${encodeURIComponent(messageId)}`,
                { method: "DELETE" },
            );
        },
    );
    ipcMain.handle(
        "inkwell:clear-agent-conversation",
        (_event, agentId: string, conversationId: string): Promise<void> => {
            requireAuthenticated();
            return request<void>(
                `/api/agents/${encodeURIComponent(agentId)}/conversations/${encodeURIComponent(conversationId)}/clear`,
                { method: "POST" },
            );
        },
    );
    ipcMain.handle(
        "inkwell:delete-agent-conversation",
        (_event, agentId: string, conversationId: string): Promise<void> => {
            requireAuthenticated();
            return request<void>(
                `/api/agents/${encodeURIComponent(agentId)}/conversations/${encodeURIComponent(conversationId)}`,
                { method: "DELETE" },
            );
        },
    );

    ipcMain.handle(
        "inkwell:chat",
        async (_event, input: ChatRequest): Promise<void> => {
            requireAuthenticated();
            const existingRun = chatRuns.get(input.requestId);
            if (existingRun?.snapshot.status === "running") {
                throw new Error(
                    "A chat run with this request ID is already running.",
                );
            }
            for (
                let index = completedChatRunIds.indexOf(input.requestId);
                index >= 0;
                index = completedChatRunIds.indexOf(input.requestId)
            ) {
                completedChatRunIds.splice(index, 1);
            }

            const controller = new AbortController();
            const run: {
                agentId: string;
                conversationId: string | null;
                controller: AbortController;
                userMessage: ChatMessage;
                snapshot: ChatRunSnapshot;
            } = {
                agentId: input.agentId,
                conversationId: input.conversationId,
                controller,
                userMessage: input.messages.at(-1) ?? {
                    role: "user",
                    content: "",
                },
                snapshot: {
                    requestId: input.requestId,
                    status: "running" as const,
                    content: "",
                    skillActivities: [],
                },
            };
            chatRuns.set(input.requestId, run);
            broadcastChatRun(run.snapshot, true);
            const versionQuery = input.runMode === "draft" ? "?version=draft" : "";
            try {
                const textByMessageId = new Map<string, string>();
                let protocolError: ChatRunError | undefined;
                let pendingUsage: ChatTokenUsage | undefined;
                const messages: Message[] = input.messages.map((message, index) => ({
                    id: message.id ?? `${input.requestId}:message:${index}`,
                    role: message.role,
                    content: message.content,
                }));
                const agent = new HttpAgent({
                    url: `${apiBaseUrl}/agent/${input.agentId}${versionQuery}`,
                    threadId: input.conversationId ?? input.requestId,
                    initialMessages: messages,
                    headers: {
                        Authorization: `Bearer ${sessionToken ?? ""}`,
                        "X-Inkwell-Agent-Run-Mode": input.runMode,
                        ...(input.conversationId
                            ? { "X-Inkwell-Conversation-Id": input.conversationId }
                            : {}),
                    },
                    fetch: async (url, requestInit) => {
                        let response: Response;
                        try {
                            response = await fetch(url, requestInit);
                        } catch (reason) {
                            if (controller.signal.aborted) throw reason;
                            throw new ChatRunFailure({
                                code: "NETWORK_ERROR",
                                reason: "无法连接模型服务，请检查网络后重试。",
                            });
                        }
                        if (!response.ok) {
                            const reason = await getSafeErrorReason(response);
                            if (response.status === 401) await clearAuthentication();
                            throw new ChatRunFailure({
                                code: `HTTP_${response.status}`,
                                reason,
                            });
                        }
                        return response;
                    },
                });

                await agent.runAgent(
                    { runId: input.requestId, abortController: controller },
                    {
                        onTextMessageContentEvent: ({ event }) => {
                            textByMessageId.set(
                                event.messageId,
                                (textByMessageId.get(event.messageId) ?? "") +
                                    event.delta,
                            );
                            run.snapshot = {
                                ...run.snapshot,
                                content: Array.from(textByMessageId.values()).join(""),
                            };
                            broadcastChatRun(run.snapshot);
                        },
                        onToolCallEndEvent: ({ event, toolCallName, toolCallArgs }) => {
                            if (
                                run.snapshot.skillActivities.some(
                                    (activity) => activity.callId === event.toolCallId,
                                )
                            ) {
                                return;
                            }
                            const activity = toToolRunActivity({
                                id: event.toolCallId,
                                function: {
                                    name: toolCallName,
                                    arguments: JSON.stringify(toolCallArgs),
                                },
                            });
                            if (!activity) return;
                            run.snapshot = {
                                ...run.snapshot,
                                skillActivities: [
                                    ...run.snapshot.skillActivities,
                                    activity,
                                ],
                            };
                            broadcastChatRun(run.snapshot, true);
                        },
                        onToolCallResultEvent: ({ event }) => {
                            run.snapshot = {
                                ...run.snapshot,
                                skillActivities: run.snapshot.skillActivities.map(
                                    (activity) =>
                                        activity.callId === event.toolCallId
                                            ? { ...activity, status: "success" }
                                            : activity,
                                ),
                            };
                            broadcastChatRun(run.snapshot, true);
                        },
                        onCustomEvent: ({ event }) => {
                            if (event.name !== "inkwell.token_usage") return;
                            pendingUsage = addAguiTokenUsage(
                                pendingUsage,
                                event.value as AguiTokenUsage,
                            );
                            if (!pendingUsage) return;
                            run.snapshot = {
                                ...run.snapshot,
                                usage: pendingUsage,
                            };
                            broadcastChatRun(run.snapshot, true);
                        },
                        onRunErrorEvent: ({ event }) => {
                            protocolError = {
                                code: event.code ?? "STREAM_ERROR",
                                reason: event.message,
                            };
                        },
                    },
                );

                if (protocolError) throw new ChatRunFailure(protocolError);

                let usage = pendingUsage;
                if (input.conversationId) {
                    const persistedMessages = normalizePersistedChatMessages(
                        await requestAllPages<AgentChatMessageApiResponse>(
                            `/api/agents/${encodeURIComponent(input.agentId)}/conversations/${encodeURIComponent(input.conversationId)}/messages`,
                        ),
                    );
                    usage = [...persistedMessages]
                        .reverse()
                        .find(
                            (message) =>
                                message.role === "assistant" && message.usage,
                        )?.usage;
                }
                finishChatRun(input.requestId, "completed", undefined, usage);
            } catch (reason) {
                if (controller.signal.aborted) {
                    finishChatRun(input.requestId, "stopped");
                    return;
                }

                const error =
                    reason instanceof ChatRunFailure
                        ? reason.error
                        : {
                              code: "STREAM_ERROR",
                              reason: "模型响应流意外中断，请重试。",
                          };
                finishChatRun(input.requestId, "failed", error);
                throw new Error(`${error.code}: ${error.reason}`);
            }
        },
    );
    ipcMain.handle(
        "inkwell:get-chat-run",
        (_event, requestId: string): ChatRunSnapshot | null => {
            requireAuthenticated();
            const snapshot = chatRuns.get(requestId)?.snapshot;
            return snapshot ? copyChatRunSnapshot(snapshot) : null;
        },
    );
    ipcMain.handle(
        "inkwell:get-active-agent-chat-run",
        (_event, agentId: string): ActiveAgentChatRun | null => {
            requireAuthenticated();
            const run = Array.from(chatRuns.values())
                .reverse()
                .find(
                    (candidate) =>
                        candidate.agentId === agentId &&
                        candidate.snapshot.status === "running",
                );
            return run
                ? copyActiveAgentChatRun({
                      agentId: run.agentId,
                      conversationId: run.conversationId,
                      userMessage: run.userMessage,
                      snapshot: run.snapshot,
                  })
                : null;
        },
    );
    ipcMain.handle(
        "inkwell:stop-chat",
        (_event, requestId: string): boolean => {
            requireAuthenticated();
            const run = chatRuns.get(requestId);
            if (!run || run.snapshot.status !== "running") return false;
            run.controller.abort();
            return true;
        },
    );
};

const createWindow = (): void => {
    const window = new BrowserWindow({
        width: 1440,
        height: 920,
        minWidth: 1080,
        minHeight: 720,
        icon: applicationIconPath,
        show: false,
        title: "Inkwell",
        backgroundColor: "#f3f5f7",
        webPreferences: {
            preload: join(__dirname, "../preload/index.cjs"),
            contextIsolation: true,
            nodeIntegration: false,
            sandbox: true,
        },
    });

    window.once("ready-to-show", () => window.show());
    window.webContents.setWindowOpenHandler(({ url }) => {
        if (url.startsWith("https://")) {
            void shell.openExternal(url);
        }

        return { action: "deny" };
    });

    if (process.env.ELECTRON_RENDERER_URL) {
        void window.loadURL(process.env.ELECTRON_RENDERER_URL);
    } else {
        void window.loadFile(join(__dirname, "../renderer/index.html"));
    }
};

app.whenReady().then(() => {
    app.dock?.setIcon(applicationIconPath);
    protocol.handle("inkwell", (protocolRequest) => {
        const resource = new URL(protocolRequest.url);
        if (resource.hostname !== "agent-avatars" || !sessionToken) {
            return new Response(null, { status: 404 });
        }

        const key = resource.pathname
            .split("/")
            .filter(Boolean)
            .map((segment) => encodeURIComponent(decodeURIComponent(segment)))
            .join("/");
        return fetch(`${apiBaseUrl}/api/agents/avatar/${key}`, {
            headers: { Authorization: `Bearer ${sessionToken}` },
        });
    });
    registerApiHandlers();
    powerMonitor.on("lock-screen", lockAuthentication);
    app.on("browser-window-blur", scheduleIdleLock);
    createWindow();

    app.on("activate", () => {
        if (BrowserWindow.getAllWindows().length === 0) {
            createWindow();
        }
    });
});

app.on("window-all-closed", () => {
    if (process.platform !== "darwin") {
        app.quit();
    }
});
