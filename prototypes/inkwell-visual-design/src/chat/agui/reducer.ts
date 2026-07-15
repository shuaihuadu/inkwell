import type { ChatMessage, ChatRole, SetMessages } from "../useMockChat";
import { formatNow } from "../useMockChat";
import type { HarnessStep } from "../HarnessThoughtChain";
import type { TodoItem } from "../TodosPanel";
import { applyJsonPatch } from "./jsonPatch";
import type { AGUIEvent } from "./types";

// ─── AG-UI 事件 → ChatMessage 状态变化的 reducer ───────────────────────────────
// 本原型所有场景（普通单条回复、Harness、Agent Loop，以及各页面的静态种子历史）都不再
// 直接手写"追加/更新 ChatMessage"，而是先产出一份 AG-UI 事件序列（实时演示见
// harnessDemo.ts/useMockChat.ts，经 agui/sse.ts 编解码一轮后逐个事件喂进来；静态种子历史见
// agui/replay.ts，同步、不经过编解码地直接喂进来）——这层只做"协议事件 → 界面状态"的翻译，
// 不关心事件是怎么产生/怎么送进来的，方便以后真的接后端时替换成真实 SSE 连接。

/** Activity 的 `activityType` 判别字符串 → 本原型 ChatRole 的映射；harness/loop 复用
 * 同一个 ThoughtChain 渲染组件，但在 AG-UI 事件层仍然是两个不同的 activityType（跟真实
 * `microsoft/agent-framework` 里 Harness 的 plan→execute 循环、Agent Loop 的迭代循环是
 * 两种不同的自主循环这一事实保持一致）。 */
const ACTIVITY_TYPE_TO_ROLE: Record<string, ChatRole> = {
    harness: "harness",
    loop: "loop",
    todos: "todos",
};

function activityContentToFields(role: ChatRole, content: Record<string, unknown>): Partial<ChatMessage> {
    if (role === "todos") {
        return { todos: (content.todos as TodoItem[] | undefined) ?? [] };
    }
    if (role === "loop") {
        return { loopSteps: (content.steps as HarnessStep[] | undefined) ?? [] };
    }
    return { harnessSteps: (content.steps as HarnessStep[] | undefined) ?? [] };
}

/**
 * 创建一个消费单个 AG-UI 事件、把它落地成 `ChatMessage` 列表变化的处理函数。
 *
 * @param setMessages 消息列表的更新函数（来自 `useMockChat`）
 * @param setReplying 是否仍在等待本轮回复的状态更新函数（来自 `useMockChat`）
 * @returns 处理单个 `AGUIEvent` 的函数，交给 `agui/sse.ts` 的 `playAGUITimeline` 调用
 */
export function createAGUIEventHandler(
    setMessages: SetMessages,
    setReplying: (v: boolean) => void,
): (event: AGUIEvent) => void {
    // messageId -> 当前 Activity 的结构化 content，用来在收到 ACTIVITY_DELTA 时应用 JSON
    // Patch；不从 messages 状态里反查（`setMessages` 的更新是异步的，读不到"刚刚"的最新值），
    // 在这个闭包里维护一份同步镜像，跟旧版 harnessDemo.ts 用局部变量累积 steps 是同一个道理。
    const activityContentById = new Map<string, Record<string, unknown>>();

    return (event: AGUIEvent) => {
        switch (event.type) {
            case "RUN_STARTED":
                setReplying(true);
                break;

            case "RUN_FINISHED":
                setReplying(false);
                break;

            case "REASONING_START":
                // 阶段起点，纯粹的 pass-through 事件，不建消息（对齐协议里的语义）。
                break;

            case "REASONING_MESSAGE_START":
                setMessages((prev) => [
                    ...prev,
                    {
                        id: event.messageId,
                        role: "reasoning",
                        content: "",
                        time: formatNow(),
                        reasoningLoading: true,
                    },
                ]);
                break;

            case "REASONING_MESSAGE_CONTENT":
                setMessages((prev) =>
                    prev.map((m) =>
                        m.id === event.messageId ? { ...m, content: m.content + event.delta } : m,
                    ),
                );
                break;

            case "REASONING_MESSAGE_END":
                setMessages((prev) =>
                    prev.map((m) => (m.id === event.messageId ? { ...m, reasoningLoading: false } : m)),
                );
                break;

            case "REASONING_END":
                // 阶段终点，纯粹的 pass-through 事件。
                break;

            case "ACTIVITY_SNAPSHOT": {
                activityContentById.set(event.messageId, event.content);
                const role = ACTIVITY_TYPE_TO_ROLE[event.activityType] ?? "harness";
                setMessages((prev) => [
                    ...prev,
                    {
                        id: event.messageId,
                        role,
                        content: "",
                        time: formatNow(),
                        ...activityContentToFields(role, event.content),
                    },
                ]);
                break;
            }

            case "ACTIVITY_DELTA": {
                const current = activityContentById.get(event.messageId) ?? {};
                const next = applyJsonPatch(current, event.patch);
                activityContentById.set(event.messageId, next);
                const role = ACTIVITY_TYPE_TO_ROLE[event.activityType] ?? "harness";
                setMessages((prev) =>
                    prev.map((m) =>
                        m.id === event.messageId ? { ...m, ...activityContentToFields(role, next) } : m,
                    ),
                );
                break;
            }

            case "TEXT_MESSAGE_START":
                setMessages((prev) => [
                    ...prev,
                    { id: event.messageId, role: "ai", content: "", time: formatNow() },
                ]);
                break;

            case "TEXT_MESSAGE_CONTENT":
                setMessages((prev) =>
                    prev.map((m) =>
                        m.id === event.messageId ? { ...m, content: m.content + event.delta } : m,
                    ),
                );
                break;

            case "TEXT_MESSAGE_END":
                // 内容已经在 TEXT_MESSAGE_CONTENT 里逐块拼好了，这里不需要再做什么。
                break;

            case "CUSTOM":
                // 目前只用来承载 Token 用量提示（对应真实 Harness 控制台的
                // UsageDisplayObserver）；协议本身 CustomEvent 没有 messageId 字段，
                // 挂到"当前最新一条 ai 回复"身上（这版每次演示只有一条最终回复，足够用）。
                if (event.name === "usage") {
                    setMessages((prev) => {
                        const lastAiMessage = [...prev].reverse().find((m) => m.role === "ai");
                        if (!lastAiMessage) return prev;
                        return prev.map((m) =>
                            m.id === lastAiMessage.id ? { ...m, usage: String(event.value) } : m,
                        );
                    });
                }
                break;

            case "TOOL_CALL_START":
            case "TOOL_CALL_ARGS":
            case "TOOL_CALL_END":
            case "TOOL_CALL_RESULT":
                // 工具调用事件本身只用来标识"这是一次真实的工具调用"（对齐真实协议里工具
                // 执行的独立事件通道）；本演示里工具调用的可视化结果（loading→success、
                // description/detail）统一由配套发出的 ACTIVITY_DELTA 事件驱动，这里不用
                // 重复处理，仅作为协议真实性存档（真实后端很可能既发工具调用事件用于执行
                // 侧的记录/遥测，也发 Activity 事件用于这个 Harness UI 组件的渲染）。
                break;

            default:
                break;
        }
    };
}
