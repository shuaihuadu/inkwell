import { _electron as electron, expect, test } from "@playwright/test";
import electronPath from "electron";
import { createServer } from "node:http";
import {
    applicationEntry,
    myAgentsResponse,
    sharedAgentsResponse,
    toolsResponse,
} from "./fixtures/test-data.js";

test("hides system administration navigation from regular users", async ({
    browserName,
}, testInfo) => {
    let memberModelTestAttempts = 0;
    let memberModelManagementAttempts = 0;
    const server = createServer((request, response) => {
        if (request.url === "/healthz") {
            response.setHeader("Content-Type", "application/json");
            response.end(JSON.stringify({ status: "healthy" }));
            return;
        }

        if (request.url === "/api/auth/login") {
            response.setHeader("Content-Type", "application/json");
            response.end(
                JSON.stringify({
                    userId: "0198a96d-19e4-7000-8000-000000000002",
                    username: "member",
                    isAdmin: false,
                    sessionToken: "member-session-token",
                    expiresAt: "2026-07-15T00:00:00Z",
                }),
            );
            return;
        }

        if (request.url === "/api/agents/mine") {
            response.setHeader("Content-Type", "application/json");
            response.end(myAgentsResponse);
            return;
        }

        if (request.url === "/api/agents/shared") {
            response.setHeader("Content-Type", "application/json");
            response.end(sharedAgentsResponse);
            return;
        }

        if (request.url === "/api/tools") {
            response.setHeader("Content-Type", "application/json");
            response.end(toolsResponse);
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
            page.getByRole("heading", { name: "Agent 空间" }),
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
