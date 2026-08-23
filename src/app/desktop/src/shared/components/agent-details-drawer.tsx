import {
    AppstoreOutlined,
    CalendarOutlined,
    CloseOutlined,
    CodeOutlined,
    CopyOutlined,
    DownOutlined,
    ReadOutlined,
    RobotOutlined,
    SlidersOutlined,
    ToolOutlined,
    UpOutlined,
    UserOutlined,
} from "@ant-design/icons";
import {
    Avatar,
    Button,
    Descriptions,
    Drawer,
    Flex,
    Space,
    Tag,
    Tooltip,
    Typography,
    message,
    theme,
} from "antd";
import { useState, type ReactNode } from "react";
import { MarkdownContent } from "./markdown-content";
import type { AgentToolDefinition, AgentVersion } from "../network/contracts";

interface AgentDetailsDrawerProps {
    appearance: "light" | "dark";
    open: boolean;
    version: AgentVersion | null;
    tools: AgentToolDefinition[];
    statusLabel: "已发布" | "历史版本" | null;
    extra?: ReactNode;
    footer?: ReactNode;
    onClose: () => void;
}

function displayValue(value: number | null | undefined): string {
    return value === null || value === undefined ? "默认" : String(value);
}

function SectionTitle({
    icon,
    children,
    extra,
}: {
    icon: ReactNode;
    children: ReactNode;
    extra?: ReactNode;
}) {
    return (
        <Flex
            align="center"
            justify="space-between"
            gap={12}
            className="agent-details-section-title"
        >
            <Space size={8}>
                {icon}
                <Typography.Text strong>{children}</Typography.Text>
            </Space>
            {extra}
        </Flex>
    );
}

