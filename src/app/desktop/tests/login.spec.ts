import { _electron as electron, expect, test } from "@playwright/test";
import electronPath from "electron";
import { createServer } from "node:http";
import type { InkwellDesktopApi } from "../src/shared/network/contracts.js";

const applicationEntry = "out/main/index.js";
const toolsResponse = JSON.stringify([
    {
        id: "0198a96d-19e4-7000-8000-000000000101",
        name: "get_current_datetime",
        description: "获取当前日期时间，可选指定 IANA 或 Windows 时区标识符。",
        parametersJsonSchema: JSON.stringify({
            type: "object",
            required: [],
            properties: {},
            additionalProperties: false,
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
        content: [
            "# 合同审查规范",
            "",
            "先识别合同类型，再按高、中、低三级输出风险。每项风险必须引用原文。",
            "",
            "## 审查步骤",
            "",
            "1. 确认合同主体、标的、金额、履行期限与争议解决条款。",
            "2. 识别权利义务不对等、责任上限缺失和单方变更等风险。",
            "3. 对每项风险引用原文，并给出可直接替换的修改建议。",
            "",
            "## 输出要求",
            "",
            "不得脱离合同原文推断未约定的事实；信息不足时应明确标记待确认事项。",
        ].join("\n"),
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
        hasUnpublishedChanges: true,
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
        hasUnpublishedChanges: false,
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
        hasUnpublishedChanges: false,
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
        expect(await application.evaluate(({ app }) => app.getName())).toBe(
            "Inkwell",
        );
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
    let agentRollbacks = 0;
    let agentAvatarUploads = 0;
    let agentShares = 0;
    let conversationMessageDeletes = 0;
    let conversationClears = 0;
    let conversationPageTwoRequests = 0;
    let messagePageTwoRequests = 0;
    const chatRequestUrls: string[] = [];
    const chatRunModes: (string | undefined)[] = [];
    const chatConversationIds: (string | undefined)[] = [];
    const conversationId = "0198a96d-19e4-7000-8000-000000000401";
    const historicalAgentVersionId = "0198a96d-19e4-7000-8000-000000000304";
    const historicalAgentInstructions = [
        "# 研发协作规则",
        "",
        "你是研发助手 v2，负责基于当前会话上下文整理可靠的研发结论。",
        "",
        "## 输出要求",
        "",
        "- 调用工具前先说明目的，调用后明确区分工具结果与推断。",
        "- 输出必须包含结论、依据、风险和后续行动。",
        "- 遇到缺失信息时直接列出待确认项，不得虚构实现细节。",
        "- 引用代码时给出准确路径，并优先提供可执行的验证步骤。",
        "- 对于破坏性操作，必须先说明影响范围并等待确认。",
        "- 保持表达简洁，避免重复用户已经明确提供的背景。",
        "- 长任务按阶段报告进展，但不要用无信息量的状态更新刷屏。",
        "- 最终回答应清楚说明已完成内容、验证结果和剩余风险。",
    ].join("\n");
    let conversationCreated = true;
    let persistedConversationMessages: Array<Record<string, unknown>> = [];
    const capturedPayloads: {
        agentCreate?: Record<string, unknown>;
        agentUpdate?: Record<string, unknown>;
        agentPublish?: Record<string, unknown>;
        agentRollback?: Record<string, unknown>;
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

        if (request.url === `/api/agents/${publishedAgent.id}/versions`) {
            response.setHeader("Content-Type", "application/json");
            response.end(
                JSON.stringify([
                    {
                        id: publishedAgent.currentPublishedVersionId,
                        agentId: publishedAgent.id,
                        versionNumber: 3,
                        snapshot: {
                            name: publishedAgent.name,
                            avatarUri: publishedAgent.avatarUri,
                            description: "最新发布的研发助手配置。",
                            instructions: "使用最新 v3 流程回答研发问题。",
                            buildOptions: publishedAgent.buildOptions,
                        },
                        ownerUserId: publishedAgent.ownerUserId,
                        ownerUserName: "admin",
                        changeSummary: "更新到第三版",
                        createdTime: "2026-07-20T08:00:00Z",
                        updatedTime: "2026-07-20T08:00:00Z",
                        publishedTime: "2026-07-20T08:00:00Z",
                    },
                    {
                        id: historicalAgentVersionId,
                        agentId: publishedAgent.id,
                        versionNumber: 2,
                        snapshot: {
                            name: publishedAgent.name,
                            avatarUri: publishedAgent.avatarUri,
                            description: "当前会话绑定的研发助手第二版。",
                            instructions: historicalAgentInstructions,
                            buildOptions: {
                                ...publishedAgent.buildOptions,
                                toolBindings: [
                                    {
                                        toolId: "0198a96d-19e4-7000-8000-000000000101",
                                        parametersJson: null,
                                    },
                                ],
                            },
                        },
                        ownerUserId: publishedAgent.ownerUserId,
                        ownerUserName: "admin",
                        changeSummary: "稳定研发分析流程",
                        createdTime: "2026-07-19T08:00:00Z",
                        updatedTime: "2026-07-19T08:00:00Z",
                        publishedTime: "2026-07-19T08:00:00Z",
                    },
                ]),
            );
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
                                  agentVersionId: historicalAgentVersionId,
                                  title:
                                      persistedConversationMessages.length > 0
                                          ? "验证正式发布版"
                                          : null,
                                  lastActivityTime: "2026-07-20T07:00:00Z",
                                  createdTime: "2026-07-20T07:00:00Z",
                              },
                          ]
                        : [],
                    totalCount: conversationCreated ? 2 : 0,
                    pagination: { page: 1, pageSize: 100 },
                }),
            );
            return;
        }

        if (
            request.url ===
                `/api/agents/${publishedAgent.id}/conversations?page=2&pageSize=100` &&
            request.method === "GET"
        ) {
            conversationPageTwoRequests += 1;
            response.setHeader("Content-Type", "application/json");
            response.end(
                JSON.stringify({
                    items: conversationCreated
                        ? [
                              {
                                  id: "0198a96d-19e4-7000-8000-000000000404",
                                  agentVersionId: historicalAgentVersionId,
                                  title: "更早的历史会话",
                                  lastActivityTime: "2026-07-19T07:00:00Z",
                                  createdTime: "2026-07-19T07:00:00Z",
                              },
                          ]
                        : [],
                    totalCount: conversationCreated ? 2 : 0,
                    pagination: { page: 2, pageSize: 100 },
                }),
            );
            return;
        }

        if (
            request.url === `/api/agents/${publishedAgent.id}/conversations` &&
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
                    items: persistedConversationMessages.slice(0, 1),
                    totalCount: persistedConversationMessages.length,
                    page: 1,
                    pageSize: 100,
                }),
            );
            return;
        }

        if (
            request.url ===
                `/api/agents/${publishedAgent.id}/conversations/${conversationId}/messages?page=2&pageSize=100` &&
            request.method === "GET"
        ) {
            messagePageTwoRequests += 1;
            response.setHeader("Content-Type", "application/json");
            response.end(
                JSON.stringify({
                    items: persistedConversationMessages.slice(1),
                    totalCount: persistedConversationMessages.length,
                    page: 2,
                    pageSize: 100,
                }),
            );
            return;
        }

        if (
            request.url ===
                `/api/agents/${publishedAgent.id}/conversations/${conversationId}/messages/0198a96d-19e4-7000-8000-000000000403` &&
            request.method === "DELETE"
        ) {
            conversationMessageDeletes += 1;
            persistedConversationMessages =
                persistedConversationMessages.filter(
                    (item) =>
                        item.id !== "0198a96d-19e4-7000-8000-000000000403",
                );
            response.statusCode = 204;
            response.end();
            return;
        }

        if (
            request.url ===
                `/api/agents/${publishedAgent.id}/conversations/${conversationId}/clear` &&
            request.method === "POST"
        ) {
            conversationClears += 1;
            persistedConversationMessages = [];
            response.statusCode = 204;
            response.end();
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
                const versionNumber = agentPublishes;
                const versionId =
                    versionNumber === 1
                        ? "0198a96d-19e4-7000-8000-000000000305"
                        : "0198a96d-19e4-7000-8000-000000000308";
                response.setHeader("Content-Type", "application/json");
                response.end(
                    JSON.stringify({
                        id: versionId,
                        agentId: editableAgent.id,
                        versionNumber,
                        snapshot: {
                            name: editableAgent.name,
                            avatarUri: editableAgent.avatarUri,
                            description: editableAgent.description,
                            instructions: editableAgent.instructions,
                            buildOptions: editableAgent.buildOptions,
                        },
                        ownerUserId: editableAgent.ownerUserId,
                        ownerUserName: "admin",
                        changeSummary: "补充头像与发布说明",
                        createdTime: "2026-07-19T00:01:00Z",
                        updatedTime: "2026-07-19T00:01:00Z",
                        publishedTime: "2026-07-19T00:01:00Z",
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

        if (
            request.url ===
                `/api/agents/${editableAgent.id}/versions/0198a96d-19e4-7000-8000-000000000305/rollback` &&
            request.method === "POST"
        ) {
            agentRollbacks += 1;
            const chunks: Buffer[] = [];
            request.on("data", (chunk: Buffer) => chunks.push(chunk));
            request.on("end", () => {
                capturedPayloads.agentRollback = JSON.parse(
                    Buffer.concat(chunks).toString(),
                ) as Record<string, unknown>;
                response.setHeader("Content-Type", "application/json");
                response.end(
                    JSON.stringify({
                        id: "0198a96d-19e4-7000-8000-000000000309",
                        agentId: editableAgent.id,
                        versionNumber: 3,
                        snapshot: {
                            name: editableAgent.name,
                            avatarUri: editableAgent.avatarUri,
                            description: editableAgent.description,
                            instructions: editableAgent.instructions,
                            buildOptions: editableAgent.buildOptions,
                        },
                        ownerUserId: editableAgent.ownerUserId,
                        ownerUserName: "admin",
                        changeSummary: "Rollback from v1",
                        createdTime: "2026-07-19T00:03:00Z",
                        updatedTime: "2026-07-19T00:03:00Z",
                        publishedTime: "2026-07-19T00:03:00Z",
                    }),
                );
            });
            return;
        }

        if (request.url === `/api/agents/${editableAgent.id}/versions`) {
            const versions = [
                ...(agentRollbacks > 0
                    ? [
                          {
                              id: "0198a96d-19e4-7000-8000-000000000309",
                              agentId: editableAgent.id,
                              versionNumber: 3,
                              snapshot: {
                                  name: editableAgent.name,
                                  avatarUri: editableAgent.avatarUri,
                                  description: editableAgent.description,
                                  instructions: editableAgent.instructions,
                                  buildOptions: editableAgent.buildOptions,
                              },
                              ownerUserId: editableAgent.ownerUserId,
                              ownerUserName: "admin",
                              changeSummary: "Rollback from v1",
                              createdTime: "2026-07-19T00:03:00Z",
                              updatedTime: "2026-07-19T00:03:00Z",
                              publishedTime: "2026-07-19T00:03:00Z",
                          },
                      ]
                    : []),
                ...(agentPublishes > 1
                    ? [
                          {
                              id: "0198a96d-19e4-7000-8000-000000000308",
                              agentId: editableAgent.id,
                              versionNumber: 2,
                              snapshot: {
                                  name: editableAgent.name,
                                  avatarUri: editableAgent.avatarUri,
                                  description: "整理第二版发布内容。",
                                  instructions: editableAgent.instructions,
                                  buildOptions: editableAgent.buildOptions,
                              },
                              ownerUserId: editableAgent.ownerUserId,
                              ownerUserName: "admin",
                              changeSummary: "补充第二版发布说明",
                              createdTime: "2026-07-19T00:02:00Z",
                              updatedTime: "2026-07-19T00:02:00Z",
                              publishedTime: "2026-07-19T00:02:00Z",
                          },
                      ]
                    : []),
                {
                    id: "0198a96d-19e4-7000-8000-000000000305",
                    agentId: editableAgent.id,
                    versionNumber: 1,
                    snapshot: {
                        name: editableAgent.name,
                        avatarUri: editableAgent.avatarUri,
                        description: editableAgent.description,
                        instructions: editableAgent.instructions,
                        buildOptions: editableAgent.buildOptions,
                    },
                    ownerUserId: editableAgent.ownerUserId,
                    ownerUserName: "admin",
                    changeSummary: "补充头像与发布说明",
                    createdTime: "2026-07-19T00:01:00Z",
                    updatedTime: "2026-07-19T00:01:00Z",
                    publishedTime: "2026-07-19T00:01:00Z",
                },
            ];
            response.setHeader("Content-Type", "application/json");
            response.end(JSON.stringify(versions));
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

        if (request.url?.startsWith("/agent/") && request.method === "POST") {
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
                const runInput = JSON.parse(
                    Buffer.concat(chunks).toString(),
                ) as { threadId: string; runId: string };
                const messageId = `${runInput.runId}:assistant`;
                response.write(
                    `data: ${JSON.stringify({ type: "RUN_STARTED", threadId: runInput.threadId, runId: runInput.runId })}\n\n`,
                );
                response.write(
                    `data: ${JSON.stringify({ type: "TEXT_MESSAGE_START", messageId, role: "assistant" })}\n\n`,
                );
                const characters = Array.from(content);
                let characterIndex = 0;
                const writeNextDelta = (): void => {
                    if (characterIndex >= characters.length) {
                        response.write(
                            `data: ${JSON.stringify({ type: "TEXT_MESSAGE_END", messageId })}\n\n`,
                        );
                        response.end(
                            `data: ${JSON.stringify({ type: "RUN_FINISHED", threadId: runInput.threadId, runId: runInput.runId })}\n\n`,
                        );
                        return;
                    }

                    const delta = characters
                        .slice(characterIndex, characterIndex + 2)
                        .join("");
                    characterIndex += 2;
                    response.write(
                        `data: ${JSON.stringify({ type: "TEXT_MESSAGE_CONTENT", messageId, delta })}\n\n`,
                    );
                    setTimeout(writeNextDelta, 1);
                };
                writeNextDelta();
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
        await expect(
            page.getByText("有未发布的修改", { exact: true }),
        ).toBeVisible();
        const agentCards = page.locator(".agent-space-card");
        const agentCardTexts = await agentCards.allInnerTexts();
        for (const agentCardText of agentCardTexts) {
            expect(agentCardText).not.toContain(
                "0198a96d-19e4-7000-8000-000000000001",
            );
            expect(agentCardText).not.toMatch(/更新于|分钟前|小时前|天前/);
        }

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
        await expect(page.getByText("版本：v2", { exact: true })).toBeVisible();
        await expect(page.getByText("会话版本", { exact: false })).toHaveCount(
            0,
        );
        await page.evaluate(() => {
            Object.defineProperty(navigator, "clipboard", {
                configurable: true,
                value: {
                    writeText: async (text: string) => {
                        window.sessionStorage.setItem(
                            "copied-instructions",
                            text,
                        );
                    },
                },
            });
        });
        await page
            .getByRole("button", { name: "查看 Agent 详情" })
            .dispatchEvent("click");
        const chatAgentDetails = page.getByRole("dialog", {
            name: "Agent 详情",
        });
        await expect(chatAgentDetails).toBeVisible();
        await expect(
            chatAgentDetails.getByText("版本：v2", { exact: true }),
        ).toBeVisible();
        await expect(
            chatAgentDetails.getByText("当前会话绑定的研发助手第二版。", {
                exact: true,
            }),
        ).toBeVisible();
        await expect(
            chatAgentDetails.getByRole("heading", {
                name: "研发协作规则",
            }),
        ).toBeVisible();
        await expect(
            chatAgentDetails.locator(".agent-details-instructions .x-markdown"),
        ).toBeVisible();
        await expect(
            chatAgentDetails.getByText("最新发布的研发助手配置。", {
                exact: true,
            }),
        ).toHaveCount(0);
        await expect(
            chatAgentDetails.getByText("已发布", { exact: true }),
        ).toHaveCount(0);
        await expect(
            chatAgentDetails.getByRole("button", { name: "展开全文" }),
        ).toBeVisible();
        await chatAgentDetails
            .getByRole("button", { name: "展开全文" })
            .dispatchEvent("click");
        await expect(
            chatAgentDetails.getByRole("button", { name: "收起" }),
        ).toBeVisible();
        await chatAgentDetails
            .getByRole("button", { name: "复制 Instructions" })
            .dispatchEvent("click");
        await expect(page.getByText("Instructions 已复制")).toBeVisible();
        expect(
            await page.evaluate(() =>
                window.sessionStorage.getItem("copied-instructions"),
            ),
        ).toBe(historicalAgentInstructions);
        await chatAgentDetails
            .getByRole("button", { name: "关闭 Agent 详情" })
            .dispatchEvent("click");
        await expect(chatAgentDetails).toBeHidden();
        await expect.poll(() => conversationPageTwoRequests).toBeGreaterThan(0);
        await expect(
            page
                .locator(".chat-history")
                .getByText("更早的历史会话", { exact: true }),
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
            page.getByText("请介绍你能提供哪些帮助，并给出几个具体示例", {
                exact: true,
            }),
        ).toBeVisible();
        await page.screenshot({
            path: testInfo.outputPath("agent-chat-published-dark-1080x720.png"),
            fullPage: true,
        });
        const publishedSender = page.getByPlaceholder(
            "输入消息，Enter 发送，Shift + Enter 换行",
        );
        await page.evaluate(() => {
            const metrics = {
                runningContentUpdates: 0,
                completed: false,
                visualContentLengths: [] as number[],
            };
            Object.assign(window, { __inkwellStreamMetrics: metrics });
            const observer = new MutationObserver(() => {
                const markdown = Array.from(
                    document.querySelectorAll(
                        ".chat-full-messages .markdown-content-frame",
                    ),
                ).at(-1);
                const contentLength = Number(
                    markdown?.getAttribute("data-stream-content-length") ?? 0,
                );
                if (
                    contentLength > 0 &&
                    metrics.visualContentLengths.at(-1) !== contentLength
                ) {
                    metrics.visualContentLengths.push(contentLength);
                }
            });
            observer.observe(document.body, {
                attributes: true,
                attributeFilter: ["data-stream-content-length"],
                childList: true,
                subtree: true,
            });
            const desktop = (
                globalThis as unknown as { inkwell: InkwellDesktopApi }
            ).inkwell;
            desktop.onChatRunChanged((snapshot) => {
                if (snapshot.status === "running" && snapshot.content) {
                    metrics.runningContentUpdates += 1;
                }
                if (snapshot.status === "completed") {
                    metrics.completed = true;
                }
            });
        });
        await publishedSender.fill("验证正式发布版");
        await publishedSender.press("Enter");
        await expect(
            page.locator(".chat-full-messages .x-markdown h1"),
        ).toHaveText("运行成功");
        await expect
            .poll(() =>
                page.evaluate(
                    () =>
                        (
                            window as typeof window & {
                                __inkwellStreamMetrics: {
                                    completed: boolean;
                                };
                            }
                        ).__inkwellStreamMetrics.completed,
                ),
            )
            .toBe(true);
        await expect(
            page.locator(".chat-full-messages .x-markdown").last(),
        ).toContainText("第 80 段用于验证长回复滚动行为的内容。");
        const streamMetrics = await page.evaluate(
            () =>
                (
                    window as typeof window & {
                        __inkwellStreamMetrics: {
                            runningContentUpdates: number;
                            visualContentLengths: number[];
                        };
                    }
                ).__inkwellStreamMetrics,
        );
        expect(streamMetrics.runningContentUpdates).toBeGreaterThan(2);
        expect(streamMetrics.runningContentUpdates).toBeLessThan(200);
        expect(streamMetrics.visualContentLengths.length).toBeGreaterThan(
            streamMetrics.runningContentUpdates,
        );
        const visualIncrements = streamMetrics.visualContentLengths
            .slice(1)
            .map(
                (length, index) =>
                    length - streamMetrics.visualContentLengths[index],
            )
            .filter((increment) => increment > 0);
        expect(Math.max(...visualIncrements)).toBeLessThanOrEqual(48);
        await expect.poll(() => messagePageTwoRequests).toBeGreaterThan(0);
        await expect(
            page
                .locator(".chat-history")
                .getByText("验证正式发布版", { exact: true }),
        ).toBeVisible();
        const chatQuickPrompts = page.locator(
            ".chat-quick-prompts-full .ant-prompts-item",
        );
        await expect(chatQuickPrompts).toHaveCount(3);
        await expect(chatQuickPrompts).toHaveText([
            "请介绍你能提供哪些帮助，并给出几个具体示例",
            "根据你的能力，推荐三个适合立即开始的任务",
            "告诉我怎样提问能让你给出更好的回答",
        ]);
        await expect(chatQuickPrompts.first()).toHaveCSS(
            "font-family",
            '"PingFang SC", "Microsoft YaHei", sans-serif',
        );
        await expect(chatQuickPrompts.first()).toHaveCSS("font-size", "12px");
        await expect(chatQuickPrompts.first()).toHaveCSS("font-weight", "400");
        await expect(chatQuickPrompts.first()).toHaveCSS(
            "background-color",
            "rgba(0, 0, 0, 0.06)",
        );
        await expect(chatQuickPrompts.first()).toHaveCSS(
            "border",
            "1px solid rgb(211, 206, 218)",
        );
        const publishedBubbles = page.locator(
            ".chat-full-messages .ant-bubble",
        );
        await expect(publishedBubbles).toHaveCount(2);
        await expect(
            publishedBubbles.first().locator(".x-markdown"),
        ).toBeVisible();
        await expect(
            publishedBubbles.last().locator(".x-markdown"),
        ).toBeVisible();
        const publishedBubbleMetrics = await publishedBubbles.evaluateAll(
            (elements) =>
                elements.map((element) => {
                    const content = element.querySelector(
                        ".ant-bubble-content",
                    ) as HTMLElement;
                    const body = element.querySelector(
                        ".ant-bubble-body",
                    ) as HTMLElement;
                    const bubbleStyles = getComputedStyle(element);
                    const styles = getComputedStyle(content);
                    return {
                        rowWidth: element.getBoundingClientRect().width,
                        bodyWidth: body.getBoundingClientRect().width,
                        width: content.getBoundingClientRect().width,
                        maxWidth: styles.maxWidth,
                        padding: styles.padding,
                        paddingInlineEnd: Number.parseFloat(
                            bubbleStyles.paddingInlineEnd,
                        ),
                    };
                }),
        );
        const publishedScrollWidth = await page
            .locator(".chat-full-messages .ant-bubble-list-scroll-box")
            .evaluate((element) => element.clientWidth);
        expect(publishedBubbleMetrics.every(({ width }) => width > 0)).toBe(
            true,
        );
        expect(
            publishedBubbleMetrics.every(
                ({ width }) => width <= publishedScrollWidth,
            ),
        ).toBe(true);
        expect(publishedBubbleMetrics.map(({ maxWidth }) => maxWidth)).toEqual([
            "100%",
            "100%",
        ]);
        const publishedAssistantMetrics = publishedBubbleMetrics[1];
        expect(publishedAssistantMetrics.paddingInlineEnd).toBeCloseTo(
            publishedAssistantMetrics.rowWidth * 0.15,
            0,
        );
        expect(publishedAssistantMetrics.bodyWidth).toBeLessThanOrEqual(
            publishedAssistantMetrics.rowWidth * 0.85 + 1,
        );
        expect(
            new Set(publishedBubbleMetrics.map(({ padding }) => padding)).size,
        ).toBe(1);
        const publishedMarkdownStyles = await publishedBubbles
            .locator(".x-markdown")
            .evaluateAll((elements) =>
                elements.map((element) => {
                    const styles = getComputedStyle(element);
                    return {
                        fontFamily: styles.fontFamily,
                        fontSize: styles.fontSize,
                        lineHeight: styles.lineHeight,
                    };
                }),
            );
        expect(publishedMarkdownStyles[0]).toEqual(publishedMarkdownStyles[1]);
        const publishedScrollBox = page.locator(
            ".chat-full-messages .ant-bubble-list-scroll-box",
        );
        const scrollToLatestButton = page.getByRole("button", {
            name: "滚动到最新消息",
        });
        await expect(scrollToLatestButton).toBeHidden();
        await expect
            .poll(() =>
                publishedScrollBox.evaluate(
                    (element) => element.scrollHeight > element.clientHeight,
                ),
            )
            .toBe(true);
        await publishedScrollBox.evaluate((element) => {
            element.scrollTop = -element.scrollHeight;
        });
        await expect(scrollToLatestButton).toBeVisible();
        await page.screenshot({
            path: testInfo.outputPath(
                "agent-chat-scroll-to-latest-dark-1080x720.png",
            ),
            fullPage: true,
        });
        await scrollToLatestButton.dispatchEvent("click");
        await expect(scrollToLatestButton).toBeHidden();
        await expect
            .poll(() =>
                publishedScrollBox.evaluate(
                    (element) => Math.abs(element.scrollTop) <= 1,
                ),
            )
            .toBe(true);
        const publishedActions = publishedBubbles
            .last()
            .locator(".chat-message-actions");
        await expect(publishedActions.locator(".ant-actions-icon")).toHaveCount(
            1,
        );
        await expect(page.getByLabel("复制第 2 条消息")).toBeVisible();
        await expect(
            publishedActions.locator(".ant-actions-feedback-item-like"),
        ).toBeVisible();
        await expect(
            publishedActions.locator(".ant-actions-feedback-item-dislike"),
        ).toBeVisible();
        await publishedActions
            .locator(".ant-actions-feedback-item-like")
            .dispatchEvent("click");
        await expect(
            publishedActions.locator(".ant-actions-feedback-item-like"),
        ).toHaveClass(/ant-actions-feedback-item-like-active/);
        await expect(page.getByLabel(/删除第 \d+ 条消息/)).toHaveCount(0);
        expect(conversationMessageDeletes).toBe(0);
        await page
            .getByRole("button", { name: "清空当前会话" })
            .dispatchEvent("click");
        const clearConversationDialog = page.getByRole("dialog", {
            name: "清空当前会话？",
        });
        await clearConversationDialog
            .getByRole("button", { name: "确认清空" })
            .dispatchEvent("click");
        await expect.poll(() => conversationClears).toBe(1);
        await expect(page.locator(".chat-full-messages")).toHaveCount(0);
        await expect(
            page.locator(".chat-history").getByText("新会话", { exact: true }),
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
            page
                .locator(".chat-page-header")
                .getByText("版本：v3", { exact: true }),
        ).toBeVisible();
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
        const sharedAgentCard = page
            .locator(".agent-space-card")
            .filter({ hasText: "合同审查助手" });
        await sharedAgentCard.hover();
        await sharedAgentCard
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
        const sharedAgentCardAfterReturn = page
            .locator(".agent-space-card")
            .filter({ hasText: "合同审查助手" });
        await sharedAgentCardAfterReturn.hover();
        await sharedAgentCardAfterReturn
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
        const languageOptions = page.locator(".appearance-options").first();
        await expect(
            languageOptions.getByText("跟随系统", { exact: true }),
        ).toBeVisible();
        const expectedSystemLocale = await page.evaluate(() => {
            const language = navigator.languages[0]?.toLowerCase();
            if (language?.startsWith("en")) return "en-US";
            return "zh-CN";
        });
        await languageOptions
            .getByText("跟随系统", { exact: true })
            .dispatchEvent("click");
        await expect(page.locator("html")).toHaveAttribute(
            "data-locale",
            expectedSystemLocale,
        );
        await languageOptions
            .getByText("English", { exact: true })
            .dispatchEvent("click");
        await expect(
            page.getByRole("dialog", { name: "Preferences" }),
        ).toBeVisible();
        await expect(
            languageOptions.getByText("System", { exact: true }),
        ).toBeVisible();
        await expect(
            page.getByText("Workspace", { exact: true }),
        ).toBeVisible();
        await expect(page.locator("html")).toHaveAttribute(
            "data-locale",
            "en-US",
        );
        await page.reload();
        await expect(
            page.getByText("Workspace", { exact: true }),
        ).toBeVisible();
        await expect(page.locator("html")).toHaveAttribute(
            "data-locale",
            "en-US",
        );
        await page
            .getByRole("button", { name: "Open user menu" })
            .dispatchEvent("click");
        await page
            .getByText("Preferences", { exact: true })
            .dispatchEvent("click");
        await page
            .getByText("简体中文", { exact: true })
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
            "操作",
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
        const firstModelRow = modelTable.locator(".ant-table-row").first();
        await expect(
            firstModelRow.getByRole("button", { name: "查看 gpt-5.4" }),
        ).toHaveCSS("border-style", "solid");
        await expect(
            firstModelRow.getByRole("button", { name: "测试 gpt-5.4" }),
        ).toHaveCSS("border-style", "solid");
        await firstModelRow
            .getByRole("button", { name: "查看 gpt-5.4" })
            .dispatchEvent("click");
        const modelDetails = page.getByRole("dialog", { name: "模型详情" });
        await expect(modelDetails).toBeVisible();
        await expect(
            modelDetails.getByText("Token 上限", { exact: true }),
        ).toBeVisible();
        await expect(
            modelDetails.getByText("1,050,000", { exact: true }),
        ).toBeVisible();
        await expect(
            modelDetails.getByText("128,000", { exact: true }),
        ).toBeVisible();
        await expect(
            modelDetails.locator(".ant-tag-success").first(),
        ).toHaveCSS("background-color", "rgb(32, 43, 36)");
        await modelDetails
            .getByRole("button", { name: "关闭模型详情" })
            .dispatchEvent("click");
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
            toolTable.getByText("get_current_datetime", { exact: true }),
        ).toBeVisible();
        await expect(
            toolTable.getByText("0 项", { exact: true }),
        ).toBeVisible();
        const toolPage = page.locator(".inkwell-data-list-page").filter({
            has: page.getByRole("heading", { name: "工具", exact: true }),
        });
        const toolPagination = toolPage.locator(
            ".inkwell-data-list-pagination",
        );
        const toolPaginationY = await toolPagination.evaluate(
            (element) => element.getBoundingClientRect().y,
        );
        const toolTableBody = toolPage.locator(".ant-table-body");
        const toolTableBodyHeight = await toolTableBody.evaluate(
            (element) => element.getBoundingClientRect().height,
        );
        await page.getByPlaceholder("搜索名称或描述").fill("不存在的工具");
        await expect(toolTable.locator(".ant-empty")).toBeVisible();
        expect(
            await toolPagination.evaluate(
                (element) => element.getBoundingClientRect().y,
            ),
        ).toBeCloseTo(toolPaginationY, 1);
        await page.getByPlaceholder("搜索名称或描述").clear();
        await expect(
            toolTable.getByText("get_current_datetime", { exact: true }),
        ).toBeVisible();
        await page.setViewportSize({ width: 1080, height: 900 });
        await expect
            .poll(() =>
                toolPagination.evaluate(
                    (element) => element.getBoundingClientRect().y,
                ),
            )
            .toBeCloseTo(toolPaginationY + 180, 0);
        await expect
            .poll(() =>
                toolTableBody.evaluate(
                    (element) => element.getBoundingClientRect().height,
                ),
            )
            .toBeCloseTo(toolTableBodyHeight + 180, 0);
        await page.setViewportSize({ width: 1080, height: 720 });
        await expect
            .poll(() =>
                toolPagination.evaluate(
                    (element) => element.getBoundingClientRect().y,
                ),
            )
            .toBeCloseTo(toolPaginationY, 0);
        await page.screenshot({
            path: testInfo.outputPath("tool-management-dark-1080x720.png"),
            fullPage: true,
        });
        await page
            .getByRole("button", { name: "查看 get_current_datetime" })
            .dispatchEvent("click");
        const toolDetails = page.getByRole("dialog", { name: "Tool 详情" });
        await expect(toolDetails).toBeVisible();
        await expect(toolDetails.getByText("此工具没有参数")).toBeVisible();
        await toolDetails
            .getByText("查看原始 Schema", { exact: true })
            .dispatchEvent("click");
        await expect(
            toolDetails.getByText(/"additionalProperties":false/),
        ).toBeVisible();
        await toolDetails
            .getByRole("button", { name: "关闭 Tool 详情" })
            .dispatchEvent("click");
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
        ).toHaveCount(0);
        await expect(
            skillDetails.getByText("SKILL.md", { exact: true }),
        ).toBeVisible();
        await expect(
            skillDetails.getByText("References", { exact: true }),
        ).toBeVisible();
        await expect(skillDetails.locator("input, textarea")).toHaveCount(0);
        await expect(
            skillDetails.getByRole("button", { name: "编辑" }),
        ).toHaveCount(0);
        const skillMarkdown = skillDetails.locator(".skill-details-markdown");
        await expect(skillMarkdown).toHaveClass(/collapsed/);
        await skillDetails
            .getByRole("button", { name: "展开全文" })
            .dispatchEvent("click");
        await expect(skillMarkdown).toHaveClass(/expanded/);
        await skillDetails
            .getByRole("button", { name: "关闭 Skill 详情" })
            .dispatchEvent("click");
        await page
            .getByRole("button", { name: "编辑 合同审查规范" })
            .dispatchEvent("click");
        const skillEditorDialog = page.getByRole("dialog", {
            name: "编辑 Skill",
        });
        const skillEditor = skillEditorDialog.locator(
            ".skill-markdown-editor .monaco-editor",
        );
        await expect(skillEditor).toBeVisible();
        await expect(skillEditor).toHaveCSS("height", "480px");
        await skillEditorDialog
            .getByRole("button", { name: "关闭 Skill 详情" })
            .dispatchEvent("click");
        await page
            .getByRole("button", { name: "上传 Skill" })
            .dispatchEvent("click");
        const uploadDialog = page.getByRole("dialog", { name: "上传 Skill" });
        await uploadDialog.locator('input[type="file"]').setInputFiles({
            name: "SKILL.md",
            mimeType: "text/markdown",
            buffer: Buffer.from(
                "---\nname: contract-review\ndescription: 按团队法务标准识别合同风险并输出分级建议。\n---\n# 合同审查规范",
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
        const agentPagination = page
            .locator(".agent-space-page")
            .locator(".agent-space-pagination");
        await expect(firstAgentCard).toBeVisible();
        const agentPaginationY = await agentPagination.evaluate(
            (element) => element.getBoundingClientRect().y,
        );
        await page.getByPlaceholder("搜索 Agent").fill("研发");
        expect(
            await agentPagination.evaluate(
                (element) => element.getBoundingClientRect().y,
            ),
        ).toBeCloseTo(agentPaginationY, 1);
        await page.getByPlaceholder("搜索 Agent").clear();
        await page.setViewportSize({ width: 1080, height: 900 });
        await expect
            .poll(() =>
                agentPagination.evaluate(
                    (element) => element.getBoundingClientRect().y,
                ),
            )
            .toBeCloseTo(agentPaginationY + 180, 0);
        await page.setViewportSize({ width: 1080, height: 720 });
        await expect
            .poll(() =>
                agentPagination.evaluate(
                    (element) => element.getBoundingClientRect().y,
                ),
            )
            .toBeCloseTo(agentPaginationY, 0);
        const firstAgentActions = firstAgentCard.locator(".agent-card-actions");
        const firstAgentAction = firstAgentActions.locator(".ant-btn").first();
        await expect(firstAgentActions).toHaveCSS("opacity", "0");
        await expect(firstAgentAction.locator(".anticon")).toHaveCount(1);
        await expect(firstAgentAction).toHaveText("");
        await firstAgentCard.hover();
        await expect(firstAgentActions).toHaveCSS("opacity", "1");
        await expect(firstAgentAction).toHaveAttribute(
            "aria-label",
            /^(编辑|查看|共享|撤销) /,
        );
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
        expect(firstAgentCardMetrics.width).toBeCloseTo(157.6, 1);
        expect(firstAgentCardMetrics).toMatchObject({
            x: 220,
            height: 148,
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
        const instructionsEditor = page.locator(
            ".agent-instructions-editor .monaco-editor",
        );
        await expect(instructionsEditor).toBeVisible();
        await expect(instructionsEditor).toHaveCSS("height", "480px");
        const instructionsInput = instructionsEditor.locator("textarea");
        await instructionsInput.focus();
        await page.keyboard.press("Meta+A");
        await page.keyboard.insertText("输出简洁的发布说明。");
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
        const firstBindingItem = page.locator(".agent-binding-item").first();
        await expect(firstBindingItem).toHaveCSS("padding", "14px");
        const bindingSelectorWidth = await page
            .locator("#toolIds")
            .evaluate((element) => element.getBoundingClientRect().width);
        const bindingItemWidth = await firstBindingItem.evaluate(
            (element) => element.getBoundingClientRect().width,
        );
        expect(bindingSelectorWidth - bindingItemWidth).toBeCloseTo(2, 0);
        await expect(
            firstBindingItem.locator(".agent-binding-item-icon"),
        ).toBeVisible();
        const toolBindingCheckbox = firstBindingItem.getByRole("checkbox");
        await toolBindingCheckbox.dispatchEvent("click");
        await expect(toolBindingCheckbox).toBeChecked();
        await expect(firstBindingItem).toHaveClass(/selected/);
        await expect(toolBindingCheckbox.locator("xpath=..")).toHaveClass(
            /ant-checkbox-checked/,
        );
        expect(
            await page
                .locator("html")
                .evaluate((element) =>
                    getComputedStyle(element)
                        .getPropertyValue("--primary-soft")
                        .trim()
                        .toLowerCase(),
                ),
        ).toBe("#533730");
        await expect(firstBindingItem).toHaveCSS(
            "background-color",
            "rgb(83, 55, 48)",
        );
        await expect(
            firstBindingItem.locator(".agent-binding-config"),
        ).toHaveCount(0);
        await page.evaluate(() => window.getSelection()?.removeAllRanges());
        await page.screenshot({
            path: testInfo.outputPath("agent-tools-editor-dark-1080x720.png"),
            fullPage: true,
        });
        await toolBindingCheckbox.dispatchEvent("click");
        await expect(toolBindingCheckbox).not.toBeChecked();
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
        await expect(page.locator(".agent-skill-stages")).toHaveCount(0);
        const skillBindingCheckbox = page.getByRole("checkbox", {
            name: "合同审查规范",
        });
        await skillBindingCheckbox.dispatchEvent("click");
        await expect(skillBindingCheckbox).toBeChecked();
        await expect(
            skillBindingCheckbox.locator(
                "xpath=ancestor::div[contains(concat(' ', normalize-space(@class), ' '), ' agent-binding-item ')]",
            ),
        ).toHaveClass(/selected/);
        await page.evaluate(() => window.getSelection()?.removeAllRanges());
        await page.screenshot({
            path: testInfo.outputPath("agent-skills-editor-dark-1080x720.png"),
            fullPage: true,
        });
        await skillBindingCheckbox.dispatchEvent("click");
        await expect(skillBindingCheckbox).not.toBeChecked();
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
        await page.evaluate(() => {
            const metrics = { runningContentUpdates: 0, completed: false };
            Object.assign(window, { __inkwellTrialStreamMetrics: metrics });
            const desktop = (
                globalThis as unknown as { inkwell: InkwellDesktopApi }
            ).inkwell;
            desktop.onChatRunChanged((snapshot) => {
                if (snapshot.status === "running" && snapshot.content) {
                    metrics.runningContentUpdates += 1;
                }
                if (snapshot.status === "completed") {
                    metrics.completed = true;
                }
            });
        });
        await draftTrialSender.fill("验证未发布草稿");
        await draftTrialSender.press("Enter");
        await expect(page.getByText("运行成功")).toBeVisible();
        await expect
            .poll(() =>
                page.evaluate(
                    () =>
                        (
                            window as typeof window & {
                                __inkwellTrialStreamMetrics: {
                                    completed: boolean;
                                };
                            }
                        ).__inkwellTrialStreamMetrics.completed,
                ),
            )
            .toBe(true);
        const trialRunningContentUpdates = await page.evaluate(
            () =>
                (
                    window as typeof window & {
                        __inkwellTrialStreamMetrics: {
                            runningContentUpdates: number;
                        };
                    }
                ).__inkwellTrialStreamMetrics.runningContentUpdates,
        );
        expect(trialRunningContentUpdates).toBeGreaterThan(2);
        expect(trialRunningContentUpdates).toBeLessThan(200);
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
        const trialBubbles = page.locator(
            ".chat-panel-trial .chat-bubble-list .ant-bubble",
        );
        await expect(trialBubbles.first().locator(".x-markdown")).toBeVisible();
        const trialBubbleMetrics = await trialBubbles.evaluateAll((elements) =>
            elements.map((element) => {
                const content = element.querySelector(
                    ".ant-bubble-content",
                ) as HTMLElement;
                const body = element.querySelector(
                    ".ant-bubble-body",
                ) as HTMLElement;
                const bubbleStyles = getComputedStyle(element);
                const styles = getComputedStyle(content);
                return {
                    rowWidth: element.getBoundingClientRect().width,
                    bodyWidth: body.getBoundingClientRect().width,
                    width: content.getBoundingClientRect().width,
                    maxWidth: styles.maxWidth,
                    padding: styles.padding,
                    paddingInlineEnd: Number.parseFloat(
                        bubbleStyles.paddingInlineEnd,
                    ),
                };
            }),
        );
        const trialScrollWidth = await page
            .locator(".chat-panel-trial .ant-bubble-list-scroll-box")
            .evaluate((element) => element.clientWidth);
        expect(
            trialBubbleMetrics.every(
                ({ width }) => width > 0 && width <= trialScrollWidth,
            ),
        ).toBe(true);
        expect(trialBubbleMetrics.map(({ maxWidth }) => maxWidth)).toEqual([
            "100%",
            "100%",
        ]);
        const trialAssistantMetrics = trialBubbleMetrics[1];
        expect(trialAssistantMetrics.paddingInlineEnd).toBeCloseTo(
            trialAssistantMetrics.rowWidth * 0.15,
            0,
        );
        expect(trialAssistantMetrics.bodyWidth).toBeLessThanOrEqual(
            trialAssistantMetrics.rowWidth * 0.85 + 1,
        );
        expect(
            new Set(trialBubbleMetrics.map(({ padding }) => padding)).size,
        ).toBe(1);
        const trialMarkdownStyles = await trialBubbles
            .locator(".x-markdown")
            .evaluateAll((elements) =>
                elements.map((element) => {
                    const styles = getComputedStyle(element);
                    return {
                        fontFamily: styles.fontFamily,
                        fontSize: styles.fontSize,
                        lineHeight: styles.lineHeight,
                    };
                }),
            );
        expect(trialMarkdownStyles[0]).toEqual(trialMarkdownStyles[1]);
        const trialActions = trialBubbles
            .last()
            .locator(".chat-message-actions");
        await expect(trialActions.locator(".ant-actions-icon")).toHaveCount(1);
        await expect(page.getByLabel("复制第 2 条消息")).toBeVisible();
        await expect(
            trialActions.locator(".ant-actions-feedback-item-like"),
        ).toBeVisible();
        await expect(
            trialActions.locator(".ant-actions-feedback-item-dislike"),
        ).toBeVisible();
        await expect(page.getByLabel(/删除第 \d+ 条消息/)).toHaveCount(0);
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
        await expect(
            page
                .locator(".chat-panel-trial")
                .getByRole("button", { name: "滚动到最新消息" }),
        ).toHaveCount(0);
        await expect(page.locator(".chat-bubble-list .ant-bubble")).toHaveCount(
            2,
        );
        expect(chatRequestUrls).toEqual([
            "/agent/0198a96d-19e4-7000-8000-000000000301",
            `/agent/${sharedAgent.id}`,
            `/agent/${editableAgent.id}?version=draft`,
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
        await page
            .getByRole("button", { name: "基础信息" })
            .dispatchEvent("click");
        await page.getByLabel("描述").fill("整理第二版发布内容。");
        await publishButton.dispatchEvent("click");
        await publishDialog
            .getByPlaceholder("说明本次修改的内容，会记录到版本历史里")
            .fill("补充第二版发布说明");
        await page
            .locator(".ant-modal-footer:visible .ant-btn-primary")
            .dispatchEvent("click");
        await expect(publishDialog).toBeHidden();
        await expect(page.getByText("已发布为 v2")).toBeVisible();
        expect(agentUpdates).toBe(2);
        expect(agentPublishes).toBe(2);
        expect(capturedPayloads.agentPublish?.changeSummary).toBe(
            "补充第二版发布说明",
        );
        await page.getByRole("button", { name: "版本" }).dispatchEvent("click");
        await expect(
            page.getByRole("columnheader", { name: "变更摘要" }),
        ).toBeVisible();
        await expect(
            page.getByText("补充头像与发布说明", { exact: true }),
        ).toBeVisible();
        const firstVersionRow = page
            .locator(".ant-table-row")
            .filter({ hasText: "补充头像与发布说明" });
        await firstVersionRow
            .getByRole("button", { name: "查看" })
            .dispatchEvent("click");
        const versionAgentDetails = page.getByRole("dialog", {
            name: "Agent 详情",
        });
        await expect(versionAgentDetails).toBeVisible();
        await expect(
            versionAgentDetails.getByText("历史版本", { exact: true }),
        ).toBeVisible();
        await expect(
            versionAgentDetails.getByText("版本：v1", { exact: true }),
        ).toBeVisible();
        await versionAgentDetails
            .getByRole("button", { name: "回滚到本版" })
            .dispatchEvent("click");
        const rollbackDialog = page.getByRole("dialog", {
            name: "回滚到 v1？",
        });
        await rollbackDialog
            .getByRole("button", { name: "确认回滚" })
            .dispatchEvent("click");
        await expect.poll(() => agentRollbacks).toBe(1);
        expect(capturedPayloads.agentRollback?.changeSummary).toBeNull();
        await expect(page.getByText("已回滚，生成新版本 v3")).toBeVisible();
        await expect(
            page.getByText("Rollback from v1", { exact: true }),
        ).toBeVisible();
        await page
            .getByRole("button", { name: "基础信息" })
            .dispatchEvent("click");
        await expect(page.getByLabel("描述")).toHaveValue(
            editableAgent.description,
        );
        await page
            .getByRole("button", { name: "Instructions" })
            .dispatchEvent("click");
        await expect(
            page.locator(".agent-instructions-editor .view-lines"),
        ).toContainText(editableAgent.instructions);
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
            page.getByText("请介绍你能提供哪些帮助，并给出几个具体示例", {
                exact: true,
            }),
        ).toBeVisible();
        await expect(
            page.getByText("根据你的能力，推荐三个适合立即开始的任务", {
                exact: true,
            }),
        ).toBeVisible();
        await expect(
            page.getByText("告诉我怎样提问能让你给出更好的回答", {
                exact: true,
            }),
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
        await expect(
            page.locator(".chat-panel-trial .chat-composer-trial"),
        ).toHaveCSS("padding", "10px 16px 16px");
        await expect(
            page.locator(".chat-panel-trial .chat-quick-prompts-trial button"),
        ).toHaveCount(3);
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

test("preserves, stops, and retries chat runs through Electron", async ({
    browserName,
}, testInfo) => {
    test.setTimeout(45_000);
    let conversationSequence = 0;
    let rateLimitedAttempts = 0;
    let stoppedConnectionClosed = false;
    const conversationIds: string[] = [];
    const persistedMessages = new Map<string, Array<Record<string, unknown>>>();

    const server = createServer((request, response) => {
        if (request.url === "/api/auth/login") {
            response.setHeader("Content-Type", "application/json");
            response.end(
                JSON.stringify({
                    userId: "0198a96d-19e4-7000-8000-000000000001",
                    username: "admin",
                    isAdmin: true,
                    mustChangePassword: false,
                    sessionToken: "chat-run-session-token",
                    expiresAt: "2026-07-22T00:00:00Z",
                }),
            );
            return;
        }

        if (request.url === "/api/auth/unlock") {
            response.statusCode = 204;
            response.end();
            return;
        }

        if (request.url === "/api/agents/mine") {
            response.setHeader("Content-Type", "application/json");
            response.end(myAgentsResponse);
            return;
        }

        if (request.url === "/api/agents/shared") {
            response.setHeader("Content-Type", "application/json");
            response.end("[]");
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
                    items: conversationIds.map((id) => ({
                        id,
                        agentVersionId:
                            publishedAgent.currentPublishedVersionId,
                        title: "Chat run E2E",
                        lastActivityTime: "2026-07-21T05:00:00Z",
                        createdTime: "2026-07-21T05:00:00Z",
                    })),
                    totalCount: conversationIds.length,
                    page: 1,
                    pageSize: 100,
                }),
            );
            return;
        }

        if (
            request.url === `/api/agents/${publishedAgent.id}/conversations` &&
            request.method === "POST"
        ) {
            conversationSequence += 1;
            const id = `0198a96d-19e4-7000-8000-00000000041${conversationSequence}`;
            conversationIds.unshift(id);
            persistedMessages.set(id, []);
            response.statusCode = 201;
            response.setHeader("Content-Type", "application/json");
            response.end(
                JSON.stringify({
                    id,
                    agentId: publishedAgent.id,
                    agentVersionId: publishedAgent.currentPublishedVersionId,
                    title: null,
                    lastActivityTime: "2026-07-21T05:00:00Z",
                    createdTime: "2026-07-21T05:00:00Z",
                    updatedTime: "2026-07-21T05:00:00Z",
                }),
            );
            return;
        }

        const messagesMatch = request.url?.match(
            /^\/api\/agents\/[^/]+\/conversations\/([^/]+)\/messages\?page=1&pageSize=100$/,
        );
        if (messagesMatch && request.method === "GET") {
            const items = persistedMessages.get(messagesMatch[1]) ?? [];
            response.setHeader("Content-Type", "application/json");
            response.end(
                JSON.stringify({
                    items,
                    totalCount: items.length,
                    page: 1,
                    pageSize: 100,
                }),
            );
            return;
        }

        if (
            request.url?.split("?")[0] === `/agent/${publishedAgent.id}` &&
            request.method === "POST"
        ) {
            const chunks: Buffer[] = [];
            request.on("data", (chunk: Buffer) => chunks.push(chunk));
            request.on("end", () => {
                const body = JSON.parse(Buffer.concat(chunks).toString()) as {
                    threadId: string;
                    runId: string;
                    messages: Array<{ role: string; content: string }>;
                };
                const userContent = body.messages.at(-1)?.content ?? "";
                const conversationId = request.headers[
                    "x-inkwell-conversation-id"
                ] as string | undefined;
                const assistantMessageId = `${body.runId}:assistant`;
                let textMessageStarted = false;
                const writeEvent = (event: Record<string, unknown>): void => {
                    response.write(`data: ${JSON.stringify(event)}\n\n`);
                };
                const startRun = (): void => {
                    response.setHeader("Content-Type", "text/event-stream");
                    writeEvent({
                        type: "RUN_STARTED",
                        threadId: body.threadId,
                        runId: body.runId,
                    });
                };
                const writeDelta = (delta: string): void => {
                    if (!textMessageStarted) {
                        textMessageStarted = true;
                        writeEvent({
                            type: "TEXT_MESSAGE_START",
                            messageId: assistantMessageId,
                            role: "assistant",
                        });
                    }
                    writeEvent({
                        type: "TEXT_MESSAGE_CONTENT",
                        messageId: assistantMessageId,
                        delta,
                    });
                };
                const finishRun = (): void => {
                    if (textMessageStarted) {
                        writeEvent({
                            type: "TEXT_MESSAGE_END",
                            messageId: assistantMessageId,
                        });
                    }
                    response.end(
                        `data: ${JSON.stringify({ type: "RUN_FINISHED", threadId: body.threadId, runId: body.runId })}\n\n`,
                    );
                };
                const persist = (assistantContent: string): void => {
                    if (!conversationId) return;
                    persistedMessages.set(conversationId, [
                        {
                            id: `${conversationId}-user`,
                            message: {
                                Role: "user",
                                Contents: [
                                    { $type: "text", Text: userContent },
                                ],
                            },
                            sequenceNumber: 1,
                        },
                        {
                            id: `${conversationId}-function-calls`,
                            message: {
                                Role: "assistant",
                                Contents:
                                    userContent === "验证 Skill 调用"
                                        ? [
                                              {
                                                  $type: "functionCall",
                                                  Name: "load_skill",
                                                  Arguments: {
                                                      skillName: "code-review",
                                                  },
                                                  CallId: "call-load-skill",
                                              },
                                              {
                                                  $type: "functionCall",
                                                  Name: "read_skill_resource",
                                                  Arguments: {
                                                      skillName: "code-review",
                                                      resourceName:
                                                          "references/rule.md",
                                                  },
                                                  CallId: "call-read-skill-resource",
                                              },
                                              {
                                                  $type: "functionCall",
                                                  Name: "get_current_datetime",
                                                  Arguments: {
                                                      timeZoneId:
                                                          "Asia/Shanghai",
                                                  },
                                                  CallId: "call-current-time",
                                              },
                                          ]
                                        : [],
                            },
                            sequenceNumber: 2,
                        },
                        {
                            id: `${conversationId}-assistant`,
                            usage: {
                                inputTokenCount: 10,
                                outputTokenCount: 20,
                                totalTokenCount: 30,
                                cachedInputTokenCount: null,
                                reasoningTokenCount: null,
                            },
                            message: {
                                Role: "assistant",
                                Contents: [
                                    { $type: "text", Text: assistantContent },
                                ],
                            },
                            sequenceNumber: 3,
                        },
                        {
                            id: `${conversationId}-empty-assistant`,
                            message: {
                                Role: "assistant",
                                Contents: [],
                            },
                            sequenceNumber: 4,
                        },
                        {
                            id: `${conversationId}-tool-result`,
                            message: {
                                Role: "tool",
                                Contents: [
                                    {
                                        $type: "functionResult",
                                        CallId: "call-load-skill",
                                    },
                                ],
                            },
                            sequenceNumber: 5,
                        },
                    ]);
                };

                if (userContent === "验证限流错误") {
                    rateLimitedAttempts += 1;
                    if (rateLimitedAttempts === 1) {
                        response.statusCode = 429;
                        response.setHeader("Content-Type", "application/json");
                        response.end(
                            JSON.stringify({
                                detail: "请求过于频繁，请稍后重试。",
                            }),
                        );
                        return;
                    }

                    startRun();
                    writeDelta("重试成功");
                    persist("重试成功");
                    finishRun();
                    return;
                }

                if (userContent === "验证 Skill 调用") {
                    startRun();
                    const toolCalls = [
                        {
                            id: "call-load-skill",
                            name: "load_skill",
                            argumentsJson: '{"skillName":"code-review"}',
                        },
                        {
                            id: "call-read-skill-resource",
                            name: "read_skill_resource",
                            argumentsJson:
                                '{"skillName":"code-review","resourceName":"references/rule.md"}',
                        },
                        {
                            id: "call-current-time",
                            name: "get_current_datetime",
                            argumentsJson: '{"timeZoneId":"Asia/Shanghai"}',
                        },
                    ];
                    for (const toolCall of toolCalls) {
                        writeEvent({
                            type: "TOOL_CALL_START",
                            toolCallId: toolCall.id,
                            toolCallName: toolCall.name,
                            parentMessageId: assistantMessageId,
                        });
                        writeEvent({
                            type: "TOOL_CALL_ARGS",
                            toolCallId: toolCall.id,
                            delta: toolCall.argumentsJson,
                        });
                        writeEvent({
                            type: "TOOL_CALL_END",
                            toolCallId: toolCall.id,
                        });
                        writeEvent({
                            type: "TOOL_CALL_RESULT",
                            messageId: `${toolCall.id}:result`,
                            toolCallId: toolCall.id,
                            content: "ok",
                            role: "tool",
                        });
                    }
                    writeEvent({
                        type: "CUSTOM",
                        name: "inkwell.token_usage",
                        value: {
                            inputTokenCount: 3,
                            outputTokenCount: 5,
                            totalTokenCount: 8,
                            cachedInputTokenCount: null,
                            reasoningTokenCount: null,
                        },
                    });
                    setTimeout(() => {
                        writeEvent({
                            type: "CUSTOM",
                            name: "inkwell.token_usage",
                            value: {
                                inputTokenCount: 7,
                                outputTokenCount: 15,
                                totalTokenCount: 22,
                                cachedInputTokenCount: null,
                                reasoningTokenCount: null,
                            },
                        });
                        writeDelta("已按 Skill 完成评审");
                        persist("已按 Skill 完成评审");
                        finishRun();
                    }, 1_500);
                    return;
                }

                startRun();
                if (userContent === "验证锁屏恢复") {
                    writeDelta("锁屏前");
                    setTimeout(() => {
                        writeDelta("，锁屏期间完成");
                        persist("锁屏前，锁屏期间完成");
                        finishRun();
                    }, 300);
                    return;
                }

                if (userContent === "验证停止") {
                    writeDelta("停止前已收到的部分文本");
                    response.on("close", () => {
                        stoppedConnectionClosed = true;
                    });
                    return;
                }

                response.statusCode = 400;
                response.end();
            });
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
            `--user-data-dir=${testInfo.outputPath(`${browserName}-chat-runs`)}`,
        ],
        env: {
            ...process.env,
            INKWELL_WEBAPI_URL: `http://127.0.0.1:${address.port}`,
        },
    });

    try {
        const page = await application.firstWindow();
        await page.evaluate(() => {
            const api = (
                globalThis as unknown as { inkwell: InkwellDesktopApi }
            ).inkwell;
            api.onChatRunChanged((snapshot) => {
                const snapshots = JSON.parse(
                    sessionStorage.getItem("chat-run-snapshots") ?? "[]",
                ) as unknown[];
                snapshots.push(snapshot);
                sessionStorage.setItem(
                    "chat-run-snapshots",
                    JSON.stringify(snapshots),
                );
            });
        });
        await page.getByPlaceholder("请输入账号").fill("admin");
        await page.getByPlaceholder("请输入密码").fill("correct-password");
        await page.getByRole("button", { name: /登\s*录/ }).click();
        await page
            .locator(".agent-space-card")
            .filter({ hasText: "研发助手" })
            .dispatchEvent("click");

        const sender = page.getByPlaceholder(
            "输入消息，Enter 发送，Shift + Enter 换行",
        );
        await sender.fill("验证 Skill 调用");
        await sender.press("Enter");
        await expect(page.getByText("工具调用", { exact: true })).toBeVisible();
        await expect
            .poll(
                () =>
                    page.evaluate(() => {
                        const snapshots = JSON.parse(
                            sessionStorage.getItem("chat-run-snapshots") ??
                                "[]",
                        ) as Array<{
                            status: string;
                            content: string;
                            skillActivities: Array<{ status: string }>;
                            usage?: {
                                inputTokenCount: number | null;
                                outputTokenCount: number | null;
                                totalTokenCount: number | null;
                                cachedInputTokenCount: number | null;
                                reasoningTokenCount: number | null;
                            };
                        }>;
                        const snapshot = snapshots.at(-1);
                        return snapshot
                            ? {
                                  status: snapshot.status,
                                  content: snapshot.content,
                                  activityStatuses:
                                      snapshot.skillActivities.map(
                                          (activity) => activity.status,
                                      ),
                                  usage: snapshot.usage,
                              }
                            : null;
                    }),
                { timeout: 1_000 },
            )
            .toEqual({
                status: "running",
                content: "",
                activityStatuses: ["success", "success", "success"],
                usage: {
                    inputTokenCount: 3,
                    outputTokenCount: 5,
                    totalTokenCount: 8,
                    cachedInputTokenCount: null,
                    reasoningTokenCount: null,
                },
            });
        await page
            .getByRole("button", { name: "返回 Agent 空间" })
            .dispatchEvent("click");
        await page
            .locator(".agent-space-card")
            .filter({ hasText: "研发助手" })
            .dispatchEvent("click");
        await expect(page.getByText("工具调用", { exact: true })).toBeVisible();
        await expect(
            page.getByText("3 项已完成", { exact: true }),
        ).toBeVisible();
        await expect(
            page.getByText("加载 Skill：code-review", { exact: true }),
        ).toBeVisible();
        await expect(
            page.getByText("读取资源：references/rule.md", { exact: true }),
        ).toBeVisible();
        await expect(
            page.getByText("调用工具：get_current_datetime", { exact: true }),
        ).toBeVisible();
        await expect
            .poll(() =>
                page.evaluate(() => {
                    const snapshots = JSON.parse(
                        sessionStorage.getItem("chat-run-snapshots") ?? "[]",
                    ) as Array<{
                        status: string;
                        content: string;
                        error?: { code: string; reason: string };
                    }>;
                    const snapshot = snapshots.at(-1);
                    return snapshot
                        ? {
                              status: snapshot.status,
                              content: snapshot.content,
                              error: snapshot.error,
                          }
                        : null;
                }),
            )
            .toEqual({
                status: "completed",
                content: "已按 Skill 完成评审",
                error: undefined,
            });
        await expect(
            page.getByText("已按 Skill 完成评审", { exact: true }),
        ).toBeVisible();
        const liveUsage = page.getByText(
            "用量 — 输入 10 · 输出 20 · 总计 30 tokens",
            { exact: true },
        );
        await expect(liveUsage).toBeVisible();
        await expect(liveUsage.locator(".anticon-bar-chart")).toBeVisible();
        await expect(liveUsage).toHaveCSS("font-size", "12px");
        expect(
            await liveUsage.evaluate((element) => ({
                hasFooter: Boolean(element.closest(".chat-message-footer")),
                insideMessageContent: Boolean(
                    element.closest(".chat-message-content"),
                ),
            })),
        ).toEqual({ hasFooter: true, insideMessageContent: false });
        await expect(
            page.locator(".chat-bubble-list .ant-bubble-avatar"),
        ).toHaveCount(0);
        await expect(page.locator(".chat-bubble-list .ant-bubble")).toHaveCount(
            3,
        );
        await expect(page.getByText("等待确认")).toHaveCount(0);
        await expect(page.getByText("脚本执行需要用户确认")).toHaveCount(0);

        await page
            .getByRole("button", { name: "新建会话" })
            .dispatchEvent("click");
        const persistedSkillConversation = page
            .locator(".chat-history-list")
            .getByText("Chat run E2E", { exact: true });
        await expect(persistedSkillConversation).toBeVisible();
        await persistedSkillConversation.dispatchEvent("click");
        await expect(
            page.getByText("加载 Skill：code-review", { exact: true }),
        ).toBeVisible();
        await expect(
            page.getByText("读取资源：references/rule.md", { exact: true }),
        ).toBeVisible();
        await expect(
            page.getByText("调用工具：get_current_datetime", { exact: true }),
        ).toBeVisible();
        await expect(
            page.getByText("用量 — 输入 10 · 输出 20 · 总计 30 tokens", {
                exact: true,
            }),
        ).toBeVisible();
        await expect(
            page.locator(".chat-bubble-list .ant-bubble-avatar"),
        ).toHaveCount(0);
        await expect(page.locator(".chat-bubble-list .ant-bubble")).toHaveCount(
            3,
        );

        await page
            .getByRole("button", { name: "新建会话" })
            .dispatchEvent("click");
        await sender.fill("验证锁屏恢复");
        await sender.press("Enter");
        await expect(page.getByText("锁屏前", { exact: true })).toBeVisible();
        await application.evaluate(({ powerMonitor }) => {
            powerMonitor.emit("lock-screen");
        });
        await expect(
            page.getByRole("heading", { name: "Inkwell 已锁定" }),
        ).toBeVisible();
        const lockedWriteError = await page.evaluate(async () => {
            try {
                const api = (
                    globalThis as unknown as { inkwell: InkwellDesktopApi }
                ).inkwell;
                await api.chat({
                    requestId: "locked-write",
                    agentId: "0198a96d-19e4-7000-8000-000000000301",
                    runMode: "published",
                    conversationId: null,
                    messages: [{ role: "user", content: "锁定后写入" }],
                });
                return null;
            } catch (reason) {
                return reason instanceof Error
                    ? reason.message
                    : String(reason);
            }
        });
        expect(lockedWriteError).toContain("Client is locked.");
        const lockedRunErrors = await page.evaluate(async () => {
            const api = (
                globalThis as unknown as { inkwell: InkwellDesktopApi }
            ).inkwell;
            const snapshots = JSON.parse(
                sessionStorage.getItem("chat-run-snapshots") ?? "[]",
            ) as Array<{ requestId: string }>;
            const requestId = snapshots.at(-1)?.requestId;
            if (!requestId) return [];

            const invoke = async (
                operation: () => Promise<unknown>,
            ): Promise<string | null> => {
                try {
                    await operation();
                    return null;
                } catch (reason) {
                    return reason instanceof Error
                        ? reason.message
                        : String(reason);
                }
            };
            return Promise.all([
                invoke(() => api.getChatRun(requestId)),
                invoke(() => api.stopChat(requestId)),
            ]);
        });
        expect(lockedRunErrors).toHaveLength(2);
        expect(lockedRunErrors[0]).toContain("Client is locked.");
        expect(lockedRunErrors[1]).toContain("Client is locked.");
        await expect
            .poll(() =>
                page.evaluate(() => {
                    const snapshots = JSON.parse(
                        sessionStorage.getItem("chat-run-snapshots") ?? "[]",
                    ) as Array<{ status: string }>;
                    return snapshots.at(-1)?.status;
                }),
            )
            .toBe("completed");
        await page.getByPlaceholder("密码").fill("correct-password");
        await page.keyboard.press("Enter");
        await expect(
            page.getByText("锁屏前，锁屏期间完成", { exact: true }),
        ).toBeVisible();

        await page
            .getByRole("button", { name: "新建会话" })
            .dispatchEvent("click");
        await sender.fill("验证停止");
        await sender.press("Enter");
        await expect(
            page.getByText("停止前已收到的部分文本", { exact: true }),
        ).toBeVisible();
        await page
            .locator(".ant-sender-actions-btn-loading-button")
            .dispatchEvent("click");
        await expect(
            page.locator(".ant-sender-actions-btn-loading-button"),
        ).toHaveCount(0);
        await expect(
            page.getByText("停止前已收到的部分文本", { exact: true }),
        ).toBeVisible();
        await expect(
            page.getByText("已停止生成", { exact: true }),
        ).toBeVisible();
        await expect.poll(() => stoppedConnectionClosed).toBe(true);

        await page
            .getByRole("button", { name: "新建会话" })
            .dispatchEvent("click");
        await sender.fill("验证限流错误");
        await sender.press("Enter");
        await expect(page.getByText("HTTP_429", { exact: true })).toBeVisible();
        await expect(
            page.getByText("请求过于频繁，请稍后重试。", { exact: true }),
        ).toBeVisible();
        const failedRequestId = await page.evaluate(() => {
            const snapshots = JSON.parse(
                sessionStorage.getItem("chat-run-snapshots") ?? "[]",
            ) as Array<{ requestId: string; status: string }>;
            return snapshots.findLast(({ status }) => status === "failed")
                ?.requestId;
        });
        await page
            .getByRole("button", { name: "重试失败消息" })
            .dispatchEvent("click");
        await expect(page.getByText("重试成功", { exact: true })).toBeVisible();
        const completedRequestId = await page.evaluate(() => {
            const snapshots = JSON.parse(
                sessionStorage.getItem("chat-run-snapshots") ?? "[]",
            ) as Array<{
                requestId: string;
                status: string;
                content: string;
            }>;
            return snapshots.findLast(
                ({ status, content }) =>
                    status === "completed" && content === "重试成功",
            )?.requestId;
        });
        expect(failedRequestId).toBeTruthy();
        expect(completedRequestId).toBeTruthy();
        expect(completedRequestId).not.toBe(failedRequestId);
        expect(rateLimitedAttempts).toBe(2);
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
