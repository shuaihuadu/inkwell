import { EyeOutlined } from "@ant-design/icons";
import { useQuery } from "@tanstack/react-query";
import { Collapse, Drawer, Space, Table, Tag, Typography, theme } from "antd";
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
            description="查看系统内置的 Agent 可用工具及其参数定义。工具信息仅供查看，不可编辑。"
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
            totalLabel={(total) => `共 ${total} 个 Tool`}
            loading={toolsQuery.isLoading}
            errorMessage={toolsQuery.isError ? "工具列表加载失败，请稍后重试" : undefined}
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
                width={520}
                title="Tool 详情"
                open={selectedTool !== null}
                onClose={() => setSelectedTool(null)}
            >
                {selectedTool && (
                    <Space orientation="vertical" size={20} style={{ width: "100%" }}>
                        <div>
                            <Typography.Text type="secondary">名称</Typography.Text>
                            <Typography.Title level={5} style={{ margin: "4px 0 0" }}>
                                <Typography.Text code>{selectedTool.name}</Typography.Text>
                            </Typography.Title>
                        </div>
                        <div>
                            <Typography.Text type="secondary">描述</Typography.Text>
                            <Typography.Paragraph style={{ marginTop: 4 }}>
                                {selectedTool.description}
                            </Typography.Paragraph>
                        </div>
                        <div>
                            <Typography.Text type="secondary">参数</Typography.Text>
                            <Table<ToolParameter>
                                size="small"
                                pagination={false}
                                rowKey="name"
                                dataSource={selectedParameters}
                                locale={{ emptyText: "此工具没有参数" }}
                                style={{ marginTop: 8 }}
                                columns={[
                                    {
                                        title: "名称",
                                        dataIndex: "name",
                                        render: (value: string) => (
                                            <Typography.Text code>{value}</Typography.Text>
                                        ),
                                    },
                                    { title: "类型", dataIndex: "type", width: 92 },
                                    {
                                        title: "必填",
                                        dataIndex: "required",
                                        width: 72,
                                        render: (value: boolean) =>
                                            value ? (
                                                <Tag color="success">是</Tag>
                                            ) : (
                                                <Tag>否</Tag>
                                            ),
                                    },
                                    {
                                        title: "可选值",
                                        dataIndex: "allowedValues",
                                        render: (values: string[]) =>
                                            values.length > 0 ? values.join(", ") : "-",
                                    },
                                ]}
                            />
                        </div>
                        <Collapse
                            ghost
                            items={[
                                {
                                    key: "schema",
                                    label: "原始 JSON Schema",
                                    children: (
                                        <pre
                                            className="tool-schema"
                                            style={{
                                                borderRadius: token.borderRadius,
                                                color: token.colorText,
                                                background: token.colorFillQuaternary,
                                                border: `1px solid ${token.colorBorderSecondary}`,
                                            }}
                                        >
                                            {selectedTool.parametersJsonSchema}
                                        </pre>
                                    ),
                                },
                            ]}
                        />
                        <Typography.Text type="secondary">
                            最近更新：{formatTime(selectedTool.updatedTime)}
                        </Typography.Text>
                    </Space>
                )}
            </Drawer>
        </DataListPage>
    );
}