export interface LoginRequest {
  username: string
  password: string
}

export interface AuthSession {
  userId: string
  username: string
  isSuper: boolean
  sessionToken: string
  expiresAt: string
}

export interface AgentSummary {
  id: string
  name: string
  avatarUri: string | null
  descriptionExcerpt: string | null
  ownerUserId: string
  isShared: boolean
  latestPublishedVersionNumber: number
  updatedTime: string
}

export interface AgentDefinition {
  id: string
  ownerUserId: string
  latestPublishedVersionNumber: number
}

export interface ModelDefinition {
  id: string
  displayName: string
  publisherDisplayName: string | null
  familyDisplayName: string | null
  sourceId: string
  runtimeId: string
  isAvailable: boolean
  unavailableReason: string | null
}

export interface CreateAgentRequest {
  name: string
  description: string
  instructions: string
  modelId: string
}

export interface ChatMessage {
  role: 'user' | 'assistant'
  content: string
}

export interface ChatRequest {
  requestId: string
  agentId: string
  messages: ChatMessage[]
}

export interface InkwellDesktopApi {
  platform: string
  login: (request: LoginRequest) => Promise<AuthSession>
  logout: () => Promise<void>
  listAgents: () => Promise<AgentSummary[]>
  listModels: () => Promise<ModelDefinition[]>
  createAgent: (request: CreateAgentRequest) => Promise<AgentDefinition>
  chat: (request: ChatRequest) => Promise<void>
  onChatDelta: (listener: (requestId: string, content: string) => void) => () => void
}