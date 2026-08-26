import { _electron as electron, expect, test } from "@playwright/test";
import electronPath from "electron";
import { createServer } from "node:http";
import type { InkwellDesktopApi } from "../src/shared/network/contracts.js";
import {
    applicationEntry,
    myAgentsResponse,
    publishedAgent,
} from "./fixtures/test-data.js";

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
        if (request.url === "/healthz") {
            response.setHeader("Content-Type", "application/json");
            response.end(JSON.stringify({ status: "healthy" }));
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
        await expect(page.locator(".ant-bubble-loading")).toBeVisible();
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
