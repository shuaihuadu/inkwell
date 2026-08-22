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

    test("mobile overview has no horizontal overflow", async ({
        page,
    }, testInfo) => {
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

    test("tablet layout has no horizontal overflow", async ({
        page,
    }, testInfo) => {
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
        const logosLoaded = await page
            .locator("img")
            .evaluateAll((images) =>
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

    test("mobile layout has no horizontal overflow", async ({
        page,
    }, testInfo) => {
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
    test("configuration workspace fills wide viewports", async ({
        page,
    }, testInfo) => {
        test.skip(testInfo.project.name !== "desktop-hd");
        await page.setViewportSize({ width: 1920, height: 1080 });
        await page.goto("/agent");

        const workspace = page.getByTestId("agent-configuration-workspace");
        await expect(workspace).toBeVisible();
        const widths = await workspace.evaluate((element) => {
            const parent = element.parentElement;
            const parentStyle = parent ? getComputedStyle(parent) : null;
            return {
                workspace: element.getBoundingClientRect().width,
                parentContent:
                    (parent?.getBoundingClientRect().width ?? 0) -
                    Number.parseFloat(parentStyle?.paddingLeft ?? "0") -
                    Number.parseFloat(parentStyle?.paddingRight ?? "0"),
                outerPadding: Number.parseFloat(
                    parentStyle?.paddingLeft ?? "0",
                ),
            };
        });

        expect(widths.workspace).toBeCloseTo(widths.parentContent, 0);
        expect(widths.outerPadding).toBe(0);
    });

    test("opens model settings and conversation drawer", async ({
        page,
    }, testInfo) => {
        test.skip(testInfo.project.name !== "desktop-hd");
        await page.goto("/agent");
        await page.getByText("模型与参数", { exact: true }).click();
        await expect(
            page.getByText("Temperature", { exact: true }),
        ).toBeVisible();
        await page.getByRole("button", { name: "试运行" }).click();
        await expect(
            page.getByRole("heading", { name: "从一个研究问题开始" }),
        ).toBeVisible();
        await page.screenshot({
            path: screenshotPath("08-agent-conversation.png"),
            fullPage: true,
        });
    });

    test("reuses the read-only Agent details in chat and version history", async ({
        page,
    }, testInfo) => {
        test.skip(testInfo.project.name !== "desktop-hd");
        const consoleErrors: string[] = [];
        page.on("console", (message) => {
            if (message.type() === "error") consoleErrors.push(message.text());
        });
        page.on("pageerror", (error) => consoleErrors.push(error.message));
        await page
            .context()
            .grantPermissions(["clipboard-read", "clipboard-write"], {
                origin: "http://localhost:4174",
            });

        await page.goto("/shell");
        await page.getByText("客服助手", { exact: true }).first().click();
        await page.getByRole("button", { name: "查看 Agent 详情" }).click();
        const chatDetails = page.getByRole("dialog", { name: "Agent 详情" });
        await expect(chatDetails).toBeVisible();
        await expect(
            chatDetails.getByText("版本：v3", { exact: true }),
        ).toBeVisible();
        await expect(
            chatDetails.getByText("会话版本", { exact: true }),
        ).toHaveCount(0);
        await expect(
            chatDetails.getByText("模型与上下文", { exact: true }),
        ).toBeVisible();
        await expect(
            chatDetails.getByText("web_search", { exact: true }),
        ).toBeVisible();
        const instructions = chatDetails.locator(
            ".inkwell-agent-details-instructions",
        );
        await expect(instructions).toHaveClass(/collapsed/);
        const collapsedHeight = await instructions.evaluate(
            (element) => element.getBoundingClientRect().height,
        );
        await page.waitForTimeout(400);
        await page.screenshot({
            path: screenshotPath("17-agent-chat-readonly-details.png"),
        });
        await chatDetails.getByRole("button", { name: "展开全文" }).click();
        await expect(instructions).toHaveClass(/expanded/);
        await expect(
            chatDetails.getByRole("button", { name: "收起" }),
        ).toBeVisible();
        await page.waitForTimeout(220);
        const expandedHeight = await instructions.evaluate(
            (element) => element.getBoundingClientRect().height,
        );
        expect(expandedHeight).toBeGreaterThan(collapsedHeight);
        await chatDetails
            .getByRole("button", { name: "复制 Instructions" })
            .click();
        await expect(
            page.getByText("Instructions 已复制", { exact: true }),
        ).toBeVisible();
        const copiedInstructions = await page.evaluate(() =>
            navigator.clipboard.readText(),
        );
        expect(copiedInstructions).toContain("你是一名严谨的深度研究助手");
        expect(copiedInstructions).toContain("不得编造链接");
        await page.mouse.move(600, 600);
        await page.waitForTimeout(400);
        await page.screenshot({
            path: screenshotPath(
                "19-agent-chat-long-instructions-expanded.png",
            ),
        });
        await expectNoHorizontalOverflow(page);

        await chatDetails
            .getByRole("button", { name: "关闭 Agent 详情" })
            .click();
        await page.getByRole("button", { name: "返回 Agent 空间" }).click();
        await page.getByRole("button", { name: "编辑 客服助手" }).click();
        await page.getByRole("button", { name: "版本" }).click();
        const historicalRow = page.locator(".ant-table-row").filter({
            hasText: "v2",
        });
        await historicalRow.getByRole("button", { name: "查看" }).click();
        const versionDetails = page.getByRole("dialog", {
            name: "Agent 详情",
        });
        await expect(versionDetails).toBeVisible();
        await expect(
            versionDetails.getByText("历史版本", { exact: true }),
        ).toBeVisible();
        await expect(
            versionDetails.getByText("合同风险清单", { exact: false }),
        ).toBeVisible();
        await page.waitForTimeout(400);
        await page.screenshot({
            path: screenshotPath("18-agent-version-readonly-details.png"),
        });
        await expectNoHorizontalOverflow(page);
        expect(consoleErrors).toEqual([]);
    });

    test("shows the same AG-UI run summary in chat and trial", async ({
        page,
    }, testInfo) => {
        test.skip(testInfo.project.name !== "desktop-hd");
        const consoleErrors: string[] = [];
        page.on("console", (message) => {
            if (message.type() === "error") consoleErrors.push(message.text());
        });
        page.on("pageerror", (error) => consoleErrors.push(error.message));

        await page.goto("/shell");
        await page.getByText("客服助手", { exact: true }).first().click();
        await page.getByText("调研一下行业报告模板", { exact: true }).click();
        await expect(page.getByText("工具调用", { exact: true })).toBeVisible();
        await expect(
            page.getByText("4 项已完成", { exact: true }),
        ).toBeVisible();
        await page.screenshot({
            path: screenshotPath("10-agent-chat-agui-run.png"),
            fullPage: true,
        });

        await page.getByRole("button", { name: "返回 Agent 空间" }).click();
        await page.getByRole("button", { name: "编辑 客服助手" }).click();
        await page.getByRole("button", { name: "试运行" }).click();
        await page.getByRole("button", { name: "研究框架" }).click();
        await expect(page.getByText("4 项已完成", { exact: true })).toBeVisible(
            {
                timeout: 10_000,
            },
        );
        await expect(
            page.getByText(/已完成关于.*整理一份竞品研究框架.*的研究/),
        ).toBeVisible({ timeout: 10_000 });
        const trialRunSummary = page
            .locator("strong")
            .filter({ hasText: /^工具调用$/ });
        await trialRunSummary.scrollIntoViewIfNeeded();
        await expect(trialRunSummary).toBeVisible();
        await page.screenshot({
            path: screenshotPath("11-agent-trial-agui-run.png"),
            fullPage: true,
        });

        expect(consoleErrors).toEqual([]);
    });

    test("shows complete tool call states in trial and chat", async ({
        page,
    }, testInfo) => {
        test.skip(testInfo.project.name !== "desktop-hd");
        const consoleErrors: string[] = [];
        page.on("console", (message) => {
            if (message.type() === "error") consoleErrors.push(message.text());
        });
        page.on("pageerror", (error) => consoleErrors.push(error.message));

        await page.goto("/shell");
        await page.getByRole("button", { name: "编辑 客服助手" }).click();
        await page.getByRole("button", { name: "试运行" }).click();
        await page.getByRole("button", { name: "工具调用" }).click();
        await expect(page.getByText("调用中", { exact: true })).toBeVisible();
        await expect(page.getByText("参数", { exact: true })).toBeVisible();
        await page.screenshot({
            path: screenshotPath("12-agent-trial-tool-calling.png"),
            fullPage: true,
        });

        await expect(page.getByText("1 项失败", { exact: true })).toBeVisible({
            timeout: 10_000,
        });
        await expect(page.getByText("失败原因", { exact: true })).toBeVisible();
        await expect(
            page.getByText(/知识库检索已完成；网页搜索调用失败后/),
        ).toBeVisible({ timeout: 10_000 });
        await expect(
            page.locator(".ant-sender-actions-btn-loading-button"),
        ).toBeHidden();
        await expect(page.getByText("结果", { exact: true })).toBeVisible();
        await page
            .getByText("工具调用", { exact: true })
            .first()
            .scrollIntoViewIfNeeded();
        await page.screenshot({
            path: screenshotPath("13-agent-trial-tool-complete.png"),
            fullPage: true,
        });
        await page
            .getByText("失败原因", { exact: true })
            .scrollIntoViewIfNeeded();
        await page.screenshot({
            path: screenshotPath("15-agent-trial-tool-failure-detail.png"),
            fullPage: true,
        });

        await page.goto("/shell");
        await page.getByText("客服助手", { exact: true }).first().click();
        const chatInput = page.getByPlaceholder(
            "输入消息，Enter 发送，Shift+Enter 换行",
        );
        await chatInput.fill("展示工具调用示例");
        await chatInput.press("Enter");
        await expect(page.getByText("1 项失败", { exact: true })).toBeVisible({
            timeout: 10_000,
        });
        await expect(page.getByText("失败原因", { exact: true })).toBeVisible();
        await expect(
            page.getByText(/知识库检索已完成；网页搜索调用失败后/),
        ).toBeVisible({ timeout: 10_000 });
        await expect(
            page.locator(".ant-sender-actions-btn-loading-button"),
        ).toBeHidden();
        await expect(page.getByText("结果", { exact: true })).toBeVisible();
        for (const shortcut of [
            "整理一份竞品研究框架",
            "为调研报告设计目录",
            "帮我优化一段产品介绍文案",
            "展示工具调用示例",
        ]) {
            await expect(
                page.getByText(shortcut, { exact: true }).last(),
            ).toBeVisible();
        }
        await page
            .getByText("工具调用", { exact: true })
            .first()
            .scrollIntoViewIfNeeded();
        await page.screenshot({
            path: screenshotPath("14-agent-chat-tool-complete.png"),
            fullPage: true,
        });
        const appearanceSwitch = page.getByRole("switch", {
            name: "切换外观",
        });
        await appearanceSwitch.click();
        await expect(appearanceSwitch).toBeChecked();
        await expect(
            page.getByText("展示工具调用示例", { exact: true }).last(),
        ).toBeVisible();
        await page.screenshot({
            path: screenshotPath("16-agent-chat-tool-dark.png"),
            fullPage: true,
        });

        expect(consoleErrors).toEqual([]);
    });

    test("delete action requires confirmation", async ({ page }, testInfo) => {
        test.skip(testInfo.project.name !== "desktop-hd");
        await page.goto("/agent");
        await page.getByRole("button", { name: "删除 Agent" }).click();
        await expect(page.getByText("删除这个 Agent？")).toBeVisible();
    });

    test("shows the chat history limit in model settings", async ({
        page,
    }, testInfo) => {
        test.skip(testInfo.project.name !== "desktop-hd");
        await page.goto("/agent");
        await page.getByText("模型与参数", { exact: true }).click();
        await expect(
            page.getByText("最大消息记录数", { exact: true }),
        ).toBeVisible();
        await expect(
            page.getByText(
                "超过该数量时，最早的历史消息会被裁剪，避免无限增长挤占模型上下文。",
                { exact: true },
            ),
        ).toBeVisible();
        await expect(page.getByRole("spinbutton").last()).toHaveValue("40");
    });

    test("tablet layout has no horizontal overflow", async ({
        page,
    }, testInfo) => {
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
