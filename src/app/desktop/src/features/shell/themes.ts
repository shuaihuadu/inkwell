import type { ThemeConfig } from "antd";

export type ThemeName = "amethyst" | "terracotta" | "teal";

export interface DesktopTheme {
    label: string;
    primaryColor: string;
    light: ThemeConfig["token"];
    dark: ThemeConfig["token"];
}

const commonTokens: ThemeConfig["token"] = {
    borderRadius: 8,
    fontFamily: "Avenir Next, PingFang SC, Microsoft YaHei, sans-serif",
};

export const desktopThemes: Record<ThemeName, DesktopTheme> = {
    amethyst: {
        label: "曜石紫",
        primaryColor: "#68469C",
        light: {
            ...commonTokens,
            colorPrimary: "#68469C",
            colorInfo: "#496E9C",
            colorSuccess: "#3F745D",
            colorWarning: "#A06E2F",
            colorError: "#B54153",
            colorBgLayout: "#F6F5F8",
            colorBgContainer: "#FFFFFF",
            colorBorder: "#D3CEDA",
            colorBorderSecondary: "#E8E5EC",
        },
        dark: {
            ...commonTokens,
            colorPrimary: "#B89AE5",
            colorInfo: "#78A2D4",
            colorSuccess: "#79B696",
            colorWarning: "#D4A963",
            colorError: "#E18290",
            colorBgLayout: "#121114",
            colorBgContainer: "#1C1A20",
            colorBorder: "#48424F",
            colorBorderSecondary: "#312D36",
        },
    },
    terracotta: {
        label: "朱砂橙",
        primaryColor: "#B4533C",
        light: {
            ...commonTokens,
            colorPrimary: "#B4533C",
            colorInfo: "#3E7185",
            colorSuccess: "#46725A",
            colorWarning: "#A96F2A",
            colorError: "#B13D3D",
            colorBgLayout: "#F8F6F4",
            colorBgContainer: "#FFFFFF",
            colorBorder: "#D8CEC9",
            colorBorderSecondary: "#EAE3DF",
        },
        dark: {
            ...commonTokens,
            colorPrimary: "#E58A70",
            colorInfo: "#76A9BA",
            colorSuccess: "#7EB18F",
            colorWarning: "#D6A25B",
            colorError: "#E47C7C",
            colorBgLayout: "#171413",
            colorBgContainer: "#211D1B",
            colorBorder: "#51433E",
            colorBorderSecondary: "#392F2C",
        },
    },
    teal: {
        label: "碧海青",
        primaryColor: "#0D9488",
        light: {
            ...commonTokens,
            colorPrimary: "#0D9488",
            colorInfo: "#0891B2",
            colorSuccess: "#059669",
            colorWarning: "#D97706",
            colorError: "#DC2626",
            colorBgLayout: "#F4F7F7",
            colorBgContainer: "#FFFFFF",
            colorBorder: "#CBD7D6",
            colorBorderSecondary: "#E1E8E7",
        },
        dark: {
            ...commonTokens,
            colorPrimary: "#2DD4BF",
            colorInfo: "#22D3EE",
            colorSuccess: "#34D399",
            colorWarning: "#FBBF24",
            colorError: "#F87171",
            colorBgLayout: "#101515",
            colorBgContainer: "#192120",
            colorBorder: "#3A4A48",
            colorBorderSecondary: "#2A3735",
        },
    },
};

export const themeNames: ThemeName[] = ["amethyst", "terracotta", "teal"];
