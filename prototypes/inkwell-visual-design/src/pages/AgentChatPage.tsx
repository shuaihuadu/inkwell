import { useState } from "react";
import {
    Avatar,
    Button,
    Flex,
    Tag,
    Tooltip,
    Typography,
    theme as antdTheme,
} from "antd";
import {
    ArrowLeftOutlined,
    DeleteOutlined,
    MenuFoldOutlined,
    MenuUnfoldOutlined,
    PaperClipOutlined,
    PlusOutlined,
    RobotOutlined,
} from "@ant-design/icons";
import {
    Bubble,
    Conversations,
    Prompts,
    Sender,
    type ConversationItemType,
} from "@ant-design/x";
import { useMockChat, formatNow, type ChatMessage } from "../chat/useMockChat";
import { toBubbleItems, useChatBubbleRoles } from "../chat/chatBubbleRoles";
import { useAttachments } from "../chat/useAttachments";
import { ChatAttachmentsHeader } from "../chat/ChatAttachmentsHeader";
import {
    isHarnessTrigger,
    isAgentLoopTrigger,
    runHarnessDemo,
    runAgentLoopDemo,
} from "../chat/harnessDemo";
import {
    assistantTextEvents,
    replaySeed,
    type SeedStep,
} from "../chat/agui/replay";
import type { AGUIEvent } from "../chat/agui/types";

// ─── UI-005 · Agent 对话页（ui-spec.md §5，REQ-010 / REQ-016 / 场景 S3） ──────────
// 视觉形态照抄 Ant Design X "ultramodern" playground 的结构：左侧固定 Conversations
// 会话列表（可删除、分组）；右侧无消息时是居中的"新对话"起始页（大号 Agent 名 + 居中
// 输入框），有消息后消息流限宽居中显示。核心的"发消息 →
// mock 回复"状态机与 UI-004 内嵌的 CopilotPanel（copilot 形态）共用
// ../chat/useMockChat；Bubble.List 的 role 配置与 items 转换共用 ../chat/chatBubbleRoles。
// 入口：requirements.md §13 第 30 条——Agent 空间列表项若已发布，点击直达本页（使用当前
// 已发布版本）；上传附件用 Sender.Header + Attachments 弹出面板（与 UI-004 的
// CopilotPanel 同一套交互，为多模态输入预留统一入口），语音按钮保留但不接入真实解析。

const MOCK_SESSIONS: ConversationItemType[] = [
    { key: "s1", label: "自我介绍与能力咨询", group: "今天" },
    { key: "s4", label: "调研一下行业报告模板", group: "今天" },
    { key: "s5", label: "优化一段产品介绍文案", group: "今天" },
    { key: "s2", label: "之前讨论的问题进展如何", group: "昨天" },
    { key: "s3", label: "关于配置的一个问题", group: "3 天前" },
];

// 新会话起始页的一键快捷问题——不是随便挑的示例文案，三条分别命中 harnessDemo.ts 的
// 触发词集合（研究/调研 → Harness plan→execute 演示；优化 → Agent Loop 迭代优化演示），
// 点一下就能直接看到带工具调用/reasoning/ThoughtChain 的完整效果，不需要先手打关键词。
const QUICK_PROMPTS = [
    { key: "qp1", description: "整理一份竞品研究框架" },
    { key: "qp2", description: "为调研报告设计目录" },
    { key: "qp3", description: "帮我优化一段产品介绍文案" },
];

