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

interface DesignContextValue {
    themeName: ThemeName;
    /** 用户选择的外观模式（个人设置面板里选的原始值） */
    appearanceMode: AppearanceMode;
    /** 当前实际生效的暗色状态（"跟随系统"时由系统偏好推导） */
    isDark: boolean;
    setThemeName: (name: ThemeName) => void;
    setAppearanceMode: (mode: AppearanceMode) => void;
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

export function DesignProvider({ children }: { children: ReactNode }) {
    const [themeName, setThemeName] = useState<ThemeName>("amethyst");
    const [appearanceMode, setAppearanceMode] =
        useState<AppearanceMode>("system");
    const [systemPrefersDark, setSystemPrefersDark] = useState(
        getSystemPrefersDark,
    );

    useEffect(() => {
        const media = window.matchMedia?.("(prefers-color-scheme: dark)");
        if (!media) return;
        const handleChange = (e: MediaQueryListEvent) =>
            setSystemPrefersDark(e.matches);
        media.addEventListener("change", handleChange);
        return () => media.removeEventListener("change", handleChange);
    }, []);

    const isDark =
        appearanceMode === "system" ? systemPrefersDark : appearanceMode === "dark";

    return (
        <DesignContext.Provider
            value={{
                themeName,
                appearanceMode,
                isDark,
                setThemeName,
                setAppearanceMode,
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
