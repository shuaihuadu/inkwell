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
import { useMutation, useQuery } from "@tanstack/react-query";
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
    theme,
} from "antd";
import { useState } from "react";
import DataListPage, {
    DataListRowAction,
    DataListRowActions,
} from "../../shared/components/data-list-page";
import { desktopApi } from "../../shared/network/desktop-api";
import type {
    LLMModel,
    LLMModelCategory,
} from "../../shared/network/contracts";
import { useAuthStore } from "../auth/auth-store";

const categoryLabels: Record<LLMModelCategory, string> = {
    Unknown: "未知",
    Chat: "对话",
    Embedding: "嵌入",
    ImageGeneration: "图像生成",
    VideoGeneration: "视频生成",
};

type ModelCategoryFilter = LLMModelCategory | "All";
type TestState = "success" | "failure";

function CapabilityTag({ value }: { value: boolean | null }) {
    if (value === null) {
        return <Tag>未知</Tag>;
    }

    return value ? (
        <Tag color="success">支持</Tag>
    ) : (
        <Tag color="default">不支持</Tag>
    );
}

const formatTokens = (value: number | null): string =>
    value === null ? "未知" : value.toLocaleString();

const formatLatency = (value: string): string => {
    const match = /^(?:(\d+)\.)?(\d{2}):(\d{2}):(\d{2}(?:\.\d+)?)$/.exec(
        value,
    );
    if (!match) {
        return value;
    }

    const days = Number(match[1] ?? 0);
    const hours = Number(match[2]);
    const minutes = Number(match[3]);
    const seconds = Number(match[4]);
    const milliseconds =
        (((days * 24 + hours) * 60 + minutes) * 60 + seconds) * 1000;
    return `${Math.round(milliseconds).toLocaleString()} ms`;
};

