import { useState } from "react";
import {
    Avatar,
    Button,
    Descriptions,
    Drawer,
    Flex,
    Select,
    Space,
    Tag,
    Tooltip,
    Typography,
    message,
    theme as antdTheme,
} from "antd";
import {
    ApiOutlined,
    AppstoreOutlined,
    CloseOutlined,
    ClockCircleOutlined,
    DatabaseOutlined,
    EyeOutlined,
    ExperimentOutlined,
    ExportOutlined,
} from "@ant-design/icons";
import ResourceListPage, {
    ResourceRowAction,
    ResourceRowActions,
} from "../components/ResourceListPage";

type Capability = boolean | null;
type TestState = "idle" | "testing" | "success" | "failure";
type ModelCategory =
    | "Chat"
    | "Embedding"
    | "ImageGeneration"
    | "VideoGeneration"
    | "Unknown";

const CATEGORY_LABELS: Record<ModelCategory, string> = {
    Chat: "对话",
    Embedding: "嵌入",
    ImageGeneration: "图像生成",
    VideoGeneration: "视频生成",
    Unknown: "未知",
};

interface ModelItem {
    key: string;
    id: string;
    category: ModelCategory;
    providerMode: string | null;
    ownedBy: string | null;
    maxInputTokens: number | null;
    maxOutputTokens: number | null;
    vision: Capability;
    tools: Capability;
    structuredOutput: Capability;
    reasoning: Capability;
}

const MODELS: ModelItem[] = [
    {
        key: "gpt-4.1",
        id: "gpt-4.1",
        category: "Chat",
        providerMode: "chat",
        ownedBy: "openai",
        maxInputTokens: 1_047_576,
        maxOutputTokens: 32_768,
        vision: true,
        tools: true,
        structuredOutput: true,
        reasoning: false,
    },
    {
        key: "claude-sonnet-4",
        id: "claude-sonnet-4",
        category: "Chat",
        providerMode: "chat",
        ownedBy: "anthropic",
        maxInputTokens: 200_000,
        maxOutputTokens: 64_000,
        vision: true,
        tools: true,
        structuredOutput: null,
        reasoning: true,
    },
    {
        key: "text-embedding-3-large",
        id: "text-embedding-3-large",
        category: "Embedding",
        providerMode: "embedding",
        ownedBy: "openai",
        maxInputTokens: 8_191,
        maxOutputTokens: null,
        vision: false,
        tools: false,
        structuredOutput: false,
        reasoning: false,
    },
    {
        key: "qwen3-235b",
        id: "qwen3-235b-a22b",
        category: "Chat",
        providerMode: "chat",
        ownedBy: "alibaba",
        maxInputTokens: 131_072,
        maxOutputTokens: 32_768,
        vision: false,
        tools: true,
        structuredOutput: null,
        reasoning: true,
    },
    {
        key: "image-gen",
        id: "gpt-image-1",
        category: "ImageGeneration",
        providerMode: "image_generation",
        ownedBy: "openai",
        maxInputTokens: null,
        maxOutputTokens: null,
        vision: null,
        tools: false,
        structuredOutput: false,
        reasoning: false,
    },
    {
        key: "video-gen",
        id: "sora-2",
        category: "VideoGeneration",
        providerMode: "video_generation",
        ownedBy: "openai",
        maxInputTokens: null,
        maxOutputTokens: null,
        vision: true,
        tools: false,
        structuredOutput: false,
        reasoning: false,
    },
    {
        key: "custom-rerank",
        id: "custom-rerank-v1",
        category: "Unknown",
        providerMode: "rerank",
        ownedBy: "internal",
        maxInputTokens: null,
        maxOutputTokens: null,
        vision: null,
        tools: null,
        structuredOutput: null,
        reasoning: null,
    },
];

function CapabilityTag({ value }: { value: Capability }) {
    if (value === null) return <Tag>未知</Tag>;
    return value ? (
        <Tag color="success">支持</Tag>
    ) : (
        <Tag color="default">不支持</Tag>
    );
}

