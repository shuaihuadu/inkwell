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

const parseToolParameters = (schemaText: string): ToolParameter[] => {
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
                  : "未知";
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

const formatTime = (value: string): string =>
    new Intl.DateTimeFormat("zh-CN", {
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
        hour12: false,
    }).format(new Date(value));

export function ToolManagement() {
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
        ? parseToolParameters(selectedTool.parametersJsonSchema)
        : [];

    return (
        <DataListPage<AgentToolDefinition>
            title="工具"
            description="查看 Agent 可使用的工具。工具帮助 Agent 查询信息、调用服务或完成具体操作。"
            refreshLabel="刷新工具"
            onRefresh={() => void toolsQuery.refetch()}
            refreshing={toolsQuery.isFetching && !toolsQuery.isLoading}
            searchValue={searchText}
            searchPlaceholder="搜索名称或描述"
            searchMaxLength={128}
            onSearchChange={setSearchText}
            paginationResetKey={searchText}
            dataSource={tools}
            rowKey="id"
            tableScrollX={800}
            loading={toolsQuery.isLoading}
            errorMessage={
                toolsQuery.isError ? "工具列表加载失败，请稍后重试" : undefined
            }
            onRetry={() => void toolsQuery.refetch()}
            emptyText="当前没有已注册的工具"
            filteredEmptyText="没有匹配的工具，请清除搜索条件"
            isFiltered={normalizedSearch.length > 0}
            columns={[
                {
                    title: "名称",
                    dataIndex: "name",
                    width: 210,
                    render: (value: string) => (
                        <Typography.Text code>{value}</Typography.Text>
                    ),
                },
                {
                    title: "描述",
                    dataIndex: "description",
                    ellipsis: true,
                },
                {
                    title: "参数",
                    dataIndex: "parametersJsonSchema",
                    width: 90,
                    render: (value: string) =>
                        `${parseToolParameters(value).length} 项`,
                },
                {
                    title: "更新时间",
                    dataIndex: "updatedTime",
                    width: 168,
                    render: formatTime,
                },
                {
                    title: "操作",
                    key: "actions",
                    width: 92,
                    fixed: "right",
                    align: "center",
                    className: "inkwell-action-column",
                    render: (_, tool) => (
                        <DataListRowAction
                            label={`查看 ${tool.name}`}
                            text="查看"
                            icon={<EyeOutlined />}
                            onClick={() => setSelectedTool(tool)}
                        />
                    ),
                },
            ]}
        >
            <Drawer
                width={600}
                title="Tool 详情"
                closable={false}
                open={selectedTool !== null}
                onClose={() => setSelectedTool(null)}
                extra={
                    <Tooltip title="关闭">
                        <Button
                            type="text"
                            aria-label="关闭 Tool 详情"
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
                                        {selectedParameters.length} 个参数
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
                                    {formatTime(selectedTool.updatedTime)}
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
                                        参数
                                    </Typography.Text>
                                </Space>
                                <Table<ToolParameter>
                                    size="small"
                                    pagination={false}
                                    rowKey="name"
                                    dataSource={selectedParameters}
                                    locale={{ emptyText: "此工具没有参数" }}
                                    columns={[
                                        {
                                            title: "名称",
                                            dataIndex: "name",
                                            render: (value: string) => (
                                                <Typography.Text code>
                                                    {value}
                                                </Typography.Text>
                                            ),
                                        },
                                        {
                                            title: "类型",
                                            dataIndex: "type",
                                            width: 92,
                                        },
                                        {
                                            title: "必填",
                                            dataIndex: "required",
                                            width: 72,
                                            render: (value: boolean) =>
                                                value ? (
                                                    <Tag color="processing">
                                                        是
                                                    </Tag>
                                                ) : (
                                                    <Tag>否</Tag>
                                                ),
                                        },
                                        {
                                            title: "可选值",
                                            dataIndex: "allowedValues",
                                            render: (values: string[]) =>
                                                values.length > 0
                                                    ? values.join("、")
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
                                        原始 JSON Schema
                                    </Typography.Text>
                                </Space>
                                <Collapse
                                    size="small"
                                    items={[
                                        {
                                            key: "schema",
                                            label: "查看原始 Schema",
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
                                        时间信息
                                    </Typography.Text>
                                </Space>
                                <Descriptions
                                    size="small"
                                    column={1}
                                    items={[
                                        {
                                            key: "updated",
                                            label: "更新时间",
                                            children: formatTime(
                                                selectedTool.updatedTime,
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
