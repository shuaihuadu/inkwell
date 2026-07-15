import type { AGUIEvent } from "./types";

/** 把一段文本拆成一串 `TEXT_MESSAGE_CONTENT` 事件（按字符分块，默认每块 3 个字符、间隔
 * 28ms），用来模拟真实的流式文本推送；`harnessDemo.ts` 的 Harness/Agent Loop 演示和
 * `useMockChat.ts` 的普通单条回复共用这一个函数，两处的"流式"节奏保持一致。 */
export function streamingTextEvents(
    messageId: string,
    text: string,
    startAt: number,
    step = 3,
    interval = 28,
): Array<{ at: number; event: AGUIEvent }> {
    const events: Array<{ at: number; event: AGUIEvent }> = [];
    for (let i = 0; i < text.length; i += step) {
        events.push({
            at: startAt + (i / step) * interval,
            event: { type: "TEXT_MESSAGE_CONTENT", messageId, delta: text.slice(i, i + step) },
        });
    }
    return events;
}

/** 一段文本按 `streamingTextEvents` 的默认节奏播完所需要的时长（毫秒），用来算
 * `TEXT_MESSAGE_END`/`RUN_FINISHED` 应该排在哪个时刻。 */
export function streamingDuration(text: string, step = 3, interval = 28): number {
    return Math.ceil(text.length / step) * interval;
}
