export interface LoginRequest {
    username: string;
    password: string;
}

export type LoginFailureCode =
    | "invalid-credentials"
    | "account-locked"
    | "rate-limited"
    | "offline"
    | "unknown";

export type LoginResult =
    | { ok: true; identity: AuthIdentity }
    | { ok: false; code: LoginFailureCode };

export type UnlockFailureCode =
    | "invalid-password"
    | "account-locked"
    | "offline"
    | "unknown";

export type UnlockResult =
    | { ok: true; identity: AuthIdentity }
    | { ok: false; code: UnlockFailureCode };

export interface AuthIdentity {
    userId: string;
    username: string;
    isSuper: boolean;
    expiresAt: string;
}

export type AuthStatus =
    | "anonymous"
    | "restoring"
    | "authenticated"
    | "locked"
    | "offline";

export interface AuthSnapshot {
    status: AuthStatus;
    identity: AuthIdentity | null;
}

export interface AppMetadata {
    version: string;
    buildNumber: string | null;
    commit: string | null;
}

export interface AgentListItem {
    id: string;
    name: string;
    avatarUri: string | null;
    descriptionExcerpt: string | null;
    ownerUserId: string;
    isShared: boolean;
    latestPublishedVersionNumber: number;
    updatedTime: string;
}

export interface AgentDefinition {
    id: string;
    ownerUserId: string;
    latestPublishedVersionNumber: number;
}

export interface ModelDefinition {
    id: string;
    displayName: string;
    publisherDisplayName: string | null;
    familyDisplayName: string | null;
    sourceId: string;
    runtimeId: string;
    isAvailable: boolean;
    unavailableReason: string | null;
}

export interface CreateAgentRequest {
    name: string;
    description: string;
    instructions: string;
    modelId: string;
}

export interface ChatMessage {
    role: "user" | "assistant";
    content: string;
}

export interface ChatRequest {
    requestId: string;
    agentId: string;
    messages: ChatMessage[];
}

export interface InkwellDesktopApi {
    platform: string;
    getAppMetadata: () => Promise<AppMetadata>;
    restoreAuth: () => Promise<AuthSnapshot>;
    login: (request: LoginRequest) => Promise<LoginResult>;
    unlock: (password: string) => Promise<UnlockResult>;
    logout: () => Promise<void>;
    reportActivity: () => void;
    onAuthStateChanged: (
        listener: (snapshot: AuthSnapshot) => void,
    ) => () => void;
    listAgents: () => Promise<AgentListItem[]>;
    listModels: () => Promise<ModelDefinition[]>;
    createAgent: (request: CreateAgentRequest) => Promise<AgentDefinition>;
    chat: (request: ChatRequest) => Promise<void>;
    onChatDelta: (
        listener: (requestId: string, content: string) => void,
    ) => () => void;
}
