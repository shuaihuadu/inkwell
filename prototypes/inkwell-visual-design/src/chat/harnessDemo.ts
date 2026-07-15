import type { SetMessages } from "./useMockChat";
import { createAGUIEventHandler } from "./agui/reducer";
import { playAGUITimeline } from "./agui/sse";
import { streamingDuration, streamingTextEvents } from "./agui/textStream";
import type { AGUIEvent } from "./agui/types";

// ─── Harness / Agent Loop 场景模拟（对照 microsoft/agent-framework 的
// dotnet/samples/02-agents/Harness：plan/execute 双模式 + TodoCompletionLoopEvaluator 驱动的
// 自主循环，循环里会调用工具、维护并逐项勾掉任务清单，直到全部完成才生成最终回复；参考了真实
// 控制台 Observer 会展示的内容种类——ReasoningDisplayObserver（思考过程）、
// ToolCallDisplayObserver（工具调用）、TodoProvider（任务清单）、UsageDisplayObserver（Tokens
// 用量统计）；Agent Loop 对应 `dotnet/src/Microsoft.Agents.AI/Harness/Loop/LoopAgent` +
// `LoopEvaluator`）───────────────────────────────────────────────────────────────
// 本文件不再直接用一串 setTimeout 手写"追加/更新 ChatMessage"，而是先产出一份 AG-UI 协议
// 事件时间线（`AGUIEvent` + 触发时刻），交给 `agui/sse.ts` 的 `playAGUITimeline` 按时间播放
// ——每个事件真的会先编码成 SSE 文本再解码回来，然后喂给 `agui/reducer.ts` 翻译成
// `ChatMessage` 的变化。这样"用什么样的事件形状驱动界面"就跟真实 Inkwell 后端将来通过
// AG-UI 协议推给前端的事件形状对齐（见 AGENTS.md §2.1 ADR-012），具体的
// `activityType`/`content` schema 仍然只是本原型演示用，真正的 H3 详细设计定案后可能会调整。
// 两个聊天页面（AgentChatPage / CopilotPanel）共用同一份编排。

const TRIGGER_KEYWORDS = ["研究", "调研", "分析一下", "深度"];
const LOOP_TRIGGER_KEYWORDS = ["优化", "改进", "迭代", "精简", "润色"];

/** 判断一句用户输入是否应该触发 Harness 演示流程，而不是走普通的单条 mock 回复。 */
export function isHarnessTrigger(text: string): boolean {
    return TRIGGER_KEYWORDS.some((keyword) => text.includes(keyword));
}

/** 判断一句用户输入是否应该触发 Agent Loop 演示流程（对应 `LoopAgent` + `LoopEvaluator`
 * 驱动的"迭代优化直到评估器满意"场景，跟 Harness 的 plan→execute 是两种不同的自主循环）。 */
export function isAgentLoopTrigger(text: string): boolean {
    return LOOP_TRIGGER_KEYWORDS.some((keyword) => text.includes(keyword));
}

/**
 * 播放一次 Harness 风格的 plan → execute 自主循环演示，用 AG-UI 事件时间线还原真实控制台
 * 会展示的内容种类：
 * 1. `REASONING_*`（对应 `ReasoningDisplayObserver`）：深度思考中 → 完成思考；
 * 2. `ACTIVITY_SNAPSHOT`/`ACTIVITY_DELTA`（`activityType:"harness"`）：制定计划 loading →
 *    success；
 * 3. `TOOL_CALL_*`（对应 `ToolCallDisplayObserver`）+ 配套的 `ACTIVITY_DELTA`：依次追加两个
 *    独立的工具调用步骤——"调用工具：网页搜索"与"调用工具：知识库检索"，各自 loading →
 *    success 并带 detail；
 * 4. `ACTIVITY_SNAPSHOT`/`ACTIVITY_DELTA`（`activityType:"todos"`，对应
 *    `TodoProvider`/`TodoCompletionLoopEvaluator` 维护的任务清单）：逐项从
 *    pending → in-progress → done；
 * 5. harness activity 追加"整理结果"节点 loading → success；
 * 6. `TEXT_MESSAGE_START/CONTENT/END`（最终回复，逐块流式）+ `CUSTOM`（对应
 *    `UsageDisplayObserver` 的 Tokens 用量统计，挂在这条回复消息自己身上）。
 */
