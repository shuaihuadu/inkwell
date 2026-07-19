import { ConfigProvider, theme } from "antd";
import zhCN from "antd/locale/zh_CN";
import type { ReactNode } from "react";
import { useEffect } from "react";
import { useAppearanceStore, useResolvedAppearance } from "./appearance-store";
import { desktopThemes } from "./themes";

interface DesktopThemeProviderProps {
    children: ReactNode;
}

export function DesktopThemeProvider({ children }: DesktopThemeProviderProps) {
    const resolvedAppearance = useResolvedAppearance();
    const isDark = resolvedAppearance === "dark";
    const themeName = useAppearanceStore((state) => state.themeName);
    const activeTheme = desktopThemes[themeName];
    const activeTokens = isDark ? activeTheme.dark : activeTheme.light;

    useEffect(() => {
        document.documentElement.dataset.appearance = isDark ? "dark" : "light";
        document.documentElement.dataset.theme = themeName;
    }, [isDark, themeName]);

    return (
        <ConfigProvider
            locale={zhCN}
            theme={{
                algorithm: isDark
                    ? theme.darkAlgorithm
                    : theme.defaultAlgorithm,
                token: activeTokens,
            }}
        >
            <ThemeCssVariables>{children}</ThemeCssVariables>
        </ConfigProvider>
    );
}

function ThemeCssVariables({ children }: DesktopThemeProviderProps) {
    const { token } = theme.useToken();

    useEffect(() => {
        const rootStyle = document.documentElement.style;
        rootStyle.setProperty("--primary", token.colorPrimary);
        rootStyle.setProperty("--primary-soft", token.colorPrimaryBg);
        rootStyle.setProperty("--shell-bg", token.colorBgLayout);
        rootStyle.setProperty("--shell-panel", token.colorBgContainer);
        rootStyle.setProperty("--shell-border", token.colorBorderSecondary);
        rootStyle.setProperty(
            "--shell-text-description",
            token.colorTextDescription,
        );
        rootStyle.setProperty(
            "--shell-text-tertiary",
            token.colorTextTertiary,
        );
        rootStyle.setProperty(
            "--shell-fill-quaternary",
            token.colorFillQuaternary,
        );
    }, [token]);

    return children;
}
