import {
    ApiOutlined,
    ClockCircleOutlined,
    ExperimentOutlined,
    ExportOutlined,
} from "@ant-design/icons";
import { useMutation, useQuery } from "@tanstack/react-query";
import {
    Button,
    Drawer,
    Select,
    Space,
    Tag,
    Typography,
    message,
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
    const isSuper = useAuthStore((state) => state.identity?.isSuper === true);
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
        enabled: isSuper,
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
            primaryAction={isSuper ? (
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
            totalLabel={(total) => `实时发现 ${total} 个模型`}
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
                    render: (value: string, model) => (
                        <Button
                            type="link"
                            className="model-id-link"
                            onClick={() => setSelectedModel(model)}
                        >
                            {value}
                        </Button>
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
                    title: "连通性",
                    key: "test",
                    width: 144,
                    fixed: "right",
                    align: "center",
                    className: "inkwell-action-column",
                    render: (_, model) => (
                        <DataListRowActions>
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
                width={500}
                title="模型详情"
                open={selectedModel !== null}
                onClose={() => setSelectedModel(null)}
            >
                {selectedModel && (
                    <Space
                        direction="vertical"
                        size={20}
                        style={{ width: "100%" }}
                    >
                        <Space>
                            <ApiOutlined />
                            <Typography.Title level={5} style={{ margin: 0 }}>
                                {selectedModel.id}
                            </Typography.Title>
                        </Space>
                        <Space wrap>
                            <Tag color="blue">
                                {categoryLabels[selectedModel.category]}
                            </Tag>
                            <Tag>{selectedModel.providerMode ?? "模式未知"}</Tag>
                            <Tag>{selectedModel.ownedBy ?? "提供方未知"}</Tag>
                        </Space>
                        <div>
                            <Typography.Text type="secondary">
                                上下文限制
                            </Typography.Text>
                            <Typography.Paragraph style={{ marginTop: 4 }}>
                                最大输入 {formatTokens(selectedModel.maxInputTokens)} 个令牌，最大输出{" "}
                                {formatTokens(selectedModel.maxOutputTokens)} 个令牌
                            </Typography.Paragraph>
                        </div>
                        <div>
                            <Typography.Text type="secondary">
                                能力
                            </Typography.Text>
                            <div className="model-capability-grid">
                                <span>
                                    视觉{" "}
                                    <CapabilityTag
                                        value={selectedModel.supportsVision}
                                    />
                                </span>
                                <span>
                                    工具调用{" "}
                                    <CapabilityTag
                                        value={selectedModel.supportsTools}
                                    />
                                </span>
                                <span>
                                    结构化输出{" "}
                                    <CapabilityTag
                                        value={
                                            selectedModel.supportsStructuredOutput
                                        }
                                    />
                                </span>
                                <span>
                                    推理{" "}
                                    <CapabilityTag
                                        value={selectedModel.supportsReasoning}
                                    />
                                </span>
                            </div>
                        </div>
                        <Typography.Text type="secondary">
                            <ClockCircleOutlined /> 数据来自 LiteLLM 实时发现，不在
                            Inkwell 中保存副本。
                        </Typography.Text>
                    </Space>
                )}
            </Drawer>
        </DataListPage>
    );
}
