import {
    AppstoreAddOutlined,
    FileSearchOutlined,
} from "@ant-design/icons";
import { Prompts } from "@ant-design/x";
import { Button, Space, theme } from "antd";

const FullChatPrompts = [
    { key: "research", description: "整理一份竞品研究框架" },
    { key: "outline", description: "为调研报告设计目录" },
    { key: "rewrite", description: "帮我优化一段产品介绍文案" },
    { key: "tools", description: "展示工具调用示例" },
];

const TrialChatPrompts = [
    {
        key: "research",
        label: "研究框架",
        description: "整理一份竞品研究框架",
        icon: <FileSearchOutlined />,
    },
    {
        key: "outline",
        label: "报告目录",
        description: "为调研报告设计目录",
        icon: <AppstoreAddOutlined />,
    },
    {
        key: "tools",
        label: "工具调用",
        description: "展示工具调用示例",
        icon: <AppstoreAddOutlined />,
    },
];

interface ChatQuickPromptsProps {
    variant: "full" | "trial";
    compact?: boolean;
    onSelect: (prompt: string) => void;
}

export function ChatQuickPrompts({
    variant,
    compact = true,
    onSelect,
}: ChatQuickPromptsProps) {
    const { token } = theme.useToken();

    if (variant === "trial") {
        return (
            <Space className="chat-quick-prompts chat-quick-prompts-trial" size={8}>
                {TrialChatPrompts.map((prompt) => (
                    <Button
                        key={prompt.key}
                        size="small"
                        icon={prompt.icon}
                        onClick={() => onSelect(prompt.description)}
                    >
                        {prompt.label}
                    </Button>
                ))}
            </Space>
        );
    }

    return (
        <Prompts
            className="chat-quick-prompts chat-quick-prompts-full"
            items={FullChatPrompts}
            wrap
            onItemClick={(info) => onSelect(String(info.data.description))}
            styles={
                compact
                    ? {
                          item: {
                              paddingBlock: 5,
                              paddingInline: 10,
                              border: `1px solid ${token.colorBorder}`,
                              color: token.colorText,
                              background: token.colorFillSecondary,
                              fontSize: 12,
                          },
                          itemContent: {
                              color: token.colorText,
                          },
                      }
                    : undefined
            }
        />
    );
}