export default function ModelListPage() {
    const { token } = antdTheme.useToken();
    const [searchText, setSearchText] = useState("");
    const [category, setCategory] = useState("all");
    const [selectedModel, setSelectedModel] = useState<ModelItem | null>(null);
    const [testStates, setTestStates] = useState<Record<string, TestState>>({});
    const [messageApi, contextHolder] = message.useMessage();

    const filteredModels = MODELS.filter((model) => {
        const matchesText = `${model.id} ${model.ownedBy ?? ""}`
            .toLowerCase()
            .includes(searchText.trim().toLowerCase());
        const matchesCategory =
            category === "all" || model.category === category;
        return matchesText && matchesCategory;
    });

    const testModel = (model: ModelItem) => {
        setTestStates((current) => ({ ...current, [model.key]: "testing" }));
        window.setTimeout(() => {
            const succeeded =
                model.key !== "qwen3-235b" && model.category !== "Unknown";
            setTestStates((current) => ({
                ...current,
                [model.key]: succeeded ? "success" : "failure",
            }));
            succeeded
                ? messageApi.success(
                      `${model.id} ${CATEGORY_LABELS[model.category]}最小请求成功 · 842 ms`,
                  )
                : messageApi.error(`${model.id} 测试失败 · 提供方暂时不可用`);
        }, 700);
    };

    const modelActions = (model: ModelItem) => {
        const state = testStates[model.key] ?? "idle";
        return (
            <ResourceRowActions>
                <ResourceRowAction
                    label={`查看 ${model.id}`}
                    text="查看"
                    icon={<EyeOutlined />}
                    onClick={() => setSelectedModel(model)}
                />
                <ResourceRowAction
                    label={`测试 ${model.id}`}
                    text="测试"
                    icon={<ExperimentOutlined />}
                    loading={state === "testing"}
                    onClick={() => testModel(model)}
                />
                {state === "failure" && <Tag color="error">失败</Tag>}
            </ResourceRowActions>
        );
    };

    return (
        <ResourceListPage<ModelItem>
            title="模型"
            description="查看 LiteLLM 实时发现的模型与能力。此列表只读，模型配置在 LiteLLM 中维护。"
            primaryAction={
                <Button
                    type="primary"
                    ghost
                    icon={<ExportOutlined />}
                    onClick={() =>
                        messageApi.info(
                            "原型演示：将在系统浏览器打开已配置的 LiteLLM Dashboard URL",
                        )
                    }
                >
                    模型管理
                </Button>
            }
            filters={
                <Select
                    value={category}
                    onChange={setCategory}
                    style={{ width: 170 }}
                    options={[
                        { value: "all", label: "全部类型" },
                        { value: "Chat", label: CATEGORY_LABELS.Chat },
                        {
                            value: "Embedding",
                            label: CATEGORY_LABELS.Embedding,
                        },
                        {
                            value: "ImageGeneration",
                            label: CATEGORY_LABELS.ImageGeneration,
                        },
                        {
                            value: "VideoGeneration",
                            label: CATEGORY_LABELS.VideoGeneration,
                        },
                        {
                            value: "Unknown",
                            label: CATEGORY_LABELS.Unknown,
                        },
                    ]}
                />
            }
            refreshLabel="刷新模型"
            searchValue={searchText}
            searchPlaceholder="搜索模型标识或提供方"
            onSearchChange={setSearchText}
            paginationResetKey={`${searchText}:${category}`}
            dataSource={filteredModels}
            rowKey="key"
            tableScrollX={1120}
            columns={[
                {
                    title: "模型标识",
                    dataIndex: "id",
                    width: 210,
                    fixed: "left",
                    render: (value: string) => (
                        <Typography.Text>{value}</Typography.Text>
                    ),
                },
                {
                    title: "模型类型",
                    dataIndex: "category",
                    width: 150,
                    render: (value: ModelCategory) => CATEGORY_LABELS[value],
                },
                {
                    title: "提供方",
                    dataIndex: "ownedBy",
                    width: 110,
                    render: (value) => value ?? "未知",
                },
                {
                    title: "Token 上限",
                    key: "tokens",
                    width: 178,
                    render: (_, model) => (
                        <Typography.Text type="secondary">
                            输入{" "}
                            {model.maxInputTokens?.toLocaleString() ?? "未知"} /
                            输出{" "}
                            {model.maxOutputTokens?.toLocaleString() ?? "未知"}
                        </Typography.Text>
                    ),
                },
                {
                    title: "视觉",
                    dataIndex: "vision",
                    width: 78,
                    render: (value) => <CapabilityTag value={value} />,
                },
                {
                    title: "工具",
                    dataIndex: "tools",
                    width: 78,
                    render: (value) => <CapabilityTag value={value} />,
                },
                {
                    title: "结构化",
                    dataIndex: "structuredOutput",
                    width: 78,
                    render: (value) => <CapabilityTag value={value} />,
                },
                {
                    title: "推理",
                    dataIndex: "reasoning",
                    width: 78,
                    render: (value) => <CapabilityTag value={value} />,
                },
                {
                    title: "操作",
                    key: "actions",
                    width: 220,
                    fixed: "right",
                    align: "center",
                    className: "inkwell-action-column",
                    render: (_, model) => modelActions(model),
                },
            ]}
        >
            {contextHolder}
            <Drawer
                width={600}
                title="模型详情"
                closable={false}
                open={selectedModel !== null}
                onClose={() => setSelectedModel(null)}
                extra={
                    <Tooltip title="关闭">
                        <Button
                            type="text"
                            aria-label="关闭模型详情"
                            icon={<CloseOutlined />}
                            onClick={() => setSelectedModel(null)}
                        />
                    </Tooltip>
                }
                className="inkwell-resource-details-drawer"
                styles={{ body: { padding: 0 } }}
            >
                {selectedModel && (
                    <div>
                        <div
                            className="inkwell-agent-details-identity"
                            style={{
                                background: token.colorFillQuaternary,
                                borderBottom: `1px solid ${token.colorBorderSecondary}`,
                            }}
                        >
                            <Avatar
                                size={52}
                                icon={<ApiOutlined />}
                                style={{ background: token.colorPrimary }}
                            />
                            <div className="inkwell-agent-details-identity-copy">
                                <Flex align="center" gap={8} wrap>
                                    <Typography.Title level={4} style={{ margin: 0 }}>
                                        {selectedModel.id}
                                    </Typography.Title>
                                    <Tag color="processing">
                                        {CATEGORY_LABELS[selectedModel.category]}
                                    </Tag>
                                </Flex>
                                <Flex gap={8} wrap style={{ marginTop: 8 }}>
                                    <Tag>
                                        {selectedModel.providerMode ?? "模式未知"}
                                    </Tag>
                                    <Tag>{selectedModel.ownedBy ?? "提供方未知"}</Tag>
                                </Flex>
                            </div>
                        </div>

                        <div className="inkwell-agent-details-content">
                            <section className="inkwell-agent-details-section">
                                <Space
                                    size={8}
                                    className="inkwell-agent-details-section-title"
                                >
                                    <ApiOutlined />
                                    <Typography.Text strong>模型信息</Typography.Text>
                                </Space>
                                <Descriptions
                                    size="small"
                                    column={1}
                                    items={[
                                        {
                                            key: "id",
                                            label: "模型 ID",
                                            children: selectedModel.id,
                                        },
                                        {
                                            key: "category",
                                            label: "Category",
                                            children: selectedModel.category,
                                        },
                                        {
                                            key: "providerMode",
                                            label: "Provider Mode",
                                            children:
                                                selectedModel.providerMode ?? "未知",
                                        },
                                        {
                                            key: "ownedBy",
                                            label: "OwnedBy",
                                            children: selectedModel.ownedBy ?? "未知",
                                        },
                                    ]}
                                />
                            </section>

                            <section className="inkwell-agent-details-section">
                                <Space
                                    size={8}
                                    className="inkwell-agent-details-section-title"
                                >
                                    <DatabaseOutlined />
                                    <Typography.Text strong>Token 上限</Typography.Text>
                                </Space>
                                <div className="inkwell-model-metric-grid">
                                    <div className="inkwell-model-metric-item">
                                        <Typography.Text type="secondary">
                                            最大输入
                                        </Typography.Text>
                                        <Typography.Title level={4} style={{ margin: 0 }}>
                                            {selectedModel.maxInputTokens?.toLocaleString() ??
                                                "未知"}
                                        </Typography.Title>
                                        <Typography.Text type="secondary">
                                            tokens
                                        </Typography.Text>
                                    </div>
                                    <div className="inkwell-model-metric-item">
                                        <Typography.Text type="secondary">
                                            最大输出
                                        </Typography.Text>
                                        <Typography.Title level={4} style={{ margin: 0 }}>
                                            {selectedModel.maxOutputTokens?.toLocaleString() ??
                                                "未知"}
                                        </Typography.Title>
                                        <Typography.Text type="secondary">
                                            tokens
                                        </Typography.Text>
                                    </div>
                                </div>
                            </section>

                            <section className="inkwell-agent-details-section">
                                <Space
                                    size={8}
                                    className="inkwell-agent-details-section-title"
                                >
                                    <AppstoreOutlined />
                                    <Typography.Text strong>能力</Typography.Text>
                                </Space>
                                <div className="inkwell-model-capability-grid">
                                    {[
                                        ["视觉", selectedModel.vision],
                                        ["工具调用", selectedModel.tools],
                                        [
                                            "结构化输出",
                                            selectedModel.structuredOutput,
                                        ],
                                        ["推理", selectedModel.reasoning],
                                    ].map(([label, value]) => (
                                        <div
                                            key={String(label)}
                                            className="inkwell-model-capability-item"
                                        >
                                            <Typography.Text>{label}</Typography.Text>
                                            <CapabilityTag value={value as Capability} />
                                        </div>
                                    ))}
                                </div>
                            </section>

                            <section className="inkwell-agent-details-section">
                                <Space
                                    size={8}
                                    className="inkwell-agent-details-section-title"
                                >
                                    <ClockCircleOutlined />
                                    <Typography.Text strong>数据来源</Typography.Text>
                                </Space>
                                <Typography.Text type="secondary">
                                    数据来自 LiteLLM 实时发现，不在 Inkwell 中保存副本。
                                </Typography.Text>
                            </section>
                        </div>
                    </div>
                )}
            </Drawer>
        </ResourceListPage>
    );
}
