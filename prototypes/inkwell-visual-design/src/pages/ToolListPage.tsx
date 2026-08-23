import { useState } from "react";
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
    theme as antdTheme,
} from "antd";
import {
    CalendarOutlined,
    CloseOutlined,
    CodeOutlined,
    EyeOutlined,
    FormOutlined,
    ToolOutlined,
} from "@ant-design/icons";
import ResourceListPage, {
    ResourceRowAction,
} from "../components/ResourceListPage";

interface ToolItem {
    key: string;
    name: string;
    description: string;
    parameterCount: number;
    updatedTime: string;
    schema: string;
}

interface ToolParameter {
    name: string;
    type: string;
    required: boolean;
    options: string[];
}

function getToolParameters(tool: ToolItem): ToolParameter[] {
    const schema = JSON.parse(tool.schema) as {
        required?: string[];
        properties?: Record<
            string,
            { type?: string; enum?: Array<string | number | boolean> }
        >;
    };
    const required = new Set(schema.required ?? []);

    return Object.entries(schema.properties ?? {}).map(([name, property]) => ({
        name,
        type: property.type ?? "未知",
        required: required.has(name),
        options: property.enum?.map(String) ?? [],
    }));
}

const INITIAL_TOOLS: ToolItem[] = [
    {
        key: "web-search",
        name: "web_search",
        description: "检索公开网页并返回带来源的结果摘要。",
        parameterCount: 3,
        updatedTime: "2026-07-17 16:42",
        schema: '{\n  "type": "object",\n  "required": ["query"],\n  "properties": {\n    "query": { "type": "string" },\n    "maxResults": { "type": "integer" },\n    "language": { "type": "string" }\n  }\n}',
    },
    {
        key: "calculator",
        name: "calculator",
        description: "执行确定性的数学表达式计算。",
        parameterCount: 1,
        updatedTime: "2026-07-12 09:18",
        schema: '{\n  "type": "object",\n  "required": ["expression"],\n  "properties": {\n    "expression": { "type": "string" }\n  }\n}',
    },
    {
        key: "weather",
        name: "weather_forecast",
        description: "按城市查询未来七天的天气预报。",
        parameterCount: 2,
        updatedTime: "2026-07-10 11:06",
        schema: '{\n  "type": "object",\n  "required": ["city"],\n  "properties": {\n    "city": { "type": "string" },\n    "days": { "type": "integer" }\n  }\n}',
    },
    ...Array.from(
        { length: 22 },
        (_, index): ToolItem => ({
            key: `tool-${index + 1}`,
            name:
                ["document_reader", "ticket_lookup", "time_zone", "url_fetch"][
                    index % 4
                ] + `_${index + 1}`,
            description: [
                "读取指定资源并返回结构化内容。",
                "查询业务记录并返回当前状态。",
                "执行受控的辅助能力调用。",
            ][index % 3],
            parameterCount: (index % 4) + 1,
            updatedTime: `2026-06-${String(28 - (index % 20)).padStart(2, "0")} 14:20`,
            schema: '{\n  "type": "object",\n  "properties": {\n    "input": { "type": "string" }\n  }\n}',
        }),
    ),
];

export default function ToolListPage() {
    const { token } = antdTheme.useToken();
    const [searchText, setSearchText] = useState("");
    const [selectedTool, setSelectedTool] = useState<ToolItem | null>(null);

    const filteredTools = INITIAL_TOOLS.filter((tool) =>
        `${tool.name} ${tool.description}`
            .toLowerCase()
            .includes(searchText.trim().toLowerCase()),
    );
    const selectedParameters = selectedTool
        ? getToolParameters(selectedTool)
        : [];

    return (
        <ResourceListPage<ToolItem>
            title="工具"
            description="查看 Agent 可使用的工具。工具帮助 Agent 查询信息、调用服务或完成具体操作。"
            refreshLabel="刷新工具"
            searchValue={searchText}
            searchPlaceholder="搜索名称或描述"
            onSearchChange={setSearchText}
            paginationResetKey={searchText}
            dataSource={filteredTools}
            rowKey="key"
            tableScrollX={800}
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
                    dataIndex: "parameterCount",
                    width: 90,
                    render: (value: number) => `${value} 项`,
                },
                { title: "更新时间", dataIndex: "updatedTime", width: 168 },
                {
                    title: "操作",
                    key: "actions",
                    width: 92,
                    fixed: "right",
                    align: "center",
                    className: "inkwell-action-column",
                    render: (_, tool) => (
                        <ResourceRowAction
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
                className="inkwell-resource-details-drawer"
                styles={{ body: { padding: 0 } }}
            >
                {selectedTool && (
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
                                icon={<ToolOutlined />}
                                style={{ background: token.colorPrimary }}
                            />
                            <div className="inkwell-agent-details-identity-copy">
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
                                        {selectedTool.parameterCount} 个参数
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
                                    {selectedTool.updatedTime}
                                </Typography.Text>
                            </div>
                        </div>

                        <div className="inkwell-agent-details-content">
                            <section className="inkwell-agent-details-section">
                                <Space
                                    size={8}
                                    className="inkwell-agent-details-section-title"
                                >
                                    <FormOutlined />
                                    <Typography.Text strong>
                                        参数
                                    </Typography.Text>
                                </Space>
                                <Table<ToolParameter>
                                    size="small"
                                    rowKey="name"
                                    pagination={false}
                                    dataSource={selectedParameters}
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
                                            width: 96,
                                        },
                                        {
                                            title: "必填",
                                            dataIndex: "required",
                                            width: 80,
                                            render: (value: boolean) => (
                                                <Tag
                                                    color={
                                                        value
                                                            ? "processing"
                                                            : "default"
                                                    }
                                                >
                                                    {value ? "是" : "否"}
                                                </Tag>
                                            ),
                                        },
                                        {
                                            title: "可选值",
                                            dataIndex: "options",
                                            width: 120,
                                            render: (value: string[]) =>
                                                value.length > 0
                                                    ? value.join("、")
                                                    : "—",
                                        },
                                    ]}
                                />
                            </section>

                            <section className="inkwell-agent-details-section">
                                <Space
                                    size={8}
                                    className="inkwell-agent-details-section-title"
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
                                                <pre className="inkwell-resource-schema">
                                                    {selectedTool.schema}
                                                </pre>
                                            ),
                                        },
                                    ]}
                                />
                            </section>

                            <section className="inkwell-agent-details-section">
                                <Space
                                    size={8}
                                    className="inkwell-agent-details-section-title"
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
                                            children: selectedTool.updatedTime,
                                        },
                                    ]}
                                />
                            </section>
                        </div>
                    </div>
                )}
            </Drawer>
        </ResourceListPage>
    );
}
