import {
    ArrowLeftOutlined,
    CameraOutlined,
    CloudUploadOutlined,
    CopyOutlined,
    DeleteOutlined,
    DiffOutlined,
    HistoryOutlined,
    MenuFoldOutlined,
    MenuUnfoldOutlined,
    PlayCircleOutlined,
    QuestionCircleOutlined,
    ReadOutlined,
    RobotOutlined,
    RollbackOutlined,
    SaveOutlined,
    SlidersOutlined,
    ToolOutlined,
    UserOutlined,
} from "@ant-design/icons";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
    Alert,
    Avatar,
    Button,
    Card,
    Checkbox,
    Empty,
    Flex,
    Form,
    Input,
    InputNumber,
    Modal,
    Select,
    Skeleton,
    Slider,
    Space,
    Switch,
    Table,
    Tag,
    Tooltip,
    Typography,
    Upload,
    message,
    theme,
} from "antd";
import { Fragment, useEffect, useState } from "react";
import type { TFunction } from "i18next";
import { useTranslation } from "react-i18next";
import { AgentDetailsDrawer } from "../../shared/components/agent-details-drawer";
import { MarkdownContent } from "../../shared/components/markdown-content";
import { MarkdownEditor } from "../../shared/components/markdown-editor";
import { desktopApi } from "../../shared/network/desktop-api";
import type {
    AgentDefinition,
    AgentUpsertRequest,
    AgentVersion,
} from "../../shared/network/contracts";
import { useAuthStore } from "../auth/auth-store";
import { ChatPanel } from "../chat/chat-panel";
import { useResolvedAppearance } from "../shell/appearance-store";

type AgentEditorSection =
    | "basic"
    | "instructions"
    | "model"
    | "tools"
    | "skills"
    | "version";

interface AgentEditorProps {
    agentId: string | null;
    onBack: () => void;
    onClone: (agentId: string) => void;
    onDirtyChange: (dirty: boolean) => void;
}

interface AgentFormValues {
    name: string;
    description: string;
    instructions: string;
    isShared: boolean;
    modelId: string;
    customTemperature: boolean;
    temperature: number;
    customTopP: boolean;
    topP: number;
    customMaxTokens: boolean;
    maxTokens: number;
    maxMessages: number;
    toolIds: string[];
    toolParameters: Record<string, Record<string, string>>;
    skillIds: string[];
}

const Sections: Array<{
    key: AgentEditorSection;
    translationKey: AgentEditorSection;
    icon: React.ReactNode;
}> = [
    { key: "basic", translationKey: "basic", icon: <UserOutlined /> },
    {
        key: "instructions",
        translationKey: "instructions",
        icon: <RobotOutlined />,
    },
    { key: "model", translationKey: "model", icon: <SlidersOutlined /> },
    { key: "tools", translationKey: "tools", icon: <ToolOutlined /> },
    { key: "skills", translationKey: "skills", icon: <ReadOutlined /> },
    { key: "version", translationKey: "version", icon: <HistoryOutlined /> },
];

const InitialValues: AgentFormValues = {
    name: "",
    description: "",
    instructions: "",
    isShared: false,
    modelId: "",
    customTemperature: true,
    temperature: 0.7,
    customTopP: false,
    topP: 1,
    customMaxTokens: false,
    maxTokens: 4096,
    maxMessages: 40,
    toolIds: [],
    toolParameters: {},
    skillIds: [],
};

