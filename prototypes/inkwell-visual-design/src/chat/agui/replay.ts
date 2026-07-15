import type { ChatMessage, SetMessages } from "../useMockChat";
import { formatNow } from "../useMockChat";
import { createAGUIEventHandler } from "./reducer";
import type { AGUIEvent } from "./types";

// ─── 静态种子历史的"重放"入口 ──────────────────────────────────────────────
// 各页面里"点开就能看到的完整历史会话"（比如 AgentChatPage 的调研/优化文案两个种子会话）
// 不能凭空手写一份长得像 ChatMessage 的结构——那样这份数据跟真正驱动 Harness/Agent Loop
// 演示的 AG-UI 事件管线就是两套互相不知道对方存在的东西，以后接手的人看到 ChatMessage
// 里那些 harnessSteps/loopSteps/todos 字段，很容易误以为这就是"随便定的展示用数据形状"，
// 而看不出它们其实是 AG-UI 协议事件（ACTIVITY_SNAPSHOT/ACTIVITY_DELTA 等）翻译出来的结果。
// 这里改成：种子会话也用同一份 AG-UI 事件描述"已经发生过什么"，只是不需要动画播放的时序，
// 同步、一次性地喂给跟 `runHarnessDemo` 完全一样的 `createAGUIEventHandler`，直接产出最终
// 定稿的 `ChatMessage[]`——种子数据和实时演示是同一套"事件 → 界面状态"翻译逻辑，不是两份
// 各自维护、容易跑偏的平行实现。

/** 种子会话的一"轮"：用户自己发的话（本来就不是协议事件的一部分——客户端本地已经知道
 * 自己发了什么，不需要等后端事件回放）或者一串助手侧的 AG-UI 事件（真正喂给 reducer）。 */
export type SeedStep =
    | { kind: "user"; content: string; time?: string }
    | { kind: "events"; events: AGUIEvent[] };

/** 同步重放一段种子会话的 AG-UI 事件序列，返回最终定稿的 `ChatMessage[]`。
 *
 * @param steps 用户发言与助手侧 AG-UI 事件交替组成的时间线（不需要 `at` 时刻，同步顺序应用）
 * @returns 重放完成后的消息列表
 */
export function replaySeed(steps: SeedStep[]): ChatMessage[] {
    let messages: ChatMessage[] = [];
    const fakeSetMessages: SetMessages = (updater) => {
        messages = updater(messages);
    };
    const handleEvent = createAGUIEventHandler(fakeSetMessages, () => {});

    for (const step of steps) {
        if (step.kind === "user") {
            messages = [
                ...messages,
                {
                    id: `u-${messages.length}-${Date.now()}`,
                    role: "user",
                    content: step.content,
                    time: step.time ?? formatNow(),
                },
            ];
        } else {
            for (const event of step.events) {
                handleEvent(event);
            }
        }
    }

    return messages;
}

/** 生成一次完整的助手文本回复事件三元组（Start → Content → End），种子数据不需要真的
 * 分块流式，一次性把全量文本作为一个 delta 发出即可。 */
export function assistantTextEvents(messageId: string, text: string): AGUIEvent[] {
    return [
        { type: "TEXT_MESSAGE_START", messageId, role: "assistant" },
        { type: "TEXT_MESSAGE_CONTENT", messageId, delta: text },
        { type: "TEXT_MESSAGE_END", messageId },
    ];
}
