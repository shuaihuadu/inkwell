import {
    ArrowDownOutlined,
    ArrowLeftOutlined,
    ClearOutlined,
    CloseOutlined,
    CommentOutlined,
    DeleteOutlined,
    EyeOutlined,
    MenuFoldOutlined,
    MenuUnfoldOutlined,
    PlusOutlined,
    RobotOutlined,
} from "@ant-design/icons";
import { useQuery } from "@tanstack/react-query";
import {
    Conversations,
    Prompts,
    Welcome,
    type ConversationItemType,
} from "@ant-design/x";
import {
    Avatar,
    Button,
    Empty,
    message,
    Modal,
    Tag,
    Tooltip,
    Typography,
} from "antd";
import { useCallback, useEffect, useRef, useState, type UIEvent } from "react";
import type { TFunction } from "i18next";
import { useTranslation } from "react-i18next";
import { AgentDetailsDrawer } from "../../shared/components/agent-details-drawer";
import { desktopApi } from "../../shared/network/desktop-api";
import type {
    ActiveAgentChatRun,
    AgentConversationListItem,
    AgentListItem,
    ChatMessage,
    ChatRunError,
    ChatRunSnapshot,
} from "../../shared/network/contracts";
import { useAuthStore } from "../auth/auth-store";
import { useResolvedAppearance } from "../shell/appearance-store";
import { ChatComposer } from "./chat-composer";
import { ChatMessageList, type ChatMessageListRef } from "./chat-message-list";
import { ChatQuickPromptKeys } from "./chat-quick-prompt-items";
import { ChatQuickPrompts } from "./chat-quick-prompts";
import { applyChatRunSnapshotToMessages } from "./chat-run-messages";

interface ChatPanelProps {
    agent: AgentListItem | null;
    variant?: "full" | "trial";
    runMode?: "published" | "draft";
    onClose?: () => void;
}

type LocalConversation = ConversationItemType & {
    key: string;
    agentVersionId: string;
};

const toConversationItem = (
    conversation: AgentConversationListItem,
    t: TFunction,
): LocalConversation => ({
    key: conversation.id,
    agentVersionId: conversation.agentVersionId,
    label: conversation.title ?? t("chat.panel.newConversation"),
    group: t("chat.panel.historyGroup"),
});

const restoreActiveRunMessages = (
    persistedMessages: ChatMessage[],
    run: ActiveAgentChatRun,
): ChatMessage[] => {
    const matchingUserIndex = persistedMessages.findLastIndex(
        (message) =>
            message.role === "user" &&
            message.content === run.userMessage.content,
    );
    const messagesBeforeAssistant =
        matchingUserIndex >= 0
            ? persistedMessages.slice(0, matchingUserIndex + 1)
            : [...persistedMessages, run.userMessage];
    const persistedActivities =
        matchingUserIndex >= 0
            ? persistedMessages
                  .slice(matchingUserIndex + 1)
                  .flatMap((message) => message.skillActivities ?? [])
            : [];
    const activities = [
        ...persistedActivities,
        ...run.snapshot.skillActivities.filter(
            (activity) =>
                !persistedActivities.some(
                    (persisted) => persisted.callId === activity.callId,
                ),
        ),
    ];

    return [
        ...messagesBeforeAssistant,
        {
            role: "assistant",
            content: run.snapshot.content,
            ...(activities.length > 0 ? { skillActivities: activities } : {}),
            runStatus: run.snapshot.status,
        },
    ];
};

