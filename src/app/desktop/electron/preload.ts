import { contextBridge, ipcRenderer } from 'electron'
import type { InkwellDesktopApi } from '../src/shared/network/contracts.js'

const api: InkwellDesktopApi = {
  platform: process.platform,
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
  listModels: () => ipcRenderer.invoke('inkwell:list-models'),
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