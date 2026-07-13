import { expect, test, type Page } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";
import { fileURLToPath } from "url";

const dirname = path.dirname(fileURLToPath(import.meta.url));
const screenshotsDir = path.join(dirname, "..", "screenshots");
fs.mkdirSync(screenshotsDir, { recursive: true });

function screenshotPath(name: string) {
    return path.join(screenshotsDir, name);
}

async function waitForRender(page: Page) {
    await page.waitForTimeout(600);
}

async function expectNoHorizontalOverflow(page: Page) {
    const overflow = await page.evaluate(() => ({
        body: document.body.scrollWidth - document.documentElement.clientWidth,
        root:
            document.documentElement.scrollWidth -
            document.documentElement.clientWidth,
    }));

    expect(overflow, "The page must not overflow horizontally").toEqual({
        body: 0,
        root: 0,
    });
}

test.describe("Design Lab", () => {
    test("desktop overview", async ({ page }, testInfo) => {
        test.skip(testInfo.project.name !== "desktop-hd");
        await page.goto("/");
        await expect(page.getByText("Inkwell Visual Design Lab")).toBeVisible();
        await page.screenshot({
            path: screenshotPath("01-design-lab-desktop.png"),
            fullPage: true,
        });
    });

    test("mobile overview has no horizontal overflow", async ({ page }, testInfo) => {
        test.skip(testInfo.project.name !== "mobile");
        await page.goto("/");
        await waitForRender(page);
        await expectNoHorizontalOverflow(page);
        await page.screenshot({
            path: screenshotPath("02-design-lab-mobile.png"),
            fullPage: true,
        });
    });
});

test.describe("Theme Explorer", () => {
    test("shows the three-theme comparison", async ({ page }, testInfo) => {
        test.skip(testInfo.project.name !== "desktop-hd");
        await page.goto("/themes");
        await page.getByText("三主题对比", { exact: true }).click();
        await expect(page.getByText("曜石紫亮色")).toBeVisible();
        await expect(page.getByText("朱砂橙亮色")).toBeVisible();
        await expect(page.getByText("碧海青亮色")).toBeVisible();
        await page.screenshot({
            path: screenshotPath("03-themes-comparison.png"),
            fullPage: true,
        });
    });

    test("tablet layout has no horizontal overflow", async ({ page }, testInfo) => {
        test.skip(testInfo.project.name !== "tablet");
        await page.goto("/themes");
        await waitForRender(page);
        await expectNoHorizontalOverflow(page);
        await page.screenshot({
            path: screenshotPath("04-themes-tablet.png"),
            fullPage: true,
        });
    });
});

test.describe("Logo Explorer", () => {
    test("loads the selected Logo", async ({ page }, testInfo) => {
        test.skip(testInfo.project.name !== "desktop-hd");
        await page.goto("/logos");
        await waitForRender(page);
        const logosLoaded = await page.locator("img").evaluateAll((images) =>
            images.every(
                (image) =>
                    image instanceof HTMLImageElement &&
                    image.complete &&
                    image.naturalWidth > 0,
            ),
        );
        expect(logosLoaded).toBe(true);
        await page.screenshot({
            path: screenshotPath("05-logo-desktop.png"),
            fullPage: true,
        });
    });
});

test.describe("Login Explorer", () => {
    test("renders the workstation login", async ({ page }, testInfo) => {
        test.skip(testInfo.project.name !== "desktop-hd");
        await page.goto("/login");
        await expect(
            page.getByRole("button", { name: /登\s*录/ }),
        ).toBeVisible();
        await page.screenshot({
            path: screenshotPath("06-login-desktop.png"),
            fullPage: true,
        });
    });

    test("mobile layout has no horizontal overflow", async ({ page }, testInfo) => {
        test.skip(testInfo.project.name !== "mobile");
        await page.goto("/login");
        await waitForRender(page);
        await expectNoHorizontalOverflow(page);
        await page.screenshot({
            path: screenshotPath("07-login-mobile.png"),
            fullPage: true,
        });
    });
});

test.describe("Agent Design Page", () => {
    test("opens model settings and conversation drawer", async ({ page }, testInfo) => {
        test.skip(testInfo.project.name !== "desktop-hd");
        await page.goto("/agent");
        await page.getByText("模型与参数", { exact: true }).click();
        await expect(page.getByText("Temperature", { exact: true })).toBeVisible();
        await page.getByRole("button", { name: "开始对话" }).click();
        await expect(page.getByRole("dialog")).toBeVisible();
        await page.screenshot({
            path: screenshotPath("08-agent-conversation.png"),
            fullPage: true,
        });
    });

    test("delete action requires confirmation", async ({ page }, testInfo) => {
        test.skip(testInfo.project.name !== "desktop-hd");
        await page.goto("/agent");
        await page.getByRole("button", { name: "删除 Agent" }).click();
        await expect(page.getByText("删除这个 Agent？")).toBeVisible();
    });

    test("tablet layout has no horizontal overflow", async ({ page }, testInfo) => {
        test.skip(testInfo.project.name !== "tablet");
        await page.goto("/agent");
        await waitForRender(page);
        await expectNoHorizontalOverflow(page);
        await page.screenshot({
            path: screenshotPath("09-agent-tablet.png"),
            fullPage: true,
        });
    });
});
