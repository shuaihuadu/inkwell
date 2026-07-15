import { useState } from "react";
import {
    Alert,
    Button,
    Card,
    Checkbox,
    Col,
    Flex,
    Form,
    Input,
    InputNumber,
    message,
    Modal,
    Popconfirm,
    Popover,
    Row,
    Select,
    Slider,
    Space,
    Switch,
    Table,
    Tag,
    Tooltip,
    Typography,
    Upload,
    theme as antdTheme,
    Segmented,
} from "antd";
import {
    SaveOutlined,
    CloudUploadOutlined,
    CloseOutlined,
    PlayCircleOutlined,
    DeleteOutlined,
    CopyOutlined,
    CameraOutlined,
    QuestionCircleOutlined,
    CheckCircleOutlined,
    CloseCircleOutlined,
    LoadingOutlined,
    SlidersOutlined,
    ToolOutlined,
    ReadOutlined,
    HistoryOutlined,
    UserOutlined,
    RobotOutlined,
    MenuFoldOutlined,
    MenuUnfoldOutlined,
    ArrowLeftOutlined,
    PlusOutlined,
    CommentOutlined,
    FileSearchOutlined,
    AppstoreAddOutlined,
    PaperClipOutlined,
} from "@ant-design/icons";
import {
    Bubble,
    Conversations,
    Prompts,
    Sender,
    Welcome,
    type ConversationItemType,
} from "@ant-design/x";
import { useMockChat, formatNow, type ChatMessage } from "../chat/useMockChat";
import { toBubbleItems, useChatBubbleRoles } from "../chat/chatBubbleRoles";
import { useAttachments } from "../chat/useAttachments";
import { ChatAttachmentsHeader } from "../chat/ChatAttachmentsHeader";
import { isHarnessTrigger, isAgentLoopTrigger, runHarnessDemo, runAgentLoopDemo } from "../chat/harnessDemo";

// ─── Types ────────────────────────────────────────────────────────────────────

/** ui-spec.md §4.2 */
type AgentState =
    | "editing"
    | "new-draft"
    | "draft-pending"
    | "readonly"
    | "submitting"
    | "submit-failed"
    | "submit-success";

type SectionKey =
    | "basic"
    | "instructions"
    | "model"
    | "tools"
    | "skills"
    | "version";

type Density = "standard" | "compact";

// ─── Section Config ───────────────────────────────────────────────────────────

const SECTIONS: { key: SectionKey; label: string; icon: React.ReactNode }[] = [
    { key: "basic", label: "基础信息", icon: <UserOutlined /> },
    { key: "instructions", label: "Instructions", icon: <RobotOutlined /> },
    { key: "model", label: "模型与参数", icon: <SlidersOutlined /> },
    { key: "tools", label: "工具", icon: <ToolOutlined /> },
    { key: "skills", label: "Skills", icon: <ReadOutlined /> },
    { key: "version", label: "版本", icon: <HistoryOutlined /> },
];

// ─── Mock Data ────────────────────────────────────────────────────────────────

const MOCK_MODELS = [
    {
        value: "azure-gpt4o",
        label: "Azure OpenAI GPT-4o",
        capabilities: ["视觉", "工具", "结构化输出"],
    },
    {
        value: "azure-gpt4o-mini",
        label: "Azure OpenAI GPT-4o-mini",
        capabilities: ["视觉", "工具"],
    },
    {
        value: "qwen-max",
        label: "Qwen Max（元数据待补齐）",
        capabilities: [],
        disabled: true,
    },
];

const MOCK_TOOLS = [
    { id: "t1", name: "当前时间", description: "获取系统当前时间" },
    {
        id: "t2",
        name: "HTTP 请求",
        description: "调用外部 HTTP 接口",
        params: ["URL", "方法"],
    },
    {
        id: "t3",
        name: "代码执行",
        description: "执行 Python/JS 代码片段",
        params: ["language", "timeout"],
    },
];

const MOCK_SKILLS = [
    {
        id: "s1",
        name: "ReportWriter",
        description: "生成报告 Skill，含模板与格式规范",
    },
    {
        id: "s2",
        name: "DataAnalyzer",
        description: "数据分析与图表生成",
    },
];

// ─── Section Components ───────────────────────────────────────────────────────