export function AgentEditor({
    agentId,
    onBack,
    onClone,
    onDirtyChange,
}: AgentEditorProps) {
    const { t } = useTranslation();
    const identity = useAuthStore((state) => state.identity);
    const queryClient = useQueryClient();
    const [form] = Form.useForm<AgentFormValues>();
    const agentName = Form.useWatch("name", form);
    const [section, setSection] = useState<AgentEditorSection>("basic");
    const [savedAgent, setSavedAgent] = useState<AgentDefinition | null>(null);
    const [dirty, setDirty] = useState(false);
    const [savedSincePublish, setSavedSincePublish] = useState(false);
    const [publishOpen, setPublishOpen] = useState(false);
    const [changeSummary, setChangeSummary] = useState("");
    const [shareAfterPublish, setShareAfterPublish] = useState(false);
    const [avatarUri, setAvatarUri] = useState<string | null>(null);
    const [sectionsCollapsed, setSectionsCollapsed] = useState(false);
    const [trialOpen, setTrialOpen] = useState(false);
    const [messageApi, messageContextHolder] = message.useMessage();
    const agentQuery = useQuery({
        queryKey: ["agent", agentId],
        queryFn: () => desktopApi.getAgent(agentId!),
        enabled: agentId !== null,
    });
    const modelsQuery = useQuery({
        queryKey: ["models"],
        queryFn: desktopApi.listModels,
    });
    const toolsQuery = useQuery({
        queryKey: ["tools"],
        queryFn: desktopApi.listTools,
    });
    const skillsQuery = useQuery({
        queryKey: ["skills"],
        queryFn: desktopApi.listSkills,
    });
    const versionAgentId = savedAgent?.id ?? agentId;
    const versionsQuery = useQuery({
        queryKey: ["agent-versions", versionAgentId],
        queryFn: () => desktopApi.listAgentVersions(versionAgentId!),
        enabled: versionAgentId !== null && section === "version",
    });
    const saveMutation = useMutation({
        mutationFn: (values: AgentFormValues) =>
            persistAgent(
                values,
                currentAgent,
                avatarUri ?? currentAgent?.avatarUri ?? null,
            ),
        onSuccess: async (saved) => {
            setSavedAgent(saved);
            setDirty(false);
            setSavedSincePublish(true);
            form.setFieldsValue(toFormValues(saved));
            await queryClient.invalidateQueries({ queryKey: ["agents"] });
            messageApi.success(t("agents.editor.messages.saved"));
        },
        onError: (reason) =>
            messageApi.error(
                reason instanceof Error
                    ? reason.message
                    : t("agents.editor.messages.saveFailed"),
            ),
    });
    const avatarMutation = useMutation({
        mutationFn: async (file: File) =>
            desktopApi.uploadAgentAvatar({
                name: file.name,
                contentType: file.type,
                bytes: new Uint8Array(await file.arrayBuffer()),
            }),
        onSuccess: (response) => {
            setAvatarUri(response.avatarUri);
            setDirty(true);
            messageApi.success(t("agents.editor.messages.avatarUploaded"));
        },
        onError: (reason) =>
            messageApi.error(
                reason instanceof Error
                    ? reason.message
                    : t("agents.editor.messages.avatarUploadFailed"),
            ),
    });
    const publishMutation = useMutation({
        mutationFn: async () => {
            const values = await form.validateFields();
            const publishValues = shareAfterPublish
                ? { ...values, isShared: true }
                : values;
            const saved = await persistAgent(
                publishValues,
                currentAgent,
                avatarUri ?? currentAgent?.avatarUri ?? null,
            );
            const version = await desktopApi.publishAgent(
                saved.id,
                changeSummary.trim() || null,
            );
            return { saved, version };
        },
        onSuccess: async ({ saved, version }) => {
            const published = {
                ...saved,
                currentPublishedVersionId: version.id,
                latestPublishedVersionNumber: version.versionNumber,
            };
            setSavedAgent(published);
            setDirty(false);
            setSavedSincePublish(false);
            setPublishOpen(false);
            setChangeSummary("");
            setShareAfterPublish(false);
            form.setFieldValue("isShared", saved.isShared);
            await queryClient.invalidateQueries({ queryKey: ["agents"] });
            await queryClient.invalidateQueries({
                queryKey: ["agent-versions", saved.id],
            });
            messageApi.success(
                t("agents.editor.messages.published", {
                    version: version.versionNumber,
                }),
            );
        },
        onError: (reason) =>
            messageApi.error(
                reason instanceof Error
                    ? reason.message
                    : t("agents.editor.messages.publishFailed"),
            ),
    });
    const cloneMutation = useMutation({
        mutationFn: (sourceAgentId: string) =>
            desktopApi.cloneAgent(sourceAgentId),
        onSuccess: async (clone) => {
            await queryClient.invalidateQueries({ queryKey: ["agents"] });
            messageApi.success(t("agents.editor.messages.cloned"));
            onClone(clone.id);
        },
        onError: (reason) =>
            messageApi.error(
                reason instanceof Error
                    ? reason.message
                    : t("agents.editor.messages.cloneFailed"),
            ),
    });
    const rollbackMutation = useMutation({
        mutationFn: ({
            versionId,
            changeSummary: rollbackChangeSummary,
        }: {
            versionId: string;
            changeSummary: string | null;
        }) =>
            desktopApi.rollbackAgentVersion(
                currentAgent!.id,
                versionId,
                rollbackChangeSummary,
            ),
        onSuccess: async (version) => {
            setSavedAgent((previous) =>
                previous
                    ? {
                          ...previous,
                          currentPublishedVersionId: version.id,
                          latestPublishedVersionNumber: version.versionNumber,
                      }
                    : previous,
            );
            await queryClient.invalidateQueries({ queryKey: ["agents"] });
            await queryClient.invalidateQueries({
                queryKey: ["agent-versions", versionAgentId],
            });
            messageApi.success(
                t("agents.editor.messages.rolledBack", {
                    version: version.versionNumber,
                }),
            );
        },
        onError: (reason) =>
            messageApi.error(
                reason instanceof Error
                    ? reason.message
                    : t("agents.editor.messages.rollbackFailed"),
            ),
    });
    const deleteMutation = useMutation({
        mutationFn: (targetAgentId: string) =>
            desktopApi.deleteAgent(targetAgentId),
        onSuccess: async () => {
            await queryClient.invalidateQueries({ queryKey: ["agents"] });
            messageApi.success(t("agents.editor.messages.deleted"));
            onBack();
        },
        onError: (reason) =>
            messageApi.error(
                reason instanceof Error
                    ? reason.message
                    : t("agents.editor.messages.deleteFailed"),
            ),
    });

    useEffect(() => {
        if (!agentQuery.data) return;
        form.setFieldsValue(toFormValues(agentQuery.data));
    }, [agentQuery.data, form]);

    useEffect(() => {
        onDirtyChange(dirty);
    }, [dirty, onDirtyChange]);

    useEffect(() => {
        if (agentId !== null || form.getFieldValue("modelId")) return;
        const firstChatModel = modelsQuery.data?.find(
            (model) => model.category === "Chat",
        );
        if (firstChatModel) form.setFieldValue("modelId", firstChatModel.id);
    }, [agentId, form, modelsQuery.data]);

    const currentAgent = savedAgent ?? agentQuery.data ?? null;
    const effectiveAvatarUri = avatarUri ?? currentAgent?.avatarUri ?? null;
    const isNew = currentAgent === null && agentId === null;
    const isOwner = isNew || currentAgent?.ownerUserId === identity?.userId;
    const busy =
        saveMutation.isPending ||
        publishMutation.isPending ||
        avatarMutation.isPending ||
        cloneMutation.isPending ||
        rollbackMutation.isPending ||
        deleteMutation.isPending;
    const sections = Sections.map((item) => ({
        ...item,
        label: t(`agents.editor.sections.${item.translationKey}`),
    }));
    const selectedSection = sections.find((item) => item.key === section)!;
    const chatModels = (modelsQuery.data ?? []).filter(
        (model) => model.category === "Chat",
    );

    const save = async (): Promise<AgentDefinition | null> => {
        try {
            const values = await form.validateFields();
            return await saveMutation.mutateAsync(values);
        } catch {
            return null;
        }
    };
    const openTrial = async (): Promise<void> => {
        if (isOwner && (!currentAgent || dirty)) {
            const saved = await save();
            if (!saved) return;
        }

        setTrialOpen(true);
        setSectionsCollapsed(true);
    };
    const confirmDelete = (): void => {
        if (!currentAgent) return;
        Modal.confirm({
            title: t("agents.editor.deleteDialog.title", {
                name: currentAgent.name,
            }),
            content: t("agents.editor.deleteDialog.content"),
            okText: t("agents.editor.deleteDialog.confirm"),
            okButtonProps: { danger: true },
            cancelText: t("common.cancel"),
            onOk: async () => deleteMutation.mutateAsync(currentAgent.id),
        });
    };

    if (agentId && agentQuery.isLoading) {
        return (
            <main className="agent-editor-loading">
                <Skeleton active />
            </main>
        );
    }
    if (agentId && agentQuery.isError) {
        return (
            <main className="agent-editor-loading">
                <Empty description={t("agents.editor.loadFailed")}>
                    <Button onClick={() => void agentQuery.refetch()}>
                        {t("common.retry")}
                    </Button>
                </Empty>
            </main>
        );
    }

    return (
        <main className="agent-editor-page">
            {messageContextHolder}
            <header className="agent-editor-header">
                <Button
                    type="text"
                    aria-label={t("agents.editor.backLabel")}
                    icon={<ArrowLeftOutlined />}
                    onClick={onBack}
                />
                <Avatar
                    className="agent-editor-avatar"
                    shape="square"
                    src={effectiveAvatarUri ?? undefined}
                >
                    {(agentName || "A").slice(0, 1).toUpperCase()}
                </Avatar>
                <div className="agent-editor-identity">
                    <Typography.Text strong>
                        {agentName || t("agents.editor.newAgent")}
                    </Typography.Text>
                    <Space size={8}>
                        <Typography.Text type="secondary">
                            {currentAgent
                                ? t("agents.editor.versionOwner", {
                                      version:
                                          currentAgent.latestPublishedVersionNumber,
                                      owner: isOwner
                                          ? identity?.username
                                          : currentAgent.ownerUserId.slice(
                                                0,
                                                8,
                                            ),
                                  })
                                : t("agents.editor.notSaved")}
                        </Typography.Text>
                        <AgentStateTag
                            agent={currentAgent}
                            dirty={dirty}
                            savedSincePublish={savedSincePublish}
                        />
                    </Space>
                </div>
                <div className="agent-editor-actions">
                    {isOwner && currentAgent && (
                        <Tooltip title={t("agents.editor.delete")}>
                            <Button
                                danger
                                type="text"
                                aria-label={t("agents.editor.delete")}
                                icon={<DeleteOutlined />}
                                loading={deleteMutation.isPending}
                                disabled={busy}
                                onClick={confirmDelete}
                            />
                        </Tooltip>
                    )}
                    <Button
                        icon={<PlayCircleOutlined />}
                        loading={saveMutation.isPending}
                        disabled={busy}
                        onClick={() => void openTrial()}
                    >
                        {t("agents.editor.trial")}
                    </Button>
                    {isOwner && (
                        <Button
                            icon={<SaveOutlined />}
                            loading={saveMutation.isPending}
                            disabled={busy}
                            onClick={() => void save()}
                        >
                            {t("common.save")}
                        </Button>
                    )}
                    {isOwner && (
                        <Button
                            type="primary"
                            icon={<CloudUploadOutlined />}
                            loading={publishMutation.isPending}
                            disabled={busy}
                            onClick={() => setPublishOpen(true)}
                        >
                            {t("agents.editor.publish")}
                        </Button>
                    )}
                    {!isOwner && currentAgent && (
                        <Button
                            type="primary"
                            icon={<CopyOutlined />}
                            loading={cloneMutation.isPending}
                            disabled={busy}
                            onClick={() =>
                                cloneMutation.mutate(currentAgent.id)
                            }
                        >
                            {t("agents.editor.copyToMine")}
                        </Button>
                    )}
                </div>
            </header>

            {!isOwner && (
                <Alert
                    banner
                    type="info"
                    showIcon
                    message={t("agents.editor.readonly")}
                />
            )}

            <div
                className={`agent-editor-body ${trialOpen ? "trial-open" : ""}`}
            >
                <div className="agent-editor-workspace">
                    <nav
                        className={`agent-editor-sections ${sectionsCollapsed ? "collapsed" : ""}`}
                        aria-label={t("agents.editor.navigationLabel")}
                    >
                        <div className="agent-editor-sections-toggle">
                            <Button
                                type="text"
                                size="small"
                                aria-label={
                                    sectionsCollapsed
                                        ? t("agents.editor.expandNavigation")
                                        : t("agents.editor.collapseNavigation")
                                }
                                icon={
                                    sectionsCollapsed ? (
                                        <MenuUnfoldOutlined />
                                    ) : (
                                        <MenuFoldOutlined />
                                    )
                                }
                                onClick={() =>
                                    setSectionsCollapsed((value) => !value)
                                }
                            />
                        </div>
                        <div className="agent-editor-section-list">
                            {sections.map((item) => (
                                <Tooltip
                                    key={item.key}
                                    title={
                                        sectionsCollapsed
                                            ? item.label
                                            : undefined
                                    }
                                    placement="right"
                                >
                                    <button
                                        type="button"
                                        className={
                                            item.key === section ? "active" : ""
                                        }
                                        aria-label={
                                            sectionsCollapsed
                                                ? item.label
                                                : undefined
                                        }
                                        onClick={() => setSection(item.key)}
                                    >
                                        {item.icon}
                                        {!sectionsCollapsed && (
                                            <span>{item.label}</span>
                                        )}
                                    </button>
                                </Tooltip>
                            ))}
                        </div>
                    </nav>
                    <section className="agent-editor-content">
                        <div className="agent-editor-section-heading">
                            <Typography.Title level={5}>
                                {selectedSection.label}
                            </Typography.Title>
                            {section === "instructions" && (
                                <Tooltip
                                    title={t("agents.editor.instructionsHelp")}
                                >
                                    <Button
                                        type="text"
                                        size="small"
                                        aria-label={t(
                                            "agents.editor.instructionsHelpLabel",
                                        )}
                                        icon={<QuestionCircleOutlined />}
                                    />
                                </Tooltip>
                            )}
                        </div>
                        <div className="agent-editor-content-scroll">
                            <Form<AgentFormValues>
                                form={form}
                                layout="vertical"
                                initialValues={InitialValues}
                                disabled={!isOwner || busy}
                                onValuesChange={() => setDirty(true)}
                            >
                                {sections.map((item) => (
                                    <div
                                        key={item.key}
                                        hidden={section !== item.key}
                                    >
                                        <AgentSection
                                            section={item.key}
                                            agent={currentAgent}
                                            agentName={agentName}
                                            avatarUri={effectiveAvatarUri}
                                            readonly={!isOwner}
                                            avatarUploading={
                                                avatarMutation.isPending
                                            }
                                            onAvatarUpload={(file) =>
                                                avatarMutation.mutate(file)
                                            }
                                            chatModels={chatModels}
                                            modelsLoading={
                                                modelsQuery.isLoading
                                            }
                                            tools={toolsQuery.data ?? []}
                                            toolsLoading={toolsQuery.isLoading}
                                            skills={skillsQuery.data ?? []}
                                            skillsLoading={
                                                skillsQuery.isLoading
                                            }
                                            versions={versionsQuery.data ?? []}
                                            versionsLoading={
                                                versionsQuery.isLoading
                                            }
                                            onRollback={(
                                                versionId,
                                                rollbackChangeSummary,
                                            ) =>
                                                rollbackMutation.mutateAsync({
                                                    versionId,
                                                    changeSummary:
                                                        rollbackChangeSummary,
                                                })
                                            }
                                            rollbackPending={
                                                rollbackMutation.isPending
                                            }
                                        />
                                    </div>
                                ))}
                            </Form>
                        </div>
                    </section>
                </div>
                {trialOpen && currentAgent && (
                    <aside className="agent-editor-trial">
                        <ChatPanel
                            variant="trial"
                            runMode={isOwner ? "draft" : "published"}
                            onClose={() => {
                                setTrialOpen(false);
                                setSectionsCollapsed(false);
                            }}
                            agent={{
                                id: currentAgent.id,
                                name: currentAgent.name,
                                avatarUri: currentAgent.avatarUri,
                                descriptionExcerpt: currentAgent.description,
                                ownerUserId: currentAgent.ownerUserId,
                                isShared: currentAgent.isShared,
                                latestPublishedVersionNumber:
                                    currentAgent.latestPublishedVersionNumber,
                                updatedTime: currentAgent.updatedTime,
                            }}
                        />
                    </aside>
                )}
            </div>

            <Modal
                open={publishOpen}
                title={t("agents.editor.publishDialog.title")}
                okText={t("agents.editor.publish")}
                cancelText={t("common.cancel")}
                confirmLoading={publishMutation.isPending}
                onCancel={() => {
                    setPublishOpen(false);
                    setShareAfterPublish(false);
                }}
                onOk={() => publishMutation.mutate()}
            >
                <Typography.Paragraph
                    type="secondary"
                    style={{ marginBottom: 16 }}
                >
                    {t("agents.editor.publishDialog.description")}
                </Typography.Paragraph>
                <Form layout="vertical">
                    <Form.Item
                        label={t("agents.editor.publishDialog.changeSummary")}
                        style={{ marginBottom: 0 }}
                    >
                        <Input
                            value={changeSummary}
                            onChange={(event) =>
                                setChangeSummary(event.target.value)
                            }
                            placeholder={t(
                                "agents.editor.publishDialog.changeSummaryPlaceholder",
                            )}
                            maxLength={100}
                        />
                    </Form.Item>
                    <Form.Item style={{ marginTop: 16, marginBottom: 0 }}>
                        <Checkbox
                            checked={shareAfterPublish}
                            onChange={(event) =>
                                setShareAfterPublish(event.target.checked)
                            }
                        >
                            {t("agents.editor.publishDialog.shareAfterPublish")}
                        </Checkbox>
                        <Typography.Text
                            className="agent-publish-share-hint"
                            type="secondary"
                        >
                            {t("agents.editor.publishDialog.shareHint")}
                        </Typography.Text>
                    </Form.Item>
                </Form>
            </Modal>
        </main>
    );
}

