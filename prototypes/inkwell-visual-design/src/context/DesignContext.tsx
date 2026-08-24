import {
    createContext,
    useContext,
    useEffect,
    useState,
    type ReactNode,
} from "react";
import type { ThemeName } from "../tokens/themes";

/** 外观模式：亮色 / 暗色 / 跟随系统（跟随 `prefers-color-scheme`） */
export type AppearanceMode = "light" | "dark" | "system";
export type AppLocale = "zh-CN" | "en-US";
export type LocalePreference = AppLocale | "system";

interface DesignContextValue {
    themeName: ThemeName;
    /** 用户选择的外观模式（个人设置面板里选的原始值） */
    appearanceMode: AppearanceMode;
    /** 当前实际生效的暗色状态（"跟随系统"时由系统偏好推导） */
    isDark: boolean;
    locale: AppLocale;
    localePreference: LocalePreference;
    setThemeName: (name: ThemeName) => void;
    setAppearanceMode: (mode: AppearanceMode) => void;
    setLocale: (locale: LocalePreference) => void;
    /** 兼容旧调用：等价于 setAppearanceMode(dark ? "dark" : "light") */
    setIsDark: (dark: boolean) => void;
}

const DesignContext = createContext<DesignContextValue | null>(null);

function getSystemPrefersDark(): boolean {
    return (
        typeof window !== "undefined" &&
        window.matchMedia?.("(prefers-color-scheme: dark)").matches === true
    );
}

function getSystemLocale(): AppLocale {
    const language =
        typeof navigator === "undefined"
            ? undefined
            : navigator.languages[0]?.toLowerCase();
    if (language?.startsWith("en")) return "en-US";
    return "zh-CN";
}

function getStoredLocalePreference(): LocalePreference {
    if (typeof window === "undefined") return "zh-CN";
    const preference = window.localStorage.getItem("inkwell-prototype-locale");
    return preference === "system" ||
        preference === "zh-CN" ||
        preference === "en-US"
        ? preference
        : "zh-CN";
}

export function DesignProvider({ children }: { children: ReactNode }) {
    const [themeName, setThemeName] = useState<ThemeName>("amethyst");
    const [appearanceMode, setAppearanceMode] =
        useState<AppearanceMode>("system");
    const [localePreference, setLocale] = useState<LocalePreference>(
        getStoredLocalePreference,
    );
    const [systemLocale, setSystemLocale] = useState(getSystemLocale);
    const [systemPrefersDark, setSystemPrefersDark] =
        useState(getSystemPrefersDark);

    useEffect(() => {
        const media = window.matchMedia?.("(prefers-color-scheme: dark)");
        if (!media) return;
        const handleChange = (e: MediaQueryListEvent) =>
            setSystemPrefersDark(e.matches);
        media.addEventListener("change", handleChange);
        return () => media.removeEventListener("change", handleChange);
    }, []);

    useEffect(() => {
        const handleLanguageChange = () => setSystemLocale(getSystemLocale());
        window.addEventListener("languagechange", handleLanguageChange);
        return () =>
            window.removeEventListener("languagechange", handleLanguageChange);
    }, []);

    useEffect(() => {
        window.localStorage.setItem(
            "inkwell-prototype-locale",
            localePreference,
        );
    }, [localePreference]);

    const isDark =
        appearanceMode === "system"
            ? systemPrefersDark
            : appearanceMode === "dark";
    const locale =
        localePreference === "system" ? systemLocale : localePreference;

    return (
        <DesignContext.Provider
            value={{
                themeName,
                appearanceMode,
                isDark,
                locale,
                localePreference,
                setThemeName,
                setAppearanceMode,
                setLocale,
                setIsDark: (dark) => setAppearanceMode(dark ? "dark" : "light"),
            }}
        >
            {children}
        </DesignContext.Provider>
    );
}

export function useDesign(): DesignContextValue {
    const ctx = useContext(DesignContext);
    if (!ctx) throw new Error("useDesign must be used within DesignProvider");
    return ctx;
}
