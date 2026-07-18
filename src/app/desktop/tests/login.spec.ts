import { _electron as electron, expect, test } from "@playwright/test";
import electronPath from "electron";
import { createServer } from "node:http";

const applicationEntry = "out/main/index.js";

test("renders the prototype-aligned login experience", async ({
    browserName,
}, testInfo) => {
    const application = await electron.launch({
        executablePath: electronPath as unknown as string,
        args: [
            applicationEntry,
            `--user-data-dir=${testInfo.outputPath(`${browserName}-user-data`)}`,
        ],
        env: {
            ...process.env,
            INKWELL_WEBAPI_URL: "http://127.0.0.1:1",
        },
    });

    try {
        const page = await application.firstWindow();
        await expect(
            page.getByRole("heading", { name: "Inkwell", exact: true }),
        ).toBeVisible();
        await expect(
            page.getByRole("heading", { name: "Inkwell Agent 平台" }),
        ).toBeVisible();
        const logo = page.locator(".login-heading img");
        await expect(logo).toBeVisible();
        expect(
            await logo.evaluate(
                (element) => (element as HTMLImageElement).naturalWidth,
            ),
        ).toBeGreaterThan(0);
        await expect(page.getByPlaceholder("请输入账号")).toHaveValue("");
        await expect(page.getByPlaceholder("请输入密码")).toHaveValue("");
        await expect(
            page.locator(".login-form .ant-form-item-label"),
        ).toHaveCount(0);
        await expect(page.locator('link[rel="icon"]')).toHaveAttribute(
            "href",
            "./logo.svg",
        );
        await expect(
            page.getByRole("button", { name: /登\s*录/ }),
        ).toBeEnabled();
        await expect(
            page.getByText("如忘记密码或需要开通账号，请联系系统管理员"),
        ).toBeVisible();

        const brandBox = await page.locator(".login-brand").boundingBox();
        const brandTitleBox = await page
            .getByRole("heading", { name: "Inkwell", exact: true })
            .boundingBox();
        const formBox = await page.locator(".login-form-wrap").boundingBox();
        expect(brandBox).not.toBeNull();
        expect(brandTitleBox).not.toBeNull();
        expect(formBox).not.toBeNull();
        expect(brandBox!.x + brandBox!.width).toBeLessThanOrEqual(formBox!.x);
        expect(
            Math.abs(
                brandTitleBox!.y +
                    brandTitleBox!.height / 2 -
                    (brandBox!.y + brandBox!.height / 2),
            ),
        ).toBeLessThanOrEqual(1);
        expect(formBox!.width).toBeLessThanOrEqual(360);

        await page.screenshot({
            path: testInfo.outputPath("login.png"),
            fullPage: true,
        });
    } finally {
        await application.close();
    }
});