export function AgentDetailsDrawer({
    appearance,
    open,
    version,
    tools,
    statusLabel,
    extra,
    footer,
    onClose,
}: AgentDetailsDrawerProps) {
    const { token } = theme.useToken();
    const [messageApi, messageContextHolder] = message.useMessage();
    const [expandedVersionId, setExpandedVersionId] = useState<string | null>(
        null,
    );
    const snapshot = version?.snapshot;
    const instructions = snapshot?.instructions || "未配置 Instructions";
    const hasLongInstructions =
        instructions.length > 180 || instructions.split("\n").length > 8;
    const instructionsExpanded = expandedVersionId === version?.id;

    const closeDrawer = (): void => {
        setExpandedVersionId(null);
        onClose();
    };

    const copyInstructions = async (): Promise<void> => {
        try {
            await navigator.clipboard.writeText(instructions);
            void messageApi.success("Instructions 已复制");
        } catch {
            void messageApi.error("复制失败，请重试");
        }
    };

    const model = snapshot?.buildOptions.modelOptions;
    const chatHistory = snapshot?.buildOptions.chatHistoryOptions;
    const toolBindings = snapshot?.buildOptions.toolBindings ?? [];
    const skills = snapshot?.buildOptions.skills ?? [];
    const toolNameById = new Map(tools.map((tool) => [tool.id, tool.name]));

    return (
        <>
            {messageContextHolder}
            <Drawer
                open={open}
                onClose={closeDrawer}
                closable={false}
                title="Agent 详情"
                width={600}
                extra={
                    <Space size={8}>
                        {extra}
                        <Tooltip title="关闭">
                            <Button
                                type="text"
                                aria-label="关闭 Agent 详情"
                                icon={<CloseOutlined />}
                                onClick={closeDrawer}
                            />
                        </Tooltip>
                    </Space>
                }
                footer={footer}
                className="agent-details-drawer"
                styles={{ body: { padding: 0 } }}
            >
                {version && snapshot && model ? (
                    <>
                        <div
                            className="agent-details-identity"
                            style={{
                                background: token.colorFillQuaternary,
                                borderBottom: `1px solid ${token.colorBorderSecondary}`,
                            }}
                        >
                            <Avatar
                                size={52}
                                src={snapshot.avatarUri ?? undefined}
                                icon={
                                    snapshot.avatarUri ? undefined : (
                                        <RobotOutlined />
                                    )
                                }
                                style={{ background: token.colorPrimary }}
                            />
                            <div className="agent-details-identity-copy">
                                <Flex align="center" gap={8} wrap>
                                    <Typography.Title
                                        level={4}
                                        style={{ margin: 0 }}
                                    >
                                        {snapshot.name}
                                    </Typography.Title>
                                    {statusLabel && (
                                        <Tag
                                            color={
                                                statusLabel === "已发布"
                                                    ? "success"
                                                    : "default"
                                            }
                                        >
                                            {statusLabel}
                                        </Tag>
                                    )}
                                    <Tag>版本：v{version.versionNumber}</Tag>
                                </Flex>
                                <Typography.Paragraph
                                    type="secondary"
                                    style={{ margin: "6px 0 0" }}
                                >
                                    {snapshot.description || "暂无描述"}
                                </Typography.Paragraph>
                                <Flex gap={16} wrap style={{ marginTop: 8 }}>
                                    <Typography.Text type="secondary">
                                        <UserOutlined />{" "}
                                        {version.ownerUserName ??
                                            version.ownerUserId}
                                    </Typography.Text>
                                    <Typography.Text type="secondary">
                                        <CalendarOutlined />{" "}
                                        {new Date(
                                            version.createdTime,
                                        ).toLocaleString("zh-CN")}
                                    </Typography.Text>
                                </Flex>
                            </div>
                        </div>

                        <div className="agent-details-content">
                            <section className="agent-details-section">
                                <SectionTitle icon={<AppstoreOutlined />}>
                                    版本概览
                                </SectionTitle>
                                <Descriptions
                                    size="small"
                                    column={1}
                                    items={[
                                        {
                                            key: "summary",
                                            label: "变更摘要",
                                            children:
                                                version.changeSummary || "无",
                                        },
                                        {
                                            key: "model",
                                            label: "运行模型",
                                            children: model.modelId || "未配置",
                                        },
                                    ]}
                                />
                            </section>

                            <section className="agent-details-section">
                                <SectionTitle
                                    icon={<CodeOutlined />}
                                    extra={
                                        <Space size={4}>
                                            <Typography.Text
                                                type="secondary"
                                                className="agent-details-instructions-count"
                                            >
                                                {instructions.length} 字符
                                            </Typography.Text>
                                            <Tooltip title="复制 Instructions">
                                                <Button
                                                    type="text"
                                                    size="small"
                                                    aria-label="复制 Instructions"
                                                    icon={<CopyOutlined />}
                                                    onClick={() =>
                                                        void copyInstructions()
                                                    }
                                                />
                                            </Tooltip>
                                        </Space>
                                    }
                                >
                                    Instructions
                                </SectionTitle>
                                <div
                                    className={`agent-details-instructions ${
                                        instructionsExpanded
                                            ? "expanded"
                                            : "collapsed"
                                    }`}
                                    style={{
                                        background: token.colorFillQuaternary,
                                        border: `1px solid ${token.colorBorderSecondary}`,
                                    }}
                                >
                                    <MarkdownContent
                                        appearance={appearance}
                                        content={instructions}
                                    />
                                </div>
                                {hasLongInstructions && (
                                    <Button
                                        type="link"
                                        size="small"
                                        className="agent-details-instructions-toggle"
                                        icon={
                                            instructionsExpanded ? (
                                                <UpOutlined />
                                            ) : (
                                                <DownOutlined />
                                            )
                                        }
                                        onClick={() =>
                                            setExpandedVersionId(
                                                instructionsExpanded
                                                    ? null
                                                    : version.id,
                                            )
                                        }
                                    >
                                        {instructionsExpanded
                                            ? "收起"
                                            : "展开全文"}
                                    </Button>
                                )}
                            </section>

                            <section className="agent-details-section">
                                <SectionTitle icon={<SlidersOutlined />}>
                                    模型与上下文
                                </SectionTitle>
                                <Descriptions
                                    size="small"
                                    column={2}
                                    items={[
                                        {
                                            key: "temperature",
                                            label: "Temperature",
                                            children: displayValue(
                                                model.temperature,
                                            ),
                                        },
                                        {
                                            key: "topP",
                                            label: "Top P",
                                            children: displayValue(model.topP),
                                        },
                                        {
                                            key: "maxTokens",
                                            label: "Max Tokens",
                                            children: displayValue(
                                                model.maxTokens,
                                            ),
                                        },
                                        {
                                            key: "maxMessages",
                                            label: "历史消息上限",
                                            children: displayValue(
                                                chatHistory?.maxMessages,
                                            ),
                                        },
                                    ]}
                                />
                            </section>

                            <section className="agent-details-section">
                                <SectionTitle icon={<ToolOutlined />}>
                                    工具
                                </SectionTitle>
                                <Flex gap={8} wrap>
                                    {toolBindings.length > 0 ? (
                                        toolBindings.map((binding) => (
                                            <Tag
                                                key={binding.toolId}
                                                icon={<ToolOutlined />}
                                            >
                                                {toolNameById.get(
                                                    binding.toolId,
                                                ) ?? binding.toolId.slice(0, 8)}
                                            </Tag>
                                        ))
                                    ) : (
                                        <Typography.Text type="secondary">
                                            未挂载工具
                                        </Typography.Text>
                                    )}
                                </Flex>
                            </section>

                            <section className="agent-details-section">
                                <SectionTitle icon={<ReadOutlined />}>
                                    Skills
                                </SectionTitle>
                                <Flex gap={8} wrap>
                                    {skills.length > 0 ? (
                                        skills.map((skill) => (
                                            <Tag
                                                key={skill.id}
                                                icon={<ReadOutlined />}
                                            >
                                                {skill.name}
                                            </Tag>
                                        ))
                                    ) : (
                                        <Typography.Text type="secondary">
                                            未挂载 Skill
                                        </Typography.Text>
                                    )}
                                </Flex>
                            </section>
                        </div>
                    </>
                ) : null}
            </Drawer>
        </>
    );
}
