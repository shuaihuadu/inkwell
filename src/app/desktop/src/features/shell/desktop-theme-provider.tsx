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
        document.documentElement.style.setProperty(
            "--primary",
            String(activeTokens?.colorPrimary),
        );
        document.documentElement.style.setProperty(
            "--shell-bg",
            String(activeTokens?.colorBgLayout),
        );
        document.documentElement.style.setProperty(
            "--shell-panel",
            String(activeTokens?.colorBgContainer),
        );
        document.documentElement.style.setProperty(
            "--shell-border",
            String(activeTokens?.colorBorderSecondary),
        );
    }, [activeTokens, isDark, themeName]);

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
            {children}
        </ConfigProvider>
    );
}
