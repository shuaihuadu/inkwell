import { ClearOutlined } from "@ant-design/icons";
import { Button, Flex, Space, Typography } from "antd";
import type { ReactNode } from "react";

interface AguiChatToolbarProps {
  title: string;
  leftExtra?: ReactNode;
  rightExtra?: ReactNode;
  onClear?: () => void;
  clearDisabled?: boolean;
  clearText?: string;
  showClear?: boolean;
}

export default function AguiChatToolbar({
  title,
  leftExtra,
  rightExtra,
  onClear,
  clearDisabled = false,
  clearText = "新对话",
  showClear = true,
}: AguiChatToolbarProps) {
  return (
    <Flex justify="space-between" align="center">
      <Space>
        <Typography.Title level={4} style={{ margin: 0 }}>
          {title}
        </Typography.Title>
        {leftExtra}
      </Space>

      <Space>
        {rightExtra}
        {showClear && (
          <Button
            icon={<ClearOutlined />}
            onClick={onClear}
            disabled={clearDisabled}
          >
            {clearText}
          </Button>
        )}
      </Space>
    </Flex>
  );
}
