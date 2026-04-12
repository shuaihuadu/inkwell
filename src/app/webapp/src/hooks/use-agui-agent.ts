import { useCallback, useRef, useState } from "react";
import type { RunAgentInput, AGUIMessage } from "../services/agui-types";
import { API_BASE } from "../services/api";

export interface ChatMessage {
  id: string;
  role: "user" | "assistant" | "system";
  content: string;
  status: "pending" | "streaming" | "done" | "error";
}

export interface UseAGUIAgentReturn {
  /** 消息列表 */
  messages: ChatMessage[];
  /** 是否正在运行 */
  loading: boolean;
  /** 发送消息 */
  sendMessage: (content: string) => void;
  /** 重置对话 */
  reset: () => void;
}

/**
 * 自定义 Hook：对接后端 AG-UI 端点
 * 通过 SSE 流式接收 Agent 响应
 */
export function useAGUIAgent(aguiRoute: string = "/api/agui/writer"): UseAGUIAgentReturn {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [loading, setLoading] = useState(false);
  const abortRef = useRef<AbortController | null>(null);
  const threadIdRef = useRef<string>(crypto.randomUUID());

  const sendMessage = useCallback(
    async (content: string) => {
      // 添加用户消息
      const userMsg: ChatMessage = {
        id: crypto.randomUUID(),
        role: "user",
        content,
        status: "done",
      };

      setMessages((prev) => [...prev, userMsg]);
      setLoading(true);

      // 准备 AG-UI 请求
      const allMessages: AGUIMessage[] = [
        ...messages
          .filter((m) => m.status === "done")
          .map((m) => ({ id: m.id, role: m.role, content: m.content })),
        { id: userMsg.id, role: "user", content },
      ];

      const input: RunAgentInput = {
        threadId: threadIdRef.current,
        runId: crypto.randomUUID(),
        messages: allMessages,
      };

      // 创建助手消息占位
      const assistantMsgId = crypto.randomUUID();
      const assistantMsg: ChatMessage = {
        id: assistantMsgId,
        role: "assistant",
        content: "",
        status: "streaming",
      };
      setMessages((prev) => [...prev, assistantMsg]);

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

                if (event.type === "TEXT_MESSAGE_CONTENT") {
                  setMessages((prev) =>
                    prev.map((m) =>
                      m.id === assistantMsgId
                        ? { ...m, content: m.content + (event.delta ?? "") }
                        : m
                    )
                  );
                } else if (event.type === "RUN_FINISHED") {
                  setMessages((prev) =>
                    prev.map((m) =>
                      m.id === assistantMsgId ? { ...m, status: "done" } : m
                    )
                  );
                } else if (event.type === "RUN_ERROR") {
                  setMessages((prev) =>
                    prev.map((m) =>
                      m.id === assistantMsgId
                        ? {
                            ...m,
                            content: event.message ?? "发生错误",
                            status: "error",
                          }
                        : m
                    )
                  );
                }
              } catch {
                // 忽略不可解析的行
              }
            }
          }
        }

        // 确保最终状态
        setMessages((prev) =>
          prev.map((m) =>
            m.id === assistantMsgId && m.status === "streaming"
              ? { ...m, status: "done" }
              : m
          )
        );
      } catch (error) {
        if ((error as Error).name !== "AbortError") {
          setMessages((prev) =>
            prev.map((m) =>
              m.id === assistantMsgId
                ? {
                    ...m,
                    content: `请求失败: ${(error as Error).message}`,
                    status: "error",
                  }
                : m
            )
          );
        }
      } finally {
        setLoading(false);
        abortRef.current = null;
      }
    },
    [messages]
  );

  const reset = useCallback(() => {
    abortRef.current?.abort();
    setMessages([]);
    setLoading(false);
    threadIdRef.current = crypto.randomUUID();
  }, []);

  return { messages, loading, sendMessage, reset };
}
