import {
    CalendarOutlined,
    CloseOutlined,
    DeleteOutlined,
    DownOutlined,
    EditOutlined,
    EyeOutlined,
    FileTextOutlined,
    FolderOpenOutlined,
    InboxOutlined,
    PlusOutlined,
    ReadOutlined,
    UpOutlined,
    UserOutlined,
} from "@ant-design/icons";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
    Avatar,
    Button,
    Descriptions,
    Drawer,
    Flex,
    Form,
    Input,
    Modal,
    Select,
    Space,
    Tag,
    Tooltip,
    Typography,
    Upload,
    message,
    theme,
    type UploadFile,
} from "antd";
import JSZip from "jszip";
import type { TFunction } from "i18next";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import DataListPage, {
    DataListRowAction,
    DataListRowActions,
} from "../../shared/components/data-list-page";
import { MarkdownContent } from "../../shared/components/markdown-content";
import { MarkdownEditor } from "../../shared/components/markdown-editor";
import { desktopApi } from "../../shared/network/desktop-api";
import type {
    AgentSkillDefinition,
    AgentSkillUpdateRequest,
} from "../../shared/network/contracts";
import { useAuthStore } from "../auth/auth-store";
import { useResolvedAppearance } from "../shell/appearance-store";

interface SkillFormValues {
    name: string;
    description: string;
    content: string;
}

interface SkillUploadPreview {
    name: string;
    description: string;
    references: number;
    assets: number;
    scripts: number;
}

const skillNamePattern = /^[a-z0-9](?:[a-z0-9]*-[a-z0-9])*[a-z0-9]*$/;

const parseSkillMarkdown = (
    content: string,
    t: TFunction,
): Pick<SkillUploadPreview, "name" | "description"> => {
    const frontmatter = content.match(/^---\s*\n([\s\S]*?)\n---\s*\n/);
    if (!frontmatter)
        throw new Error(t("skills.validation.missingFrontmatter"));
    const readField = (field: string): string =>
        frontmatter[1]
            .split("\n")
            .map((line) => line.match(/^\s*([^:]+):\s*(.*)\s*$/))
            .find(
                (match) => match?.[1].trim().toLocaleLowerCase() === field,
            )?.[2]
            .trim() ?? "";
    const name = readField("name");
    const description = readField("description");
    if (!name || !description)
        throw new Error(t("skills.validation.missingFields"));
    if (name.length > 64 || !skillNamePattern.test(name)) {
        throw new Error(
            t("skills.validation.invalidName", {
                message: t("skills.validation.namePattern"),
            }),
        );
    }
    return { name, description };
};

const createUploadPreview = async (
    file: File,
    t: TFunction,
): Promise<SkillUploadPreview> => {
    if (file.name.toLocaleLowerCase().endsWith(".md")) {
        return {
            ...parseSkillMarkdown(await file.text(), t),
            references: 0,
            assets: 0,
            scripts: 0,
        };
    }
    const archive = await JSZip.loadAsync(file);
    const files = Object.values(archive.files).filter((entry) => !entry.dir);
    const markdownFiles = files.filter((entry) =>
        /(^|\/)SKILL\.md$/i.test(entry.name),
    );
    if (markdownFiles.length !== 1)
        throw new Error(t("skills.validation.archiveSkillFile"));
    const markdown = markdownFiles[0];
    const root = markdown.name.slice(0, -"SKILL.md".length);
    const relativePaths = files
        .filter((entry) => entry !== markdown)
        .map((entry) => {
            if (!entry.name.startsWith(root))
                throw new Error(t("skills.validation.filesOutsideRoot"));
            return entry.name.slice(root.length);
        });
    const count = (folder: string): number =>
        relativePaths.filter((path) =>
            path.toLocaleLowerCase().startsWith(`${folder}/`),
        ).length;
    if (
        relativePaths.some(
            (path) => !/^(references|assets|scripts)\//i.test(path),
        )
    ) {
        throw new Error(t("skills.validation.unsupportedFolder"));
    }
    return {
        ...parseSkillMarkdown(await markdown.async("text"), t),
        references: count("references"),
        assets: count("assets"),
        scripts: count("scripts"),
    };
};

