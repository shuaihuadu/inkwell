/** AG-UI 事件类型 */
export interface AGUIEvent {
  type: string;
  rawEvent?: Record<string, unknown>;
}

export interface AGUITextMessageStartEvent extends AGUIEvent {
  type: "TEXT_MESSAGE_START";
  messageId: string;
  role: string;
}

export interface AGUITextMessageContentEvent extends AGUIEvent {
  type: "TEXT_MESSAGE_CONTENT";
  messageId: string;
  delta: string;
}

export interface AGUITextMessageEndEvent extends AGUIEvent {
  type: "TEXT_MESSAGE_END";
  messageId: string;
}

export interface AGUIRunStartedEvent extends AGUIEvent {
  type: "RUN_STARTED";
  threadId: string;
  runId: string;
}

export interface AGUIRunFinishedEvent extends AGUIEvent {
  type: "RUN_FINISHED";
  threadId: string;
  runId: string;
}

export interface AGUIRunErrorEvent extends AGUIEvent {
  type: "RUN_ERROR";
  message: string;
}

/** AG-UI 消息 */
export interface AGUIMessage {
  id: string;
  role: string;
  content: string;
}

/** AG-UI 运行输入 */
export interface RunAgentInput {
  threadId: string;
  runId: string;
  messages: AGUIMessage[];
  tools?: unknown[];
  state?: unknown;
  context?: Array<{ description: string; value: string }>;
  forwardedProperties?: unknown;
}
