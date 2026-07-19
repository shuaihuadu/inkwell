import { useState } from "react";
import {
    Alert,
    Avatar,
    Button,
    Card,
    Checkbox,
    Col,
    Divider,
    Dropdown,
    Drawer,
    Empty,
    Form,
    Input,
    Modal,
    Pagination,
    Progress,
    Row,
    Segmented,
    Select,
    Space,
    Switch,
    Tabs,
    Tag,
    Tooltip,
    Typography,
    message,
    theme as antdTheme,
} from "antd";
import {
    AppstoreOutlined,
    ApiOutlined,
    BookOutlined,
    BulbFilled,
    BulbOutlined,
    DesktopOutlined,
    DownOutlined,
    EditOutlined,
    EyeOutlined,
    GithubOutlined,
    InfoCircleOutlined,
    KeyOutlined,
    LockOutlined,
    LogoutOutlined,
    MoonFilled,
    PlusOutlined,
    QuestionCircleOutlined,
    ReadOutlined,
    ReloadOutlined,
    RightOutlined,
    RocketOutlined,
    SafetyCertificateOutlined,
    SearchOutlined,
    SettingOutlined,
    ShareAltOutlined,
    SunFilled,
    ToolOutlined,
    UndoOutlined,
    UserOutlined,
} from "@ant-design/icons";
import logo from "../../assets/logos/logo.svg?no-inline";
import qrCode from "../../assets/quanzhange.jpg?no-inline";
import { LockScreen } from "../components/LockScreen";
import { useDesign, type AppearanceMode } from "../context/DesignContext";
import { THEMES, THEME_NAMES } from "../tokens/themes";
import AgentDesignPage from "./AgentDesignPage";
import AgentChatPage from "./AgentChatPage";
import ToolListPage from "./ToolListPage";
import SkillListPage from "./SkillListPage";
import ModelListPage from "./ModelListPage";
import UserManagementPage from "./UserManagementPage";
import UserGuidePage, { type GuideSection } from "./UserGuidePage";

interface ChangePasswordFormValues {
    currentPassword: string;
    newPassword: string;
    confirmPassword: string;
}

// ─── Types（对齐 ui-spec.md §0.2 / §11） ──────────────────────────────────────

type NetworkStatus = "online" | "reconnecting" | "offline";

type ErrorScenario = "none" | "ex-001" | "401" | "429" | "5xx";

type NavKey = "library" | "tools" | "skills" | "models" | "admin" | "guide";

type AgentStatusFilter = "all" | "published" | "draft";

// ─── Mock Data ────────────────────────────────────────────────────────────────

// 2026-07-15 修订：“在线”与用户在线状态（presence）易混淆；“网络正常”同样有歧义（可能被理解为本机网络），改为明确指代与后端服务的连接状态（对齐 ui-spec.md §0.2 网络状态徽标）。
const NETWORK_META: Record<NetworkStatus, { color: string; label: string }> = {
    online: { color: "#52C41A", label: "后台服务正常" },
    reconnecting: { color: "#FAAD14", label: "重连中" },
    offline: { color: "#F5222D", label: "后台服务异常" },
};

const ERROR_BANNER_META: Record<
    Exclude<ErrorScenario, "none">,
    { type: "warning" | "error"; message: string }
> = {
    "ex-001": {
        type: "warning",
        message:
            "网络连接已断开，正在尝试重新连接…写操作已禁用，恢复后自动收起",
    },
    "401": {
        type: "error",
        message: "登录状态已失效，请重新登录",
    },
    "429": {
        type: "warning",
        message: "操作过于频繁，请稍后再试",
    },
    "5xx": {
        type: "error",
        message: "服务暂时不可用（5xx），请稍后重试",
    },
};

// 左侧 nav 三个分组，每个分组都可独立展开/折叠（requirements.md §13 第 29/31 条 / ui-spec.md OQ-011 2026-07-15 两轮修订）：
// - 工作区（原“工作空间”）：Agent 空间（原“Agent 库”，实际入口）
// - 资源中心（原“能力管理”）：工具 / Skills / 模型
// - 系统管理：用户管理（仅 is_super 可见）
const NAV_GROUPS: {
    key: string;
    label: string;
    items: {
        key: NavKey;
        label: string;
        icon: React.ReactNode;
        requiresAdmin?: boolean;
    }[];
}[] = [
    {
        key: "workspace",
        label: "工作区",
        items: [
            { key: "library", label: "Agent 空间", icon: <AppstoreOutlined /> },
        ],
    },
    {
        key: "capability",
        label: "资源中心",
        items: [
            {
                key: "tools",
                label: "工具",
                icon: <ToolOutlined />,
            },
            {
                key: "skills",
                label: "Skills",
                icon: <ReadOutlined />,
            },
            {
                key: "models",
                label: "模型",
                icon: <ApiOutlined />,
            },
        ],
    },
    {
        key: "system",
        label: "系统管理",
        items: [
            {
                key: "admin",
                label: "用户管理",
                icon: <SafetyCertificateOutlined />,
                requiresAdmin: true,
            },
        ],
    },
];

