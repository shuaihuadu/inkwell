import { useCallback, useEffect, useRef, useState } from "react";
import type { RunAgentInput } from "../services/agui-types";
import { API_BASE } from "../services/api";

export interface ChatMessage {
  id: string;
  role: "user" | "assistant" | "system";
  content: string;
  status: "pending" | "streaming" | "done" | "error";
}

export interface UseAGUIAgentReturn {
  messages: ChatMessage[];
  loading: boolean;
  threadId: string;
  sendMessage: (content: string) => Promise<void>;
  reset: () => void;
  setThreadId: (id: string) => void;
  loadMessages: (sessionId: string) => Promise<void>;
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
                      prev.map((m) =>
                        m.id === assistantMsgId
                          ? {
                              ...m,
                              content: m.content + (event.delta ?? ""),
                            }
                          : m,
                      ),
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

  return {
    messages,
    loading,
    threadId,
    sendMessage,
    reset,
    setThreadId,
    loadMessages,
  };
}
