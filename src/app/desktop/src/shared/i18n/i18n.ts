import i18n, { createInstance, type i18n as I18nInstance } from "i18next";
import { initReactI18next } from "react-i18next";
import {
    defaultLocale,
    resources,
    type AppLocale,
    type LocalePreference,
} from "./resources";

export function resolveLocalePreference(
    preference: LocalePreference,
    systemLanguages: readonly string[] = typeof navigator === "undefined"
        ? []
        : navigator.languages,
): AppLocale {
    if (preference !== "system") {
        return preference;
    }

    const systemLanguage = systemLanguages[0]?.toLowerCase();
    if (systemLanguage?.startsWith("en")) {
        return "en-US";
    }
    if (systemLanguage?.startsWith("zh")) {
        return "zh-CN";
    }
    return defaultLocale;
}

export async function createAppI18n(locale: AppLocale): Promise<I18nInstance> {
    const instance = createInstance();
    await instance.use(initReactI18next).init({
        resources,
        lng: locale,
        fallbackLng: defaultLocale,
        interpolation: { escapeValue: false },
        returnNull: false,
    });
    return instance;
}

void i18n.use(initReactI18next).init({
    resources,
    lng: defaultLocale,
    fallbackLng: defaultLocale,
    interpolation: { escapeValue: false },
    returnNull: false,
});

export default i18n;
