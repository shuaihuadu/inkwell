import { useCallback, useEffect, useRef, useState } from "react";
import type { RunAgentInput } from "../services/agui-types";
import { API_BASE } from "../services/api";

export interface HitlRequest {
  requestId: string;
  payload: unknown;
  decided: boolean;
  /** 审核请求正在回写到服务端（按钮点击后、响应返回前） */
  submitting?: boolean;
  /** 已决策时的选择（true=通过, false=退回），用于展示不同的结果 Banner */
  approvedValue?: boolean;
}

export interface ChatMessage {
  id: string;
  role: "user" | "assistant" | "system";
  content: string;
  status: "pending" | "streaming" | "done" | "error";
  hitl?: HitlRequest;
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

// 与后端 WorkflowChatClient.HitlMarkerPrefix / Suffix 保持一致
// 使用 g 标志以便一次剥离多个标记；非贪婪 JSON 体匹配
const HITL_MARKER_REGEX = /<<<HITL_REQUEST:(\{[\s\S]*?\})>>>/g;
// 仅匹配"前缀已到、尾标还没到"的残片，用于流式过程中隐藏未闭合的标记头
const HITL_PARTIAL_PREFIX_REGEX = /<<<HITL_REQUEST:[\s\S]*$/;

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

                        // 1) 先扫描所有已闭合的 HITL 标记，收集 payload 并逐个剥离
                        let stripped = merged;
                        let extractedHitl: HitlRequest | undefined = m.hitl;
                        const matches = Array.from(
                          merged.matchAll(HITL_MARKER_REGEX),
                        );
                        if (matches.length > 0) {
                          // 统一剥离所有闭合标记，避免 JSON 解析失败时残留半截 <<<...>>>
                          stripped = merged.replace(HITL_MARKER_REGEX, "");

                          // 取最后一个可解析的 payload 作为当前待审核对象
                          for (let i = matches.length - 1; i >= 0; i--) {
                            try {
                              const parsed = JSON.parse(matches[i][1]) as {
                                id: string;
                                payload: unknown;
                              };
                              extractedHitl = {
                                requestId: parsed.id,
                                payload: parsed.payload,
                                decided: false,
                              };
                              break;
                            } catch {
                              // 这一个 payload 解析失败，尝试前一个
                            }
                          }
                        }

                        // 2) 对于"前缀已到、后缀还没到"的流式中途状态，
                        //    暂时把前缀之后的未闭合片段从展示内容里摘掉，避免用户看到 <<<...
                        if (HITL_PARTIAL_PREFIX_REGEX.test(stripped)) {
                          stripped = stripped.replace(
                            HITL_PARTIAL_PREFIX_REGEX,
                            "",
                          );
                        }

                        return {
                          ...m,
                          content: stripped.trim(),
                          ...(extractedHitl ? { hitl: extractedHitl } : {}),
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
