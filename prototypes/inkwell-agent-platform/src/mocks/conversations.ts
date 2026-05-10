import { ConversationMessage, ConversationMeta } from '../types';

export const MOCK_CONVERSATIONS: ConversationMeta[] = [
  {
    id: 'conv-001',
    agentId: 'agent-001',
    agentName: '客服质检助手',
    title: '帮我看看这通通话有没有违规',
    startedAt: '2026-05-08 14:32'
  },
  {
    id: 'conv-002',
    agentId: 'agent-001',
    agentName: '客服质检助手',
    title: '昨天的对话样本能再过一遍吗',
    startedAt: '2026-05-07 09:15'
  },
  {
    id: 'conv-003',
    agentId: 'agent-001',
    agentName: '客服质检助手',
    title: '为什么这条提醒被标记为高风险',
    startedAt: '2026-05-05 17:48'
  }
];

export const MOCK_MESSAGES: ConversationMessage[] = [
  {
    id: 'msg-001',
    role: 'user',
    content: '帮我看看这通通话有没有违规：「客户问利率，客服说『按你说的算』。」',
    status: '已完成',
    timestamp: '2026-05-08 14:32:01'
  },
  {
    id: 'msg-002',
    role: 'agent',
    content:
      '检测到风险点：销售口径未给出明确利率引用条款，疑似口头承诺。建议复核话术。',
    status: '已完成',
    timestamp: '2026-05-08 14:32:04'
  },
  {
    id: 'msg-003',
    role: 'tool',
    content:
      '调用 search_compliance_rule，参数 {keyword: "利率"} → 命中 3 条合规细则',
    status: '已完成',
    timestamp: '2026-05-08 14:32:05'
  },
  {
    id: 'msg-004',
    role: 'system',
    content: 'Skill 「客服质检 SOP v2.1」加载失败：references/ 内文件缺失（EX-008）',
    status: '已完成',
    timestamp: '2026-05-08 14:32:06'
  },
  {
    id: 'msg-005',
    role: 'agent',
    content: '已结合 3 条合规细则给出整改建议（详见详情区）。',
    status: '流式中',
    timestamp: '2026-05-08 14:32:08'
  }
];
