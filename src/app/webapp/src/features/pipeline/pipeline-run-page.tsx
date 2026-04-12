import { useState } from "react";
import { Typography, Button, Space, Flex } from "antd";
import { ClearOutlined, RobotOutlined, UserOutlined } from "@ant-design/icons";
import { Bubble, Sender } from "@ant-design/x";
import { useAGUIAgent } from "../../hooks/use-agui-agent";
import type { ChatMessage } from "../../hooks/use-agui-agent";

/** 将 ChatMessage 转为 Bubble.List 的 items 格式 */
function toBubbleItems(messages: ChatMessage[]) {
  return messages.map((msg) => ({
    key: msg.id,
    role: msg.role as string,
    content: msg.content || (msg.status === "streaming" ? "思考中..." : ""),
    loading: msg.status === "streaming",
  }));
}

/** 角色配置 */
const roles: Record<string, { name: string; avatar: React.ReactNode }> = {
  user: {
    name: "用户",
    avatar: <UserOutlined />,
  },
  assistant: {
    name: "Inkwell",
    avatar: <RobotOutlined />,
  },
};

export default function PipelineRunPage() {
  const { messages, loading, sendMessage, reset } = useAGUIAgent();
  const [inputValue, setInputValue] = useState("");

  const handleSend = (value: string) => {
    if (!value.trim()) return;
    sendMessage(value.trim());
    setInputValue("");
  };

  return (
    <Flex vertical style={{ height: "100%" }} gap={16}>
      {/* 标题栏 */}
      <Flex justify="space-between" align="center">
        <Typography.Title level={4} style={{ margin: 0 }}>
          内容创作助手
        </Typography.Title>
        <Space>
          <Button icon={<ClearOutlined />} onClick={reset} disabled={loading}>
            新对话
          </Button>
        </Space>
      </Flex>

      {/* 对话区域 */}
      <div
        style={{
          flex: 1,
          overflow: "auto",
          padding: "0 8px",
          minHeight: 0,
        }}
      >
        {messages.length === 0 ? (
          <Flex
            vertical
            align="center"
            justify="center"
            style={{ height: "100%", opacity: 0.5 }}
          >
            <RobotOutlined style={{ fontSize: 48, marginBottom: 16 }} />
            <Typography.Text type="secondary">
              输入文章主题开始创作，例如：AI 在医疗健康领域的未来
            </Typography.Text>
          </Flex>
        ) : (
          <Bubble.List
            items={toBubbleItems(messages)}
            roles={{
              user: {
                placement: "end",
                avatar: { icon: roles.user.avatar, style: { background: "#1677ff" } },
              },
              assistant: {
                placement: "start",
                avatar: { icon: roles.assistant.avatar, style: { background: "#52c41a" } },
                typing: { step: 2, interval: 30 },
              },
            }}
          />
        )}
      </div>

      {/* 输入区域 */}
      <Sender
        value={inputValue}
        onChange={setInputValue}
        onSubmit={handleSend}
        loading={loading}
        placeholder="输入文章主题开始创作..."
      />
    </Flex>
  );
}
