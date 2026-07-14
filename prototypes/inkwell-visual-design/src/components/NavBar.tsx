import { Link, useLocation } from "react-router-dom";
import {
    Layout,
    Space,
    Segmented,
    Switch,
    Typography,
    theme as antdTheme,
} from "antd";
import { BulbOutlined, BulbFilled } from "@ant-design/icons";
import { useDesign } from "../context/DesignContext";
import { THEMES, THEME_NAMES } from "../tokens/themes";
import logo from "../../assets/logos/logo.svg?no-inline";

const NAV_ITEMS = [
    { path: "/", label: "Design Lab" },
    { path: "/themes", label: "主题" },
    { path: "/logos", label: "Logo" },
    { path: "/login", label: "登录页" },
    { path: "/agent", label: "Agent 设计" },
];

export default function NavBar() {
    const { themeName, isDark, setThemeName, setIsDark } = useDesign();
    const location = useLocation();
    const { token } = antdTheme.useToken();

    return (
        <Layout.Header
            className="design-nav"
            style={{
                display: "flex",
                alignItems: "center",
                gap: 24,
                padding: "0 24px",
                background: isDark ? token.colorBgContainer : "#fff",
                borderBottom: `1px solid ${token.colorBorderSecondary}`,
                position: "sticky",
                top: 0,
                zIndex: 100,
                height: 56,
                flexWrap: "wrap",
                overflow: "visible",
            }}
        >
            {/* Brand */}
            <Space className="design-nav-brand" align="center" size={8}>
                <img src={logo} alt="Inkwell" width={28} height={28} />
                <Typography.Text
                    strong
                    style={{
                        fontSize: 15,
                        color: token.colorPrimary,
                        letterSpacing: 0.5,
                    }}
                >
                    Inkwell
                </Typography.Text>
                <Typography.Text
                    className="design-nav-subtitle"
                    type="secondary"
                    style={{ fontSize: 11 }}
                >
                    Design Lab
                </Typography.Text>
            </Space>

            {/* Page Navigation */}
            <nav
                className="design-nav-links"
                style={{ display: "flex", gap: 2 }}
            >
                {NAV_ITEMS.map((item) => {
                    const active =
                        item.path === "/"
                            ? location.pathname === "/"
                            : location.pathname.startsWith(item.path);
                    return (
                        <Link
                            key={item.path}
                            to={item.path}
                            style={{
                                padding: "4px 12px",
                                borderRadius: token.borderRadius,
                                fontSize: 13,
                                fontWeight: active ? 600 : 400,
                                color: active
                                    ? token.colorPrimary
                                    : token.colorTextSecondary,
                                background: active
                                    ? token.colorPrimaryBg
                                    : "transparent",
                                textDecoration: "none",
                                transition: "all 0.15s",
                            }}
                        >
                            {item.label}
                        </Link>
                    );
                })}
            </nav>

            <div
                className="design-nav-controls"
                style={{
                    marginLeft: "auto",
                    display: "flex",
                    alignItems: "center",
                    gap: 12,
                }}
            >
                {/* Theme Selector */}
                <Segmented
                    className="design-nav-theme"
                    size="small"
                    value={themeName}
                    onChange={(v) => setThemeName(v as typeof themeName)}
                    options={THEME_NAMES.map((n) => ({
                        value: n,
                        label: (
                            <span
                                style={{
                                    display: "flex",
                                    alignItems: "center",
                                    gap: 4,
                                }}
                            >
                                <span
                                    style={{
                                        display: "inline-block",
                                        width: 8,
                                        height: 8,
                                        borderRadius: "50%",
                                        background: THEMES[n].primaryColor,
                                        flexShrink: 0,
                                    }}
                                />
                                <span style={{ fontSize: 11 }}>
                                    {THEMES[n].label}
                                </span>
                            </span>
                        ),
                    }))}
                />
                {/* Dark mode toggle */}
                <Switch
                    size="small"
                    checked={isDark}
                    onChange={setIsDark}
                    checkedChildren={<BulbFilled />}
                    unCheckedChildren={<BulbOutlined />}
                />
            </div>
        </Layout.Header>
    );
}