export function ChatPanel({
    agent,
    variant = "full",
    runMode = "published",
    onClose,
}: ChatPanelProps) {
    const { t, i18n } = useTranslation();
    const appearance = useResolvedAppearance();
    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [draft, setDraft] = useState("");
    const [activeRequestId, setActiveRequestId] = useState<string | null>(null);
    const [latestRequestId, setLatestRequestId] = useState<string | null>(null);
    const [chatError, setChatError] = useState<
        (ChatRunError & { input: string }) | null
    >(null);
    const [historyCollapsed, setHistoryCollapsed] = useState(false);
    const [showScrollToLatest, setShowScrollToLatest] = useState(false);
    const messageListRef = useRef<ChatMessageListRef>(null);
    const [conversations, setConversations] = useState<LocalConversation[]>([]);
    const [activeConversationKey, setActiveConversationKey] = useState<
        string | null
    >(null);
    const [detailsOpen, setDetailsOpen] = useState(false);
    const [messageApi, contextHolder] = message.useMessage();
    const authStatus = useAuthStore((state) => state.status);
    const agentDetailsQuery = useQuery({
        queryKey: ["agents", agent?.id, "chat-details"],
        queryFn: () => desktopApi.getAgent(agent!.id),
        enabled: variant === "full" && Boolean(agent),
    });
    const agentVersionsQuery = useQuery({
        queryKey: ["agent-versions", agent?.id],
        queryFn: () => desktopApi.listAgentVersions(agent!.id),
        enabled: variant === "full" && Boolean(agent),
    });
    const toolsQuery = useQuery({
        queryKey: ["tools", "agent-details"],
        queryFn: desktopApi.listTools,
        enabled: variant === "full" && Boolean(agent),
    });
    const activeConversation = activeConversationKey
        ? conversations.find((item) => item.key === activeConversationKey)
        : undefined;
    const activeConversationVersionNumber = activeConversation
        ? agentVersionsQuery.data?.find(
              (version) => version.id === activeConversation.agentVersionId,
          )?.versionNumber
        : undefined;
    const detailVersion = activeConversation
        ? (agentVersionsQuery.data?.find(
              (version) => version.id === activeConversation.agentVersionId,
          ) ?? null)
        : (agentVersionsQuery.data?.find(
              (version) =>
                  version.id ===
                  agentDetailsQuery.data?.currentPublishedVersionId,
          ) ?? null);

    useEffect(() => {
        if (variant !== "full" || !agent) return;

        let disposed = false;
        void Promise.all([
            desktopApi.listAgentConversations(agent.id),
            desktopApi.getActiveAgentChatRun(agent.id),
        ])
            .then(async ([items, activeRun]) => {
                if (disposed) return;
                setConversations(
                    items.map((conversation) =>
                        toConversationItem(conversation, t),
                    ),
                );
                const selectedConversation =
                    (activeRun?.conversationId
                        ? items.find(
                              (item) => item.id === activeRun.conversationId,
                          )
                        : undefined) ?? items[0];
                if (!selectedConversation) {
                    setActiveConversationKey(null);
                    setMessages([]);
                    setActiveRequestId(null);
                    setLatestRequestId(null);
                    return;
                }

                const persistedMessages =
                    await desktopApi.getAgentConversationMessages(
                        agent.id,
                        selectedConversation.id,
                    );
                if (disposed) return;
                setActiveConversationKey(selectedConversation.id);
                if (
                    activeRun?.conversationId === selectedConversation.id &&
                    activeRun.snapshot.status === "running"
                ) {
                    setMessages(
                        restoreActiveRunMessages(persistedMessages, activeRun),
                    );
                    setActiveRequestId(activeRun.snapshot.requestId);
                    setLatestRequestId(activeRun.snapshot.requestId);
                } else {
                    setMessages(persistedMessages);
                    setActiveRequestId(null);
                    setLatestRequestId(null);
                }
            })
            .catch((reason: unknown) => {
                if (!disposed) {
                    void messageApi.error(
                        reason instanceof Error
                            ? reason.message
                            : t("chat.panel.errors.historyLoad"),
                    );
                }
            });

        return () => {
            disposed = true;
        };
    }, [agent, i18n.language, messageApi, t, variant]);

    const applySnapshot = useCallback(
        (snapshot: ChatRunSnapshot, originalInput?: string): void => {
            setMessages((current) =>
                applyChatRunSnapshotToMessages(current, snapshot),
            );
            if (snapshot.status === "failed" && snapshot.error) {
                setChatError((current) => ({
                    ...snapshot.error!,
                    input: originalInput ?? current?.input ?? "",
                }));
            } else if (snapshot.status !== "running") {
                setChatError(null);
            }
            if (snapshot.status !== "running") {
                setActiveRequestId((current) =>
                    current === snapshot.requestId ? null : current,
                );
            }
        },
        [],
    );

    const refreshPersistedConversation = useCallback(
        async (conversationKey: string): Promise<void> => {
            if (!agent || variant !== "full") return;
            try {
                const [persistedMessages, persistedConversations] =
                    await Promise.all([
                        desktopApi.getAgentConversationMessages(
                            agent.id,
                            conversationKey,
                        ),
                        desktopApi.listAgentConversations(agent.id),
                    ]);
                if (persistedMessages.at(-1)?.role === "assistant") {
                    setMessages((current) => {
                        const skillActivities =
                            current.at(-1)?.skillActivities ?? [];
                        if (
                            skillActivities.length === 0 ||
                            persistedMessages.some(
                                (message) => message.skillActivities?.length,
                            )
                        ) {
                            return persistedMessages;
                        }

                        const targetIndex = persistedMessages.findLastIndex(
                            (message) =>
                                message.role === "assistant" &&
                                message.content.trim().length > 0,
                        );
                        return persistedMessages.map((message, index) =>
                            index === targetIndex
                                ? { ...message, skillActivities }
                                : message,
                        );
                    });
                }
                setConversations(
                    persistedConversations.map((conversation) =>
                        toConversationItem(conversation, t),
                    ),
                );
            } catch (reason) {
                void messageApi.error(
                    reason instanceof Error
                        ? reason.message
                        : t("chat.panel.errors.historyRefresh"),
                );
            }
        },
        [agent, messageApi, t, variant],
    );

    useEffect(
        () =>
            desktopApi.onChatRunChanged((snapshot) => {
                if (snapshot.requestId !== latestRequestId) return;
                applySnapshot(snapshot);
            }),
        [applySnapshot, latestRequestId],
    );

    useEffect(() => {
        if (authStatus !== "authenticated" || !latestRequestId) return;

        void desktopApi.getChatRun(latestRequestId).then((snapshot) => {
            if (!snapshot) return;
            applySnapshot(snapshot);
            if (snapshot.status !== "running" && activeConversationKey) {
                void refreshPersistedConversation(activeConversationKey);
            }
        });
    }, [
        activeConversationKey,
        applySnapshot,
        authStatus,
        latestRequestId,
        refreshPersistedConversation,
    ]);

    const send = async (value = draft): Promise<void> => {
        const content = value.trim();
        if (!agent || !content || activeRequestId) return;
        const requestId = crypto.randomUUID();
        const userMessage: ChatMessage = { role: "user", content };
        const history: ChatMessage[] = [...messages, userMessage];
        const pendingMessages: ChatMessage[] = [
            ...history,
            { role: "assistant", content: "" },
        ];
        let conversationKey = activeConversationKey;
        setMessages(pendingMessages);
        setDraft("");
        setChatError(null);
        setActiveRequestId(requestId);
        setLatestRequestId(requestId);
        try {
            if (variant === "full" && !conversationKey) {
                const conversation = await desktopApi.createAgentConversation(
                    agent.id,
                );
                conversationKey = conversation.id;
                setActiveConversationKey(conversation.id);
                setConversations((current) => [
                    toConversationItem(conversation, t),
                    ...current,
                ]);
            }

            await desktopApi.chat({
                requestId,
                agentId: agent.id,
                runMode,
                conversationId: variant === "full" ? conversationKey : null,
                messages: variant === "full" ? [userMessage] : history,
            });
        } catch (reason) {
            const snapshot = await desktopApi
                .getChatRun(requestId)
                .catch(() => null);
            if (snapshot) applySnapshot(snapshot, content);
            else {
                setChatError({
                    code: "NETWORK_ERROR",
                    reason:
                        reason instanceof Error
                            ? reason.message
                            : t("chat.panel.errors.agentCall"),
                    input: content,
                });
            }
        } finally {
            const snapshot = await desktopApi
                .getChatRun(requestId)
                .catch(() => null);
            if (snapshot) applySnapshot(snapshot, content);
            if (variant === "full" && conversationKey) {
                await refreshPersistedConversation(conversationKey);
            }
            setActiveRequestId((current) =>
                current === requestId ? null : current,
            );
        }
    };

    const stop = (): void => {
        if (activeRequestId) void desktopApi.stopChat(activeRequestId);
    };

    const retry = (): void => {
        if (chatError?.input) void send(chatError.input);
    };

    const regenerate = (assistantMessageIndex: number): void => {
        if (activeRequestId) return;
        const sourceMessage = messages
            .slice(0, assistantMessageIndex)
            .findLast((chatMessage) => chatMessage.role === "user");
        if (sourceMessage?.content) void send(sourceMessage.content);
    };

    const startNewConversation = (): void => {
        if (activeRequestId) return;
        setShowScrollToLatest(false);
        setActiveConversationKey(null);
        setMessages([]);
        setDraft("");
        setChatError(null);
        setLatestRequestId(null);
    };

    const switchConversation = async (key: string): Promise<void> => {
        if (activeRequestId) return;
        const conversation = conversations.find((item) => item.key === key);
        if (!conversation || !agent) return;
        setShowScrollToLatest(false);
        setActiveConversationKey(key);
        setDraft("");
        setChatError(null);
        setLatestRequestId(null);
        try {
            setMessages(
                await desktopApi.getAgentConversationMessages(agent.id, key),
            );
        } catch (reason) {
            void messageApi.error(
                reason instanceof Error
                    ? reason.message
                    : t("chat.panel.errors.messagesLoad"),
            );
        }
    };

    const deleteConversation = async (key: string): Promise<void> => {
        if (activeRequestId || !agent) return;
        try {
            await desktopApi.deleteAgentConversation(agent.id, key);
        } catch (reason) {
            void messageApi.error(
                reason instanceof Error
                    ? reason.message
                    : t("chat.panel.errors.deleteConversation"),
            );
            return;
        }

        const remaining = conversations.filter((item) => item.key !== key);
        setConversations(remaining);
        if (activeConversationKey !== key) return;

        const next = remaining[0];
        setActiveConversationKey(next ? String(next.key) : null);
        setMessages([]);
        if (next) await switchConversation(String(next.key));
    };

    const reloadConversation = async (
        conversationKey: string,
    ): Promise<void> => {
        if (!agent) return;
        setShowScrollToLatest(false);
        const [persistedMessages, persistedConversations] = await Promise.all([
            desktopApi.getAgentConversationMessages(agent.id, conversationKey),
            desktopApi.listAgentConversations(agent.id),
        ]);
        setMessages(persistedMessages);
        setConversations(
            persistedConversations.map((conversation) =>
                toConversationItem(conversation, t),
            ),
        );
    };

    const confirmDeleteConversation = (key: string): void => {
        Modal.confirm({
            title: t("chat.panel.deleteDialog.title"),
            content: t("chat.panel.deleteDialog.content"),
            okText: t("chat.panel.deleteDialog.confirm"),
            okButtonProps: { danger: true },
            cancelText: t("common.cancel"),
            onOk: () => deleteConversation(key),
        });
    };

    const confirmClearConversation = (): void => {
        if (!agent || !activeConversationKey || activeRequestId) return;
        const agentId = agent.id;
        const conversationKey = activeConversationKey;
        Modal.confirm({
            title: t("chat.panel.clearDialog.title"),
            content: t("chat.panel.clearDialog.content"),
            okText: t("chat.panel.clearDialog.confirm"),
            okButtonProps: { danger: true },
            cancelText: t("common.cancel"),
            onOk: async () => {
                try {
                    await desktopApi.clearAgentConversation(
                        agentId,
                        conversationKey,
                    );
                    await reloadConversation(conversationKey);
                } catch (reason) {
                    void messageApi.error(
                        reason instanceof Error
                            ? reason.message
                            : t("chat.panel.errors.clearConversation"),
                    );
                    throw reason;
                }
            },
        });
    };

    const handleMessageScroll = (event: UIEvent<HTMLDivElement>): void => {
        const scrollBox = event.currentTarget;
        const hasOverflow = scrollBox.scrollHeight > scrollBox.clientHeight + 1;
        setShowScrollToLatest(
            hasOverflow && Math.abs(scrollBox.scrollTop) > 24,
        );
    };

    const scrollToLatestMessage = (): void => {
        messageListRef.current?.scrollTo({
            top: "bottom",
            behavior: "smooth",
        });
    };

    if (!agent)
        return (
            <section className="chat-panel chat-empty">
                <Empty description={t("chat.panel.selectAgent")} />
            </section>
        );

    if (variant === "full") {
        return (
            <section className="chat-panel-full">
                {contextHolder}
                <header className="chat-page-header">
                    <Tooltip title={t("chat.panel.back")}>
                        <Button
                            type="text"
                            aria-label={t("chat.panel.back")}
                            icon={<ArrowLeftOutlined />}
                            onClick={onClose}
                        />
                    </Tooltip>
                    <Avatar
                        className="agent-avatar"
                        size={28}
                        src={agent.avatarUri ?? undefined}
                        icon={agent.avatarUri ? undefined : <RobotOutlined />}
                    />
                    <Typography.Text strong>{agent.name}</Typography.Text>
                    <Tag>
                        {t("chat.panel.model", {
                            model:
                                agentDetailsQuery.data?.buildOptions
                                    .modelOptions.modelId ??
                                t("chat.panel.modelNotConfigured"),
                        })}
                    </Tag>
                    <Tag>
                        {activeConversation
                            ? activeConversationVersionNumber === undefined
                                ? t("chat.panel.versionLoading")
                                : t("chat.panel.version", {
                                      version: activeConversationVersionNumber,
                                  })
                            : t("chat.panel.version", {
                                  version: agent.latestPublishedVersionNumber,
                              })}
                    </Tag>
                    <div className="chat-page-header-actions">
                        <Tooltip title={t("chat.panel.viewDetails")}>
                            <Button
                                type="text"
                                aria-label={t("chat.panel.viewDetails")}
                                icon={<EyeOutlined />}
                                loading={
                                    agentDetailsQuery.isLoading ||
                                    agentVersionsQuery.isLoading ||
                                    toolsQuery.isLoading
                                }
                                disabled={!detailVersion}
                                onClick={() => setDetailsOpen(true)}
                            />
                        </Tooltip>
                        <Tooltip title={t("chat.panel.clearConversation")}>
                            <Button
                                type="text"
                                aria-label={t("chat.panel.clearConversation")}
                                icon={<ClearOutlined />}
                                disabled={
                                    !activeConversationKey ||
                                    messages.length === 0 ||
                                    Boolean(activeRequestId)
                                }
                                onClick={confirmClearConversation}
                            />
                        </Tooltip>
                    </div>
                </header>

                <AgentDetailsDrawer
                    appearance={appearance}
                    open={detailsOpen}
                    version={detailVersion}
                    tools={toolsQuery.data ?? []}
                    statusLabel={null}
                    onClose={() => setDetailsOpen(false)}
                />

                <div className="chat-page-body">
                    <aside
                        className={`chat-history ${historyCollapsed ? "collapsed" : ""}`}
                    >
                        <div className="chat-history-header">
                            {!historyCollapsed && (
                                <Typography.Text type="secondary">
                                    {t("chat.panel.conversations")}
                                </Typography.Text>
                            )}
                            <Tooltip
                                title={
                                    historyCollapsed
                                        ? t("chat.panel.expandConversations")
                                        : t("chat.panel.collapseConversations")
                                }
                            >
                                <Button
                                    type="text"
                                    size="small"
                                    aria-label={
                                        historyCollapsed
                                            ? t(
                                                  "chat.panel.expandConversations",
                                              )
                                            : t(
                                                  "chat.panel.collapseConversations",
                                              )
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
                                <Tooltip
                                    title={t("chat.panel.createConversation")}
                                    placement="right"
                                >
                                    <Button
                                        block
                                        type="text"
                                        aria-label={t(
                                            "chat.panel.createConversation",
                                        )}
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
                                        label: t(
                                            "chat.panel.createConversation",
                                        ),
                                        icon: <PlusOutlined />,
                                        onClick: startNewConversation,
                                    }}
                                    onActiveChange={(key) =>
                                        void switchConversation(String(key))
                                    }
                                    menu={(conversation) => ({
                                        items: [
                                            {
                                                key: "delete",
                                                label: t("common.delete"),
                                                danger: true,
                                                icon: <DeleteOutlined />,
                                                onClick: () =>
                                                    confirmDeleteConversation(
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
                            <>
                                <div className="chat-full-messages">
                                    <ChatMessageList
                                        listRef={messageListRef}
                                        messages={messages}
                                        activeRequestId={activeRequestId}
                                        error={chatError}
                                        onRegenerate={regenerate}
                                        onRetry={retry}
                                        onScroll={handleMessageScroll}
                                    />
                                    {showScrollToLatest && (
                                        <Tooltip
                                            title={t(
                                                "chat.panel.scrollToLatest",
                                            )}
                                        >
                                            <Button
                                                className="chat-scroll-to-latest"
                                                shape="circle"
                                                aria-label={t(
                                                    "chat.panel.scrollToLatest",
                                                )}
                                                icon={<ArrowDownOutlined />}
                                                onClick={scrollToLatestMessage}
                                            />
                                        </Tooltip>
                                    )}
                                </div>
                                <ChatComposer
                                    variant="full"
                                    value={draft}
                                    loading={Boolean(activeRequestId)}
                                    showPrompts
                                    onChange={setDraft}
                                    onSubmit={(value) => void send(value)}
                                    onCancel={stop}
                                />
                            </>
                        ) : (
                            <div className="chat-full-empty">
                                <Typography.Title level={3}>
                                    {agent.name}
                                </Typography.Title>
                                <ChatQuickPrompts
                                    variant="full"
                                    compact={false}
                                    onSelect={(prompt) => void send(prompt)}
                                />
                                <ChatComposer
                                    variant="full"
                                    value={draft}
                                    loading={Boolean(activeRequestId)}
                                    showPrompts={false}
                                    onChange={setDraft}
                                    onSubmit={(value) => void send(value)}
                                    onCancel={stop}
                                />
                            </div>
                        )}
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
                                ? t("chat.panel.currentDraft")
                                : t("chat.panel.publishedVersion", {
                                      version:
                                          agent.latestPublishedVersionNumber,
                                  })
                            : t("chat.panel.publishedVersion", {
                                  version: agent.latestPublishedVersionNumber,
                              })}
                    </Typography.Text>
                </div>
                {variant === "trial" && (
                    <div className="chat-trial-actions">
                        <Button
                            type="text"
                            aria-label={t("chat.panel.newTrial")}
                            icon={<PlusOutlined />}
                            onClick={() => setMessages([])}
                        />
                        <Button
                            type="text"
                            aria-label={t("chat.panel.trialList")}
                            icon={<CommentOutlined />}
                        />
                        <Button
                            type="text"
                            aria-label={t("chat.panel.closeTrial")}
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
                                title={t("chat.panel.trialWelcomeTitle")}
                                description={t(
                                    "chat.panel.trialWelcomeDescription",
                                )}
                            />
                            <Prompts
                                vertical
                                items={ChatQuickPromptKeys.map((key) => ({
                                    key,
                                    description: t(
                                        `chat.prompts.${key}.description`,
                                    ),
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
                                {t("chat.panel.startConversation")}
                            </Typography.Title>
                            <Typography.Text type="secondary">
                                {t("chat.panel.publishedMessage")}
                            </Typography.Text>
                        </div>
                    )
                ) : (
                    <ChatMessageList
                        messages={messages}
                        activeRequestId={activeRequestId}
                        error={chatError}
                        onRegenerate={regenerate}
                        onRetry={retry}
                    />
                )}
            </div>
            <ChatComposer
                variant="trial"
                value={draft}
                loading={Boolean(activeRequestId)}
                showPrompts
                versionLabel={
                    runMode === "draft"
                        ? t("chat.panel.usingDraft")
                        : t("chat.panel.usingPublished", {
                              version: agent.latestPublishedVersionNumber,
                          })
                }
                onChange={setDraft}
                onSubmit={(value) => void send(value)}
                onCancel={stop}
            />
        </section>
    );
}