function SectionBasic({ readonly }: { readonly: boolean }) {
    const { token } = antdTheme.useToken();
    const [agentName, setAgentName] = useState("智能研究助手");
    const avatarText = agentName.trim().charAt(0) || "A";

    return (
        <Form layout="vertical">
            <Row gutter={[24, 20]} align="top">
                <Col flex="96px">
                    <Form.Item label="头像" style={{ marginBottom: 0 }}>
                        <Flex vertical align="center" gap={6}>
                            {readonly ? (
                                <div
                                    className="inkwell-agent-avatar"
                                    style={{
                                        background: `linear-gradient(145deg, ${token.colorPrimary}B8, ${token.colorPrimary})`,
                                        boxShadow: `0 8px 20px ${token.colorPrimary}28`,
                                    }}
                                >
                                    <Typography.Text
                                        style={{ color: "#fff", fontSize: 24, fontWeight: 700 }}
                                    >
                                        {avatarText}
                                    </Typography.Text>
                                </div>
                            ) : (
                                <Tooltip title="更换头像">
                                    <Upload
                                        showUploadList={false}
                                        accept="image/*"
                                    >
                                        <button
                                            type="button"
                                            className="inkwell-agent-avatar inkwell-agent-avatar-editor"
                                            aria-label="更换 Agent 头像"
                                            style={{
                                                background: `linear-gradient(145deg, ${token.colorPrimary}B8, ${token.colorPrimary})`,
                                                boxShadow: `0 8px 20px ${token.colorPrimary}28`,
                                            }}
                                        >
                                            <Typography.Text
                                                style={{ color: "#fff", fontSize: 24, fontWeight: 700 }}
                                            >
                                                {avatarText}
                                            </Typography.Text>
                                            <span className="inkwell-agent-avatar-editor-overlay">
                                                <CameraOutlined />
                                            </span>
                                        </button>
                                    </Upload>
                                </Tooltip>
                            )}
                            <Typography.Text
                                type="secondary"
                                style={{ fontSize: 11, whiteSpace: "nowrap" }}
                            >
                                {readonly ? "Agent 头像" : "点击更换"}
                            </Typography.Text>
                        </Flex>
                    </Form.Item>
                </Col>
                <Col flex="1 1 320px">
                    <Form.Item
                        label="Agent 名称"
                        name="name"
                        initialValue="智能研究助手"
                        rules={[
                            {
                                required: true,
                                message: "名称为必填，且长度需在 1–50 字符之间",
                            },
                            {
                                max: 50,
                                message: "名称为必填，且长度需在 1–50 字符之间",
                            },
                        ]}
                    >
                        <Input
                            placeholder="Agent 名称（1–50 字符）"
                            disabled={readonly}
                            onChange={(event) => setAgentName(event.target.value)}
                        />
                    </Form.Item>
                    <Form.Item
                        label="描述"
                        name="description"
                        initialValue="帮助团队进行深度研究、文献检索与报告生成的智能助手。支持多轮对话、工具调用和 Skills 扩展。"
                        style={{ marginBottom: 0 }}
                    >
                        <Input.TextArea
                            rows={5}
                            maxLength={500}
                            showCount
                            placeholder="简要描述这个 Agent 的职责和能力"
                            disabled={readonly}
                        />
                    </Form.Item>
                    <Form.Item
                        label="团队可见"
                        tooltip="对应 Agent 管理态的 IsShared，不随版本快照回滚。"
                        style={{ marginTop: 18, marginBottom: 0 }}
                    >
                        <Switch
                            defaultChecked
                            checkedChildren="已共享"
                            unCheckedChildren="仅自己"
                            disabled={readonly}
                        />
                    </Form.Item>
                </Col>
            </Row>
        </Form>
    );
}

function SectionInstructions({ readonly }: { readonly: boolean }) {
    const [charCount, setCharCount] = useState(420);
    const WARN_THRESHOLD = 32000;
    return (
        <Form layout="vertical">
            <Form.Item>
                {charCount >= WARN_THRESHOLD && (
                    <Alert
                        type="warning"
                        showIcon
                        className="inkwell-compact-alert"
                        message={
                            <span style={{ fontSize: 12 }}>
                                Instructions 较长，可能挤占模型上下文
                            </span>
                        }
                        style={{ marginBottom: 8 }}
                    />
                )}
                <Input.TextArea
                    aria-label="Instructions"
                    rows={20}
                    disabled={readonly}
                    defaultValue={`你是一名专业的研究助手，擅长以下工作：
1. 文献检索与摘要提炼
2. 数据分析与可视化建议
3. 报告框架设计与撰写

工作原则：
- 回答要有依据，引用来源
- 专业术语使用准确
- 遇到不确定的内容，明确说明而非猜测`}
                    onChange={(e) => setCharCount(e.target.value.length)}
                    showCount={{
                        formatter: ({ count }) =>
                            `${count} / ${WARN_THRESHOLD}`,
                    }}
                    placeholder="输入给 Agent 的系统指令…"
                />
            </Form.Item>
        </Form>
    );
}