export function runHarnessDemo(
    userText: string,
    setMessages: SetMessages,
    setReplying: (v: boolean) => void,
): void {
    const runId = `run-${Date.now()}`;
    const reasoningId = `reasoning-${Date.now()}`;
    const harnessId = `harness-${Date.now() + 1}`;
    const todosId = `todos-${Date.now() + 2}`;
    const tool1Id = `tool-${Date.now() + 3}`;
    const tool2Id = `tool-${Date.now() + 4}`;
    const finalMessageId = `a-${Date.now() + 5}`;

    const finalReply = `已完成关于"${userText}"的研究：任务清单里的 3 项都已勾选完成，结论已整理成结构化要点，可以在需要时导出为完整报告。`;

    const timeline: Array<{ at: number; event: AGUIEvent }> = [
        { at: 0, event: { type: "RUN_STARTED", threadId: runId, runId } },
        { at: 0, event: { type: "REASONING_START", messageId: reasoningId } },
        { at: 0, event: { type: "REASONING_MESSAGE_START", messageId: reasoningId, role: "reasoning" } },

        // 1. 思考完成 → harness activity 出现，"制定计划"节点 loading
        {
            at: 600,
            event: {
                type: "REASONING_MESSAGE_CONTENT",
                messageId: reasoningId,
                delta: `用户想了解"${userText}"，需要先明确范围和目标读者，再决定用哪些工具检索外部资料、哪些结论需要交叉核对。`,
            },
        },
        { at: 600, event: { type: "REASONING_MESSAGE_END", messageId: reasoningId } },
        { at: 600, event: { type: "REASONING_END", messageId: reasoningId } },
        {
            at: 600,
            event: {
                type: "ACTIVITY_SNAPSHOT",
                messageId: harnessId,
                activityType: "harness",
                content: { steps: [{ key: "plan", title: "制定研究计划", status: "loading" }] },
            },
        },

        // 2. 计划完成 → 第一个工具调用 loading
        {
            at: 1300,
            event: {
                type: "ACTIVITY_DELTA",
                messageId: harnessId,
                activityType: "harness",
                patch: [
                    { op: "replace", path: "/steps/0", value: { key: "plan", title: "制定研究计划", status: "success" } },
                ],
            },
        },
        { at: 1300, event: { type: "TOOL_CALL_START", toolCallId: tool1Id, toolCallName: "web_search", parentMessageId: harnessId } },
        {
            at: 1300,
            event: {
                type: "ACTIVITY_DELTA",
                messageId: harnessId,
                activityType: "harness",
                patch: [{ op: "add", path: "/steps/-", value: { key: "tool-1", title: "调用工具：网页搜索", status: "loading" } }],
            },
        },

        // 3. 第一个工具调用完成 → 第二个工具调用 loading
        { at: 1900, event: { type: "TOOL_CALL_ARGS", toolCallId: tool1Id, delta: "查询关键词与来源筛选" } },
        { at: 1900, event: { type: "TOOL_CALL_END", toolCallId: tool1Id } },
        {
            at: 1900,
            event: {
                type: "TOOL_CALL_RESULT",
                messageId: harnessId,
                toolCallId: tool1Id,
                content: `检索到多篇与"${userText}"相关的公开资料，已挑选出可信度较高的几篇作为参考来源。`,
            },
        },
        {
            at: 1900,
            event: {
                type: "ACTIVITY_DELTA",
                messageId: harnessId,
                activityType: "harness",
                patch: [
                    {
                        op: "replace",
                        path: "/steps/1",
                        value: {
                            key: "tool-1",
                            title: "调用工具：网页搜索",
                            description: "查询关键词与来源筛选",
                            detail: `检索到多篇与"${userText}"相关的公开资料，已挑选出可信度较高的几篇作为参考来源。`,
                            status: "success",
                        },
                    },
                ],
            },
        },
        { at: 1900, event: { type: "TOOL_CALL_START", toolCallId: tool2Id, toolCallName: "knowledge_base_search", parentMessageId: harnessId } },
        {
            at: 1900,
            event: {
                type: "ACTIVITY_DELTA",
                messageId: harnessId,
                activityType: "harness",
                patch: [{ op: "add", path: "/steps/-", value: { key: "tool-2", title: "调用工具：知识库检索", status: "loading" } }],
            },
        },

        // 4. 第二个工具调用完成 → 出现任务清单
        { at: 2500, event: { type: "TOOL_CALL_ARGS", toolCallId: tool2Id, delta: "匹配内部历史资料" } },
        { at: 2500, event: { type: "TOOL_CALL_END", toolCallId: tool2Id } },
        {
            at: 2500,
            event: {
                type: "TOOL_CALL_RESULT",
                messageId: harnessId,
                toolCallId: tool2Id,
                content: "在知识库中找到相关的历史研究记录，补充到本轮参考资料里，避免重复调研。",
            },
        },
        {
            at: 2500,
            event: {
                type: "ACTIVITY_DELTA",
                messageId: harnessId,
                activityType: "harness",
                patch: [
                    {
                        op: "replace",
                        path: "/steps/2",
                        value: {
                            key: "tool-2",
                            title: "调用工具：知识库检索",
                            description: "匹配内部历史资料",
                            detail: "在知识库中找到相关的历史研究记录，补充到本轮参考资料里，避免重复调研。",
                            status: "success",
                        },
                    },
                ],
            },
        },
        {
            at: 2500,
            event: {
                type: "ACTIVITY_SNAPSHOT",
                messageId: todosId,
                activityType: "todos",
                content: {
                    todos: [
                        { key: "t1", label: "梳理资料来源与关键结论", status: "in-progress" },
                        { key: "t2", label: "交叉核对存在分歧的信息点", status: "pending" },
                        { key: "t3", label: "整理成结构化报告草稿", status: "pending" },
                    ],
                },
            },
        },

        // 5. 任务清单逐项完成
        {
            at: 3100,
            event: {
                type: "ACTIVITY_DELTA",
                messageId: todosId,
                activityType: "todos",
                patch: [
                    { op: "replace", path: "/todos/0", value: { key: "t1", label: "梳理资料来源与关键结论", status: "done" } },
                    { op: "replace", path: "/todos/1", value: { key: "t2", label: "交叉核对存在分歧的信息点", status: "in-progress" } },
                ],
            },
        },
        {
            at: 3700,
            event: {
                type: "ACTIVITY_DELTA",
                messageId: todosId,
                activityType: "todos",
                patch: [
                    { op: "replace", path: "/todos/1", value: { key: "t2", label: "交叉核对存在分歧的信息点", status: "done" } },
                    { op: "replace", path: "/todos/2", value: { key: "t3", label: "整理成结构化报告草稿", status: "in-progress" } },
                ],
            },
        },

        // 6. 任务清单全部完成 → harness "整理结果" loading
        {
            at: 4300,
            event: {
                type: "ACTIVITY_DELTA",
                messageId: todosId,
                activityType: "todos",
                patch: [{ op: "replace", path: "/todos/2", value: { key: "t3", label: "整理成结构化报告草稿", status: "done" } }],
            },
        },
        {
            at: 4300,
            event: {
                type: "ACTIVITY_DELTA",
                messageId: harnessId,
                activityType: "harness",
                patch: [{ op: "add", path: "/steps/-", value: { key: "wrap-up", title: "整理结果", status: "loading" } }],
            },
        },

        // 7. 整理结果完成 → 最终流式回复 + 用量统计
        {
            at: 4900,
            event: {
                type: "ACTIVITY_DELTA",
                messageId: harnessId,
                activityType: "harness",
                patch: [{ op: "replace", path: "/steps/3", value: { key: "wrap-up", title: "整理结果", status: "success" } }],
            },
        },
        { at: 4900, event: { type: "TEXT_MESSAGE_START", messageId: finalMessageId, role: "assistant" } },
        {
            at: 4900,
            event: { type: "CUSTOM", name: "usage", value: "用量 — 输入 1.2k · 输出 340 · 总计 1.5k tokens" },
        },
        ...streamingTextEvents(finalMessageId, finalReply, 4900),
    ];
    const streamEndAt = 4900 + streamingDuration(finalReply);
    timeline.push({ at: streamEndAt, event: { type: "TEXT_MESSAGE_END", messageId: finalMessageId } });
    timeline.push({ at: streamEndAt, event: { type: "RUN_FINISHED", threadId: runId, runId } });

    playAGUITimeline(timeline, createAGUIEventHandler(setMessages, setReplying));
}

