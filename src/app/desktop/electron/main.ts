import { app, BrowserWindow, ipcMain, shell } from 'electron'
import { join } from 'node:path'
import type {
  AgentDefinition,
  AgentSummary,
  AuthSession,
  ChatRequest,
  CreateAgentRequest,
  LoginRequest,
  ModelDefinition,
} from '../src/shared/network/contracts.js'

const apiBaseUrl = (process.env.INKWELL_WEBAPI_URL ?? 'http://localhost:6801').replace(/\/$/, '')
let sessionToken: string | null = null

const request = async <T>(path: string, init?: RequestInit): Promise<T> => {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      Accept: 'application/json',
      ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
      ...(sessionToken ? { Authorization: `Bearer ${sessionToken}` } : {}),
      ...init?.headers,
    },
  })

  if (!response.ok) {
    const detail = await response.text()
    throw new Error(detail || `Inkwell API request failed with status ${response.status}.`)
  }

  return response.status === 204 ? (undefined as T) : (response.json() as Promise<T>)
}

const registerApiHandlers = (): void => {
  ipcMain.handle('inkwell:login', async (_event, login: LoginRequest): Promise<AuthSession> => {
    const session = await request<AuthSession>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify(login),
    })
    sessionToken = session.sessionToken
    return { ...session, sessionToken: '' }
  })

  ipcMain.handle('inkwell:logout', async (): Promise<void> => {
    try {
      await request<void>('/api/auth/logout', { method: 'POST' })
    } finally {
      sessionToken = null
    }
  })

  ipcMain.handle('inkwell:list-agents', () => request<AgentSummary[]>('/api/agents/mine'))
  ipcMain.handle('inkwell:list-models', () => request<ModelDefinition[]>('/api/models'))
  ipcMain.handle('inkwell:create-agent', async (_event, input: CreateAgentRequest): Promise<AgentDefinition> => {
    const agent = await request<AgentDefinition>('/api/agents', {
      method: 'POST',
      body: JSON.stringify(input),
    })
    await request(`/api/agents/${agent.id}/publish`, { method: 'POST' })
    return agent
  })

  ipcMain.handle('inkwell:chat', async (event, input: ChatRequest): Promise<void> => {
    const response = await fetch(`${apiBaseUrl}/agent/${input.agentId}/v1/chat/completions`, {
      method: 'POST',
      headers: {
        Accept: 'text/event-stream',
        Authorization: `Bearer ${sessionToken ?? ''}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        model: 'inkwell',
        messages: input.messages,
        stream: true,
      }),
    })

    if (!response.ok || !response.body) {
      const detail = await response.text()
      throw new Error(detail || `Agent request failed with status ${response.status}.`)
    }

    const reader = response.body.getReader()
    const decoder = new TextDecoder()
    let buffer = ''

    while (true) {
      const { done, value } = await reader.read()
      if (done) break

      buffer += decoder.decode(value, { stream: true })
      const lines = buffer.split('\n')
      buffer = lines.pop() ?? ''

      for (const line of lines) {
        const data = line.trim().replace(/^data:\s*/, '')
        if (!data || data === '[DONE]') continue

        const chunk = JSON.parse(data) as { choices?: Array<{ delta?: { content?: string } }> }
        const content = chunk.choices?.[0]?.delta?.content
        if (content) {
          event.sender.send('inkwell:chat-delta', input.requestId, content)
        }
      }
    }
  })
}

const createWindow = (): void => {
  const window = new BrowserWindow({
    width: 1440,
    height: 920,
    minWidth: 1080,
    minHeight: 720,
    show: false,
    title: 'Inkwell',
    backgroundColor: '#f3f5f7',
    webPreferences: {
      preload: join(__dirname, '../preload/index.cjs'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
    },
  })

  window.once('ready-to-show', () => window.show())
  window.webContents.setWindowOpenHandler(({ url }) => {
    if (url.startsWith('https://')) {
      void shell.openExternal(url)
    }

    return { action: 'deny' }
  })

  if (process.env.ELECTRON_RENDERER_URL) {
    void window.loadURL(process.env.ELECTRON_RENDERER_URL)
  } else {
    void window.loadFile(join(__dirname, '../renderer/index.html'))
  }
}

app.whenReady().then(() => {
  registerApiHandlers()
  createWindow()

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow()
    }
  })
})

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit()
  }
})