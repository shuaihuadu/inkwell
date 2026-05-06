// ============================================================
// Inkwell · 自定义 Agent 功能 · H1 原型 · Mock 数据
// ============================================================
//
// 与 ui-spec.md / acceptance-criteria.md 的样例数据保持一致：
// - 3 个示例 Agent（客服小助手 / 邮件起草 / 翻译助手）
// - 1 个软删除中的 Agent（剩余 5 天）
// - 1 个示例 Skill（polite-cn）
// - 内置 Tool 启用情况（联网搜索 / 当前日期）

(function () {
    const now = new Date('2026-05-06T14:30:00').getTime();
    const days = (d) => 1000 * 60 * 60 * 24 * d;

    window.MOCK = {
        user: {
            name: '张三',
            lang: 'zh-CN',
            // 已确认过 E6 须知的工具集合（每个用户记一次）
            acknowledgedTools: [],
        },

        agents: [
            {
                id: 'agent-001',
                name: '客服小助手',
                avatar: '🎧',
                description: '面向电商业务的售前 / 售后客服 Agent，能识别订单号并按 SOP 回复。',
                prompt:
                    `你是 "客服小助手"，一名礼貌、克制、用中文回复的电商客服。

要求：
1. 始终用中文，并保持礼貌；
2. 当用户提供订单号（形如 ORD-XXXXXXX）时，先复述确认；
3. 遇到不确定的问题，提示用户拨打人工热线 400-XXX-XXXX。`,
                skills: ['polite-cn'],
                tools: ['t-1-web-search', 't-3-current-date'],
                updatedAt: now - days(0.2),
                deletedAt: null,
            },
            {
                id: 'agent-002',
                name: '邮件起草',
                avatar: '✉️',
                description: '帮我把要点变成正式邮件，支持中英文双语模板。',
                prompt: '你是邮件起草助手。把用户给的要点整理成结构清晰、正式得体的邮件，默认中文，需要英文时按指令切换。',
                skills: [],
                tools: ['t-3-current-date'],
                updatedAt: now - days(2),
                deletedAt: null,
            },
            {
                id: 'agent-003',
                name: '翻译助手',
                avatar: '🌐',
                description: '中英互译，按场景（合同 / 邮件 / 闲聊）调整语气。',
                prompt: '你是中英翻译助手。先识别原文语种，再按场景调整目标语言的语气。',
                skills: [],
                tools: [],
                updatedAt: now - days(7),
                deletedAt: null,
            },
            {
                id: 'agent-archived-001',
                name: '老版邮件起草',
                avatar: '📮',
                description: '已删除：用户 5 天前主动删除，剩 2 天可恢复。',
                prompt: '',
                skills: [],
                tools: [],
                updatedAt: now - days(10),
                deletedAt: now - days(5), // 软删 5 天 → 剩余 2 天
            },
        ],

        skills: [
            {
                id: 'polite-cn',
                name: 'polite-cn',
                description: 'Always reply in polite Chinese, addressing the user as "您".',
                license: 'MIT',
                compatibility: 'gpt-4.1, claude-3.5-sonnet',
                metadata: [
                    { key: 'language', value: 'zh-CN' },
                    { key: 'version', value: '1.2.0' },
                ],
                body:
                    `# polite-cn

When responding to the user, follow these rules:

- Always reply in Simplified Chinese.
- Address the user as 您 instead of 你.
- Avoid imperative sentences; prefer 请 / 您可以 phrasing.
- End each reply with a polite closing such as 祝您愉快 or 期待为您服务。`,
                usedBy: ['agent-001'],
            },
        ],

        tools: [
            {
                id: 't-1-web-search',
                name: '联网搜索',
                description: '给定查询返回网页摘要 / 链接清单。本工具会把对话内容发送给搜索服务方。',
                externalCall: true,
            },
            {
                id: 't-3-current-date',
                name: '当前日期 / 时间',
                description: '返回服务器当前时间戳（含时区）。本工具不外发数据。',
                externalCall: false,
            },
        ],
    };

    // 简单的"距今多久"格式化
    window.formatRelativeTime = function (ts) {
        const diff = Math.floor((now - ts) / 1000);
        if (diff < 60) return '刚刚';
        if (diff < 3600) return Math.floor(diff / 60) + ' 分钟前';
        if (diff < 86400) return Math.floor(diff / 3600) + ' 小时前';
        const d = Math.floor(diff / 86400);
        if (d < 30) return d + ' 天前';
        const m = Math.floor(d / 30);
        return m + ' 个月前';
    };

    // 软删剩余天数（DB-004：保留 7 天）
    window.daysLeftSoftDelete = function (deletedAt) {
        const left = 7 - Math.floor((now - deletedAt) / (1000 * 60 * 60 * 24));
        return Math.max(0, left);
    };
})();
