import {
    EditOutlined,
    EyeOutlined,
    PlusOutlined,
    ReloadOutlined,
    SearchOutlined,
    ShareAltOutlined,
    UndoOutlined,
} from "@ant-design/icons";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
    Button,
    Card,
    Col,
    Empty,
    Input,
    Modal,
    Pagination,
    Row,
    Segmented,
    Skeleton,
    Space,
    Tabs,
    Tag,
    Tooltip,
    Typography,
    message,
    theme as antdTheme,
} from "antd";
import type { ModalFuncProps } from "antd";
import type { TFunction } from "i18next";
import { useDeferredValue, useState } from "react";
import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { desktopApi } from "../../shared/network/desktop-api";
import type { AgentListItem } from "../../shared/network/contracts";
import { useAuthStore } from "../auth/auth-store";
import { ChatPanel } from "../chat/chat-panel";

type AgentTab = "mine" | "shared";
type AgentStatusFilter = "all" | "published" | "draft";
type AgentAction = "share" | "unshare" | "revoke";

const AgentsPerPage = 20;

interface AgentWorkspaceProps {
    onCreateAgent?: () => void;
    onEditAgent?: (agentId: string) => void;
}

export function AgentWorkspace({
    onCreateAgent,
    onEditAgent,
}: AgentWorkspaceProps) {
    const { t } = useTranslation();
    const identity = useAuthStore((state) => state.identity);
    const { token } = antdTheme.useToken();
    const queryClient = useQueryClient();
    const [activeTab, setActiveTab] = useState<AgentTab>("mine");
    const [statusFilter, setStatusFilter] = useState<AgentStatusFilter>("all");
    const [searchText, setSearchText] = useState("");
    const deferredSearch = useDeferredValue(
        searchText.trim().toLocaleLowerCase(),
    );
    const [page, setPage] = useState(1);
    const [chatAgent, setChatAgent] = useState<AgentListItem | null>(null);
    const [modalApi, modalContextHolder] = Modal.useModal();
    const [messageApi, messageContextHolder] = message.useMessage();
    const mineQuery = useQuery({
        queryKey: ["agents", "mine"],
        queryFn: desktopApi.listMyAgents,
    });
    const sharedQuery = useQuery({
        queryKey: ["agents", "shared"],
        queryFn: desktopApi.listSharedAgents,
    });
    const actionMutation = useMutation({
        mutationFn: async ({
            action,
            agentId,
        }: {
            action: AgentAction;
            agentId: string;
        }) => {
            if (action === "share") await desktopApi.shareAgent(agentId);
            if (action === "unshare") await desktopApi.unshareAgent(agentId);
            if (action === "revoke") await desktopApi.revokeAgentShare(agentId);
        },
        onSuccess: async (_result, variables) => {
            await queryClient.invalidateQueries({ queryKey: ["agents"] });
            const successMessageKeys: Record<AgentAction, string> = {
                share: "agents.space.actionSuccess.share",
                unshare: "agents.space.actionSuccess.unshare",
                revoke: "agents.space.actionSuccess.revoke",
            };
            messageApi.success(t(successMessageKeys[variables.action]));
        },
        onError: (reason) => {
            messageApi.error(
                reason instanceof Error
                    ? reason.message
                    : t("agents.space.actionFailed"),
            );
        },
    });

    const currentQuery = activeTab === "mine" ? mineQuery : sharedQuery;
    const scopedAgents = currentQuery.data ?? [];
    const publishedCount = scopedAgents.filter(isPublished).length;
    const draftCount = scopedAgents.length - publishedCount;
    const filteredAgents = scopedAgents
        .filter((agent) => {
            if (activeTab !== "mine" || statusFilter === "all") return true;
            return statusFilter === "published"
                ? isPublished(agent)
                : !isPublished(agent);
        })
        .filter((agent) =>
            agent.name.toLocaleLowerCase().includes(deferredSearch),
        )
        .sort((left, right) =>
            activeTab === "mine"
                ? Number(isPublished(left)) - Number(isPublished(right))
                : 0,
        );
    const lastPage = Math.max(
        1,
        Math.ceil(filteredAgents.length / AgentsPerPage),
    );
    const currentPage = Math.min(page, lastPage);
    const pageAgents = filteredAgents.slice(
        (currentPage - 1) * AgentsPerPage,
        currentPage * AgentsPerPage,
    );

    if (chatAgent) {
        return (
            <main className="agent-chat-workspace">
                <ChatPanel
                    key={chatAgent.id}
                    agent={chatAgent}
                    onClose={() => setChatAgent(null)}
                />
            </main>
        );
    }

    const openEditor = (agentId?: string): void => {
        if (agentId && onEditAgent) {
            onEditAgent(agentId);
            return;
        }
        if (!agentId && onCreateAgent) {
            onCreateAgent();
            return;
        }
        messageApi.info(t("agents.space.configComingSoon"));
    };
    const openAgent = (agent: AgentListItem): void => {
        if (isPublished(agent)) setChatAgent(agent);
        else openEditor(agent.id);
    };
    const requestAction = (action: AgentAction, agent: AgentListItem): void => {
        modalApi.confirm({
            ...getActionDialog(action, agent.name, t),
            onOk: async () =>
                actionMutation.mutateAsync({ action, agentId: agent.id }),
        });
    };
    const refresh = (): void => {
        void currentQuery.refetch();
    };
    const changeTab = (key: string): void => {
        setActiveTab(key as AgentTab);
        setStatusFilter("all");
        setSearchText("");
        setPage(1);
    };

    return (
        <main className="agent-space-page">
            {modalContextHolder}
            {messageContextHolder}
            <header className="agent-space-header">
                <Typography.Title level={4}>
                    {t("agents.space.title")}
                </Typography.Title>
                <Button
                    type="primary"
                    icon={<PlusOutlined />}
                    onClick={() => openEditor()}
                >
                    {t("agents.space.create")}
                </Button>
            </header>

            <section className="agent-space-toolbar">
                <Tabs
                    activeKey={activeTab}
                    onChange={changeTab}
                    items={[
                        { key: "mine", label: t("agents.space.mine") },
                        { key: "shared", label: t("agents.space.shared") },
                    ]}
                />
                <Space wrap>
                    <Tooltip title={t("common.refresh")}>
                        <Button
                            aria-label={t("agents.space.refreshLabel")}
                            icon={<ReloadOutlined />}
                            loading={currentQuery.isFetching}
                            onClick={refresh}
                        />
                    </Tooltip>
                    <Input
                        allowClear
                        maxLength={50}
                        prefix={<SearchOutlined />}
                        placeholder={t("agents.space.searchPlaceholder")}
                        value={searchText}
                        onChange={(event) => {
                            setSearchText(event.target.value);
                            setPage(1);
                        }}
                    />
                </Space>
            </section>

            {activeTab === "mine" && (
                <Segmented<AgentStatusFilter>
                    className="agent-status-filter"
                    value={statusFilter}
                    onChange={(value) => {
                        setStatusFilter(value);
                        setPage(1);
                    }}
                    options={[
                        {
                            value: "all",
                            label: t("agents.space.filters.all", {
                                count: scopedAgents.length,
                            }),
                        },
                        {
                            value: "published",
                            label: t("agents.space.filters.published", {
                                count: publishedCount,
                            }),
                        },
                        {
                            value: "draft",
                            label: t("agents.space.filters.draft", {
                                count: draftCount,
                            }),
                        },
                    ]}
                />
            )}

            <section className={`agent-space-canvas ${activeTab}`}>
                {currentQuery.isLoading ? (
                    <AgentGridSkeleton />
                ) : currentQuery.isError ? (
                    <AgentGridEmpty
                        description={t("agents.space.loadError")}
                        action={
                            <Button onClick={refresh}>
                                {t("common.retry")}
                            </Button>
                        }
                    />
                ) : pageAgents.length === 0 ? (
                    <AgentGridEmpty
                        description={getEmptyDescription(
                            activeTab,
                            Boolean(deferredSearch),
                            statusFilter,
                            t,
                        )}
                    />
                ) : (
                    <Row gutter={[12, 12]} align="stretch">
                        {pageAgents.map((agent, index) => (
                            <Col
                                className="agent-space-column"
                                key={agent.id}
                                flex="0 0 20%"
                            >
                                <AgentCard
                                    agent={agent}
                                    avatarColor={
                                        [
                                            token.colorPrimary,
                                            token.colorInfo,
                                            token.colorSuccess,
                                            token.colorWarning,
                                        ][index % 4]
                                    }
                                    activeTab={activeTab}
                                    currentUserId={identity?.userId}
                                    isAdmin={identity?.isAdmin === true}
                                    isPending={actionMutation.isPending}
                                    onOpen={() => openAgent(agent)}
                                    onEdit={() => openEditor(agent.id)}
                                    onView={() => openEditor(agent.id)}
                                    onAction={(action) =>
                                        requestAction(action, agent)
                                    }
                                />
                            </Col>
                        ))}
                    </Row>
                )}
            </section>

            <footer className="agent-space-pagination">
                <Typography.Text type="secondary">
                    {t("common.itemCount", {
                        count: filteredAgents.length,
                    })}
                </Typography.Text>
                <Pagination
                    size="small"
                    current={currentPage}
                    pageSize={AgentsPerPage}
                    total={filteredAgents.length}
                    showSizeChanger={false}
                    onChange={setPage}
                />
            </footer>
        </main>
    );
}

