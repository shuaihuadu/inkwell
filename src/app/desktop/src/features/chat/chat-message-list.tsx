import { SyncOutlined } from "@ant-design/icons";
import { Actions, Bubble, type BubbleItemType } from "@ant-design/x";
import { Button, Flex, Typography } from "antd";
import { useState } from "react";
import { MarkdownContent } from "../../shared/components/markdown-content";
import type { ChatMessage, ChatRunError } from "../../shared/network/contracts";
import { useResolvedAppearance } from "../shell/appearance-store";
import { SkillActivityChain } from "./skill-activity-chain";

type FeedbackValue = "default" | "like" | "dislike";

interface ChatMessageListProps {
    messages: ChatMessage[];
    activeRequestId: string | null;
    error: ChatRunError | null;
    onRegenerate: (messageIndex: number) => void;
    onRetry: () => void;
}

export function ChatMessageList({
    messages,
    activeRequestId,
    error,
    onRegenerate,
    onRetry,
}: ChatMessageListProps) {
    const appearance = useResolvedAppearance();
    const [feedbackByMessage, setFeedbackByMessage] = useState<
        Record<string, FeedbackValue>
    >({});
    const items: BubbleItemType[] = messages.flatMap((item, index) => {
        const messageKey = item.id ?? `${item.role}-${index}`;
        const isLatestRunning =
            Boolean(activeRequestId) && index === messages.length - 1;
        const hasContent = item.content.trim().length > 0;

        if (item.role === "user") {
            if (!hasContent) return [];
            return [
                {
                    key: messageKey,
                    role: "user",
                    content: item.content,
                    contentRender: (content) => (
                        <MarkdownContent
                            appearance={appearance}
                            content={String(content)}
                        />
                    ),
                },
            ];
        }

        const assistantItems: BubbleItemType[] = [];
        const hasSkillActivities = Boolean(item.skillActivities?.length);
        if (hasSkillActivities) {
            assistantItems.push({
                key: `${messageKey}-skills`,
                role: "activity",
                content: "",
                contentRender: () => (
                    <SkillActivityChain activities={item.skillActivities!} />
                ),
            });
        }

        const showError = Boolean(error) && index === messages.length - 1;
        if (
            hasContent ||
            showError ||
            item.runStatus === "stopped" ||
            (!hasSkillActivities && isLatestRunning)
        ) {
            assistantItems.push({
                key: messageKey,
                role: "assistant",
                content: item.content,
                loading: !hasContent && !hasSkillActivities && isLatestRunning,
                contentRender: (content) => (
                    <div className="chat-message-content">
                        {String(content) && (
                            <MarkdownContent
                                appearance={appearance}
                                content={String(content)}
                                streaming={isLatestRunning}
                            />
                        )}
                        {item.runStatus === "stopped" && (
                            <Typography.Text
                                className="chat-run-status"
                                type="secondary"
                            >
                                已停止生成
                            </Typography.Text>
                        )}
                        {showError && error && (
                            <Flex vertical gap={4}>
                                <Typography.Text type="danger" strong>
                                    {error.code}
                                </Typography.Text>
                                <Typography.Text>
                                    {error.reason}
                                </Typography.Text>
                                <Button
                                    className="chat-retry-button"
                                    size="small"
                                    aria-label="重试失败消息"
                                    onClick={onRetry}
                                >
                                    重试
                                </Button>
                            </Flex>
                        )}
                    </div>
                ),
                footer:
                    hasContent && !isLatestRunning ? (
                        <Actions
                            className="chat-message-actions"
                            items={[
                                {
                                    key: "regenerate",
                                    label: "重新生成",
                                    icon: <SyncOutlined />,
                                    onItemClick: () => onRegenerate(index),
                                },
                                {
                                    key: "copy",
                                    actionRender: (
                                        <Actions.Copy
                                            text={item.content}
                                            aria-label={`复制第 ${index + 1} 条消息`}
                                        />
                                    ),
                                },
                                {
                                    key: "feedback",
                                    actionRender: (
                                        <Actions.Feedback
                                            aria-label={`评价第 ${index + 1} 条消息`}
                                            value={
                                                feedbackByMessage[messageKey] ??
                                                "default"
                                            }
                                            onChange={(value) =>
                                                setFeedbackByMessage(
                                                    (current) => ({
                                                        ...current,
                                                        [messageKey]: value,
                                                    }),
                                                )
                                            }
                                        />
                                    ),
                                },
                            ]}
                        />
                    ) : undefined,
            });
        }

        return assistantItems;
    });

    return (
        <Bubble.List
            className="chat-bubble-list"
            items={items}
            role={{
                user: {
                    placement: "end",
                    variant: "filled",
                },
                assistant: {
                    placement: "start",
                    variant: "outlined",
                },
                activity: {
                    placement: "start",
                    variant: "borderless",
                    avatar: false,
                },
            }}
        />
    );
}
