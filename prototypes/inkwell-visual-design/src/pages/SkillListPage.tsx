import { useState } from "react";
import {
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
} from "antd";
import {
    DeleteOutlined,
    EditOutlined,
    EyeOutlined,
    InboxOutlined,
    PlusOutlined,
} from "@ant-design/icons";
import ResourceListPage, {
    ResourceRowAction,
    ResourceRowActions,
} from "../components/ResourceListPage";

interface SkillItem {
    key: string;
    name: string;
    description: string;
    content: string;
    owner: string;
    references: number;
    assets: number;
    scripts: number;
    updatedTime: string;
}

const CURRENT_USER = "alice";
const INITIAL_SKILLS: SkillItem[] = [
    {
        key: "contract-review",
        name: "合同审查规范",
        description: "按团队法务标准识别合同风险并输出分级建议。",
        content:
            "# 合同审查规范\n\n先识别合同类型，再按高、中、低三级输出风险。每项风险必须引用原文。",
        owner: CURRENT_USER,
        references: 4,
        assets: 1,
        scripts: 2,
        updatedTime: "2026-07-17 17:20",
    },
    {
        key: "weekly-report",
        name: "研发周报",
        description: "将工作记录整理为统一的研发周报格式。",
        content: "# 研发周报\n\n按本周完成、风险、下周计划三个章节组织内容。",
        owner: "bob",
        references: 2,
        assets: 0,
        scripts: 0,
        updatedTime: "2026-07-16 10:08",
    },
    {
        key: "incident-response",
        name: "故障复盘",
        description: "引导完成事实、时间线、根因与行动项复盘。",
        content: "# 故障复盘\n\n区分直接原因与系统性根因，不以责任人代替根因。",
        owner: CURRENT_USER,
        references: 3,
        assets: 2,
        scripts: 1,
        updatedTime: "2026-07-11 08:35",
    },
    ...Array.from(
        { length: 20 },
        (_, index): SkillItem => ({
            key: `skill-${index + 1}`,
            name:
                ["需求澄清", "会议纪要", "内容校对", "数据分析"][index % 4] +
                ` ${index + 1}`,
            description: [
                "提供团队统一的处理步骤和输出格式。",
                "在回答前加载相关参考资料并校验关键事实。",
                "把非结构化输入整理为可执行结果。",
            ][index % 3],
            content: `# ${["需求澄清", "会议纪要", "内容校对", "数据分析"][index % 4]}\n\n遵循团队规范完成任务，并明确标记不确定信息。`,
            owner:
                index % 3 === 0
                    ? CURRENT_USER
                    : index % 3 === 1
                      ? "bob"
                      : "carol",
            references: index % 5,
            assets: index % 3,
            scripts: index % 4 === 0 ? 1 : 0,
            updatedTime: `2026-06-${String(28 - (index % 20)).padStart(2, "0")} 13:10`,
        }),
    ),
];