function AgentCard({
    agent,
    avatarColor,
    activeTab,
    currentUserId,
    isAdmin,
    isPending,
    onOpen,
    onEdit,
    onView,
    onAction,
}: {
    agent: AgentListItem;
    avatarColor: string;
    activeTab: AgentTab;
    currentUserId?: string;
    isAdmin: boolean;
    isPending: boolean;
    onOpen: () => void;
    onEdit: () => void;
    onView: () => void;
    onAction: (action: AgentAction) => void;
}) {
    const { t } = useTranslation();
    const isOwner = agent.ownerUserId === currentUserId;
    const showOwnerActions = activeTab === "mine" && isOwner;
    const actionCount = showOwnerActions
        ? isPublished(agent)
            ? 2
            : 1
        : activeTab === "shared"
          ? isAdmin
              ? 2
              : 1
          : 0;

    return (
        <Card
            hoverable
            className="agent-space-card"
            onClick={onOpen}
            styles={{ body: { padding: 12 } }}
        >
            {(showOwnerActions || activeTab === "shared") && (
                <Space size={6} className="agent-card-actions">
                    {showOwnerActions && (
                        <Tooltip title={t("agents.space.actions.edit")}>
                            <Button
                                type="text"
                                size="small"
                                aria-label={t(
                                    "agents.space.actions.editLabel",
                                    { name: agent.name },
                                )}
                                icon={<EditOutlined />}
                                onClick={(event) => {
                                    event.stopPropagation();
                                    onEdit();
                                }}
                            />
                        </Tooltip>
                    )}
                    {showOwnerActions &&
                        isPublished(agent) &&
                        !agent.isShared && (
                            <Tooltip title={t("agents.space.actions.share")}>
                                <Button
                                    type="text"
                                    size="small"
                                    aria-label={t(
                                        "agents.space.actions.shareLabel",
                                        { name: agent.name },
                                    )}
                                    icon={<ShareAltOutlined />}
                                    loading={isPending}
                                    onClick={(event) => {
                                        event.stopPropagation();
                                        onAction("share");
                                    }}
                                />
                            </Tooltip>
                        )}
                    {showOwnerActions && agent.isShared && (
                        <Tooltip title={t("agents.space.actions.revoke")}>
                            <Button
                                danger
                                type="text"
                                size="small"
                                aria-label={t(
                                    "agents.space.actions.revokeLabel",
                                    { name: agent.name },
                                )}
                                icon={<UndoOutlined />}
                                loading={isPending}
                                onClick={(event) => {
                                    event.stopPropagation();
                                    onAction("unshare");
                                }}
                            />
                        </Tooltip>
                    )}
                    {activeTab === "shared" && (
                        <Tooltip title={t("agents.space.actions.view")}>
                            <Button
                                type="text"
                                size="small"
                                aria-label={t(
                                    "agents.space.actions.viewLabel",
                                    { name: agent.name },
                                )}
                                icon={<EyeOutlined />}
                                onClick={(event) => {
                                    event.stopPropagation();
                                    onView();
                                }}
                            />
                        </Tooltip>
                    )}
                    {activeTab === "shared" && isAdmin && (
                        <Tooltip title={t("agents.space.actions.revoke")}>
                            <Button
                                danger
                                type="text"
                                size="small"
                                aria-label={t(
                                    "agents.space.actions.revokeLabel",
                                    { name: agent.name },
                                )}
                                icon={<UndoOutlined />}
                                loading={isPending}
                                onClick={(event) => {
                                    event.stopPropagation();
                                    onAction("revoke");
                                }}
                            />
                        </Tooltip>
                    )}
                </Space>
            )}
            <div className="agent-card-heading">
                <div
                    className="agent-card-avatar"
                    style={{ background: avatarColor }}
                >
                    {agent.avatarUri ? (
                        <img src={agent.avatarUri} alt="" />
                    ) : (
                        <Typography.Text>
                            {agent.name.slice(0, 1)}
                        </Typography.Text>
                    )}
                </div>
                <div className="agent-card-copy">
                    <Space size={6}>
                        <Typography.Text strong ellipsis>
                            {agent.name}
                        </Typography.Text>
                        {!isPublished(agent) && (
                            <Tag color="warning">
                                {t("agents.space.status.draft")}
                            </Tag>
                        )}
                        {activeTab === "mine" && agent.isShared && (
                            <Tag color="processing">
                                {t("agents.space.status.shared")}
                            </Tag>
                        )}
                    </Space>
                    <Typography.Text type="secondary" ellipsis>
                        {activeTab === "shared"
                            ? `${shortId(agent.ownerUserId)} · `
                            : ""}
                        {isPublished(agent)
                            ? `v${agent.latestPublishedVersionNumber}`
                            : t("agents.space.status.unpublished")}
                        {` · ${formatRelativeTime(agent.updatedTime, t)}`}
                    </Typography.Text>
                </div>
            </div>
            <Typography.Paragraph
                className={`agent-card-description actions-${actionCount}`}
                type="secondary"
                ellipsis={{ rows: 2 }}
            >
                {agent.descriptionExcerpt || t("agents.space.noDescription")}
            </Typography.Paragraph>
        </Card>
    );
}