function SectionModel({
    readonly,
    density,
}: {
    readonly: boolean;
    density: Density;
}) {
    const { token } = antdTheme.useToken();
    const [useDefaultTemp, setUseDefaultTemp] = useState(false);
    const [useDefaultTopP, setUseDefaultTopP] = useState(true);
    const [useDefaultTokens, setUseDefaultTokens] = useState(true);

    return (
        <Form layout="vertical">
            <div
                style={{
                    padding: density === "compact" ? 14 : 18,
                    marginBottom: 18,
                    border: `1px solid ${token.colorBorderSecondary}`,
                    borderRadius: 8,
                    background: token.colorFillQuaternary,
                }}
            >
                <Form.Item
                    label="运行模型"
                    name="model"
                    initialValue="azure-gpt4o"
                    style={{ marginBottom: 0 }}
                >
                    <Select
                        disabled={readonly}
                        options={MOCK_MODELS.map((model) => ({
                            value: model.value,
                            disabled: model.disabled,
                            label: (
                                <Space size={6} wrap>
                                    <span>{model.label}</span>
                                    {model.capabilities.map((capability) => (
                                        <Tag
                                            key={capability}
                                            style={{ marginInlineEnd: 0 }}
                                        >
                                            {capability}
                                        </Tag>
                                    ))}
                                </Space>
                            ),
                        }))}
                    />
                </Form.Item>
            </div>

            <Typography.Text
                strong
                style={{ display: "block", marginBottom: 4 }}
            >
                生成参数
            </Typography.Text>
            <Typography.Text
                type="secondary"
                style={{ display: "block", fontSize: 12, marginBottom: 14 }}
            >
                关闭“自定义”后使用模型默认值，不写入请求参数。
            </Typography.Text>

            <Row gutter={[16, 16]}>
                <Col xs={24} lg={8}>
                    <Card
                        size="small"
                        style={{ height: "100%", borderRadius: 8 }}
                    >
                        <div
                            style={{
                                display: "flex",
                                justifyContent: "space-between",
                                alignItems: "center",
                                marginBottom: 16,
                            }}
                        >
                            <div>
                                <Typography.Text strong>
                                    Temperature
                                </Typography.Text>
                                <Typography.Text
                                    type="secondary"
                                    style={{ display: "block", fontSize: 11 }}
                                >
                                    控制输出的随机性
                                </Typography.Text>
                            </div>
                            <Switch
                                size="small"
                                checked={!useDefaultTemp}
                                onChange={(custom) =>
                                    setUseDefaultTemp(!custom)
                                }
                                checkedChildren="自定义"
                                unCheckedChildren="默认"
                                disabled={readonly}
                            />
                        </div>
                        <div
                            style={{
                                display: "flex",
                                gap: 12,
                                alignItems: "center",
                            }}
                        >
                            <Slider
                                min={0}
                                max={2}
                                step={0.01}
                                defaultValue={0.7}
                                disabled={useDefaultTemp || readonly}
                                style={{ flex: 1 }}
                            />
                            <InputNumber
                                min={0}
                                max={2}
                                step={0.01}
                                defaultValue={0.7}
                                size="small"
                                disabled={useDefaultTemp || readonly}
                                style={{ width: 80 }}
                            />
                        </div>
                    </Card>
                </Col>
                <Col xs={24} lg={8}>
                    <Card
                        size="small"
                        style={{ height: "100%", borderRadius: 8 }}
                    >
                        <div
                            style={{
                                display: "flex",
                                justifyContent: "space-between",
                                alignItems: "center",
                                marginBottom: 16,
                            }}
                        >
                            <div>
                                <Typography.Text strong>Top P</Typography.Text>
                                <Typography.Text
                                    type="secondary"
                                    style={{ display: "block", fontSize: 11 }}
                                >
                                    限制候选词概率范围
                                </Typography.Text>
                            </div>
                            <Switch
                                size="small"
                                checked={!useDefaultTopP}
                                onChange={(custom) =>
                                    setUseDefaultTopP(!custom)
                                }
                                checkedChildren="自定义"
                                unCheckedChildren="默认"
                                disabled={readonly}
                            />
                        </div>
                        <div
                            style={{
                                display: "flex",
                                gap: 12,
                                alignItems: "center",
                            }}
                        >
                            <Slider
                                min={0}
                                max={1}
                                step={0.01}
                                defaultValue={1}
                                disabled={useDefaultTopP || readonly}
                                style={{ flex: 1 }}
                            />
                            <InputNumber
                                min={0}
                                max={1}
                                step={0.01}
                                defaultValue={1}
                                size="small"
                                disabled={useDefaultTopP || readonly}
                                style={{ width: 80 }}
                            />
                        </div>
                    </Card>
                </Col>
                <Col xs={24} lg={8}>
                    <Card
                        size="small"
                        style={{ height: "100%", borderRadius: 8 }}
                    >
                        <div
                            style={{
                                display: "flex",
                                justifyContent: "space-between",
                                alignItems: "center",
                                marginBottom: 16,
                            }}
                        >
                            <div>
                                <Typography.Text strong>
                                    Max Tokens
                                </Typography.Text>
                                <Typography.Text
                                    type="secondary"
                                    style={{ display: "block", fontSize: 11 }}
                                >
                                    限制单次输出长度
                                </Typography.Text>
                            </div>
                            <Switch
                                size="small"
                                checked={!useDefaultTokens}
                                onChange={(custom) =>
                                    setUseDefaultTokens(!custom)
                                }
                                checkedChildren="自定义"
                                unCheckedChildren="默认"
                                disabled={readonly}
                            />
                        </div>
                        <InputNumber
                            min={1}
                            max={128000}
                            defaultValue={4096}
                            disabled={useDefaultTokens || readonly}
                            style={{ width: "100%" }}
                        />
                    </Card>
                </Col>
            </Row>

            <Typography.Text
                strong
                style={{ display: "block", marginTop: 20, marginBottom: 4 }}
            >
                上下文
            </Typography.Text>
            <Typography.Text
                type="secondary"
                style={{ display: "block", fontSize: 12, marginBottom: 10 }}
            >
                超过该数量时，最早的历史消息会被裁剪，避免无限增长挤占模型上下文。
            </Typography.Text>
            <Form.Item
                label="最大消息记录数"
                style={{ marginBottom: 0 }}
            >
                <InputNumber
                    min={1}
                    max={500}
                    defaultValue={40}
                    suffix="条"
                    disabled={readonly}
                    style={{ width: 160 }}
                />
            </Form.Item>
        </Form>
    );
}

function SectionTools({
    readonly,
    density,
}: {
    readonly: boolean;
    density: Density;
}) {
    const [checked, setChecked] = useState<string[]>(["t1"]);
    return (
        <div>
            <Space
                direction="vertical"
                style={{ width: "100%" }}
                size={density === "compact" ? 8 : 12}
            >
                {MOCK_TOOLS.map((t) => (
                    <Card
                        key={t.id}
                        size="small"
                        style={{ borderRadius: 8 }}
                        styles={{
                            body: {
                                padding:
                                    density === "compact"
                                        ? "8px 12px"
                                        : "12px 16px",
                            },
                        }}
                    >
                        <div
                            style={{
                                display: "flex",
                                alignItems: "flex-start",
                                gap: 12,
                            }}
                        >
                            <Checkbox
                                checked={checked.includes(t.id)}
                                disabled={readonly}
                                onChange={(e) => {
                                    setChecked((prev) =>
                                        e.target.checked
                                            ? [...prev, t.id]
                                            : prev.filter((x) => x !== t.id),
                                    );
                                }}
                            />
                            <div style={{ flex: 1 }}>
                                <Typography.Text
                                    strong
                                    style={{ fontSize: 13 }}
                                >
                                    {t.name}
                                </Typography.Text>
                                <Typography.Text
                                    type="secondary"
                                    style={{ fontSize: 12, display: "block" }}
                                >
                                    {t.description}
                                </Typography.Text>
                                {checked.includes(t.id) && t.params && (
                                    <Form
                                        layout="inline"
                                        size="small"
                                        style={{ marginTop: 8 }}
                                    >
                                        {t.params.map((p) => (
                                            <Form.Item
                                                key={p}
                                                label={p}
                                                style={{ marginBottom: 0 }}
                                            >
                                                <Input
                                                    size="small"
                                                    style={{ width: 120 }}
                                                    disabled={readonly}
                                                />
                                            </Form.Item>
                                        ))}
                                    </Form>
                                )}
                            </div>
                        </div>
                    </Card>
                ))}
                {MOCK_TOOLS.length === 0 && (
                    <div style={{ textAlign: "center", padding: 32 }}>
                        <Typography.Text type="secondary">
                            还没有挂载任何工具
                        </Typography.Text>
                    </div>
                )}
            </Space>
        </div>
    );
}