// 2026-07-15 新增 published 字段：requirements.md §13 第 30 条——已发布 Agent 点击列表项
// 直达 UI-005 对话页，仅存在未发布过的草稿才跳 UI-004 详情/编辑页。
const MOCK_AGENTS = [
    {
        name: "客服助手",
        owner: "owner-alice",
        version: "v3",
        updated: "3 小时前",
        desc: "处理售前售后常见问题，支持多轮澄清与工单转接。",
        published: true,
        shared: true,
    },
    {
        name: "合同审查",
        owner: "owner-bob",
        version: "v2",
        updated: "昨天",
        desc: "审查合同条款风险点，输出结构化风险清单。",
        published: true,
        shared: true,
    },
    {
        name: "周报生成",
        owner: "owner-carol",
        version: "未发布",
        updated: "2 天前",
        desc: "汇总本周工作记录，生成结构化周报草稿。",
        published: false,
        shared: false,
    },
    {
        name: "代码评审与质量风险分析助手",
        owner: "owner-alice",
        version: "v5",
        updated: "5 天前",
        desc: "针对大型 Pull Request 的代码差异、依赖变化和上下游影响进行综合分析，识别潜在缺陷、安全风险与可维护性问题，并输出结构化评审建议。",
        published: true,
        shared: false,
    },
    ...Array.from({ length: 48 }, (_, index) => ({
        name:
            [
                "市场洞察",
                "会议纪要",
                "数据分析",
                "知识问答",
                "内容策划",
                "招聘助理",
                "项目复盘",
            ][index % 7] + ` ${index + 1}`,
        owner: index % 3 === 2 ? "owner-bob" : "owner-alice",
        version: index % 5 === 0 ? "未发布" : `v${(index % 4) + 1}`,
        updated: `${index + 1} 天前`,
        desc: [
            "归纳关键信息并输出可执行的结构化建议。",
            "结合团队知识库完成资料检索与内容整理。",
            "围绕业务目标生成分析结果和后续行动项。",
        ][index % 3],
        published: index % 5 !== 0,
        shared: index % 5 !== 0 && (index % 3 === 2 || index % 4 === 0),
    })),
];

const AGENTS_PER_PAGE = 20;

// ─── Agent Avatar ─────────────────────────────────────────────────────────────

function AgentAvatar({ name, color }: { name: string; color: string }) {
    return (
        <div
            style={{
                width: 40,
                height: 40,
                borderRadius: 10,
                background: color,
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                flexShrink: 0,
            }}
        >
            <Typography.Text
                style={{ color: "#fff", fontWeight: 700, fontSize: 16 }}
            >
                {name.slice(0, 1)}
            </Typography.Text>
        </div>
    );
}

// ─── UI-003 Agent 空间（占位内容） ──────────────────────────────────────────────

