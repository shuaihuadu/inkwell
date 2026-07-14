import { useState } from "react";
import {
    Alert,
    Button,
    Card,
    Checkbox,
    Col,
    Descriptions,
    Drawer,
    Form,
    Input,
    InputNumber,
    Popconfirm,
    Row,
    Select,
    Slider,
    Space,
    Switch,
    Tag,
    Tooltip,
    Typography,
    Upload,
    theme as antdTheme,
    Segmented,
} from "antd";
import {
    SaveOutlined,
    PlayCircleOutlined,
    SendOutlined,
    DeleteOutlined,
    CopyOutlined,
    UploadOutlined,
    QuestionCircleOutlined,
    EyeOutlined,
    EyeInvisibleOutlined,
    CheckCircleOutlined,
    CloseCircleOutlined,
    LoadingOutlined,
    SlidersOutlined,
    ToolOutlined,
    ReadOutlined,
    CompressOutlined,
    HistoryOutlined,
    UserOutlined,
    RobotOutlined,
    MenuFoldOutlined,
    MenuUnfoldOutlined,
} from "@ant-design/icons";

// ─── Types ────────────────────────────────────────────────────────────────────

/** ui-spec.md §4.2 */
type AgentState =
    | "editing"
    | "new-draft"
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
    | "context"
    | "version";

type Density = "standard" | "compact";

// ─── Section Config ───────────────────────────────────────────────────────────

