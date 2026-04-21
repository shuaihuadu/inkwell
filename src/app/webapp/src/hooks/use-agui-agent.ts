import { useCallback, useEffect, useRef, useState } from "react";
import type { RunAgentInput } from "../services/agui-types";
import { API_BASE } from "../services/api";
import {
  HITL_MARKER,
  TOOL_CALL_MARKER,
  TOOL_RESULT_MARKER,
  extractMarkers,
  tryParseMarkerPayload,
} from "../services/stream-markers";

export interface HitlRequest {
  requestId: string;
  payload: unknown;
  decided: boolean;
  /** 审核请求正在回写到服务端（按钮点击后、响应返回前） */
  submitting?: boolean;
  /** 已决策时的选择（true=通过, false=退回），用于展示不同的结果 Banner */
  approvedValue?: boolean;
}

/**
 * 工具调用（Agent 内部触发的 Function Call）的可视化模型
 * 同一个 callId 在 TOOL_CALL 到达时创建为 running，TOOL_RESULT 到达时翻转为 done/error
 */
export interface ToolCallInfo {
  /** 调用链上 Executor 的 Id，用于前端分组 */
  executor?: string;
  /** LLM 分配的 Function Call Id */
  callId: string;
  /** 函数名，例如 publish_article */
  name?: string;
  /** 序列化后的参数文本（JSON 或原始字符串） */
  arguments?: string;
  /** 序列化后的返回文本 */
  result?: string;
  /** 工具调用异常信息 */
  error?: string;
  /** 当前状态 */
  status: "running" | "done" | "error";
}

export interface ChatMessage {
  id: string;
  role: "user" | "assistant" | "system";
  content: string;
  status: "pending" | "streaming" | "done" | "error";
  hitl?: HitlRequest;
  /** 本条 assistant 气泡里发生过的工具调用（按 callId 去重） */
  toolCalls?: ToolCallInfo[];
}

export interface UseAGUIAgentReturn {
  messages: ChatMessage[];
  loading: boolean;
  threadId: string;
  sendMessage: (content: string) => Promise<void>;
  reset: () => void;
  setThreadId: (id: string) => void;
  loadMessages: (sessionId: string) => Promise<void>;
  respondHitl: (
    messageId: string,
    requestId: string,
    approved: boolean,
  ) => Promise<void>;
}

// 与后端 WorkflowChatClient 的 *MarkerPrefix 常量对齐。
// 所有正则 / 剥离都走 services/stream-markers.ts，不在这里各写一份。

interface HitlMarkerPayload {
  id: string;
  payload: unknown;
}

interface ToolCallPayload {
  executor?: string;
  callId?: string;
  name?: string;
  arguments?: string;
}

interface ToolResultPayload {
  executor?: string;
  callId?: string;
  result?: string;
  exception?: string;
}

/**
 * 按 callId 合并 TOOL_CALL / TOOL_RESULT payload 列表到现有 toolCalls 数组
 */
function mergeToolCalls(
  existing: ToolCallInfo[] | undefined,
  callPayloads: readonly string[],
  resultPayloads: readonly string[],
): { next: ToolCallInfo[] | undefined; changed: boolean } {
  if (callPayloads.length === 0 && resultPayloads.length === 0) {
    return { next: existing, changed: false };
  }

  const map = new Map<string, ToolCallInfo>();
  for (const item of existing ?? []) {
    map.set(item.callId, { ...item });
  }

  for (const raw of callPayloads) {
    const p = tryParseMarkerPayload<ToolCallPayload>(raw);
    if (!p?.callId) continue;
    const prev = map.get(p.callId);
    map.set(p.callId, {
      callId: p.callId,
      executor: p.executor ?? prev?.executor,
      name: p.name ?? prev?.name,
      arguments: p.arguments ?? prev?.arguments,
      result: prev?.result,
      error: prev?.error,
      status:
        prev?.status === "done" || prev?.status === "error"
          ? prev.status
          : "running",
    });
  }

  for (const raw of resultPayloads) {
    const p = tryParseMarkerPayload<ToolResultPayload>(raw);
    if (!p?.callId) continue;
    const prev = map.get(p.callId) ?? {
      callId: p.callId,
      status: "running" as const,
    };
    map.set(p.callId, {
      ...prev,
      executor: p.executor ?? prev.executor,
      result: p.result ?? prev.result,
      error: p.exception ?? prev.error,
      status: p.exception ? "error" : "done",
    });
  }

  return { next: Array.from(map.values()), changed: true };
}

