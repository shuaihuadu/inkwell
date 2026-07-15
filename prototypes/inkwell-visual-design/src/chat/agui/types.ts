// ─── AG-UI 协议事件类型（对齐 https://docs.ag-ui.com/concepts/events 的真实字段形状） ──
// 只挑了本原型两个演示场景（Harness / Agent Loop）用得到的事件子集，字段名、类型判别
// 字符串（如 "RUN_STARTED"）都跟官方 EventType 枚举保持一致，方便以后对照真实后端接入。
// 没有实现的事件类型（STATE_SNAPSHOT/STATE_DELTA、MESSAGES_SNAPSHOT、RAW、STEP_STARTED/
// FINISHED 等）本原型没有对应场景用到，先不引入。

/** 生命周期事件：一次 Agent 运行的起止标记。 */
export interface RunStartedEvent {
    type: "RUN_STARTED";
    threadId: string;
    runId: string;
}

export interface RunFinishedEvent {
    type: "RUN_FINISHED";
    threadId: string;
    runId: string;
}

/** 文本消息事件：流式文本回复的 Start → Content → End 三段式。 */
export interface TextMessageStartEvent {
    type: "TEXT_MESSAGE_START";
    messageId: string;
    role: "assistant";
}

export interface TextMessageContentEvent {
    type: "TEXT_MESSAGE_CONTENT";
    messageId: string;
    delta: string;
}

export interface TextMessageEndEvent {
    type: "TEXT_MESSAGE_END";
    messageId: string;
}

/** 工具调用事件：Start → Args → End → Result。本原型只用它标识"这是一次真实的工具
 * 调用"（对齐真实协议的工具执行事件通道），可视化效果统一由配套发出的 ACTIVITY_DELTA
 * 驱动，见 reducer.ts 里的处理说明。 */
export interface ToolCallStartEvent {
    type: "TOOL_CALL_START";
    toolCallId: string;
    toolCallName: string;
    parentMessageId?: string;
}

export interface ToolCallArgsEvent {
    type: "TOOL_CALL_ARGS";
    toolCallId: string;
    delta: string;
}

export interface ToolCallEndEvent {
    type: "TOOL_CALL_END";
    toolCallId: string;
}

export interface ToolCallResultEvent {
    type: "TOOL_CALL_RESULT";
    messageId: string;
    toolCallId: string;
    content: string;
    role?: "tool";
}

/** Activity 事件：AG-UI 协议里专门用来承载"消息之间的结构化进度信息"的事件类型
 * （`activityType` 判别字段 + 任意结构化 `content`），Harness 的步骤链、Agent Loop 的
 * 迭代轮次、Todos 任务清单三种场景都用它承载——这三者在概念上都是"结构化进度"，不是
 * 常规的文本消息或工具调用。 */
export interface ActivitySnapshotEvent {
    type: "ACTIVITY_SNAPSHOT";
    messageId: string;
    activityType: string;
    content: Record<string, unknown>;
}

export interface ActivityDeltaEvent {
    type: "ACTIVITY_DELTA";
    messageId: string;
    activityType: string;
    /** RFC 6902 JSON Patch 操作数组（本原型只实现了 add/replace/remove 这三种最常用的，
     * 见 jsonPatch.ts 里的说明）。 */
    patch: JsonPatchOperation[];
}

export interface JsonPatchOperation {
    op: "add" | "replace" | "remove";
    path: string;
    value?: unknown;
}

/** 推理事件：Start（阶段起点，不建消息）→ MessageStart（真正建一条 reasoning 消息）→
 * MessageContent（流式内容）→ MessageEnd（内容结束）→ End（阶段终点）。 */
export interface ReasoningStartEvent {
    type: "REASONING_START";
    messageId: string;
}

export interface ReasoningMessageStartEvent {
    type: "REASONING_MESSAGE_START";
    messageId: string;
    role: "reasoning";
}

export interface ReasoningMessageContentEvent {
    type: "REASONING_MESSAGE_CONTENT";
    messageId: string;
    delta: string;
}

export interface ReasoningMessageEndEvent {
    type: "REASONING_MESSAGE_END";
    messageId: string;
}

export interface ReasoningEndEvent {
    type: "REASONING_END";
    messageId: string;
}

/** 特殊事件：协议没有专门字段覆盖的场景走这个扩展点（本原型用来承载 Token 用量提示，
 * 对应真实 Harness 控制台的 UsageDisplayObserver）。 */
export interface CustomEvent {
    type: "CUSTOM";
    name: string;
    value: unknown;
}

export type AGUIEvent =
    | RunStartedEvent
    | RunFinishedEvent
    | TextMessageStartEvent
    | TextMessageContentEvent
    | TextMessageEndEvent
    | ToolCallStartEvent
    | ToolCallArgsEvent
    | ToolCallEndEvent
    | ToolCallResultEvent
    | ActivitySnapshotEvent
    | ActivityDeltaEvent
    | ReasoningStartEvent
    | ReasoningMessageStartEvent
    | ReasoningMessageContentEvent
    | ReasoningMessageEndEvent
    | ReasoningEndEvent
    | CustomEvent;