function AgentGridSkeleton() {
    return (
        <Row gutter={[12, 12]}>
            {Array.from({ length: 8 }, (_, index) => (
                <Col className="agent-space-column" key={index} flex="0 0 20%">
                    <Card
                        className="agent-space-card"
                        styles={{ body: { padding: 14 } }}
                    >
                        <Skeleton
                            active
                            avatar
                            paragraph={{ rows: 2 }}
                            title={{ width: "55%" }}
                        />
                    </Card>
                </Col>
            ))}
        </Row>
    );
}

function AgentGridEmpty({
    description,
    action,
}: {
    description: string;
    action?: ReactNode;
}) {
    return (
        <div className="agent-space-empty">
            <Empty
                image={Empty.PRESENTED_IMAGE_SIMPLE}
                description={description}
            >
                {action}
            </Empty>
        </div>
    );
}

function isPublished(agent: AgentListItem): boolean {
    return agent.latestPublishedVersionNumber > 0;
}

function getEmptyDescription(
    activeTab: AgentTab,
    hasSearch: boolean,
    statusFilter: AgentStatusFilter,
    t: TFunction,
): string {
    if (hasSearch || statusFilter !== "all")
        return t("agents.space.empty.filtered");
    return activeTab === "mine"
        ? t("agents.space.empty.mine")
        : t("agents.space.empty.shared");
}