function AgentSection({
    section,
    agent,
    agentName,
    avatarUri,
    readonly,
    avatarUploading,
    onAvatarUpload,
    chatModels,
    modelsLoading,
    tools,
    toolsLoading,
    skills,
    skillsLoading,
    versions,
    versionsLoading,
    onRollback,
    rollbackPending,
}: {
    section: AgentEditorSection;
    agent: AgentDefinition | null;
    agentName: string | undefined;
    avatarUri: string | null;
    readonly: boolean;
    avatarUploading: boolean;
    onAvatarUpload: (file: File) => void;
    chatModels: Awaited<ReturnType<typeof desktopApi.listModels>>;
    modelsLoading: boolean;
    tools: Awaited<ReturnType<typeof desktopApi.listTools>>;
    toolsLoading: boolean;
    skills: Awaited<ReturnType<typeof desktopApi.listSkills>>;
    skillsLoading: boolean;
    versions: Awaited<ReturnType<typeof desktopApi.listAgentVersions>>;
    versionsLoading: boolean;
    onRollback: (
        versionId: string,
        changeSummary: string | null,
    ) => Promise<AgentVersion>;
    rollbackPending: boolean;
}) {
    const { t } = useTranslation();
    if (section === "basic") {
        return (
            <div className="agent-basic-grid">
                <Form.Item
                    label={t("agents.editor.basic.avatar")}
                    style={{ marginBottom: 0 }}
                >
                    <div className="agent-basic-avatar-wrap">
                        {readonly ? (
                            <Avatar
                                className="agent-basic-avatar"
                                shape="square"
                                src={avatarUri ?? undefined}
                            >
                                {(agentName || agent?.name || "A").slice(0, 1)}
                            </Avatar>
                        ) : (
                            <Tooltip
                                title={t("agents.editor.basic.changeAvatar")}
                            >
                                <Upload
                                    showUploadList={false}
                                    accept="image/png,image/jpeg,image/webp"
                                    disabled={avatarUploading}
                                    beforeUpload={(file) => {
                                        onAvatarUpload(file);
                                        return false;
                                    }}
                                >
                                    <button
                                        type="button"
                                        className="agent-basic-avatar agent-basic-avatar-editor"
                                        aria-label={t(
                                            "agents.editor.basic.changeAvatarLabel",
                                        )}
                                    >
                                        {avatarUri ? (
                                            <img src={avatarUri} alt="" />
                                        ) : (
                                            <span>
                                                {(agentName || "A").slice(0, 1)}
                                            </span>
                                        )}
                                        <span className="agent-basic-avatar-overlay">
                                            <CameraOutlined
                                                spin={avatarUploading}
                                            />
                                        </span>
                                    </button>
                                </Upload>
                            </Tooltip>
                        )}
                        <Typography.Text type="secondary">
                            {readonly
                                ? t("agents.editor.basic.avatarReadonly")
                                : t("agents.editor.basic.clickToChange")}
                        </Typography.Text>
                    </div>
                </Form.Item>
                <div>
                    <Form.Item
                        label={t("agents.editor.basic.name")}
                        name="name"
                        rules={[
                            {
                                required: true,
                                message: t("agents.editor.basic.nameRequired"),
                            },
                            {
                                max: 50,
                                message: t("agents.editor.basic.nameTooLong"),
                            },
                        ]}
                    >
                        <Input
                            placeholder={t(
                                "agents.editor.basic.namePlaceholder",
                            )}
                        />
                    </Form.Item>
                    <Form.Item
                        label={t("agents.editor.basic.description")}
                        name="description"
                        style={{ marginBottom: 0 }}
                    >
                        <Input.TextArea
                            rows={5}
                            maxLength={500}
                            showCount
                            placeholder={t(
                                "agents.editor.basic.descriptionPlaceholder",
                            )}
                        />
                    </Form.Item>
                    <Form.Item
                        label={t("agents.editor.basic.teamVisible")}
                        name="isShared"
                        valuePropName="checked"
                        tooltip={t("agents.editor.basic.teamVisibleHelp")}
                        style={{ marginTop: 18, marginBottom: 0 }}
                    >
                        <Switch
                            checkedChildren={t("agents.editor.basic.shared")}
                            unCheckedChildren={t("agents.editor.basic.private")}
                        />
                    </Form.Item>
                </div>
            </div>
        );
    }
    if (section === "instructions") {
        return <InstructionsSection readonly={readonly} />;
    }
    if (section === "model") {
        return <ModelSection chatModels={chatModels} loading={modelsLoading} />;
    }
    if (section === "tools") {
        return (
            <BindingSelector
                name="toolIds"
                loading={toolsLoading}
                empty={t("agents.editor.bindings.noTools")}
                items={tools.map((tool) => ({
                    id: tool.id,
                    name: tool.name,
                    description: tool.description,
                    parameters: getToolParameterNames(
                        tool.parametersJsonSchema,
                    ),
                }))}
            />
        );
    }
    if (section === "skills") {
        return (
            <>
                <Typography.Paragraph
                    type="secondary"
                    className="agent-skills-description"
                >
                    {t("agents.editor.bindings.skillsDescription")}
                </Typography.Paragraph>
                <BindingSelector
                    name="skillIds"
                    loading={skillsLoading}
                    empty={t("agents.editor.bindings.noSkills")}
                    items={skills.map((skill) => ({
                        id: skill.id,
                        name: skill.name,
                        description: skill.description,
                    }))}
                />
            </>
        );
    }
    if (versionsLoading) return <Skeleton active paragraph={{ rows: 4 }} />;
    return (
        <VersionHistorySection
            agent={agent}
            versions={versions}
            tools={tools}
            readonly={readonly}
            onRollback={onRollback}
            rollbackPending={rollbackPending}
        />
    );
}

