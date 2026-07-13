import { contextBridge, ipcRenderer } from 'electron'
import type { InkwellDesktopApi } from '../src/shared/network/contracts.js'

const api: InkwellDesktopApi = {
  platform: process.platform,
  login: (request) => ipcRenderer.invoke('inkwell:login', request),
  logout: () => ipcRenderer.invoke('inkwell:logout'),
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