export default function SkillListPage({ isSuper }: { isSuper: boolean }) {
    const [skills, setSkills] = useState(INITIAL_SKILLS);
    const [searchText, setSearchText] = useState("");
    const [owner, setOwner] = useState("all");
    const [selectedSkill, setSelectedSkill] = useState<SkillItem | null>(null);
    const [editing, setEditing] = useState(false);
    const [uploadOpen, setUploadOpen] = useState(false);
    const [uploadSelected, setUploadSelected] = useState(false);
    const [form] = Form.useForm();
    const [messageApi, contextHolder] = message.useMessage();
    const [modalApi, modalContextHolder] = Modal.useModal();

    const canManage = (skill: SkillItem) =>
        isSuper || skill.owner === CURRENT_USER;
    const filteredSkills = skills.filter((skill) => {
        const matchesText = `${skill.name} ${skill.description} ${skill.owner}`
            .toLowerCase()
            .includes(searchText.trim().toLowerCase());
        const matchesOwner =
            owner === "all" ||
            (owner === "mine"
                ? skill.owner === CURRENT_USER
                : skill.owner !== CURRENT_USER);
        return matchesText && matchesOwner;
    });

    const openDetail = (skill: SkillItem, edit = false) => {
        setSelectedSkill(skill);
        setEditing(edit);
        form.setFieldsValue(skill);
    };

    const deleteSkill = (skill: SkillItem) => {
        modalApi.confirm({
            title: `删除「${skill.name}」`,
            content:
                "删除不可恢复。新配置将无法再选择它；已保存草稿与已发布版本继续使用各自 Snapshot。",
            okText: "确认删除",
            okButtonProps: { danger: true },
            cancelText: "取消",
            onOk: () => {
                setSkills((current) =>
                    current.filter((item) => item.key !== skill.key),
                );
                setSelectedSkill(null);
                messageApi.success("Skill 已删除");
            },
        });
    };

    const saveSkill = async () => {
        const values = await form.validateFields();
        if (!selectedSkill) return;
        const updated = { ...selectedSkill, ...values, updatedTime: "刚刚" };
        setSkills((current) =>
            current.map((item) => (item.key === updated.key ? updated : item)),
        );
        setSelectedSkill(updated);
        setEditing(false);
        messageApi.success("Skill 已保存");
    };

    return (
        <ResourceListPage<SkillItem>
            title="Skills"
            description="查看和管理 Agent 的 Skill。Skill 通过任务说明和参考资料，教 Agent 如何完成特定工作。"
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
                    value={owner}
                    onChange={setOwner}
                    style={{ width: 132 }}
                    options={[
                        { value: "all", label: "全部归属" },
                        { value: "mine", label: "我上传的" },
                        { value: "others", label: "其他成员" },
                    ]}
                />
            }
            refreshLabel="刷新 Skills"
            searchValue={searchText}
            searchPlaceholder="搜索名称、描述或所有者"
            onSearchChange={setSearchText}
            paginationResetKey={`${searchText}:${owner}`}
            dataSource={filteredSkills}
            rowKey="key"
            tableScrollX={940}
            totalLabel={(total) => `共 ${total} 个 Skill`}
            columns={[
                {
                    title: "名称",
                    dataIndex: "name",
                    width: 180,
                    ellipsis: true,
                },
                { title: "描述", dataIndex: "description", ellipsis: true },
                { title: "所有者", dataIndex: "owner", width: 110 },
                {
                    title: "资料",
                    key: "files",
                    width: 190,
                    render: (_, skill) =>
                        `${skill.references} 引用 · ${skill.assets} 素材 · ${skill.scripts} 脚本`,
                },
                { title: "更新时间", dataIndex: "updatedTime", width: 162 },
                {
                    title: "操作",
                    key: "actions",
                    width: 164,
                    fixed: "right",
                    align: "center",
                    className: "inkwell-action-column",
                    render: (_, skill) => (
                        <ResourceRowActions>
                            <ResourceRowAction
                                label={`查看 ${skill.name}`}
                                text="查看"
                                icon={<EyeOutlined />}
                                onClick={() => openDetail(skill)}
                            />
                            {canManage(skill) && (
                                <ResourceRowAction
                                    label={`编辑 ${skill.name}`}
                                    text="编辑"
                                    icon={<EditOutlined />}
                                    onClick={() => openDetail(skill, true)}
                                />
                            )}
                        </ResourceRowActions>
                    ),
                },
            ]}
        >
            {contextHolder}
            {modalContextHolder}
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
                                        setSelectedSkill(null);
                                        setEditing(false);
                                    }}
                                >
                                    取消
                                </Button>
                                <Button type="primary" onClick={saveSkill}>
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
                        <Space size={24} wrap>
                            <Typography.Text type="secondary">
                                所有者：{selectedSkill.owner}
                            </Typography.Text>
                            <Typography.Text type="secondary">
                                引用：{selectedSkill.references} 个（只读）
                            </Typography.Text>
                            <Typography.Text type="secondary">
                                素材：{selectedSkill.assets} 个（只读）
                            </Typography.Text>
                            <Typography.Text type="secondary">
                                脚本：{selectedSkill.scripts} 个（只读）
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
                okButtonProps={{ disabled: !uploadSelected }}
                onCancel={() => {
                    setUploadOpen(false);
                    setUploadSelected(false);
                }}
                onOk={() => {
                    setUploadOpen(false);
                    setUploadSelected(false);
                    messageApi.success(
                        "原型演示：Skill 已通过结构校验并加入列表",
                    );
                }}
            >
                <Upload.Dragger
                    beforeUpload={() => false}
                    accept=".zip,.md"
                    maxCount={1}
                    onChange={({ fileList }) =>
                        setUploadSelected(fileList.length > 0)
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
                        frontmatter，上传后可在详情中编辑。 支持
                        references/、assets/ 和 scripts/。
                    </p>
                </Upload.Dragger>
                {uploadSelected && (
                    <div style={{ marginTop: 16 }}>
                        <Typography.Title level={5}>
                            SKILL.md 解析预览
                        </Typography.Title>
                        <Descriptions size="small" column={1} bordered>
                            <Descriptions.Item label="名称">
                                合同审查规范
                            </Descriptions.Item>
                            <Descriptions.Item label="描述">
                                按团队法务标准识别合同风险并输出分级建议。
                            </Descriptions.Item>
                            <Descriptions.Item label="包内资源">
                                4 个 references · 1 个 asset · 2 个 scripts
                            </Descriptions.Item>
                        </Descriptions>
                    </div>
                )}
            </Modal>
        </ResourceListPage>
    );
}