function mockConversation(sessionId: string, agentName: string): ChatMessage[] {
    // 每个种子会话都描述成"用户说了什么 + 助手侧发生过哪些 AG-UI 事件"，同步重放
    // （`replaySeed`）成最终定稿的消息列表——跟 Harness/Agent Loop 实时演示走的是同一套
    // `agui/reducer.ts` 翻译逻辑，不是另外手写一份长得像 `ChatMessage` 的静态数据；
    // 这样以后接手的人不会把 `harnessSteps`/`loopSteps`/`todos` 这些字段误当成随便定的
    // 展示用形状——它们其实都是 AG-UI 协议事件（`ACTIVITY_SNAPSHOT` 等）翻译出来的结果。
    switch (sessionId) {
        case "s5": {
            // 展示 Agent Loop（`LoopAgent` + `LoopEvaluator`）驱动的迭代优化循环：第 1 轮初稿
            // → 评估器反馈需要继续 → 第 2 轮改进 → 评估器反馈已达标，与 Harness 的 plan→execute
            // 是两种不同的自主循环。种子历史不需要动画时序，用一个 `ACTIVITY_SNAPSHOT` 直接
            // 给出已完成的最终步骤链即可（对应真实协议里"同步一份完整快照"的用法）。
            const loopId = "s5-loop";
            const finalReply =
                "优化后的文案：相比同价位竞品，我们在核心功能响应速度上快 30%，且提供 7×24 小时人工支持；已有 500+ 团队在用，续费率超过 90%。";
            const steps: SeedStep[] = [
                {
                    kind: "user",
                    content: "帮我优化一段产品介绍文案",
                    time: "今天 11:05",
                },
                {
                    kind: "events",
                    events: [
                        {
                            type: "ACTIVITY_SNAPSHOT",
                            messageId: loopId,
                            activityType: "loop",
                            content: {
                                steps: [
                                    {
                                        key: "round-1",
                                        title: "第 1 轮：生成初稿",
                                        description:
                                            '基于"帮我优化一段产品介绍文案"生成初稿',
                                        detail: "我们的产品性价比高、功能齐全，用户反馈普遍不错。",
                                        status: "success",
                                    },
                                    {
                                        key: "eval-1",
                                        title: "评估反馈：需要继续优化",
                                        description:
                                            "内容偏空泛，缺少具体数据和差异化卖点",
                                        status: "error",
                                    },
                                    {
                                        key: "round-2",
                                        title: "第 2 轮：根据反馈修改",
                                        detail: "相比同价位竞品，我们在核心功能响应速度上快 30%，且提供 7×24 小时人工支持；已有 500+ 团队在用，续费率超过 90%。",
                                        status: "success",
                                    },
                                    {
                                        key: "eval-2",
                                        title: "评估反馈：已达标，结束循环",
                                        description:
                                            "已补充具体数据与差异化卖点，满足要求",
                                        status: "success",
                                    },
                                ],
                            },
                        },
                        ...assistantTextEvents("s5-ai", finalReply),
                    ] as AGUIEvent[],
                },
            ];
            return replaySeed(steps).map((m, i) => ({
                ...m,
                id: `s5-${i + 1}`,
                time: "今天 11:05",
            }));
        }
        case "s4": {
            // 展示 Harness 多轮 plan→execute 自主循环会用到的气泡种类——思考过程/工具调用/
            // 任务清单/用量统计——方便在不输入触发词的情况下也能直接浏览到完整演示。
            const harnessId = "s4-harness";
            const todosId = "s4-todos";
            const reasoningId = "s4-reasoning";
            const finalReply =
                "常见行业报告模板大致分三类结构：市场环境分析、竞品对比与数据支撑、结论与行动建议。结合内部已有模板，建议你优先复用团队已验证过的结构，再根据本次需求补充特定章节。";
            const steps: SeedStep[] = [
                {
                    kind: "user",
                    content: "帮我调研一下同行常用的行业报告模板都有哪些结构",
                    time: "昨天 14:20",
                },
                {
                    kind: "events",
                    events: [
                        { type: "REASONING_START", messageId: reasoningId },
                        {
                            type: "REASONING_MESSAGE_START",
                            messageId: reasoningId,
                            role: "reasoning",
                        },
                        {
                            type: "REASONING_MESSAGE_CONTENT",
                            messageId: reasoningId,
                            delta: "用户想了解行业报告模板的常见结构，需要先检索多份外部案例，再结合内部知识库里已有的报告模板交叉比对。",
                        },
                        {
                            type: "REASONING_MESSAGE_END",
                            messageId: reasoningId,
                        },
                        { type: "REASONING_END", messageId: reasoningId },
                        {
                            type: "ACTIVITY_SNAPSHOT",
                            messageId: harnessId,
                            activityType: "harness",
                            content: {
                                steps: [
                                    {
                                        key: "plan",
                                        title: "制定调研计划",
                                        status: "success",
                                    },
                                    {
                                        key: "tool-1",
                                        title: "调用工具：网页搜索",
                                        description: "查询关键词与来源筛选",
                                        detail: "检索到多篇行业报告模板范例，涵盖市场/产品/运营类结构。",
                                        status: "success",
                                    },
                                    {
                                        key: "tool-2",
                                        title: "调用工具：知识库检索",
                                        description: "匹配内部历史资料",
                                        detail: "在知识库中找到两份团队以往用过的报告模板，作为内部基准参考。",
                                        status: "success",
                                    },
                                    {
                                        key: "wrap-up",
                                        title: "整理结果",
                                        status: "success",
                                    },
                                ],
                            },
                        },
                        {
                            type: "ACTIVITY_SNAPSHOT",
                            messageId: todosId,
                            activityType: "todos",
                            content: {
                                todos: [
                                    {
                                        key: "t1",
                                        label: "汇总外部案例的共性结构",
                                        status: "done",
                                    },
                                    {
                                        key: "t2",
                                        label: "对比内部已有模板的异同",
                                        status: "done",
                                    },
                                    {
                                        key: "t3",
                                        label: "输出推荐结构清单",
                                        status: "done",
                                    },
                                ],
                            },
                        },
                        ...assistantTextEvents("s4-ai", finalReply),
                        {
                            type: "CUSTOM",
                            name: "usage",
                            value: "用量 — 输入 980 · 输出 260 · 总计 1.2k tokens",
                        },
                    ] as AGUIEvent[],
                },
            ];
            return replaySeed(steps).map((m, i) => ({
                ...m,
                id: `s4-${i + 1}`,
                time: "昨天 14:20",
            }));
        }
        case "s2": {
            const finalReply =
                "已加载上一次会话的上下文摘要：我们讨论的是提高整体处理效率的方案，目前已完成两项，还差最后一步验证。你想继续从哪里开始？";
            const steps: SeedStep[] = [
                {
                    kind: "user",
                    content: "我们上次聊到的问题，你还记得吗？",
                    time: "09:14",
                },
                {
                    kind: "events",
                    events: assistantTextEvents("s2-ai", finalReply),
                },
            ];
            return replaySeed(steps).map((m, i) => ({
                ...m,
                id: `s2-${i + 1}`,
                time: "09:14",
            }));
        }
        case "s3": {
            const finalReply =
                "那个参数用于控制模型输出的随机性：数值越低回答越稳定保守，越高越有创造性。默认值适合大多数场景，你可以在配置页『模型与参数』区段调整。";
            const steps: SeedStep[] = [
                {
                    kind: "user",
                    content: "刚才配置里那个参数具体是干什么用的？",
                    time: "07-12 16:20",
                },
                {
                    kind: "events",
                    events: assistantTextEvents("s3-ai", finalReply),
                },
            ];
            return replaySeed(steps).map((m, i) => ({
                ...m,
                id: `s3-${i + 1}`,
                time: "07-12 16:20",
            }));
        }
        default: {
            const finalReply = `你好，我是 ${agentName}。我可以结合知识库与已挂载的工具，帮你处理相关问题；直接说出你的需求，我会尽量给出结构化的回答。`;
            const steps: SeedStep[] = [
                {
                    kind: "user",
                    content: "你好，可以先自我介绍一下你能帮我做什么吗？",
                    time: "10:02",
                },
                { kind: "events", events: assistantTextEvents("s1-ai", finalReply) },
            ];
            return replaySeed(steps).map((m, i) => ({
                ...m,
                id: `s1-${i + 1}`,
                time: "10:02",
            }));
        }
    }
}

