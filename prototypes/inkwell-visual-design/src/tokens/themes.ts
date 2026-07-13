import type { ThemeConfig } from "antd";

export type ThemeName = "teal" | "terracotta" | "amethyst";

export interface ThemeDefinition {
    name: ThemeName;
    label: string;
    tagline: string;
    /** Hex color representing primary in light mode */
    primaryColor: string;
    light: ThemeConfig["token"];
    dark: ThemeConfig["token"];
}

/**
 * 三套主题定义
 *
 * 设计原则：
 * - 主色分别承载清澈、温润与高端三种品牌气质
 * - 大面积背景与边框使用轻色温中性色，主色集中在操作与状态层
 * - 暗色模式保持中性层级，不把整页染成主色
 */
export const THEMES: Record<ThemeName, ThemeDefinition> = {
    /* ── 1. 碧海青 · Aqua Teal ─────────────────────────────────────────── */
    teal: {
        name: "teal",
        label: "碧海青",
        tagline: "清澈、现代、技术感",
        primaryColor: "#0D9488",
        light: {
            colorPrimary: "#0D9488",
            colorInfo: "#0891B2",
            colorSuccess: "#059669",
            colorSuccessBg: "#F1F7F4",
            colorSuccessBorder: "#D8E8DF",
            colorSuccessText: "#35624E",
            colorWarning: "#D97706",
            colorWarningBg: "#FBF7EF",
            colorWarningBorder: "#ECDFC9",
            colorWarningText: "#805921",
            colorError: "#DC2626",
            colorErrorBg: "#FBF3F3",
            colorErrorBorder: "#EFDADA",
            colorErrorText: "#934343",
            colorInfoBg: "#F1F6F8",
            colorInfoBorder: "#D9E7EB",
            colorInfoText: "#376877",
            colorBgLayout: "#F4F7F7",
            colorBgContainer: "#FFFFFF",
            colorBorder: "#CBD7D6",
            colorBorderSecondary: "#E1E8E7",
            borderRadius: 8,
            fontFamily: "'PingFang SC', 'Microsoft YaHei', sans-serif",
        },
        dark: {
            colorPrimary: "#2DD4BF",
            colorInfo: "#22D3EE",
            colorSuccess: "#34D399",
            colorSuccessBg: "#172A24",
            colorSuccessBorder: "#29483D",
            colorSuccessText: "#9AD8BB",
            colorWarning: "#FBBF24",
            colorWarningBg: "#2B2518",
            colorWarningBorder: "#4A3E24",
            colorWarningText: "#E8C77B",
            colorError: "#F87171",
            colorErrorBg: "#2C1D1D",
            colorErrorBorder: "#4D2E2E",
            colorErrorText: "#E9A0A0",
            colorInfoBg: "#17272B",
            colorInfoBorder: "#29434A",
            colorInfoText: "#94CAD5",
            colorBgLayout: "#101515",
            colorBgContainer: "#192120",
            colorBorder: "#3A4A48",
            colorBorderSecondary: "#2A3735",
            borderRadius: 8,
            fontFamily: "'PingFang SC', 'Microsoft YaHei', sans-serif",
        },
    },

    /* ── 2. 朱砂橙 · Terracotta ──────────────────────────────────────────── */
    terracotta: {
        name: "terracotta",
        label: "朱砂橙",
        tagline: "温润、雅致、创造力",
        primaryColor: "#B4533C",
        light: {
            colorPrimary: "#B4533C",
            colorInfo: "#3E7185",
            colorSuccess: "#46725A",
            colorSuccessBg: "#F2F6F3",
            colorSuccessBorder: "#DCE7DF",
            colorSuccessText: "#3E624D",
            colorWarning: "#A96F2A",
            colorWarningBg: "#FAF6F0",
            colorWarningBorder: "#EADDCB",
            colorWarningText: "#7F592A",
            colorError: "#B13D3D",
            colorErrorBg: "#FAF2F2",
            colorErrorBorder: "#ECD8D8",
            colorErrorText: "#8C4040",
            colorInfoBg: "#F1F5F6",
            colorInfoBorder: "#D8E3E6",
            colorInfoText: "#416775",
            colorBgLayout: "#F8F6F4",
            colorBgContainer: "#FFFFFF",
            colorBorder: "#D8CEC9",
            colorBorderSecondary: "#EAE3DF",
            borderRadius: 8,
            fontFamily: "'PingFang SC', 'Microsoft YaHei', sans-serif",
        },
        dark: {
            colorPrimary: "#E58A70",
            colorInfo: "#76A9BA",
            colorSuccess: "#7EB18F",
            colorSuccessBg: "#202B24",
            colorSuccessBorder: "#37463B",
            colorSuccessText: "#A6CBB1",
            colorWarning: "#D6A25B",
            colorWarningBg: "#2C251C",
            colorWarningBorder: "#4A3C29",
            colorWarningText: "#DFC18E",
            colorError: "#E47C7C",
            colorErrorBg: "#2E2020",
            colorErrorBorder: "#4D3030",
            colorErrorText: "#E7AAAA",
            colorInfoBg: "#1D282B",
            colorInfoBorder: "#314247",
            colorInfoText: "#9ABFC9",
            colorBgLayout: "#171413",
            colorBgContainer: "#211D1B",
            colorBorder: "#51433E",
            colorBorderSecondary: "#392F2C",
            borderRadius: 8,
            fontFamily: "'PingFang SC', 'Microsoft YaHei', sans-serif",
        },
    },

    /* ── 3. 曜石紫 · Amethyst ────────────────────────────────────────────── */
    amethyst: {
        name: "amethyst",
        label: "曜石紫",
        tagline: "高端、从容、精致感",
        primaryColor: "#68469C",
        light: {
            colorPrimary: "#68469C",
            colorInfo: "#496E9C",
            colorSuccess: "#3F745D",
            colorSuccessBg: "#F2F6F4",
            colorSuccessBorder: "#DAE7E0",
            colorSuccessText: "#3A624F",
            colorWarning: "#A06E2F",
            colorWarningBg: "#FAF6F0",
            colorWarningBorder: "#EADDCB",
            colorWarningText: "#7D592C",
            colorError: "#B54153",
            colorErrorBg: "#FAF2F4",
            colorErrorBorder: "#ECD8DD",
            colorErrorText: "#8F4050",
            colorInfoBg: "#F2F5F8",
            colorInfoBorder: "#DCE4EC",
            colorInfoText: "#466583",
            colorBgLayout: "#F6F5F8",
            colorBgContainer: "#FFFFFF",
            colorBorder: "#D3CEDA",
            colorBorderSecondary: "#E8E5EC",
            borderRadius: 8,
            fontFamily: "'PingFang SC', 'Microsoft YaHei', sans-serif",
        },
        dark: {
            colorPrimary: "#B89AE5",
            colorInfo: "#78A2D4",
            colorSuccess: "#79B696",
            colorSuccessBg: "#202923",
            colorSuccessBorder: "#37453C",
            colorSuccessText: "#A5CBB4",
            colorWarning: "#D4A963",
            colorWarningBg: "#2B261D",
            colorWarningBorder: "#473D2C",
            colorWarningText: "#DFC38F",
            colorError: "#E18290",
            colorErrorBg: "#2E2024",
            colorErrorBorder: "#4B3037",
            colorErrorText: "#E6A8B1",
            colorInfoBg: "#1E252D",
            colorInfoBorder: "#323E4B",
            colorInfoText: "#A3BCD8",
            colorBgLayout: "#121114",
            colorBgContainer: "#1C1A20",
            colorBorder: "#48424F",
            colorBorderSecondary: "#312D36",
            borderRadius: 8,
            fontFamily: "'PingFang SC', 'Microsoft YaHei', sans-serif",
        },
    },
};

export const THEME_NAMES: ThemeName[] = ["amethyst", "terracotta", "teal"];
