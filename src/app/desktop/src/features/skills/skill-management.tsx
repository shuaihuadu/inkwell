import {
    DeleteOutlined,
    EditOutlined,
    EyeOutlined,
    InboxOutlined,
    PlusOutlined,
} from "@ant-design/icons";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
    Alert,
    Button,
    Descriptions,
    Drawer,
    Form,
    Input,
    Modal,
    Select,
    Space,
    Typography,
    Upload,
    message,
    type UploadFile,
} from "antd";
import JSZip from "jszip";
import { useState } from "react";
import DataListPage, {
    DataListRowAction,
    DataListRowActions,
} from "../../shared/components/data-list-page";
import { desktopApi } from "../../shared/network/desktop-api";
import type {
    AgentSkillDefinition,
    AgentSkillUpdateRequest,
} from "../../shared/network/contracts";
import { useAuthStore } from "../auth/auth-store";

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

const parseSkillMarkdown = (
    content: string,
): Pick<SkillUploadPreview, "name" | "description"> => {
    const frontmatter = content.match(/^---\s*\n([\s\S]*?)\n---\s*\n/);
    if (!frontmatter) throw new Error("SKILL.md 缺少有效的 YAML frontmatter");
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
    if (!name || !description) throw new Error("SKILL.md 必须包含名称和描述");
    return { name, description };
};