function getActionDialog(
    action: AgentAction,
    agentName: string,
    t: TFunction,
): ModalFuncProps {
    if (action === "revoke") {
        return {
            title: t("agents.space.dialogs.revokeTitle", { name: agentName }),
            content: t("agents.space.dialogs.revokeContent"),
            okText: t("agents.space.dialogs.confirmRevoke"),
            okButtonProps: { danger: true },
            cancelText: t("common.cancel"),
        };
    }
    return {
        title: t("agents.space.dialogs.shareTitle", { name: agentName }),
        content: t("agents.space.dialogs.shareContent"),
        okText: t("agents.space.dialogs.confirmShare"),
        cancelText: t("common.cancel"),
    };
}

function shortId(value: string): string {
    return value.slice(0, 8);
}

function formatRelativeTime(value: string, t: TFunction): string {
    const time = Date.parse(value);
    if (Number.isNaN(time)) return t("agents.space.relativeTime.now");
    const elapsedMinutes = Math.max(
        0,
        Math.floor((Date.now() - time) / 60_000),
    );
    if (elapsedMinutes < 1) return t("agents.space.relativeTime.now");
    if (elapsedMinutes < 60)
        return t("agents.space.relativeTime.minutes", {
            count: elapsedMinutes,
        });
    const elapsedHours = Math.floor(elapsedMinutes / 60);
    if (elapsedHours < 24)
        return t("agents.space.relativeTime.hours", { count: elapsedHours });
    return t("agents.space.relativeTime.days", {
        count: Math.floor(elapsedHours / 24),
    });
}
