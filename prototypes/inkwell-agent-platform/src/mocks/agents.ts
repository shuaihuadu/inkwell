import { AgentSummary, AgentVersion } from '../types';

export const MOCK_AGENTS: AgentSummary[] = [
  {
    id: 'agent-001',
    name: '客服质检助手',
    description:
      '基于客服对话语料的质量检测 Agent；能识别违规话术并给出整改建议。',
    ownerName: 'owner-bob',
    ownerId: 'owner-bob',
    lastUsedAt: '2026-05-08 14:32',
    shared: true,
    visibility: ['mine', 'used']
  },
  {
    id: 'agent-002',
    name: '合同条款摘要',
    description: '从合同 PDF 中提取关键条款（金额、期限、违约条款）并生成摘要。',
    ownerName: 'owner-bob',
    ownerId: 'owner-bob',
    lastUsedAt: '2026-05-07 09:15',
    shared: false,
    visibility: ['mine']
  },
  {
    id: 'agent-003',
    name: '团队周报生成器',
    description: '基于成员日报自动汇总周报；可调用日历工具核对会议纪要。',
    ownerName: 'alice',
    ownerId: 'alice',
    lastUsedAt: '2026-05-06 18:00',
    shared: true,
    visibility: ['shared', 'used']
  },
  {
    id: 'agent-004',
    name: '代码审查辅助',
    description: '挂载内部代码规范 Skill；对 PR diff 给出风格与安全审查意见。',
    ownerName: 'dave',
    ownerId: 'dave',
    lastUsedAt: '2026-05-05 11:23',
    shared: true,
    visibility: ['shared']
  },
  {
    id: 'agent-005',
    name: '客户调研对话伙伴',
    description: '模拟客户角色进行需求访谈对话；生成结构化访谈纪要。',
    ownerName: 'eve',
    ownerId: 'eve',
    lastUsedAt: '2026-05-04 16:50',
    shared: true,
    visibility: ['shared', 'used']
  }
];

export const MOCK_VERSIONS: AgentVersion[] = [
  { version: 'v3', savedAt: '2026-05-08 14:32', savedBy: 'owner-bob', note: '调整 Instructions 措辞' },
  { version: 'v2', savedAt: '2026-05-06 10:11', savedBy: 'owner-bob', note: '挂载 SOP Skill' },
  { version: 'v1', savedAt: '2026-05-04 09:00', savedBy: 'owner-bob', note: '初始版本' }
];
