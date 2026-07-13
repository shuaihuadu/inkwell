import { createContext, useContext, useState, type ReactNode } from "react";
import type { ThemeName } from "../tokens/themes";

interface DesignContextValue {
    themeName: ThemeName;
    isDark: boolean;
    setThemeName: (name: ThemeName) => void;
    setIsDark: (dark: boolean) => void;
}

const DesignContext = createContext<DesignContextValue | null>(null);

export function DesignProvider({ children }: { children: ReactNode }) {
    const [themeName, setThemeName] = useState<ThemeName>("amethyst");
    const [isDark, setIsDark] = useState(false);

    return (
        <DesignContext.Provider
            value={{ themeName, isDark, setThemeName, setIsDark }}
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
