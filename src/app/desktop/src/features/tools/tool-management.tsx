import {
    CalendarOutlined,
    CloseOutlined,
    CodeOutlined,
    EyeOutlined,
    FormOutlined,
    ToolOutlined,
} from "@ant-design/icons";
import { useQuery } from "@tanstack/react-query";
import {
    Avatar,
    Button,
    Collapse,
    Descriptions,
    Drawer,
    Flex,
    Space,
    Table,
    Tag,
    Tooltip,
    Typography,
    theme,
} from "antd";
import { useState } from "react";
import type { TFunction } from "i18next";
import { useTranslation } from "react-i18next";
import DataListPage, {
    DataListRowAction,
} from "../../shared/components/data-list-page";
import { desktopApi } from "../../shared/network/desktop-api";
import type { AgentToolDefinition } from "../../shared/network/contracts";

interface ToolParameter {
    name: string;
    type: string;
    required: boolean;
    allowedValues: string[];
}

interface JsonSchemaProperty {
    type?: unknown;
    enum?: unknown;
}

const isRecord = (value: unknown): value is Record<string, unknown> =>
    typeof value === "object" && value !== null && !Array.isArray(value);

const parseToolParameters = (
    schemaText: string,
    t: TFunction,
): ToolParameter[] => {
    try {
        const schema: unknown = JSON.parse(schemaText);
        if (!isRecord(schema) || !isRecord(schema.properties)) {
            return [];
        }

        const required = new Set(
            Array.isArray(schema.required)
                ? schema.required.filter(
                      (value): value is string => typeof value === "string",
                  )
                : [],
        );

        return Object.entries(schema.properties).map(([name, value]) => {
            const property: JsonSchemaProperty = isRecord(value) ? value : {};
            const type = Array.isArray(property.type)
                ? property.type
                      .filter(
                          (item): item is string => typeof item === "string",
                      )
                      .join(" | ")
                : typeof property.type === "string"
                  ? property.type
                  : t("common.unknown");
            const allowedValues = Array.isArray(property.enum)
                ? property.enum.map(String)
                : [];

            return {
                name,
                type,
                required: required.has(name),
                allowedValues,
            };
        });
    } catch {
        return [];
    }
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

export function ToolManagement() {
    const { t, i18n } = useTranslation();
    const { token } = theme.useToken();
    const [searchText, setSearchText] = useState("");
    const [selectedTool, setSelectedTool] =
        useState<AgentToolDefinition | null>(null);
    const toolsQuery = useQuery({
        queryKey: ["tools"],
        queryFn: desktopApi.listTools,
    });
    const normalizedSearch = searchText.trim().toLocaleLowerCase();
    const tools = (toolsQuery.data ?? []).filter((tool) =>
        `${tool.name} ${tool.description}`
            .toLocaleLowerCase()
            .includes(normalizedSearch),
    );
    const selectedParameters = selectedTool
        ? parseToolParameters(selectedTool.parametersJsonSchema, t)
        : [];

    return (
        <DataListPage<AgentToolDefinition>
            title={t("tools.title")}
            description={t("tools.description")}
            refreshLabel={t("tools.refreshLabel")}
            onRefresh={() => void toolsQuery.refetch()}
            refreshing={toolsQuery.isFetching && !toolsQuery.isLoading}
            searchValue={searchText}
            searchPlaceholder={t("tools.searchPlaceholder")}
            searchMaxLength={128}
            onSearchChange={setSearchText}
            paginationResetKey={searchText}
            dataSource={tools}
            rowKey="id"
            tableScrollX={800}
            loading={toolsQuery.isLoading}
            errorMessage={
                toolsQuery.isError ? t("tools.loadFailed") : undefined
            }
            onRetry={() => void toolsQuery.refetch()}
            emptyText={t("tools.empty")}
            filteredEmptyText={t("tools.filteredEmpty")}
            isFiltered={normalizedSearch.length > 0}
            columns={[
                {
                    title: t("tools.columns.name"),
                    dataIndex: "name",
                    width: 210,
                    render: (value: string) => (
                        <Typography.Text code>{value}</Typography.Text>
                    ),
                },
                {
                    title: t("tools.columns.description"),
                    dataIndex: "description",
                    ellipsis: true,
                },
                {
                    title: t("tools.columns.parameters"),
                    dataIndex: "parametersJsonSchema",
                    width: 90,
                    render: (value: string) =>
                        t("tools.parameterCount", {
                            count: parseToolParameters(value, t).length,
                        }),
                },
                {
                    title: t("tools.columns.updatedTime"),
                    dataIndex: "updatedTime",
                    width: 168,
                    render: (value: string) => formatTime(value, i18n.language),
                },
                {
                    title: t("tools.columns.actions"),
                    key: "actions",
                    width: 92,
                    fixed: "right",
                    align: "center",
                    className: "inkwell-action-column",
                    render: (_, tool) => (
                        <DataListRowAction
                            label={t("tools.viewLabel", { name: tool.name })}
                            text={t("common.view")}
                            icon={<EyeOutlined />}
                            onClick={() => setSelectedTool(tool)}
                        />
                    ),
                },
            ]}
        >
            <Drawer
                width={600}
                title={t("tools.details.title")}
                closable={false}
                open={selectedTool !== null}
                onClose={() => setSelectedTool(null)}
                extra={
                    <Tooltip title={t("common.close")}>
                        <Button
                            type="text"
                            aria-label={t("tools.details.closeLabel")}
                            icon={<CloseOutlined />}
                            onClick={() => setSelectedTool(null)}
                        />
                    </Tooltip>
                }
                className="resource-details-drawer"
                styles={{ body: { padding: 0 } }}
            >
                {selectedTool && (
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
                                icon={<ToolOutlined />}
                                style={{ background: token.colorPrimary }}
                            />
                            <div className="agent-details-identity-copy">
                                <Flex align="center" gap={8} wrap>
                                    <Typography.Title
                                        level={4}
                                        style={{ margin: 0 }}
                                    >
                                        <Typography.Text code>
                                            {selectedTool.name}
                                        </Typography.Text>
                                    </Typography.Title>
                                    <Tag>
                                        {t("tools.details.parameterCount", {
                                            count: selectedParameters.length,
                                        })}
                                    </Tag>
                                </Flex>
                                <Typography.Paragraph
                                    type="secondary"
                                    style={{ margin: "6px 0 0" }}
                                >
                                    {selectedTool.description}
                                </Typography.Paragraph>
                                <Typography.Text type="secondary">
                                    <CalendarOutlined />{" "}
                                    {formatTime(
                                        selectedTool.updatedTime,
                                        i18n.language,
                                    )}
                                </Typography.Text>
                            </div>
                        </div>

                        <div className="agent-details-content">
                            <section className="agent-details-section">
                                <Space
                                    size={8}
                                    className="agent-details-section-title"
                                >
                                    <FormOutlined />
                                    <Typography.Text strong>
                                        {t("tools.details.parameters")}
                                    </Typography.Text>
                                </Space>
                                <Table<ToolParameter>
                                    size="small"
                                    pagination={false}
                                    rowKey="name"
                                    dataSource={selectedParameters}
                                    locale={{
                                        emptyText: t(
                                            "tools.details.noParameters",
                                        ),
                                    }}
                                    columns={[
                                        {
                                            title: t("tools.columns.name"),
                                            dataIndex: "name",
                                            render: (value: string) => (
                                                <Typography.Text code>
                                                    {value}
                                                </Typography.Text>
                                            ),
                                        },
                                        {
                                            title: t("tools.details.type"),
                                            dataIndex: "type",
                                            width: 92,
                                        },
                                        {
                                            title: t("tools.details.required"),
                                            dataIndex: "required",
                                            width: 72,
                                            render: (value: boolean) =>
                                                value ? (
                                                    <Tag color="processing">
                                                        {t("common.yes")}
                                                    </Tag>
                                                ) : (
                                                    <Tag>{t("common.no")}</Tag>
                                                ),
                                        },
                                        {
                                            title: t(
                                                "tools.details.allowedValues",
                                            ),
                                            dataIndex: "allowedValues",
                                            render: (values: string[]) =>
                                                values.length > 0
                                                    ? values.join(
                                                          t(
                                                              "common.listSeparator",
                                                          ),
                                                      )
                                                    : "—",
                                        },
                                    ]}
                                />
                            </section>

                            <section className="agent-details-section">
                                <Space
                                    size={8}
                                    className="agent-details-section-title"
                                >
                                    <CodeOutlined />
                                    <Typography.Text strong>
                                        {t("tools.details.rawSchema")}
                                    </Typography.Text>
                                </Space>
                                <Collapse
                                    size="small"
                                    items={[
                                        {
                                            key: "schema",
                                            label: t(
                                                "tools.details.viewSchema",
                                            ),
                                            children: (
                                                <pre className="resource-schema">
                                                    {
                                                        selectedTool.parametersJsonSchema
                                                    }
                                                </pre>
                                            ),
                                        },
                                    ]}
                                />
                            </section>

                            <section className="agent-details-section">
                                <Space
                                    size={8}
                                    className="agent-details-section-title"
                                >
                                    <CalendarOutlined />
                                    <Typography.Text strong>
                                        {t("tools.details.timeInformation")}
                                    </Typography.Text>
                                </Space>
                                <Descriptions
                                    size="small"
                                    column={1}
                                    items={[
                                        {
                                            key: "updated",
                                            label: t(
                                                "tools.columns.updatedTime",
                                            ),
                                            children: formatTime(
                                                selectedTool.updatedTime,
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
        </DataListPage>
    );
}
