import { Outlet } from "react-router-dom";
import { ConfigProvider, theme as antdTheme } from "antd";
import antdEnUS from "antd/locale/en_US";
import antdZhCN from "antd/locale/zh_CN";
import { XProvider } from "@ant-design/x";
import xEnUS from "@ant-design/x/es/locale/en_US";
import xZhCN from "@ant-design/x/es/locale/zh_CN";
import { DesignProvider, useDesign } from "../context/DesignContext";
import { THEMES } from "../tokens/themes";
import NavBar from "../components/NavBar";

function ThemedShell() {
    const { themeName, isDark, locale } = useDesign();
    const def = THEMES[themeName];
    return (
        <ConfigProvider
            locale={locale === "en-US" ? antdEnUS : antdZhCN}
            theme={{
                algorithm: isDark
                    ? antdTheme.darkAlgorithm
                    : antdTheme.defaultAlgorithm,
                token: isDark ? def.dark : def.light,
            }}
        >
            <XProvider locale={locale === "en-US" ? xEnUS : xZhCN}>
                <div
                    style={{
                        minHeight: "100vh",
                        background: isDark
                            ? ((def.dark?.colorBgLayout ??
                                  def.light?.colorBgLayout) as string)
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
