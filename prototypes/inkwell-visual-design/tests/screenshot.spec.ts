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
    test("shows bilingual personal settings", async ({ page }, testInfo) => {
        test.skip(testInfo.project.name !== "desktop-hd");
        await page.goto("/shell");
        await waitForRender(page);

        await page.getByText("alice", { exact: true }).click();
        await page.getByText("个人设置", { exact: true }).click();
        await page.getByText("English", { exact: true }).click();

        const preferences = page.getByRole("dialog", {
            name: "Preferences",
        });
        await expect(preferences).toBeVisible();
        await expect(
            preferences.getByText("Display language", { exact: true }),
        ).toBeVisible();
        await expect(
            preferences.getByText("Appearance", { exact: true }),
        ).toBeVisible();
        const languageOptions = preferences.locator(".ant-segmented").first();
        await expect(
            languageOptions.getByText("System", { exact: true }),
        ).toBeVisible();
        await expect(page.locator(".ant-dropdown")).toBeHidden();
        await expect(preferences).toHaveCSS("opacity", "1");
        await expect(preferences).toHaveCSS("transform", "none");
        await expectNoHorizontalOverflow(page);
        await page.screenshot({
            path: screenshotPath("32-bilingual-personal-settings.png"),
            fullPage: true,
        });
    });

    test("shows polished read-only Tool and model details", async ({
        page,
    }, testInfo) => {
        test.skip(testInfo.project.name !== "desktop-hd");
        await page.goto("/shell");
        await page.getByText("工具", { exact: true }).first().click();

        const firstToolRow = page.locator(".ant-table-row").first();
        await firstToolRow.getByRole("button", { name: /^查看 / }).click();
        const toolDetails = page.getByRole("dialog", { name: "Tool 详情" });
        await expect(toolDetails).toBeVisible();
        await expect(
            toolDetails.getByRole("heading", { name: "web_search", level: 4 }),
        ).toBeVisible();
        await expect(
            toolDetails.getByText("参数", { exact: true }),
        ).toBeVisible();
        await expect(
            toolDetails.getByText("query", { exact: true }),
        ).toBeVisible();
        await expect(
            toolDetails.getByText("原始 JSON Schema", { exact: true }),
        ).toBeVisible();
        await expect(toolDetails.locator("input, textarea")).toHaveCount(0);
        await toolDetails.getByText("查看原始 Schema", { exact: true }).click();
        await expect(
            toolDetails.locator(".inkwell-resource-schema"),
        ).toBeVisible();
        await page.waitForTimeout(400);
        await page.screenshot({
            path: screenshotPath("30-tool-readonly-details.png"),
        });

        await toolDetails
            .getByRole("button", { name: "关闭 Tool 详情" })
            .click();
        await page.getByText("模型", { exact: true }).first().click();
        const firstModelRow = page.locator(".ant-table-row").first();
        const viewModelButton = firstModelRow.getByRole("button", {
            name: "查看 gpt-4.1",
        });
        const testModelButton = firstModelRow.getByRole("button", {
            name: "测试 gpt-4.1",
        });
        await expect(viewModelButton).toHaveCSS("border-style", "solid");
        await expect(testModelButton).toHaveCSS("border-style", "solid");
        await viewModelButton.click();
        const modelDetails = page.getByRole("dialog", { name: "模型详情" });
        await expect(modelDetails).toBeVisible();
        await expect(
            modelDetails.getByRole("heading", { name: "gpt-4.1", level: 4 }),
        ).toBeVisible();
        await expect(
            modelDetails.getByText("Token 上限", { exact: true }),
        ).toBeVisible();
        await expect(
            modelDetails.getByText("工具调用", { exact: true }),
        ).toBeVisible();
        await expect(
            modelDetails.getByText("不支持", { exact: true }),
        ).toBeVisible();
        await expect(modelDetails.locator("input, textarea")).toHaveCount(0);
        await expect(
            modelDetails.getByRole("button", { name: /测试/ }),
        ).toHaveCount(0);
        await page.waitForTimeout(400);
        await page.screenshot({
            path: screenshotPath("31-model-readonly-details.png"),
        });
    });

    test("shows a dedicated Skill detail view before editing", async ({
        page,
    }, testInfo) => {
        test.skip(testInfo.project.name !== "desktop-hd");
        await page.goto("/shell");
        await page.getByText("Skills", { exact: true }).first().click();

        const firstSkillRow = page.locator(".ant-table-row").first();
        await firstSkillRow.getByRole("button", { name: /^查看 / }).click();
        const skillDetails = page.getByRole("dialog", { name: "Skill 详情" });
        await expect(skillDetails).toBeVisible();
        await expect(
            skillDetails.getByRole("heading", {
                name: "合同审查规范",
                level: 4,
            }),
        ).toBeVisible();
        await expect(
            skillDetails.getByText("SKILL.md", { exact: true }),
        ).toBeVisible();
        await expect(
            skillDetails.getByText("References", { exact: true }),
        ).toBeVisible();
        await expect(
            skillDetails.getByText("脚本已保存，当前版本不会执行"),
        ).toHaveCount(0);
        const skillMarkdown = skillDetails.locator(
            ".inkwell-skill-details-markdown",
        );
        await expect(skillMarkdown).toHaveClass(/collapsed/);
        const collapsedHeight = await skillMarkdown.evaluate(
            (element) => element.getBoundingClientRect().height,
        );
        await expect(skillDetails.locator("input, textarea")).toHaveCount(0);
        await expect(
            skillDetails.getByRole("button", { name: "编辑" }),
        ).toHaveCount(0);
        await expect(
            skillDetails.getByRole("button", { name: "删除 Skill" }),
        ).toHaveCount(0);
        await page.waitForTimeout(400);
        await page.screenshot({
            path: screenshotPath("28-skill-readonly-details.png"),
        });

        await skillDetails.getByRole("button", { name: "展开全文" }).click();
        await expect(skillMarkdown).toHaveClass(/expanded/);
        await expect(
            skillDetails.getByRole("button", { name: "收起" }),
        ).toBeVisible();
        await page.waitForTimeout(220);
        const expandedHeight = await skillMarkdown.evaluate(
            (element) => element.getBoundingClientRect().height,
        );
        expect(expandedHeight).toBeGreaterThan(collapsedHeight);
        await page.screenshot({
            path: screenshotPath("29-skill-long-content-expanded.png"),
        });

        await skillDetails
            .getByRole("button", { name: "关闭 Skill 详情" })
            .click();
        await firstSkillRow.getByRole("button", { name: /^编辑 / }).click();
        const skillEditor = page.getByRole("dialog", { name: "编辑 Skill" });
        await expect(skillEditor.locator("input")).toHaveCount(1);
        await expect(skillEditor.locator("textarea")).toHaveCount(2);
    });

    test("unifies list baselines, actions, and Agent binding editors", async ({
        page,
    }, testInfo) => {
        test.skip(testInfo.project.name !== "desktop-hd");
        await page.goto("/shell");
        await waitForRender(page);

        const firstAgent = page.locator(".inkwell-agent-card").first();
        await firstAgent.hover();
        const firstAgentAction = firstAgent.getByRole("button", {
            name: /^编辑 /,
        });
        await expect(firstAgentAction).toBeVisible();
        await expect(firstAgentAction).toHaveCSS("border-style", "solid");
        await expect(firstAgentAction).toHaveCSS("width", "28px");
        await expect(firstAgentAction).toHaveText("");
        await expect(firstAgentAction.locator(".anticon")).toHaveCount(1);
        const agentPagination = page.locator(".inkwell-agent-pagination");
        const agentPaginationY = await agentPagination.evaluate(
            (element) => element.getBoundingClientRect().y,
        );
        await page.getByPlaceholder("搜索 Agent").fill("市场洞察 1");
        await expect(page.getByText("共 1 项", { exact: true })).toBeVisible();
        const filteredAgentPaginationY = await agentPagination.evaluate(
            (element) => element.getBoundingClientRect().y,
        );
        expect(filteredAgentPaginationY).toBeCloseTo(agentPaginationY, 0);
        await page.getByPlaceholder("搜索 Agent").clear();
        await firstAgent.hover();
        await page.screenshot({
            path: screenshotPath("20-agent-space-unified-list.png"),
            fullPage: true,
        });

        await firstAgent.getByRole("button", { name: /^编辑 / }).click();
        await page.getByRole("button", { name: "Instructions" }).click();
        const instructionsEditor = page.locator(
            ".inkwell-instructions-editor .monaco-editor",
        );
        await expect(instructionsEditor).toBeVisible();
        await expect(instructionsEditor).toHaveCSS("height", "480px");
        await expect(
            page.locator(".inkwell-instructions-editor textarea"),
        ).toHaveCSS("resize", "none");
        await page.screenshot({
            path: screenshotPath("21-agent-instructions-monaco.png"),
            fullPage: true,
        });

        await page.getByRole("button", { name: /工具$/ }).click();
        await expect(page.locator(".inkwell-binding-item")).toHaveCount(3);
        await page.screenshot({
            path: screenshotPath("22-agent-tool-bindings.png"),
            fullPage: true,
        });

        await page.getByRole("button", { name: /Skills$/ }).click();
        await expect(page.locator(".inkwell-binding-item")).toHaveCount(2);
        await page.screenshot({
            path: screenshotPath("23-agent-skill-bindings.png"),
            fullPage: true,
        });

        await page.goto("/shell");
        await page.getByText("工具", { exact: true }).first().click();
        const pagination = page.locator(".inkwell-resource-pagination");
        const paginationY = await pagination.evaluate(
            (element) => element.getBoundingClientRect().y,
        );
        await page.getByPlaceholder("搜索名称或描述").fill("weather_forecast");
        await expect(page.getByText("共 1 项", { exact: true })).toBeVisible();
        const filteredPaginationY = await pagination.evaluate(
            (element) => element.getBoundingClientRect().y,
        );
        expect(filteredPaginationY).toBeCloseTo(paginationY, 0);
        await page.getByPlaceholder("搜索名称或描述").clear();
        await page.screenshot({
            path: screenshotPath("24-tool-list-unified.png"),
            fullPage: true,
        });

        await page.getByText("Skills", { exact: true }).first().click();
        await expect(page.getByText(/共 \d+ 项/)).toBeVisible();
        await page.screenshot({
            path: screenshotPath("25-skill-list-unified.png"),
            fullPage: true,
        });

        await page.getByText("模型", { exact: true }).first().click();
        await expect(page.getByText(/共 \d+ 项/)).toBeVisible();
        await page.screenshot({
            path: screenshotPath("26-model-list-unified.png"),
            fullPage: true,
        });

        await page.getByText("用户管理", { exact: true }).first().click();
        await expect(page.getByText(/共 \d+ 项/)).toBeVisible();
        await page.screenshot({
            path: screenshotPath("27-user-list-unified.png"),
            fullPage: true,
        });
    });

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
                origin: "http://localhost:4195",
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
        const versionAgentCard = page.locator(".inkwell-agent-card").filter({
            hasText: "客服助手",
        });
        await versionAgentCard.hover();
        await versionAgentCard.getByRole("button", { name: /^编辑 / }).click();
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
        const trialAgentCard = page.locator(".inkwell-agent-card").filter({
            hasText: "客服助手",
        });
        await trialAgentCard.hover();
        await trialAgentCard.getByRole("button", { name: /^编辑 / }).click();
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
        const toolCallAgentCard = page
            .locator(".inkwell-agent-card")
            .filter({ hasText: "客服助手" });
        await toolCallAgentCard.hover();
        await toolCallAgentCard.getByRole("button", { name: /^编辑 / }).click();
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
