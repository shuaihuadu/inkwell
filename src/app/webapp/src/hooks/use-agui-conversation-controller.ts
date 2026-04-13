import { useCallback, useState } from "react";
import { useAGUIAgent } from "./use-agui-agent";

export interface UseAguiConversationControllerReturn {
  route: string;
  inputValue: string;
  loading: boolean;
  messages: ReturnType<typeof useAGUIAgent>["messages"];
  threadId: string;
  setInputValue: (value: string) => void;
  submit: () => Promise<void>;
  clear: () => void;
  changeRoute: (route: string) => void;
  switchSession: (sessionId: string) => Promise<void>;
}

export function useAguiConversationController(
  initialRoute: string,
): UseAguiConversationControllerReturn {
  const [route, setRoute] = useState(initialRoute);
  const [inputValue, setInputValue] = useState("");
  const {
    messages,
    loading,
    threadId,
    sendMessage,
    reset,
    loadMessages,
  } = useAGUIAgent(route);

  const clear = useCallback(() => {
    reset();
    setInputValue("");
  }, [reset]);

  const changeRoute = useCallback(
    (nextRoute: string) => {
      setRoute(nextRoute);
      setInputValue("");
      reset();
    },
    [reset],
  );

  const submit = useCallback(async () => {
    const normalized = inputValue.trim();
    if (!normalized) return;
    await sendMessage(normalized);
    setInputValue("");
  }, [inputValue, sendMessage]);

  const switchSession = useCallback(
    async (sessionId: string) => {
      setInputValue("");
      await loadMessages(sessionId);
    },
    [loadMessages],
  );

  return {
    route,
    inputValue,
    loading,
    messages,
    threadId,
    setInputValue,
    submit,
    clear,
    changeRoute,
    switchSession,
  };
}
