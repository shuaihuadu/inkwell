import {
    AppstoreAddOutlined,
    ArrowLeftOutlined,
    CloseOutlined,
    CommentOutlined,
    DeleteOutlined,
    FileSearchOutlined,
    MenuFoldOutlined,
    MenuUnfoldOutlined,
    PaperClipOutlined,
    PlusOutlined,
    RobotOutlined,
} from "@ant-design/icons";
import { useQuery } from "@tanstack/react-query";
import {
    Bubble,
    Conversations,
    Prompts,
    Sender,
    Welcome,
    type ConversationItemType,
} from "@ant-design/x";
import { XMarkdown } from "@ant-design/x-markdown";
import {
    Avatar,
    Button,
    Empty,
    Flex,
    message,
    Space,
    Tag,
    Tooltip,
    Typography,
} from "antd";
import { useEffect, useRef, useState } from "react";
import { desktopApi } from "../../shared/network/desktop-api";
import type {
    AgentListItem,
    ChatMessage,
} from "../../shared/network/contracts";

interface ChatPanelProps {
    agent: AgentListItem | null;
    variant?: "full" | "trial";
    runMode?: "published" | "draft";
    onClose?: () => void;
}

const TrialPrompts = [
    "整理一份竞品研究框架",
    "分析这份资料的关键结论",
    "为调研报告设计目录",
];

const ChatPrompts = [
    { key: "research", description: "整理一份竞品研究框架" },
    { key: "outline", description: "为调研报告设计目录" },
    { key: "rewrite", description: "帮我优化一段产品介绍文案" },
];

type LocalConversation = ConversationItemType;

