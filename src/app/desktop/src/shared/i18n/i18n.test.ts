import { describe, expect, it } from "vitest";
import { createAppI18n, resolveLocalePreference } from "./i18n";
import { resources } from "./resources";

function collectKeys(value: unknown, prefix = ""): string[] {
    if (typeof value !== "object" || value === null) {
        return [prefix];
    }

    return Object.entries(value).flatMap(([key, child]) =>
        collectKeys(child, prefix ? `${prefix}.${key}` : key),
    );
}

function collectComparableKeys(value: unknown): string[] {
    return [
        ...new Set(
            collectKeys(value)
                .filter((key) => !key.startsWith("test."))
                .map((key) => key.replace(/_other$/, "")),
        ),
    ].sort();
}

describe("desktop translations", () => {
    it("uses Chinese as the default product language", async () => {
        const i18n = await createAppI18n("zh-CN");

        expect(i18n.t("common.cancel")).toBe("取消");
    });

    it("provides English translations for shared actions", async () => {
        const i18n = await createAppI18n("en-US");

        expect(i18n.t("common.cancel")).toBe("Cancel");
        expect(i18n.t("shell.settings")).toBe("Preferences");
    });

    it("falls back to Chinese when an English translation is missing", async () => {
        const i18n = await createAppI18n("en-US");
        i18n.addResource("zh-CN", "translation", "test.fallback", "回退文案");

        expect(i18n.t("test.fallback")).toBe("回退文案");
    });

    it("resolves the system language to a supported locale", () => {
        expect(resolveLocalePreference("system", ["zh-Hans-CN"])).toBe("zh-CN");
        expect(resolveLocalePreference("system", ["en-GB"])).toBe("en-US");
        expect(resolveLocalePreference("system", ["fr-FR"])).toBe("zh-CN");
    });

    it("keeps Chinese and English resource keys aligned", () => {
        const chineseKeys = collectComparableKeys(
            resources["zh-CN"].translation,
        );
        const englishKeys = collectComparableKeys(
            resources["en-US"].translation,
        );

        expect(englishKeys).toEqual(chineseKeys);
    });
});
