import { PlusOutlined } from "@ant-design/icons";
import { Button, Flex, Space } from "antd";
import type { ReactNode } from "react";

interface AguiChatToolbarProps {
  leftExtra?: ReactNode;
  rightExtra?: ReactNode;
  onClear?: () => void;
  clearDisabled?: boolean;
  clearText?: string;
  showClear?: boolean;
}

export default function AguiChatToolbar({
  leftExtra,
  rightExtra,
  onClear,
  clearDisabled = false,
  clearText = "新会话",
  showClear = true,
}: AguiChatToolbarProps) {
  return (
    <Flex justify="space-between" align="center" style={{ width: "100%" }}>
      <Space>{leftExtra}</Space>

      <Space>
        {rightExtra}
        {showClear && (
          <Button
            icon={<PlusOutlined />}
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
