import { PaperClipOutlined } from "@ant-design/icons";
import { Sender } from "@ant-design/x";
import { Button, Flex, Space, Tooltip, Typography } from "antd";
import { useTranslation } from "react-i18next";
import { ChatQuickPrompts } from "./chat-quick-prompts";

interface ChatComposerProps {
    variant: "full" | "trial";
    value: string;
    loading: boolean;
    showPrompts: boolean;
    versionLabel?: string;
    onChange: (value: string) => void;
    onSubmit: (value: string) => void;
    onCancel: () => void;
}

export function ChatComposer({
    variant,
    value,
    loading,
    showPrompts,
    versionLabel,
    onChange,
    onSubmit,
    onCancel,
}: ChatComposerProps) {
    const { t } = useTranslation();
    return (
        <div className={`chat-composer chat-composer-${variant}`}>
            {showPrompts && (
                <ChatQuickPrompts variant={variant} onSelect={onSubmit} />
            )}
            <Sender
                className="chat-sender"
                suffix={false}
                autoSize={{
                    minRows: variant === "full" && showPrompts ? 1 : 2,
                    maxRows: variant === "full" ? 6 : 5,
                }}
                placeholder={t("chat.composer.placeholder")}
                value={value}
                onChange={onChange}
                onSubmit={onSubmit}
                onCancel={onCancel}
                loading={loading}
                allowSpeech
                footer={(actionNode) => (
                    <Flex justify="space-between" align="center">
                        <Space size={8}>
                            <Tooltip title={t("chat.composer.attachmentUnavailable")}>
                                <Button
                                    type="text"
                                    aria-label={t("chat.composer.addAttachment")}
                                    icon={<PaperClipOutlined />}
                                    disabled
                                />
                            </Tooltip>
                            {versionLabel && (
                                <Typography.Text type="secondary">
                                    {versionLabel}
                                </Typography.Text>
                            )}
                        </Space>
                        {actionNode}
                    </Flex>
                )}
            />
        </div>
    );
}
