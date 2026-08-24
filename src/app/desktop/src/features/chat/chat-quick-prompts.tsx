import {
    BulbOutlined,
    QuestionCircleOutlined,
    RobotOutlined,
} from "@ant-design/icons";
import { Prompts } from "@ant-design/x";
import { Button, Space, theme } from "antd";
import { useTranslation } from "react-i18next";
import { ChatQuickPromptKeys } from "./chat-quick-prompt-items";

const PromptIcons = {
    capabilities: <RobotOutlined />,
    tasks: <BulbOutlined />,
    questionTips: <QuestionCircleOutlined />,
};

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
            <Space
                className="chat-quick-prompts chat-quick-prompts-trial"
                size={8}
            >
                {ChatQuickPromptKeys.map((key) => (
                    <Button
                        key={key}
                        size="small"
                        icon={PromptIcons[key]}
                        onClick={() => onSelect(promptDescription(key))}
                    >
                        {t(`chat.prompts.${key}.label`)}
                    </Button>
                ))}
            </Space>
        );
    }

    return (
        <Prompts
            className="chat-quick-prompts chat-quick-prompts-full"
            items={ChatQuickPromptKeys.map((key) => ({
                key,
                description: promptDescription(key),
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