export function ChatPanel({
    agent,
    variant = "full",
    runMode = "published",
    onClose,
}: ChatPanelProps) {
    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [draft, setDraft] = useState("");
    const [activeRequestId, setActiveRequestId] = useState<string | null>(null);
    const [historyCollapsed, setHistoryCollapsed] = useState(false);
    const [conversations, setConversations] = useState<LocalConversation[]>([]);
    const [activeConversationKey, setActiveConversationKey] = useState<
        string | null
    >(null);
    const conversationMessages = useRef(new Map<string, ChatMessage[]>());
    const [messageApi, contextHolder] = message.useMessage();
    const agentDetailsQuery = useQuery({
        queryKey: ["agents", agent?.id, "chat-details"],
        queryFn: () => desktopApi.getAgent(agent!.id),
        enabled: variant === "full" && Boolean(agent),
    });

    useEffect(
        () =>
            desktopApi.onChatDelta((requestId, content) => {
                if (requestId !== activeRequestId) return;
                setMessages((current) => {
                    const next = current.map((item, index) =>
                        index === current.length - 1
                            ? { ...item, content: item.content + content }
                            : item,
                    );
                    if (activeConversationKey)
                        conversationMessages.current.set(
                            activeConversationKey,
                            next,
                        );
                    return next;
                });
            }),
        [activeConversationKey, activeRequestId],
    );

    const send = async (value = draft): Promise<void> => {
        const content = value.trim();
        if (!agent || !content || activeRequestId) return;
        const requestId = crypto.randomUUID();
        const history: ChatMessage[] = [...messages, { role: "user", content }];
        const pendingMessages: ChatMessage[] = [
            ...history,
            { role: "assistant", content: "" },
        ];
        if (variant === "full" && !activeConversationKey) {
            const conversationKey = crypto.randomUUID();
            setConversations((current) => [
                {
                    key: conversationKey,
                    label:
                        content.length > 24
                            ? `${content.slice(0, 24)}…`
                            : content,
                    group: "今天",
                },
                ...current,
            ]);
            conversationMessages.current.set(conversationKey, pendingMessages);
            setActiveConversationKey(conversationKey);
        } else if (activeConversationKey) {
            conversationMessages.current.set(
                activeConversationKey,
                pendingMessages,
            );
        }
        setMessages(pendingMessages);
        setDraft("");
        setActiveRequestId(requestId);
        try {
            await desktopApi.chat({
                requestId,
                agentId: agent.id,
                runMode,
                messages: history,
            });
        } catch (reason) {
            messageApi.error(
                reason instanceof Error ? reason.message : "Agent 调用失败。",
            );
            setMessages((current) => {
                const next = current.map((item, index) =>
                    index === current.length - 1 && !item.content
                        ? {
                              ...item,
                              content: "暂时无法生成回复，请检查模型服务配置。",
                          }
                        : item,
                );
                if (activeConversationKey)
                    conversationMessages.current.set(
                        activeConversationKey,
                        next,
                    );
                return next;
            });
        } finally {
            setActiveRequestId(null);
        }
    };

    const startNewConversation = (): void => {
        if (activeRequestId) return;
        setActiveConversationKey(null);
        setMessages([]);
        setDraft("");
    };

    const switchConversation = (key: string): void => {
        if (activeRequestId) return;
        const conversation = conversations.find((item) => item.key === key);
        if (!conversation) return;
        setActiveConversationKey(key);
        setMessages(conversationMessages.current.get(key) ?? []);
        setDraft("");
    };

    const deleteConversation = (key: string): void => {
        if (activeRequestId) return;
        const remaining = conversations.filter((item) => item.key !== key);
        conversationMessages.current.delete(key);
        setConversations(remaining);
        if (activeConversationKey !== key) return;

        const next = remaining[0];
        setActiveConversationKey(next ? String(next.key) : null);
        setMessages(
            next
                ? (conversationMessages.current.get(String(next.key)) ?? [])
                : [],
        );
    };

    const renderMessages = () => (
        <Bubble.List
            className="chat-bubble-list"
            items={messages.map((item, index) => ({
                key: `${item.role}-${index}`,
                role: item.role,
                content: item.content,
                loading:
                    item.role === "assistant" &&
                    !item.content &&
                    Boolean(activeRequestId),
                contentRender:
                    item.role === "assistant"
                        ? (content) => (
                              <XMarkdown
                                  content={String(content)}
                                  streaming={{
                                      hasNextChunk:
                                          Boolean(activeRequestId) &&
                                          index === messages.length - 1,
                                  }}
                              />
                          )
                        : undefined,
            }))}
            role={{
                user: {
                    placement: "end",
                    variant: "filled",
                },
                assistant: {
                    placement: "start",
                    variant: "outlined",
                },
            }}
        />
    );

    if (!agent)
        return (
            <section className="chat-panel chat-empty">
                <Empty description="选择一个 Agent 开始对话" />
            </section>
        );

    if (variant === "full") {
        return (
            <section className="chat-panel-full">
                {contextHolder}
                <header className="chat-page-header">
                    <Tooltip title="返回 Agent 空间">
                        <Button
                            type="text"
                            aria-label="返回 Agent 空间"
                            icon={<ArrowLeftOutlined />}
                            onClick={onClose}
                        />
                    </Tooltip>
                    <Avatar
                        className="agent-avatar"
                        src={agent.avatarUri ?? undefined}
                    >
                        {agent.name.slice(0, 1)}
                    </Avatar>
                    <Typography.Text strong>{agent.name}</Typography.Text>
                    <Tag>
                        模型：
                        {agentDetailsQuery.data?.buildOptions.modelOptions
                            .modelId ?? "未配置"}
                    </Tag>
                </header>

                <div className="chat-page-body">
                    <aside
                        className={`chat-history ${historyCollapsed ? "collapsed" : ""}`}
                    >
                        <div className="chat-history-header">
                            {!historyCollapsed && (
                                <Typography.Text type="secondary">
                                    会话
                                </Typography.Text>
                            )}
                            <Tooltip
                                title={
                                    historyCollapsed ? "展开会话" : "收起会话"
                                }
                            >
                                <Button
                                    type="text"
                                    size="small"
                                    aria-label={
                                        historyCollapsed
                                            ? "展开会话"
                                            : "收起会话"
                                    }
                                    icon={
                                        historyCollapsed ? (
                                            <MenuUnfoldOutlined />
                                        ) : (
                                            <MenuFoldOutlined />
                                        )
                                    }
                                    onClick={() =>
                                        setHistoryCollapsed((value) => !value)
                                    }
                                />
                            </Tooltip>
                        </div>
                        <div className="chat-history-list">
                            {historyCollapsed ? (
                                <Tooltip title="新建会话" placement="right">
                                    <Button
                                        block
                                        type="text"
                                        aria-label="新建会话"
                                        icon={<PlusOutlined />}
                                        onClick={startNewConversation}
                                    />
                                </Tooltip>
                            ) : (
                                <Conversations
                                    items={conversations}
                                    activeKey={
                                        activeConversationKey ?? undefined
                                    }
                                    groupable
                                    creation={{
                                        label: "新建会话",
                                        icon: <PlusOutlined />,
                                        onClick: startNewConversation,
                                    }}
                                    onActiveChange={(key) =>
                                        switchConversation(String(key))
                                    }
                                    menu={(conversation) => ({
                                        items: [
                                            {
                                                key: "delete",
                                                label: "删除",
                                                danger: true,
                                                icon: <DeleteOutlined />,
                                                onClick: () =>
                                                    deleteConversation(
                                                        String(
                                                            conversation.key,
                                                        ),
                                                    ),
                                            },
                                        ],
                                    })}
                                />
                            )}
                        </div>
                    </aside>

                    <div className="chat-main">
                        {messages.length > 0 ? (
                            <div className="chat-full-messages">
                                {renderMessages()}
                            </div>
                        ) : (
                            <div className="chat-full-empty">
                                <Typography.Title level={3}>
                                    {agent.name}
                                </Typography.Title>
                                <Prompts
                                    items={ChatPrompts}
                                    wrap
                                    onItemClick={(info) =>
                                        void send(String(info.data.description))
                                    }
                                />
                            </div>
                        )}
                        <div className="chat-full-composer">
                            <Sender
                                className="chat-sender"
                                suffix={false}
                                autoSize={{
                                    minRows: messages.length === 0 ? 2 : 1,
                                    maxRows: 6,
                                }}
                                placeholder="输入消息，Enter 发送，Shift + Enter 换行"
                                value={draft}
                                onChange={setDraft}
                                onSubmit={(value) => void send(value)}
                                loading={Boolean(activeRequestId)}
                                allowSpeech
                                footer={(actionNode) => (
                                    <Flex
                                        justify="space-between"
                                        align="center"
                                    >
                                        <Tooltip title="添加附件">
                                            <Button
                                                type="text"
                                                aria-label="添加附件"
                                                icon={<PaperClipOutlined />}
                                            />
                                        </Tooltip>
                                        {actionNode}
                                    </Flex>
                                )}
                            />
                        </div>
                    </div>
                </div>
            </section>
        );
    }

    return (
        <section className="chat-panel chat-panel-trial">
            {contextHolder}
            <header className="chat-header">
                <Avatar className="agent-avatar">
                    {agent.name.slice(0, 1)}
                </Avatar>
                <div className="chat-agent-identity">
                    <Typography.Title level={4}>{agent.name}</Typography.Title>
                    <Typography.Text type="secondary">
                        {variant === "trial"
                            ? runMode === "draft"
                                ? "当前草稿"
                                : `已发布 v${agent.latestPublishedVersionNumber}`
                            : `已发布 v${agent.latestPublishedVersionNumber}`}
                    </Typography.Text>
                </div>
                {variant === "trial" && (
                    <div className="chat-trial-actions">
                        <Button
                            type="text"
                            aria-label="新建试运行会话"
                            icon={<PlusOutlined />}
                            onClick={() => setMessages([])}
                        />
                        <Button
                            type="text"
                            aria-label="试运行会话列表"
                            icon={<CommentOutlined />}
                        />
                        <Button
                            type="text"
                            aria-label="关闭试运行"
                            icon={<CloseOutlined />}
                            onClick={onClose}
                        />
                    </div>
                )}
            </header>
            <div className="message-list">
                {messages.length === 0 ? (
                    variant === "trial" ? (
                        <div className="conversation-starter">
                            <Welcome
                                variant="borderless"
                                icon={
                                    <div className="chat-trial-welcome-icon">
                                        {agent.name.slice(0, 1)}
                                    </div>
                                }
                                title="从一个研究问题开始"
                                description="我可以协助检索资料、梳理证据并生成结构化报告。"
                            />
                            <Prompts
                                vertical
                                items={TrialPrompts.map((prompt, index) => ({
                                    key: `trial-${index}`,
                                    description: prompt,
                                }))}
                                onItemClick={(info) =>
                                    void send(String(info.data.description))
                                }
                            />
                        </div>
                    ) : (
                        <div className="conversation-starter">
                            <RobotOutlined />
                            <Typography.Title level={4}>
                                开始新的对话
                            </Typography.Title>
                            <Typography.Text type="secondary">
                                消息将由当前 Agent 的已发布版本处理。
                            </Typography.Text>
                        </div>
                    )
                ) : (
                    renderMessages()
                )}
            </div>
            <div className="composer">
                {variant === "trial" && (
                    <div className="chat-trial-shortcuts">
                        <Button
                            size="small"
                            icon={<FileSearchOutlined />}
                            onClick={() => void send("整理一份竞品研究框架")}
                        >
                            研究框架
                        </Button>
                        <Button
                            size="small"
                            icon={<AppstoreAddOutlined />}
                            onClick={() => void send("为调研报告设计目录")}
                        >
                            报告目录
                        </Button>
                    </div>
                )}
                <Sender
                    className="chat-sender"
                    suffix={false}
                    autoSize={{ minRows: 2, maxRows: 5 }}
                    placeholder="输入消息，Enter 发送，Shift + Enter 换行"
                    value={draft}
                    onChange={setDraft}
                    onSubmit={(value) => void send(value)}
                    loading={Boolean(activeRequestId)}
                    allowSpeech
                    footer={(actionNode) => (
                        <Flex justify="space-between" align="center">
                            <Space size={8}>
                                <Button
                                    type="text"
                                    aria-label="添加附件"
                                    icon={<PaperClipOutlined />}
                                />
                                <Typography.Text type="secondary">
                                    {runMode === "draft"
                                        ? "当前使用已保存草稿"
                                        : `当前使用已发布版本 v${agent.latestPublishedVersionNumber}`}
                                </Typography.Text>
                            </Space>
                            {actionNode}
                        </Flex>
                    )}
                />
            </div>
        </section>
    );
}
