import { useCallback, useState } from "react";
import { useAGUIAgent } from "./use-agui-agent";

export interface UseAguiConversationControllerReturn {
  route: string;
  inputValue: string;
  loading: boolean;
  messages: ReturnType<typeof useAGUIAgent>["messages"];
  setInputValue: (value: string) => void;
  submit: () => Promise<void>;
  clear: () => void;
  changeRoute: (route: string) => void;
}

/**
 * 统一 AGUI 会话控制：路由切换、输入发送、会话清空
 */
export function useAguiConversationController(
  initialRoute: string,
): UseAguiConversationControllerReturn {
  const [route, setRoute] = useState(initialRoute);
  const [inputValue, setInputValue] = useState("");
  const { messages, loading, sendMessage, reset } = useAGUIAgent(route);

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
    if (!normalized) {
      return;
    }

    await sendMessage(normalized);
    setInputValue("");
  }, [inputValue, sendMessage]);

  return {
    route,
    inputValue,
    loading,
    messages,
    setInputValue,
    submit,
    clear,
    changeRoute,
  };
}