/**
 * 播放一次 Agent Loop 风格的迭代优化演示，对应 `LoopAgent` + `LoopEvaluator`：每一轮产出一个
 * 结果后，评估器判断 `ShouldReinvoke`（继续/停止），继续时把 `Feedback` 带进下一轮输入，直到
 * 评估器满意为止（受 `LoopAgentOptions.MaxIterations` 兜底，本演示固定两轮）。跟 Harness 的
 * plan→execute 是两种不同的自主循环，`activityType` 用 `"loop"` 区分，但复用同一个
 * `HarnessThoughtChain` 渲染组件：
 * 1. `ACTIVITY_SNAPSHOT`/`ACTIVITY_DELTA`：第 1 轮生成初稿 loading → success；
 * 2. 评估反馈：判定需要继续优化，带上具体原因；
 * 3. 第 2 轮：根据反馈修改，loading → success；
 * 4. 评估反馈：判定已达标，结束循环；
 * 5. `TEXT_MESSAGE_START/CONTENT/END`：最终流式回复（内容即第 2 轮的定稿）。
 */
export function runAgentLoopDemo(
    userText: string,
    setMessages: SetMessages,
    setReplying: (v: boolean) => void,
): void {
    const runId = `run-${Date.now()}`;
    const loopId = `loop-${Date.now()}`;
    const finalMessageId = `a-${Date.now() + 1}`;

    const draft1 = "我们的产品性价比高、功能齐全，用户反馈普遍不错。";
    const draft2 =
        "相比同价位竞品，我们在核心功能响应速度上快 30%，且提供 7×24 小时人工支持；已有 500+ 团队在用，续费率超过 90%。";
    const finalReply = `优化后的文案：${draft2}`;

    const timeline: Array<{ at: number; event: AGUIEvent }> = [
        { at: 0, event: { type: "RUN_STARTED", threadId: runId, runId } },
        {
            at: 0,
            event: {
                type: "ACTIVITY_SNAPSHOT",
                messageId: loopId,
                activityType: "loop",
                content: { steps: [{ key: "round-1", title: "第 1 轮：生成初稿", status: "loading" }] },
            },
        },

        // 1. 第 1 轮完成 → 评估反馈：需要继续优化
        {
            at: 900,
            event: {
                type: "ACTIVITY_DELTA",
                messageId: loopId,
                activityType: "loop",
                patch: [
                    {
                        op: "replace",
                        path: "/steps/0",
                        value: {
                            key: "round-1",
                            title: "第 1 轮：生成初稿",
                            description: `基于"${userText}"生成初稿`,
                            detail: draft1,
                            status: "success",
                        },
                    },
                    {
                        op: "add",
                        path: "/steps/-",
                        value: {
                            key: "eval-1",
                            title: "评估反馈：需要继续优化",
                            description: "内容偏空泛，缺少具体数据和差异化卖点",
                            status: "error",
                        },
                    },
                ],
            },
        },

        // 2. 第 2 轮开始
        {
            at: 1600,
            event: {
                type: "ACTIVITY_DELTA",
                messageId: loopId,
                activityType: "loop",
                patch: [{ op: "add", path: "/steps/-", value: { key: "round-2", title: "第 2 轮：根据反馈修改", status: "loading" } }],
            },
        },

        // 3. 第 2 轮完成 → 评估反馈：已达标，结束循环
        {
            at: 2500,
            event: {
                type: "ACTIVITY_DELTA",
                messageId: loopId,
                activityType: "loop",
                patch: [
                    {
                        op: "replace",
                        path: "/steps/2",
                        value: { key: "round-2", title: "第 2 轮：根据反馈修改", detail: draft2, status: "success" },
                    },
                    {
                        op: "add",
                        path: "/steps/-",
                        value: {
                            key: "eval-2",
                            title: "评估反馈：已达标，结束循环",
                            description: "已补充具体数据与差异化卖点，满足要求",
                            status: "success",
                        },
                    },
                ],
            },
        },

        // 4. 最终流式回复
        { at: 3200, event: { type: "TEXT_MESSAGE_START", messageId: finalMessageId, role: "assistant" } },
        ...streamingTextEvents(finalMessageId, finalReply, 3200),
    ];
    const streamEndAt = 3200 + streamingDuration(finalReply);
    timeline.push({ at: streamEndAt, event: { type: "TEXT_MESSAGE_END", messageId: finalMessageId } });
    timeline.push({ at: streamEndAt, event: { type: "RUN_FINISHED", threadId: runId, runId } });

    playAGUITimeline(timeline, createAGUIEventHandler(setMessages, setReplying));
}