function AgentLibraryMock({
    activeTab,
    onTabChange,
    onSelectAgent,
    onCreateAgent,
    onEditAgent,
    onViewAgent,
    isAdmin,
}: {
    activeTab: string;
    onTabChange: (key: string) => void;
    onSelectAgent: (agent: { name: string; published: boolean }) => void;
    onCreateAgent: () => void;
    onEditAgent: (name: string) => void;
    onViewAgent: (name: string) => void;
    isAdmin: boolean;
}) {
    const { token } = antdTheme.useToken();
    const [statusFilter, setStatusFilter] = useState<AgentStatusFilter>("all");
    const [searchText, setSearchText] = useState("");
    const [page, setPage] = useState(1);
    const [sharedAgentNames, setSharedAgentNames] = useState(
        () =>
            new Set(
                MOCK_AGENTS.filter((agent) => agent.shared).map(
                    (agent) => agent.name,
                ),
            ),
    );
    const [modalApi, modalContextHolder] = Modal.useModal();
    const [messageApi, messageContextHolder] = message.useMessage();
    const palette = [
        token.colorPrimary,
        token.colorInfo,
        token.colorSuccess,
        token.colorWarning,
    ];
    const scopedAgents = MOCK_AGENTS.filter((agent) =>
        activeTab === "shared"
            ? agent.published &&
              sharedAgentNames.has(agent.name) &&
              agent.owner !== "owner-alice"
            : agent.owner === "owner-alice",
    );
    const publishedCount = scopedAgents.filter(
        (agent) => agent.published,
    ).length;
    const draftCount = scopedAgents.length - publishedCount;
    const filteredAgents = scopedAgents
        .filter((agent) => {
            if (statusFilter === "published") return agent.published;
            if (statusFilter === "draft") return !agent.published;
            return true;
        })
        .filter((agent) =>
            `${agent.name} ${agent.desc}`
                .toLowerCase()
                .includes(searchText.trim().toLowerCase()),
        )
        .sort((left, right) =>
            activeTab === "mine"
                ? Number(left.published) - Number(right.published)
                : 0,
        );
    const pageAgents = filteredAgents.slice(
        (page - 1) * AGENTS_PER_PAGE,
        page * AGENTS_PER_PAGE,
    );

    const handleTabChange = (key: string) => {
        onTabChange(key);
        setStatusFilter("all");
        setPage(1);
    };

    const shareAgent = (name: string, version: string) => {
        modalApi.confirm({
            title: `共享「${name}」给团队`,
            content: `团队成员将可以查看并使用当前已发布版本 ${version}。未发布的修改不会被共享。`,
            okText: "确认共享",
            cancelText: "取消",
            onOk: () => {
                setSharedAgentNames((current) => {
                    const next = new Set(current);
                    next.add(name);
                    return next;
                });
                messageApi.success("已共享给团队");
            },
        });
    };

    const unshareAgent = (name: string) => {
        modalApi.confirm({
            title: `撤销「${name}」的共享`,
            content: "撤销后，团队成员将无法继续访问该 Agent；你的 Agent 和已有配置不会被删除。",
            okText: "确认撤销",
            okButtonProps: { danger: true },
            cancelText: "取消",
            onOk: () => {
                setSharedAgentNames((current) => {
                    const next = new Set(current);
                    next.delete(name);
                    return next;
                });
                messageApi.success("已撤销团队共享");
            },
        });
    };

    return (
        <div>
            {modalContextHolder}
            {messageContextHolder}
            <div
                style={{
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                    marginBottom: 14,
                }}
            >
                <Typography.Title level={4} style={{ margin: 0 }}>
                    Agent 空间
                </Typography.Title>
                <Button
                    type="primary"
                    icon={<PlusOutlined />}
                    onClick={onCreateAgent}
                >
                    新建 Agent
                </Button>
            </div>

            <div
                style={{
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                    marginBottom: 8,
                    flexWrap: "wrap",
                    gap: 12,
                }}
            >
                <Tabs
                    activeKey={activeTab}
                    onChange={handleTabChange}
                    items={[
                        { key: "mine", label: "我的" },
                        { key: "shared", label: "团队共享" },
                    ]}
                />
                <Space>
                    <Tooltip title="刷新">
                        <Button
                            aria-label="刷新 Agent"
                            icon={<ReloadOutlined />}
                        />
                    </Tooltip>
                    <Input
                        value={searchText}
                        onChange={(event) => {
                            setSearchText(event.target.value);
                            setPage(1);
                        }}
                        placeholder="搜索 Agent"
                        prefix={<SearchOutlined />}
                        style={{ width: 200 }}
                    />
                </Space>
            </div>

            {activeTab === "mine" && (
                <div
                    style={{
                        height: 46,
                        display: "flex",
                        alignItems: "flex-start",
                    }}
                >
                    <Segmented<AgentStatusFilter>
                        block
                        value={statusFilter}
                        onChange={(value) => {
                            setStatusFilter(value);
                            setPage(1);
                        }}
                        options={[
                            {
                                value: "all",
                                label: `全部 ${scopedAgents.length}`,
                            },
                            {
                                value: "published",
                                label: `已发布 ${publishedCount}`,
                            },
                            { value: "draft", label: `草稿 ${draftCount}` },
                        ]}
                        style={{ width: 320 }}
                    />
                </div>
            )}

            {/* 每页固定为 5 列 × 4 行的画布。即使筛选后不足 20 项，也保留四行高度，
             * 让下方总数和分页控件的位置保持稳定；标题单行、描述两行省略。 */}
            <div
                style={{
                    minHeight: activeTab === "shared" ? 594 : 548,
                    position: "relative",
                }}
            >
                <Row gutter={[12, 12]} align="stretch">
                    {pageAgents.map((agent, index) => (
                        <Col
                            key={agent.name}
                            flex="0 0 20%"
                            style={{ maxWidth: "20%" }}
                        >
                            <Card
                                hoverable
                                className="inkwell-agent-card"
                                onClick={() => onSelectAgent(agent)}
                                style={{ height: 128, position: "relative" }}
                                styles={{
                                    body: {
                                        padding: 12,
                                        height: "100%",
                                        display: "flex",
                                        flexDirection: "column",
                                        overflow: "hidden",
                                    },
                                }}
                            >
                                {activeTab === "mine" && (
                                    <Space
                                        size={0}
                                        style={{
                                            position: "absolute",
                                            bottom: 6,
                                            right: 6,
                                            zIndex: 1,
                                        }}
                                    >
                                        <Tooltip title="编辑配置（跳 UI-004）">
                                            <Button
                                                className="inkwell-agent-card-edit-btn"
                                                type="text"
                                                size="small"
                                                aria-label={`编辑 ${agent.name}`}
                                                icon={<EditOutlined />}
                                                onClick={(event) => {
                                                    event.stopPropagation();
                                                    onEditAgent(agent.name);
                                                }}
                                            />
                                        </Tooltip>
                                        {agent.published &&
                                            !sharedAgentNames.has(
                                                agent.name,
                                            ) && (
                                                <Tooltip title="共享已发布版本">
                                                    <Button
                                                        className="inkwell-agent-card-edit-btn"
                                                        type="text"
                                                        size="small"
                                                        aria-label={`共享 ${agent.name}`}
                                                        icon={
                                                            <ShareAltOutlined />
                                                        }
                                                        onClick={(event) => {
                                                            event.stopPropagation();
                                                            shareAgent(
                                                                agent.name,
                                                                agent.version,
                                                            );
                                                        }}
                                                    />
                                                </Tooltip>
                                            )}
                                        {sharedAgentNames.has(agent.name) && (
                                            <Tooltip title="撤销共享">
                                                <Button
                                                    className="inkwell-agent-card-edit-btn"
                                                    danger
                                                    type="text"
                                                    size="small"
                                                    aria-label={`撤销 ${agent.name} 共享`}
                                                    icon={<UndoOutlined />}
                                                    onClick={(event) => {
                                                        event.stopPropagation();
                                                        unshareAgent(agent.name);
                                                    }}
                                                />
                                            </Tooltip>
                                        )}
                                    </Space>
                                )}
                                {activeTab === "shared" && (
                                    <Space
                                        size={0}
                                        style={{
                                            position: "absolute",
                                            bottom: 6,
                                            right: 6,
                                            zIndex: 1,
                                        }}
                                    >
                                        <Tooltip title="查看详情">
                                            <Button
                                                className="inkwell-agent-card-edit-btn"
                                                type="text"
                                                size="small"
                                                aria-label={`查看 ${agent.name} 详情`}
                                                icon={<EyeOutlined />}
                                                onClick={(event) => {
                                                    event.stopPropagation();
                                                    onViewAgent(agent.name);
                                                }}
                                            />
                                        </Tooltip>
                                        {isAdmin && (
                                            <Tooltip title="撤销共享">
                                                <Button
                                                    className="inkwell-agent-card-edit-btn"
                                                    danger
                                                    type="text"
                                                    size="small"
                                                    aria-label={`撤销 ${agent.name} 共享`}
                                                    icon={<UndoOutlined />}
                                                    onClick={(event) => {
                                                        event.stopPropagation();
                                                        modalApi.confirm({
                                                            title: `撤销「${agent.name}」的共享`,
                                                            content:
                                                                "撤销后，其他成员将无法继续访问；Owner 原件不会被删除。",
                                                            okText: "确认撤销",
                                                            okButtonProps: {
                                                                danger: true,
                                                            },
                                                            cancelText: "取消",
                                                        });
                                                    }}
                                                />
                                            </Tooltip>
                                        )}
                                    </Space>
                                )}
                                <div
                                    style={{
                                        width: "100%",
                                        minWidth: 0,
                                        display: "flex",
                                        alignItems: "flex-start",
                                        gap: 10,
                                    }}
                                >
                                    <AgentAvatar
                                        name={agent.name}
                                        color={palette[index % palette.length]}
                                    />
                                    <div style={{ minWidth: 0, flex: 1 }}>
                                        <div
                                            style={{
                                                width: "100%",
                                                minWidth: 0,
                                                display: "flex",
                                                alignItems: "center",
                                                gap: 6,
                                            }}
                                        >
                                            <Typography.Text
                                                strong
                                                ellipsis
                                                style={{
                                                    display: "block",
                                                    flex: 1,
                                                    minWidth: 0,
                                                    fontSize: 14,
                                                }}
                                            >
                                                {agent.name}
                                            </Typography.Text>
                                            {!agent.published && (
                                                <Tag
                                                    color="warning"
                                                    style={{
                                                        margin: 0,
                                                        fontSize: 10,
                                                        lineHeight: "16px",
                                                        padding: "0 4px",
                                                    }}
                                                >
                                                    草稿
                                                </Tag>
                                            )}
                                            {activeTab === "mine" &&
                                                sharedAgentNames.has(
                                                    agent.name,
                                                ) && (
                                                    <Tag
                                                        color="processing"
                                                        style={{
                                                            margin: 0,
                                                            fontSize: 10,
                                                            lineHeight: "16px",
                                                            padding: "0 4px",
                                                            flexShrink: 0,
                                                        }}
                                                    >
                                                        已共享
                                                    </Tag>
                                                )}
                                        </div>
                                        <Typography.Text
                                            type="secondary"
                                            ellipsis
                                            style={{
                                                display: "block",
                                                fontSize: 11,
                                            }}
                                        >
                                            {activeTab === "shared"
                                                ? `${agent.owner} · ${agent.version} · ${agent.updated}`
                                                : `${agent.version} · ${agent.updated}`}
                                        </Typography.Text>
                                    </div>
                                </div>
                                <Typography.Paragraph
                                    type="secondary"
                                    style={{
                                        fontSize: 12,
                                        marginTop: 6,
                                        marginBottom: 0,
                                        paddingRight:
                                            activeTab === "mine"
                                                ? agent.published
                                                    ? 52
                                                    : 26
                                                : activeTab === "shared"
                                                  ? isAdmin
                                                      ? 52
                                                      : 26
                                                  : 0,
                                    }}
                                    ellipsis={{ rows: 2 }}
                                >
                                    {agent.desc}
                                </Typography.Paragraph>
                            </Card>
                        </Col>
                    ))}
                </Row>
                {filteredAgents.length === 0 && (
                    <div
                        style={{
                            position: "absolute",
                            inset: 0,
                            display: "grid",
                            placeItems: "center",
                        }}
                    >
                        <Empty
                            image={Empty.PRESENTED_IMAGE_SIMPLE}
                            description="没有符合条件的 Agent"
                        />
                    </div>
                )}
            </div>
            {filteredAgents.length > 0 && (
                <div
                    style={{
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "space-between",
                        gap: 16,
                        marginTop: 16,
                    }}
                >
                    <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                        共 {filteredAgents.length} 个 Agent
                    </Typography.Text>
                    <Pagination
                        size="small"
                        current={page}
                        pageSize={AGENTS_PER_PAGE}
                        total={filteredAgents.length}
                        showSizeChanger={false}
                        onChange={setPage}
                    />
                </div>
            )}
        </div>
    );
}