export function ModelManagement() {
    const { token } = theme.useToken();
    const isAdmin = useAuthStore((state) => state.identity?.isAdmin === true);
    const [category, setCategory] = useState<ModelCategoryFilter>("All");
    const [searchText, setSearchText] = useState("");
    const [selectedModel, setSelectedModel] = useState<LLMModel | null>(null);
    const [testStates, setTestStates] = useState<Record<string, TestState>>({});
    const [messageApi, messageContext] = message.useMessage();
    const modelsQuery = useQuery({
        queryKey: ["models"],
        queryFn: desktopApi.listModels,
    });
    const managementQuery = useQuery({
        queryKey: ["model-management-info"],
        queryFn: desktopApi.getModelManagementInfo,
        enabled: isAdmin,
    });
    const testMutation = useMutation({
        mutationFn: desktopApi.testModel,
        onSuccess: (result) => {
            const testedModel = modelsQuery.data?.find(
                (model) => model.id === result.modelId,
            );
            const categoryLabel = testedModel
                ? categoryLabels[testedModel.category]
                : "";
            setTestStates((current) => ({
                ...current,
                [result.modelId]: result.isSuccess ? "success" : "failure",
            }));
            if (result.isSuccess) {
                messageApi.success(
                    `${result.modelId} ${categoryLabel}最小请求成功 · ${formatLatency(result.latency)}`,
                );
            } else {
                messageApi.error(result.errorMessage ?? "模型连接测试失败");
            }
        },
        onError: (reason, modelId) => {
            setTestStates((current) => ({
                ...current,
                [modelId]: "failure",
            }));
            messageApi.error(
                reason instanceof Error ? reason.message : "模型连接测试失败",
            );
        },
    });
    const normalizedSearch = searchText.trim().toLocaleLowerCase();
    const models = (modelsQuery.data ?? []).filter(
        (model) =>
            (category === "All" || model.category === category) &&
            (model.id.toLocaleLowerCase().includes(normalizedSearch) ||
                (model.ownedBy ?? "")
                    .toLocaleLowerCase()
                    .includes(normalizedSearch)),
    );
    const dashboardUrl = managementQuery.data?.dashboardUrl;

    return (
        <DataListPage<LLMModel>
            title="模型"
            description="查看 LiteLLM 实时发现的模型与能力。此列表只读，模型配置在 LiteLLM 中维护。"
            primaryAction={isAdmin ? (
                <Button
                    type="primary"
                    ghost
                    icon={<ExportOutlined />}
                    disabled={!dashboardUrl}
                    loading={managementQuery.isLoading}
                    onClick={() =>
                        dashboardUrl && void desktopApi.openExternal(dashboardUrl)
                    }
                >
                    模型管理
                </Button>
            ) : undefined}
            filters={
                <Select<ModelCategoryFilter>
                    aria-label="筛选模型类型"
                    value={category}
                    onChange={setCategory}
                    style={{ width: 170 }}
                    options={[
                        { label: "全部类型", value: "All" },
                        ...Object.entries(categoryLabels).map(
                            ([value, label]) => ({ value, label }),
                        ),
                    ]}
                />
            }
            refreshLabel="刷新模型"
            onRefresh={() => {
                void modelsQuery.refetch();
                void managementQuery.refetch();
            }}
            refreshing={modelsQuery.isFetching && !modelsQuery.isLoading}
            searchValue={searchText}
            searchPlaceholder="搜索模型标识或提供方"
            searchMaxLength={128}
            onSearchChange={setSearchText}
            paginationResetKey={`${searchText}:${category}`}
            dataSource={models}
            rowKey="id"
            tableScrollX={1120}
            loading={modelsQuery.isLoading}
            errorMessage={
                modelsQuery.isError ? "无法读取 LiteLLM 模型，请重试" : undefined
            }
            onRetry={() => void modelsQuery.refetch()}
            emptyText="LiteLLM 当前未返回模型"
            filteredEmptyText="在所选条件内没有结果，请清除筛选"
            isFiltered={normalizedSearch.length > 0 || category !== "All"}
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
                    render: (value: LLMModelCategory) => categoryLabels[value],
                },
                {
                    title: "提供方",
                    dataIndex: "ownedBy",
                    width: 110,
                    render: (value: string | null) => value ?? "未知",
                },
                {
                    title: "Token 上限",
                    key: "tokens",
                    width: 178,
                    render: (_, model) => (
                        <Typography.Text type="secondary">
                            输入 {formatTokens(model.maxInputTokens)} / 输出{" "}
                            {formatTokens(model.maxOutputTokens)}
                        </Typography.Text>
                    ),
                },
                {
                    title: "视觉",
                    dataIndex: "supportsVision",
                    width: 78,
                    render: (value: boolean | null) => (
                        <CapabilityTag value={value} />
                    ),
                },
                {
                    title: "工具",
                    dataIndex: "supportsTools",
                    width: 78,
                    render: (value: boolean | null) => (
                        <CapabilityTag value={value} />
                    ),
                },
                {
                    title: "结构化",
                    dataIndex: "supportsStructuredOutput",
                    width: 78,
                    render: (value: boolean | null) => (
                        <CapabilityTag value={value} />
                    ),
                },
                {
                    title: "推理",
                    dataIndex: "supportsReasoning",
                    width: 78,
                    render: (value: boolean | null) => (
                        <CapabilityTag value={value} />
                    ),
                },
                {
                    title: "操作",
                    key: "actions",
                    width: 220,
                    fixed: "right",
                    align: "center",
                    className: "inkwell-action-column",
                    render: (_, model) => (
                        <DataListRowActions>
                            <DataListRowAction
                                label={`查看 ${model.id}`}
                                text="查看"
                                icon={<EyeOutlined />}
                                onClick={() => setSelectedModel(model)}
                            />
                            <DataListRowAction
                                label={`测试 ${model.id}`}
                                text="测试"
                                icon={<ExperimentOutlined />}
                                loading={
                                    testMutation.isPending &&
                                    testMutation.variables === model.id
                                }
                                onClick={() => testMutation.mutate(model.id)}
                            />
                            {testStates[model.id] === "failure" && (
                                <Tag color="error">失败</Tag>
                            )}
                        </DataListRowActions>
                    ),
                },
            ]}
        >
            {messageContext}
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
                className="resource-details-drawer"
                styles={{ body: { padding: 0 } }}
            >
                {selectedModel && (
                    <div>
                        <div
                            className="agent-details-identity"
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
                            <div className="agent-details-identity-copy">
                                <Flex align="center" gap={8} wrap>
                                    <Typography.Title level={4} style={{ margin: 0 }}>
                                        {selectedModel.id}
                                    </Typography.Title>
                                    <Tag color="processing">
                                        {categoryLabels[selectedModel.category]}
                                    </Tag>
                                </Flex>
                                <Flex gap={8} wrap style={{ marginTop: 8 }}>
                                    <Tag>{selectedModel.providerMode ?? "模式未知"}</Tag>
                                    <Tag>{selectedModel.ownedBy ?? "提供方未知"}</Tag>
                                </Flex>
                            </div>
                        </div>

                        <div className="agent-details-content">
                            <section className="agent-details-section">
                                <Space size={8} className="agent-details-section-title">
                                    <ApiOutlined />
                                    <Typography.Text strong>模型信息</Typography.Text>
                                </Space>
                                <Descriptions
                                    size="small"
                                    column={1}
                                    items={[
                                        { key: "id", label: "模型 ID", children: selectedModel.id },
                                        { key: "category", label: "Category", children: selectedModel.category },
                                        {
                                            key: "providerMode",
                                            label: "Provider Mode",
                                            children: selectedModel.providerMode ?? "未知",
                                        },
                                        {
                                            key: "ownedBy",
                                            label: "OwnedBy",
                                            children: selectedModel.ownedBy ?? "未知",
                                        },
                                    ]}
                                />
                            </section>

                            <section className="agent-details-section">
                                <Space size={8} className="agent-details-section-title">
                                    <DatabaseOutlined />
                                    <Typography.Text strong>Token 上限</Typography.Text>
                                </Space>
                                <div className="model-metric-grid">
                                    <div className="model-metric-item">
                                        <Typography.Text type="secondary">最大输入</Typography.Text>
                                        <Typography.Title level={4} style={{ margin: 0 }}>
                                            {formatTokens(selectedModel.maxInputTokens)}
                                        </Typography.Title>
                                        <Typography.Text type="secondary">tokens</Typography.Text>
                                    </div>
                                    <div className="model-metric-item">
                                        <Typography.Text type="secondary">最大输出</Typography.Text>
                                        <Typography.Title level={4} style={{ margin: 0 }}>
                                            {formatTokens(selectedModel.maxOutputTokens)}
                                        </Typography.Title>
                                        <Typography.Text type="secondary">tokens</Typography.Text>
                                    </div>
                                </div>
                            </section>

                            <section className="agent-details-section">
                                <Space size={8} className="agent-details-section-title">
                                    <AppstoreOutlined />
                                    <Typography.Text strong>能力</Typography.Text>
                                </Space>
                                <div className="model-capability-grid">
                                    <div className="model-capability-item">
                                        <Typography.Text>视觉</Typography.Text>
                                        <CapabilityTag value={selectedModel.supportsVision} />
                                    </div>
                                    <div className="model-capability-item">
                                        <Typography.Text>工具调用</Typography.Text>
                                        <CapabilityTag value={selectedModel.supportsTools} />
                                    </div>
                                    <div className="model-capability-item">
                                        <Typography.Text>结构化输出</Typography.Text>
                                        <CapabilityTag value={selectedModel.supportsStructuredOutput} />
                                    </div>
                                    <div className="model-capability-item">
                                        <Typography.Text>推理</Typography.Text>
                                        <CapabilityTag value={selectedModel.supportsReasoning} />
                                    </div>
                                </div>
                            </section>

                            <section className="agent-details-section">
                                <Space size={8} className="agent-details-section-title">
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
        </DataListPage>
    );
}