interface VersionSnapshotField {
    section: string;
    label: string;
    value: string;
}

function versionSnapshotFields(
    snapshot: AgentVersion["snapshot"],
    tools: Awaited<ReturnType<typeof desktopApi.listTools>>,
    t: TFunction,
): VersionSnapshotField[] {
    const toolName = (toolId: string): string =>
        tools.find((tool) => tool.id === toolId)?.name ?? toolId.slice(0, 8);
    const model = snapshot.buildOptions.modelOptions;
    const toolSummary =
        (snapshot.buildOptions.toolBindings ?? [])
            .map((binding) => toolName(binding.toolId))
            .join(t("common.listSeparator")) ||
        t("agents.editor.version.emptyValue");
    const skillSummary =
        (snapshot.buildOptions.skills ?? [])
            .map((skill) => skill.name)
            .join(t("common.listSeparator")) ||
        t("agents.editor.version.emptyValue");
    return [
        {
            section: t("agents.editor.version.snapshot.basic"),
            label: t("agents.editor.version.snapshot.name"),
            value: snapshot.name,
        },
        {
            section: t("agents.editor.version.snapshot.basic"),
            label: t("agents.editor.version.snapshot.description"),
            value: snapshot.description ?? "-",
        },
        {
            section: t("agents.editor.version.snapshot.instructions"),
            label: t("agents.editor.version.snapshot.instructions"),
            value: snapshot.instructions ?? "-",
        },
        {
            section: t("agents.editor.version.snapshot.model"),
            label: t("agents.editor.version.snapshot.modelId"),
            value: model.modelId ?? "-",
        },
        {
            section: t("agents.editor.version.snapshot.model"),
            label: t("agents.editor.model.temperature"),
            value: String(
                model.temperature ?? t("agents.editor.version.defaultValue"),
            ),
        },
        {
            section: t("agents.editor.version.snapshot.model"),
            label: t("agents.editor.model.topP"),
            value: String(
                model.topP ?? t("agents.editor.version.defaultValue"),
            ),
        },
        {
            section: t("agents.editor.version.snapshot.model"),
            label: t("agents.editor.model.maxTokens"),
            value: String(
                model.maxTokens ?? t("agents.editor.version.defaultValue"),
            ),
        },
        {
            section: t("agents.editor.version.snapshot.capabilities"),
            label: t("agents.editor.version.snapshot.tools"),
            value: toolSummary,
        },
        {
            section: t("agents.editor.version.snapshot.capabilities"),
            label: t("agents.editor.version.snapshot.skills"),
            value: skillSummary,
        },
    ];
}

