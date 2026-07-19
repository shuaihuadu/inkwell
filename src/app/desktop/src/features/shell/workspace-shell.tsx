import {
    ApiOutlined,
    AppstoreOutlined,
    BookOutlined,
    BulbFilled,
    BulbOutlined,
    DesktopOutlined,
    DownOutlined,
    GithubOutlined,
    InfoCircleOutlined,
    KeyOutlined,
    LogoutOutlined,
    MoonFilled,
    QuestionCircleOutlined,
    ReadOutlined,
    RightOutlined,
    RocketOutlined,
    SafetyCertificateOutlined,
    SettingOutlined,
    SunFilled,
    ToolOutlined,
} from "@ant-design/icons";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
    Avatar,
    Button,
    Checkbox,
    Divider,
    Dropdown,
    Drawer,
    Empty,
    Modal,
    Progress,
    Segmented,
    Space,
    Switch,
    Tag,
    Tooltip,
    Typography,
} from "antd";
import type { ReactNode } from "react";
import { useState } from "react";
import { desktopApi } from "../../shared/network/desktop-api";
import { useAuthStore } from "../auth/auth-store";
import { ChangePasswordModal } from "../auth/change-password-modal";
import { UserGuide, type GuideSection } from "../help/user-guide";
import { ModelManagement } from "../models/model-management";
import { SkillManagement } from "../skills/skill-management";
import { ToolManagement } from "../tools/tool-management";
import { UserManagement } from "../users/user-management";
import {
    type AppearanceMode,
    useAppearanceStore,
    useResolvedAppearance,
} from "./appearance-store";
import { desktopThemes, themeNames, type ThemeName } from "./themes";

type NavigationKey =
    | "agents"
    | "tools"
    | "skills"
    | "models"
    | "admin"
    | "guide";

interface NavigationItem {
    key: NavigationKey;
    label: string;
    icon: ReactNode;
    requiresAdmin?: boolean;
    placeholder?: boolean;
}

interface NavigationGroup {
    key: string;
    label: string;
    items: NavigationItem[];
}

