import {
    app,
    BrowserWindow,
    ipcMain,
    powerMonitor,
    safeStorage,
    shell,
} from "electron";
import { readFile, rename, rm, writeFile } from "node:fs/promises";
import { join } from "node:path";
import type {
    AgentDefinition,
    AgentListItem,
    AppMetadata,
    AuthIdentity,
    AuthSnapshot,
    AuthStatus,
    ChatRequest,
    CreateAgentRequest,
    LoginRequest,
    LoginResult,
    LLMModel,
    LLMModelTestResult,
    UnlockResult,
} from "../src/shared/network/contracts.js";

const apiBaseUrl = (
    process.env.INKWELL_WEBAPI_URL ?? "http://localhost:6801"
).replace(/\/$/, "");
const authSessionFileName = "auth-session.bin";
const idleLockMilliseconds = 5 * 60 * 1000;
const applicationIconPath = join(__dirname, "../renderer/logo.png");
let sessionToken: string | null = null;
let authSnapshot: AuthSnapshot = { status: "restoring", identity: null };
let idleLockTimer: NodeJS.Timeout | null = null;

interface InternalAuthSession extends AuthIdentity {
    sessionToken: string;
}

class ApiRequestError extends Error {
    public readonly status: number;

    public constructor(status: number, message: string) {
        super(message);
        this.status = status;
    }
}

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
            ...(init?.body ? { "Content-Type": "application/json" } : {}),
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

const requireAuthenticated = (): void => {
    if (authSnapshot.status !== "authenticated" || !sessionToken) {
        throw new Error(
            authSnapshot.status === "locked"
                ? "Client is locked."
                : "Authentication is required.",
        );
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
    isSuper: session.isSuper,
    expiresAt: session.expiresAt,
});

const registerApiHandlers = (): void => {
    ipcMain.handle(
        "inkwell:app-metadata",
        (): AppMetadata => ({
            version: app.getVersion(),
            buildNumber: process.env.INKWELL_BUILD_NUMBER ?? null,
            commit: process.env.INKWELL_COMMIT_SHA ?? null,
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

    ipcMain.on("inkwell:activity", scheduleIdleLock);
    ipcMain.handle("inkwell:list-agents", () => {
        requireAuthenticated();
        return request<AgentListItem[]>("/api/agents/mine");
    });
    ipcMain.handle("inkwell:list-models", () => {
        requireAuthenticated();
        return request<LLMModel[]>("/api/models");
    });
    ipcMain.handle("inkwell:test-model", (_event, modelId: string) => {
        requireAuthenticated();
        return request<LLMModelTestResult>(
            `/api/models/${encodeURIComponent(modelId)}/test`,
            { method: "POST" },
        );
    });
    ipcMain.handle(
        "inkwell:create-agent",
        async (_event, input: CreateAgentRequest): Promise<AgentDefinition> => {
            requireAuthenticated();
            const agent = await request<AgentDefinition>("/api/agents", {
                method: "POST",
                body: JSON.stringify(input),
            });
            await request(`/api/agents/${agent.id}/publish`, {
                method: "POST",
            });
            return agent;
        },
    );

    ipcMain.handle(
        "inkwell:chat",
        async (event, input: ChatRequest): Promise<void> => {
            requireAuthenticated();
            const response = await fetch(
                `${apiBaseUrl}/agent/${input.agentId}/v1/chat/completions`,
                {
                    method: "POST",
                    headers: {
                        Accept: "text/event-stream",
                        Authorization: `Bearer ${sessionToken ?? ""}`,
                        "Content-Type": "application/json",
                    },
                    body: JSON.stringify({
                        model: "inkwell",
                        messages: input.messages,
                        stream: true,
                    }),
                },
            );

            if (!response.ok || !response.body) {
                const detail = await response.text();
                if (response.status === 401) await clearAuthentication();
                throw new Error(
                    detail ||
                        `Agent request failed with status ${response.status}.`,
                );
            }

            const reader = response.body.getReader();
            const decoder = new TextDecoder();
            let buffer = "";

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                buffer += decoder.decode(value, { stream: true });
                const lines = buffer.split("\n");
                buffer = lines.pop() ?? "";

                for (const line of lines) {
                    const data = line.trim().replace(/^data:\s*/, "");
                    if (!data || data === "[DONE]") continue;

                    const chunk = JSON.parse(data) as {
                        choices?: Array<{ delta?: { content?: string } }>;
                    };
                    const content = chunk.choices?.[0]?.delta?.content;
                    if (content) {
                        event.sender.send(
                            "inkwell:chat-delta",
                            input.requestId,
                            content,
                        );
                    }
                }
            }
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
