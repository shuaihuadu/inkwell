import { Flex } from "antd";
import type { CSSProperties, ReactNode } from "react";
import type { ChatMessage } from "../hooks/use-agui-agent";
import AguiChatPanel from "./agui-chat-panel";
import AguiChatToolbar from "./agui-chat-toolbar";

interface AguiConversationShellProps {
  leftExtra?: ReactNode;
  rightExtra?: ReactNode;
  onClear?: () => void;
  clearDisabled?: boolean;
  clearText?: string;
  showClear?: boolean;
  messages: ChatMessage[];
  loading: boolean;
  inputValue: string;
  onInputChange: (value: string) => void;
  onSubmit: () => void;
  placeholder: string;
  emptyText: string;
  streamingText?: string;
  statusText?: string;
  submitType?: "enter" | "shiftEnter";
  disableWhileLoading?: boolean;
  shellStyle?: CSSProperties;
  toolbarWrapperStyle?: CSSProperties;
  messageContainerStyle?: CSSProperties;
}

export default function AguiConversationShell({
  leftExtra,
  rightExtra,
  onClear,
  clearDisabled,
  clearText,
  showClear,
  messages,
  loading,
  inputValue,
  onInputChange,
  onSubmit,
  placeholder,
  emptyText,
  streamingText,
  statusText,
  submitType,
  disableWhileLoading,
  shellStyle,
  toolbarWrapperStyle,
  messageContainerStyle,
}: AguiConversationShellProps) {
  return (
    <Flex vertical gap={0} style={{ height: "100%", ...shellStyle }}>
      <div
        style={{
          padding: "0 16px",
          height: 56,
          display: "flex",
          alignItems: "center",
          borderBottom: "1px solid #f0f0f0",
          ...toolbarWrapperStyle,
        }}
      >
        <AguiChatToolbar
          leftExtra={leftExtra}
          rightExtra={rightExtra}
          onClear={onClear}
          clearDisabled={clearDisabled}
          clearText={clearText}
          showClear={showClear}
        />
      </div>

      <AguiChatPanel
        messages={messages}
        loading={loading}
        inputValue={inputValue}
        onInputChange={onInputChange}
        onSubmit={onSubmit}
        placeholder={placeholder}
        emptyText={emptyText}
        streamingText={streamingText}
        statusText={statusText}
        submitType={submitType}
        disableWhileLoading={disableWhileLoading}
        containerStyle={{ flex: 1, minHeight: 0 }}
        messageContainerStyle={messageContainerStyle}
      />
    </Flex>
  );
}
