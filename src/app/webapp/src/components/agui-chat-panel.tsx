import {
  RobotOutlined,
  UserOutlined,
  InfoCircleOutlined,
} from "@ant-design/icons";
import { Bubble, Sender } from "@ant-design/x";
import { XMarkdown } from "@ant-design/x-markdown";
import { Flex, Typography } from "antd";
import { useEffect, useMemo, useRef, useState } from "react";
import type { CSSProperties } from "react";
import type { ChatMessage } from "../hooks/use-agui-agent";

interface AguiChatPanelProps {
  messages: ChatMessage[];
  loading: boolean;
  inputValue: string;
  onInputChange: (value: string) => void;
  onSubmit: () => void;
  placeholder: string;
  emptyText: string;
  streamingText?: string;
  containerStyle?: CSSProperties;
  messageContainerStyle?: CSSProperties;
  submitType?: "enter" | "shiftEnter";
  statusText?: string;
  disableWhileLoading?: boolean;
}

function toBubbleItems(messages: ChatMessage[], streamingText: string) {
  return messages.map((msg) => {
    const isStreaming = msg.status === "streaming";
    const hasContent = !!msg.content;

    if (msg.role === "system") {
      return {
        key: msg.id,
        role: "system" as string,
        content: msg.content,
        variant: "borderless" as const,
      };
    }

    return {
      key: msg.id,
      role: msg.role as string,
      content: msg.content || (isStreaming ? streamingText : ""),
      // loading 仅在等待首个 token 时显示加载指示器
      loading: isStreaming && !hasContent,
      streaming: isStreaming,
      // 流式期间不挂 contentRender，让 Bubble 逐字显示纯文本
      // 完成后才用 XMarkdown 渲染格式化 Markdown
      ...(msg.role === "assistant" && hasContent && !isStreaming
        ? {
            contentRender: () => <XMarkdown content={msg.content} />,
          }
        : {}),
    };
  });
}

export default function AguiChatPanel({
  messages,
  loading,
  inputValue,
  onInputChange,
  onSubmit,
  placeholder,
  emptyText,
  streamingText = "思考中...",
  containerStyle,
  messageContainerStyle,
  submitType = "enter",
  statusText,
  disableWhileLoading = true,
}: AguiChatPanelProps) {
  const scrollRef = useRef<HTMLDivElement | null>(null);
  const [autoScrollEnabled, setAutoScrollEnabled] = useState(true);

  const bubbleItems = useMemo(
    () => toBubbleItems(messages, streamingText),
    [messages, streamingText],
  );

  useEffect(() => {
    const el = scrollRef.current;
    if (!el || !autoScrollEnabled) {
      return;
    }

    el.scrollTop = el.scrollHeight;
  }, [bubbleItems, autoScrollEnabled]);

  const handleScroll = () => {
    const el = scrollRef.current;
    if (!el) {
      return;
    }

    const distanceFromBottom = el.scrollHeight - el.scrollTop - el.clientHeight;
    setAutoScrollEnabled(distanceFromBottom < 60);
  };

  return (
    <Flex
      vertical
      gap={12}
      style={{
        height: "100%",
        padding: "12px 0 16px",
        marginLeft: "20px",
        ...containerStyle,
      }}
    >
      <div
        ref={scrollRef}
        onScroll={handleScroll}
        style={{
          flex: 1,
          overflow: "auto",
          padding: "0 16px",
          minHeight: 0,
          ...messageContainerStyle,
        }}
      >
        {messages.length === 0 ? (
          <Flex
            vertical
            align="center"
            justify="center"
            style={{ height: "100%", opacity: 0.55 }}
          >
            <RobotOutlined style={{ fontSize: 42, marginBottom: 12 }} />
            <Typography.Text type="secondary">{emptyText}</Typography.Text>
          </Flex>
        ) : (
          <Bubble.List
            items={bubbleItems}
            role={{
              user: {
                placement: "end",
                avatar: <UserOutlined />,
              },
              assistant: {
                placement: "start",
                avatar: <RobotOutlined />,
                typing: { effect: "typing", step: 20, interval: 50 },
              },
              system: {
                placement: "start",
                avatar: <InfoCircleOutlined />,
                variant: "borderless",
              },
            }}
          />
        )}
      </div>

      {statusText && (
        <Typography.Text type="secondary" style={{ fontSize: 12 }}>
          {statusText}
        </Typography.Text>
      )}

      <Sender
        value={inputValue}
        onChange={onInputChange}
        onSubmit={onSubmit}
        loading={loading}
        disabled={disableWhileLoading && loading}
        placeholder={placeholder}
        submitType={submitType}
      />
    </Flex>
  );
}