// ─── Main Page ────────────────────────────────────────────────────────────────

export default function AppShellExplorer() {
    const { token } = antdTheme.useToken();
    const {
        themeName,
        appearanceMode,
        isDark,
        setThemeName,
        setAppearanceMode,
        setIsDark,
    } = useDesign();
    const [isAdmin, setIsAdmin] = useState(true);
    const [network, setNetwork] = useState<NetworkStatus>("online");
    const [errorScenario, setErrorScenario] = useState<ErrorScenario>("none");
    const [activeNav, setActiveNav] = useState<NavKey>("library");
    const [activeTab, setActiveTab] = useState("mine");
    const [selectedAgent, setSelectedAgent] = useState<string | null>(null);
    const [agentDetailReadonly, setAgentDetailReadonly] = useState(false);
    const [creatingAgent, setCreatingAgent] = useState(false);
    const [chattingAgent, setChattingAgent] = useState<string | null>(null);
    const [guideSection, setGuideSection] =
        useState<GuideSection>("quick-start");
    const [quickStartOpen, setQuickStartOpen] = useState(false);
    const [completedGuideSteps, setCompletedGuideSteps] = useState<Set<number>>(
        () => new Set(),
    );
    const [aboutOpen, setAboutOpen] = useState(false);
    const [settingsOpen, setSettingsOpen] = useState(false);
    const [changePasswordOpen, setChangePasswordOpen] = useState(false);
    const [changePasswordForm] = Form.useForm<ChangePasswordFormValues>();
    const [locked, setLocked] = useState(false);
    const [expandedGroups, setExpandedGroups] = useState<Set<string>>(
        () => new Set(NAV_GROUPS.map((group) => group.key)),
    );

    // 导航不提供收起/展开能力——之前进入 Agent 对话页/打开 Agent 编辑页的"开始对话"
    // 面板时会自动收起，结果用户反馈这会导致导航卡在收起状态展不开（因为这里也没有
    // 手动折叠按钮可以手动恢复）——现在彻底取消了自动收起，`collapsed` 固定为
    // `false`（不再是 useState），导航始终保持默认展开状态，不受子页面影响。
    const collapsed = false;
    const siderWidth = collapsed ? 64 : 200;
    const showAgentDetail =
        activeNav === "library" && (selectedAgent !== null || creatingAgent);
    const showAgentChat = activeNav === "library" && chattingAgent !== null;
    const visibleNavGroups = NAV_GROUPS.map((group) => ({
        ...group,
        items: group.items.filter((item) => !item.requiresAdmin || isAdmin),
    })).filter((group) => group.items.length > 0);

    const userMenuItems = [
        { key: "settings", icon: <SettingOutlined />, label: "个人设置" },
        { key: "change-password", icon: <KeyOutlined />, label: "修改密码" },
        { type: "divider" as const },
        // UI-002 锁定页（ui-spec.md §2）的模拟入口——真实产品里这个页面主要由 5
        // 分钟无操作/主窗口失焦自动触发（NFR-003），但很多真实产品也会在用户菜单里附带一个
        // 手动“锁定”入口，方便评审时不用真的等 5 分钟。
        { key: "lock", icon: <LockOutlined />, label: "锁定" },
        { key: "logout", icon: <LogoutOutlined />, label: "登出" },
    ];
    const helpMenuItems = [
        { key: "guide", icon: <BookOutlined />, label: "使用指南" },
        { key: "quick-start", icon: <RocketOutlined />, label: "快速开始" },
        { key: "faq", icon: <QuestionCircleOutlined />, label: "常见问题" },
        { type: "divider" as const },
        { key: "about", icon: <InfoCircleOutlined />, label: "关于 Inkwell" },
    ];

    const openGuide = (section: GuideSection) => {
        setGuideSection(section);
        setActiveNav("guide");
        setSelectedAgent(null);
        setCreatingAgent(false);
        setChattingAgent(null);
    };

    const changePassword = async () => {
        await changePasswordForm.validateFields();
        message.success("密码已修改");
        changePasswordForm.resetFields();
        setChangePasswordOpen(false);
    };

    return (
        <div
            style={{
                height: "calc(100vh - 56px)",
                display: "flex",
                flexDirection: "column",
                overflow: "hidden",
                background: token.colorBgLayout,
            }}
        >
            {/* Prototype controls */}
            <div
                style={{
                    minHeight: 44,
                    padding: "6px 28px",
                    background: token.colorFillQuaternary,
                    borderBottom: `1px solid ${token.colorBorderSecondary}`,
                    display: "flex",
                    gap: 12,
                    alignItems: "center",
                    flexWrap: "wrap",
                    flexShrink: 0,
                }}
            >
                <Typography.Text
                    type="secondary"
                    style={{ fontSize: 11, marginRight: 4 }}
                >
                    设计评审
                </Typography.Text>
                <Segmented
                    size="small"
                    value={network}
                    onChange={(v) => setNetwork(v as NetworkStatus)}
                    options={[
                        { value: "online", label: "后台服务正常" },
                        { value: "reconnecting", label: "重连中" },
                        { value: "offline", label: "后台服务异常" },
                    ]}
                />
                <Select
                    size="small"
                    value={errorScenario}
                    onChange={(v) => setErrorScenario(v as ErrorScenario)}
                    style={{ width: 170 }}
                    options={[
                        { value: "none", label: "无全局错误条" },
                        { value: "ex-001", label: "EX-001 网络断开" },
                        { value: "401", label: "401 鉴权失效" },
                        { value: "429", label: "429 速率超限" },
                        { value: "5xx", label: "5xx 后端错误" },
                    ]}
                />
                <Space size={6}>
                    <Switch
                        size="small"
                        checked={isAdmin}
                        onChange={(checked) => {
                            setIsAdmin(checked);
                            if (!checked && activeNav === "admin") {
                                setActiveNav("library");
                            }
                        }}
                    />
                    <Typography.Text style={{ fontSize: 12 }}>
                        is_super（显示管理能力）
                    </Typography.Text>
                </Space>
                <Tag
                    style={{
                        marginLeft: "auto",
                        marginRight: 0,
                        fontSize: 11,
                        color: token.colorPrimaryText,
                        background: token.colorPrimaryBg,
                        borderColor: "transparent",
                    }}
                >
                    OQ-011 · 顶栏 + 左侧 nav + 主区
                </Tag>
            </div>

            {/* Simulated AppShell */}
            <div
                style={{
                    flex: 1,
                    minHeight: 0,
                    display: "flex",
                    flexDirection: "column",
                    overflow: "hidden",
                    position: "relative",
                }}
            >
                {/* UI-002 锁定页：绝对定位覆盖住下面整个模拟 AppShell（顶栏 + nav + 主区），
                 * 但不盖住上面这条"设计评审"控制条本身——那条是评审工具的元信息，不是产品的
                 * 一部分。"离线"态直接复用上面已经有的后台连通性模拟器（network），不用再加
                 * 一个新的、跟它含义重复的控制。 */}
                {locked && (
                    <LockScreen
                        username="alice"
                        offline={network !== "online"}
                        onUnlock={() => setLocked(false)}
                        onSwitchAccount={() => {
                            setLocked(false);
                            message.info(
                                "已终止当前会话，模拟跳转到登录页（切换账号）",
                            );
                        }}
                        onLogout={() => {
                            setLocked(false);
                            message.info("已登出，模拟跳转到登录页");
                        }}
                    />
                )}

                {errorScenario !== "none" && (
                    <Alert
                        banner
                        showIcon
                        closable={false}
                        className="inkwell-compact-alert"
                        type={ERROR_BANNER_META[errorScenario].type}
                        title={
                            <span style={{ fontSize: 13 }}>
                                {ERROR_BANNER_META[errorScenario].message}
                            </span>
                        }
                        style={{
                            borderRadius: 0,
                            flexShrink: 0,
                            padding: "7px 20px",
                        }}
                    />
                )}

                {/* Header · 56px */}
                <div
                    style={{
                        height: 56,
                        flexShrink: 0,
                        display: "flex",
                        alignItems: "center",
                        padding: "0 20px",
                        background: token.colorBgContainer,
                        borderBottom: `1px solid ${token.colorBorderSecondary}`,
                        gap: 12,
                    }}
                >
                    <img src={logo} width={26} height={26} alt="Inkwell" />
                    <Typography.Text
                        strong
                        style={{ fontSize: 15, color: token.colorPrimary }}
                    >
                        Inkwell
                    </Typography.Text>
                    <button
                        type="button"
                        aria-label="关于 Inkwell"
                        onClick={() => setAboutOpen(true)}
                        className="inkwell-breathe-icon"
                        style={{
                            width: 10,
                            height: 10,
                            borderRadius: "50%",
                            background: token.colorPrimary,
                            color: token.colorPrimary,
                            border: "none",
                            padding: 0,
                            cursor: "pointer",
                            flexShrink: 0,
                        }}
                    />

                    <div
                        style={{
                            marginLeft: "auto",
                            display: "flex",
                            alignItems: "center",
                            gap: 16,
                        }}
                    >
                        <Switch
                            size="small"
                            aria-label="切换外观"
                            checked={isDark}
                            onChange={setIsDark}
                            checkedChildren={<MoonFilled />}
                            unCheckedChildren={<SunFilled />}
                        />
                        <Space size={6}>
                            <span
                                style={{
                                    width: 8,
                                    height: 8,
                                    borderRadius: "50%",
                                    background: NETWORK_META[network].color,
                                    display: "inline-block",
                                }}
                            />
                            <Typography.Text
                                type="secondary"
                                style={{ fontSize: 12 }}
                            >
                                {NETWORK_META[network].label}
                            </Typography.Text>
                        </Space>
                        <Dropdown
                            menu={{
                                items: helpMenuItems,
                                onClick: ({ key }) => {
                                    if (key === "guide")
                                        openGuide("quick-start");
                                    if (key === "quick-start")
                                        setQuickStartOpen(true);
                                    if (key === "faq") openGuide("faq");
                                    if (key === "about") setAboutOpen(true);
                                },
                            }}
                            trigger={["click"]}
                            placement="bottomRight"
                        >
                            <Tooltip title="帮助">
                                <Button
                                    type="text"
                                    aria-label="帮助"
                                    icon={<QuestionCircleOutlined />}
                                />
                            </Tooltip>
                        </Dropdown>
                        <div
                            style={{
                                width: 1,
                                height: 20,
                                background: token.colorBorderSecondary,
                            }}
                        />
                        <Dropdown
                            menu={{
                                items: userMenuItems,
                                onClick: ({ key }) => {
                                    if (key === "settings")
                                        setSettingsOpen(true);
                                    if (key === "change-password")
                                        setChangePasswordOpen(true);
                                    if (key === "lock") setLocked(true);
                                },
                            }}
                            trigger={["click"]}
                        >
                            <Space style={{ cursor: "pointer" }} size={8}>
                                <Avatar
                                    size={28}
                                    icon={<UserOutlined />}
                                    style={{ background: token.colorPrimary }}
                                />
                                <Typography.Text style={{ fontSize: 13 }}>
                                    alice
                                </Typography.Text>
                                <DownOutlined
                                    style={{
                                        fontSize: 10,
                                        color: token.colorTextTertiary,
                                    }}
                                />
                            </Space>
                        </Dropdown>
                    </div>
                </div>

                {/* Body: Sider + Content */}
                <div
                    style={{
                        flex: 1,
                        minHeight: 0,
                        display: "flex",
                        overflow: "hidden",
                    }}
                >
                    {/* Sider */}
                    <div
                        style={{
                            width: siderWidth,
                            flexShrink: 0,
                            background: token.colorBgContainer,
                            borderRight: `1px solid ${token.colorBorderSecondary}`,
                            display: "flex",
                            flexDirection: "column",
                            transition: "width 0.2s",
                        }}
                    >
                        <div
                            style={{
                                flex: 1,
                                overflow: "auto",
                                padding: collapsed ? "12px 8px" : "12px",
                            }}
                        >
                            {visibleNavGroups.map((group) => {
                                const isExpanded =
                                    collapsed || expandedGroups.has(group.key);
                                return (
                                    <div
                                        key={group.key}
                                        style={{ marginBottom: 8 }}
                                    >
                                        {!collapsed && (
                                            <div
                                                onClick={() =>
                                                    setExpandedGroups(
                                                        (previous) => {
                                                            const next =
                                                                new Set(
                                                                    previous,
                                                                );
                                                            if (
                                                                next.has(
                                                                    group.key,
                                                                )
                                                            ) {
                                                                next.delete(
                                                                    group.key,
                                                                );
                                                            } else {
                                                                next.add(
                                                                    group.key,
                                                                );
                                                            }
                                                            return next;
                                                        },
                                                    )
                                                }
                                                style={{
                                                    display: "flex",
                                                    alignItems: "center",
                                                    justifyContent:
                                                        "space-between",
                                                    padding: "4px 12px",
                                                    cursor: "pointer",
                                                    userSelect: "none",
                                                }}
                                            >
                                                <Typography.Text
                                                    type="secondary"
                                                    style={{
                                                        fontSize: 11,
                                                        fontWeight: 600,
                                                    }}
                                                >
                                                    {group.label}
                                                </Typography.Text>
                                                <RightOutlined
                                                    style={{
                                                        fontSize: 9,
                                                        color: token.colorTextTertiary,
                                                        transition:
                                                            "transform 0.15s",
                                                        transform: isExpanded
                                                            ? "rotate(90deg)"
                                                            : "rotate(0deg)",
                                                    }}
                                                />
                                            </div>
                                        )}
                                        {isExpanded &&
                                            group.items.map((item) => {
                                                const active =
                                                    activeNav === item.key;
                                                return (
                                                    <Tooltip
                                                        key={item.key}
                                                        title=""
                                                        placement="right"
                                                    >
                                                        <div
                                                            onClick={() => {
                                                                setActiveNav(
                                                                    item.key,
                                                                );
                                                                setSelectedAgent(
                                                                    null,
                                                                );
                                                                setCreatingAgent(
                                                                    false,
                                                                );
                                                                setChattingAgent(
                                                                    null,
                                                                );
                                                            }}
                                                            style={{
                                                                display: "flex",
                                                                alignItems:
                                                                    "center",
                                                                gap: 10,
                                                                padding:
                                                                    collapsed
                                                                        ? "10px 0"
                                                                        : "7px 12px",
                                                                justifyContent:
                                                                    collapsed
                                                                        ? "center"
                                                                        : "flex-start",
                                                                borderRadius:
                                                                    token.borderRadius,
                                                                marginBottom: 3,
                                                                cursor: "pointer",
                                                                color: active
                                                                    ? token.colorPrimary
                                                                    : token.colorText,
                                                                background:
                                                                    active
                                                                        ? token.colorPrimaryBg
                                                                        : "transparent",
                                                                fontWeight:
                                                                    active
                                                                        ? 600
                                                                        : 400,
                                                                fontSize: 13,
                                                            }}
                                                        >
                                                            {item.icon}
                                                            {!collapsed && (
                                                                <span
                                                                    style={{
                                                                        flex: 1,
                                                                        minWidth: 0,
                                                                        overflow:
                                                                            "hidden",
                                                                        textOverflow:
                                                                            "ellipsis",
                                                                        whiteSpace:
                                                                            "nowrap",
                                                                    }}
                                                                >
                                                                    {item.label}
                                                                </span>
                                                            )}
                                                        </div>
                                                    </Tooltip>
                                                );
                                            })}
                                    </div>
                                );
                            })}
                        </div>
                    </div>

                    {/* Content */}
                    <div
                        style={{
                            flex: 1,
                            minHeight: 0,
                            overflow:
                                showAgentDetail || showAgentChat || activeNav === "guide"
                                    ? "hidden"
                                    : "auto",
                            padding:
                                showAgentDetail || showAgentChat || activeNav === "guide"
                                    ? 0
                                    : 20,
                        }}
                    >
                        {activeNav === "library" ? (
                            showAgentChat ? (
                                <AgentChatPage
                                    agentName={chattingAgent as string}
                                    onBack={() => {
                                        setChattingAgent(null);
                                    }}
                                />
                            ) : showAgentDetail ? (
                                <AgentDesignPage
                                    initialState={
                                        creatingAgent
                                            ? "new-draft"
                                            : agentDetailReadonly
                                              ? "readonly"
                                              : "editing"
                                    }
                                    onBack={() => {
                                        setSelectedAgent(null);
                                        setAgentDetailReadonly(false);
                                        setCreatingAgent(false);
                                    }}
                                />
                            ) : (
                                <AgentLibraryMock
                                    activeTab={activeTab}
                                    onTabChange={setActiveTab}
                                    onSelectAgent={(agent) => {
                                        if (agent.published) {
                                            setChattingAgent(agent.name);
                                        } else {
                                            setSelectedAgent(agent.name);
                                        }
                                    }}
                                    onCreateAgent={() => {
                                        setAgentDetailReadonly(false);
                                        setCreatingAgent(true);
                                    }}
                                    onEditAgent={(name) => {
                                        setAgentDetailReadonly(false);
                                        setSelectedAgent(name);
                                    }}
                                    onViewAgent={(name) => {
                                        setAgentDetailReadonly(true);
                                        setSelectedAgent(name);
                                    }}
                                    isAdmin={isAdmin}
                                />
                            )
                        ) : activeNav === "tools" ? (
                            <ToolListPage />
                        ) : activeNav === "skills" ? (
                            <SkillListPage isAdmin={isAdmin} />
                        ) : activeNav === "models" ? (
                            <ModelListPage />
                        ) : activeNav === "admin" ? (
                            <UserManagementPage />
                        ) : (
                            <UserGuidePage
                                key={guideSection}
                                initialSection={guideSection}
                                onStartQuickGuide={() =>
                                    setQuickStartOpen(true)
                                }
                                onGoToAgentSpace={() =>
                                    setActiveNav("library")
                                }
                            />
                        )}
                    </div>
                </div>
            </div>

            <Drawer
                open={quickStartOpen}
                onClose={() => setQuickStartOpen(false)}
                title="快速开始"
                size={400}
                extra={
                    <Typography.Text type="secondary">
                        {completedGuideSteps.size} / 5
                    </Typography.Text>
                }
            >
                <Typography.Paragraph type="secondary">
                    完成这些关键步骤，建立从配置到团队使用的完整工作流。
                </Typography.Paragraph>
                <Progress
                    percent={completedGuideSteps.size * 20}
                    showInfo={false}
                    style={{ marginBottom: 18 }}
                />
                <Space orientation="vertical" size={4} style={{ width: "100%" }}>
                    {[
                        ["创建一个 Agent", "填写名称和用途"],
                        ["完成核心配置", "补充 Instructions 并选择模型"],
                        ["进行一次试运行", "用真实问题检查回答"],
                        ["发布第一个版本", "生成可用于对话的正式版本"],
                        ["按需共享给团队", "允许成员只读查看和使用"],
                    ].map(([title, description], index) => (
                        <label
                            key={title}
                            style={{
                                display: "grid",
                                gridTemplateColumns: "24px minmax(0, 1fr)",
                                gap: 10,
                                padding: "12px 4px",
                                borderBottom: `1px solid ${token.colorBorderSecondary}`,
                                cursor: "pointer",
                            }}
                        >
                            <Checkbox
                                checked={completedGuideSteps.has(index)}
                                onChange={(event) => {
                                    setCompletedGuideSteps((current) => {
                                        const next = new Set(current);
                                        if (event.target.checked) next.add(index);
                                        else next.delete(index);
                                        return next;
                                    });
                                }}
                            />
                            <span>
                                <Typography.Text strong>{title}</Typography.Text>
                                <Typography.Text
                                    type="secondary"
                                    style={{ display: "block", fontSize: 12 }}
                                >
                                    {description}
                                </Typography.Text>
                            </span>
                        </label>
                    ))}
                </Space>
                <Button
                    type="primary"
                    block
                    icon={<RocketOutlined />}
                    style={{ marginTop: 22 }}
                    onClick={() => {
                        setQuickStartOpen(false);
                        setActiveNav("library");
                    }}
                >
                    前往 Agent 空间
                </Button>
            </Drawer>

            {/* 关于弹层：版本信息从 Header 移入此处，附作者 / GitHub / 公众号占位 */}
            <Modal
                open={aboutOpen}
                onCancel={() => setAboutOpen(false)}
                footer={null}
                centered
                width={420}
                title={null}
            >
                <div style={{ textAlign: "center", padding: "8px 0 4px" }}>
                    <img src={logo} width={48} height={48} alt="Inkwell" />
                    <Typography.Title
                        level={5}
                        style={{ margin: "12px 0 2px" }}
                    >
                        Inkwell
                    </Typography.Title>
                </div>
                <Divider style={{ margin: "16px 0" }} />
                <Space
                    orientation="vertical"
                    size={6}
                    style={{ width: "100%", fontSize: 12 }}
                >
                    {[
                        { label: "版本", value: "1.0.0" },
                        { label: "构建号", value: "20260714.1" },
                        { label: "提交", value: "a1b2c3d" },
                        { label: "作者", value: "ShuaiHua Du" },
                        {
                            label: "GitHub",
                            value: "shuaihuadu/inkwell",
                            href: "https://github.com/shuaihuadu/inkwell",
                        },
                    ].map((row) => (
                        <div
                            key={row.label}
                            style={{
                                display: "flex",
                                justifyContent: "space-between",
                            }}
                        >
                            <Typography.Text
                                type="secondary"
                                style={{ fontSize: 12 }}
                            >
                                {row.label}
                            </Typography.Text>
                            {row.href ? (
                                <Typography.Link
                                    href={row.href}
                                    target="_blank"
                                    rel="noreferrer"
                                    style={{ fontSize: 12 }}
                                >
                                    <GithubOutlined
                                        style={{ marginRight: 4 }}
                                    />
                                    {row.value}
                                </Typography.Link>
                            ) : (
                                <Typography.Text style={{ fontSize: 12 }}>
                                    {row.value}
                                </Typography.Text>
                            )}
                        </div>
                    ))}
                </Space>
                <Divider style={{ margin: "16px 0" }} />
                <div style={{ textAlign: "center", padding: "4px 0 8px" }}>
                    <img
                        src={qrCode}
                        alt="公众号二维码"
                        width={200}
                        height={200}
                        style={{
                            borderRadius: 12,
                            border: `1px solid ${token.colorBorderSecondary}`,
                            objectFit: "cover",
                            display: "inline-block",
                        }}
                    />
                    <Typography.Text
                        type="secondary"
                        style={{ fontSize: 12, display: "block", marginTop: 8 }}
                    >
                        扫码关注作者公众号
                    </Typography.Text>
                </div>
            </Modal>

            {/* 个人设置弹层：仅管理外观 */}
            <Modal
                open={settingsOpen}
                onCancel={() => setSettingsOpen(false)}
                footer={null}
                centered
                width={440}
                title="个人设置"
            >
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                    外观模式
                </Typography.Text>
                <div style={{ marginTop: 8, marginBottom: 20 }}>
                    <Segmented
                        block
                        value={appearanceMode}
                        onChange={(value) =>
                            setAppearanceMode(value as AppearanceMode)
                        }
                        options={[
                            {
                                value: "light",
                                label: (
                                    <Space size={4}>
                                        <BulbOutlined />
                                        亮色
                                    </Space>
                                ),
                            },
                            {
                                value: "dark",
                                label: (
                                    <Space size={4}>
                                        <BulbFilled />
                                        暗色
                                    </Space>
                                ),
                            },
                            {
                                value: "system",
                                label: (
                                    <Space size={4}>
                                        <DesktopOutlined />
                                        跟随系统
                                    </Space>
                                ),
                            },
                        ]}
                    />
                </div>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                    主题色
                </Typography.Text>
                <div style={{ marginTop: 8 }}>
                    <Segmented
                        block
                        value={themeName}
                        onChange={(value) =>
                            setThemeName(value as typeof themeName)
                        }
                        options={THEME_NAMES.map((name) => ({
                            value: name,
                            label: (
                                <Space size={4}>
                                    <span
                                        style={{
                                            display: "inline-block",
                                            width: 8,
                                            height: 8,
                                            borderRadius: "50%",
                                            background:
                                                THEMES[name].primaryColor,
                                            flexShrink: 0,
                                        }}
                                    />
                                    {THEMES[name].label}
                                </Space>
                            ),
                        }))}
                    />
                </div>
            </Modal>

            <Modal
                open={changePasswordOpen}
                onCancel={() => {
                    setChangePasswordOpen(false);
                    changePasswordForm.resetFields();
                }}
                footer={null}
                centered
                width={440}
                title="修改密码"
            >
                <Form<ChangePasswordFormValues>
                    form={changePasswordForm}
                    layout="vertical"
                    onFinish={() => void changePassword()}
                    requiredMark="optional"
                >
                    <Form.Item
                        name="currentPassword"
                        label="当前密码"
                        rules={[{ required: true, message: "请输入当前密码" }]}
                    >
                        <Input.Password
                            autoComplete="current-password"
                            placeholder="输入当前密码"
                        />
                    </Form.Item>
                    <Form.Item
                        name="newPassword"
                        label="新密码"
                        extra="使用 8–128 个字符，且不能与当前密码相同。"
                        dependencies={["currentPassword"]}
                        rules={[
                            { required: true, message: "请输入新密码" },
                            {
                                min: 8,
                                max: 128,
                                message: "密码长度应为 8–128 个字符",
                            },
                            ({ getFieldValue }) => ({
                                validator: (_, value: string | undefined) =>
                                    value &&
                                    value === getFieldValue("currentPassword")
                                        ? Promise.reject(
                                              new Error(
                                                  "新密码不能与当前密码相同",
                                              ),
                                          )
                                        : Promise.resolve(),
                            }),
                        ]}
                    >
                        <Input.Password
                            autoComplete="new-password"
                            placeholder="输入新密码"
                        />
                    </Form.Item>
                    <Form.Item
                        name="confirmPassword"
                        label="确认新密码"
                        dependencies={["newPassword"]}
                        rules={[
                            { required: true, message: "请再次输入新密码" },
                            ({ getFieldValue }) => ({
                                validator: (_, value: string | undefined) =>
                                    value &&
                                    value !== getFieldValue("newPassword")
                                        ? Promise.reject(
                                              new Error(
                                                  "两次输入的新密码不一致",
                                              ),
                                          )
                                        : Promise.resolve(),
                            }),
                        ]}
                    >
                        <Input.Password
                            autoComplete="new-password"
                            placeholder="再次输入新密码"
                        />
                    </Form.Item>
                    <div
                        style={{ display: "flex", justifyContent: "flex-end" }}
                    >
                        <Button type="primary" htmlType="submit">
                            修改密码
                        </Button>
                    </div>
                </Form>
            </Modal>
        </div>
    );
}