test("shows authentication errors and enters the workspace after login", async ({
    browserName,
}, testInfo) => {
    let loginAttempts = 0;
    let modelTestAttempts = 0;
    let accountLocked = true;
    let accountUnlockAttempts = 0;
    const server = createServer((request, response) => {
        if (request.url === "/api/auth/login") {
            loginAttempts += 1;
            response.setHeader("Content-Type", "application/json");

            if (loginAttempts === 1) {
                response.statusCode = 401;
                response.end(
                    JSON.stringify({ detail: "Invalid username or password." }),
                );
                return;
            }

            response.end(
                JSON.stringify({
                    userId: "0198a96d-19e4-7000-8000-000000000001",
                    username: "admin",
                    isSuper: true,
                    sessionToken: "test-session-token",
                    expiresAt: "2026-07-15T00:00:00Z",
                }),
            );
            return;
        }

        if (request.url === "/api/agents/mine") {
            response.setHeader("Content-Type", "application/json");
            response.end("[]");
            return;
        }

        if (request.url === "/api/models") {
            response.setHeader("Content-Type", "application/json");
            response.end(
                JSON.stringify([
                    {
                        id: "gpt-5.4",
                        category: "Chat",
                        providerMode: "chat",
                        ownedBy: "openai",
                        maxInputTokens: 1_050_000,
                        maxOutputTokens: 128_000,
                        supportsVision: true,
                        supportsTools: true,
                        supportsStructuredOutput: true,
                        supportsReasoning: true,
                    },
                    {
                        id: "text-embedding-3-large",
                        category: "Embedding",
                        providerMode: "embedding",
                        ownedBy: "openai",
                        maxInputTokens: 8_191,
                        maxOutputTokens: null,
                        supportsVision: null,
                        supportsTools: null,
                        supportsStructuredOutput: null,
                        supportsReasoning: null,
                    },
                ]),
            );
            return;
        }

        if (request.url === "/api/models/management") {
            response.setHeader("Content-Type", "application/json");
            response.end(
                JSON.stringify({ dashboardUrl: "https://litellm.example/" }),
            );
            return;
        }

        if (
            request.url === "/api/models/gpt-5.4/test" &&
            request.method === "POST"
        ) {
            modelTestAttempts += 1;
            response.setHeader("Content-Type", "application/json");
            response.end(
                JSON.stringify({
                    modelId: "gpt-5.4",
                    isSuccess: true,
                    latency: "00:00:00.1250000",
                    errorMessage: null,
                }),
            );
            return;
        }

        if (request.url === "/api/auth/accounts") {
            response.setHeader("Content-Type", "application/json");
            response.end(
                JSON.stringify([
                    {
                        userId: "0198a96d-19e4-7000-8000-000000000001",
                        username: "admin",
                        isSuper: true,
                        isLocked: false,
                        lastLoginTime: "2026-07-18T14:56:00Z",
                        createdTime: "2026-05-01T09:00:00Z",
                    },
                    {
                        userId: "0198a96d-19e4-7000-8000-000000000002",
                        username: "bob",
                        isSuper: false,
                        isLocked: accountLocked,
                        lastLoginTime: "2026-07-17T18:20:00Z",
                        createdTime: "2026-05-03T10:15:00Z",
                    },
                ]),
            );
            return;
        }

        if (
            request.url ===
                "/api/auth/accounts/0198a96d-19e4-7000-8000-000000000002/unlock" &&
            request.method === "POST"
        ) {
            accountUnlockAttempts += 1;
            accountLocked = false;
            response.statusCode = 204;
            response.end();
            return;
        }

        if (request.url === "/api/auth/unlock") {
            response.statusCode = 204;
            response.end();
            return;
        }

        response.statusCode = 404;
        response.end();
    });
    await new Promise<void>((resolve) =>
        server.listen(0, "127.0.0.1", resolve),
    );
    const address = server.address();
    if (!address || typeof address === "string")
        throw new Error("Test server did not bind a TCP port.");

    const application = await electron.launch({
        executablePath: electronPath as unknown as string,
        args: [
            applicationEntry,
            `--user-data-dir=${testInfo.outputPath(`${browserName}-user-data`)}`,
        ],
        env: {
            ...process.env,
            INKWELL_WEBAPI_URL: `http://127.0.0.1:${address.port}`,
        },
    });

    try {
        const page = await application.firstWindow();
        const username = page.getByPlaceholder("请输入账号");
        const password = page.getByPlaceholder("请输入密码");
        const submit = page.getByRole("button", { name: /登\s*录/ });

        await username.fill("admin");
        await password.fill("wrong-password");
        await submit.click();
        await expect(page.getByText("账号或密码错误，请重试")).toBeVisible();
        await expect(username).toHaveValue("admin");
        await expect(password).toHaveValue("");
        await expect(password).toBeFocused();

        await password.fill("correct-password");
        await submit.click();
        await expect(
            page.getByRole("heading", { name: "Agent 库" }),
        ).toBeVisible();
        await expect(page.getByText("工作区", { exact: true })).toBeVisible();
        await expect(page.getByText("资源中心", { exact: true })).toBeVisible();
        await expect(page.getByText("系统管理", { exact: true })).toBeVisible();
        await expect(
            page.getByRole("button", { name: "用户管理" }),
        ).toBeVisible();

        await page.getByRole("button", { name: "关于 Inkwell" }).click();
        await expect(
            page.getByRole("heading", { name: "Inkwell", exact: true }),
        ).toBeVisible();
        await expect(page.getByText("版本", { exact: true })).toBeVisible();
        await expect(page.getByTestId("app-version")).not.toHaveText("-");
        const qrCode = page.getByRole("img", { name: "公众号二维码" });
        await expect(qrCode).toBeVisible();
        expect(
            await qrCode.evaluate(
                (element) => (element as HTMLImageElement).naturalWidth,
            ),
        ).toBeGreaterThan(0);
        await page.keyboard.press("Escape");

        await page
            .getByRole("button", { name: "打开用户菜单" })
            .dispatchEvent("click");
        await expect(page.getByText("个人设置", { exact: true })).toBeVisible();
        await expect(page.getByText("管理", { exact: true })).toBeVisible();
        await page
            .getByText("个人设置", { exact: true })
            .dispatchEvent("click");
        await expect(
            page.getByRole("dialog", { name: "个人设置" }),
        ).toBeVisible();
        await expect(page.getByText("曜石紫", { exact: true })).toBeVisible();
        await expect(page.getByText("朱砂橙", { exact: true })).toBeVisible();
        await expect(page.getByText("碧海青", { exact: true })).toBeVisible();
        await page.getByText("亮色", { exact: true }).dispatchEvent("click");
        await page.getByText("朱砂橙", { exact: true }).dispatchEvent("click");
        await page.keyboard.press("Escape");
        await expect(page.locator("html")).toHaveAttribute(
            "data-theme",
            "terracotta",
        );
        await expect(page.locator("html")).toHaveAttribute(
            "data-appearance",
            "light",
        );

        const appearanceSwitch = page.getByRole("switch", { name: "切换外观" });
        await appearanceSwitch.dispatchEvent("click");
        await expect(page.locator("html")).toHaveAttribute(
            "data-appearance",
            "dark",
        );
        await expect(page.locator(".library-pane")).toHaveCSS(
            "background-color",
            "rgb(23, 20, 19)",
        );

        await page
            .locator(".app-sidebar .nav-item", { hasText: "模型" })
            .dispatchEvent("click");
        await expect(
            page.getByRole("heading", { name: "模型", exact: true }),
        ).toBeVisible();
        await expect(
            page
                .locator(".inkwell-data-list-header")
                .getByRole("button", { name: "模型管理" }),
        ).toBeEnabled();
        await expect(page.getByText("gpt-5.4", { exact: true })).toBeVisible();
        await expect(
            page.getByText("text-embedding-3-large", { exact: true }),
        ).toBeVisible();
        const modelTable = page.getByRole("table");
        for (const column of [
            "模型标识",
            "模型类型",
            "提供方",
            "Token 上限",
            "视觉",
            "工具",
            "结构化",
            "推理",
            "连通性",
        ]) {
            await expect(
                modelTable.getByRole("columnheader", { name: column }),
            ).toBeVisible();
        }
        await expect(modelTable.getByText("对话", { exact: true })).toBeVisible();
        await expect(modelTable.getByText("嵌入", { exact: true })).toBeVisible();
        const listCapabilityTag = modelTable.locator(".ant-tag-success").first();
        await expect(listCapabilityTag).toHaveCSS(
            "background-color",
            "rgb(32, 43, 36)",
        );
        await expect(listCapabilityTag).toHaveCSS("border-radius", "6px");
        await expect(listCapabilityTag).toHaveCSS("font-size", "12px");
        await expect(listCapabilityTag).toHaveCSS("line-height", "20px");
        await page
            .getByRole("button", { name: "gpt-5.4", exact: true })
            .dispatchEvent("click");
        const modelDetails = page.getByRole("dialog", { name: "模型详情" });
        await expect(modelDetails).toBeVisible();
        await expect(
            modelDetails.getByText(
                /最大输入 1,050,000 个令牌，最大输出 128,000 个令牌/,
            ),
        ).toBeVisible();
        await expect(
            modelDetails.locator(".ant-tag-success").first(),
        ).toHaveCSS("background-color", "rgb(32, 43, 36)");
        await modelDetails
            .locator(".ant-drawer-close")
            .dispatchEvent("click");
        await expect(modelDetails).toBeHidden();
        await page
            .getByRole("button", { name: "测试 gpt-5.4" })
            .dispatchEvent("click");
        await expect(
            page.getByText("gpt-5.4 对话最小请求成功 · 125 ms"),
        ).toBeVisible();
        expect(modelTestAttempts).toBe(1);
        await expect(page.locator(".ant-dropdown")).toBeHidden();
        await page.setViewportSize({ width: 1080, height: 720 });
        expect(
            await page.evaluate(() => document.documentElement.scrollWidth),
        ).toBeLessThanOrEqual(1080);
        await page.screenshot({
            path: testInfo.outputPath("model-management-dark-1080x720.png"),
            fullPage: true,
        });

        await page
            .getByRole("button", { name: "用户管理" })
            .dispatchEvent("click");
        await expect(
            page.getByRole("heading", { name: "用户管理" }),
        ).toBeVisible();
        await expect(page.getByText("bob", { exact: true })).toBeVisible();
        await expect(page.getByText("已锁定", { exact: true })).toBeVisible();
        await page
            .getByRole("button", { name: "解封 bob" })
            .dispatchEvent("click");
        await expect(
            page.getByRole("dialog", { name: "解封账号 bob" }),
        ).toBeVisible();
        await page
            .getByRole("button", { name: "确认解封" })
            .dispatchEvent("click");
        await expect(page.getByText("bob 已解封")).toBeVisible();
        expect(accountUnlockAttempts).toBe(1);
        await expect(page.getByText("正常", { exact: true })).toHaveCount(2);

        await page.getByRole("button", { name: /工具管理/ }).click();
        await expect(
            page.getByRole("heading", { name: "工具管理" }),
        ).toBeVisible();
        await expect(page.getByText("即将上线", { exact: true })).toBeVisible();

        await page.getByRole("button", { name: "Agent 空间" }).click();
        await expect(
            page.getByRole("heading", { name: "Agent 库" }),
        ).toBeVisible();
        await page.setViewportSize({ width: 1080, height: 720 });
        expect(
            await page.evaluate(() => document.documentElement.scrollWidth),
        ).toBeLessThanOrEqual(1080);
        await page.screenshot({
            path: testInfo.outputPath("workspace-dark-1080x720.png"),
            fullPage: true,
        });

        await application.evaluate(({ app }) => {
            app.emit("browser-window-blur", {} as never, null as never);
        });
        await expect(
            page.getByRole("heading", { name: "Agent 库" }),
        ).toBeVisible();
        await expect(
            page.getByRole("heading", { name: "Inkwell 已锁定" }),
        ).toHaveCount(0);

        await application.evaluate(({ powerMonitor }) => {
            powerMonitor.emit("lock-screen");
        });
        await expect(
            page.getByRole("heading", { name: "Inkwell 已锁定" }),
        ).toBeVisible();
        await expect(
            page.getByText("admin，请输入密码继续", { exact: true }),
        ).toBeVisible();
        await expect(
            page.getByRole("button", { name: "切换账号" }),
        ).toBeVisible();
        await expect(page.getByRole("button", { name: "登出" })).toBeVisible();
        await page.screenshot({
            path: testInfo.outputPath("lock-dark-1080x720.png"),
            fullPage: true,
        });
        await page.getByPlaceholder("密码").fill("correct-password");
        await page.keyboard.press("Enter");
        await expect(
            page.getByRole("heading", { name: "Agent 库" }),
        ).toBeVisible();
    } finally {
        await application.close();
        await new Promise<void>((resolve, reject) => {
            server.close((error) => (error ? reject(error) : resolve()));
        });
    }
});

