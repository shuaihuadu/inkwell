import { useEffect, useState, type ReactNode } from "react";
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
    message,
    Space,
    Tag,
    Tooltip,
    Typography,
    theme as antdTheme,
} from "antd";

export interface AgentDetailsSnapshot {
    name: string;
    description: string;
    instructions: string;
    model: string;
    temperature: number;
    topP?: number;
    maxTokens?: number;
    maxMessages?: number;
    tools: string[];
    skills: string[];
}

export interface AgentDetailsVersion {
    version: string;
    status: string;
    savedAt: string;
    savedBy: string;
    summary: string;
    snapshot: AgentDetailsSnapshot;
}

interface AgentDetailsDrawerProps {
    open: boolean;
    version: AgentDetailsVersion;
    hidePublishedStatus?: boolean;
    extra?: ReactNode;
    footer?: ReactNode;
    onClose: () => void;
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
            className="inkwell-agent-details-section-title"
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
    open,
    version,
    hidePublishedStatus = false,
    extra,
    footer,
    onClose,
}: AgentDetailsDrawerProps) {
    const { token } = antdTheme.useToken();
    const { snapshot } = version;
    const [instructionsExpanded, setInstructionsExpanded] = useState(false);
    const instructions = snapshot.instructions || "未配置 Instructions";
    const hasLongInstructions =
        instructions.length > 180 || instructions.split("\n").length > 8;

    useEffect(() => {
        setInstructionsExpanded(false);
    }, [open, version.version]);

    const copyInstructions = async () => {
        try {
            await navigator.clipboard.writeText(instructions);
            void message.success("Instructions 已复制");
        } catch {
            void message.error("复制失败，请重试");
        }
    };

    return (
        <Drawer
            open={open}
            onClose={onClose}
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
                            onClick={onClose}
                        />
                    </Tooltip>
                </Space>
            }
            footer={footer}
            className="inkwell-agent-details-drawer"
            styles={{ body: { padding: 0 } }}
        >
            <div
                className="inkwell-agent-details-identity"
                style={{
                    background: token.colorFillQuaternary,
                    borderBottom: `1px solid ${token.colorBorderSecondary}`,
                }}
            >
                <Avatar
                    size={52}
                    icon={<RobotOutlined />}
                    style={{ background: token.colorPrimary }}
                />
                <div className="inkwell-agent-details-identity-copy">
                    <Flex align="center" gap={8} wrap>
                        <Typography.Title level={4} style={{ margin: 0 }}>
                            {snapshot.name}
                        </Typography.Title>
                        {(!hidePublishedStatus || version.status !== "已发布") && (
                            <Tag
                                color={
                                    version.status === "已发布"
                                        ? "success"
                                        : "default"
                                }
                            >
                                {version.status}
                            </Tag>
                        )}
                        <Tag>版本：{version.version}</Tag>
                    </Flex>
                    <Typography.Paragraph
                        type="secondary"
                        style={{ margin: "6px 0 0" }}
                    >
                        {snapshot.description || "暂无描述"}
                    </Typography.Paragraph>
                    <Flex gap={16} wrap style={{ marginTop: 8 }}>
                        <Typography.Text type="secondary">
                            <UserOutlined /> {version.savedBy}
                        </Typography.Text>
                        <Typography.Text type="secondary">
                            <CalendarOutlined /> {version.savedAt}
                        </Typography.Text>
                    </Flex>
                </div>
            </div>

            <div className="inkwell-agent-details-content">
                <section className="inkwell-agent-details-section">
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
                                children: version.summary || "无",
                            },
                            {
                                key: "model",
                                label: "运行模型",
                                children: snapshot.model || "未配置",
                            },
                        ]}
                    />
                </section>

                <section className="inkwell-agent-details-section">
                    <SectionTitle
                        icon={<CodeOutlined />}
                        extra={
                            <Space size={4}>
                                <Typography.Text
                                    type="secondary"
                                    className="inkwell-agent-details-instructions-count"
                                >
                                    {instructions.length} 字符
                                </Typography.Text>
                                <Tooltip title="复制 Instructions">
                                    <Button
                                        type="text"
                                        size="small"
                                        aria-label="复制 Instructions"
                                        icon={<CopyOutlined />}
                                        onClick={() => void copyInstructions()}
                                    />
                                </Tooltip>
                            </Space>
                        }
                    >
                        Instructions
                    </SectionTitle>
                    <div
                        className={`inkwell-agent-details-instructions ${
                            instructionsExpanded ? "expanded" : "collapsed"
                        }`}
                        style={{
                            background: token.colorFillQuaternary,
                            border: `1px solid ${token.colorBorderSecondary}`,
                        }}
                    >
                        <Typography.Paragraph style={{ margin: 0 }}>
                            {instructions}
                        </Typography.Paragraph>
                    </div>
                    {hasLongInstructions && (
                        <Button
                            type="link"
                            size="small"
                            className="inkwell-agent-details-instructions-toggle"
                            icon={
                                instructionsExpanded ? (
                                    <UpOutlined />
                                ) : (
                                    <DownOutlined />
                                )
                            }
                            onClick={() =>
                                setInstructionsExpanded((current) => !current)
                            }
                        >
                            {instructionsExpanded ? "收起" : "展开全文"}
                        </Button>
                    )}
                </section>

                <section className="inkwell-agent-details-section">
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
                                children: snapshot.temperature,
                            },
                            {
                                key: "topP",
                                label: "Top P",
                                children: snapshot.topP ?? "默认",
                            },
                            {
                                key: "maxTokens",
                                label: "Max Tokens",
                                children: snapshot.maxTokens ?? "默认",
                            },
                            {
                                key: "maxMessages",
                                label: "历史消息上限",
                                children: snapshot.maxMessages ?? "默认",
                            },
                        ]}
                    />
                </section>

                <section className="inkwell-agent-details-section">
                    <SectionTitle icon={<ToolOutlined />}>工具</SectionTitle>
                    <Flex gap={8} wrap>
                        {snapshot.tools.length > 0 ? (
                            snapshot.tools.map((tool) => (
                                <Tag key={tool} icon={<ToolOutlined />}>
                                    {tool}
                                </Tag>
                            ))
                        ) : (
                            <Typography.Text type="secondary">
                                未挂载工具
                            </Typography.Text>
                        )}
                    </Flex>
                </section>

                <section className="inkwell-agent-details-section">
                    <SectionTitle icon={<ReadOutlined />}>Skills</SectionTitle>
                    <Flex gap={8} wrap>
                        {snapshot.skills.length > 0 ? (
                            snapshot.skills.map((skill) => (
                                <Tag key={skill} icon={<ReadOutlined />}>
                                    {skill}
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
        </Drawer>
    );
}
