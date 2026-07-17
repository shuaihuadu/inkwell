import {
    ApiOutlined,
    AppstoreOutlined,
    BulbFilled,
    BulbOutlined,
    DesktopOutlined,
    DownOutlined,
    GithubOutlined,
    LogoutOutlined,
    MoonFilled,
    ReadOutlined,
    RightOutlined,
    SafetyCertificateOutlined,
    SettingOutlined,
    SunFilled,
    ToolOutlined,
} from "@ant-design/icons";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
    Avatar,
    Divider,
    Dropdown,
    Empty,
    Modal,
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
import {
    type AppearanceMode,
    useAppearanceStore,
    useResolvedAppearance,
} from "./appearance-store";
import { desktopThemes, themeNames, type ThemeName } from "./themes";

type NavigationKey = "agents" | "tools" | "skills" | "models" | "admin";

interface NavigationItem {
    key: NavigationKey;
    label: string;
    icon: ReactNode;
    requiresSuper?: boolean;
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
                label: "工具管理",
                icon: <ToolOutlined />,
                placeholder: true,
            },
            {
                key: "skills",
                label: "Skills 管理",
                icon: <ReadOutlined />,
                placeholder: true,
            },
            {
                key: "models",
                label: "模型管理",
                icon: <ApiOutlined />,
                placeholder: true,
            },
        ],
    },
    {
        key: "system",
        label: "系统管理",
        items: [
            {
                key: "admin",
                label: "Admin",
                icon: <SafetyCertificateOutlined />,
                requiresSuper: true,
            },
        ],
    },
];

interface WorkspaceShellProps {
    children: ReactNode;
}

export function WorkspaceShell({ children }: WorkspaceShellProps) {
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
    const [expandedGroups, setExpandedGroups] = useState<Set<string>>(
        () => new Set(navigationGroups.map((group) => group.key)),
    );
    const visibleGroups = navigationGroups
        .map((group) => ({
            ...group,
            items: group.items.filter(
                (item) => !item.requiresSuper || identity?.isSuper,
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
                                ...(identity?.isSuper
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
                                if (key === "admin")
                                    setActiveNavigation("admin");
                                if (key === "logout") void logout();
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
                                                    setActiveNavigation(
                                                        item.key,
                                                    )
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
                        hidden={activeNavigation !== "agents"}
                    >
                        {children}
                    </div>
                    {activeNavigation !== "agents" && (
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
        </div>
    );
}