/**
 * 从最新 payload 列表中挑选一个可解析的 HITL 请求（取最后一个能解析的）
 */
function pickLatestHitl(
  payloads: readonly string[],
  fallback: HitlRequest | undefined,
): HitlRequest | undefined {
  for (let i = payloads.length - 1; i >= 0; i--) {
    const parsed = tryParseMarkerPayload<HitlMarkerPayload>(payloads[i]);
    if (parsed?.id) {
      return {
        requestId: parsed.id,
        payload: parsed.payload,
        decided: false,
      };
    }
  }
  return fallback;
}

export function useAGUIAgent(
  aguiRoute: string = "/api/agui/writer",
): UseAGUIAgentReturn {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [loading, setLoading] = useState(false);
  const abortRef = useRef<AbortController | null>(null);
  const threadIdRef = useRef<string>(crypto.randomUUID());
  const [threadId, setThreadIdState] = useState(threadIdRef.current);

  useEffect(() => {
    return () => {
      abortRef.current?.abort();
    };
  }, []);

  const setThreadId = useCallback((id: string) => {
    threadIdRef.current = id;
    setThreadIdState(id);
  }, []);

  const loadMessages = useCallback(async (sessionId: string) => {
    try {
      const res = await fetch(`${API_BASE}/api/sessions/${sessionId}/messages`);
      if (!res.ok) return;
      const data: Array<{
        id: string;
        role: string;
        content: string;
        status: string;
      }> = await res.json();
      const loaded: ChatMessage[] = data.map((m) => ({
        id: m.id,
        role: m.role as ChatMessage["role"],
        content: m.content,
        status: (m.status || "done") as ChatMessage["status"],
      }));
      setMessages(loaded);
      threadIdRef.current = sessionId;
      setThreadIdState(sessionId);
    } catch {
      // ignore
    }
  }, []);

  const sendMessage = useCallback(
    async (content: string) => {
      const normalized = content.trim();
      if (!normalized) return;

      abortRef.current?.abort();

      const userMsg: ChatMessage = {
        id: crypto.randomUUID(),
        role: "user",
        content: normalized,
        status: "done",
      };

      const assistantMsgId = crypto.randomUUID();
      const assistantMsg: ChatMessage = {
        id: assistantMsgId,
        role: "assistant",
        content: "",
        status: "streaming",
      };

      setMessages((prev) => [...prev, userMsg, assistantMsg]);
      setLoading(true);

      const input: RunAgentInput = {
        threadId: threadIdRef.current,
        runId: crypto.randomUUID(),
        messages: [{ id: userMsg.id, role: "user", content: normalized }],
      };

      try {
        abortRef.current = new AbortController();

        const response = await fetch(`${API_BASE}${aguiRoute}`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(input),
          signal: abortRef.current.signal,
        });

        if (!response.ok) {
          throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const reader = response.body?.getReader();
        const decoder = new TextDecoder();
        let buffer = "";

        if (reader) {
          while (true) {
            const { done, value } = await reader.read();
            if (done) break;

            buffer += decoder.decode(value, { stream: true });
            const lines = buffer.split("\n");
            buffer = lines.pop() ?? "";

            for (const line of lines) {
              if (!line.startsWith("data: ")) continue;
              const data = line.slice(6).trim();
              if (!data || data === "[DONE]") continue;

              try {
                const event = JSON.parse(data);

                switch (event.type) {
                  case "TEXT_MESSAGE_CONTENT":
                    setMessages((prev) =>
                      prev.map((m) => {
                        if (m.id !== assistantMsgId) return m;
                        const merged = m.content + (event.delta ?? "");

                        // 从当前累积文本里提取 HITL / 工具调用 payload。
                        // 注意：这里只用 payloads 更新结构化状态，正文依然保留原始 merged
                        // （含 markers 原文）。原因是 marker 可能跨 SSE delta 到达，
                        // 提前在 m.content 上剥离会导致"前缀在上一段被抹掉、尾部片段留在下一段"
                        // 的残片（典型症状就是正文里出现 <<>> 等散落字符）。
                        // 真正的剥离统一放到渲染组件里做一次。
                        const { payloads } = extractMarkers(merged, [
                          HITL_MARKER,
                          TOOL_CALL_MARKER,
                          TOOL_RESULT_MARKER,
                        ]);

                        const hitlPayloads = payloads.get(HITL_MARKER) ?? [];
                        const callPayloads =
                          payloads.get(TOOL_CALL_MARKER) ?? [];
                        const resultPayloads =
                          payloads.get(TOOL_RESULT_MARKER) ?? [];

                        const hitl =
                          hitlPayloads.length > 0
                            ? pickLatestHitl(hitlPayloads, m.hitl)
                            : m.hitl;
                        const toolMerge = mergeToolCalls(
                          m.toolCalls,
                          callPayloads,
                          resultPayloads,
                        );

                        return {
                          ...m,
                          content: merged,
                          ...(hitl ? { hitl } : {}),
                          ...(toolMerge.changed
                            ? { toolCalls: toolMerge.next }
                            : {}),
                        };
                      }),
                    );
                    break;

                  case "TOOL_CALL_START":
                    setMessages((prev) => [
                      ...prev,
                      {
                        id: `tool-${event.toolCallId ?? crypto.randomUUID()}`,
                        role: "system" as const,
                        content: `调用工具: ${event.toolCallName ?? "unknown"}`,
                        status: "done" as const,
                      },
                    ]);
                    break;

                  case "TOOL_CALL_RESULT":
                    setMessages((prev) => [
                      ...prev,
                      {
                        id: `tool-result-${event.toolCallId ?? crypto.randomUUID()}`,
                        role: "system" as const,
                        content: "工具返回结果",
                        status: "done" as const,
                      },
                    ]);
                    break;

                  case "RUN_FINISHED":
                    setMessages((prev) =>
                      prev.map((m) =>
                        m.id === assistantMsgId
                          ? { ...m, status: "done" as const }
                          : m,
                      ),
                    );
                    break;

                  case "RUN_ERROR":
                    setMessages((prev) =>
                      prev.map((m) =>
                        m.id === assistantMsgId
                          ? {
                              ...m,
                              content: event.message ?? "Error occurred",
                              status: "error" as const,
                            }
                          : m,
                      ),
                    );
                    break;

                  default:
                    break;
                }
              } catch {
                // skip
              }
            }
          }
        }

        setMessages((prev) =>
          prev.map((m) =>
            m.id === assistantMsgId && m.status === "streaming"
              ? { ...m, status: "done" as const }
              : m,
          ),
        );
      } catch (error) {
        if ((error as Error).name !== "AbortError") {
          setMessages((prev) =>
            prev.map((m) =>
              m.id === assistantMsgId
                ? {
                    ...m,
                    content: `Request failed: ${(error as Error).message}`,
                    status: "error" as const,
                  }
                : m,
            ),
          );
        }
      } finally {
        setLoading(false);
        abortRef.current = null;
      }
    },
    [aguiRoute],
  );

  const reset = useCallback(() => {
    abortRef.current?.abort();
    setMessages([]);
    setLoading(false);
    const newId = crypto.randomUUID();
    threadIdRef.current = newId;
    setThreadIdState(newId);
  }, []);

  const respondHitl = useCallback(
    async (messageId: string, requestId: string, approved: boolean) => {
      // 先标记 submitting，让按钮进入 loading 态
      setMessages((prev) =>
        prev.map((m) =>
          m.id === messageId && m.hitl
            ? { ...m, hitl: { ...m.hitl, submitting: true } }
            : m,
        ),
      );

      try {
        const res = await fetch(`${API_BASE}/api/hitl/${requestId}/respond`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ approved }),
        });
        if (!res.ok) {
          throw new Error(`HTTP ${res.status}`);
        }
        setMessages((prev) =>
          prev.map((m) =>
            m.id === messageId && m.hitl
              ? {
                  ...m,
                  hitl: {
                    ...m.hitl,
                    submitting: false,
                    decided: true,
                    approvedValue: approved,
                  },
                }
              : m,
          ),
        );
      } catch (err) {
        // 回写失败：清掉 submitting 让用户可以重试，并追加一条系统提示
        setMessages((prev) => [
          ...prev.map((m) =>
            m.id === messageId && m.hitl
              ? { ...m, hitl: { ...m.hitl, submitting: false } }
              : m,
          ),
          {
            id: crypto.randomUUID(),
            role: "system",
            content: `审核回写失败：${(err as Error).message}`,
            status: "error",
          },
        ]);
      }
    },
    [],
  );

  return {
    messages,
    loading,
    threadId,
    sendMessage,
    reset,
    setThreadId,
    loadMessages,
    respondHitl,
  };
}