const formatTime = (value: string, locale: string): string =>
    new Intl.DateTimeFormat(locale, {
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
        hour12: false,
    }).format(new Date(value));

export function SkillManagement() {
    const { t, i18n } = useTranslation();
    const { token } = theme.useToken();
    const appearance = useResolvedAppearance();
    const identity = useAuthStore((state) => state.identity);
    const queryClient = useQueryClient();
    const [form] = Form.useForm<SkillFormValues>();
    const [messageApi, messageContext] = message.useMessage();
    const [modalApi, modalContext] = Modal.useModal();
    const [searchText, setSearchText] = useState("");
    const [ownerFilter, setOwnerFilter] = useState("all");
    const [selectedSkill, setSelectedSkill] =
        useState<AgentSkillDefinition | null>(null);
    const [editing, setEditing] = useState(false);
    const [skillContentExpanded, setSkillContentExpanded] = useState(false);
    const [saving, setSaving] = useState(false);
    const [uploadOpen, setUploadOpen] = useState(false);
    const [uploading, setUploading] = useState(false);
    const [uploadFiles, setUploadFiles] = useState<UploadFile[]>([]);
    const [uploadPreview, setUploadPreview] =
        useState<SkillUploadPreview | null>(null);
    const [previewing, setPreviewing] = useState(false);
    const skillsQuery = useQuery({
        queryKey: ["skills"],
        queryFn: desktopApi.listSkills,
    });
    const normalizedSearch = searchText.trim().toLocaleLowerCase();
    const canManage = (skill: AgentSkillDefinition): boolean =>
        identity?.isAdmin === true || skill.ownerUserId === identity?.userId;
    const ownerLabel = (skill: AgentSkillDefinition): string =>
        skill.ownerUserId === identity?.userId
            ? (identity?.username ?? t("skills.owner.me"))
            : t("skills.owner.others");
    const skills = (skillsQuery.data ?? []).filter((skill) => {
        const matchesText =
            `${skill.name} ${skill.description} ${ownerLabel(skill)}`
                .toLocaleLowerCase()
                .includes(normalizedSearch);
        const matchesOwner =
            ownerFilter === "all" ||
            (ownerFilter === "mine"
                ? skill.ownerUserId === identity?.userId
                : skill.ownerUserId !== identity?.userId);
        return matchesText && matchesOwner;
    });
    const hasLongSkillContent =
        selectedSkill !== null &&
        (selectedSkill.content.length > 180 ||
            selectedSkill.content.split("\n").length > 8);

    const openSkill = (skill: AgentSkillDefinition, edit = false): void => {
        setSkillContentExpanded(false);
        setSelectedSkill(skill);
        setEditing(edit);
        form.setFieldsValue(skill);
    };

    const saveSkill = async (): Promise<void> => {
        if (!selectedSkill) return;
        const values = await form.validateFields();
        const request: AgentSkillUpdateRequest = values;
        setSaving(true);
        try {
            const updated = await desktopApi.updateSkill(
                selectedSkill.id,
                request,
            );
            queryClient.setQueryData<AgentSkillDefinition[]>(
                ["skills"],
                (current) =>
                    current?.map((item) =>
                        item.id === updated.id ? updated : item,
                    ),
            );
            setSelectedSkill(updated);
            setEditing(false);
            messageApi.success(t("skills.messages.saved"));
        } catch {
            messageApi.error(t("skills.messages.saveFailed"));
        } finally {
            setSaving(false);
        }
    };

    const deleteSkill = (skill: AgentSkillDefinition): void => {
        modalApi.confirm({
            title: t("skills.deleteDialog.title", { name: skill.name }),
            content: t("skills.deleteDialog.content"),
            okText: t("skills.deleteDialog.confirm"),
            okButtonProps: { danger: true },
            cancelText: t("common.cancel"),
            onOk: async () => {
                try {
                    await desktopApi.deleteSkill(skill.id);
                    queryClient.setQueryData<AgentSkillDefinition[]>(
                        ["skills"],
                        (current) =>
                            current?.filter((item) => item.id !== skill.id),
                    );
                    setSelectedSkill(null);
                    setEditing(false);
                    messageApi.success(t("skills.messages.deleted"));
                } catch {
                    messageApi.error(t("skills.messages.deleteFailed"));
                    throw new Error("Skill deletion failed");
                }
            },
        });
    };

    const updateUploadFiles = async (files: UploadFile[]): Promise<void> => {
        setUploadFiles(files);
        setUploadPreview(null);
        const file = files[0]?.originFileObj;
        if (!file) return;
        setPreviewing(true);
        try {
            setUploadPreview(await createUploadPreview(file, t));
        } catch (error) {
            messageApi.error(
                error instanceof Error
                    ? error.message
                    : t("skills.messages.parseFailed"),
            );
        } finally {
            setPreviewing(false);
        }
    };

    const uploadSkill = async (): Promise<void> => {
        const file = uploadFiles[0]?.originFileObj;
        if (!file) return;
        setUploading(true);
        try {
            const created = await desktopApi.uploadSkill({
                name: file.name,
                bytes: new Uint8Array(await file.arrayBuffer()),
            });
            queryClient.setQueryData<AgentSkillDefinition[]>(
                ["skills"],
                (current) => [created, ...(current ?? [])],
            );
            setUploadOpen(false);
            setUploadFiles([]);
            messageApi.success(t("skills.messages.uploaded"));
        } catch {
            messageApi.error(t("skills.messages.uploadFailed"));
        } finally {
            setUploading(false);
        }
    };

    return (
        <DataListPage<AgentSkillDefinition>
            title={t("skills.title")}
            description={t("skills.description")}
            primaryAction={
                <Button
                    type="primary"
                    icon={<PlusOutlined />}
                    onClick={() => setUploadOpen(true)}
                >
                    {t("skills.upload")}
                </Button>
            }
            filters={
                <Select
                    value={ownerFilter}
                    onChange={setOwnerFilter}
                    style={{ width: 132 }}
                    options={[
                        { value: "all", label: t("skills.filters.all") },
                        { value: "mine", label: t("skills.filters.mine") },
                        {
                            value: "others",
                            label: t("skills.filters.others"),
                        },
                    ]}
                />
            }
            refreshLabel={t("skills.refreshLabel")}
            onRefresh={() => void skillsQuery.refetch()}
            refreshing={skillsQuery.isFetching && !skillsQuery.isLoading}
            searchValue={searchText}
            searchPlaceholder={t("skills.searchPlaceholder")}
            searchMaxLength={128}
            onSearchChange={setSearchText}
            paginationResetKey={`${searchText}:${ownerFilter}`}
            dataSource={skills}
            rowKey="id"
            tableScrollX={940}
            loading={skillsQuery.isLoading}
            errorMessage={
                skillsQuery.isError ? t("skills.loadFailed") : undefined
            }
            onRetry={() => void skillsQuery.refetch()}
            emptyText={t("skills.empty")}
            filteredEmptyText={t("skills.filteredEmpty")}
            isFiltered={normalizedSearch.length > 0 || ownerFilter !== "all"}
            columns={[
                {
                    title: t("skills.columns.name"),
                    dataIndex: "name",
                    width: 180,
                    ellipsis: true,
                },
                {
                    title: t("skills.columns.description"),
                    dataIndex: "description",
                    ellipsis: true,
                },
                {
                    title: t("skills.columns.owner"),
                    key: "owner",
                    width: 110,
                    render: (_, skill) => ownerLabel(skill),
                },
                {
                    title: t("skills.columns.resources"),
                    key: "files",
                    width: 190,
                    render: (_, skill) =>
                        t("skills.resourceSummary", {
                            references: skill.referenceFileUris.length,
                            assets: skill.assetFileUris.length,
                            scripts: skill.scriptFileUris.length,
                        }),
                },
                {
                    title: t("skills.columns.updatedTime"),
                    dataIndex: "updatedTime",
                    width: 162,
                    render: (value: string) => formatTime(value, i18n.language),
                },
                {
                    title: t("skills.columns.actions"),
                    key: "actions",
                    width: 244,
                    fixed: "right",
                    align: "center",
                    className: "inkwell-action-column",
                    render: (_, skill) => (
                        <DataListRowActions>
                            <DataListRowAction
                                label={t("skills.actions.viewLabel", {
                                    name: skill.name,
                                })}
                                text={t("common.view")}
                                icon={<EyeOutlined />}
                                onClick={() => openSkill(skill)}
                            />
                            {canManage(skill) && (
                                <>
                                    <DataListRowAction
                                        label={t("skills.actions.editLabel", {
                                            name: skill.name,
                                        })}
                                        text={t("common.edit")}
                                        icon={<EditOutlined />}
                                        onClick={() => openSkill(skill, true)}
                                    />
                                    <DataListRowAction
                                        label={t("skills.actions.deleteLabel", {
                                            name: skill.name,
                                        })}
                                        text={t("common.delete")}
                                        icon={<DeleteOutlined />}
                                        danger
                                        onClick={() => deleteSkill(skill)}
                                    />
                                </>
                            )}
                        </DataListRowActions>
                    ),
                },
            ]}
        >
            {messageContext}
            {modalContext}
            <Drawer
                width={600}
                title={
                    editing
                        ? t("skills.details.editTitle")
                        : t("skills.details.title")
                }
                closable={false}
                open={selectedSkill !== null}
                onClose={() => {
                    setSelectedSkill(null);
                    setEditing(false);
                }}
                extra={
                    selectedSkill ? (
                        <Tooltip title={t("common.close")}>
                            <Button
                                type="text"
                                aria-label={t("skills.details.closeLabel")}
                                icon={<CloseOutlined />}
                                onClick={() => {
                                    setSelectedSkill(null);
                                    setEditing(false);
                                }}
                            />
                        </Tooltip>
                    ) : null
                }
                footer={
                    editing ? (
                        <div style={{ textAlign: "right" }}>
                            <Space>
                                <Button
                                    onClick={() => {
                                        form.setFieldsValue(
                                            selectedSkill ?? {},
                                        );
                                        setEditing(false);
                                    }}
                                >
                                    {t("common.cancel")}
                                </Button>
                                <Button
                                    type="primary"
                                    loading={saving}
                                    onClick={() => void saveSkill()}
                                >
                                    {t("common.save")}
                                </Button>
                            </Space>
                        </div>
                    ) : null
                }
                className="skill-details-drawer"
                styles={{ body: { padding: editing ? 24 : 0 } }}
            >
                {selectedSkill && editing && (
                    <Form form={form} layout="vertical">
                        <Form.Item
                            label={t("skills.details.machineName")}
                            name="name"
                            extra={t("skills.details.machineNameHelp")}
                            rules={[
                                {
                                    required: true,
                                    message: t(
                                        "skills.details.machineNameRequired",
                                    ),
                                },
                                {
                                    max: 64,
                                    message: t(
                                        "skills.details.machineNameTooLong",
                                    ),
                                },
                                {
                                    pattern: skillNamePattern,
                                    message: t("skills.validation.namePattern"),
                                },
                            ]}
                        >
                            <Input
                                maxLength={64}
                                showCount
                                placeholder={t(
                                    "skills.details.machineNamePlaceholder",
                                )}
                            />
                        </Form.Item>
                        <Form.Item
                            label={t("skills.columns.description")}
                            name="description"
                            rules={[
                                {
                                    required: true,
                                    message: t(
                                        "skills.details.descriptionRequired",
                                    ),
                                },
                            ]}
                        >
                            <Input.TextArea
                                rows={3}
                                maxLength={240}
                                showCount
                            />
                        </Form.Item>
                        <Form.Item
                            label={t("skills.details.content")}
                            name="content"
                            className="skill-markdown-editor"
                            rules={[
                                {
                                    required: true,
                                    message: t(
                                        "skills.details.contentRequired",
                                    ),
                                },
                            ]}
                        >
                            <MarkdownEditor
                                aria-label={t("skills.details.content")}
                                appearance={appearance}
                            />
                        </Form.Item>
                        <Space size={24} wrap>
                            <Typography.Text type="secondary">
                                {t("skills.details.owner", {
                                    owner: ownerLabel(selectedSkill),
                                })}
                            </Typography.Text>
                            <Typography.Text type="secondary">
                                {t("skills.details.referenceCount", {
                                    count: selectedSkill.referenceFileUris
                                        .length,
                                })}
                            </Typography.Text>
                            <Typography.Text type="secondary">
                                {t("skills.details.assetCount", {
                                    count: selectedSkill.assetFileUris.length,
                                })}
                            </Typography.Text>
                            <Typography.Text type="secondary">
                                {t("skills.details.scriptCount", {
                                    count: selectedSkill.scriptFileUris.length,
                                })}
                            </Typography.Text>
                        </Space>
                    </Form>
                )}
                {selectedSkill && !editing && (
                    <div className="skill-details">
                        <div
                            className="agent-details-identity"
                            style={{
                                background: token.colorFillQuaternary,
                                borderBottom: `1px solid ${token.colorBorderSecondary}`,
                            }}
                        >
                            <Avatar
                                size={52}
                                icon={<ReadOutlined />}
                                style={{ background: token.colorPrimary }}
                            />
                            <div className="agent-details-identity-copy">
                                <Flex align="center" gap={8} wrap>
                                    <Typography.Title
                                        level={4}
                                        style={{ margin: 0 }}
                                    >
                                        {selectedSkill.name}
                                    </Typography.Title>
                                    <Tag color="processing">Skill</Tag>
                                    {selectedSkill.ownerUserId ===
                                        identity?.userId && (
                                        <Tag>{t("skills.owner.mine")}</Tag>
                                    )}
                                </Flex>
                                <Typography.Paragraph
                                    type="secondary"
                                    style={{ margin: "6px 0 0" }}
                                >
                                    {selectedSkill.description}
                                </Typography.Paragraph>
                                <Flex gap={16} wrap style={{ marginTop: 8 }}>
                                    <Typography.Text type="secondary">
                                        <UserOutlined />{" "}
                                        {ownerLabel(selectedSkill)}
                                    </Typography.Text>
                                    <Typography.Text type="secondary">
                                        <CalendarOutlined />{" "}
                                        {formatTime(
                                            selectedSkill.updatedTime,
                                            i18n.language,
                                        )}
                                    </Typography.Text>
                                </Flex>
                            </div>
                        </div>

                        <div className="agent-details-content">
                            <section className="agent-details-section">
                                <Flex
                                    align="center"
                                    justify="space-between"
                                    gap={12}
                                    className="agent-details-section-title"
                                >
                                    <Space size={8}>
                                        <FileTextOutlined />
                                        <Typography.Text strong>
                                            SKILL.md
                                        </Typography.Text>
                                    </Space>
                                    <Typography.Text type="secondary">
                                        {t("skills.details.characterCount", {
                                            count: selectedSkill.content.length,
                                        })}
                                    </Typography.Text>
                                </Flex>
                                <div
                                    className={`skill-details-markdown ${
                                        skillContentExpanded
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
                                        content={selectedSkill.content}
                                    />
                                </div>
                                {hasLongSkillContent && (
                                    <Button
                                        type="link"
                                        size="small"
                                        className="agent-details-instructions-toggle"
                                        icon={
                                            skillContentExpanded ? (
                                                <UpOutlined />
                                            ) : (
                                                <DownOutlined />
                                            )
                                        }
                                        onClick={() =>
                                            setSkillContentExpanded(
                                                (current) => !current,
                                            )
                                        }
                                    >
                                        {skillContentExpanded
                                            ? t("skills.details.collapse")
                                            : t("skills.details.expand")}
                                    </Button>
                                )}
                            </section>

                            <section className="agent-details-section">
                                <Space
                                    size={8}
                                    className="agent-details-section-title"
                                >
                                    <FolderOpenOutlined />
                                    <Typography.Text strong>
                                        {t("skills.details.resources")}
                                    </Typography.Text>
                                </Space>
                                <div className="skill-resource-grid">
                                    <div className="skill-resource-item">
                                        <FileTextOutlined />
                                        <Typography.Text strong>
                                            {
                                                selectedSkill.referenceFileUris
                                                    .length
                                            }
                                        </Typography.Text>
                                        <Typography.Text type="secondary">
                                            {t("skills.details.references")}
                                        </Typography.Text>
                                    </div>
                                    <div className="skill-resource-item">
                                        <FolderOpenOutlined />
                                        <Typography.Text strong>
                                            {selectedSkill.assetFileUris.length}
                                        </Typography.Text>
                                        <Typography.Text type="secondary">
                                            {t("skills.details.assets")}
                                        </Typography.Text>
                                    </div>
                                    <div className="skill-resource-item">
                                        <FileTextOutlined />
                                        <Typography.Text strong>
                                            {
                                                selectedSkill.scriptFileUris
                                                    .length
                                            }
                                        </Typography.Text>
                                        <Typography.Text type="secondary">
                                            {t("skills.details.scripts")}
                                        </Typography.Text>
                                    </div>
                                </div>
                            </section>

                            <section className="agent-details-section">
                                <Space
                                    size={8}
                                    className="agent-details-section-title"
                                >
                                    <CalendarOutlined />
                                    <Typography.Text strong>
                                        {t("skills.details.timeInformation")}
                                    </Typography.Text>
                                </Space>
                                <Descriptions
                                    size="small"
                                    column={1}
                                    items={[
                                        {
                                            key: "created",
                                            label: t(
                                                "skills.details.createdTime",
                                            ),
                                            children: formatTime(
                                                selectedSkill.createdTime,
                                                i18n.language,
                                            ),
                                        },
                                        {
                                            key: "updated",
                                            label: t(
                                                "skills.details.updatedTime",
                                            ),
                                            children: formatTime(
                                                selectedSkill.updatedTime,
                                                i18n.language,
                                            ),
                                        },
                                    ]}
                                />
                            </section>
                        </div>
                    </div>
                )}
            </Drawer>
            <Modal
                title={t("skills.uploadDialog.title")}
                open={uploadOpen}
                width={600}
                okText={t("skills.uploadDialog.start")}
                cancelText={t("common.cancel")}
                confirmLoading={uploading}
                okButtonProps={{
                    disabled: uploadPreview === null || previewing,
                }}
                onCancel={() => {
                    setUploadOpen(false);
                    setUploadFiles([]);
                    setUploadPreview(null);
                }}
                onOk={() => void uploadSkill()}
            >
                <Upload.Dragger
                    beforeUpload={() => false}
                    accept=".zip,.md"
                    maxCount={1}
                    fileList={uploadFiles}
                    onChange={({ fileList }) =>
                        void updateUploadFiles(fileList)
                    }
                >
                    <p className="ant-upload-drag-icon">
                        <InboxOutlined />
                    </p>
                    <p className="ant-upload-text">
                        {t("skills.uploadDialog.selectFile")}
                    </p>
                    <p className="ant-upload-hint">
                        {t("skills.uploadDialog.hint")}
                    </p>
                </Upload.Dragger>
                {previewing && (
                    <Typography.Text type="secondary">
                        {t("skills.uploadDialog.parsing")}
                    </Typography.Text>
                )}
                {uploadPreview && (
                    <div style={{ marginTop: 16 }}>
                        <Typography.Title level={5}>
                            {t("skills.uploadDialog.preview")}
                        </Typography.Title>
                        <Descriptions size="small" column={1} bordered>
                            <Descriptions.Item
                                label={t("skills.uploadDialog.machineName")}
                            >
                                {uploadPreview.name}
                            </Descriptions.Item>
                            <Descriptions.Item
                                label={t("skills.uploadDialog.description")}
                            >
                                {uploadPreview.description}
                            </Descriptions.Item>
                            <Descriptions.Item
                                label={t(
                                    "skills.uploadDialog.packageResources",
                                )}
                            >
                                {t("skills.uploadDialog.packageSummary", {
                                    references: uploadPreview.references,
                                    assets: uploadPreview.assets,
                                    scripts: uploadPreview.scripts,
                                })}
                            </Descriptions.Item>
                        </Descriptions>
                    </div>
                )}
            </Modal>
        </DataListPage>
    );
}
