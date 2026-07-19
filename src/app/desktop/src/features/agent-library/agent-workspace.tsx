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
import { useDeferredValue, useState } from "react";
import type { ReactNode } from "react";
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
    const identity = useAuthStore((state) => state.identity);
    const { token } = antdTheme.useToken();
    const queryClient = useQueryClient();
    const [activeTab, setActiveTab] = useState<AgentTab>("mine");
    const [statusFilter, setStatusFilter] =
        useState<AgentStatusFilter>("all");
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
            if (action === "revoke")
                await desktopApi.revokeAgentShare(agentId);
        },
        onSuccess: async (_result, variables) => {
            await queryClient.invalidateQueries({ queryKey: ["agents"] });
            const successMessages: Record<AgentAction, string> = {
                share: "Agent 已共享给团队",
                unshare: "已撤销团队共享",
                revoke: "已由管理员撤销共享",
            };
            messageApi.success(successMessages[variables.action]);
        },
        onError: (reason) => {
            messageApi.error(
                reason instanceof Error ? reason.message : "Agent 操作失败。",
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
        messageApi.info("Agent 配置页将在下一项工作中接入。");
    };
    const openAgent = (agent: AgentListItem): void => {
        if (isPublished(agent)) setChatAgent(agent);
        else openEditor(agent.id);
    };
    const requestAction = (action: AgentAction, agent: AgentListItem): void => {
        modalApi.confirm({
            ...getActionDialog(action, agent.name),
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
                <Typography.Title level={4}>Agent 空间</Typography.Title>
                <Button
                    type="primary"
                    icon={<PlusOutlined />}
                    onClick={() => openEditor()}
                >
                    新建 Agent
                </Button>
            </header>

            <section className="agent-space-toolbar">
                <Tabs
                    activeKey={activeTab}
                    onChange={changeTab}
                    items={[
                        { key: "mine", label: "我的" },
                        { key: "shared", label: "团队共享" },
                    ]}
                />
                <Space wrap>
                    <Tooltip title="刷新">
                        <Button
                            aria-label="刷新 Agent"
                            icon={<ReloadOutlined />}
                            loading={currentQuery.isFetching}
                            onClick={refresh}
                        />
                    </Tooltip>
                    <Input
                        allowClear
                        maxLength={50}
                        prefix={<SearchOutlined />}
                        placeholder="搜索 Agent"
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
                        { value: "all", label: `全部 ${scopedAgents.length}` },
                        {
                            value: "published",
                            label: `已发布 ${publishedCount}`,
                        },
                        { value: "draft", label: `草稿 ${draftCount}` },
                    ]}
                />
            )}

            <section className={`agent-space-canvas ${activeTab}`}>
                {currentQuery.isLoading ? (
                    <AgentGridSkeleton />
                ) : currentQuery.isError ? (
                    <AgentGridEmpty
                        description="加载失败，请检查网络后重试"
                        action={<Button onClick={refresh}>重试</Button>}
                    />
                ) : pageAgents.length === 0 ? (
                    <AgentGridEmpty
                        description={getEmptyDescription(
                            activeTab,
                            Boolean(deferredSearch),
                            statusFilter,
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
                                    avatarColor={[
                                        token.colorPrimary,
                                        token.colorInfo,
                                        token.colorSuccess,
                                        token.colorWarning,
                                    ][index % 4]}
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

            {filteredAgents.length > 0 && (
                <footer className="agent-space-pagination">
                    <Typography.Text type="secondary">
                        共 {filteredAgents.length} 个 Agent
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
            )}
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
                <Space size={0} className="agent-card-actions">
                    {showOwnerActions && (
                        <Tooltip title="编辑配置">
                            <Button
                                type="text"
                                size="small"
                                aria-label={`编辑 ${agent.name}`}
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
                        <Tooltip title="共享已发布版本">
                            <Button
                                type="text"
                                size="small"
                                aria-label={`共享 ${agent.name}`}
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
                        <Tooltip title="撤销共享">
                            <Button
                                danger
                                type="text"
                                size="small"
                                aria-label={`撤销 ${agent.name} 共享`}
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
                        <Tooltip title="查看详情">
                            <Button
                                type="text"
                                size="small"
                                aria-label={`查看 ${agent.name} 详情`}
                                icon={<EyeOutlined />}
                                onClick={(event) => {
                                    event.stopPropagation();
                                    onView();
                                }}
                            />
                        </Tooltip>
                    )}
                    {activeTab === "shared" && isAdmin && (
                        <Tooltip title="撤销共享">
                            <Button
                                danger
                                type="text"
                                size="small"
                                aria-label={`撤销 ${agent.name} 共享`}
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
                        {!isPublished(agent) && <Tag color="warning">草稿</Tag>}
                        {activeTab === "mine" && agent.isShared && (
                            <Tag color="processing">已共享</Tag>
                        )}
                    </Space>
                    <Typography.Text type="secondary" ellipsis>
                        {activeTab === "shared"
                            ? `${shortId(agent.ownerUserId)} · `
                            : ""}
                        {isPublished(agent)
                            ? `v${agent.latestPublishedVersionNumber}`
                            : "未发布"}
                        {` · ${formatRelativeTime(agent.updatedTime)}`}
                    </Typography.Text>
                </div>
            </div>
            <Typography.Paragraph
                className={`agent-card-description actions-${actionCount}`}
                type="secondary"
                ellipsis={{ rows: 2 }}
            >
                {agent.descriptionExcerpt || "暂无描述"}
            </Typography.Paragraph>
        </Card>
    );
}

function AgentGridSkeleton() {
    return (
        <Row gutter={[12, 12]}>
            {Array.from({ length: 8 }, (_, index) => (
                <Col
                    className="agent-space-column"
                    key={index}
                    flex="0 0 20%"
                >
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
): string {
    if (hasSearch || statusFilter !== "all") return "没有符合条件的 Agent";
    return activeTab === "mine"
        ? '还没有自己的 Agent，点击“新建 Agent”开始'
        : "团队成员还没有共享 Agent";
}

function getActionDialog(
    action: AgentAction,
    agentName: string,
): ModalFuncProps {
    if (action === "revoke") {
        return {
            title: `撤销「${agentName}」的共享`,
            content: "撤销后，其他成员将无法继续访问；Owner 原件不会被删除。",
            okText: "确认撤销",
            okButtonProps: { danger: true },
            cancelText: "取消",
        };
    }
    return {
        title: `共享「${agentName}」`,
        content: "共享后，团队成员可以查看并使用该 Agent 的已发布版本。",
        okText: "确认共享",
        cancelText: "取消",
    };
}

function shortId(value: string): string {
    return value.slice(0, 8);
}

function formatRelativeTime(value: string): string {
    const time = Date.parse(value);
    if (Number.isNaN(time)) return "刚刚更新";
    const elapsedMinutes = Math.max(
        0,
        Math.floor((Date.now() - time) / 60_000),
    );
    if (elapsedMinutes < 1) return "刚刚更新";
    if (elapsedMinutes < 60) return `${elapsedMinutes} 分钟前`;
    const elapsedHours = Math.floor(elapsedMinutes / 60);
    if (elapsedHours < 24) return `${elapsedHours} 小时前`;
    return `${Math.floor(elapsedHours / 24)} 天前`;
}