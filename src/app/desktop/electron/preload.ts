import { contextBridge, ipcRenderer } from 'electron'
import type { InkwellDesktopApi } from '../src/shared/network/contracts.js'

const api: InkwellDesktopApi = {
  platform: process.platform,
  getAppMetadata: () => ipcRenderer.invoke('inkwell:app-metadata'),
  restoreAuth: () => ipcRenderer.invoke('inkwell:restore-auth'),
  login: (request) => ipcRenderer.invoke('inkwell:login', request),
  unlock: (password) => ipcRenderer.invoke('inkwell:unlock', password),
  logout: () => ipcRenderer.invoke('inkwell:logout'),
  reportActivity: () => ipcRenderer.send('inkwell:activity'),
  onAuthStateChanged: (listener) => {
    const handler = (_event: Electron.IpcRendererEvent, snapshot: Parameters<typeof listener>[0]): void => {
      listener(snapshot)
    }
    ipcRenderer.on('inkwell:auth-state-changed', handler)
    return () => ipcRenderer.removeListener('inkwell:auth-state-changed', handler)
  },
  listAgents: () => ipcRenderer.invoke('inkwell:list-agents'),
  listTools: () => ipcRenderer.invoke('inkwell:list-tools'),
  listModels: () => ipcRenderer.invoke('inkwell:list-models'),
  getModelManagementInfo: () => ipcRenderer.invoke('inkwell:model-management-info'),
  testModel: (modelId) => ipcRenderer.invoke('inkwell:test-model', modelId),
  openExternal: (url) => ipcRenderer.invoke('inkwell:open-external', url),
  listAccounts: () => ipcRenderer.invoke('inkwell:list-accounts'),
  unlockAccount: (userId) => ipcRenderer.invoke('inkwell:unlock-account', userId),
  createAgent: (request) => ipcRenderer.invoke('inkwell:create-agent', request),
  chat: (request) => ipcRenderer.invoke('inkwell:chat', request),
  onChatDelta: (listener) => {
    const handler = (_event: Electron.IpcRendererEvent, requestId: string, content: string): void => {
      listener(requestId, content)
    }
    ipcRenderer.on('inkwell:chat-delta', handler)
    return () => ipcRenderer.removeListener('inkwell:chat-delta', handler)
  },
}

contextBridge.exposeInMainWorld('inkwell', api)