function VersionHistorySection({
    agent,
    versions,
    tools,
    readonly,
    onRollback,
    rollbackPending,
}: {
    agent: AgentDefinition | null;
    versions: Awaited<ReturnType<typeof desktopApi.listAgentVersions>>;
    tools: Awaited<ReturnType<typeof desktopApi.listTools>>;
    readonly: boolean;
    onRollback: (
        versionId: string,
        changeSummary: string | null,
    ) => Promise<AgentVersion>;
    rollbackPending: boolean;
}) {
    const { t, i18n } = useTranslation();
    const appearance = useResolvedAppearance();
    const { token } = theme.useToken();
    const [modal, modalContextHolder] = Modal.useModal();
    const [detailTarget, setDetailTarget] = useState<AgentVersion | null>(null);
    const [compareOpen, setCompareOpen] = useState(false);
    const [baseVersionId, setBaseVersionId] = useState<string | null>(null);
    const [targetVersionId, setTargetVersionId] = useState<string | null>(null);

    if (versions.length === 0)
        return <Empty description={t("agents.editor.version.none")} />;

    const latestVersionNumber =
        agent?.latestPublishedVersionNumber ??
        Math.max(...versions.map((version) => version.versionNumber));
    const isPublished = (version: AgentVersion): boolean =>
        version.versionNumber === latestVersionNumber;
    const savedBy = (version: AgentVersion): string =>
        version.ownerUserName ?? version.ownerUserId;

    const openComparison = (baseId: string, targetId: string): void => {
        setBaseVersionId(baseId);
        setTargetVersionId(targetId);
        setCompareOpen(true);
    };

    const confirmRollback = (version: AgentVersion): void => {
        modal.confirm({
            title: t("agents.editor.version.rollbackTitle", {
                version: version.versionNumber,
            }),
            content: t("agents.editor.version.rollbackContent"),
            okText: t("agents.editor.version.confirmRollback"),
            cancelText: t("common.cancel"),
            okButtonProps: { danger: true },
            onOk: async () => {
                await onRollback(version.id, null);
                setDetailTarget(null);
            },
        });
    };

    const base =
        versions.find((version) => version.id === baseVersionId) ??
        versions[Math.min(1, versions.length - 1)];
    const target =
        versions.find((version) => version.id === targetVersionId) ??
        versions[0];
    const baseFields = versionSnapshotFields(base.snapshot, tools, t);
    const targetFields = versionSnapshotFields(target.snapshot, tools, t);
    const differences = baseFields
        .map((field, index) => ({
            ...field,
            targetValue: targetFields[index].value,
        }))
        .filter((field) => field.value !== field.targetValue);

    const detailIndex = detailTarget
        ? versions.findIndex((version) => version.id === detailTarget.id)
        : -1;

    return (
        <div style={{ minWidth: 0 }}>
            {modalContextHolder}
            <Flex
                justify="space-between"
                align="flex-start"
                gap={16}
                style={{ marginBottom: 16 }}
            >
                <div>
                    <Typography.Title level={5} style={{ margin: 0 }}>
                        {t("agents.editor.version.history")}
                    </Typography.Title>
                    <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                        {t("agents.editor.version.historyDescription")}
                    </Typography.Text>
                </div>
                <Button
                    icon={<DiffOutlined />}
                    disabled={versions.length < 2}
                    onClick={() =>
                        openComparison(versions[1].id, versions[0].id)
                    }
                >
                    {t("agents.editor.version.compare")}
                </Button>
            </Flex>
            <Table
                size="small"
                pagination={false}
                scroll={{ x: 760 }}
                rowKey="id"
                dataSource={versions}
                columns={[
                    {
                        title: t("agents.editor.version.columns.version"),
                        dataIndex: "versionNumber",
                        width: 72,
                        render: (versionNumber: number) => (
                            <Typography.Text strong>
                                v{versionNumber}
                            </Typography.Text>
                        ),
                    },
                    {
                        title: t("agents.editor.version.columns.status"),
                        key: "status",
                        width: 96,
                        render: (_value: unknown, version: AgentVersion) => (
                            <Tag
                                color={
                                    isPublished(version) ? "success" : "default"
                                }
                            >
                                {isPublished(version)
                                    ? t("agents.editor.version.published")
                                    : t("agents.editor.version.historical")}
                            </Tag>
                        ),
                    },
                    {
                        title: t("agents.editor.version.columns.savedTime"),
                        dataIndex: "createdTime",
                        width: 168,
                        render: (createdTime: string) =>
                            new Date(createdTime).toLocaleString(i18n.language),
                    },
                    {
                        title: t("agents.editor.version.columns.savedBy"),
                        key: "savedBy",
                        width: 120,
                        render: (_value: unknown, version: AgentVersion) =>
                            savedBy(version),
                    },
                    {
                        title: t("agents.editor.version.columns.summary"),
                        dataIndex: "changeSummary",
                        render: (summary: string | null) => summary || "-",
                    },
                    {
                        title: t("agents.editor.version.columns.actions"),
                        key: "actions",
                        width: 72,
                        align: "center",
                        render: (_value: unknown, version: AgentVersion) => (
                            <Button
                                type="link"
                                size="small"
                                onClick={() => setDetailTarget(version)}
                            >
                                {t("common.view")}
                            </Button>
                        ),
                    },
                ]}
            />

            <AgentDetailsDrawer
                appearance={appearance}
                open={detailTarget !== null}
                onClose={() => setDetailTarget(null)}
                version={detailTarget}
                tools={tools}
                statusLabel={
                    detailTarget && isPublished(detailTarget)
                        ? "published"
                        : "historical"
                }
                extra={
                    detailTarget && (
                        <Space>
                            {detailIndex >= 0 &&
                                detailIndex < versions.length - 1 && (
                                    <Button
                                        size="small"
                                        icon={<DiffOutlined />}
                                        onClick={() =>
                                            openComparison(
                                                versions[detailIndex + 1].id,
                                                detailTarget.id,
                                            )
                                        }
                                    >
                                        {t(
                                            "agents.editor.version.previousCompare",
                                        )}
                                    </Button>
                                )}
                            {!isPublished(detailTarget) && (
                                <Button
                                    size="small"
                                    icon={<DiffOutlined />}
                                    onClick={() =>
                                        openComparison(
                                            detailTarget.id,
                                            versions[0].id,
                                        )
                                    }
                                >
                                    {t("agents.editor.version.latestCompare")}
                                </Button>
                            )}
                        </Space>
                    )
                }
                footer={
                    detailTarget && !readonly && !isPublished(detailTarget) ? (
                        <Button
                            danger
                            block
                            icon={<RollbackOutlined />}
                            loading={rollbackPending}
                            onClick={() => confirmRollback(detailTarget)}
                        >
                            {t("agents.editor.version.rollbackThis")}
                        </Button>
                    ) : undefined
                }
            />

            <Modal
                open={compareOpen}
                onCancel={() => setCompareOpen(false)}
                footer={null}
                width={920}
                centered
                title={t("agents.editor.version.compare")}
            >
                <Flex align="center" gap={12} style={{ marginBottom: 20 }}>
                    <Select
                        value={base.id}
                        onChange={setBaseVersionId}
                        style={{ flex: 1 }}
                        options={versions.map((version) => ({
                            value: version.id,
                            label: `v${version.versionNumber} · ${version.changeSummary || t("agents.editor.version.noSummary")}`,
                            disabled: version.id === target.id,
                        }))}
                    />
                    <Typography.Text type="secondary">
                        {t("agents.editor.version.versus")}
                    </Typography.Text>
                    <Select
                        value={target.id}
                        onChange={setTargetVersionId}
                        style={{ flex: 1 }}
                        options={versions.map((version) => ({
                            value: version.id,
                            label: `v${version.versionNumber} · ${version.changeSummary || t("agents.editor.version.noSummary")}`,
                            disabled: version.id === base.id,
                        }))}
                    />
                </Flex>
                <div
                    style={{
                        display: "grid",
                        gridTemplateColumns:
                            "140px minmax(0, 1fr) minmax(0, 1fr)",
                        borderTop: `1px solid ${token.colorBorderSecondary}`,
                    }}
                >
                    <Typography.Text strong style={{ padding: 12 }}>
                        {t("agents.editor.version.field")}
                    </Typography.Text>
                    <Typography.Text strong style={{ padding: 12 }}>
                        v{base.versionNumber}
                    </Typography.Text>
                    <Typography.Text strong style={{ padding: 12 }}>
                        v{target.versionNumber}
                    </Typography.Text>
                    {differences.map((field) => (
                        <Fragment key={field.label}>
                            <div
                                style={{
                                    padding: 12,
                                    borderTop: `1px solid ${token.colorBorderSecondary}`,
                                }}
                            >
                                <Typography.Text
                                    type="secondary"
                                    style={{ display: "block", fontSize: 11 }}
                                >
                                    {field.section}
                                </Typography.Text>
                                <Typography.Text strong>
                                    {field.label}
                                </Typography.Text>
                            </div>
                            <div
                                style={{
                                    padding: 12,
                                    whiteSpace: "pre-wrap",
                                    background: token.colorErrorBg,
                                    borderTop: `1px solid ${token.colorBorderSecondary}`,
                                }}
                            >
                                {field.value}
                            </div>
                            <div
                                style={{
                                    padding: 12,
                                    whiteSpace: "pre-wrap",
                                    background: token.colorSuccessBg,
                                    borderTop: `1px solid ${token.colorBorderSecondary}`,
                                }}
                            >
                                {field.targetValue}
                            </div>
                        </Fragment>
                    ))}
                </div>
                {differences.length === 0 && (
                    <Typography.Text type="secondary">
                        {t("agents.editor.version.identical")}
                    </Typography.Text>
                )}
            </Modal>
        </div>
    );
}