const createUploadPreview = async (file: File): Promise<SkillUploadPreview> => {
    if (file.name.toLocaleLowerCase().endsWith(".md")) {
        return {
            ...parseSkillMarkdown(await file.text()),
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
        throw new Error("压缩包必须包含且只包含一个 SKILL.md");
    const markdown = markdownFiles[0];
    const root = markdown.name.slice(0, -"SKILL.md".length);
    const relativePaths = files
        .filter((entry) => entry !== markdown)
        .map((entry) => {
            if (!entry.name.startsWith(root))
                throw new Error("所有文件必须位于 SKILL.md 所在文件夹内");
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
        throw new Error(
            "包内文件只能放在 references、assets 或 scripts 文件夹",
        );
    }
    return {
        ...parseSkillMarkdown(await markdown.async("text")),
        references: count("references"),
        assets: count("assets"),
        scripts: count("scripts"),
    };
};

const formatTime = (value: string): string =>
    new Intl.DateTimeFormat("zh-CN", {
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
        hour12: false,
    }).format(new Date(value));

export function SkillManagement() {
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
        identity?.isSuper === true || skill.ownerUserId === identity?.userId;
    const ownerLabel = (skill: AgentSkillDefinition): string =>
        skill.ownerUserId === identity?.userId
            ? (identity?.username ?? "我")
            : "其他成员";
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

    const openSkill = (skill: AgentSkillDefinition, edit = false): void => {
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
            messageApi.success("Skill 已保存");
        } catch {
            messageApi.error("Skill 保存失败，请稍后重试");
        } finally {
            setSaving(false);
        }
    };

    const deleteSkill = (skill: AgentSkillDefinition): void => {
        modalApi.confirm({
            title: `删除「${skill.name}」`,
            content:
                "删除后，新配置将无法再选择此 Skill；已保存草稿与已发布版本继续使用各自 Snapshot。确认删除？",
            okText: "确认删除",
            okButtonProps: { danger: true },
            cancelText: "取消",
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
                    messageApi.success("Skill 已删除");
                } catch {
                    messageApi.error("Skill 删除失败，请稍后重试");
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
            setUploadPreview(await createUploadPreview(file));
        } catch (error) {
            messageApi.error(
                error instanceof Error ? error.message : "Skill 文件无法解析",
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
            messageApi.success("Skill 已上传");
        } catch {
            messageApi.error("Skill 上传失败，请检查文件结构");
        } finally {
            setUploading(false);
        }
    };

    return (
        <DataListPage<AgentSkillDefinition>
            title="Skills"
            description="管理团队共享的静态知识与指令。保存 Agent 时，完整内容进入 Snapshot。"
            primaryAction={
                <Button
                    type="primary"
                    icon={<PlusOutlined />}
                    onClick={() => setUploadOpen(true)}
                >
                    上传 Skill
                </Button>
            }
            filters={
                <Select
                    value={ownerFilter}
                    onChange={setOwnerFilter}
                    style={{ width: 132 }}
                    options={[
                        { value: "all", label: "全部归属" },
                        { value: "mine", label: "我上传的" },
                        { value: "others", label: "其他成员" },
                    ]}
                />
            }
            refreshLabel="刷新 Skills"
            onRefresh={() => void skillsQuery.refetch()}
            refreshing={skillsQuery.isFetching && !skillsQuery.isLoading}
            searchValue={searchText}
            searchPlaceholder="搜索名称、描述或所有者"
            searchMaxLength={128}
            onSearchChange={setSearchText}
            paginationResetKey={`${searchText}:${ownerFilter}`}
            dataSource={skills}
            rowKey="id"
            tableScrollX={940}
            totalLabel={(total) => `共 ${total} 个 Skill`}
            loading={skillsQuery.isLoading}
            errorMessage={
                skillsQuery.isError ? "Skills 加载失败，请稍后重试" : undefined
            }
            onRetry={() => void skillsQuery.refetch()}
            emptyText="还没有 Skill"
            filteredEmptyText="在所选条件内没有结果，请清除筛选"
            isFiltered={normalizedSearch.length > 0 || ownerFilter !== "all"}
            columns={[
                {
                    title: "名称",
                    dataIndex: "name",
                    width: 180,
                    ellipsis: true,
                },
                { title: "描述", dataIndex: "description", ellipsis: true },
                {
                    title: "所有者",
                    key: "owner",
                    width: 110,
                    render: (_, skill) => ownerLabel(skill),
                },
                {
                    title: "资料",
                    key: "files",
                    width: 190,
                    render: (_, skill) =>
                        `${skill.referenceFileUris.length} 引用 · ${skill.assetFileUris.length} 素材 · ${skill.scriptFileUris.length} 脚本`,
                },
                {
                    title: "更新时间",
                    dataIndex: "updatedTime",
                    width: 162,
                    render: formatTime,
                },
                {
                    title: "操作",
                    key: "actions",
                    width: 164,
                    fixed: "right",
                    align: "center",
                    className: "inkwell-action-column",
                    render: (_, skill) => (
                        <DataListRowActions>
                            <DataListRowAction
                                label={`查看 ${skill.name}`}
                                text="查看"
                                icon={<EyeOutlined />}
                                onClick={() => openSkill(skill)}
                            />
                            {canManage(skill) && (
                                <DataListRowAction
                                    label={`编辑 ${skill.name}`}
                                    text="编辑"
                                    icon={<EditOutlined />}
                                    onClick={() => openSkill(skill, true)}
                                />
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
                title={editing ? "编辑 Skill" : "Skill 详情"}
                open={selectedSkill !== null}
                onClose={() => {
                    setSelectedSkill(null);
                    setEditing(false);
                }}
                extra={
                    selectedSkill && canManage(selectedSkill) ? (
                        <Space>
                            {!editing && (
                                <Button
                                    icon={<EditOutlined />}
                                    onClick={() => setEditing(true)}
                                >
                                    编辑
                                </Button>
                            )}
                            <Button
                                danger
                                type="text"
                                aria-label="删除 Skill"
                                icon={<DeleteOutlined />}
                                onClick={() => deleteSkill(selectedSkill)}
                            />
                        </Space>
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
                                    取消
                                </Button>
                                <Button
                                    type="primary"
                                    loading={saving}
                                    onClick={() => void saveSkill()}
                                >
                                    保存
                                </Button>
                            </Space>
                        </div>
                    ) : null
                }
            >
                {selectedSkill && (
                    <Form form={form} layout="vertical" disabled={!editing}>
                        <Form.Item
                            label="名称"
                            name="name"
                            rules={[{ required: true, message: "请输入名称" }]}
                        >
                            <Input maxLength={80} showCount />
                        </Form.Item>
                        <Form.Item
                            label="描述"
                            name="description"
                            rules={[{ required: true, message: "请输入描述" }]}
                        >
                            <Input.TextArea
                                rows={3}
                                maxLength={240}
                                showCount
                            />
                        </Form.Item>
                        <Form.Item
                            label="SKILL.md 内容"
                            name="content"
                            rules={[{ required: true, message: "请输入内容" }]}
                        >
                            <Input.TextArea
                                rows={14}
                                className="inkwell-skill-editor"
                            />
                        </Form.Item>
                        {selectedSkill.scriptFileUris.length > 0 && (
                            <Alert
                                type="warning"
                                showIcon
                                message="脚本已保存，当前版本不会执行"
                                style={{ marginBottom: 16 }}
                            />
                        )}
                        <Space size={24} wrap>
                            <Typography.Text type="secondary">
                                所有者：{ownerLabel(selectedSkill)}
                            </Typography.Text>
                            <Typography.Text type="secondary">
                                引用：{selectedSkill.referenceFileUris.length}{" "}
                                个（只读）
                            </Typography.Text>
                            <Typography.Text type="secondary">
                                素材：{selectedSkill.assetFileUris.length}{" "}
                                个（只读）
                            </Typography.Text>
                            <Typography.Text type="secondary">
                                脚本：{selectedSkill.scriptFileUris.length}{" "}
                                个（只读）
                            </Typography.Text>
                        </Space>
                    </Form>
                )}
            </Drawer>
            <Modal
                title="上传 Skill"
                open={uploadOpen}
                width={600}
                okText="开始上传"
                cancelText="取消"
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
                        选择 Skill 文件夹压缩包或 SKILL.md
                    </p>
                    <p className="ant-upload-hint">
                        名称和描述读取自 SKILL.md 的 YAML
                        frontmatter，上传后可在详情中编辑。支持
                        references/、assets/ 和 scripts/。
                    </p>
                </Upload.Dragger>
                {previewing && (
                    <Typography.Text type="secondary">
                        正在解析 SKILL.md...
                    </Typography.Text>
                )}
                {uploadPreview && (
                    <div style={{ marginTop: 16 }}>
                        <Typography.Title level={5}>
                            SKILL.md 解析预览
                        </Typography.Title>
                        <Descriptions size="small" column={1} bordered>
                            <Descriptions.Item label="名称">
                                {uploadPreview.name}
                            </Descriptions.Item>
                            <Descriptions.Item label="描述">
                                {uploadPreview.description}
                            </Descriptions.Item>
                            <Descriptions.Item label="包内资源">
                                {uploadPreview.references} 个 references ·{" "}
                                {uploadPreview.assets} 个 asset ·{" "}
                                {uploadPreview.scripts} 个 scripts
                            </Descriptions.Item>
                        </Descriptions>
                    </div>
                )}
            </Modal>
        </DataListPage>
    );
}