export default function AgentChatPage({
    agentName,
    onBack,
}: {
    agentName: string;
    onBack: () => void;
}) {
    const { token } = antdTheme.useToken();
    const [historyCollapsed, setHistoryCollapsed] = useState(false);
    const [sessions, setSessions] = useState(MOCK_SESSIONS);
    const [activeSession, setActiveSession] = useState("s1");
    const { attachmentsOpen, setAttachmentsOpen, files, setFiles } =
        useAttachments();
    const {
        messages,
        setMessages,
        resetMessages,
        replying,
        setReplying,
        input,
        setInput,
        submit,
        retryLast,
        startNewSession,
    } = useMockChat(mockConversation("s1", agentName));

    const roles = useChatBubbleRoles(retryLast);

    const handleSwitchSession = (key: string) => {
        setActiveSession(key);
        resetMessages(mockConversation(key, agentName));
    };

    const handleNewSession = () => {
        if (!startNewSession([])) return;
        const key = `s-${Date.now()}`;
        setSessions((prev) => [
            { key, label: "新建会话", group: "今天" },
            ...prev,
        ]);
        setActiveSession(key);
    };

    const handleDeleteSession = (key: string) => {
        const rest = sessions.filter((item) => item.key !== key);
        setSessions(rest);
        if (key === activeSession) {
            const next = rest[0]?.key ?? "";
            setActiveSession(next);
            resetMessages(next ? mockConversation(next, agentName) : []);
        }
    };

    const isEmpty = messages.length === 0 && !replying;

    /** 输入包含"研究/调研/分析一下/深度"等关键词时，触发 Harness 风格的 plan→execute
     * 自主循环演示（计划 → 工具调用 → 任务清单逐项完成 → 流式回复），而不是普通的单条 mock 回复。 */
    const handleUserSubmit = (value: string) => {
        const trimmed = value.trim();
        if (!trimmed || replying) return;
        if (isHarnessTrigger(trimmed) || isAgentLoopTrigger(trimmed)) {
            const userMessage: ChatMessage = {
                id: `u-${Date.now()}`,
                role: "user",
                content: trimmed,
                time: formatNow(),
            };
            setMessages((prev) => [...prev, userMessage]);
            setInput("");
            if (isAgentLoopTrigger(trimmed)) {
                runAgentLoopDemo(trimmed, setMessages, setReplying);
            } else {
                runHarnessDemo(trimmed, setMessages, setReplying);
            }
            return;
        }
        submit(trimmed);
    };

    return (
        <div
            style={{
                display: "flex",
                flexDirection: "column",
                height: "100%",
                minHeight: 0,
            }}
        >
            {/* 顶部栏：返回 / Agent 身份 / 编辑入口——嵌入 AppShell 内容区，需要保留导航affordance，
             * 与 ultramodern playground 本身作为独立整页应用（没有外层"返回"概念）略有差异。 */}
            <div
                style={{
                    height: 52,
                    flexShrink: 0,
                    display: "flex",
                    alignItems: "center",
                    gap: 10,
                    padding: "0 16px",
                    borderBottom: `1px solid ${token.colorBorderSecondary}`,
                }}
            >
                <Tooltip title="返回 Agent 空间">
                    <Button
                        type="text"
                        aria-label="返回 Agent 空间"
                        icon={<ArrowLeftOutlined />}
                        onClick={onBack}
                    />
                </Tooltip>
                <Avatar
                    size={28}
                    icon={<RobotOutlined />}
                    style={{ background: token.colorPrimary }}
                />
                <Typography.Text strong style={{ fontSize: 14 }}>
                    {agentName}
                </Typography.Text>
                <Tag style={{ margin: 0 }}>模型：gpt-4o-mini</Tag>
            </div>

            <div style={{ flex: 1, minHeight: 0, display: "flex" }}>
                {/* 历史会话侧栏（ui-spec.md §5.1：与 §0.2 主导航 nav 同屏共存，可折叠，默认展开）。
                 * 折叠按钮统一放在侧栏自己的标题行里（跟 AgentDesignPage「配置区段」侧栏同一个
                 * 位置约定），不再钉在侧栏底部——那样会跟 AppShell 全局导航（已改成完全由页面
                 * 上下文自动收起/展开，不再提供手动折叠按钮）在屏幕底部挤在一起，两个长得一样
                 * 的折叠图标并排出现，容易被当成重复功能。 */}
                <div
                    style={{
                        width: historyCollapsed ? 44 : 240,
                        flexShrink: 0,
                        background: token.colorBgLayout,
                        borderRight: `1px solid ${token.colorBorderSecondary}`,
                        display: "flex",
                        flexDirection: "column",
                        transition: "width 0.15s",
                        overflow: "hidden",
                    }}
                >
                    <div
                        style={{
                            minHeight: 44,
                            padding: historyCollapsed
                                ? "8px"
                                : "8px 12px 8px 16px",
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "space-between",
                            borderBottom: `1px solid ${token.colorBorderSecondary}`,
                        }}
                    >
                        {!historyCollapsed && (
                            <Typography.Text
                                type="secondary"
                                style={{ fontSize: 11, fontWeight: 600 }}
                            >
                                会话
                            </Typography.Text>
                        )}
                        <Button
                            type="text"
                            size="small"
                            icon={
                                historyCollapsed ? (
                                    <MenuUnfoldOutlined />
                                ) : (
                                    <MenuFoldOutlined />
                                )
                            }
                            onClick={() => setHistoryCollapsed((v) => !v)}
                        />
                    </div>
                    <div
                        style={{
                            flex: 1,
                            overflow: "auto",
                            padding: historyCollapsed ? 6 : "8px 6px",
                        }}
                    >
                        {historyCollapsed ? (
                            <Button
                                block
                                type="text"
                                icon={<PlusOutlined />}
                                onClick={handleNewSession}
                            />
                        ) : (
                            <Conversations
                                items={sessions}
                                activeKey={activeSession}
                                groupable
                                onActiveChange={handleSwitchSession}
                                creation={{
                                    label: "新建会话",
                                    icon: <PlusOutlined />,
                                    onClick: handleNewSession,
                                }}
                                menu={(conversation) => ({
                                    items: [
                                        {
                                            key: "delete",
                                            label: "删除",
                                            icon: <DeleteOutlined />,
                                            danger: true,
                                            onClick: () =>
                                                handleDeleteSession(
                                                    conversation.key,
                                                ),
                                        },
                                    ],
                                })}
                            />
                        )}
                    </div>
                </div>

                {/* 主聊天区：Ant Design X 的 Sender 默认是"透明底 + 描边 + 阴影"的悬浮卡片，
                 * 设计上假定衬在 colorBgContainer（纯色）背板上；照官方 ultramodern demo 的
                 * `.chat { background: colorBgContainer }` 补上，否则 Sender 的描边卡片衬在
                 * 灰色 colorBgLayout 背景上会显得输入框与下面的按钮行像两个不相干的独立盒子。 */}
                <div
                    style={{
                        flex: 1,
                        minWidth: 0,
                        display: "flex",
                        flexDirection: "column",
                        background: token.colorBgContainer,
                    }}
                >
                    {!isEmpty && (
                        // Bubble.List 内部自带的 `.scroll-box` 才是真正应该滚动、且带
                        // "column-reverse 贴底"自动跟随技巧的容器（`autoScroll` 默认开启）；
                        // 这里的外层 div 不能再叠加一层 overflow:auto，否则会出现两层独立的
                        // 滚动容器——外层这层是普通从上到下滚动，不会跟着新内容自动贴底，
                        // 内层 Bubble.List 自己又因为拿不到确定高度而永远不触发溢出，实际
                        // 表现就是"流式输出时页面不会自动跟着往下滚"。外层用 flex:1 +
                        // minHeight:0 提供一个确定高度即可，但光靠外层确定高度还不够——
                        // Bubble.List 自己的 CSS 只写了 `max-height:100%`，没写 `height`，
                        // 而 CSS percentage-height 解析规则里，子元素的百分比高度只认祖先的
                        // "显式指定的 height"，不认祖先被 max-height 钳制之后的实际渲染高度；
                        // 所以必须显式传 `style={{height:"100%"}}` 把它也变成"显式指定"，
                        // 否则内部 `.scroll-box` 的 `max-height:100%` 解析不到基准，会撑到
                        // 内容真实高度，被外层 overflow:hidden 直接裁掉、完全看不见也无法滚动
                        // （这个坑排查时用 getBoundingClientRect 逐层量过才确认）。padding 也
                        // 要挪到 Bubble.List 自己的 styles.scroll 上（滚动内容需要跟着一起
                        // 滚，不能钉在外层）。
                        <div
                            style={{
                                flex: 1,
                                minHeight: 0,
                                overflow: "hidden",
                            }}
                        >
                            <Bubble.List
                                items={toBubbleItems(messages, replying)}
                                role={roles}
                                style={{ height: "100%" }}
                                styles={{ scroll: { padding: "20px 24px 0" } }}
                            />
                        </div>
                    )}

                    <div
                        style={{
                            display: "flex",
                            flexDirection: "column",
                            alignItems: "center",
                            justifyContent: isEmpty ? "center" : "flex-end",
                            flex: isEmpty ? 1 : "none",
                            padding: isEmpty
                                ? `0 ${token.paddingXS}px`
                                : `16px ${token.paddingXS}px 20px`,
                        }}
                    >
                        {isEmpty && (
                            <>
                                <Typography.Title
                                    level={3}
                                    style={{ marginBottom: 24 }}
                                >
                                    {agentName}
                                </Typography.Title>
                                <Prompts
                                    items={QUICK_PROMPTS}
                                    wrap
                                    onItemClick={(info) =>
                                        handleUserSubmit(
                                            info.data.description as string,
                                        )
                                    }
                                    style={{
                                        marginBottom: 20,
                                        maxWidth: 640,
                                        justifyContent: "center",
                                    }}
                                />
                            </>
                        )}
                        <div style={{ width: "100%" }}>
                            <Sender
                                suffix={false}
                                value={input}
                                onChange={setInput}
                                onSubmit={handleUserSubmit}
                                loading={replying}
                                allowSpeech
                                placeholder="输入消息，Enter 发送，Shift+Enter 换行"
                                autoSize={{
                                    minRows: isEmpty ? 2 : 1,
                                    maxRows: 6,
                                }}
                                header={
                                    <ChatAttachmentsHeader
                                        open={attachmentsOpen}
                                        onOpenChange={setAttachmentsOpen}
                                        files={files}
                                        onFilesChange={setFiles}
                                    />
                                }
                                footer={(actionNode) => (
                                    <Flex
                                        justify="space-between"
                                        align="center"
                                    >
                                        <Tooltip title="上传图片 / 文档（多模态输入预留入口，详见 ui-spec.md §5.1）">
                                            <Button
                                                type="text"
                                                icon={<PaperClipOutlined />}
                                                onClick={() =>
                                                    setAttachmentsOpen(
                                                        !attachmentsOpen,
                                                    )
                                                }
                                            />
                                        </Tooltip>
                                        <Flex align="center">{actionNode}</Flex>
                                    </Flex>
                                )}
                            />
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
