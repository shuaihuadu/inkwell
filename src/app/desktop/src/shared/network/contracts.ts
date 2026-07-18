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

export type LLMModelCategory =
    | "Unknown"
    | "Chat"
    | "Embedding"
    | "ImageGeneration"
    | "VideoGeneration";

export interface LLMModel {
    id: string;
    category: LLMModelCategory;
    providerMode: string | null;
    ownedBy: string | null;
    maxInputTokens: number | null;
    maxOutputTokens: number | null;
    supportsVision: boolean | null;
    supportsTools: boolean | null;
    supportsStructuredOutput: boolean | null;
    supportsReasoning: boolean | null;
}

export interface LLMModelTestResult {
    modelId: string;
    isSuccess: boolean;
    latency: string;
    errorMessage: string | null;
}

export interface LLMProviderManagementInfo {
    dashboardUrl: string | null;
}

export interface AgentToolDefinition {
    id: string;
    name: string;
    description: string;
    parametersJsonSchema: string;
    createdTime: string;
    updatedTime: string;
}

export interface UserListItem {
    userId: string;
    username: string;
    isSuper: boolean;
    isLocked: boolean;
    lastLoginTime: string | null;
    createdTime: string;
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
    listTools: () => Promise<AgentToolDefinition[]>;
    listModels: () => Promise<LLMModel[]>;
    getModelManagementInfo: () => Promise<LLMProviderManagementInfo>;
    testModel: (modelId: string) => Promise<LLMModelTestResult>;
    openExternal: (url: string) => Promise<void>;
    listAccounts: () => Promise<UserListItem[]>;
    unlockAccount: (userId: string) => Promise<void>;
    createAgent: (request: CreateAgentRequest) => Promise<AgentDefinition>;
    chat: (request: ChatRequest) => Promise<void>;
    onChatDelta: (
        listener: (requestId: string, content: string) => void,
    ) => () => void;
}
