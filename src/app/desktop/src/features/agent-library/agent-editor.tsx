import {
    ArrowLeftOutlined,
    CameraOutlined,
    CloudUploadOutlined,
    CopyOutlined,
    DeleteOutlined,
    HistoryOutlined,
    MenuFoldOutlined,
    MenuUnfoldOutlined,
    PlayCircleOutlined,
    QuestionCircleOutlined,
    ReadOutlined,
    RobotOutlined,
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
} from "antd";
import { useEffect, useState } from "react";
import { desktopApi } from "../../shared/network/desktop-api";
import type {
    AgentDefinition,
    AgentUpsertRequest,
} from "../../shared/network/contracts";
import { useAuthStore } from "../auth/auth-store";
import { ChatPanel } from "../chat/chat-panel";

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
    label: string;
    icon: React.ReactNode;
}> = [
    { key: "basic", label: "基础信息", icon: <UserOutlined /> },
    { key: "instructions", label: "Instructions", icon: <RobotOutlined /> },
    { key: "model", label: "模型与参数", icon: <SlidersOutlined /> },
    { key: "tools", label: "工具", icon: <ToolOutlined /> },
    { key: "skills", label: "Skills", icon: <ReadOutlined /> },
    { key: "version", label: "版本", icon: <HistoryOutlined /> },
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
            messageApi.success("已存为草稿，未影响已发布版本");
        },
        onError: (reason) =>
            messageApi.error(
                reason instanceof Error ? reason.message : "草稿保存失败。",
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
            messageApi.success("头像已上传，请保存 Agent");
        },
        onError: (reason) =>
            messageApi.error(
                reason instanceof Error ? reason.message : "头像上传失败。",
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
            messageApi.success(`已发布为 v${version.versionNumber}`);
        },
        onError: (reason) =>
            messageApi.error(
                reason instanceof Error
                    ? reason.message
                    : "发布失败，草稿已保留。",
            ),
    });
    const cloneMutation = useMutation({
        mutationFn: (sourceAgentId: string) =>
            desktopApi.cloneAgent(sourceAgentId),
        onSuccess: async (clone) => {
            await queryClient.invalidateQueries({ queryKey: ["agents"] });
            messageApi.success("已复制为我的 Agent");
            onClone(clone.id);
        },
        onError: (reason) =>
            messageApi.error(
                reason instanceof Error ? reason.message : "复制 Agent 失败。",
            ),
    });
    const deleteMutation = useMutation({
        mutationFn: (targetAgentId: string) =>
            desktopApi.deleteAgent(targetAgentId),
        onSuccess: async () => {
            await queryClient.invalidateQueries({ queryKey: ["agents"] });
            messageApi.success("Agent 已删除");
            onBack();
        },
        onError: (reason) =>
            messageApi.error(
                reason instanceof Error ? reason.message : "删除 Agent 失败。",
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
        deleteMutation.isPending;
    const selectedSection = Sections.find((item) => item.key === section)!;
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
            title: `删除「${currentAgent.name}」？`,
            content:
                "将永久删除该 Agent、全部版本及相关会话历史，操作不可恢复。",
            okText: "确认删除",
            okButtonProps: { danger: true },
            cancelText: "取消",
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
                <Empty description="Agent 加载失败">
                    <Button onClick={() => void agentQuery.refetch()}>
                        重试
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
                    aria-label="返回 Agent 空间"
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
                        {agentName || "新建 Agent"}
                    </Typography.Text>
                    <Space size={8}>
                        <Typography.Text type="secondary">
                            {currentAgent
                                ? `v${currentAgent.latestPublishedVersionNumber} · Owner: ${isOwner ? identity?.username : currentAgent.ownerUserId.slice(0, 8)}`
                                : "尚未保存"}
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
                        <Tooltip title="删除 Agent">
                            <Button
                                danger
                                type="text"
                                aria-label="删除 Agent"
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
                        试运行
                    </Button>
                    {isOwner && (
                        <Button
                            icon={<SaveOutlined />}
                            loading={saveMutation.isPending}
                            disabled={busy}
                            onClick={() => void save()}
                        >
                            保存
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
                            发布
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
                            复制为我的 Agent
                        </Button>
                    )}
                </div>
            </header>

            {!isOwner && (
                <Alert
                    banner
                    type="info"
                    showIcon
                    message="这是其他成员共享的 Agent，当前为只读模式。"
                />
            )}

            <div
                className={`agent-editor-body ${trialOpen ? "trial-open" : ""}`}
            >
                <div className="agent-editor-workspace">
                    <nav
                        className={`agent-editor-sections ${sectionsCollapsed ? "collapsed" : ""}`}
                        aria-label="Agent 配置区段"
                    >
                        <div className="agent-editor-sections-toggle">
                            <Button
                                type="text"
                                size="small"
                                aria-label={
                                    sectionsCollapsed
                                        ? "展开配置导航"
                                        : "收起配置导航"
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
                            {Sections.map((item) => (
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
                                <Tooltip title="给 Agent 的系统指令。支持 Markdown，超过 32K 字符时给出警告。">
                                    <Button
                                        type="text"
                                        size="small"
                                        aria-label="系统指令说明"
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
                                {Sections.map((item) => (
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
                title="发布新版本"
                okText="发布"
                cancelText="取消"
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
                    发布后将成为新的正式版本，正在使用该 Agent
                    的对话会从下一轮开始使用新版本。
                </Typography.Paragraph>
                <Form layout="vertical">
                    <Form.Item
                        label="变更说明（可选）"
                        style={{ marginBottom: 0 }}
                    >
                        <Input
                            value={changeSummary}
                            onChange={(event) =>
                                setChangeSummary(event.target.value)
                            }
                            placeholder="说明本次修改的内容，会记录到版本历史里"
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
                            发布后共享给团队
                        </Checkbox>
                        <Typography.Text
                            className="agent-publish-share-hint"
                            type="secondary"
                        >
                            团队成员将可以查看并使用本次发布的新版本。
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
}) {
    if (section === "basic") {
        return (
            <div className="agent-basic-grid">
                <Form.Item label="头像" style={{ marginBottom: 0 }}>
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
                            <Tooltip title="更换头像">
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
                                        aria-label="更换 Agent 头像"
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
                            {readonly ? "Agent 头像" : "点击更换"}
                        </Typography.Text>
                    </div>
                </Form.Item>
                <div>
                    <Form.Item
                        label="Agent 名称"
                        name="name"
                        rules={[
                            { required: true, message: "请输入 Agent 名称" },
                            { max: 50, message: "名称不能超过 50 个字符" },
                        ]}
                    >
                        <Input placeholder="Agent 名称（1–50 字符）" />
                    </Form.Item>
                    <Form.Item
                        label="描述"
                        name="description"
                        style={{ marginBottom: 0 }}
                    >
                        <Input.TextArea
                            rows={5}
                            maxLength={500}
                            showCount
                            placeholder="简要描述这个 Agent 的职责和能力"
                        />
                    </Form.Item>
                    <Form.Item
                        label="团队可见"
                        name="isShared"
                        valuePropName="checked"
                        tooltip="对应 Agent 管理态的 IsShared，不随版本快照回滚。"
                        style={{ marginTop: 18, marginBottom: 0 }}
                    >
                        <Switch
                            checkedChildren="已共享"
                            unCheckedChildren="仅自己"
                        />
                    </Form.Item>
                </div>
            </div>
        );
    }
    if (section === "instructions") {
        return <InstructionsSection />;
    }
    if (section === "model") {
        return <ModelSection chatModels={chatModels} loading={modelsLoading} />;
    }
    if (section === "tools") {
        return (
            <BindingSelector
                name="toolIds"
                loading={toolsLoading}
                empty="统一工具管理中暂无可用工具"
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
                    从统一 Skill 管理中选择需要挂载到当前 Agent 的 Skill。
                </Typography.Paragraph>
                <BindingSelector
                    name="skillIds"
                    loading={skillsLoading}
                    empty="统一 Skill 管理中暂无可用 Skill"
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
    if (!agent?.latestPublishedVersionNumber || versions.length === 0)
        return <Empty description="此 Agent 尚未发布" />;
    return (
        <Table
            size="small"
            pagination={false}
            rowKey="id"
            dataSource={versions}
            columns={[
                {
                    title: "版本",
                    dataIndex: "versionNumber",
                    width: 72,
                    render: (versionNumber: number) => (
                        <Typography.Text strong>
                            v{versionNumber}
                        </Typography.Text>
                    ),
                },
                {
                    title: "状态",
                    dataIndex: "versionNumber",
                    width: 96,
                    render: (versionNumber: number) => (
                        <Tag
                            color={
                                versionNumber ===
                                agent.latestPublishedVersionNumber
                                    ? "success"
                                    : "default"
                            }
                        >
                            {versionNumber ===
                            agent.latestPublishedVersionNumber
                                ? "已发布"
                                : "历史版本"}
                        </Tag>
                    ),
                },
                {
                    title: "保存时间",
                    dataIndex: "createdTime",
                    width: 168,
                    render: (createdTime: string) =>
                        new Date(createdTime).toLocaleString("zh-CN"),
                },
                { title: "保存人", dataIndex: "createdByUserId", width: 120 },
                {
                    title: "变更摘要",
                    dataIndex: "changeSummary",
                    render: (summary: string | null) => summary || "-",
                },
            ]}
        />
    );
}

function InstructionsSection() {
    const form = Form.useFormInstance<AgentFormValues>();
    const instructions = Form.useWatch("instructions", form) ?? "";
    return (
        <>
            {instructions.length >= 32_000 && (
                <Alert
                    className="agent-instructions-warning"
                    type="warning"
                    showIcon
                    message="Instructions 较长，可能挤占模型上下文"
                />
            )}
            <Form.Item name="instructions">
                <Input.TextArea
                    aria-label="Instructions"
                    rows={20}
                    showCount={{
                        formatter: ({ count }) => `${count} / 32000`,
                    }}
                    placeholder="输入给 Agent 的系统指令…"
                />
            </Form.Item>
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
    const form = Form.useFormInstance<AgentFormValues>();
    const selectedIds = Form.useWatch(name, form) ?? [];
    if (loading) return <Skeleton active paragraph={{ rows: 3 }} />;
    if (items.length === 0) return <Empty description={empty} />;
    return (
        <Form.Item name={name} noStyle>
            <Checkbox.Group className="agent-binding-selector">
                {items.map((item) => (
                    <Card key={item.id} size="small">
                        <Checkbox value={item.id}>
                            <Typography.Text strong>
                                {item.name}
                            </Typography.Text>
                            <Typography.Text type="secondary">
                                {item.description}
                            </Typography.Text>
                            {name === "toolIds" &&
                                selectedIds.includes(item.id) &&
                                item.parameters &&
                                item.parameters.length > 0 && (
                                    <div className="agent-tool-parameters">
                                        {item.parameters.map((parameter) => (
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
                                        ))}
                                    </div>
                                )}
                        </Checkbox>
                    </Card>
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
    return (
        <>
            <Card size="small" className="agent-model-picker">
                <Form.Item
                    label="运行模型"
                    name="modelId"
                    required={false}
                    rules={[{ required: true, message: "请选择运行模型" }]}
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
                                            视觉
                                        </Tag>
                                    )}
                                    {model.supportsTools && (
                                        <Tag style={{ marginInlineEnd: 0 }}>
                                            工具
                                        </Tag>
                                    )}
                                    {model.supportsStructuredOutput && (
                                        <Tag style={{ marginInlineEnd: 0 }}>
                                            结构化输出
                                        </Tag>
                                    )}
                                </Space>
                            ),
                        }))}
                    />
                </Form.Item>
            </Card>
            <Typography.Text strong className="agent-model-section-title">
                生成参数
            </Typography.Text>
            <Typography.Text
                type="secondary"
                className="agent-model-section-description"
            >
                关闭“自定义”后使用模型默认值，不写入请求参数。
            </Typography.Text>
            <div className="agent-parameter-grid">
                <ParameterCard
                    title="Temperature"
                    description="控制输出的随机性"
                    customName="customTemperature"
                    valueName="temperature"
                    min={0}
                    max={2}
                    step={0.01}
                />
                <ParameterCard
                    title="Top P"
                    description="限制候选词概率范围"
                    customName="customTopP"
                    valueName="topP"
                    min={0}
                    max={1}
                    step={0.01}
                />
                <ParameterCard
                    title="Max Tokens"
                    description="限制单次输出长度"
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
                上下文
            </Typography.Text>
            <Typography.Text
                type="secondary"
                className="agent-model-section-description context"
            >
                超过该数量时，最早的历史消息会被裁剪，避免无限增长挤占模型上下文。
            </Typography.Text>
            <Form.Item
                className="agent-model-context-input"
                label="最大消息记录数"
                name="maxMessages"
            >
                <InputNumber
                    min={1}
                    max={500}
                    suffix="条"
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
                        checkedChildren="自定义"
                        unCheckedChildren="默认"
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
    if (dirty) return <Tag color="warning">未保存</Tag>;
    if (!agent?.latestPublishedVersionNumber)
        return <Tag color="warning">未发布的草稿</Tag>;
    if (savedSincePublish) return <Tag color="processing">有未发布的修改</Tag>;
    return <Tag color="success">已发布</Tag>;
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