const navigationGroups: NavigationGroup[] = [
    {
        key: "workspace",
        label: "工作区",
        items: [
            { key: "agents", label: "Agent 空间", icon: <AppstoreOutlined /> },
        ],
    },
    {
        key: "resources",
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

interface WorkspaceShellProps {
    children: ReactNode;
    onNavigate: (navigate: () => void) => void;
}

export function WorkspaceShell({ children, onNavigate }: WorkspaceShellProps) {
    const identity = useAuthStore((state) => state.identity);
    const setSnapshot = useAuthStore((state) => state.setSnapshot);
    const appearanceMode = useAppearanceStore((state) => state.mode);
    const setAppearanceMode = useAppearanceStore((state) => state.setMode);
    const themeName = useAppearanceStore((state) => state.themeName);
    const setThemeName = useAppearanceStore((state) => state.setThemeName);
    const resolvedAppearance = useResolvedAppearance();
    const queryClient = useQueryClient();
    const [activeNavigation, setActiveNavigation] =
        useState<NavigationKey>("agents");
    const [aboutOpen, setAboutOpen] = useState(false);
    const [settingsOpen, setSettingsOpen] = useState(false);
    const [changePasswordOpen, setChangePasswordOpen] = useState(false);
    const [quickStartOpen, setQuickStartOpen] = useState(false);
    const [guideSection, setGuideSection] =
        useState<GuideSection>("quick-start");
    const [completedGuideSteps, setCompletedGuideSteps] = useState<Set<number>>(
        () => new Set(),
    );
    const [expandedGroups, setExpandedGroups] = useState<Set<string>>(
        () => new Set(navigationGroups.map((group) => group.key)),
    );
    const visibleGroups = navigationGroups
        .map((group) => ({
            ...group,
            items: group.items.filter(
                (item) => !item.requiresAdmin || identity?.isAdmin,
            ),
        }))
        .filter((group) => group.items.length > 0);
    const activeItem = visibleGroups
        .flatMap((group) => group.items)
        .find((item) => item.key === activeNavigation);
    const metadataQuery = useQuery({
        queryKey: ["app-metadata"],
        queryFn: desktopApi.getAppMetadata,
        staleTime: Number.POSITIVE_INFINITY,
    });

    const toggleGroup = (groupKey: string): void => {
        setExpandedGroups((current) => {
            const next = new Set(current);
            if (next.has(groupKey)) next.delete(groupKey);
            else next.add(groupKey);
            return next;
        });
    };

    const navigateTo = (navigation: NavigationKey): void => {
        onNavigate(() => setActiveNavigation(navigation));
    };

    const openGuide = (section: GuideSection): void => {
        setGuideSection(section);
        navigateTo("guide");
    };

    const logout = async (): Promise<void> => {
        await desktopApi.logout();
        queryClient.clear();
        setSnapshot({ status: "anonymous", identity: null });
    };

    return (
        <div className="workspace-shell">
            <header className="app-header">
                <div className="app-identity">
                    <img
                        src="./logo.svg"
                        alt="Inkwell"
                        width="28"
                        height="28"
                    />
                    <strong>Inkwell</strong>
                    <button
                        type="button"
                        className="about-trigger"
                        aria-label="关于 Inkwell"
                        onClick={() => setAboutOpen(true)}
                    />
                </div>
                <div className="app-header-actions">
                    <Switch
                        size="small"
                        aria-label="切换外观"
                        checked={resolvedAppearance === "dark"}
                        checkedChildren={<MoonFilled />}
                        unCheckedChildren={<SunFilled />}
                        onChange={(checked) =>
                            setAppearanceMode(checked ? "dark" : "light")
                        }
                    />
                    <div className="connection-state" aria-label="后台服务正常">
                        <span /> 后台服务正常
                    </div>
                    <Dropdown
                        trigger={["click"]}
                        placement="bottomRight"
                        menu={{
                            items: [
                                {
                                    key: "guide",
                                    icon: <BookOutlined />,
                                    label: "使用指南",
                                },
                                {
                                    key: "quick-start",
                                    icon: <RocketOutlined />,
                                    label: "快速开始",
                                },
                                {
                                    key: "faq",
                                    icon: <QuestionCircleOutlined />,
                                    label: "常见问题",
                                },
                                { type: "divider" },
                                {
                                    key: "about",
                                    icon: <InfoCircleOutlined />,
                                    label: "关于 Inkwell",
                                },
                            ],
                            onClick: ({ key }) => {
                                if (key === "guide") openGuide("quick-start");
                                if (key === "quick-start")
                                    setQuickStartOpen(true);
                                if (key === "faq") openGuide("faq");
                                if (key === "about") setAboutOpen(true);
                            },
                        }}
                    >
                        <Tooltip title="帮助">
                            <Button
                                type="text"
                                aria-label="帮助"
                                icon={<QuestionCircleOutlined />}
                            />
                        </Tooltip>
                    </Dropdown>
                    <div className="header-divider" />
                    <Dropdown
                        trigger={["click"]}
                        menu={{
                            items: [
                                {
                                    key: "settings",
                                    icon: <SettingOutlined />,
                                    label: "个人设置",
                                },
                                {
                                    key: "change-password",
                                    icon: <KeyOutlined />,
                                    label: "修改密码",
                                },
                                ...(identity?.isAdmin
                                    ? [
                                          {
                                              key: "admin",
                                              icon: (
                                                  <SafetyCertificateOutlined />
                                              ),
                                              label: "管理",
                                          },
                                      ]
                                    : []),
                                { type: "divider" as const },
                                {
                                    key: "logout",
                                    icon: <LogoutOutlined />,
                                    label: "登出",
                                },
                            ],
                            onClick: ({ key }) => {
                                if (key === "settings") setSettingsOpen(true);
                                if (key === "change-password")
                                    setChangePasswordOpen(true);
                                if (key === "admin") navigateTo("admin");
                                if (key === "logout")
                                    onNavigate(() => void logout());
                            },
                        }}
                    >
                        <button
                            type="button"
                            className="user-menu-trigger"
                            aria-label="打开用户菜单"
                        >
                            <Avatar size={28}>
                                {identity?.username.slice(0, 1).toUpperCase()}
                            </Avatar>
                            <Typography.Text>
                                {identity?.username}
                            </Typography.Text>
                            <DownOutlined />
                        </button>
                    </Dropdown>
                </div>
            </header>

            <div className="workspace-body">
                <aside className="app-sidebar" aria-label="主导航">
                    {visibleGroups.map((group) => {
                        const expanded = expandedGroups.has(group.key);
                        return (
                            <section className="nav-group" key={group.key}>
                                <button
                                    type="button"
                                    className="nav-group-trigger"
                                    aria-expanded={expanded}
                                    onClick={() => toggleGroup(group.key)}
                                >
                                    <span>{group.label}</span>
                                    <RightOutlined
                                        className={expanded ? "expanded" : ""}
                                    />
                                </button>
                                {expanded &&
                                    group.items.map((item) => (
                                        <Tooltip
                                            key={item.key}
                                            title={
                                                item.placeholder
                                                    ? "占位入口 · 即将上线"
                                                    : undefined
                                            }
                                            placement="right"
                                        >
                                            <button
                                                type="button"
                                                className={`nav-item${activeNavigation === item.key ? " active" : ""}`}
                                                onClick={() =>
                                                    navigateTo(item.key)
                                                }
                                            >
                                                {item.icon}
                                                <span>{item.label}</span>
                                                {item.placeholder && (
                                                    <Tag bordered={false}>
                                                        待上线
                                                    </Tag>
                                                )}
                                            </button>
                                        </Tooltip>
                                    ))}
                            </section>
                        );
                    })}
                </aside>

                <div className="workspace-content">
                    <div
                        className="workspace-route"
                        hidden={activeNavigation !== "skills"}
                    >
                        <SkillManagement />
                    </div>
                    <div
                        className="workspace-route"
                        hidden={activeNavigation !== "agents"}
                    >
                        {children}
                    </div>
                    <div
                        className="workspace-route"
                        hidden={activeNavigation !== "tools"}
                    >
                        <ToolManagement />
                    </div>
                    <div
                        className="workspace-route"
                        hidden={activeNavigation !== "models"}
                    >
                        <ModelManagement />
                    </div>
                    <div
                        className="workspace-route"
                        hidden={activeNavigation !== "admin"}
                    >
                        {identity?.isAdmin && <UserManagement />}
                    </div>
                    <div
                        className="workspace-route"
                        hidden={activeNavigation !== "guide"}
                    >
                        <UserGuide
                            key={guideSection}
                            initialSection={guideSection}
                            onStartQuickGuide={() => setQuickStartOpen(true)}
                            onGoToAgentSpace={() => navigateTo("agents")}
                        />
                    </div>
                    {activeNavigation !== "agents" &&
                        activeNavigation !== "tools" &&
                        activeNavigation !== "skills" &&
                        activeNavigation !== "models" &&
                        activeNavigation !== "admin" &&
                        activeNavigation !== "guide" && (
                            <main className="placeholder-page">
                                <Empty
                                    image={Empty.PRESENTED_IMAGE_SIMPLE}
                                    description={
                                        <div className="placeholder-copy">
                                            <Typography.Title level={4}>
                                                {activeItem?.label}
                                            </Typography.Title>
                                            <Typography.Text type="secondary">
                                                即将上线
                                            </Typography.Text>
                                        </div>
                                    }
                                />
                            </main>
                        )}
                </div>
            </div>

            <Drawer
                open={quickStartOpen}
                onClose={() => setQuickStartOpen(false)}
                destroyOnHidden
                title="快速开始"
                width={400}
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
                    className="quick-start-progress"
                />
                <Space
                    orientation="vertical"
                    size={4}
                    className="quick-start-list"
                >
                    {[
                        ["创建一个 Agent", "填写名称和用途"],
                        ["完成核心配置", "补充 Instructions 并选择模型"],
                        ["进行一次试运行", "用真实问题检查回答"],
                        ["发布第一个版本", "生成可用于对话的正式版本"],
                        ["按需共享给团队", "允许成员只读查看和使用"],
                    ].map(([title, description], index) => (
                        <label className="quick-start-item" key={title}>
                            <Checkbox
                                checked={completedGuideSteps.has(index)}
                                onChange={(event) => {
                                    setCompletedGuideSteps((current) => {
                                        const next = new Set(current);
                                        if (event.target.checked)
                                            next.add(index);
                                        else next.delete(index);
                                        return next;
                                    });
                                }}
                            />
                            <span>
                                <Typography.Text strong>
                                    {title}
                                </Typography.Text>
                                <Typography.Text type="secondary">
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
                    className="quick-start-agent-button"
                    onClick={() => {
                        setQuickStartOpen(false);
                        navigateTo("agents");
                    }}
                >
                    前往 Agent 空间
                </Button>
            </Drawer>

            <Modal
                open={aboutOpen}
                onCancel={() => setAboutOpen(false)}
                footer={null}
                centered
                width={420}
                title={null}
            >
                <div className="about-heading">
                    <img
                        src="./logo.svg"
                        width="48"
                        height="48"
                        alt="Inkwell"
                    />
                    <Typography.Title level={5}>Inkwell</Typography.Title>
                </div>
                <Divider />
                <div className="about-details">
                    <div>
                        <Typography.Text type="secondary">版本</Typography.Text>
                        <Typography.Text data-testid="app-version">
                            {metadataQuery.data?.version ?? "-"}
                        </Typography.Text>
                    </div>
                    <div>
                        <Typography.Text type="secondary">
                            构建号
                        </Typography.Text>
                        <Typography.Text>
                            {metadataQuery.data?.buildNumber ?? "未提供"}
                        </Typography.Text>
                    </div>
                    <div>
                        <Typography.Text type="secondary">提交</Typography.Text>
                        <Typography.Text>
                            {metadataQuery.data?.commit?.slice(0, 12) ??
                                "未提供"}
                        </Typography.Text>
                    </div>
                    <div>
                        <Typography.Text type="secondary">
                            GitHub
                        </Typography.Text>
                        <Typography.Link
                            href="https://github.com/shuaihuadu/inkwell"
                            target="_blank"
                            rel="noreferrer"
                        >
                            <GithubOutlined /> shuaihuadu/inkwell
                        </Typography.Link>
                    </div>
                </div>
                <Divider />
                <div className="about-qr-code">
                    <img
                        src="./quanzhange.jpg"
                        width="200"
                        height="200"
                        alt="公众号二维码"
                    />
                    <Typography.Text type="secondary">
                        扫码关注作者公众号
                    </Typography.Text>
                </div>
            </Modal>

            <Modal
                open={settingsOpen}
                onCancel={() => setSettingsOpen(false)}
                footer={null}
                centered
                width={440}
                title="个人设置"
            >
                <Typography.Text type="secondary">外观模式</Typography.Text>
                <Segmented
                    block
                    className="appearance-options"
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
                <Typography.Text type="secondary" className="theme-label">
                    主题色
                </Typography.Text>
                <Segmented
                    block
                    className="theme-options"
                    value={themeName}
                    onChange={(value) => setThemeName(value as ThemeName)}
                    options={themeNames.map((name) => ({
                        value: name,
                        label: (
                            <Space size={4}>
                                <span
                                    className="theme-swatch"
                                    style={{
                                        background:
                                            desktopThemes[name].primaryColor,
                                    }}
                                />
                                {desktopThemes[name].label}
                            </Space>
                        ),
                    }))}
                />
            </Modal>
            <ChangePasswordModal
                open={changePasswordOpen}
                onClose={() => setChangePasswordOpen(false)}
            />
        </div>
    );
}