const SECTIONS: { key: SectionKey; label: string; icon: React.ReactNode }[] = [
    { key: "basic", label: "基础信息", icon: <UserOutlined /> },
    { key: "instructions", label: "Instructions", icon: <RobotOutlined /> },
    { key: "model", label: "模型与参数", icon: <SlidersOutlined /> },
    { key: "tools", label: "工具", icon: <ToolOutlined /> },
    { key: "skills", label: "Skills", icon: <ReadOutlined /> },
    { key: "context", label: "上下文策略", icon: <CompressOutlined /> },
    { key: "version", label: "版本与调试", icon: <HistoryOutlined /> },
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

function SectionBasic({
    readonly,
    density,
}: {
    readonly: boolean;
    density: Density;
}) {
    const { token } = antdTheme.useToken();
    const size = density === "compact" ? "small" : "middle";
    return (
        <Form layout="vertical" size={size}>
            <Row gutter={[32, 24]} align="stretch">
                <Col xs={24} md={6} xl={5}>
                    <Form.Item label="头像" style={{ marginBottom: 0 }}>
                        <div
                            style={{
                                minHeight: 190,
                                padding: 24,
                                borderRadius: 8,
                                background: token.colorFillQuaternary,
                                border: `1px solid ${token.colorBorderSecondary}`,
                                display: "flex",
                                flexDirection: "column",
                                alignItems: "center",
                                justifyContent: "center",
                                gap: 14,
                            }}
                        >
                            <div
                                style={{
                                    width: 76,
                                    height: 76,
                                    borderRadius: 18,
                                    background: `linear-gradient(145deg, ${token.colorPrimary}B8, ${token.colorPrimary})`,
                                    display: "flex",
                                    alignItems: "center",
                                    justifyContent: "center",
                                    boxShadow: `0 10px 28px ${token.colorPrimary}28`,
                                }}
                            >
                                <Typography.Text
                                    style={{
                                        color: "#fff",
                                        fontSize: 26,
                                        fontWeight: 700,
                                    }}
                                >
                                    智
                                </Typography.Text>
                            </div>
                            {!readonly && (
                                <Upload
                                    showUploadList={false}
                                    disabled={readonly}
                                >
                                    <Button
                                        size="small"
                                        icon={<UploadOutlined />}
                                    >
                                        更换头像
                                    </Button>
                                </Upload>
                            )}
                            <Typography.Text
                                type="secondary"
                                style={{ fontSize: 10, whiteSpace: "nowrap" }}
                            >
                                可选图片
                            </Typography.Text>
                        </div>
                    </Form.Item>
                </Col>
                <Col xs={24} md={18} xl={19}>
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
                        />
                    </Form.Item>
                    <Form.Item
                        label="描述"
                        name="description"
                        initialValue="帮助团队进行深度研究、文献检索与报告生成的智能助手。支持多轮对话、工具调用和 Skills 扩展。"
                        style={{ marginBottom: 0 }}
                    >
                        <Input.TextArea
                            rows={density === "compact" ? 5 : 6}
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

function SectionInstructions({
    readonly,
    density,
}: {
    readonly: boolean;
    density: Density;
}) {
    const [charCount, setCharCount] = useState(420);
    const WARN_THRESHOLD = 32000;
    return (
        <Form
            layout="vertical"
            size={density === "compact" ? "small" : "middle"}
        >
            <Form.Item>
                {charCount >= WARN_THRESHOLD && (
                    <Alert
                        type="warning"
                        showIcon
                        message="Instructions 较长，可能挤占模型上下文"
                        style={{ marginBottom: 8 }}
                    />
                )}
                <Input.TextArea
                    aria-label="Instructions"
                    rows={density === "compact" ? 8 : 12}
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
    const size = density === "compact" ? "small" : "middle";

    return (
        <Form layout="vertical" size={size}>
            <div
                style={{
                    padding: density === "compact" ? 16 : 20,
                    marginBottom: 24,
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
                                marginBottom: 24,
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
                                marginBottom: 24,
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
                                marginBottom: 24,
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
                            size={size}
                            disabled={useDefaultTokens || readonly}
                            style={{ width: "100%" }}
                        />
                    </Card>
                </Col>
            </Row>
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

function SectionContext({
    readonly,
    density,
}: {
    readonly: boolean;
    density: Density;
}) {
    const { token } = antdTheme.useToken();
    const strategies = [
        {
            name: "工具结果压缩",
            type: "ToolResultCompactionStrategy",
            trigger: "消息数超过 7",
            description: "折叠较早的工具调用及结果，保留简短结果摘要。",
        },
        {
            name: "摘要压缩",
            type: "SummarizationCompactionStrategy",
            trigger: "Token 数超过 1,280",
            description: "调用摘要模型，将较早的消息组替换为上下文摘要。",
        },
        {
            name: "滑动窗口",
            type: "SlidingWindowCompactionStrategy",
            trigger: "对话轮次超过 4",
            description: "按完整对话轮次移除较早内容，始终保留系统消息。",
        },
        {
            name: "Token 截断",
            type: "TruncationCompactionStrategy",
            trigger: "Token 数超过 32,768",
            description: "按消息组丢弃最早内容，作为上下文预算的最后保护。",
        },
    ];

    return (
        <div>
            <Alert
                type="info"
                showIcon
                title="聊天记录存储与模型输入压缩相互独立"
                description="InkwellChatHistoryProvider 负责读取并全量保存 Session 消息；MAF CompactionProvider 只在调用模型前规整上下文，不删除后端聊天记录。"
                style={{ marginBottom: 20 }}
            />
            <div
                style={{
                    padding: density === "compact" ? 12 : 16,
                    marginBottom: 20,
                    border: `1px solid ${token.colorBorderSecondary}`,
                    borderRadius: 8,
                    background: token.colorFillQuaternary,
                }}
            >
                <Space size={8} wrap>
                    <Typography.Text strong>聊天历史提供程序</Typography.Text>
                    <Tag color="processing">InkwellChatHistoryProvider</Tag>
                    <Tag color="success">全量持久化</Tag>
                </Space>
                <Typography.Text
                    type="secondary"
                    style={{ display: "block", fontSize: 12, marginTop: 6 }}
                >
                    运行前按时间顺序加载历史；运行成功后追加本轮请求与响应。
                </Typography.Text>
            </div>

            <div
                style={{
                    display: "flex",
                    justifyContent: "space-between",
                    alignItems: "flex-start",
                    gap: 16,
                    marginBottom: 12,
                }}
            >
                <div>
                    <Space size={8} wrap>
                        <Typography.Text strong>
                            模型输入压缩流水线
                        </Typography.Text>
                        <Tag>CompactionProvider</Tag>
                        <Tag>实验性 API</Tag>
                    </Space>
                    <Typography.Text
                        type="secondary"
                        style={{ display: "block", fontSize: 12, marginTop: 4 }}
                    >
                        策略按由温和到激进的顺序执行，每项独立判断触发条件。
                    </Typography.Text>
                </div>
                <Switch
                    aria-label="启用压缩流水线"
                    checked={false}
                    disabled
                    checkedChildren="已启用"
                    unCheckedChildren="待接入"
                />
            </div>

            <Space
                direction="vertical"
                size={density === "compact" ? 8 : 10}
                style={{ width: "100%" }}
            >
                {strategies.map((strategy, index) => (
                    <Card
                        key={strategy.type}
                        size="small"
                        style={{
                            borderRadius: 8,
                            opacity: readonly ? 0.72 : 1,
                        }}
                    >
                        <div
                            style={{
                                display: "flex",
                                alignItems: "flex-start",
                                gap: 12,
                            }}
                        >
                            <Tag style={{ marginInlineEnd: 0 }}>
                                {index + 1}
                            </Tag>
                            <div style={{ flex: 1 }}>
                                <Space size={8} wrap>
                                    <Typography.Text strong>
                                        {strategy.name}
                                    </Typography.Text>
                                    <Typography.Text
                                        code
                                        type="secondary"
                                        style={{ fontSize: 11 }}
                                    >
                                        {strategy.type}
                                    </Typography.Text>
                                    <Tag color="blue">{strategy.trigger}</Tag>
                                </Space>
                                <Typography.Text
                                    type="secondary"
                                    style={{
                                        display: "block",
                                        fontSize: 12,
                                        marginTop: 4,
                                    }}
                                >
                                    {strategy.description}
                                </Typography.Text>
                            </div>
                        </div>
                    </Card>
                ))}
            </Space>

            <Typography.Text
                type="secondary"
                style={{ display: "block", fontSize: 11, marginTop: 12 }}
            >
                AgentSnapshot 的可序列化策略 DTO 与 MAF
                映射尚未锁定，当前仅展示候选流水线，不开放保存。
            </Typography.Text>
        </div>
    );
}

function SectionVersion() {
    const { token: _token } = antdTheme.useToken();
    return (
        <div>
            <Descriptions
                bordered
                size="small"
                column={1}
                style={{ maxWidth: 480, marginBottom: 20 }}
            >
                <Descriptions.Item label="当前版本">v3</Descriptions.Item>
                <Descriptions.Item label="状态">
                    <Tag color="success">已发布</Tag>
                </Descriptions.Item>
                <Descriptions.Item label="最后保存时间">
                    2026-07-11 16:45:22
                </Descriptions.Item>
                <Descriptions.Item label="创建人">
                    owner-alice
                </Descriptions.Item>
                <Descriptions.Item label="发布时间">
                    2026-07-11 16:48:05
                </Descriptions.Item>
                <Descriptions.Item label="变更摘要">
                    优化研究报告结构并更新模型参数
                </Descriptions.Item>
            </Descriptions>
            <Space>
                <Button icon={<HistoryOutlined />}>
                    查看版本历史 (UI-008)
                </Button>
                <Button icon={<EyeOutlined />}>调试 / Trace (UI-007)</Button>
            </Space>
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
            return <SectionBasic readonly={readonly} density={density} />;
        case "instructions":
            return (
                <SectionInstructions readonly={readonly} density={density} />
            );
        case "model":
            return <SectionModel readonly={readonly} density={density} />;
        case "tools":
            return <SectionTools readonly={readonly} density={density} />;
        case "skills":
            return <SectionSkills readonly={readonly} density={density} />;
        case "context":
            return <SectionContext readonly={readonly} density={density} />;
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
        "new-draft": { label: "未保存的草稿", color: token.colorWarning },
        readonly: { label: "只读", color: token.colorTextSecondary },
        submitting: { label: "保存中…", color: token.colorInfo },
        "submit-failed": { label: "保存失败", color: token.colorError },
        "submit-success": { label: "已保存为 v3", color: token.colorSuccess },
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

function ConversationDrawer({
    open,
    onClose,
}: {
    open: boolean;
    onClose: () => void;
}) {
    const { token } = antdTheme.useToken();
    const [message, setMessage] = useState("");
    const [sentMessage, setSentMessage] = useState<string | null>(null);

    const sendMessage = () => {
        const nextMessage = message.trim();
        if (!nextMessage) return;
        setSentMessage(nextMessage);
        setMessage("");
    };

    return (
        <Drawer
            title={
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
                        <Typography.Text
                            type="secondary"
                            style={{ fontSize: 11 }}
                        >
                            Azure OpenAI GPT-4o · v3
                        </Typography.Text>
                    </div>
                </Space>
            }
            open={open}
            onClose={onClose}
            size={480}
            styles={{
                body: {
                    padding: 0,
                    display: "flex",
                    flexDirection: "column",
                    background: token.colorBgLayout,
                },
            }}
        >
            <div
                style={{
                    flex: 1,
                    minHeight: 0,
                    overflow: "auto",
                    padding: "28px 24px",
                }}
            >
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
                        marginBottom: 16,
                        fontWeight: 700,
                    }}
                >
                    智
                </div>
                <Typography.Title level={4} style={{ margin: "0 0 8px" }}>
                    从一个研究问题开始
                </Typography.Title>
                <Typography.Paragraph
                    type="secondary"
                    style={{ marginBottom: 24, lineHeight: 1.7 }}
                >
                    我可以协助检索资料、梳理证据并生成结构化报告。
                </Typography.Paragraph>

                {!sentMessage ? (
                    <Space
                        orientation="vertical"
                        size={8}
                        style={{ width: "100%" }}
                    >
                        {[
                            "整理一份竞品研究框架",
                            "分析这份资料的关键结论",
                            "为调研报告设计目录",
                        ].map((prompt) => (
                            <Button
                                key={prompt}
                                block
                                style={{ textAlign: "left", height: 40 }}
                                onClick={() => setMessage(prompt)}
                            >
                                {prompt}
                            </Button>
                        ))}
                    </Space>
                ) : (
                    <>
                        <div
                            style={{
                                maxWidth: "82%",
                                marginLeft: "auto",
                                padding: "10px 14px",
                                borderRadius: "8px 8px 2px 8px",
                                background: token.colorPrimary,
                                color: "#fff",
                                lineHeight: 1.6,
                            }}
                        >
                            {sentMessage}
                        </div>
                        <div
                            style={{
                                maxWidth: "88%",
                                marginTop: 16,
                                padding: "12px 14px",
                                borderRadius: "8px 8px 8px 2px",
                                background: token.colorBgContainer,
                                border: `1px solid ${token.colorBorderSecondary}`,
                                lineHeight: 1.7,
                            }}
                        >
                            已收到。我会先明确研究范围和目标读者，再整理信息来源、分析维度与交付结构。
                        </div>
                    </>
                )}
            </div>

            <div
                style={{
                    padding: "16px 20px 20px",
                    background: token.colorBgContainer,
                    borderTop: `1px solid ${token.colorBorderSecondary}`,
                }}
            >
                <Input.TextArea
                    value={message}
                    onChange={(event) => setMessage(event.target.value)}
                    onPressEnter={(event) => {
                        if (!event.shiftKey) {
                            event.preventDefault();
                            sendMessage();
                        }
                    }}
                    autoSize={{ minRows: 2, maxRows: 5 }}
                    placeholder="输入消息，Enter 发送，Shift + Enter 换行"
                    style={{ marginBottom: 10 }}
                />
                <div
                    style={{
                        display: "flex",
                        justifyContent: "space-between",
                        alignItems: "center",
                    }}
                >
                    <Typography.Text type="secondary" style={{ fontSize: 11 }}>
                        当前使用已保存版本 v3
                    </Typography.Text>
                    <Button
                        type="primary"
                        icon={<SendOutlined />}
                        disabled={!message.trim()}
                        onClick={sendMessage}
                    >
                        发送
                    </Button>
                </div>
            </div>
        </Drawer>
    );
}

// ─── Main Page ────────────────────────────────────────────────────────────────

const STATE_OPTIONS: { value: AgentState; label: string }[] = [
    { value: "editing", label: "编辑中" },
    { value: "new-draft", label: "新建草稿" },
    { value: "readonly", label: "只读（非 Owner）" },
    { value: "submitting", label: "提交中" },
    { value: "submit-failed", label: "提交失败" },
    { value: "submit-success", label: "提交成功" },
];

export default function AgentDesignPage() {
    const { token } = antdTheme.useToken();
    const [section, setSection] = useState<SectionKey>("basic");
    const [state, setState] = useState<AgentState>("editing");
    const [density, setDensity] = useState<Density>("standard");
    const [siderCollapsed, setSiderCollapsed] = useState(false);
    const [conversationOpen, setConversationOpen] = useState(false);

    const isReadonly = state === "readonly" || state === "submitting";
    const isOwner = state !== "readonly";

    const siderWidth = siderCollapsed ? 52 : density === "compact" ? 176 : 200;

    return (
        <div
            style={{
                height: "calc(100vh - 56px)",
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
                    padding: "6px 28px",
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
                    UI-004 · 7 个配置区段
                </Tag>
            </div>

            {/* Agent header */}
            <div
                style={{
                    padding: density === "compact" ? "10px 28px" : "14px 28px",
                    background: token.colorBgContainer,
                    borderBottom: `1px solid ${token.colorBorderSecondary}`,
                    display: "flex",
                    alignItems: "center",
                    gap: 14,
                    flexShrink: 0,
                    minHeight: density === "compact" ? 60 : 72,
                }}
            >
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
                <div style={{ minWidth: 0 }}>
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
                                    size={
                                        density === "compact"
                                            ? "small"
                                            : "middle"
                                    }
                                />
                            </Tooltip>
                        </Popconfirm>
                    )}
                    <Button
                        icon={<PlayCircleOutlined />}
                        size={density === "compact" ? "small" : "middle"}
                        onClick={() => setConversationOpen(true)}
                    >
                        开始对话
                    </Button>
                    {isOwner && (
                        <Button
                            type="primary"
                            icon={<SaveOutlined />}
                            loading={state === "submitting"}
                            disabled={state === "submitting"}
                            size={density === "compact" ? "small" : "middle"}
                        >
                            {state === "submitting" ? "保存中…" : "保存"}
                        </Button>
                    )}
                    {!isOwner && (
                        <Button
                            icon={<CopyOutlined />}
                            size={density === "compact" ? "small" : "middle"}
                        >
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
                    title={
                        state === "submit-failed" ? (
                            "保存失败：500。已保留你的草稿"
                        ) : (
                            <span>
                                已保存为 <strong>v3</strong>{" "}
                                <Button
                                    type="link"
                                    size="small"
                                    style={{ padding: 0 }}
                                >
                                    查看版本
                                </Button>
                            </span>
                        )
                    }
                    style={{ flexShrink: 0, padding: "7px 28px" }}
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
                        overflow: "hidden",
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
                                padding: siderCollapsed
                                    ? "8px"
                                    : "8px 12px 8px 16px",
                                display: "flex",
                                alignItems: "center",
                                justifyContent: "space-between",
                                borderBottom: `1px solid ${token.colorBorderSecondary}`,
                            }}
                        >
                            {!siderCollapsed && (
                                <Typography.Text
                                    type="secondary"
                                    style={{ fontSize: 11, fontWeight: 600 }}
                                >
                                    配置区段
                                </Typography.Text>
                            )}
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
                                    ? "24px 28px"
                                    : "32px 40px",
                            minWidth: 0,
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
                                        density === "compact" ? 18 : 26,
                                    paddingBottom:
                                        density === "compact" ? 14 : 18,
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
                                {isReadonly && section !== "version" && (
                                    <Alert
                                        type="info"
                                        showIcon
                                        message="只读模式：当前非 Owner，所有字段不可编辑"
                                        style={{
                                            marginTop: 12,
                                            flexBasis: "100%",
                                        }}
                                        icon={<EyeInvisibleOutlined />}
                                    />
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

            <ConversationDrawer
                open={conversationOpen}
                onClose={() => setConversationOpen(false)}
            />
        </div>
    );
}