function SectionSkills({
    readonly,
    density,
}: {
    readonly: boolean;
    density: Density;
}) {
    const { token } = antdTheme.useToken();
    const [checked, setChecked] = useState<string[]>(["s1"]);

    return (
        <div>
            <Typography.Text
                type="secondary"
                style={{ display: "block", fontSize: 12, marginBottom: 12 }}
            >
                从统一 Skill 管理中选择需要挂载到当前 Agent 的 Skill。
            </Typography.Text>

            <Space
                direction="vertical"
                style={{ width: "100%" }}
                size={density === "compact" ? 8 : 12}
            >
                {MOCK_SKILLS.map((s) => (
                    <Card
                        key={s.id}
                        size="small"
                        style={{ borderRadius: 8 }}
                        styles={{
                            body: {
                                padding:
                                    density === "compact"
                                        ? "8px 12px"
                                        : "12px 16px",
                            },
                        }}
                    >
                        <div
                            style={{
                                display: "flex",
                                alignItems: "flex-start",
                                gap: 12,
                            }}
                        >
                            <Checkbox
                                checked={checked.includes(s.id)}
                                disabled={readonly}
                                onChange={(e) => {
                                    setChecked((prev) =>
                                        e.target.checked
                                            ? [...prev, s.id]
                                            : prev.filter((x) => x !== s.id),
                                    );
                                }}
                            />
                            <div>
                                <Space>
                                    <Typography.Text
                                        strong
                                        style={{ fontSize: 13 }}
                                    >
                                        {s.name}
                                    </Typography.Text>
                                </Space>
                                <Typography.Text
                                    type="secondary"
                                    style={{ fontSize: 12, display: "block" }}
                                >
                                    {s.description}
                                </Typography.Text>
                            </div>
                        </div>
                    </Card>
                ))}

                {MOCK_SKILLS.length === 0 && (
                    <div
                        style={{
                            textAlign: "center",
                            padding: 32,
                            color: token.colorTextSecondary,
                        }}
                    >
                        <Typography.Text type="secondary">
                            统一 Skill 管理中暂无可用 Skill
                        </Typography.Text>
                    </div>
                )}
            </Space>
        </div>
    );
}

// ui-spec.md UI-008 §8.1：版本列表（v1 起递增），每项含版本号、保存时间、保存人、变更摘要。
// 原型不单独跳转到 UI-008 独立页面（“版本”区段本身即等价于同一 Agent 上下文内的 UI-008，
// 详见 ui-spec.md §4.4“‘版本’：跳 UI-008 版本视图（同一个 Agent 上下文）”），因此直接在本区段
// 内列出全部版本，2026-07-15 移除原先的“查看版本历史”跳转按钮。
const MOCK_VERSIONS = [
    {
        version: "v3",
        status: "已发布",
        savedAt: "2026-07-11 16:48:05",
        savedBy: "owner-alice",
        summary: "优化研究报告结构并更新模型参数",
    },
    {
        version: "v2",
        status: "历史版本",
        savedAt: "2026-07-08 10:22:41",
        savedBy: "owner-alice",
        summary: "新增“合同风险清单”工具调用",
    },
    {
        version: "v1",
        status: "历史版本",
        savedAt: "2026-07-01 09:03:17",
        savedBy: "owner-alice",
        summary: "初始配置",
    },
];

function SectionVersion() {
    return (
        <div>
            <Table
                size="small"
                pagination={false}
                rowKey="version"
                dataSource={MOCK_VERSIONS}
                columns={[
                    {
                        title: "版本",
                        dataIndex: "version",
                        width: 72,
                        render: (v: string) => <Typography.Text strong>{v}</Typography.Text>,
                    },
                    {
                        title: "状态",
                        dataIndex: "status",
                        width: 96,
                        render: (status: string) => (
                            <Tag color={status === "已发布" ? "success" : "default"}>
                                {status}
                            </Tag>
                        ),
                    },
                    { title: "保存时间", dataIndex: "savedAt", width: 168 },
                    { title: "保存人", dataIndex: "savedBy", width: 120 },
                    { title: "变更摘要", dataIndex: "summary" },
                ]}
            />
        </div>
    );
}

// ─── Section Router ───────────────────────────────────────────────────────────

function SectionContent({
    section,
    readonly,
    density,
}: {
    section: SectionKey;
    readonly: boolean;
    density: Density;
}) {
    switch (section) {
        case "basic":
            return <SectionBasic readonly={readonly} />;
        case "instructions":
            return <SectionInstructions readonly={readonly} />;
        case "model":
            return <SectionModel readonly={readonly} density={density} />;
        case "tools":
            return <SectionTools readonly={readonly} density={density} />;
        case "skills":
            return <SectionSkills readonly={readonly} density={density} />;
        case "version":
            return <SectionVersion />;
        default:
            return null;
    }
}

// ─── State Badge ─────────────────────────────────────────────────────────────

function StateBadge({ state }: { state: AgentState }) {
    const { token } = antdTheme.useToken();
    const map: Record<AgentState, { label: string; color: string }> = {
        editing: { label: "编辑中", color: token.colorWarning },
        "new-draft": { label: "未发布的草稿", color: token.colorWarning },
        "draft-pending": {
            label: "有未发布的修改",
            color: token.colorWarning,
        },
        readonly: { label: "只读", color: token.colorTextSecondary },
        submitting: { label: "提交中…", color: token.colorInfo },
        "submit-failed": { label: "发布失败", color: token.colorError },
        "submit-success": { label: "已发布为 v3", color: token.colorSuccess },
    };
    const m = map[state];
    return (
        <Tag
            style={{
                color: m.color,
                borderColor: m.color + "60",
                background: m.color + "12",
                fontSize: 11,
            }}
        >
            {state === "submitting" && (
                <LoadingOutlined style={{ marginRight: 4 }} />
            )}
            {state === "submit-success" && (
                <CheckCircleOutlined style={{ marginRight: 4 }} />
            )}
            {state === "submit-failed" && (
                <CloseCircleOutlined style={{ marginRight: 4 }} />
            )}
            {m.label}
        </Tag>
    );
}

