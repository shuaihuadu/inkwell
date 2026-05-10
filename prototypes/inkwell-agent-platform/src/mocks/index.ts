import {
  OrchestrationEdgeMock,
  OrchestrationNodeMock,
  AuditEntry,
  LockedAccount,
  SharedRow,
  TraceMeta,
  ToolMeta,
  SkillMeta,
  KnowledgeDoc
} from '../types';

export const MOCK_NODES: OrchestrationNodeMock[] = [
  { id: 'n-1', label: '识别工单类型', agentName: '客服质检助手', lockedVersion: 'v3' },
  { id: 'n-2', label: '抽取关键条款', agentName: '合同条款摘要', lockedVersion: 'v2' },
  { id: 'n-3', label: '生成回复初稿', agentName: '客户调研对话伙伴', lockedVersion: 'v1' }
];

export const MOCK_EDGES: OrchestrationEdgeMock[] = [
  { id: 'e-1', source: 'n-1', target: 'n-2' },
  { id: 'e-2', source: 'n-2', target: 'n-3' }
];

export const MOCK_AUDIT: AuditEntry[] = [
  {
    id: 'aud-001',
    time: '2026-05-08 14:35:01',
    actor: 'owner-bob',
    eventType: 'Agent 调用',
    agentName: '客服质检助手',
    detail: '通过公开 API 调用 v3，外部 Token 末四位 ****a0c1'
  },
  {
    id: 'aud-002',
    time: '2026-05-08 11:12:33',
    actor: 'sa-carol',
    eventType: 'admin_revoke_share',
    agentName: '团队周报生成器',
    detail: '撤销 alice 的共享，理由：内部评审发现敏感字段泄漏风险'
  },
  {
    id: 'aud-003',
    time: '2026-05-08 09:01:11',
    actor: 'sa-carol',
    eventType: 'admin_unlock_account',
    agentName: '-',
    detail: '解封 dave 账号'
  },
  {
    id: 'aud-004',
    time: '2026-05-07 16:40:09',
    actor: 'owner-bob',
    eventType: '版本回滚',
    agentName: '客服质检助手',
    detail: '回滚到 v2，新生成 v3'
  },
  {
    id: 'aud-005',
    time: '2026-05-07 08:00:00',
    actor: 'alice',
    eventType: '登录',
    agentName: '-',
    detail: '设备：macOS 12+ Apple Silicon'
  }
];

export const MOCK_LOCKED_ACCOUNTS: LockedAccount[] = [
  { username: 'dave', status: '已锁', lastActiveAt: '2026-05-07 22:15' },
  { username: 'frank', status: '已锁', lastActiveAt: '2026-05-06 09:30' }
];

export const MOCK_SHARED_ROWS: SharedRow[] = [
  {
    agentId: 'agent-001',
    agentName: '客服质检助手',
    ownerName: 'owner-bob',
    sharedAt: '2026-05-04 09:00'
  },
  {
    agentId: 'agent-003',
    agentName: '团队周报生成器',
    ownerName: 'alice',
    sharedAt: '2026-05-02 10:15'
  },
  {
    agentId: 'agent-004',
    agentName: '代码审查辅助',
    ownerName: 'dave',
    sharedAt: '2026-04-28 14:50'
  }
];

export const MOCK_TRACES: TraceMeta[] = [
  {
    id: 'trace-001',
    source: 'conv-001',
    user: 'owner-bob',
    agentName: '客服质检助手',
    status: '成功',
    startedAt: '2026-05-08 14:32:01'
  },
  {
    id: 'trace-002',
    source: 'orch-run-007',
    user: 'owner-bob',
    agentName: '客服质检 → 合同摘要 → 回复初稿',
    status: '失败',
    startedAt: '2026-05-08 11:00:00'
  }
];

export const MOCK_TOOLS: ToolMeta[] = [
  {
    id: 'tool-search-compliance',
    name: 'search_compliance_rule',
    description: '在合规规则库中按关键词搜索条款',
    params: [
      { name: 'keyword', type: 'string', required: true },
      { name: 'top_k', type: 'number', required: false }
    ]
  },
  {
    id: 'tool-calendar-fetch',
    name: 'calendar_fetch',
    description: '从内部日历拉取指定时间段会议',
    params: [
      { name: 'date_from', type: 'string', required: true },
      { name: 'date_to', type: 'string', required: true }
    ]
  }
];

export const MOCK_SKILLS: SkillMeta[] = [
  {
    id: 'skill-cs-sop',
    name: '客服质检 SOP',
    version: 'v2.1',
    description: '内部客服话术合规检查 SOP（agentskills.io 格式）',
    hasScripts: false
  },
  {
    id: 'skill-contract-clauses',
    name: '合同关键条款字典',
    version: 'v1.0',
    description: '常见合同条款解释与红线（仅 SKILL.md + references/）',
    hasScripts: false
  }
];

export const MOCK_KNOWLEDGE: KnowledgeDoc[] = [
  {
    id: 'doc-001',
    filename: '合同条款汇编.pdf',
    type: 'pdf',
    sizeKb: 2150,
    uploadedAt: '2026-05-04 09:01',
    status: '解析成功'
  },
  {
    id: 'doc-002',
    filename: '客服话术红线.md',
    type: 'markdown',
    sizeKb: 18,
    uploadedAt: '2026-05-05 11:30',
    status: '解析成功'
  },
  {
    id: 'doc-003',
    filename: '产品白皮书.docx',
    type: 'word',
    sizeKb: 980,
    uploadedAt: '2026-05-08 13:00',
    status: '解析失败',
    failReason: '文件加密，未能提取文本'
  }
];
