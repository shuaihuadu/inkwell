import type { AGUIEvent } from "./types";

// ─── 最小 SSE（Server-Sent Events）编解码 ──────────────────────────────────────
// 真实 AG-UI 后端通过 SSE 把事件推给前端，每帧形如 `data: <json>\n\n`。本原型没有真实
// 网络连接，但仍然走"编码成文本 → 喂进解码器 → 解析回事件对象"这一整套流程（不是直接把
// JS 对象引用传给 reducer），这样至少做到了"字节级"的编解码往返，而不是假装模拟。

/** 把一个 AG-UI 事件编码成一帧 SSE 文本。 */
export function encodeSSEFrame(event: AGUIEvent): string {
    return `data: ${JSON.stringify(event)}\n\n`;
}

/** 增量式 SSE 解码器：维护一个文本缓冲区，每次喂入新到达的文本片段，只要缓冲区里能
 * 凑出以 `\n\n` 结尾的完整一帧就解析并返回；不完整的半帧留在缓冲区里等下一次喂入——
 * 模拟真实网络分片到达、一次 TCP 包未必正好装下一帧的情形。 */
export class SSEStreamDecoder {
    private buffer = "";

    /** 喂入一段新到达的原始文本，返回这次新解析出的完整事件（可能是空数组，也可能不止一个）。 */
    push(chunk: string): AGUIEvent[] {
        this.buffer += chunk;
        const events: AGUIEvent[] = [];
        let boundary = this.buffer.indexOf("\n\n");
        while (boundary !== -1) {
            const frame = this.buffer.slice(0, boundary);
            this.buffer = this.buffer.slice(boundary + 2);
            const dataLine = frame.split("\n").find((line) => line.startsWith("data: "));
            if (dataLine) {
                events.push(JSON.parse(dataLine.slice("data: ".length)) as AGUIEvent);
            }
            boundary = this.buffer.indexOf("\n\n");
        }
        return events;
    }
}

/** 播放一份带绝对时间戳（相对整个演示开始的毫秒数）的 AG-UI 事件时间线：每个事件到点
 * 后先编码成 SSE 文本、喂给同一个解码器解析回事件对象，再交给 `onEvent` 处理——不是
 * 直接把 timeline 里的事件对象原样传给 `onEvent`，中间真的走了一次编码/解码往返。 */
export function playAGUITimeline(
    timeline: Array<{ at: number; event: AGUIEvent }>,
    onEvent: (event: AGUIEvent) => void,
): void {
    const decoder = new SSEStreamDecoder();
    for (const { at, event } of timeline) {
        window.setTimeout(() => {
            for (const decoded of decoder.push(encodeSSEFrame(event))) {
                onEvent(decoded);
            }
        }, at);
    }
}
