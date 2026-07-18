import { useState } from "react";
import {
    Drawer,
    Space,
    Typography,
    theme as antdTheme,
} from "antd";
import { EyeOutlined } from "@ant-design/icons";
import ResourceListPage, { ResourceRowAction } from "../components/ResourceListPage";

interface ToolItem {
    key: string;
    name: string;
    description: string;
    parameterCount: number;
    updatedTime: string;
    schema: string;
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
    ...Array.from({ length: 22 }, (_, index): ToolItem => ({
        key: `tool-${index + 1}`,
        name: ["document_reader", "ticket_lookup", "time_zone", "url_fetch"][
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
    })),
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

    return (
        <ResourceListPage<ToolItem>
            title="工具"
            description="浏览系统注册的 Function Calling 工具。目录只读，Snapshot 固化 Tool ID 与静态参数。"
            refreshLabel="刷新工具"
            searchValue={searchText}
            searchPlaceholder="搜索名称或描述"
            onSearchChange={setSearchText}
            paginationResetKey={searchText}
            dataSource={filteredTools}
            rowKey="key"
            tableScrollX={800}
            totalLabel={(total) => `共 ${total} 个 Tool`}
            columns={[
                {
                    title: "名称",
                    dataIndex: "name",
                    width: 210,
                    render: (value: string) => <Typography.Text code>{value}</Typography.Text>,
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
                width={520}
                title="Tool 详情"
                open={selectedTool !== null}
                onClose={() => setSelectedTool(null)}
            >
                {selectedTool && (
                    <Space direction="vertical" size={20} style={{ width: "100%" }}>
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
                            <Typography.Text type="secondary">参数 JSON Schema</Typography.Text>
                            <pre
                                style={{
                                    marginTop: 8,
                                    padding: 14,
                                    overflow: "auto",
                                    borderRadius: token.borderRadius,
                                    color: token.colorText,
                                    background: token.colorFillQuaternary,
                                    border: `1px solid ${token.colorBorderSecondary}`,
                                }}
                            >
                                {selectedTool.schema}
                            </pre>
                        </div>
                        <Typography.Text type="secondary">
                            最近更新：{selectedTool.updatedTime}
                        </Typography.Text>
                    </Space>
                )}
            </Drawer>
        </ResourceListPage>
    );
}
