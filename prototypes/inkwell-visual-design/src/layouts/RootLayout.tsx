import { Outlet } from "react-router-dom";
import { ConfigProvider, theme as antdTheme } from "antd";
import { XProvider } from "@ant-design/x";
import zhCN from "@ant-design/x/es/locale/zh_CN";
import { DesignProvider, useDesign } from "../context/DesignContext";
import { THEMES } from "../tokens/themes";
import NavBar from "../components/NavBar";

function ThemedShell() {
    const { themeName, isDark } = useDesign();
    const def = THEMES[themeName];
    return (
        <ConfigProvider
            theme={{
                algorithm: isDark
                    ? antdTheme.darkAlgorithm
                    : antdTheme.defaultAlgorithm,
                token: isDark ? def.dark : def.light,
            }}
        >
            {/* Ant Design X 组件（Agent 对话相关页面用）统一走中文文案，与全站 zh-CN
             * 唯一语言的既定约定一致（ADR-014 / AGENTS.md §3.3 禁区）。 */}
            <XProvider locale={zhCN}>
                <div
                    style={{
                        minHeight: "100vh",
                        background: isDark
                            ? ((def.dark?.colorBgLayout ?? def.light?.colorBgLayout) as string)
                            : (def.light?.colorBgLayout as string),
                        transition: "background 0.25s ease",
                    }}
                >
                    <NavBar />
                    <Outlet />
                </div>
            </XProvider>
        </ConfigProvider>
    );
}

export default function RootLayout() {
    return (
        <DesignProvider>
            <ThemedShell />
        </DesignProvider>
    );
}
