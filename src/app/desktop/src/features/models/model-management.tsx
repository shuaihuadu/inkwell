import {
    CheckCircleOutlined,
    CloseCircleOutlined,
    ReloadOutlined,
    ThunderboltOutlined,
} from "@ant-design/icons";
import { useMutation, useQuery } from "@tanstack/react-query";
import {
    Button,
    Empty,
    Segmented,
    Space,
    Table,
    Tag,
    Tooltip,
    Typography,
    message,
} from "antd";
import { useState } from "react";
import { desktopApi } from "../../shared/network/desktop-api";
import type {
    LLMModel,
    LLMModelCategory,
} from "../../shared/network/contracts";

const categoryLabels: Record<LLMModelCategory, string> = {
    Unknown: "其他",
    Chat: "对话",
    Embedding: "嵌入",
    ImageGeneration: "图片生成",
    VideoGeneration: "视频生成",
};

interface ModelManagementProps {
    canTest: boolean;
}

export function ModelManagement({ canTest }: ModelManagementProps) {
    const [category, setCategory] = useState<LLMModelCategory | "All">("All");
    const [messageApi, contextHolder] = message.useMessage();
    const modelsQuery = useQuery({
        queryKey: ["models"],
        queryFn: desktopApi.listModels,
    });
    const testMutation = useMutation({
        mutationFn: desktopApi.testModel,
        onSuccess: (result) => {
            if (result.isSuccess) {
                messageApi.success(`${result.modelId} 连接正常`);
            } else {
                messageApi.error(result.errorMessage ?? "模型连接测试失败");
            }
        },
        onError: (reason) =>
            messageApi.error(
                reason instanceof Error ? reason.message : "模型连接测试失败",
            ),
    });
    const models = (modelsQuery.data ?? []).filter(
        (model) => category === "All" || model.category === category,
    );

    return (
        <main className="model-management">
            {contextHolder}
            <header className="model-management-header">
                <div>
                    <Typography.Title level={3}>模型管理</Typography.Title>
                    <Typography.Text type="secondary">
                        LiteLLM 当前可访问的模型
                    </Typography.Text>
                </div>
                <Button
                    icon={<ReloadOutlined />}
                    loading={modelsQuery.isFetching}
                    onClick={() => void modelsQuery.refetch()}
                >
                    刷新
                </Button>
            </header>
            <Segmented
                value={category}
                onChange={(value) =>
                    setCategory(value as LLMModelCategory | "All")
                }
                options={[
                    { label: "全部", value: "All" },
                    ...Object.entries(categoryLabels).map(([value, label]) => ({
                        label,
                        value,
                    })),
                ]}
            />
            <Table<LLMModel>
                className="model-table"
                rowKey="id"
                loading={modelsQuery.isLoading}
                dataSource={models}
                pagination={false}
                scroll={{ x: 760 }}
                tableLayout="fixed"
                locale={{
                    emptyText: modelsQuery.isError ? (
                        <Empty description="无法读取 LiteLLM 模型">
                            <Button onClick={() => void modelsQuery.refetch()}>
                                重试
                            </Button>
                        </Empty>
                    ) : (
                        <Empty description="当前分类暂无模型" />
                    ),
                }}
                columns={[
                    {
                        title: "模型",
                        dataIndex: "id",
                        render: (value: string, model) => (
                            <div className="model-name-cell">
                                <Typography.Text strong>{value}</Typography.Text>
                                <Typography.Text type="secondary">
                                    {model.ownedBy ?? "未知来源"}
                                </Typography.Text>
                            </div>
                        ),
                    },
                    {
                        title: "分类",
                        dataIndex: "category",
                        width: 130,
                        render: (value: LLMModelCategory) => (
                            <Tag>{categoryLabels[value]}</Tag>
                        ),
                    },
                    {
                        title: "上下文",
                        dataIndex: "maxInputTokens",
                        width: 130,
                        render: (value: number | null) =>
                            value === null ? "未知" : value.toLocaleString(),
                    },
                    {
                        title: "能力",
                        width: 260,
                        render: (_, model) => (
                            <Space size={[4, 4]} wrap>
                                {model.supportsVision && <Tag>视觉</Tag>}
                                {model.supportsTools && <Tag>工具</Tag>}
                                {model.supportsStructuredOutput && (
                                    <Tag>结构化输出</Tag>
                                )}
                                {model.supportsReasoning && <Tag>推理</Tag>}
                                {!model.supportsVision &&
                                    !model.supportsTools &&
                                    !model.supportsStructuredOutput &&
                                    !model.supportsReasoning && (
                                        <Typography.Text type="secondary">
                                            未报告
                                        </Typography.Text>
                                    )}
                            </Space>
                        ),
                    },
                    {
                        title: "连接",
                        width: 110,
                        align: "right",
                        render: (_, model) =>
                            canTest ? (
                                <Tooltip
                                    title={
                                        model.category === "Chat"
                                            ? "测试模型连接"
                                            : "当前仅支持测试对话模型"
                                    }
                                >
                                    <Button
                                        aria-label={`测试 ${model.id} 连接`}
                                        icon={<ThunderboltOutlined />}
                                        disabled={model.category !== "Chat"}
                                        loading={
                                            testMutation.isPending &&
                                            testMutation.variables === model.id
                                        }
                                        onClick={() =>
                                            testMutation.mutate(model.id)
                                        }
                                    />
                                </Tooltip>
                            ) : (
                                <Typography.Text type="secondary">
                                    仅管理员
                                </Typography.Text>
                            ),
                    },
                ]}
            />
            <div className="model-source-status">
                {modelsQuery.isError ? (
                    <CloseCircleOutlined />
                ) : (
                    <CheckCircleOutlined />
                )}
                <span>
                    {modelsQuery.isError
                        ? "LiteLLM 暂不可用"
                        : `已发现 ${modelsQuery.data?.length ?? 0} 个模型`}
                </span>
            </div>
        </main>
    );
}