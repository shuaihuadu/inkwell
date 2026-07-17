import { create } from "zustand";
import { createJSONStorage, persist } from "zustand/middleware";
import { useEffect, useState } from "react";
import type { ThemeName } from "./themes";

export type AppearanceMode = "light" | "dark" | "system";

interface AppearanceState {
    mode: AppearanceMode;
    themeName: ThemeName;
    setMode: (mode: AppearanceMode) => void;
    setThemeName: (themeName: ThemeName) => void;
}

export const useAppearanceStore = create<AppearanceState>()(
    persist(
        (set) => ({
            mode: "system",
            themeName: "amethyst",
            setMode: (mode) => set({ mode }),
            setThemeName: (themeName) => set({ themeName }),
        }),
        {
            name: "inkwell-appearance",
            storage: createJSONStorage(() => localStorage),
        },
    ),
);

export function useResolvedAppearance(): "light" | "dark" {
    const mode = useAppearanceStore((state) => state.mode);
    const [systemDark, setSystemDark] = useState(
        () => window.matchMedia("(prefers-color-scheme: dark)").matches,
    );

    useEffect(() => {
        const media = window.matchMedia("(prefers-color-scheme: dark)");
        const updateSystemAppearance = (event: MediaQueryListEvent): void => {
            setSystemDark(event.matches);
        };
        media.addEventListener("change", updateSystemAppearance);
        return () =>
            media.removeEventListener("change", updateSystemAppearance);
    }, []);

    return mode === "dark" || (mode === "system" && systemDark)
        ? "dark"
        : "light";
}
