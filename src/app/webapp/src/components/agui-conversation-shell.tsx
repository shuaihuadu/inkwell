import { Flex } from "antd";
import type { CSSProperties, ReactNode } from "react";
import type { ChatMessage } from "../hooks/use-agui-agent";
import AguiChatPanel from "./agui-chat-panel";
import AguiChatToolbar from "./agui-chat-toolbar";

interface AguiConversationShellProps {
  title: string;
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
  title,
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
    <Flex vertical gap={12} style={{ height: "100%", ...shellStyle }}>
      <div style={toolbarWrapperStyle}>
        <AguiChatToolbar
          title={title}
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