function InstructionsSection({ readonly }: { readonly: boolean }) {
    const { t } = useTranslation();
    const appearance = useResolvedAppearance();
    const form = Form.useFormInstance<AgentFormValues>();
    const instructions = Form.useWatch("instructions", form) ?? "";
    return (
        <>
            {instructions.length >= 32_000 && (
                <Alert
                    className="agent-instructions-warning"
                    type="warning"
                    showIcon
                    message={t("agents.editor.instructions.tooLong")}
                />
            )}
            {readonly ? (
                <div className="agent-instructions-markdown">
                    {instructions ? (
                        <MarkdownContent
                            appearance={appearance}
                            content={instructions}
                        />
                    ) : (
                        <Typography.Text type="secondary">
                            {t("agents.editor.instructions.empty")}
                        </Typography.Text>
                    )}
                </div>
            ) : (
                <Form.Item
                    name="instructions"
                    className="agent-instructions-editor"
                >
                    <MarkdownEditor
                        aria-label={t("agents.editor.sections.instructions")}
                        appearance={appearance}
                    />
                </Form.Item>
            )}
            <Typography.Text
                type="secondary"
                className="agent-instructions-editor-count"
            >
                {instructions.length} / 32000
            </Typography.Text>
        </>
    );
}

function BindingSelector({
    name,
    loading,
    empty,
    items,
}: {
    name: "toolIds" | "skillIds";
    loading: boolean;
    empty: string;
    items: Array<{
        id: string;
        name: string;
        description: string;
        parameters?: string[];
    }>;
}) {
    const { t } = useTranslation();
    const form = Form.useFormInstance<AgentFormValues>();
    const selectedIds = Form.useWatch(name, form) ?? [];
    if (loading) return <Skeleton active paragraph={{ rows: 3 }} />;
    if (items.length === 0) return <Empty description={empty} />;
    return (
        <Form.Item name={name} noStyle>
            <Checkbox.Group className="agent-binding-selector">
                {items.map((item) => (
                    <div
                        key={item.id}
                        className={`agent-binding-item${
                            selectedIds.includes(item.id) ? " selected" : ""
                        }`}
                    >
                        <div className="agent-binding-item-main">
                            <Checkbox value={item.id} aria-label={item.name} />
                            <div className="agent-binding-item-icon">
                                {name === "toolIds" ? (
                                    <ToolOutlined />
                                ) : (
                                    <ReadOutlined />
                                )}
                            </div>
                            <div className="agent-binding-item-copy">
                                <Typography.Text strong>
                                    {item.name}
                                </Typography.Text>
                                <Typography.Text type="secondary">
                                    {item.description}
                                </Typography.Text>
                                {name === "skillIds" && (
                                    <Flex
                                        gap={6}
                                        wrap
                                        className="agent-skill-stages"
                                    >
                                        <Tag>
                                            {t(
                                                "agents.editor.bindings.discovery",
                                            )}
                                        </Tag>
                                        <Tag
                                            color={
                                                selectedIds.includes(item.id)
                                                    ? "processing"
                                                    : undefined
                                            }
                                        >
                                            {t(
                                                "agents.editor.bindings.activation",
                                            )}
                                        </Tag>
                                        <Tag>
                                            {t(
                                                "agents.editor.bindings.execution",
                                            )}
                                        </Tag>
                                    </Flex>
                                )}
                                {name === "toolIds" &&
                                    selectedIds.includes(item.id) &&
                                    item.parameters &&
                                    item.parameters.length > 0 && (
                                        <Flex
                                            gap={12}
                                            wrap
                                            className="agent-binding-config"
                                        >
                                            {item.parameters.map(
                                                (parameter) => (
                                                    <Form.Item
                                                        key={parameter}
                                                        name={[
                                                            "toolParameters",
                                                            item.id,
                                                            parameter,
                                                        ]}
                                                        label={parameter}
                                                    >
                                                        <Input size="small" />
                                                    </Form.Item>
                                                ),
                                            )}
                                        </Flex>
                                    )}
                            </div>
                        </div>
                    </div>
                ))}
            </Checkbox.Group>
        </Form.Item>
    );
}

