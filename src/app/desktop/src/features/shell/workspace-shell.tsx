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
import { useTranslation } from "react-i18next";
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
import { useLocaleStore } from "./locale-store";
import type { LocalePreference } from "../../shared/i18n/resources";
import { desktopThemes, themeNames, type ThemeName } from "./themes";
import { useNetworkStore } from "./network-store";

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

interface WorkspaceShellProps {
    children: ReactNode;
    onNavigate: (navigate: () => void) => void;
}

export function WorkspaceShell({ children, onNavigate }: WorkspaceShellProps) {
    const { t } = useTranslation();
    const identity = useAuthStore((state) => state.identity);
    const setSnapshot = useAuthStore((state) => state.setSnapshot);
    const appearanceMode = useAppearanceStore((state) => state.mode);
    const setAppearanceMode = useAppearanceStore((state) => state.setMode);
    const themeName = useAppearanceStore((state) => state.themeName);
    const setThemeName = useAppearanceStore((state) => state.setThemeName);
    const locale = useLocaleStore((state) => state.locale);
    const setLocale = useLocaleStore((state) => state.setLocale);
    const resolvedAppearance = useResolvedAppearance();
    const connectionStatus = useNetworkStore((state) => state.status);
    const navigationGroups: NavigationGroup[] = [
        {
            key: "workspace",
            label: t("shell.navigation.workspace"),
            items: [
                {
                    key: "agents",
                    label: t("shell.navigation.agentSpace"),
                    icon: <AppstoreOutlined />,
                },
            ],
        },
        {
            key: "resources",
            label: t("shell.navigation.resources"),
            items: [
                {
                    key: "tools",
                    label: t("shell.navigation.tools"),
                    icon: <ToolOutlined />,
                },
                {
                    key: "skills",
                    label: t("shell.navigation.skills"),
                    icon: <ReadOutlined />,
                },
                {
                    key: "models",
                    label: t("shell.navigation.models"),
                    icon: <ApiOutlined />,
                },
            ],
        },
        {
            key: "system",
            label: t("shell.navigation.system"),
            items: [
                {
                    key: "admin",
                    label: t("shell.navigation.users"),
                    icon: <SafetyCertificateOutlined />,
                    requiresAdmin: true,
                },
            ],
        },
    ];
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
                        aria-label={t("shell.aboutInkwell")}
                        onClick={() => setAboutOpen(true)}
                    />
                </div>
                <div className="app-header-actions">
                    <Switch
                        size="small"
                        aria-label={t("shell.appearanceSwitch")}
                        checked={resolvedAppearance === "dark"}
                        checkedChildren={<MoonFilled />}
                        unCheckedChildren={<SunFilled />}
                        onChange={(checked) =>
                            setAppearanceMode(checked ? "dark" : "light")
                        }
                    />
                    <div
                        className={`connection-state ${connectionStatus}`}
                        aria-label={t(`shell.connection.${connectionStatus}`)}
                    >
                        <span />
                        {t(`shell.connection.${connectionStatus}`)}
                    </div>
                    <Dropdown
                        trigger={["click"]}
                        placement="bottomRight"
                        menu={{
                            items: [
                                {
                                    key: "guide",
                                    icon: <BookOutlined />,
                                    label: t("shell.guide"),
                                },
                                {
                                    key: "quick-start",
                                    icon: <RocketOutlined />,
                                    label: t("shell.quickStart"),
                                },
                                {
                                    key: "faq",
                                    icon: <QuestionCircleOutlined />,
                                    label: t("shell.faq"),
                                },
                                { type: "divider" },
                                {
                                    key: "about",
                                    icon: <InfoCircleOutlined />,
                                    label: t("shell.aboutInkwell"),
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
                        <Tooltip title={t("shell.help")}>
                            <Button
                                type="text"
                                aria-label={t("shell.help")}
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
                                    label: t("shell.settings"),
                                },
                                {
                                    key: "change-password",
                                    icon: <KeyOutlined />,
                                    label: t("shell.changePassword"),
                                },
                                ...(identity?.isAdmin
                                    ? [
                                          {
                                              key: "admin",
                                              icon: (
                                                  <SafetyCertificateOutlined />
                                              ),
                                              label: t("shell.administration"),
                                          },
                                      ]
                                    : []),
                                { type: "divider" as const },
                                {
                                    key: "logout",
                                    icon: <LogoutOutlined />,
                                    label: t("shell.logout"),
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
                            aria-label={t("shell.openUserMenu")}
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
                <aside
                    className="app-sidebar"
                    aria-label={t("shell.navigation.main")}
                >
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
                                                    ? t(
                                                          "shell.placeholderEntry",
                                                      )
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
                                                        {t("shell.comingSoon")}
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
                                                {t("shell.comingSoon")}
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
                title={t("shell.quickStart")}
                width={400}
                extra={
                    <Typography.Text type="secondary">
                        {completedGuideSteps.size} / 5
                    </Typography.Text>
                }
            >
                <Typography.Paragraph type="secondary">
                    {t("shell.quickStartDescription")}
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
                    {(
                        [
                            "create",
                            "configure",
                            "run",
                            "publish",
                            "share",
                        ] as const
                    ).map((step, index) => (
                        <label className="quick-start-item" key={step}>
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
                                    {t(`shell.quickStartSteps.${step}.title`)}
                                </Typography.Text>
                                <Typography.Text type="secondary">
                                    {t(
                                        `shell.quickStartSteps.${step}.description`,
                                    )}
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
                    {t("shell.goToAgentSpace")}
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
                        <Typography.Text type="secondary">
                            {t("shell.about.version")}
                        </Typography.Text>
                        <Typography.Text data-testid="app-version">
                            {metadataQuery.data?.version ?? "-"}
                        </Typography.Text>
                    </div>
                    <div>
                        <Typography.Text type="secondary">
                            {t("shell.about.buildNumber")}
                        </Typography.Text>
                        <Typography.Text data-testid="app-build-number">
                            {metadataQuery.data?.buildNumber ??
                                t("common.unavailable")}
                        </Typography.Text>
                    </div>
                    <div>
                        <Typography.Text type="secondary">
                            {t("shell.about.commit")}
                        </Typography.Text>
                        <Typography.Text data-testid="app-commit">
                            {metadataQuery.data?.commit?.slice(0, 12) ??
                                t("common.unavailable")}
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
                        alt={t("shell.about.qrAlt")}
                    />
                    <Typography.Text type="secondary">
                        {t("shell.about.followAuthor")}
                    </Typography.Text>
                </div>
            </Modal>

            <Modal
                open={settingsOpen}
                onCancel={() => setSettingsOpen(false)}
                footer={null}
                centered
                width={440}
                title={t("shell.settings")}
            >
                <Typography.Text type="secondary">
                    {t("locale.label")}
                </Typography.Text>
                <Segmented
                    block
                    className="appearance-options"
                    value={locale}
                    onChange={(value) => setLocale(value as LocalePreference)}
                    options={[
                        { value: "zh-CN", label: t("locale.chinese") },
                        { value: "en-US", label: t("locale.english") },
                        { value: "system", label: t("locale.system") },
                    ]}
                />
                <Typography.Text type="secondary" className="theme-label">
                    {t("shell.appearanceMode")}
                </Typography.Text>
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
                                    {t("shell.light")}
                                </Space>
                            ),
                        },
                        {
                            value: "dark",
                            label: (
                                <Space size={4}>
                                    <BulbFilled />
                                    {t("shell.dark")}
                                </Space>
                            ),
                        },
                        {
                            value: "system",
                            label: (
                                <Space size={4}>
                                    <DesktopOutlined />
                                    {t("shell.system")}
                                </Space>
                            ),
                        },
                    ]}
                />
                <Typography.Text type="secondary" className="theme-label">
                    {t("shell.themeColor")}
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
                                {t(`shell.themes.${name}`)}
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
