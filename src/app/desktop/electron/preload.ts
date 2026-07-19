import { contextBridge, ipcRenderer } from "electron";
import type { InkwellDesktopApi } from "../src/shared/network/contracts.js";

const api: InkwellDesktopApi = {
    platform: process.platform,
    getAppMetadata: () => ipcRenderer.invoke("inkwell:app-metadata"),
    restoreAuth: () => ipcRenderer.invoke("inkwell:restore-auth"),
    login: (request) => ipcRenderer.invoke("inkwell:login", request),
    unlock: (password) => ipcRenderer.invoke("inkwell:unlock", password),
    changePassword: (request) =>
        ipcRenderer.invoke("inkwell:change-password", request),
    logout: () => ipcRenderer.invoke("inkwell:logout"),
    reportActivity: () => ipcRenderer.send("inkwell:activity"),
    onAuthStateChanged: (listener) => {
        const handler = (
            _event: Electron.IpcRendererEvent,
            snapshot: Parameters<typeof listener>[0],
        ): void => {
            listener(snapshot);
        };
        ipcRenderer.on("inkwell:auth-state-changed", handler);
        return () =>
            ipcRenderer.removeListener("inkwell:auth-state-changed", handler);
    },
    listMyAgents: () => ipcRenderer.invoke("inkwell:list-my-agents"),
    listSharedAgents: () => ipcRenderer.invoke("inkwell:list-shared-agents"),
    deleteAgent: (agentId) =>
        ipcRenderer.invoke("inkwell:delete-agent", agentId),
    shareAgent: (agentId) => ipcRenderer.invoke("inkwell:share-agent", agentId),
    unshareAgent: (agentId) =>
        ipcRenderer.invoke("inkwell:unshare-agent", agentId),
    revokeAgentShare: (agentId) =>
        ipcRenderer.invoke("inkwell:revoke-agent-share", agentId),
    listTools: () => ipcRenderer.invoke("inkwell:list-tools"),
    listSkills: () => ipcRenderer.invoke("inkwell:list-skills"),
    uploadSkill: (file) => ipcRenderer.invoke("inkwell:upload-skill", file),
    updateSkill: (skillId, request) =>
        ipcRenderer.invoke("inkwell:update-skill", skillId, request),
    deleteSkill: (skillId) =>
        ipcRenderer.invoke("inkwell:delete-skill", skillId),
    listModels: () => ipcRenderer.invoke("inkwell:list-models"),
    getModelManagementInfo: () =>
        ipcRenderer.invoke("inkwell:model-management-info"),
    testModel: (modelId) => ipcRenderer.invoke("inkwell:test-model", modelId),
    openExternal: (url) => ipcRenderer.invoke("inkwell:open-external", url),
    listAccounts: () => ipcRenderer.invoke("inkwell:list-accounts"),
    createAccount: (request) =>
        ipcRenderer.invoke("inkwell:create-account", request),
    unlockAccount: (userId) =>
        ipcRenderer.invoke("inkwell:unlock-account", userId),
    disableAccount: (userId) =>
        ipcRenderer.invoke("inkwell:disable-account", userId),
    enableAccount: (userId) =>
        ipcRenderer.invoke("inkwell:enable-account", userId),
    resetAccountPassword: (userId) =>
        ipcRenderer.invoke("inkwell:reset-account-password", userId),
    getAgent: (agentId) => ipcRenderer.invoke("inkwell:get-agent", agentId),
    createAgent: (request) =>
        ipcRenderer.invoke("inkwell:create-agent", request),
    updateAgent: (agentId, request) =>
        ipcRenderer.invoke("inkwell:update-agent", agentId, request),
    cloneAgent: (agentId) => ipcRenderer.invoke("inkwell:clone-agent", agentId),
    uploadAgentAvatar: (file) =>
        ipcRenderer.invoke("inkwell:upload-agent-avatar", file),
    publishAgent: (agentId, changeSummary) =>
        ipcRenderer.invoke("inkwell:publish-agent", agentId, changeSummary),
    listAgentVersions: (agentId) =>
        ipcRenderer.invoke("inkwell:list-agent-versions", agentId),
    chat: (request) => ipcRenderer.invoke("inkwell:chat", request),
    onChatDelta: (listener) => {
        const handler = (
            _event: Electron.IpcRendererEvent,
            requestId: string,
            content: string,
        ): void => {
            listener(requestId, content);
        };
        ipcRenderer.on("inkwell:chat-delta", handler);
        return () => ipcRenderer.removeListener("inkwell:chat-delta", handler);
    },
};

contextBridge.exposeInMainWorld("inkwell", api);
