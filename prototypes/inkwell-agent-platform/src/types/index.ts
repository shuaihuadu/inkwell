/**
 * 全局类型定义
 * 用途：原型范围内的"看得见"字段；与 H3 详细设计无追溯关系。
 */

export type RoleKey = 'member' | 'owner' | 'admin';

export interface RoleInfo {
  key: RoleKey;
  username: string;
  isSuper: boolean;
  label: string;
}

export interface AgentSummary {
  id: string;
  name: string;
  description: string;
  avatar?: string;
  ownerName: string;
  ownerId: string;
  lastUsedAt: string;
  shared: boolean;
  /** 当前用户视角下的归属：mine / shared / used */
  visibility: Array<'mine' | 'shared' | 'used'>;
}

export interface AgentVersion {
  version: string;
  savedAt: string;
  savedBy: string;
  note: string;
}

export interface ToolMeta {
  id: string;
  name: string;
  description: string;
  params: Array<{
    name: string;
    type: 'string' | 'number' | 'boolean' | 'select';
    required: boolean;
    options?: string[];
  }>;
}

export interface SkillMeta {
  id: string;
  name: string;
  version: string;
  description: string;
  hasScripts: boolean;
}

export interface KnowledgeDoc {
  id: string;
  filename: string;
  type: 'pdf' | 'word' | 'markdown' | 'text';
  sizeKb: number;
  uploadedAt: string;
  status: '待解析' | '解析中' | '解析成功' | '解析失败';
  failReason?: string;
}

export interface ConversationMeta {
  id: string;
  agentId: string;
  agentName: string;
  title: string;
  startedAt: string;
}

export interface ConversationMessage {
  id: string;
  role: 'user' | 'agent' | 'tool' | 'system';
  content: string;
  status?: '已完成' | '发送中' | '流式中' | '已停止' | '失败';
  timestamp: string;
}

export interface OrchestrationNodeMock {
  id: string;
  label: string;
  agentName: string;
  lockedVersion: string;
}

export interface OrchestrationEdgeMock {
  id: string;
  source: string;
  target: string;
}

export interface AuditEntry {
  id: string;
  time: string;
  actor: string;
  eventType:
    | '登录'
    | 'Agent CRUD'
    | 'Agent 调用'
    | '版本回滚'
    | '公开 API 调用'
    | 'admin_unlock_account'
    | 'admin_revoke_share';
  agentName: string;
  detail: string;
}

export interface LockedAccount {
  username: string;
  status: '正常' | '已锁';
  lastActiveAt: string;
}

export interface SharedRow {
  agentId: string;
  agentName: string;
  ownerName: string;
  sharedAt: string;
}

export interface TraceMeta {
  id: string;
  source: string; // 对话 ID / 编排执行 ID
  user: string;
  agentName: string;
  status: '成功' | '失败';
  startedAt: string;
}
