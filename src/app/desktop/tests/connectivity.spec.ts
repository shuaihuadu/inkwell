import { _electron as electron, expect, test } from "@playwright/test";
import electronPath from "electron";
import { createServer } from "node:http";
import type { InkwellDesktopApi } from "../src/shared/network/contracts.js";
import {
    applicationEntry,
    myAgentsResponse,
    sharedAgentsResponse,
    toolsResponse,
} from "./fixtures/test-data.js";

test("handles service disconnects and global API errors", async ({
    browserName,
}, testInfo) => {
    test.setTimeout(45_000);
    let healthy = true;
    let toolsStatus = 200;
    let createAgentRequests = 0;
    const server = createServer((request, response) => {
        if (request.url === "/healthz") {
            response.statusCode = healthy ? 200 : 503;
            response.end();
            return;
        }

        if (request.url === "/api/auth/login") {
            response.setHeader("Content-Type", "application/json");
            response.end(
                JSON.stringify({
                    userId: "0198a96d-19e4-7000-8000-000000000001",
                    username: "admin",
                    isAdmin: true,
                    mustChangePassword: false,
                    sessionToken: "network-test-session-token",
                    expiresAt: "2026-08-26T00:00:00Z",
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

        if (request.url === "/api/agents" && request.method === "POST") {
            createAgentRequests += 1;
            response.statusCode = 201;
            response.end();
            return;
        }

        if (request.url === "/api/tools") {
            response.statusCode = toolsStatus;
            response.setHeader("Content-Type", "application/json");
            if (toolsStatus === 429) response.setHeader("Retry-After", "7");
            response.end(
                toolsStatus === 200
                    ? toolsResponse
                    : JSON.stringify({ detail: "sensitive backend detail" }),
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
        await expect(
            page.getByRole("button", { name: /登\s*录/ }),
        ).toBeEnabled();
        await page.getByPlaceholder("请输入账号").fill("admin");
        await page.getByPlaceholder("请输入密码").fill("password");
        await page.getByRole("button", { name: /登\s*录/ }).click();
        await expect(page.getByText("后台服务正常")).toBeVisible();

        healthy = false;
        await expect(page.getByText("后台服务异常")).toBeVisible({
            timeout: 40_000,
        });
        await expect(page.getByText(/网络连接已断开/)).toBeVisible();

        const writeRejected = await page.evaluate(async () => {
            const desktopApi = (
                globalThis as unknown as { inkwell: InkwellDesktopApi }
            ).inkwell;
            try {
                await desktopApi.createAgent({
                    name: "offline agent",
                    avatarUri: null,
                    description: null,
                    instructions: null,
                    modelOptions: {
                        modelId: null,
                        temperature: null,
                        topP: null,
                        maxTokens: null,
                    },
                    chatHistoryOptions: null,
                    toolBindings: [],
                    skillBindings: [],
                });
                return false;
            } catch {
                return true;
            }
        });
        expect(writeRejected).toBe(true);
        expect(createAgentRequests).toBe(0);

        healthy = true;
        await expect(page.getByText("后台服务正常")).toBeVisible({
            timeout: 10_000,
        });
        await expect(page.getByText(/网络连接已断开/)).toHaveCount(0);

        toolsStatus = 429;
        await page.evaluate(() =>
            (globalThis as unknown as { inkwell: InkwellDesktopApi }).inkwell
                .listTools()
                .catch(() => []),
        );
        await expect(
            page.getByText("操作过于频繁，请在 7 秒后重试"),
        ).toBeVisible();

        toolsStatus = 500;
        await page.evaluate(() =>
            (globalThis as unknown as { inkwell: InkwellDesktopApi }).inkwell
                .listTools()
                .catch(() => []),
        );
        await expect(
            page.getByText("后台服务暂时不可用，请稍后重试"),
        ).toBeVisible();
        await expect(page.getByText("sensitive backend detail")).toHaveCount(0);

        toolsStatus = 401;
        await page.evaluate(() =>
            (globalThis as unknown as { inkwell: InkwellDesktopApi }).inkwell
                .listTools()
                .catch(() => []),
        );
        await expect(
            page.getByRole("button", { name: /登\s*录/ }),
        ).toBeVisible();
        await expect(
            page.getByText("后台服务暂时不可用，请稍后重试"),
        ).toHaveCount(0);
    } finally {
        await application.close();
        await new Promise<void>((resolve, reject) =>
            server.close((error) => (error ? reject(error) : resolve())),
        );
    }
});
