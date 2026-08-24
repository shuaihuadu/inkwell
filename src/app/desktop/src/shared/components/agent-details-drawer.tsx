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
import type { TFunction } from "i18next";
import { useTranslation } from "react-i18next";
import { MarkdownContent } from "./markdown-content";
import type { AgentToolDefinition, AgentVersion } from "../network/contracts";

interface AgentDetailsDrawerProps {
    appearance: "light" | "dark";
    open: boolean;
    version: AgentVersion | null;
    tools: AgentToolDefinition[];
    statusLabel: "published" | "historical" | null;
    extra?: ReactNode;
    footer?: ReactNode;
    onClose: () => void;
}

function displayValue(value: number | null | undefined, t: TFunction): string {
    return value === null || value === undefined
        ? t("agents.details.defaultValue")
        : String(value);
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
    const { t, i18n } = useTranslation();
    const { token } = theme.useToken();
    const [messageApi, messageContextHolder] = message.useMessage();
    const [expandedVersionId, setExpandedVersionId] = useState<string | null>(
        null,
    );
    const snapshot = version?.snapshot;
    const instructions =
        snapshot?.instructions || t("agents.editor.instructions.empty");
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
            void messageApi.success(t("agents.details.instructionsCopied"));
        } catch {
            void messageApi.error(t("agents.details.copyFailed"));
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
                title={t("agents.details.title")}
                width={600}
                extra={
                    <Space size={8}>
                        {extra}
                        <Tooltip title={t("common.close")}>
                            <Button
                                type="text"
                                aria-label={t("agents.details.closeLabel")}
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
                                                statusLabel === "published"
                                                    ? "success"
                                                    : "default"
                                            }
                                        >
                                            {statusLabel === "published"
                                                ? t(
                                                      "agents.editor.version.published",
                                                  )
                                                : t(
                                                      "agents.editor.version.historical",
                                                  )}
                                        </Tag>
                                    )}
                                    <Tag>
                                        {t("agents.details.version", {
                                            version: version.versionNumber,
                                        })}
                                    </Tag>
                                </Flex>
                                <Typography.Paragraph
                                    type="secondary"
                                    style={{ margin: "6px 0 0" }}
                                >
                                    {snapshot.description ||
                                        t("agents.details.noDescription")}
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
                                        ).toLocaleString(i18n.language)}
                                    </Typography.Text>
                                </Flex>
                            </div>
                        </div>

                        <div className="agent-details-content">
                            <section className="agent-details-section">
                                <SectionTitle icon={<AppstoreOutlined />}>
                                    {t("agents.details.overview")}
                                </SectionTitle>
                                <Descriptions
                                    size="small"
                                    column={1}
                                    items={[
                                        {
                                            key: "summary",
                                            label: t(
                                                "agents.details.changeSummary",
                                            ),
                                            children:
                                                version.changeSummary ||
                                                t("agents.details.noSummary"),
                                        },
                                        {
                                            key: "model",
                                            label: t(
                                                "agents.details.runtimeModel",
                                            ),
                                            children:
                                                model.modelId ||
                                                t(
                                                    "agents.details.notConfigured",
                                                ),
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
                                                {t(
                                                    "agents.details.characterCount",
                                                    {
                                                        count: instructions.length,
                                                    },
                                                )}
                                            </Typography.Text>
                                            <Tooltip
                                                title={t(
                                                    "agents.details.copyInstructions",
                                                )}
                                            >
                                                <Button
                                                    type="text"
                                                    size="small"
                                                    aria-label={t(
                                                        "agents.details.copyInstructions",
                                                    )}
                                                    icon={<CopyOutlined />}
                                                    onClick={() =>
                                                        void copyInstructions()
                                                    }
                                                />
                                            </Tooltip>
                                        </Space>
                                    }
                                >
                                    {t("agents.editor.sections.instructions")}
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
                                            ? t("agents.details.collapse")
                                            : t("agents.details.expand")}
                                    </Button>
                                )}
                            </section>

                            <section className="agent-details-section">
                                <SectionTitle icon={<SlidersOutlined />}>
                                    {t("agents.details.modelAndContext")}
                                </SectionTitle>
                                <Descriptions
                                    size="small"
                                    column={2}
                                    items={[
                                        {
                                            key: "temperature",
                                            label: t(
                                                "agents.editor.model.temperature",
                                            ),
                                            children: displayValue(
                                                model.temperature,
                                                t,
                                            ),
                                        },
                                        {
                                            key: "topP",
                                            label: t(
                                                "agents.editor.model.topP",
                                            ),
                                            children: displayValue(
                                                model.topP,
                                                t,
                                            ),
                                        },
                                        {
                                            key: "maxTokens",
                                            label: t(
                                                "agents.editor.model.maxTokens",
                                            ),
                                            children: displayValue(
                                                model.maxTokens,
                                                t,
                                            ),
                                        },
                                        {
                                            key: "maxMessages",
                                            label: t(
                                                "agents.details.historyLimit",
                                            ),
                                            children: displayValue(
                                                chatHistory?.maxMessages,
                                                t,
                                            ),
                                        },
                                    ]}
                                />
                            </section>

                            <section className="agent-details-section">
                                <SectionTitle icon={<ToolOutlined />}>
                                    {t("agents.details.tools")}
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
                                            {t("agents.details.noTools")}
                                        </Typography.Text>
                                    )}
                                </Flex>
                            </section>

                            <section className="agent-details-section">
                                <SectionTitle icon={<ReadOutlined />}>
                                    {t("agents.editor.sections.skills")}
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
                                            {t("agents.details.noSkills")}
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
