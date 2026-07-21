import { _electron as electron, expect, test } from "@playwright/test";
import electronPath from "electron";
import { createServer } from "node:http";

const applicationEntry = "out/main/index.js";
const toolsResponse = JSON.stringify([
    {
        id: "0198a96d-19e4-7000-8000-000000000101",
        name: "current_date_time",
        description: "返回指定时区的当前日期和时间。",
        parametersJsonSchema: JSON.stringify({
            type: "object",
            required: ["timeZone"],
            properties: {
                timeZone: {
                    type: "string",
                    enum: ["UTC", "Asia/Shanghai"],
                },
                format: { type: "string" },
            },
        }),
        createdTime: "2026-07-17T08:00:00Z",
        updatedTime: "2026-07-18T08:42:00Z",
    },
]);
const skillsResponse = JSON.stringify([
    {
        id: "0198a96d-19e4-7000-8000-000000000201",
        ownerUserId: "0198a96d-19e4-7000-8000-000000000001",
        name: "合同审查规范",
        description: "按团队法务标准识别合同风险并输出分级建议。",
        content: "# 合同审查规范\n\n先识别合同类型，再输出风险。",
        referenceFileUris: ["inkwell://skills/references/rule.md"],
        assetFileUris: ["inkwell://skills/assets/template.docx"],
        scriptFileUris: ["inkwell://skills/scripts/check.ps1"],
        createdTime: "2026-07-17T08:00:00Z",
        updatedTime: "2026-07-18T09:20:00Z",
    },
    {
        id: "0198a96d-19e4-7000-8000-000000000202",
        ownerUserId: "0198a96d-19e4-7000-8000-000000000002",
        name: "研发周报",
        description: "将工作记录整理为统一的研发周报格式。",
        content: "# 研发周报",
        referenceFileUris: [],
        assetFileUris: [],
        scriptFileUris: [],
        createdTime: "2026-07-16T08:00:00Z",
        updatedTime: "2026-07-17T02:08:00Z",
    },
]);
const myAgentsResponse = JSON.stringify([
    {
        id: "0198a96d-19e4-7000-8000-000000000301",
        name: "研发助手",
        avatarUri: null,
        descriptionExcerpt: "帮助团队分析代码并整理研发任务。",
        ownerUserId: "0198a96d-19e4-7000-8000-000000000001",
        isShared: true,
        latestPublishedVersionNumber: 3,
        updatedTime: "2026-07-18T12:00:00Z",
    },
    {
        id: "0198a96d-19e4-7000-8000-000000000302",
        name: "产品草稿",
        avatarUri: null,
        descriptionExcerpt: "尚未发布的产品分析 Agent。",
        ownerUserId: "0198a96d-19e4-7000-8000-000000000001",
        isShared: false,
        latestPublishedVersionNumber: 0,
        updatedTime: "2026-07-18T13:00:00Z",
    },
]);
const sharedAgentsResponse = JSON.stringify([
    {
        id: "0198a96d-19e4-7000-8000-000000000303",
        name: "合同审查助手",
        avatarUri: null,
        descriptionExcerpt: "识别合同风险并输出分级建议。",
        ownerUserId: "0198a96d-19e4-7000-8000-000000000002",
        isShared: true,
        latestPublishedVersionNumber: 2,
        updatedTime: "2026-07-17T10:00:00Z",
    },
]);
const editableAgent = {
    id: "0198a96d-19e4-7000-8000-000000000304",
    ownerUserId: "0198a96d-19e4-7000-8000-000000000001",
    name: "发布助手",
    avatarUri: null,
    description: "整理发布内容。",
    instructions: "输出简洁的发布说明。",
    buildOptions: {
        modelOptions: {
            modelId: "gpt-5.4",
            temperature: 0.7,
            topP: null,
            maxTokens: null,
        },
        chatHistoryOptions: {
            maxMessages: 40,
            reducerType: null,
            maxMessagesToRetrieve: null,
        },
        toolBindings: [],
        skills: [],
    },
    currentPublishedVersionId: null,
    latestPublishedVersionNumber: 0,
    isShared: false,
    sharedRevokedByAdminTime: null,
    createdTime: "2026-07-19T00:00:00Z",
    updatedTime: "2026-07-19T00:00:00Z",
};
const editableAgentWithoutBindingCollections = {
    ...editableAgent,
    buildOptions: {
        modelOptions: editableAgent.buildOptions.modelOptions,
        chatHistoryOptions: editableAgent.buildOptions.chatHistoryOptions,
    },
};
const publishedAgent = {
    ...editableAgent,
    id: "0198a96d-19e4-7000-8000-000000000301",
    name: "研发助手",
    currentPublishedVersionId: "0198a96d-19e4-7000-8000-000000000305",
    latestPublishedVersionNumber: 3,
};
const sharedAgent = {
    ...editableAgent,
    id: "0198a96d-19e4-7000-8000-000000000303",
    ownerUserId: "0198a96d-19e4-7000-8000-000000000002",
    name: "合同审查助手",
    description: "识别合同风险并输出分级建议。",
    currentPublishedVersionId: "0198a96d-19e4-7000-8000-000000000306",
    latestPublishedVersionNumber: 2,
    isShared: true,
};
const clonedAgent = {
    ...sharedAgent,
    id: "0198a96d-19e4-7000-8000-000000000307",
    ownerUserId: "0198a96d-19e4-7000-8000-000000000001",
    name: "合同审查助手（副本）",
    currentPublishedVersionId: null,
    latestPublishedVersionNumber: 0,
    isShared: false,
};

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
    test.setTimeout(60_000);
    let loginAttempts = 0;
    let modelTestAttempts = 0;
    let accountLocked = true;
    let accountUnlockAttempts = 0;
    let agentShareRevocations = 0;
    let agentClones = 0;
    let agentCreates = 0;
    let agentUpdates = 0;
    let agentPublishes = 0;
    let agentAvatarUploads = 0;
    let agentShares = 0;
    const chatRequestUrls: string[] = [];
    const chatRunModes: (string | undefined)[] = [];
    const chatConversationIds: (string | undefined)[] = [];
    const conversationId = "0198a96d-19e4-7000-8000-000000000401";
    let conversationCreated = false;
    let persistedConversationMessages: Array<Record<string, unknown>> = [];
    const capturedPayloads: {
        agentCreate?: Record<string, unknown>;
        agentUpdate?: Record<string, unknown>;
        agentPublish?: Record<string, unknown>;
    } = {};
    const uploadedAvatarUri =
        "inkwell://agent-avatars/0198a96d19e470008000000000000001/avatar.png";
    const avatarBytes = Buffer.from(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=",
        "base64",
    );
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
                    isAdmin: true,
                    mustChangePassword: false,
                    sessionToken: "test-session-token",
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

        if (request.url === `/api/agents/${publishedAgent.id}`) {
            response.setHeader("Content-Type", "application/json");
            response.end(JSON.stringify(publishedAgent));
            return;
        }

        if (
            request.url ===
                `/api/agents/${publishedAgent.id}/conversations?page=1&pageSize=100` &&
            request.method === "GET"
        ) {
            response.setHeader("Content-Type", "application/json");
            response.end(
                JSON.stringify({
                    items: conversationCreated
                        ? [
                              {
                                  id: conversationId,
                                  agentVersionId:
                                      publishedAgent.currentPublishedVersionId,
                                  title:
                                      persistedConversationMessages.length > 0
                                          ? "验证正式发布版"
                                          : null,
                                  lastActivityTime: "2026-07-20T07:00:00Z",
                                  createdTime: "2026-07-20T07:00:00Z",
                              },
                          ]
                        : [],
                    totalCount: conversationCreated ? 1 : 0,
                    pagination: { page: 1, pageSize: 100 },
                }),
            );
            return;
        }

        if (
            request.url ===
                `/api/agents/${publishedAgent.id}/conversations` &&
            request.method === "POST"
        ) {
            conversationCreated = true;
            response.statusCode = 201;
            response.setHeader("Content-Type", "application/json");
            response.end(
                JSON.stringify({
                    id: conversationId,
                    agentId: publishedAgent.id,
                    agentVersionId: publishedAgent.currentPublishedVersionId,
                    title: null,
                    lastActivityTime: "2026-07-20T07:00:00Z",
                    createdTime: "2026-07-20T07:00:00Z",
                    updatedTime: "2026-07-20T07:00:00Z",
                }),
            );
            return;
        }

        if (
            request.url ===
                `/api/agents/${publishedAgent.id}/conversations/${conversationId}/messages?page=1&pageSize=100` &&
            request.method === "GET"
        ) {
            response.setHeader("Content-Type", "application/json");
            response.end(
                JSON.stringify({
                    items: persistedConversationMessages,
                    totalCount: persistedConversationMessages.length,
                    page: 1,
                    pageSize: 100,
                }),
            );
            return;
        }

        if (
            request.url ===
                `/api/agents/${publishedAgent.id}/conversations/${conversationId}` &&
            request.method === "DELETE"
        ) {
            conversationCreated = false;
            persistedConversationMessages = [];
            response.statusCode = 204;
            response.end();
            return;
        }

        if (request.url === `/api/agents/${sharedAgent.id}`) {
            response.setHeader("Content-Type", "application/json");
            response.end(JSON.stringify(sharedAgent));
            return;
        }

        if (request.url === `/api/agents/${clonedAgent.id}`) {
            response.setHeader("Content-Type", "application/json");
            response.end(JSON.stringify(clonedAgent));
            return;
        }

        if (
            request.url === `/api/agents/${sharedAgent.id}/clone` &&
            request.method === "POST"
        ) {
            agentClones += 1;
            response.statusCode = 201;
            response.setHeader("Content-Type", "application/json");
            response.end(JSON.stringify(clonedAgent));
            return;
        }

        if (request.url === "/api/agents" && request.method === "POST") {
            agentCreates += 1;
            const chunks: Buffer[] = [];
            request.on("data", (chunk: Buffer) => chunks.push(chunk));
            request.on("end", () => {
                capturedPayloads.agentCreate = JSON.parse(
                    Buffer.concat(chunks).toString(),
                ) as Record<string, unknown>;
                response.setHeader("Content-Type", "application/json");
                response.end(
                    JSON.stringify(editableAgentWithoutBindingCollections),
                );
            });
            return;
        }

        if (request.url === "/api/agents/avatar" && request.method === "POST") {
            agentAvatarUploads += 1;
            expect(request.headers["content-type"]).toContain(
                "multipart/form-data",
            );
            request.resume();
            request.on("end", () => {
                response.statusCode = 201;
                response.setHeader("Content-Type", "application/json");
                response.end(JSON.stringify({ avatarUri: uploadedAvatarUri }));
            });
            return;
        }

        if (
            request.url ===
                "/api/agents/avatar/0198a96d19e470008000000000000001/avatar.png" &&
            request.method === "GET"
        ) {
            response.setHeader("Content-Type", "image/png");
            response.end(avatarBytes);
            return;
        }

        if (
            request.url === `/api/agents/${editableAgent.id}` &&
            request.method === "PUT"
        ) {
            agentUpdates += 1;
            const chunks: Buffer[] = [];
            request.on("data", (chunk: Buffer) => chunks.push(chunk));
            request.on("end", () => {
                capturedPayloads.agentUpdate = JSON.parse(
                    Buffer.concat(chunks).toString(),
                ) as Record<string, unknown>;
                response.setHeader("Content-Type", "application/json");
                response.end(
                    JSON.stringify({
                        ...editableAgent,
                        avatarUri: uploadedAvatarUri,
                    }),
                );
            });
            return;
        }

        if (
            request.url === `/api/agents/${editableAgent.id}/publish` &&
            request.method === "POST"
        ) {
            agentPublishes += 1;
            const chunks: Buffer[] = [];
            request.on("data", (chunk: Buffer) => chunks.push(chunk));
            request.on("end", () => {
                capturedPayloads.agentPublish = JSON.parse(
                    Buffer.concat(chunks).toString(),
                ) as Record<string, unknown>;
                response.setHeader("Content-Type", "application/json");
                response.end(
                    JSON.stringify({
                        id: "0198a96d-19e4-7000-8000-000000000305",
                        agentId: editableAgent.id,
                        versionNumber: 1,
                        createdByUserId: editableAgent.ownerUserId,
                        changeSummary: "补充头像与发布说明",
                        createdTime: "2026-07-19T00:01:00Z",
                    }),
                );
            });
            return;
        }

        if (
            request.url === `/api/agents/${editableAgent.id}/share` &&
            request.method === "POST"
        ) {
            agentShares += 1;
            response.statusCode = 204;
            response.end();
            return;
        }

        if (request.url === `/api/agents/${editableAgent.id}/versions`) {
            response.setHeader("Content-Type", "application/json");
            response.end(
                JSON.stringify([
                    {
                        id: "0198a96d-19e4-7000-8000-000000000305",
                        agentId: editableAgent.id,
                        versionNumber: 1,
                        createdByUserId: editableAgent.ownerUserId,
                        changeSummary: "补充头像与发布说明",
                        createdTime: "2026-07-19T00:01:00Z",
                        updatedTime: "2026-07-19T00:01:00Z",
                        publishedTime: "2026-07-19T00:01:00Z",
                    },
                ]),
            );
            return;
        }

        if (
            request.url ===
                "/api/agents/0198a96d-19e4-7000-8000-000000000303/share/revoke" &&
            request.method === "POST"
        ) {
            agentShareRevocations += 1;
            response.statusCode = 204;
            response.end();
            return;
        }

        if (request.url === "/api/tools") {
            response.setHeader("Content-Type", "application/json");
            response.end(toolsResponse);
            return;
        }

        if (request.url === "/api/skills") {
            response.setHeader("Content-Type", "application/json");
            response.end(skillsResponse);
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
                        isAdmin: true,
                        isLocked: false,
                        isDisabled: false,
                        lastLoginTime: "2026-07-18T14:56:00Z",
                        createdTime: "2026-05-01T09:00:00Z",
                    },
                    {
                        userId: "0198a96d-19e4-7000-8000-000000000002",
                        username: "bob",
                        isAdmin: false,
                        isLocked: accountLocked,
                        isDisabled: false,
                        lastLoginTime: "2026-07-17T18:20:00Z",
                        createdTime: "2026-05-03T10:15:00Z",
                    },
                ]),
            );
            return;
        }

        if (
            request.url?.startsWith("/agent/") &&
            request.url.includes("/v1/chat/completions") &&
            request.method === "POST"
        ) {
            chatRequestUrls.push(request.url);
            chatRunModes.push(
                request.headers["x-inkwell-agent-run-mode"] as
                    | string
                    | undefined,
            );
                    chatConversationIds.push(
                    request.headers["x-inkwell-conversation-id"] as
                        | string
                        | undefined,
                    );
            const content = [
                "# 运行成功",
                "",
                "## 分析结果",
                "",
                "- 支持标题",
                "- 支持列表",
                "",
                "```ts",
                "const markdownEnabled = true;",
                "```",
                "",
                ...Array.from(
                    { length: 80 },
                    (_, index) =>
                        `第 ${index + 1} 段用于验证长回复滚动行为的内容。`,
                ),
            ].join("\n");
            const chunks: Buffer[] = [];
            request.on("data", (chunk: Buffer) => chunks.push(chunk));
            request.on("end", () => {
                if (
                    request.headers["x-inkwell-conversation-id"] ===
                    conversationId
                ) {
                    const body = JSON.parse(
                        Buffer.concat(chunks).toString(),
                    ) as { messages: Array<{ role: string; content: string }> };
                    persistedConversationMessages = [
                        {
                            id: "0198a96d-19e4-7000-8000-000000000402",
                            message: {
                                role: "user",
                                contents: [{ text: body.messages[0].content }],
                            },
                            sequenceNumber: 1,
                        },
                        {
                            id: "0198a96d-19e4-7000-8000-000000000403",
                            message: {
                                role: "assistant",
                                contents: [{ text: content }],
                            },
                            sequenceNumber: 2,
                        },
                    ];
                }

                response.setHeader("Content-Type", "text/event-stream");
                response.end(
                    `data: ${JSON.stringify({ choices: [{ delta: { content } }] })}\n\ndata: [DONE]\n\n`,
                );
            });
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
            page.getByRole("heading", { name: "Agent 空间" }),
        ).toBeVisible();
        await expect(page.getByRole("tab", { name: "我的" })).toBeVisible();
        await expect(page.getByRole("tab", { name: "团队共享" })).toBeVisible();
        await expect(page.getByRole("radio", { name: "全部 2" })).toBeChecked();
        await expect(page.getByText("已发布 1", { exact: true })).toBeVisible();
        await expect(page.getByText("草稿 1", { exact: true })).toBeVisible();
        await expect(page.getByText("研发助手", { exact: true })).toBeVisible();
        await expect(page.getByText("产品草稿", { exact: true })).toBeVisible();

        await page.getByRole("button", { name: "帮助" }).dispatchEvent("click");
        const helpMenu = page.getByRole("menu");
        await expect(
            helpMenu.getByText("使用指南", { exact: true }),
        ).toBeVisible();
        await expect(
            helpMenu.getByText("快速开始", { exact: true }),
        ).toBeVisible();
        await expect(
            helpMenu.getByText("常见问题", { exact: true }),
        ).toBeVisible();
        await expect(
            helpMenu.getByText("关于 Inkwell", { exact: true }),
        ).toBeVisible();
        await helpMenu
            .getByText("使用指南", { exact: true })
            .dispatchEvent("click");
        await expect(
            page.getByRole("heading", { name: "创建并发布第一个 Agent" }),
        ).toBeVisible();
        await expect(
            page.getByRole("navigation", { name: "使用指南章节" }),
        ).toBeVisible();
        const guidePageBox = await page
            .locator(".user-guide-page")
            .boundingBox();
        const workspaceBox = await page
            .locator(".workspace-content")
            .boundingBox();
        expect(guidePageBox).not.toBeNull();
        expect(workspaceBox).not.toBeNull();
        expect(guidePageBox!.height).toBeLessThanOrEqual(workspaceBox!.height);
        await page.getByRole("button", { name: "帮助" }).dispatchEvent("click");
        await page.screenshot({
            path: testInfo.outputPath("user-guide-1080x720.png"),
            fullPage: true,
        });

        await page.getByPlaceholder("搜索指南").fill("共享");
        await expect(
            page.getByRole("button", { name: "共享与复制" }),
        ).toBeVisible();
        await expect(
            page.getByRole("button", { name: "创建与配置" }),
        ).toHaveCount(0);
        await page.getByPlaceholder("搜索指南").clear();
        await page
            .getByRole("button", { name: "创建与配置" })
            .dispatchEvent("click");
        await expect(
            page.getByRole("heading", { name: "先定义职责，再补充能力" }),
        ).toBeVisible();

        await page.getByRole("button", { name: "帮助" }).dispatchEvent("click");
        await page
            .getByRole("menu")
            .getByText("常见问题", { exact: true })
            .dispatchEvent("click");
        await expect(
            page.getByRole("heading", { name: "快速找到当前状态的含义" }),
        ).toBeVisible();

        await page.getByRole("button", { name: "帮助" }).dispatchEvent("click");
        await page
            .getByRole("menu")
            .getByText("快速开始", { exact: true })
            .dispatchEvent("click");
        const quickStartDialog = page.getByRole("dialog", {
            name: "快速开始",
        });
        await expect(quickStartDialog).toBeVisible();
        await quickStartDialog
            .getByRole("checkbox", { name: /创建一个 Agent/ })
            .dispatchEvent("click");
        await expect(page.getByText("1 / 5", { exact: true })).toBeVisible();
        await page
            .getByRole("button", { name: "前往 Agent 空间" })
            .dispatchEvent("click");
        await expect(
            page.getByRole("heading", { name: "Agent 空间" }),
        ).toBeVisible();

        await page
            .locator(".agent-space-card")
            .filter({ hasText: "研发助手" })
            .dispatchEvent("click");
        await expect(page.locator(".chat-panel-full")).toBeVisible();
        await expect(page.locator(".chat-page-header")).toHaveCSS(
            "height",
            "52px",
        );
        await expect(page.locator(".chat-page-header")).toHaveCSS(
            "background-color",
            "rgb(246, 245, 248)",
        );
        const publishedAgentAvatar = page.locator(
            ".chat-page-header .agent-avatar",
        );
        await expect(publishedAgentAvatar).toHaveCSS("width", "28px");
        await expect(publishedAgentAvatar).toHaveCSS("height", "28px");
        await expect(publishedAgentAvatar).toHaveCSS(
            "background-color",
            "rgb(104, 70, 156)",
        );
        await expect(
            page.getByText("模型：gpt-5.4", { exact: true }),
        ).toBeVisible();
        await expect(page.locator(".chat-history")).toHaveCSS("width", "240px");
        await expect(page.locator(".chat-history")).toHaveCSS(
            "background-color",
            "rgb(246, 245, 248)",
        );
        await expect(
            page.getByRole("heading", { name: "研发助手" }),
        ).toBeVisible();
        await expect(
            page.getByText("整理一份竞品研究框架", { exact: true }),
        ).toBeVisible();
        await page.screenshot({
            path: testInfo.outputPath("agent-chat-published-dark-1080x720.png"),
            fullPage: true,
        });
        const publishedSender = page.getByPlaceholder(
            "输入消息，Enter 发送，Shift + Enter 换行",
        );
        await publishedSender.fill("验证正式发布版");
        await publishedSender.press("Enter");
        await expect(
            page.locator(".chat-full-messages .x-markdown h1"),
        ).toHaveText("运行成功");
        await expect(
            page
                .locator(".chat-history")
                .getByText("验证正式发布版", { exact: true }),
        ).toBeVisible();
        await page
            .getByRole("button", { name: "收起会话" })
            .dispatchEvent("click");
        await expect(page.locator(".chat-history")).toHaveCSS("width", "44px");
        await page
            .getByRole("button", { name: "展开会话" })
            .dispatchEvent("click");
        await page
            .getByRole("button", { name: "新建会话" })
            .dispatchEvent("click");
        await expect(
            page.getByRole("heading", { name: "研发助手" }),
        ).toBeVisible();
        await expect(page.locator(".chat-full-messages")).toHaveCount(0);
        await page
            .getByRole("button", { name: "返回 Agent 空间" })
            .dispatchEvent("click");
        await expect(
            page.getByRole("heading", { name: "Agent 空间" }),
        ).toBeVisible();

        await page.getByPlaceholder("搜索 Agent").fill("研发");
        await expect(page.getByText("研发助手", { exact: true })).toBeVisible();
        await expect(page.getByText("产品草稿", { exact: true })).toHaveCount(
            0,
        );
        await page.getByRole("tab", { name: "团队共享" }).click();
        await expect(
            page.getByText("合同审查助手", { exact: true }),
        ).toBeVisible();
        await page
            .getByRole("button", { name: "查看 合同审查助手 详情" })
            .dispatchEvent("click");
        await expect(
            page.getByText("这是其他成员共享的 Agent，当前为只读模式。"),
        ).toBeVisible();
        await expect(
            page
                .locator(".agent-editor-actions")
                .getByRole("button", { name: "复制为我的 Agent" }),
        ).toBeVisible();
        await page
            .locator(".agent-editor-actions")
            .getByRole("button", { name: "试运行" })
            .dispatchEvent("click");
        await expect(
            page.getByText("已发布 v2", { exact: true }),
        ).toBeVisible();
        const sharedTrialSender = page.getByPlaceholder(
            "输入消息，Enter 发送，Shift + Enter 换行",
        );
        await sharedTrialSender.fill("验证只读发布版");
        await sharedTrialSender.press("Enter");
        await expect(page.getByText("运行成功")).toBeVisible();
        await expect(page.locator(".chat-bubble-list")).toBeVisible();
        await expect(page.locator(".chat-bubble-list .ant-bubble")).toHaveCount(
            2,
        );
        await page
            .getByRole("button", { name: "关闭试运行" })
            .dispatchEvent("click");
        await page
            .locator(".agent-editor-actions")
            .getByRole("button", { name: "复制为我的 Agent" })
            .dispatchEvent("click");
        await expect.poll(() => agentClones).toBe(1);
        await expect(
            page.getByText("合同审查助手（副本）", { exact: true }),
        ).toBeVisible();
        await expect(
            page.locator(".agent-editor-actions").getByRole("button", {
                name: "保存",
            }),
        ).toBeEnabled();
        await page
            .getByRole("button", { name: "返回 Agent 空间" })
            .dispatchEvent("click");
        await page.getByRole("tab", { name: "团队共享" }).click();
        await page
            .getByRole("button", { name: "撤销 合同审查助手 共享" })
            .dispatchEvent("click");
        await page
            .getByRole("button", { name: "确认撤销" })
            .dispatchEvent("click");
        await expect(page.getByText("已由管理员撤销共享")).toBeVisible();
        expect(agentShareRevocations).toBe(1);
        await expect(page.getByText("工作区", { exact: true })).toBeVisible();
        await expect(page.getByText("资源中心", { exact: true })).toBeVisible();
        await expect(page.getByText("系统管理", { exact: true })).toBeVisible();
        await expect(
            page.getByRole("button", { name: "用户管理" }),
        ).toBeVisible();

        const aboutTrigger = page.getByRole("button", {
            name: "关于 Inkwell",
        });
        await expect(aboutTrigger).toHaveCSS(
            "animation-name",
            "inkwell-breathe",
        );
        await expect(aboutTrigger).toHaveCSS("animation-duration", "1.8s");
        await expect(aboutTrigger).toHaveCSS(
            "will-change",
            "filter, opacity, transform",
        );
        await aboutTrigger.click();
        await expect(
            page.getByRole("heading", { name: "Inkwell", exact: true }),
        ).toBeVisible();
        await expect(page.getByText("版本", { exact: true })).toBeVisible();
        await expect(page.getByTestId("app-version")).not.toHaveText("-");
        await expect(page.getByTestId("app-build-number")).not.toHaveText(
            "未提供",
        );
        await expect(page.getByTestId("app-commit")).not.toHaveText("未提供");
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
        await expect(page.getByText("修改密码", { exact: true })).toBeVisible();
        await expect(
            page.getByRole("menu").getByText("管理", { exact: true }),
        ).toBeVisible();
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
        await expect(page.locator(".agent-space-page")).toHaveCSS(
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
        await expect(
            modelTable.getByText("对话", { exact: true }),
        ).toBeVisible();
        await expect(
            modelTable.getByText("嵌入", { exact: true }),
        ).toBeVisible();
        const listCapabilityTag = modelTable
            .locator(".ant-tag-success")
            .first();
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
        await modelDetails.locator(".ant-drawer-close").dispatchEvent("click");
        await expect(modelDetails).toBeHidden();
        await page
            .getByRole("button", { name: "测试 gpt-5.4" })
            .dispatchEvent("click");
        await expect(
            page.getByText("gpt-5.4 对话最小请求成功 · 125 ms"),
        ).toBeVisible();
        expect(modelTestAttempts).toBe(1);
        await expect(
            page.locator(".ant-dropdown:not(.ant-dropdown-hidden)"),
        ).toHaveCount(0);
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
            .getByRole("button", { name: "管理 bob" })
            .dispatchEvent("click");
        const manageDialog = page.getByRole("dialog", {
            name: "管理用户 · bob",
        });
        await expect(manageDialog).toBeVisible();
        await expect(manageDialog).toContainText(
            "该账号因登录失败次数过多被系统自动锁定。",
        );
        await manageDialog
            .getByText("解锁", { exact: true })
            .dispatchEvent("click");
        const unlockDialog = page.getByRole("dialog", {
            name: "解锁账号 bob",
        });
        await expect(unlockDialog).toBeVisible();
        await page
            .getByRole("button", { name: "确认解锁" })
            .dispatchEvent("click");
        await expect(unlockDialog).toBeHidden();
        await expect(page.getByText("bob 已解锁")).toBeVisible();
        expect(accountUnlockAttempts).toBe(1);
        await expect(
            page.getByRole("table").getByText("正常", { exact: true }),
        ).toHaveCount(2);
        await page.keyboard.press("Escape");
        await expect(manageDialog).toBeHidden();

        await page
            .locator(".app-sidebar .nav-item", { hasText: "工具" })
            .dispatchEvent("click");
        await expect(
            page.getByRole("heading", { name: "工具", exact: true }),
        ).toBeVisible();
        const toolTable = page.getByRole("table");
        for (const column of ["名称", "描述", "参数", "更新时间", "操作"]) {
            await expect(
                toolTable.getByRole("columnheader", { name: column }),
            ).toBeVisible();
        }
        await expect(
            toolTable.getByText("current_date_time", { exact: true }),
        ).toBeVisible();
        await expect(
            toolTable.getByText("2 项", { exact: true }),
        ).toBeVisible();
        await page.setViewportSize({ width: 1080, height: 720 });
        await page.screenshot({
            path: testInfo.outputPath("tool-management-dark-1080x720.png"),
            fullPage: true,
        });
        await page
            .getByRole("button", { name: "查看 current_date_time" })
            .dispatchEvent("click");
        const toolDetails = page.getByRole("dialog", { name: "Tool 详情" });
        await expect(toolDetails).toBeVisible();
        await expect(
            toolDetails.getByRole("cell", { name: "timeZone" }),
        ).toBeVisible();
        await expect(
            toolDetails.getByText("UTC, Asia/Shanghai", { exact: true }),
        ).toBeVisible();
        await toolDetails
            .getByText("原始 JSON Schema", { exact: true })
            .dispatchEvent("click");
        await expect(toolDetails.getByText(/"timeZone"/)).toBeVisible();
        await toolDetails.locator(".ant-drawer-close").dispatchEvent("click");
        await expect(toolDetails).toBeHidden();

        await page
            .locator(".app-sidebar .nav-item", { hasText: "Skills" })
            .dispatchEvent("click");
        await expect(
            page.getByRole("heading", { name: "Skills", exact: true }),
        ).toBeVisible();
        const skillTable = page.getByRole("table");
        for (const column of [
            "名称",
            "描述",
            "所有者",
            "资料",
            "更新时间",
            "操作",
        ]) {
            await expect(
                skillTable.getByRole("columnheader", { name: column }),
            ).toBeVisible();
        }
        await expect(
            skillTable.getByText("合同审查规范", { exact: true }),
        ).toBeVisible();
        await expect(
            skillTable.getByText("研发周报", { exact: true }),
        ).toBeVisible();
        await expect(
            skillTable.getByText("1 引用 · 1 素材 · 1 脚本", { exact: true }),
        ).toBeVisible();
        await page.getByRole("combobox").dispatchEvent("mousedown");
        await page
            .locator(".ant-select-dropdown")
            .getByText("其他成员", { exact: true })
            .dispatchEvent("click");
        await expect(
            skillTable.getByText("合同审查规范", { exact: true }),
        ).toBeHidden();
        await expect(
            skillTable.getByText("研发周报", { exact: true }),
        ).toBeVisible();
        await page.getByRole("combobox").dispatchEvent("mousedown");
        await page
            .locator(".ant-select-dropdown")
            .getByText("全部归属", { exact: true })
            .dispatchEvent("click");
        await page
            .getByRole("button", { name: "查看 合同审查规范" })
            .dispatchEvent("click");
        const skillDetails = page.getByRole("dialog", { name: "Skill 详情" });
        await expect(skillDetails).toBeVisible();
        await expect(
            skillDetails.getByText("脚本已保存，当前版本不会执行"),
        ).toBeVisible();
        await skillDetails.locator(".ant-drawer-close").dispatchEvent("click");
        await page
            .getByRole("button", { name: "上传 Skill" })
            .dispatchEvent("click");
        const uploadDialog = page.getByRole("dialog", { name: "上传 Skill" });
        await uploadDialog.locator('input[type="file"]').setInputFiles({
            name: "SKILL.md",
            mimeType: "text/markdown",
            buffer: Buffer.from(
                "---\nname: 合同审查规范\ndescription: 按团队法务标准识别合同风险并输出分级建议。\n---\n# 合同审查规范",
            ),
        });
        await expect(uploadDialog.getByText("SKILL.md 解析预览")).toBeVisible();
        await expect(
            uploadDialog.getByText(
                "0 个 references · 0 个 asset · 0 个 scripts",
            ),
        ).toBeVisible();
        await page.keyboard.press("Escape");
        await expect(uploadDialog).toBeHidden();
        await page.setViewportSize({ width: 1080, height: 720 });
        expect(
            await page.evaluate(() => document.documentElement.scrollWidth),
        ).toBeLessThanOrEqual(1080);
        await page.screenshot({
            path: testInfo.outputPath("skill-management-dark-1080x720.png"),
            fullPage: true,
        });

        await page.getByRole("button", { name: "Agent 空间" }).click();
        await expect(
            page.getByRole("heading", { name: "Agent 空间" }),
        ).toBeVisible();
        await page.setViewportSize({ width: 1080, height: 720 });
        const workspaceGroup = page
            .getByRole("button", { name: "工作区" })
            .first();
        expect(
            await workspaceGroup.evaluate((element) => {
                const style = getComputedStyle(element);
                const arrowStyle = getComputedStyle(
                    element.querySelector(".anticon")!,
                );
                return {
                    color: style.color,
                    height: element.getBoundingClientRect().height,
                    fontSize: style.fontSize,
                    fontWeight: style.fontWeight,
                    lineHeight: style.lineHeight,
                    padding: style.padding,
                    arrowColor: arrowStyle.color,
                    arrowFontSize: arrowStyle.fontSize,
                };
            }),
        ).toEqual({
            color: "rgba(255, 255, 255, 0.45)",
            height: 25.28125,
            fontSize: "11px",
            fontWeight: "600",
            lineHeight: "17.2857px",
            padding: "4px 12px",
            arrowColor: "rgba(255, 255, 255, 0.45)",
            arrowFontSize: "9px",
        });
        expect(
            await page
                .getByRole("button", { name: "Agent 空间" })
                .evaluate((element) => {
                    const style = getComputedStyle(element);
                    return {
                        height: element.getBoundingClientRect().height,
                        color: style.color,
                        backgroundColor: style.backgroundColor,
                        fontSize: style.fontSize,
                        fontWeight: style.fontWeight,
                        padding: style.padding,
                        gap: style.gap,
                    };
                }),
        ).toEqual({
            height: 32.5,
            color: "rgb(198, 120, 98)",
            backgroundColor: "rgb(83, 55, 48)",
            fontSize: "13px",
            fontWeight: "600",
            padding: "7px 12px",
            gap: "10px",
        });
        const firstAgentCard = page.locator(".agent-space-card").first();
        const firstAgentAvatar = firstAgentCard.locator(".agent-card-avatar");
        await expect(firstAgentCard).toBeVisible();
        const firstAgentCardMetrics = await firstAgentCard.evaluate(
            (element) => {
                const box = element.getBoundingClientRect();
                const style = getComputedStyle(element);
                return {
                    x: box.x,
                    width: box.width,
                    height: box.height,
                    borderRadius: style.borderRadius,
                };
            },
        );
        expect(firstAgentCardMetrics.width).toBeCloseTo(158.4, 1);
        expect(firstAgentCardMetrics).toMatchObject({
            x: 220,
            height: 128,
            borderRadius: "10px",
        });
        expect(
            await firstAgentAvatar.evaluate((element) => {
                const box = element.getBoundingClientRect();
                const style = getComputedStyle(element);
                return {
                    x: box.x,
                    width: box.width,
                    height: box.height,
                    borderRadius: style.borderRadius,
                };
            }),
        ).toEqual({
            x: 233,
            width: 40,
            height: 40,
            borderRadius: "10px",
        });
        await expect(
            page.locator(".agent-space-toolbar .ant-input-affix-wrapper"),
        ).toHaveCSS("width", "200px");
        expect(
            await page.evaluate(() => document.documentElement.scrollWidth),
        ).toBeLessThanOrEqual(1080);
        await page.screenshot({
            path: testInfo.outputPath("workspace-dark-1080x720.png"),
            fullPage: true,
        });

        await page
            .getByRole("button", { name: "新建 Agent" })
            .dispatchEvent("click");
        await expect(page.getByText("未发布的草稿")).toBeVisible();
        const editorSections = page.locator(".agent-editor-sections");
        const basicSectionButton = editorSections.getByRole("button", {
            name: "基础信息",
        });
        const editorHeader = page.locator(".agent-editor-header");
        const editorContent = page.locator(".agent-editor-content");
        const editorContentScroll = page.locator(
            ".agent-editor-content-scroll",
        );
        const editorAvatar = editorHeader.locator(".agent-editor-avatar");
        const editorTitle = editorHeader.locator(
            ".agent-editor-identity > .ant-typography",
        );
        const editorMeta = editorHeader.locator(
            ".agent-editor-identity > .ant-space .ant-typography",
        );
        await expect(editorSections).toHaveCSS("width", "176px");
        await expect(editorContent).toHaveCSS("overflow", "hidden");
        await expect(editorContentScroll).toHaveCSS("overflow", "auto");
        await expect(editorContentScroll).toHaveCSS(
            "padding",
            "18px 24px 48px",
        );
        await expect(page.locator(".agent-editor-workspace")).toHaveCSS(
            "background-color",
            "rgb(33, 29, 27)",
        );
        await expect(editorHeader).toHaveCSS("min-height", "60px");
        await expect(editorHeader).toHaveCSS("gap", "14px");
        await expect(editorAvatar).toHaveCSS("border-radius", "12px");
        await expect(editorAvatar).toHaveCSS("font-size", "17px");
        await expect(editorTitle).toHaveCSS("font-size", "16px");
        await expect(editorMeta).toHaveCSS("font-size", "11px");
        expect(
            await Promise.all([
                editorSections.evaluate(
                    (element) => getComputedStyle(element).backgroundColor,
                ),
                editorContent.evaluate(
                    (element) => getComputedStyle(element).backgroundColor,
                ),
            ]),
        ).toEqual(["rgba(255, 255, 255, 0.04)", "rgb(33, 29, 27)"]);
        await expect(basicSectionButton).toHaveCSS(
            "background-color",
            "rgb(83, 55, 48)",
        );
        await expect(basicSectionButton).toHaveCSS(
            "color",
            "rgb(198, 120, 98)",
        );
        expect(
            await basicSectionButton.evaluate((element) => {
                const box = element.getBoundingClientRect();
                const style = getComputedStyle(element);
                const iconStyle = getComputedStyle(
                    element.querySelector(".anticon")!,
                );
                return {
                    height: box.height,
                    padding: style.padding,
                    borderRadius: style.borderRadius,
                    fontSize: style.fontSize,
                    iconFontSize: iconStyle.fontSize,
                };
            }),
        ).toEqual({
            height: 34,
            padding: "7px 10px",
            borderRadius: "6px",
            fontSize: "12px",
            iconFontSize: "14px",
        });
        const editorWorkspaceBox = await page
            .locator(".agent-editor-workspace")
            .boundingBox();
        const avatarEditorBox = await page
            .getByRole("button", { name: "更换 Agent 头像" })
            .boundingBox();
        const agentNameBox = await page.getByLabel("Agent 名称").boundingBox();
        const sectionHeadingMetrics = await page
            .locator(".agent-editor-section-heading")
            .evaluate((element) => {
                const box = element.getBoundingClientRect();
                const heading = element.querySelector("h5")!;
                const headingBox = heading.getBoundingClientRect();
                const headingStyle = getComputedStyle(heading);
                return {
                    y: box.y,
                    height: box.height,
                    headingY: headingBox.y,
                    headingHeight: headingBox.height,
                    fontSize: headingStyle.fontSize,
                    lineHeight: headingStyle.lineHeight,
                };
            });
        const sectionToggleMetrics = await page
            .locator(".agent-editor-sections-toggle")
            .evaluate((element) => {
                const box = element.getBoundingClientRect();
                return {
                    y: box.y,
                    height: box.height,
                    bottom: box.bottom,
                };
            });
        const nameInputMetrics = await page
            .getByLabel("Agent 名称")
            .evaluate((element) => {
                const box = element.getBoundingClientRect();
                const style = getComputedStyle(element);
                return {
                    height: box.height,
                    fontSize: style.fontSize,
                    lineHeight: style.lineHeight,
                    padding: style.padding,
                };
            });
        expect(editorWorkspaceBox).not.toBeNull();
        expect(avatarEditorBox).not.toBeNull();
        expect(agentNameBox).not.toBeNull();
        expect({
            avatarX: avatarEditorBox!.x - editorWorkspaceBox!.x,
            avatarY: avatarEditorBox!.y - editorWorkspaceBox!.y,
            nameX: agentNameBox!.x - editorWorkspaceBox!.x,
            nameY: agentNameBox!.y - editorWorkspaceBox!.y,
        }).toEqual({ avatarX: 200, avatarY: 96, nameX: 296, nameY: 96 });
        expect(sectionHeadingMetrics).toMatchObject({
            height: 48,
            headingHeight: 24,
            fontSize: "16px",
            lineHeight: "24px",
        });
        expect(sectionHeadingMetrics.y - editorWorkspaceBox!.y).toBe(0);
        expect(sectionHeadingMetrics.headingY - sectionHeadingMetrics.y).toBe(
            11.5,
        );
        expect(sectionToggleMetrics).toEqual({
            y: sectionHeadingMetrics.y,
            height: 48,
            bottom: sectionHeadingMetrics.y + sectionHeadingMetrics.height,
        });
        expect(nameInputMetrics).toEqual({
            height: 32,
            fontSize: "14px",
            lineHeight: "22px",
            padding: "4px 11px",
        });
        await expect(
            page.locator(
                ".agent-editor-content .ant-form-item-label .anticon-question-circle",
            ),
        ).toBeVisible();
        await page.getByLabel("Agent 名称").fill("发布助手");
        await page.getByLabel("描述").fill("整理发布内容。");
        await page
            .locator(".app-sidebar .nav-item", { hasText: "工具" })
            .dispatchEvent("click");
        const unsavedChangesDialog = page.getByRole("dialog", {
            name: "有未保存的修改",
        });
        await expect(unsavedChangesDialog).toBeVisible();
        await unsavedChangesDialog
            .getByRole("button", { name: "继续编辑" })
            .dispatchEvent("click");
        await expect(unsavedChangesDialog).toBeHidden();
        await expect(page.getByLabel("Agent 名称")).toHaveValue("发布助手");
        await page
            .locator(".app-sidebar .nav-item", { hasText: "工具" })
            .dispatchEvent("click");
        await unsavedChangesDialog
            .getByRole("button", { name: "仍然离开" })
            .dispatchEvent("click");
        await expect(
            page.getByRole("heading", { name: "工具", exact: true }),
        ).toBeVisible();
        await page
            .getByRole("button", { name: "Agent 空间" })
            .dispatchEvent("click");
        await expect(
            page.getByRole("heading", { name: "Agent 空间" }),
        ).toBeVisible();
        await page
            .getByRole("button", { name: "新建 Agent" })
            .dispatchEvent("click");
        await page.getByLabel("Agent 名称").fill("发布助手");
        await page.getByLabel("描述").fill("整理发布内容。");
        await page
            .locator('.agent-basic-avatar-wrap input[type="file"]')
            .setInputFiles({
                name: "avatar.png",
                mimeType: "image/png",
                buffer: avatarBytes,
            });
        await expect.poll(() => agentAvatarUploads).toBe(1);
        const uploadedAvatar = page.locator(".agent-basic-avatar-editor img");
        await expect(uploadedAvatar).toBeVisible();
        expect(
            await uploadedAvatar.evaluate(
                (element) => (element as HTMLImageElement).naturalWidth,
            ),
        ).toBeGreaterThan(0);
        await page.screenshot({
            path: testInfo.outputPath("agent-basic-editor-dark-1080x720.png"),
            fullPage: true,
        });
        await page
            .getByRole("button", { name: "Instructions" })
            .dispatchEvent("click");
        await page.getByLabel("Instructions").fill("输出简洁的发布说明。");
        await page
            .getByRole("button", { name: "模型与参数" })
            .dispatchEvent("click");
        await expect(
            page.locator(".agent-model-picker").getByText("gpt-5.4", {
                exact: true,
            }),
        ).toBeVisible();
        await expect(page.locator(".agent-model-picker")).toHaveCSS(
            "margin-bottom",
            "18px",
        );
        await expect(
            page.locator(".agent-model-picker .ant-form-item-required"),
        ).toHaveCount(0);
        await expect(
            page.locator(".agent-model-picker .ant-card-body"),
        ).toHaveCSS("padding", "14px");
        await expect(
            page.locator(".agent-model-picker .ant-tag").first(),
        ).toHaveCSS("margin-inline-end", "0px");
        await expect(
            page.locator(".agent-model-section-title").first(),
        ).toHaveCSS("margin-bottom", "4px");
        await expect(
            page.locator(".agent-model-section-description").first(),
        ).toHaveCSS("margin-bottom", "14px");
        await expect(page.locator(".agent-parameter-grid")).toHaveCSS(
            "gap",
            "16px",
        );
        const temperatureCard = page
            .locator(".agent-parameter-grid .ant-card")
            .first();
        await expect(temperatureCard.locator(".ant-card-body")).toHaveCSS(
            "padding",
            "12px",
        );
        await expect(
            temperatureCard.locator(".agent-parameter-heading"),
        ).toHaveCSS("align-items", "center");
        await expect(
            temperatureCard.locator(".agent-parameter-heading"),
        ).toHaveCSS("margin-bottom", "16px");
        const temperatureInputRow = temperatureCard.locator(
            ".agent-parameter-input",
        );
        await expect(temperatureInputRow).toHaveCSS("display", "flex");
        await expect(temperatureInputRow).toHaveCSS("gap", "12px");
        await expect(temperatureInputRow).toHaveCSS("align-items", "center");
        await expect(temperatureInputRow.locator(".ant-slider")).toHaveCSS(
            "flex",
            "1 1 0%",
        );
        await expect(
            temperatureInputRow.locator(".ant-input-number"),
        ).toHaveCSS("width", "80px");
        await expect(
            temperatureInputRow.locator(".ant-input-number"),
        ).toHaveCSS("height", "24px");
        await expect(
            page.locator(".agent-model-section-title.context"),
        ).toHaveCSS("margin-top", "20px");
        await expect(
            page.locator(".agent-model-section-description.context"),
        ).toHaveCSS("margin-bottom", "10px");
        await expect(page.locator(".agent-model-context-input")).toHaveCSS(
            "margin-bottom",
            "0px",
        );
        await expect(
            page
                .locator(".agent-parameter-heading .ant-typography-secondary")
                .first(),
        ).toHaveCSS("font-size", "11px");
        await expect(
            page
                .locator(".agent-editor-content .ant-form-item")
                .filter({ hasText: "最大消息记录数" })
                .locator(".ant-input-number"),
        ).toHaveCSS("width", "160px");
        expect(
            await page.evaluate(() => document.documentElement.scrollWidth),
        ).toBeLessThanOrEqual(1080);
        await page.screenshot({
            path: testInfo.outputPath("agent-model-editor-dark-1080x720.png"),
            fullPage: true,
        });
        await editorSections
            .getByRole("button", { name: "工具" })
            .dispatchEvent("click");
        const firstBindingCard = page
            .locator(".agent-binding-selector .ant-card")
            .first();
        await expect(firstBindingCard).toHaveCSS("border-radius", "8px");
        await expect(firstBindingCard.locator(".ant-card-body")).toHaveCSS(
            "padding",
            "8px 12px",
        );
        await expect(
            firstBindingCard.locator(".ant-typography").first(),
        ).toHaveCSS("font-size", "13px");
        await editorSections
            .getByRole("button", { name: "Skills" })
            .dispatchEvent("click");
        await expect(page.locator(".agent-skills-description")).toHaveCSS(
            "font-size",
            "12px",
        );
        await expect(page.locator(".agent-skills-description")).toHaveCSS(
            "margin-bottom",
            "12px",
        );
        await page
            .locator(".agent-editor-actions")
            .getByRole("button", { name: "试运行" })
            .dispatchEvent("click");
        await expect.poll(() => agentCreates).toBe(1);
        expect(agentPublishes).toBe(0);
        expect(capturedPayloads.agentCreate?.toolBindings).toEqual([]);
        expect(capturedPayloads.agentCreate?.skillBindings).toEqual([]);
        await expect(page.getByText("未发布的草稿")).toBeVisible();
        await expect(page.getByText("当前草稿", { exact: true })).toBeVisible();
        const draftTrialSender = page.getByPlaceholder(
            "输入消息，Enter 发送，Shift + Enter 换行",
        );
        await draftTrialSender.focus();
        await expect(draftTrialSender).toHaveCSS("outline-style", "none");
        await expect(draftTrialSender).toHaveCSS("box-shadow", "none");
        const trialPanelHeightBeforeSend = await page
            .locator(".agent-editor-trial")
            .evaluate((element) => element.clientHeight);
        await draftTrialSender.fill("验证未发布草稿");
        await draftTrialSender.press("Enter");
        await expect(page.getByText("运行成功")).toBeVisible();
        await expect(page.locator(".chat-bubble-list")).toBeVisible();
        await expect(page.locator(".chat-bubble-list .ant-bubble")).toHaveCount(
            2,
        );
        const assistantMarkdown = page
            .locator(".chat-bubble-list .ant-bubble")
            .last()
            .locator(".x-markdown");
        await expect(assistantMarkdown.locator("h1")).toHaveText("运行成功");
        await expect(assistantMarkdown.locator("li")).toHaveCount(2);
        await expect(assistantMarkdown.locator("pre code")).toContainText(
            "const markdownEnabled = true;",
        );
        expect(
            await page
                .locator(".chat-panel-trial")
                .evaluate((element) => element.clientHeight),
        ).toBe(trialPanelHeightBeforeSend);
        const trialScrollBox = page.locator(
            ".chat-panel-trial .ant-bubble-list-scroll-box",
        );
        await expect
            .poll(() =>
                trialScrollBox.evaluate(
                    (element) => element.scrollHeight > element.clientHeight,
                ),
            )
            .toBe(true);
        expect(chatRequestUrls).toEqual([
            "/agent/0198a96d-19e4-7000-8000-000000000301/v1/chat/completions",
            `/agent/${sharedAgent.id}/v1/chat/completions`,
            `/agent/${editableAgent.id}/v1/chat/completions?version=draft`,
        ]);
        expect(chatRunModes).toEqual(["published", "published", "draft"]);
        expect(chatConversationIds).toEqual([
            conversationId,
            undefined,
            undefined,
        ]);
        await page
            .getByRole("button", { name: "关闭试运行" })
            .dispatchEvent("click");
        const publishButton = page
            .locator(".agent-editor-actions")
            .getByRole("button", { name: /发布/ });
        await expect(publishButton).toBeEnabled();
        await publishButton.dispatchEvent("click");
        await expect(
            page.getByText("发布新版本", { exact: true }),
        ).toBeVisible();
        const publishDialog = page.getByRole("dialog", { name: "发布新版本" });
        await expect(
            publishDialog.locator(".ant-typography").first(),
        ).toHaveCSS("margin-bottom", "16px");
        await publishDialog
            .getByRole("checkbox", { name: "发布后共享给团队" })
            .check();
        await page.keyboard.press("Escape");
        await expect(publishDialog).toBeHidden();
        await publishButton.dispatchEvent("click");
        await expect(
            publishDialog.getByRole("checkbox", { name: "发布后共享给团队" }),
        ).not.toBeChecked();
        await publishDialog
            .getByPlaceholder("说明本次修改的内容，会记录到版本历史里")
            .fill("补充头像与发布说明");
        await publishDialog
            .getByRole("checkbox", { name: "发布后共享给团队" })
            .check();
        await page
            .locator(".ant-modal-footer:visible .ant-btn-primary")
            .dispatchEvent("click");
        await expect(
            page.getByRole("dialog", { name: "发布新版本" }),
        ).toBeHidden();
        await expect(page.getByText("已发布为 v1")).toBeVisible();
        await expect(page.getByText("已发布", { exact: true })).toBeVisible();
        expect(agentCreates).toBe(1);
        expect(agentUpdates).toBe(1);
        expect(agentPublishes).toBe(1);
        expect(agentShares).toBe(1);
        expect(capturedPayloads.agentUpdate?.avatarUri).toBe(uploadedAvatarUri);
        expect(capturedPayloads.agentPublish?.changeSummary).toBe(
            "补充头像与发布说明",
        );
        await page.getByRole("button", { name: "版本" }).dispatchEvent("click");
        await expect(
            page.getByRole("columnheader", { name: "变更摘要" }),
        ).toBeVisible();
        await expect(
            page.getByText("补充头像与发布说明", { exact: true }),
        ).toBeVisible();
        await page
            .getByRole("button", { name: "基础信息" })
            .dispatchEvent("click");
        await page
            .locator(".agent-editor-actions")
            .getByRole("button", { name: "试运行" })
            .dispatchEvent("click");
        await expect(
            page.getByRole("button", { name: "关闭试运行" }),
        ).toBeVisible();
        await expect(page.locator(".agent-editor-trial")).toHaveCSS(
            "width",
            "400px",
        );
        await expect(page.locator(".agent-editor-trial")).toHaveCSS(
            "animation-name",
            "agent-trial-expand",
        );
        await expect(page.locator(".agent-editor-trial")).toHaveCSS(
            "animation-duration",
            "0.2s",
        );
        await expect(
            page.locator(".agent-editor-trial .chat-header"),
        ).toHaveCSS("height", "48px");
        const trialHeaderBox = await page
            .locator(".agent-editor-trial .chat-header")
            .boundingBox();
        const sectionsToggleBox = await page
            .locator(".agent-editor-sections-toggle")
            .boundingBox();
        expect(trialHeaderBox).not.toBeNull();
        expect(sectionsToggleBox).not.toBeNull();
        expect(trialHeaderBox!.y + trialHeaderBox!.height).toBe(
            sectionsToggleBox!.y + sectionsToggleBox!.height,
        );
        await expect(
            page.locator(".chat-panel-trial .ant-welcome"),
        ).toBeVisible();
        await expect(
            page.locator(".chat-panel-trial .ant-prompts"),
        ).toBeVisible();
        await expect(
            page.locator(".chat-panel-trial .ant-sender"),
        ).toBeVisible();
        await expect(page.locator(".chat-panel-trial .message-list")).toHaveCSS(
            "padding",
            "0px",
        );
        await expect(
            page.locator(".chat-panel-trial .conversation-starter"),
        ).toHaveCSS("padding", "20px 16px 0px");
        const welcomeBox = await page
            .locator(".chat-panel-trial .ant-welcome")
            .boundingBox();
        const promptsBox = await page
            .locator(".chat-panel-trial .ant-prompts")
            .boundingBox();
        expect(welcomeBox).not.toBeNull();
        expect(promptsBox).not.toBeNull();
        expect(welcomeBox!.x).toBe(promptsBox!.x);
        await expect(
            page.locator(".chat-panel-trial .ant-welcome-title"),
        ).toHaveCSS("height", "32px");
        await expect(page.locator(".chat-panel-trial .composer")).toHaveCSS(
            "padding",
            "10px 16px 16px",
        );
        await expect(editorSections).toHaveCSS("width", "52px");
        await expect(
            editorSections
                .locator(".agent-editor-section-list > button")
                .first(),
        ).toHaveCSS("padding", "8px 0px");
        await expect(page.getByText("已发布为 v1")).toBeHidden();
        await page.screenshot({
            path: testInfo.outputPath("agent-trial-panel-dark-1080x720.png"),
            fullPage: true,
        });
        await page
            .getByRole("button", { name: "关闭试运行" })
            .dispatchEvent("click");
        await expect(editorSections).toHaveCSS("width", "176px");
        await page
            .getByRole("button", { name: "返回 Agent 空间" })
            .dispatchEvent("click");
        await expect(
            page.getByRole("heading", { name: "Agent 空间" }),
        ).toBeVisible();

        await application.evaluate(({ app }) => {
            app.emit("browser-window-blur", {} as never, null as never);
        });
        await expect(
            page.getByRole("heading", { name: "Agent 空间" }),
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
            page.getByRole("heading", { name: "Agent 空间" }),
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
