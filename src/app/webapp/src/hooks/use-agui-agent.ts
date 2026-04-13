import { useCallback, useEffect, useRef, useState } from "react";
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
  sendMessage: (content: string) => Promise<void>;
  /** 重置对话 */
  reset: () => void;
}

/** localStorage key 前缀 */
const STORAGE_PREFIX = "inkwell-chat-";

/** 从 localStorage 恢复会话 */
function loadSession(route: string): {
  messages: ChatMessage[];
  threadId: string;
} {
  try {
    const raw = localStorage.getItem(`${STORAGE_PREFIX}${route}`);
    if (raw) {
      const data = JSON.parse(raw);
      // 只恢复已完成的消息，丢弃流式中断的
      const messages: ChatMessage[] = (data.messages ?? []).filter(
        (m: ChatMessage) => m.status === "done",
      );
      return { messages, threadId: data.threadId ?? crypto.randomUUID() };
    }
  } catch {
    // 解析失败则忽略
  }
  return { messages: [], threadId: crypto.randomUUID() };
}

/** 保存会话到 localStorage */
function saveSession(
  route: string,
  messages: ChatMessage[],
  threadId: string,
) {
  try {
    const doneMessages = messages.filter((m) => m.status === "done");
    localStorage.setItem(
      `${STORAGE_PREFIX}${route}`,
      JSON.stringify({ messages: doneMessages, threadId }),
    );
  } catch {
    // 存储满或无权限则忽略
  }
}

/** 清除会话 */
function clearSession(route: string) {
  try {
    localStorage.removeItem(`${STORAGE_PREFIX}${route}`);
  } catch {
    // 忽略
  }
}

/**
 * 自定义 Hook：对接后端 AG-UI 端点
 * 通过 SSE 流式接收 Agent 响应，支持 localStorage 持久化
 */
export function useAGUIAgent(
  aguiRoute: string = "/api/agui/writer",
): UseAGUIAgentReturn {
  const [messages, setMessages] = useState<ChatMessage[]>(() => {
    return loadSession(aguiRoute).messages;
  });
  const [loading, setLoading] = useState(false);
  const abortRef = useRef<AbortController | null>(null);
  const threadIdRef = useRef<string>(loadSession(aguiRoute).threadId);
  const messagesRef = useRef<ChatMessage[]>([]);
  const routeRef = useRef(aguiRoute);

  // 路由切换时重新加载对应会话
  useEffect(() => {
    if (routeRef.current !== aguiRoute) {
      routeRef.current = aguiRoute;
      const session = loadSession(aguiRoute);
      setMessages(session.messages);
      threadIdRef.current = session.threadId;
    }
  }, [aguiRoute]);

  useEffect(() => {
    messagesRef.current = messages;
  }, [messages]);

  // 消息变化时自动保存（仅保存已完成的消息）
  useEffect(() => {
    if (messages.length > 0) {
      saveSession(routeRef.current, messages, threadIdRef.current);
    }
  }, [messages]);

  useEffect(() => {
    return () => {
      abortRef.current?.abort();
    };
  }, []);

  const sendMessage = useCallback(
    async (content: string) => {
      const normalized = content.trim();
      if (!normalized) {
        return;
      }

      // 启动新请求前中止旧请求，避免并发串流污染当前消息
      abortRef.current?.abort();

      // 添加用户消息
      const userMsg: ChatMessage = {
        id: crypto.randomUUID(),
        role: "user",
        content: normalized,
        status: "done",
      };

      // 创建助手消息占位
      const assistantMsgId = crypto.randomUUID();
      const assistantMsg: ChatMessage = {
        id: assistantMsgId,
        role: "assistant",
        content: "",
        status: "streaming",
      };

      setMessages((prev) => [...prev, userMsg, assistantMsg]);
      setLoading(true);

      // 准备 AG-UI 请求
      const allMessages: AGUIMessage[] = [
        ...messagesRef.current
          .filter((m) => m.status === "done")
          .map((m) => ({ id: m.id, role: m.role, content: m.content })),
        { id: userMsg.id, role: "user", content: normalized },
      ];

      const input: RunAgentInput = {
        threadId: threadIdRef.current,
        runId: crypto.randomUUID(),
        messages: allMessages,
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
                  case "RUN_STARTED":
                    // 静默处理，不显示系统消息
                    break;

                  case "TEXT_MESSAGE_START":
                    // 可选：标记新消息开始
                    break;

                  case "TEXT_MESSAGE_CONTENT":
                    setMessages((prev) =>
                      prev.map((m) =>
                        m.id === assistantMsgId
                          ? { ...m, content: m.content + (event.delta ?? "") }
                          : m,
                      ),
                    );
                    break;

                  case "TEXT_MESSAGE_END":
                    // 文本消息结束（RUN_FINISHED 之前可能有多段）
                    break;

                  case "TOOL_CALL_START":
                    setMessages((prev) => [
                      ...prev,
                      {
                        id: `tool-${event.toolCallId ?? crypto.randomUUID()}`,
                        role: "system",
                        content: `🔧 调用工具: ${event.toolCallName ?? "unknown"}`,
                        status: "done",
                      },
                    ]);
                    break;

                  case "TOOL_CALL_RESULT":
                    setMessages((prev) => [
                      ...prev,
                      {
                        id: `tool-result-${event.toolCallId ?? crypto.randomUUID()}`,
                        role: "system",
                        content: `✅ 工具返回结果`,
                        status: "done",
                      },
                    ]);
                    break;

                  case "STATE_SNAPSHOT":
                    // 静默处理，不显示系统消息
                    break;

                  case "RUN_FINISHED":
                    setMessages((prev) =>
                      prev.map((m) =>
                        m.id === assistantMsgId ? { ...m, status: "done" } : m,
                      ),
                    );
                    break;

                  case "RUN_ERROR":
                    setMessages((prev) =>
                      prev.map((m) =>
                        m.id === assistantMsgId
                          ? {
                              ...m,
                              content: event.message ?? "发生错误",
                              status: "error",
                            }
                          : m,
                      ),
                    );
                    break;

                  default:
                    // 其他未知事件类型静默忽略
                    break;
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
                    content: `请求失败: ${(error as Error).message}`,
                    status: "error",
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
    threadIdRef.current = crypto.randomUUID();
  }, []);

  return { messages, loading, sendMessage, reset };
}
