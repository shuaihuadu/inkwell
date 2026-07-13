import {
    ArrowUpOutlined,
    RobotOutlined,
    UserOutlined,
} from "@ant-design/icons";
import { Avatar, Button, Empty, Input, message, Typography } from "antd";
import { useEffect, useState } from "react";
import { desktopApi } from "../../shared/network/desktop-api";
import type { AgentSummary, ChatMessage } from "../../shared/network/contracts";

interface ChatPanelProps {
    agent: AgentSummary | null;
}

export function ChatPanel({ agent }: ChatPanelProps) {
    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [draft, setDraft] = useState("");
    const [activeRequestId, setActiveRequestId] = useState<string | null>(null);
    const [messageApi, contextHolder] = message.useMessage();

    useEffect(
        () =>
            desktopApi.onChatDelta((requestId, content) => {
                if (requestId !== activeRequestId) return;
                setMessages((current) =>
                    current.map((item, index) =>
                        index === current.length - 1
                            ? { ...item, content: item.content + content }
                            : item,
                    ),
                );
            }),
        [activeRequestId],
    );

    const send = async (): Promise<void> => {
        const content = draft.trim();
        if (!agent || !content || activeRequestId) return;
        const requestId = crypto.randomUUID();
        const history: ChatMessage[] = [...messages, { role: "user", content }];
        setMessages([...history, { role: "assistant", content: "" }]);
        setDraft("");
        setActiveRequestId(requestId);
        try {
            await desktopApi.chat({
                requestId,
                agentId: agent.id,
                messages: history,
            });
        } catch (reason) {
            messageApi.error(
                reason instanceof Error ? reason.message : "Agent 调用失败。",
            );
            setMessages((current) =>
                current.map((item, index) =>
                    index === current.length - 1 && !item.content
                        ? {
                              ...item,
                              content: "暂时无法生成回复，请检查模型服务配置。",
                          }
                        : item,
                ),
            );
        } finally {
            setActiveRequestId(null);
        }
    };

    if (!agent)
        return (
            <section className="chat-panel chat-empty">
                <Empty description="选择一个 Agent 开始对话" />
            </section>
        );

    return (
        <section className="chat-panel">
            {contextHolder}
            <header className="chat-header">
                <Avatar className="agent-avatar" icon={<RobotOutlined />} />
                <div>
                    <Typography.Title level={4}>{agent.name}</Typography.Title>
                    <Typography.Text type="secondary">
                        已发布 v{agent.latestPublishedVersionNumber}
                    </Typography.Text>
                </div>
            </header>
            <div className="message-list">
                {messages.length === 0 ? (
                    <div className="conversation-starter">
                        <RobotOutlined />
                        <Typography.Title level={4}>
                            开始新的对话
                        </Typography.Title>
                        <Typography.Text type="secondary">
                            消息将由当前 Agent 的已发布版本处理。
                        </Typography.Text>
                    </div>
                ) : (
                    messages.map((item, index) => (
                        <article
                            className={`message-row ${item.role}`}
                            key={`${item.role}-${index}`}
                        >
                            <Avatar
                                icon={
                                    item.role === "user" ? (
                                        <UserOutlined />
                                    ) : (
                                        <RobotOutlined />
                                    )
                                }
                            />
                            <div className="message-bubble">
                                {item.content || (
                                    <span className="typing">正在生成</span>
                                )}
                            </div>
                        </article>
                    ))
                )}
            </div>
            <div className="composer">
                <Input.TextArea
                    autoSize={{ minRows: 2, maxRows: 5 }}
                    placeholder={`给 ${agent.name} 发送消息`}
                    value={draft}
                    onChange={(event) => setDraft(event.target.value)}
                    onPressEnter={(event) => {
                        if (!event.shiftKey) {
                            event.preventDefault();
                            void send();
                        }
                    }}
                />
                <Button
                    type="primary"
                    shape="circle"
                    icon={<ArrowUpOutlined />}
                    aria-label="发送消息"
                    loading={Boolean(activeRequestId)}
                    disabled={!draft.trim()}
                    onClick={() => void send()}
                />
            </div>
        </section>
    );
}
