import {
    ApiOutlined,
    AppstoreOutlined,
    CloseOutlined,
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
import type { TFunction } from "i18next";
import { useTranslation } from "react-i18next";
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

const modelCategories: LLMModelCategory[] = [
    "Unknown",
    "Chat",
    "Embedding",
    "ImageGeneration",
    "VideoGeneration",
];

type ModelCategoryFilter = LLMModelCategory | "All";
type TestState = "success" | "failure";

function CapabilityTag({ value }: { value: boolean | null }) {
    const { t } = useTranslation();
    if (value === null) {
        return <Tag>{t("common.unknown")}</Tag>;
    }

    return value ? (
        <Tag color="success">{t("models.capability.supported")}</Tag>
    ) : (
        <Tag color="default">{t("models.capability.unsupported")}</Tag>
    );
}

const formatTokens = (
    value: number | null,
    locale: string,
    t: TFunction,
): string =>
    value === null ? t("common.unknown") : value.toLocaleString(locale);

const formatLatency = (value: string, locale: string): string => {
    const match = /^(?:(\d+)\.)?(\d{2}):(\d{2}):(\d{2}(?:\.\d+)?)$/.exec(value);
    if (!match) {
        return value;
    }

    const days = Number(match[1] ?? 0);
    const hours = Number(match[2]);
    const minutes = Number(match[3]);
    const seconds = Number(match[4]);
    const milliseconds =
        (((days * 24 + hours) * 60 + minutes) * 60 + seconds) * 1000;
    return `${Math.round(milliseconds).toLocaleString(locale)} ms`;
};

export function ModelManagement() {
    const { t, i18n } = useTranslation();
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
                ? t(`models.categories.${testedModel.category}`)
                : "";
            setTestStates((current) => ({
                ...current,
                [result.modelId]: result.isSuccess ? "success" : "failure",
            }));
            if (result.isSuccess) {
                messageApi.success(
                    t("models.test.success", {
                        model: result.modelId,
                        category: categoryLabel,
                        latency: formatLatency(result.latency, i18n.language),
                    }),
                );
            } else {
                messageApi.error(
                    result.errorMessage ?? t("models.test.failed"),
                );
            }
        },
        onError: (reason, modelId) => {
            setTestStates((current) => ({
                ...current,
                [modelId]: "failure",
            }));
            messageApi.error(
                reason instanceof Error
                    ? reason.message
                    : t("models.test.failed"),
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
            title={t("models.title")}
            description={t("models.description")}
            primaryAction={
                isAdmin ? (
                    <Button
                        type="primary"
                        ghost
                        icon={<ExportOutlined />}
                        disabled={!dashboardUrl}
                        loading={managementQuery.isLoading}
                        onClick={() =>
                            dashboardUrl &&
                            void desktopApi.openExternal(dashboardUrl)
                        }
                    >
                        {t("models.management")}
                    </Button>
                ) : undefined
            }
            filters={
                <Select<ModelCategoryFilter>
                    aria-label={t("models.filterLabel")}
                    value={category}
                    onChange={setCategory}
                    style={{ width: 170 }}
                    options={[
                        { label: t("models.allTypes"), value: "All" },
                        ...modelCategories.map((value) => ({
                            value,
                            label: t(`models.categories.${value}`),
                        })),
                    ]}
                />
            }
            refreshLabel={t("models.refreshLabel")}
            onRefresh={() => {
                void modelsQuery.refetch();
                void managementQuery.refetch();
            }}
            refreshing={modelsQuery.isFetching && !modelsQuery.isLoading}
            searchValue={searchText}
            searchPlaceholder={t("models.searchPlaceholder")}
            searchMaxLength={128}
            onSearchChange={setSearchText}
            paginationResetKey={`${searchText}:${category}`}
            dataSource={models}
            rowKey="id"
            tableScrollX={1120}
            loading={modelsQuery.isLoading}
            errorMessage={
                modelsQuery.isError ? t("models.loadFailed") : undefined
            }
            onRetry={() => void modelsQuery.refetch()}
            emptyText={t("models.empty")}
            filteredEmptyText={t("models.filteredEmpty")}
            isFiltered={normalizedSearch.length > 0 || category !== "All"}
            columns={[
                {
                    title: t("models.columns.id"),
                    dataIndex: "id",
                    width: 210,
                    fixed: "left",
                    render: (value: string) => (
                        <Typography.Text>{value}</Typography.Text>
                    ),
                },
                {
                    title: t("models.columns.type"),
                    dataIndex: "category",
                    width: 150,
                    render: (value: LLMModelCategory) =>
                        t(`models.categories.${value}`),
                },
                {
                    title: t("models.columns.provider"),
                    dataIndex: "ownedBy",
                    width: 110,
                    render: (value: string | null) =>
                        value ?? t("common.unknown"),
                },
                {
                    title: t("models.columns.tokenLimit"),
                    key: "tokens",
                    width: 178,
                    render: (_, model) => (
                        <Typography.Text type="secondary">
                            {t("models.columns.tokenSummary", {
                                input: formatTokens(
                                    model.maxInputTokens,
                                    i18n.language,
                                    t,
                                ),
                                output: formatTokens(
                                    model.maxOutputTokens,
                                    i18n.language,
                                    t,
                                ),
                            })}
                        </Typography.Text>
                    ),
                },
                {
                    title: t("models.columns.vision"),
                    dataIndex: "supportsVision",
                    width: 78,
                    render: (value: boolean | null) => (
                        <CapabilityTag value={value} />
                    ),
                },
                {
                    title: t("models.columns.tools"),
                    dataIndex: "supportsTools",
                    width: 78,
                    render: (value: boolean | null) => (
                        <CapabilityTag value={value} />
                    ),
                },
                {
                    title: t("models.columns.structured"),
                    dataIndex: "supportsStructuredOutput",
                    width: 78,
                    render: (value: boolean | null) => (
                        <CapabilityTag value={value} />
                    ),
                },
                {
                    title: t("models.columns.reasoning"),
                    dataIndex: "supportsReasoning",
                    width: 78,
                    render: (value: boolean | null) => (
                        <CapabilityTag value={value} />
                    ),
                },
                {
                    title: t("models.columns.actions"),
                    key: "actions",
                    width: 220,
                    fixed: "right",
                    align: "center",
                    className: "inkwell-action-column",
                    render: (_, model) => (
                        <DataListRowActions>
                            <DataListRowAction
                                label={t("models.viewLabel", {
                                    name: model.id,
                                })}
                                text={t("common.view")}
                                icon={<EyeOutlined />}
                                onClick={() => setSelectedModel(model)}
                            />
                            <DataListRowAction
                                label={t("models.test.actionLabel", {
                                    name: model.id,
                                })}
                                text={t("models.test.action")}
                                icon={<ExperimentOutlined />}
                                loading={
                                    testMutation.isPending &&
                                    testMutation.variables === model.id
                                }
                                onClick={() => testMutation.mutate(model.id)}
                            />
                            {testStates[model.id] === "failure" && (
                                <Tag color="error">
                                    {t("models.test.failureStatus")}
                                </Tag>
                            )}
                        </DataListRowActions>
                    ),
                },
            ]}
        >
            {messageContext}
            <Drawer
                width={600}
                title={t("models.details.title")}
                closable={false}
                open={selectedModel !== null}
                onClose={() => setSelectedModel(null)}
                extra={
                    <Tooltip title={t("common.close")}>
                        <Button
                            type="text"
                            aria-label={t("models.details.closeLabel")}
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
                                    <Typography.Title
                                        level={4}
                                        style={{ margin: 0 }}
                                    >
                                        {selectedModel.id}
                                    </Typography.Title>
                                    <Tag color="processing">
                                        {t(
                                            `models.categories.${selectedModel.category}`,
                                        )}
                                    </Tag>
                                </Flex>
                                <Flex gap={8} wrap style={{ marginTop: 8 }}>
                                    <Tag>
                                        {selectedModel.providerMode ??
                                            t("models.details.unknownMode")}
                                    </Tag>
                                    <Tag>
                                        {selectedModel.ownedBy ??
                                            t("models.details.unknownProvider")}
                                    </Tag>
                                </Flex>
                            </div>
                        </div>

                        <div className="agent-details-content">
                            <section className="agent-details-section">
                                <Space
                                    size={8}
                                    className="agent-details-section-title"
                                >
                                    <ApiOutlined />
                                    <Typography.Text strong>
                                        {t("models.details.information")}
                                    </Typography.Text>
                                </Space>
                                <Descriptions
                                    size="small"
                                    column={1}
                                    items={[
                                        {
                                            key: "id",
                                            label: t("models.details.modelId"),
                                            children: selectedModel.id,
                                        },
                                        {
                                            key: "category",
                                            label: t("models.details.category"),
                                            children: t(
                                                `models.categories.${selectedModel.category}`,
                                            ),
                                        },
                                        {
                                            key: "providerMode",
                                            label: t(
                                                "models.details.providerMode",
                                            ),
                                            children:
                                                selectedModel.providerMode ??
                                                t("common.unknown"),
                                        },
                                        {
                                            key: "ownedBy",
                                            label: t("models.details.ownedBy"),
                                            children:
                                                selectedModel.ownedBy ??
                                                t("common.unknown"),
                                        },
                                    ]}
                                />
                            </section>

                            <section className="agent-details-section">
                                <Space
                                    size={8}
                                    className="agent-details-section-title"
                                >
                                    <DatabaseOutlined />
                                    <Typography.Text strong>
                                        {t("models.details.tokenLimit")}
                                    </Typography.Text>
                                </Space>
                                <div className="model-metric-grid">
                                    <div className="model-metric-item">
                                        <Typography.Text type="secondary">
                                            {t("models.details.maxInput")}
                                        </Typography.Text>
                                        <Typography.Title
                                            level={4}
                                            style={{ margin: 0 }}
                                        >
                                            {formatTokens(
                                                selectedModel.maxInputTokens,
                                                i18n.language,
                                                t,
                                            )}
                                        </Typography.Title>
                                        <Typography.Text type="secondary">
                                            {t("models.details.tokens")}
                                        </Typography.Text>
                                    </div>
                                    <div className="model-metric-item">
                                        <Typography.Text type="secondary">
                                            {t("models.details.maxOutput")}
                                        </Typography.Text>
                                        <Typography.Title
                                            level={4}
                                            style={{ margin: 0 }}
                                        >
                                            {formatTokens(
                                                selectedModel.maxOutputTokens,
                                                i18n.language,
                                                t,
                                            )}
                                        </Typography.Title>
                                        <Typography.Text type="secondary">
                                            {t("models.details.tokens")}
                                        </Typography.Text>
                                    </div>
                                </div>
                            </section>

                            <section className="agent-details-section">
                                <Space
                                    size={8}
                                    className="agent-details-section-title"
                                >
                                    <AppstoreOutlined />
                                    <Typography.Text strong>
                                        {t("models.details.capabilities")}
                                    </Typography.Text>
                                </Space>
                                <div className="model-capability-grid">
                                    <div className="model-capability-item">
                                        <Typography.Text>
                                            {t("models.columns.vision")}
                                        </Typography.Text>
                                        <CapabilityTag
                                            value={selectedModel.supportsVision}
                                        />
                                    </div>
                                    <div className="model-capability-item">
                                        <Typography.Text>
                                            {t("models.details.toolCalls")}
                                        </Typography.Text>
                                        <CapabilityTag
                                            value={selectedModel.supportsTools}
                                        />
                                    </div>
                                    <div className="model-capability-item">
                                        <Typography.Text>
                                            {t(
                                                "models.details.structuredOutput",
                                            )}
                                        </Typography.Text>
                                        <CapabilityTag
                                            value={
                                                selectedModel.supportsStructuredOutput
                                            }
                                        />
                                    </div>
                                    <div className="model-capability-item">
                                        <Typography.Text>
                                            {t("models.columns.reasoning")}
                                        </Typography.Text>
                                        <CapabilityTag
                                            value={
                                                selectedModel.supportsReasoning
                                            }
                                        />
                                    </div>
                                </div>
                            </section>

                            <section className="agent-details-section">
                                <Typography.Text type="secondary">
                                    {t("models.details.dataSourceDescription")}
                                </Typography.Text>
                            </section>
                        </div>
                    </div>
                )}
            </Drawer>
        </DataListPage>
    );
}
