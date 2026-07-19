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
    isAdmin: boolean;
    mustChangePassword: boolean;
    expiresAt: string;
}

export interface ChangePasswordRequest {
    currentPassword: string;
    newPassword: string;
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
    name: string;
    avatarUri: string | null;
    description: string | null;
    instructions: string | null;
    buildOptions: AgentBuildOptions;
    currentPublishedVersionId: string | null;
    latestPublishedVersionNumber: number;
    isShared: boolean;
    sharedRevokedByAdminTime: string | null;
    createdTime: string;
    updatedTime: string;
}

export interface AgentModelOptions {
    modelId: string | null;
    temperature: number | null;
    topP: number | null;
    maxTokens: number | null;
}

export interface AgentChatHistoryOptions {
    maxMessages: number | null;
    reducerType: string | null;
    maxMessagesToRetrieve: number | null;
}

export interface AgentToolBinding {
    toolId: string;
    parametersJson: string | null;
}

export interface AgentSkillBinding {
    skillId: string;
}

export interface AgentBuildOptions {
    modelOptions: AgentModelOptions;
    chatHistoryOptions: AgentChatHistoryOptions | null;
    toolBindings?: AgentToolBinding[];
    skills?: AgentSkillDefinition[];
}

export interface AgentUpsertRequest {
    name: string;
    avatarUri: string | null;
    description: string | null;
    instructions: string | null;
    modelOptions: AgentModelOptions;
    chatHistoryOptions: AgentChatHistoryOptions | null;
    toolBindings: AgentToolBinding[];
    skillBindings: AgentSkillBinding[];
}

export interface AgentVersion {
    id: string;
    agentId: string;
    versionNumber: number;
    createdByUserId: string;
    changeSummary: string | null;
    createdTime: string;
    updatedTime: string;
    publishedTime: string | null;
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

export interface AgentSkillDefinition {
    id: string;
    ownerUserId: string;
    name: string;
    description: string;
    content: string;
    referenceFileUris: string[];
    assetFileUris: string[];
    scriptFileUris: string[];
    createdTime: string;
    updatedTime: string;
}

export interface AgentSkillUpdateRequest {
    name: string;
    description: string;
    content: string;
}

export interface AgentSkillUploadFile {
    name: string;
    bytes: Uint8Array;
}

export interface AgentAvatarUploadFile {
    name: string;
    contentType: string;
    bytes: Uint8Array;
}

export interface AgentAvatarUploadResponse {
    avatarUri: string;
}

export interface UserListItem {
    userId: string;
    username: string;
    isAdmin: boolean;
    isLocked: boolean;
    isDisabled: boolean;
    lastLoginTime: string | null;
    createdTime: string;
}

export interface CreateAccountRequest {
    username: string;
    isAdmin: boolean;
}

export interface IssuedCredential {
    userId: string;
    username: string;
    temporaryPassword: string;
}

export interface ChatMessage {
    role: "user" | "assistant";
    content: string;
}

export interface ChatRequest {
    requestId: string;
    agentId: string;
    runMode: "published" | "draft";
    messages: ChatMessage[];
}

export interface InkwellDesktopApi {
    platform: string;
    getAppMetadata: () => Promise<AppMetadata>;
    restoreAuth: () => Promise<AuthSnapshot>;
    login: (request: LoginRequest) => Promise<LoginResult>;
    unlock: (password: string) => Promise<UnlockResult>;
    changePassword: (request: ChangePasswordRequest) => Promise<AuthIdentity>;
    logout: () => Promise<void>;
    reportActivity: () => void;
    onAuthStateChanged: (
        listener: (snapshot: AuthSnapshot) => void,
    ) => () => void;
    listMyAgents: () => Promise<AgentListItem[]>;
    listSharedAgents: () => Promise<AgentListItem[]>;
    deleteAgent: (agentId: string) => Promise<void>;
    shareAgent: (agentId: string) => Promise<void>;
    unshareAgent: (agentId: string) => Promise<void>;
    revokeAgentShare: (agentId: string) => Promise<void>;
    listTools: () => Promise<AgentToolDefinition[]>;
    listSkills: () => Promise<AgentSkillDefinition[]>;
    uploadSkill: (file: AgentSkillUploadFile) => Promise<AgentSkillDefinition>;
    updateSkill: (
        skillId: string,
        request: AgentSkillUpdateRequest,
    ) => Promise<AgentSkillDefinition>;
    deleteSkill: (skillId: string) => Promise<void>;
    listModels: () => Promise<LLMModel[]>;
    getModelManagementInfo: () => Promise<LLMProviderManagementInfo>;
    testModel: (modelId: string) => Promise<LLMModelTestResult>;
    openExternal: (url: string) => Promise<void>;
    listAccounts: () => Promise<UserListItem[]>;
    createAccount: (request: CreateAccountRequest) => Promise<IssuedCredential>;
    unlockAccount: (userId: string) => Promise<void>;
    disableAccount: (userId: string) => Promise<void>;
    enableAccount: (userId: string) => Promise<void>;
    resetAccountPassword: (userId: string) => Promise<IssuedCredential>;
    getAgent: (agentId: string) => Promise<AgentDefinition>;
    createAgent: (request: AgentUpsertRequest) => Promise<AgentDefinition>;
    updateAgent: (
        agentId: string,
        request: AgentUpsertRequest,
    ) => Promise<AgentDefinition>;
    cloneAgent: (agentId: string) => Promise<AgentDefinition>;
    uploadAgentAvatar: (
        file: AgentAvatarUploadFile,
    ) => Promise<AgentAvatarUploadResponse>;
    publishAgent: (
        agentId: string,
        changeSummary: string | null,
    ) => Promise<AgentVersion>;
    listAgentVersions: (agentId: string) => Promise<AgentVersion[]>;
    chat: (request: ChatRequest) => Promise<void>;
    onChatDelta: (
        listener: (requestId: string, content: string) => void,
    ) => () => void;
}
