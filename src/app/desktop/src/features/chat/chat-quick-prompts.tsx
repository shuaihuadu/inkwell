import {
    AppstoreAddOutlined,
    FileSearchOutlined,
} from "@ant-design/icons";
import { Prompts } from "@ant-design/x";
import { Button, Space, theme } from "antd";
import { useTranslation } from "react-i18next";

const FullChatPrompts = [
    { key: "research" },
    { key: "outline" },
    { key: "rewrite" },
    { key: "tools" },
];

const TrialChatPrompts = [
    {
        key: "research",
        icon: <FileSearchOutlined />,
    },
    {
        key: "outline",
        icon: <AppstoreAddOutlined />,
    },
    {
        key: "tools",
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
    const { t } = useTranslation();
    const { token } = theme.useToken();
    const promptDescription = (key: string): string =>
        t(`chat.prompts.${key}.description`);

    if (variant === "trial") {
        return (
            <Space className="chat-quick-prompts chat-quick-prompts-trial" size={8}>
                {TrialChatPrompts.map((prompt) => (
                    <Button
                        key={prompt.key}
                        size="small"
                        icon={prompt.icon}
                        onClick={() => onSelect(promptDescription(prompt.key))}
                    >
                        {t(`chat.prompts.${prompt.key}.label`)}
                    </Button>
                ))}
            </Space>
        );
    }

    return (
        <Prompts
            className="chat-quick-prompts chat-quick-prompts-full"
            items={FullChatPrompts.map((prompt) => ({
                ...prompt,
                description: promptDescription(prompt.key),
            }))}
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
