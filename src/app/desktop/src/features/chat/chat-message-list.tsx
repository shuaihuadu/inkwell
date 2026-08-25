import { SyncOutlined } from "@ant-design/icons";
import { Actions, Bubble, type BubbleItemType } from "@ant-design/x";
import { Button, Flex, Typography } from "antd";
import {
    useState,
    type ComponentRef,
    type Ref,
    type UIEventHandler,
} from "react";
import { useTranslation } from "react-i18next";
import { MarkdownContent } from "../../shared/components/markdown-content";
import type { ChatMessage, ChatRunError } from "../../shared/network/contracts";
import { useResolvedAppearance } from "../shell/appearance-store";
import { ChatTokenUsageSummary } from "./chat-token-usage";
import { SkillActivityChain } from "./skill-activity-chain";

type FeedbackValue = "default" | "like" | "dislike";

export type ChatMessageListRef = ComponentRef<typeof Bubble.List>;

interface ChatMessageListProps {
    messages: ChatMessage[];
    activeRequestId: string | null;
    error: ChatRunError | null;
    onRegenerate: (messageIndex: number) => void;
    onRetry: () => void;
    listRef?: Ref<ChatMessageListRef>;
    onScroll?: UIEventHandler<HTMLDivElement>;
}

export function ChatMessageList({
    messages,
    activeRequestId,
    error,
    onRegenerate,
    onRetry,
    listRef,
    onScroll,
}: ChatMessageListProps) {
    const { t } = useTranslation();
    const appearance = useResolvedAppearance();
    const [feedbackByMessage, setFeedbackByMessage] = useState<
        Record<string, FeedbackValue>
    >({});
    const items: BubbleItemType[] = messages.flatMap((item, index) => {
        const messageKey = `${item.role}-${index}`;
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
        const hasActiveSkillActivity = Boolean(
            item.skillActivities?.some(
                (activity) => activity.status === "loading",
            ),
        );
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
        const showUsage =
            Boolean(item.usage) &&
            item.runStatus !== "running" &&
            item.runStatus !== "stopped" &&
            item.runStatus !== "failed";
        const showActions = hasContent && !isLatestRunning;
        if (
            hasContent ||
            showError ||
            item.runStatus === "stopped" ||
            (isLatestRunning && !hasActiveSkillActivity)
        ) {
            assistantItems.push({
                key: messageKey,
                role: "assistant",
                classNames: { footer: "chat-message-footer" },
                content: item.content,
                loading:
                    !hasContent && isLatestRunning && !hasActiveSkillActivity,
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
                                {t("chat.messages.stopped")}
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
                                    aria-label={t("chat.messages.retryLabel")}
                                    onClick={onRetry}
                                >
                                    {t("common.retry")}
                                </Button>
                            </Flex>
                        )}
                    </div>
                ),
                footerPlacement: "outer-start",
                footer:
                    showUsage || showActions ? (
                        <Flex vertical gap={4}>
                            {showUsage && item.usage && (
                                <ChatTokenUsageSummary usage={item.usage} />
                            )}
                            {showActions && (
                                <Actions
                                    className="chat-message-actions"
                                    items={[
                                        {
                                            key: "regenerate",
                                            label: t(
                                                "chat.messages.regenerate",
                                            ),
                                            icon: <SyncOutlined />,
                                            onItemClick: () =>
                                                onRegenerate(index),
                                        },
                                        {
                                            key: "copy",
                                            actionRender: (
                                                <Actions.Copy
                                                    text={item.content}
                                                    aria-label={t(
                                                        "chat.messages.copyLabel",
                                                        { number: index + 1 },
                                                    )}
                                                />
                                            ),
                                        },
                                        {
                                            key: "feedback",
                                            actionRender: (
                                                <Actions.Feedback
                                                    aria-label={t(
                                                        "chat.messages.feedbackLabel",
                                                        { number: index + 1 },
                                                    )}
                                                    value={
                                                        feedbackByMessage[
                                                            messageKey
                                                        ] ?? "default"
                                                    }
                                                    onChange={(value) =>
                                                        setFeedbackByMessage(
                                                            (current) => ({
                                                                ...current,
                                                                [messageKey]:
                                                                    value,
                                                            }),
                                                        )
                                                    }
                                                />
                                            ),
                                        },
                                    ]}
                                />
                            )}
                        </Flex>
                    ) : undefined,
            });
        }

        return assistantItems;
    });

    return (
        <Bubble.List
            ref={listRef}
            className="chat-bubble-list"
            items={items}
            onScroll={onScroll}
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
