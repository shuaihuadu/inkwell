import { Outlet } from "react-router-dom";
import { ConfigProvider, theme as antdTheme } from "antd";
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