// ─── UI-004 内嵌"开始对话"面板：视觉形态照抄 Ant Design X "copilot" playground（顶部
// 标题栏 + 会话切换 Popover + 消息流 + 快捷问题按钮 + Sender 输入区，用于在还未发布时
// 测试当前编辑中的配置）。2026-07-15 改为非模态的页内侍靠面板（对齐官方 demo：整页是
// `.copilotWrapper` 一个 flex 行，左边 `.workarea` 和右边 `.Copilot` 是并排的两个兄弟
// 元素，不是遮罩弹层），打开面板时左侧配置表单仍可继续滚动/编辑，不再用 antd `Drawer`
// （Drawer 自带遮罩，会挡住并禁用左侧内容的交互）。核心的"发消息 → mock 回复"状态机与
// UI-005 AgentChatPage 共用 ../chat/useMockChat，Bubble.List 角色配置共用
// ../chat/chatBubbleRoles，仅布局与空状态展示方式不同。 ────────────────────────────

const DRAWER_PROMPTS = [
    { key: "d1", description: "整理一份竞品研究框架" },
    { key: "d2", description: "分析这份资料的关键结论" },
    { key: "d3", description: "为调研报告设计目录" },
];

const DRAWER_SESSIONS: ConversationItemType[] = [
    { key: "cur", label: "当前会话" },
    { key: "prev-1", label: "上次测试：调研报告目录" },
];

/** 模块级常量而不是内联函数：保证传给 useMockChat 的 mockReply 引用稳定，配合
 * useMockChat 内部的 ref 化稳定化，避免每次 CopilotPanel 重渲染都重建函数导致
 * useChatBubbleRoles 的 useMemo 失效。 */
const COPILOT_MOCK_REPLY = (text: string) =>
    `已收到"${text}"。我会先明确研究范围和目标读者，再整理信息来源、分析维度与交付结构。`;

function CopilotPanel({
    open,
    onClose,
}: {
    open: boolean;
    onClose: () => void;
}) {
    const { token } = antdTheme.useToken();
    const { attachmentsOpen, setAttachmentsOpen, files, setFiles } = useAttachments();
    const [activeSession, setActiveSession] = useState("cur");
    const { messages, setMessages, replying, setReplying, input, setInput, submit, retryLast, startNewSession } =
        useMockChat([], COPILOT_MOCK_REPLY);
    const roles = useChatBubbleRoles(retryLast);

    const handleNewSession = () => {
        if (!startNewSession([])) return;
        setActiveSession("cur");
    };

    /** 与 AgentChatPage 共用同一套 Harness / Agent Loop 演示触发逻辑：输入包含"研究/调研"等
     * 关键词走 Harness 的 plan→execute 自主循环，包含"优化/改进"等关键词走 Agent Loop 的
     * 迭代优化循环，都不命中时走普通单条 mock 回复。 */
    const handleUserSubmit = (value: string) => {
        const trimmed = value.trim();
        if (!trimmed || replying) return;
        if (isHarnessTrigger(trimmed) || isAgentLoopTrigger(trimmed)) {
            const userMessage: ChatMessage = {
                id: `u-${Date.now()}`,
                role: "user",
                content: trimmed,
                time: formatNow(),
            };
            setMessages((prev) => [...prev, userMessage]);
            setInput("");
            if (isAgentLoopTrigger(trimmed)) {
                runAgentLoopDemo(trimmed, setMessages, setReplying);
            } else {
                runHarnessDemo(trimmed, setMessages, setReplying);
            }
            return;
        }
        submit(trimmed);
    };

    return (
        <div
            style={{
                width: open ? 400 : 0,
                flexShrink: 0,
                overflow: "hidden",
                display: "flex",
                flexDirection: "column",
                height: "100%",
                borderLeft: open
                    ? `1px solid ${token.colorBorderSecondary}`
                    : "none",
                background: token.colorBgContainer,
                transition: "width 0.2s",
            }}
        >
            {/* 面板本身固定 400px 内容宽度，用外层 width 做收起/展开动画，避免收起过程中
             * 内部 Flex 布局跟着挤压变形。 */}
            <div style={{ width: 400, display: "flex", flexDirection: "column", height: "100%" }}>
                <div
                    style={{
                        height: 52,
                        flexShrink: 0,
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "space-between",
                        padding: "0 10px 0 16px",
                        borderBottom: `1px solid ${token.colorBorderSecondary}`,
                    }}
                >
                    <Space size={10}>
                        <div
                            style={{
                                width: 32,
                                height: 32,
                                borderRadius: 9,
                                background: token.colorPrimary,
                                display: "flex",
                                alignItems: "center",
                                justifyContent: "center",
                                color: "#fff",
                                fontSize: 13,
                                fontWeight: 700,
                            }}
                        >
                            智
                        </div>
                        <div>
                            <Typography.Text strong style={{ display: "block" }}>
                                智能研究助手
                            </Typography.Text>
                            <Typography.Text type="secondary" style={{ fontSize: 11 }}>
                                Azure OpenAI GPT-4o · v3
                            </Typography.Text>
                        </div>
                    </Space>
                    <Space size={0}>
                        <Tooltip title="新建会话">
                            <Button
                                type="text"
                                icon={<PlusOutlined />}
                                onClick={handleNewSession}
                            />
                        </Tooltip>
                        <Popover
                            placement="bottomRight"
                            trigger="click"
                            styles={{ content: { padding: 0, maxHeight: 320, overflow: "auto" } }}
                            content={
                                <Conversations
                                    items={DRAWER_SESSIONS}
                                    activeKey={activeSession}
                                    onActiveChange={setActiveSession}
                                    styles={{ item: { padding: "0 8px" } }}
                                    style={{ width: 240 }}
                                />
                            }
                        >
                            <Button type="text" icon={<CommentOutlined />} />
                        </Popover>
                        <Tooltip title="关闭">
                            <Button
                                type="text"
                                icon={<CloseOutlined />}
                                onClick={onClose}
                            />
                        </Tooltip>
                    </Space>
                </div>

                <div
                    style={{
                        flex: 1,
                        minHeight: 0,
                        // 空状态（Welcome/Prompts）本身内容短，用普通 overflow:auto 即可；
                        // 有消息时改成 overflow:hidden，把真正的滚动交给 Bubble.List 自带的
                        // `.scroll-box`（带"贴底自动跟随"CSS 技巧）——这层再叠一个 overflow:auto
                        // 会形成两层独立滚动容器，导致流式输出时页面不会自动跟着往下滚。同时给
                        // Bubble.List 显式传 `style={{height:"100%"}}`（详见 AgentChatPage.tsx
                        // 同一处的注释）——只靠这层外壳有确定高度还不够，Bubble.List 自己的 CSS
                        // 只写了 max-height:100%，子元素百分比高度解析不认这种钳制后的渲染高度，
                        // 必须显式指定 height 才会被下一层正确当作百分比基准。
                        overflow: messages.length === 0 ? "auto" : "hidden",
                        padding: messages.length === 0 ? "20px 16px 0" : 0,
                    }}
                >
                    {messages.length === 0 ? (
                        <>
                            <Welcome
                                variant="borderless"
                                icon={
                                    <div
                                        style={{
                                            width: 44,
                                            height: 44,
                                            borderRadius: 12,
                                            background: token.colorPrimaryBg,
                                            color: token.colorPrimary,
                                            display: "flex",
                                            alignItems: "center",
                                            justifyContent: "center",
                                            fontWeight: 700,
                                        }}
                                    >
                                        智
                                    </div>
                                }
                                title="从一个研究问题开始"
                                description="我可以协助检索资料、梳理证据并生成结构化报告。"
                                style={{ marginBottom: 16 }}
                            />
                            <Prompts
                                vertical
                                items={DRAWER_PROMPTS}
                                onItemClick={(info) =>
                                    handleUserSubmit(info.data.description as string)
                                }
                            />
                        </>
                    ) : (
                        <Bubble.List
                            items={toBubbleItems(messages, replying)}
                            role={roles}
                            style={{ height: "100%" }}
                            styles={{ scroll: { padding: "20px 16px 0" } }}
                        />
                    )}
                </div>

                <div
                    style={{
                        padding: "10px 16px 16px",
                        background: token.colorBgContainer,
                        borderTop: `1px solid ${token.colorBorderSecondary}`,
                    }}
                >
                    <Space size={8} style={{ marginBottom: 10 }}>
                        <Button
                            size="small"
                            icon={<FileSearchOutlined />}
                            onClick={() => handleUserSubmit("整理一份竞品研究框架")}
                        >
                            研究框架
                        </Button>
                        <Button
                            size="small"
                            icon={<AppstoreAddOutlined />}
                            onClick={() => handleUserSubmit("为调研报告设计目录")}
                        >
                            报告目录
                        </Button>
                    </Space>
                    <Sender
                        suffix={false}
                        value={input}
                        onChange={setInput}
                        onSubmit={handleUserSubmit}
                        loading={replying}
                        allowSpeech
                        placeholder="输入消息，Enter 发送，Shift + Enter 换行"
                        autoSize={{ minRows: 2, maxRows: 5 }}
                        header={
                            <ChatAttachmentsHeader
                                open={attachmentsOpen}
                                onOpenChange={setAttachmentsOpen}
                                files={files}
                                onFilesChange={setFiles}
                            />
                        }
                        footer={(actionNode) => (
                            <Flex justify="space-between" align="center">
                                <Flex gap={8} align="center">
                                    <Button
                                        type="text"
                                        icon={<PaperClipOutlined />}
                                        onClick={() => setAttachmentsOpen(!attachmentsOpen)}
                                    />
                                    <Typography.Text type="secondary" style={{ fontSize: 11 }}>
                                        当前使用已保存版本 v3
                                    </Typography.Text>
                                </Flex>
                                <Flex align="center">{actionNode}</Flex>
                            </Flex>
                        )}
                    />
                </div>
            </div>
        </div>
    );
}