function getToolParameterNames(schema: string): string[] {
    try {
        const parsed = JSON.parse(schema) as {
            properties?: Record<string, unknown>;
        };
        return Object.keys(parsed.properties ?? {});
    } catch {
        return [];
    }
}

function parseToolParameters(
    parametersJson: string | null,
): Record<string, string> {
    if (!parametersJson) return {};
    try {
        const parsed = JSON.parse(parametersJson) as Record<string, unknown>;
        return Object.fromEntries(
            Object.entries(parsed).map(([key, value]) => [
                key,
                String(value ?? ""),
            ]),
        );
    } catch {
        return {};
    }
}

function ModelSection({
    chatModels,
    loading,
}: {
    chatModels: Awaited<ReturnType<typeof desktopApi.listModels>>;
    loading: boolean;
}) {
    const { t } = useTranslation();
    return (
        <>
            <Card size="small" className="agent-model-picker">
                <Form.Item
                    label={t("agents.editor.model.runtime")}
                    name="modelId"
                    required={false}
                    rules={[
                        {
                            required: true,
                            message: t("agents.editor.model.runtimeRequired"),
                        },
                    ]}
                >
                    <Select
                        loading={loading}
                        options={chatModels.map((model) => ({
                            value: model.id,
                            label: (
                                <Space size={6} wrap>
                                    <span>{model.id}</span>
                                    {model.supportsVision && (
                                        <Tag style={{ marginInlineEnd: 0 }}>
                                            {t("agents.editor.model.vision")}
                                        </Tag>
                                    )}
                                    {model.supportsTools && (
                                        <Tag style={{ marginInlineEnd: 0 }}>
                                            {t("agents.editor.model.tools")}
                                        </Tag>
                                    )}
                                    {model.supportsStructuredOutput && (
                                        <Tag style={{ marginInlineEnd: 0 }}>
                                            {t(
                                                "agents.editor.model.structuredOutput",
                                            )}
                                        </Tag>
                                    )}
                                </Space>
                            ),
                        }))}
                    />
                </Form.Item>
            </Card>
            <Typography.Text strong className="agent-model-section-title">
                {t("agents.editor.model.generationParameters")}
            </Typography.Text>
            <Typography.Text
                type="secondary"
                className="agent-model-section-description"
            >
                {t("agents.editor.model.generationDescription")}
            </Typography.Text>
            <div className="agent-parameter-grid">
                <ParameterCard
                    title={t("agents.editor.model.temperature")}
                    description={t(
                        "agents.editor.model.temperatureDescription",
                    )}
                    customName="customTemperature"
                    valueName="temperature"
                    min={0}
                    max={2}
                    step={0.01}
                />
                <ParameterCard
                    title={t("agents.editor.model.topP")}
                    description={t("agents.editor.model.topPDescription")}
                    customName="customTopP"
                    valueName="topP"
                    min={0}
                    max={1}
                    step={0.01}
                />
                <ParameterCard
                    title={t("agents.editor.model.maxTokens")}
                    description={t("agents.editor.model.maxTokensDescription")}
                    customName="customMaxTokens"
                    valueName="maxTokens"
                    min={1}
                    max={128000}
                    step={1}
                    slider={false}
                />
            </div>
            <Typography.Text
                strong
                className="agent-model-section-title context"
            >
                {t("agents.editor.model.context")}
            </Typography.Text>
            <Typography.Text
                type="secondary"
                className="agent-model-section-description context"
            >
                {t("agents.editor.model.contextDescription")}
            </Typography.Text>
            <Form.Item
                className="agent-model-context-input"
                label={t("agents.editor.model.maxMessages")}
                name="maxMessages"
            >
                <InputNumber
                    min={1}
                    max={500}
                    suffix={t("agents.editor.model.messageUnit")}
                    style={{ width: 160 }}
                />
            </Form.Item>
        </>
    );
}