test("hides system administration navigation from regular users", async ({
    browserName,
}, testInfo) => {
    let memberModelTestAttempts = 0;
    let memberModelManagementAttempts = 0;
    const server = createServer((request, response) => {
        if (request.url === "/api/auth/login") {
            response.setHeader("Content-Type", "application/json");
            response.end(
                JSON.stringify({
                    userId: "0198a96d-19e4-7000-8000-000000000002",
                    username: "member",
                    isSuper: false,
                    sessionToken: "member-session-token",
                    expiresAt: "2026-07-15T00:00:00Z",
                }),
            );
            return;
        }

        if (request.url === "/api/agents/mine") {
            response.setHeader("Content-Type", "application/json");
            response.end("[]");
            return;
        }

        if (request.url === "/api/models") {
            response.setHeader("Content-Type", "application/json");
            response.end(
                JSON.stringify([
                    {
                        id: "gpt-5.4",
                        category: "Chat",
                        providerMode: "chat",
                        ownedBy: "openai",
                        maxInputTokens: 1_050_000,
                        maxOutputTokens: 128_000,
                        supportsVision: true,
                        supportsTools: true,
                        supportsStructuredOutput: true,
                        supportsReasoning: true,
                    },
                ]),
            );
            return;
        }

        if (request.url === "/api/models/management") {
            memberModelManagementAttempts += 1;
            response.setHeader("Content-Type", "application/json");
            response.end(JSON.stringify({ dashboardUrl: null }));
            return;
        }

        if (
            request.url === "/api/models/gpt-5.4/test" &&
            request.method === "POST"
        ) {
            memberModelTestAttempts += 1;
            response.setHeader("Content-Type", "application/json");
            response.end(
                JSON.stringify({
                    modelId: "gpt-5.4",
                    isSuccess: true,
                    latency: "00:00:00.1250000",
                    errorMessage: null,
                }),
            );
            return;
        }

        response.statusCode = 404;
        response.end();
    });
    await new Promise<void>((resolve) =>
        server.listen(0, "127.0.0.1", resolve),
    );
    const address = server.address();
    if (!address || typeof address === "string")
        throw new Error("Test server did not bind a TCP port.");

    const application = await electron.launch({
        executablePath: electronPath as unknown as string,
        args: [
            applicationEntry,
            `--user-data-dir=${testInfo.outputPath(`${browserName}-user-data`)}`,
        ],
        env: {
            ...process.env,
            INKWELL_WEBAPI_URL: `http://127.0.0.1:${address.port}`,
        },
    });

    try {
        const page = await application.firstWindow();
        await page.getByPlaceholder("请输入账号").fill("member");
        await page.getByPlaceholder("请输入密码").fill("correct-password");
        await page.getByRole("button", { name: /登\s*录/ }).click();

        await expect(
            page.getByRole("heading", { name: "Agent 库" }),
        ).toBeVisible();
        await expect(page.getByText("系统管理", { exact: true })).toHaveCount(
            0,
        );
        await expect(
            page.getByRole("button", { name: "用户管理" }),
        ).toHaveCount(0);
        await expect(page.getByText("资源中心", { exact: true })).toBeVisible();
        await page
            .locator(".app-sidebar .nav-item", { hasText: "模型" })
            .dispatchEvent("click");
        await expect(
            page.getByRole("button", { name: "模型管理" }),
        ).toHaveCount(0);
        expect(memberModelManagementAttempts).toBe(0);
        await page
            .getByRole("button", { name: "测试 gpt-5.4" })
            .dispatchEvent("click");
        await expect(
            page.getByText("gpt-5.4 对话最小请求成功 · 125 ms"),
        ).toBeVisible();
        expect(memberModelTestAttempts).toBe(1);
        await page
            .getByRole("button", { name: "打开用户菜单" })
            .dispatchEvent("click");
        await expect(page.getByText("个人设置", { exact: true })).toBeVisible();
        await expect(page.getByText("管理", { exact: true })).toHaveCount(0);
    } finally {
        await application.close();
        await new Promise<void>((resolve, reject) => {
            server.close((error) => (error ? reject(error) : resolve()));
        });
    }
});
