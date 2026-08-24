import { create } from "zustand";
import { createJSONStorage, persist } from "zustand/middleware";
import { useEffect, useState } from "react";
import {
    defaultLocale,
    supportedLocales,
    type AppLocale,
    type LocalePreference,
} from "../../shared/i18n/resources";
import { resolveLocalePreference } from "../../shared/i18n/i18n";

interface LocaleState {
    locale: LocalePreference;
    setLocale: (locale: LocalePreference) => void;
}

const localePreferences: readonly LocalePreference[] = [
    "system",
    ...supportedLocales,
];

export const useLocaleStore = create<LocaleState>()(
    persist(
        (set) => ({
            locale: defaultLocale,
            setLocale: (locale) => set({ locale }),
        }),
        {
            name: "inkwell-locale",
            storage: createJSONStorage(() => localStorage),
            partialize: (state) => ({ locale: state.locale }),
            merge: (persisted, current) => {
                const locale = (persisted as Partial<LocaleState>)?.locale;
                return {
                    ...current,
                    locale: localePreferences.includes(
                        locale as LocalePreference,
                    )
                        ? (locale as LocalePreference)
                        : defaultLocale,
                };
            },
        },
    ),
);

export function useResolvedLocale(): AppLocale {
    const localePreference = useLocaleStore((state) => state.locale);
    const [systemLocale, setSystemLocale] = useState(() =>
        resolveLocalePreference("system"),
    );

    useEffect(() => {
        const updateSystemLocale = (): void => {
            setSystemLocale(resolveLocalePreference("system"));
        };
        window.addEventListener("languagechange", updateSystemLocale);
        return () =>
            window.removeEventListener("languagechange", updateSystemLocale);
    }, []);

    return localePreference === "system" ? systemLocale : localePreference;
}