// ─── Main Page ────────────────────────────────────────────────────────────────

const STATE_OPTIONS: { value: AgentState; label: string }[] = [
    { value: "editing", label: "编辑中" },
    { value: "new-draft", label: "新建草稿（从未发布）" },
    { value: "draft-pending", label: "已发布 + 有未发布修改" },
    { value: "readonly", label: "只读（非 Owner）" },
    { value: "submitting", label: "提交中" },
    { value: "submit-failed", label: "提交失败" },
    { value: "submit-success", label: "提交成功" },
];

export default function AgentDesignPage({
    onBack,
    initialState = "editing",
    onCopilotOpenChange,
}: {
    onBack?: () => void;
    initialState?: AgentState;
    /** 面板开关时回传给宿主，便于宿主 AppShell 自己的主导航 nav 同步收缩/恢复展开，
     * 避免主 nav + 本页内部区段导航 + CopilotPanel 三重宽度叠加挤币内容区。 */
    onCopilotOpenChange?: (open: boolean) => void;
} = {}) {
    const { token } = antdTheme.useToken();
    const [section, setSection] = useState<SectionKey>("basic");
    const [state, setState] = useState<AgentState>(initialState);
    const [density, setDensity] = useState<Density>("compact");
    const [siderCollapsed, setSiderCollapsed] = useState(false);
    const [conversationOpen, setConversationOpen] = useState(false);
    const [publishModalOpen, setPublishModalOpen] = useState(false);
    const [changeSummary, setChangeSummary] = useState("");

    const isReadonly = state === "readonly" || state === "submitting";
    const isOwner = state !== "readonly";

    const siderWidth = siderCollapsed ? 52 : density === "compact" ? 176 : 200;

    // 整体是一个 flex 行：左边是配置页本体（.workarea），右边是可开合的 CopilotPanel
    // （.Copilot），两者并排、不是遮罩弹层——对齐 Ant Design X "copilot" playground 的
    // `.copilotWrapper` 结构，面板打开时左侧配置表单仍可继续滚动/编辑。
    return (
        <div style={{ height: "100%", display: "flex", overflow: "hidden" }}>
            <div
                style={{
                    flex: 1,
                    minWidth: 0,
                    height: "100%",
                    display: "flex",
                    flexDirection: "column",
                overflow: "hidden",
                background: token.colorBgLayout,
            }}
        >
            {/* Prototype controls */}
            <div
                style={{
                    minHeight: 44,
                    padding: "6px 24px",
                    background: token.colorFillQuaternary,
                    borderBottom: `1px solid ${token.colorBorderSecondary}`,
                    display: "flex",
                    gap: 12,
                    alignItems: "center",
                    flexWrap: "wrap",
                    flexShrink: 0,
                }}
            >
                <Typography.Text
                    type="secondary"
                    style={{ fontSize: 11, marginRight: 4 }}
                >
                    设计评审
                </Typography.Text>
                <Select
                    aria-label="页面状态"
                    value={state}
                    onChange={setState}
                    style={{ width: 146 }}
                    size="small"
                    options={STATE_OPTIONS}
                />
                <Segmented
                    size="small"
                    value={density}
                    onChange={(v) => setDensity(v as Density)}
                    options={[
                        { value: "standard", label: "标准" },
                        { value: "compact", label: "紧凑" },
                    ]}
                />
                <Tag
                    style={{
                        marginLeft: "auto",
                        marginRight: 0,
                        fontSize: 11,
                        color: token.colorPrimaryText,
                        background: token.colorPrimaryBg,
                        borderColor: "transparent",
                    }}
                >
                    UI-004 · 6 个配置区段
                </Tag>
            </div>

            {/* Agent header */}
            <div
                style={{
                    padding: density === "compact" ? "8px 24px" : "12px 24px",
                    background: token.colorBgContainer,
                    borderBottom: `1px solid ${token.colorBorderSecondary}`,
                    display: "flex",
                    alignItems: "center",
                    gap: 14,
                    flexWrap: "wrap",
                    flexShrink: 0,
                    minHeight: density === "compact" ? 60 : 72,
                }}
            >
                {onBack && (
                    <Tooltip title="返回 Agent 空间">
                        <Button
                            type="text"
                            aria-label="返回 Agent 空间"
                            icon={<ArrowLeftOutlined />}
                            onClick={onBack}
                        />
                    </Tooltip>
                )}
                <div
                    style={{
                        width: 44,
                        height: 44,
                        borderRadius: 12,
                        background: `linear-gradient(145deg, ${token.colorPrimary}B8, ${token.colorPrimary})`,
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                        flexShrink: 0,
                        boxShadow: `0 8px 20px ${token.colorPrimary}28`,
                    }}
                >
                    <Typography.Text
                        style={{ color: "#fff", fontWeight: 700, fontSize: 17 }}
                    >
                        智
                    </Typography.Text>
                </div>
                <div style={{ minWidth: 140 }}>
                    <Typography.Text
                        strong
                        style={{
                            display: "block",
                            lineHeight: 1.35,
                            fontSize: 16,
                        }}
                    >
                        智能研究助手
                    </Typography.Text>
                    <Space size={8} style={{ marginTop: 4 }}>
                        <Typography.Text
                            type="secondary"
                            style={{ fontSize: 11 }}
                        >
                            v3 · Owner: owner-alice
                        </Typography.Text>
                        <StateBadge state={state} />
                    </Space>
                </div>

                <div
                    style={{
                        marginLeft: "auto",
                        display: "flex",
                        gap: 8,
                        alignItems: "center",
                    }}
                >
                    {isOwner && (
                        <Popconfirm
                            title="删除这个 Agent？"
                            description="删除后无法恢复，已有对话记录不会保留。"
                            okText="删除"
                            cancelText="取消"
                            okButtonProps={{ danger: true }}
                        >
                            <Tooltip title="删除 Agent">
                                <Button
                                    danger
                                    type="text"
                                    aria-label="删除 Agent"
                                    icon={<DeleteOutlined />}
                                />
                            </Tooltip>
                        </Popconfirm>
                    )}
                    <Button
                        icon={<PlayCircleOutlined />}
                        onClick={() => {
                            setConversationOpen(true);
                            setSiderCollapsed(true);
                            onCopilotOpenChange?.(true);
                        }}
                    >
                        试运行
                    </Button>
                    {isOwner && (
                        <Button
                            icon={<SaveOutlined />}
                            loading={state === "submitting"}
                            disabled={state === "submitting"}
                            onClick={() => message.success("已存为草稿，未影响已发布的版本")}
                        >
                            保存
                        </Button>
                    )}
                    {isOwner && (
                        <Button
                            type="primary"
                            icon={<CloudUploadOutlined />}
                            loading={state === "submitting"}
                            disabled={state === "submitting"}
                            onClick={() => setPublishModalOpen(true)}
                        >
                            {state === "submitting" ? "发布中…" : "发布"}
                        </Button>
                    )}
                    {!isOwner && (
                        <Button icon={<CopyOutlined />}>
                            复制为我的 Agent
                        </Button>
                    )}
                </div>
            </div>

            {(state === "submit-failed" || state === "submit-success") && (
                <Alert
                    banner
                    type={state === "submit-failed" ? "error" : "success"}
                    showIcon
                    className="inkwell-compact-alert"
                    title={
                        <span style={{ fontSize: 13 }}>
                            {state === "submit-failed" ? (
                                "发布失败：500。已保留你的草稿"
                            ) : (
                                <span>
                                    已发布为 <strong>v3</strong>{" "}
                                    <Button
                                        type="link"
                                        size="small"
                                        style={{ padding: 0 }}
                                    >
                                        查看版本
                                    </Button>
                                </span>
                            )}
                        </span>
                    }
                    style={{ flexShrink: 0, padding: "6px 24px" }}
                />
            )}

            {/* Configuration workspace */}
            <div
                style={{
                    flex: 1,
                    minHeight: 0,
                    overflow: "hidden",
                }}
            >
                <div
                    data-testid="agent-configuration-workspace"
                    style={{
                        width: "100%",
                        height: "100%",
                        display: "flex",
                        overflow: "auto",
                        background: token.colorBgContainer,
                        border: `1px solid ${token.colorBorderSecondary}`,
                    }}
                >
                    {/* Section Navigation */}
                    <div
                        style={{
                            width: siderWidth,
                            flexShrink: 0,
                            background: token.colorFillQuaternary,
                            borderRight: `1px solid ${token.colorBorderSecondary}`,
                            overflow: "hidden",
                            transition: "width 0.2s",
                            display: "flex",
                            flexDirection: "column",
                        }}
                    >
                        <div
                            style={{
                                minHeight: 48,
                                padding: "8px",
                                display: "flex",
                                alignItems: "center",
                                justifyContent: "flex-end",
                                borderBottom: `1px solid ${token.colorBorderSecondary}`,
                            }}
                        >
                            <Button
                                type="text"
                                size="small"
                                icon={
                                    siderCollapsed ? (
                                        <MenuUnfoldOutlined />
                                    ) : (
                                        <MenuFoldOutlined />
                                    )
                                }
                                onClick={() => setSiderCollapsed((v) => !v)}
                            />
                        </div>

                        {/* Section list */}
                        <div
                            style={{
                                flex: 1,
                                overflow: "auto",
                                padding: "10px 8px",
                            }}
                        >
                            {SECTIONS.map((s) => {
                                const active = s.key === section;
                                return (
                                    <button
                                        key={s.key}
                                        type="button"
                                        onClick={() => setSection(s.key)}
                                        style={{
                                            position: "relative",
                                            zIndex: 1,
                                            width: "100%",
                                            border: 0,
                                            display: "flex",
                                            alignItems: "center",
                                            gap: siderCollapsed ? 0 : 8,
                                            padding: siderCollapsed
                                                ? "8px 0"
                                                : density === "compact"
                                                  ? "7px 10px"
                                                  : "9px 10px",
                                            marginBottom: 3,
                                            borderRadius: 6,
                                            cursor: "pointer",
                                            justifyContent: siderCollapsed
                                                ? "center"
                                                : "flex-start",
                                            background: active
                                                ? token.colorPrimaryBg
                                                : "transparent",
                                            color: active
                                                ? token.colorPrimary
                                                : token.colorText,
                                            transition:
                                                "background-color 0.15s, color 0.15s",
                                            fontSize:
                                                density === "compact" ? 12 : 13,
                                            fontWeight: active ? 600 : 400,
                                            fontFamily: "inherit",
                                            textAlign: "left",
                                        }}
                                        aria-current={
                                            active ? "page" : undefined
                                        }
                                    >
                                        <Tooltip
                                            title={
                                                siderCollapsed ? s.label : ""
                                            }
                                            placement="right"
                                        >
                                            <span
                                                style={{
                                                    flexShrink: 0,
                                                    fontSize:
                                                        density === "compact"
                                                            ? 14
                                                            : 16,
                                                }}
                                            >
                                                {s.icon}
                                            </span>
                                        </Tooltip>
                                        {!siderCollapsed && (
                                            <span
                                                style={{
                                                    whiteSpace: "nowrap",
                                                    overflow: "hidden",
                                                    textOverflow: "ellipsis",
                                                }}
                                            >
                                                {s.label}
                                            </span>
                                        )}
                                    </button>
                                );
                            })}
                        </div>
                    </div>

                    {/* Section Content */}
                    <div
                        style={{
                            flex: 1,
                            overflow: "auto",
                            padding:
                                density === "compact"
                                    ? "18px 24px"
                                    : "24px 32px",
                            minWidth: 280,
                            background: token.colorBgContainer,
                        }}
                    >
                        <div
                            style={{
                                width: "100%",
                            }}
                        >
                            <div
                                style={{
                                    display: "flex",
                                    alignItems: "center",
                                    gap: 6,
                                    marginBottom:
                                        density === "compact" ? 14 : 20,
                                    paddingBottom:
                                        density === "compact" ? 10 : 14,
                                    borderBottom: `1px solid ${token.colorBorderSecondary}`,
                                }}
                            >
                                <Typography.Title
                                    level={density === "compact" ? 5 : 3}
                                    style={{ margin: 0 }}
                                >
                                    {
                                        SECTIONS.find((s) => s.key === section)
                                            ?.label
                                    }
                                </Typography.Title>
                                {section === "instructions" && (
                                    <Tooltip title="给 Agent 的系统指令。支持 Markdown，超过 32K 字符时给出警告。">
                                        <Button
                                            type="text"
                                            size="small"
                                            aria-label="Instructions 帮助"
                                            icon={<QuestionCircleOutlined />}
                                            style={{
                                                color: token.colorTextSecondary,
                                            }}
                                        />
                                    </Tooltip>
                                )}
                            </div>

                            <SectionContent
                                section={section}
                                readonly={isReadonly}
                                density={density}
                            />
                        </div>
                    </div>
                </div>
            </div>

            {/* 存为草稿与发布是两个独立动作：存为草稿不产生新版本、不影响已发布版本或正在进行的对话；发布才会把当前编辑内容提交为新版本并立即生效（对应 requirements.md REQ-015 二态版本模型） */}
            <Modal
                open={publishModalOpen}
                onCancel={() => setPublishModalOpen(false)}
                onOk={() => setPublishModalOpen(false)}
                okText="发布"
                cancelText="取消"
                title="发布新版本"
            >
                <Typography.Paragraph type="secondary" style={{ marginBottom: 16 }}>
                    发布后将成为新的正式版本，正在使用该 Agent 的对话会从下一轮开始使用新版本。
                </Typography.Paragraph>
                <Form layout="vertical">
                    <Form.Item label="变更说明（可选）" style={{ marginBottom: 0 }}>
                        <Input
                            value={changeSummary}
                            onChange={(event) => setChangeSummary(event.target.value)}
                            placeholder="说明本次修改的内容，会记录到版本历史里"
                            maxLength={100}
                        />
                    </Form.Item>
                </Form>
            </Modal>
            </div>

            <CopilotPanel
                open={conversationOpen}
                onClose={() => {
                    setConversationOpen(false);
                    setSiderCollapsed(false);
                    onCopilotOpenChange?.(false);
                }}
            />
        </div>
    );
}