function ParameterCard({
    title,
    description,
    customName,
    valueName,
    min,
    max,
    step,
    slider = true,
}: {
    title: string;
    description: string;
    customName: keyof AgentFormValues;
    valueName: keyof AgentFormValues;
    min: number;
    max: number;
    step: number;
    slider?: boolean;
}) {
    const { t } = useTranslation();
    const form = Form.useFormInstance<AgentFormValues>();
    const custom = Form.useWatch(customName, form) as boolean;
    return (
        <Card size="small">
            <div className="agent-parameter-heading">
                <div>
                    <Typography.Text strong>{title}</Typography.Text>
                    <Typography.Text type="secondary">
                        {description}
                    </Typography.Text>
                </div>
                <Form.Item name={customName} valuePropName="checked" noStyle>
                    <Switch
                        size="small"
                        checkedChildren={t("agents.editor.model.custom")}
                        unCheckedChildren={t("agents.editor.model.default")}
                    />
                </Form.Item>
            </div>
            <div className="agent-parameter-input">
                {slider && (
                    <Form.Item name={valueName} noStyle>
                        <Slider
                            min={min}
                            max={max}
                            step={step}
                            disabled={!custom}
                        />
                    </Form.Item>
                )}
                <Form.Item name={valueName} noStyle>
                    <InputNumber
                        min={min}
                        max={max}
                        step={step}
                        disabled={!custom}
                        size={slider ? "small" : undefined}
                        style={{ width: slider ? 80 : "100%" }}
                    />
                </Form.Item>
            </div>
        </Card>
    );
}

function AgentStateTag({
    agent,
    dirty,
    savedSincePublish,
}: {
    agent: AgentDefinition | null;
    dirty: boolean;
    savedSincePublish: boolean;
}) {
    const { t } = useTranslation();
    if (dirty)
        return <Tag color="warning">{t("agents.editor.state.unsaved")}</Tag>;
    if (!agent?.latestPublishedVersionNumber)
        return <Tag color="warning">{t("agents.editor.state.draft")}</Tag>;
    if (savedSincePublish)
        return <Tag color="processing">{t("agents.editor.state.changed")}</Tag>;
    return <Tag color="success">{t("agents.editor.state.published")}</Tag>;
}

function toFormValues(agent: AgentDefinition): AgentFormValues {
    const model = agent.buildOptions.modelOptions;
    const toolBindings = agent.buildOptions.toolBindings ?? [];
    const skills = agent.buildOptions.skills ?? [];
    return {
        name: agent.name,
        description: agent.description ?? "",
        instructions: agent.instructions ?? "",
        isShared: agent.isShared,
        modelId: model.modelId ?? "",
        customTemperature: model.temperature !== null,
        temperature: model.temperature ?? 0.7,
        customTopP: model.topP !== null,
        topP: model.topP ?? 1,
        customMaxTokens: model.maxTokens !== null,
        maxTokens: model.maxTokens ?? 4096,
        maxMessages: agent.buildOptions.chatHistoryOptions?.maxMessages ?? 40,
        toolIds: toolBindings.map((binding) => binding.toolId),
        toolParameters: Object.fromEntries(
            toolBindings.map((binding) => [
                binding.toolId,
                parseToolParameters(binding.parametersJson),
            ]),
        ),
        skillIds: skills.map((skill) => skill.id),
    };
}

async function persistAgent(
    values: AgentFormValues,
    currentAgent: AgentDefinition | null,
    avatarUri: string | null,
): Promise<AgentDefinition> {
    const request = toUpsertRequest(values, currentAgent, avatarUri);
    const saved = currentAgent
        ? await desktopApi.updateAgent(currentAgent.id, request)
        : await desktopApi.createAgent(request);
    if (values.isShared !== saved.isShared) {
        if (values.isShared) await desktopApi.shareAgent(saved.id);
        else await desktopApi.unshareAgent(saved.id);
        return { ...saved, isShared: values.isShared };
    }
    return saved;
}

function toUpsertRequest(
    values: AgentFormValues,
    agent: AgentDefinition | null,
    avatarUri: string | null,
): AgentUpsertRequest {
    const toolIds = values.toolIds ?? [];
    const toolParameters = values.toolParameters ?? {};
    const skillIds = values.skillIds ?? [];
    return {
        name: values.name.trim(),
        avatarUri,
        description: values.description.trim() || null,
        instructions: values.instructions || null,
        modelOptions: {
            modelId: values.modelId,
            temperature: values.customTemperature ? values.temperature : null,
            topP: values.customTopP ? values.topP : null,
            maxTokens: values.customMaxTokens ? values.maxTokens : null,
        },
        chatHistoryOptions: {
            ...(agent?.buildOptions.chatHistoryOptions ?? {
                reducerType: null,
                maxMessagesToRetrieve: null,
            }),
            maxMessages: values.maxMessages,
        },
        toolBindings: toolIds.map((toolId) => ({
            toolId,
            parametersJson:
                Object.keys(toolParameters[toolId] ?? {}).length > 0
                    ? JSON.stringify(toolParameters[toolId])
                    : null,
        })),
        skillBindings: skillIds.map((skillId) => ({ skillId })),
    };
}
