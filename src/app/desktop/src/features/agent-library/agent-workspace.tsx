import {
    PlusOutlined,
    ReloadOutlined,
    RobotOutlined,
    SearchOutlined,
} from "@ant-design/icons";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
    Avatar,
    Button,
    Empty,
    Form,
    Input,
    List,
    Modal,
    Select,
    Skeleton,
    Tag,
    Typography,
    message,
} from "antd";
import { useDeferredValue, useState } from "react";
import { desktopApi } from "../../shared/network/desktop-api";
import type {
    AgentListItem,
    CreateAgentRequest,
} from "../../shared/network/contracts";
import { ChatPanel } from "../chat/chat-panel";

export function AgentWorkspace() {
    const queryClient = useQueryClient();
    const [search, setSearch] = useState("");
    const deferredSearch = useDeferredValue(search);
    const [selectedAgent, setSelectedAgent] = useState<AgentListItem | null>(
        null,
    );
    const [createOpen, setCreateOpen] = useState(false);
    const [form] = Form.useForm<CreateAgentRequest>();
    const [messageApi, contextHolder] = message.useMessage();
    const agentsQuery = useQuery({
        queryKey: ["agents"],
        queryFn: desktopApi.listAgents,
    });
    const modelsQuery = useQuery({
        queryKey: ["models"],
        queryFn: desktopApi.listModels,
    });
    const createMutation = useMutation({
        mutationFn: desktopApi.createAgent,
        onSuccess: async () => {
            await queryClient.invalidateQueries({ queryKey: ["agents"] });
            setCreateOpen(false);
            form.resetFields();
            messageApi.success("Agent 已创建并发布");
        },
        onError: (reason) =>
            messageApi.error(
                reason instanceof Error ? reason.message : "创建 Agent 失败。",
            ),
    });
    const agents = (agentsQuery.data ?? []).filter((agent) =>
        agent.name
            .toLocaleLowerCase()
            .includes(deferredSearch.trim().toLocaleLowerCase()),
    );
    const availableModels = (modelsQuery.data ?? []).filter(
        (model) => model.isAvailable,
    );

    return (
        <>
            {contextHolder}
            <main className="workspace-main">
                    <section className="library-pane">
                        <div className="pane-heading">
                            <div>
                                <Typography.Title level={3}>
                                    Agent 库
                                </Typography.Title>
                                <Typography.Text type="secondary">
                                    我的 Agent
                                </Typography.Text>
                            </div>
                            <Button
                                type="primary"
                                icon={<PlusOutlined />}
                                onClick={() => setCreateOpen(true)}
                            >
                                新建
                            </Button>
                        </div>
                        <Input
                            allowClear
                            prefix={<SearchOutlined />}
                            placeholder="搜索 Agent"
                            value={search}
                            onChange={(event) => setSearch(event.target.value)}
                        />
                        {agentsQuery.isLoading ? (
                            <Skeleton active />
                        ) : agentsQuery.isError ? (
                            <Empty description="无法读取 Agent">
                                <Button
                                    icon={<ReloadOutlined />}
                                    onClick={() => void agentsQuery.refetch()}
                                >
                                    重试
                                </Button>
                            </Empty>
                        ) : (
                            <List
                                className="agent-list"
                                dataSource={agents}
                                locale={{
                                    emptyText: (
                                        <Empty description="还没有 Agent" />
                                    ),
                                }}
                                renderItem={(agent) => (
                                    <List.Item
                                        className={
                                            selectedAgent?.id === agent.id
                                                ? "selected"
                                                : ""
                                        }
                                        onClick={() => setSelectedAgent(agent)}
                                    >
                                        <List.Item.Meta
                                            avatar={
                                                <Avatar
                                                    className="agent-avatar"
                                                    icon={<RobotOutlined />}
                                                />
                                            }
                                            title={
                                                <div className="agent-title">
                                                    <span>{agent.name}</span>
                                                    <Tag>
                                                        v
                                                        {
                                                            agent.latestPublishedVersionNumber
                                                        }
                                                    </Tag>
                                                </div>
                                            }
                                            description={
                                                agent.descriptionExcerpt ||
                                                "暂无描述"
                                            }
                                        />
                                    </List.Item>
                                )}
                            />
                        )}
                    </section>
                    <ChatPanel key={selectedAgent?.id ?? "empty"} agent={selectedAgent} />
            </main>
            <Modal
                title="新建 Agent"
                open={createOpen}
                okText="创建并发布"
                cancelText="取消"
                confirmLoading={createMutation.isPending}
                onCancel={() => setCreateOpen(false)}
                onOk={() =>
                    void form
                        .validateFields()
                        .then((values) => createMutation.mutate(values))
                }
            >
                <Form form={form} layout="vertical" requiredMark={false}>
                    <Form.Item
                        label="名称"
                        name="name"
                        rules={[
                            { required: true, message: "请输入 Agent 名称" },
                        ]}
                    >
                        <Input placeholder="例如：研发助手" maxLength={80} />
                    </Form.Item>
                    <Form.Item label="描述" name="description">
                        <Input
                            placeholder="这个 Agent 适合做什么"
                            maxLength={200}
                        />
                    </Form.Item>
                    <Form.Item
                        label="模型"
                        name="modelId"
                        rules={[{ required: true, message: "请选择可用模型" }]}
                    >
                        <Select
                            loading={modelsQuery.isLoading}
                            placeholder={
                                availableModels.length
                                    ? "选择模型"
                                    : "暂无可用模型"
                            }
                            options={availableModels.map((model) => ({
                                value: model.id,
                                label: `${model.displayName} · ${model.publisherDisplayName ?? model.sourceId}`,
                            }))}
                        />
                    </Form.Item>
                    <Form.Item
                        label="系统指令"
                        name="instructions"
                        rules={[{ required: true, message: "请输入系统指令" }]}
                    >
                        <Input.TextArea
                            rows={5}
                            placeholder="描述 Agent 的角色、目标和回答边界"
                            maxLength={4000}
                            showCount
                        />
                    </Form.Item>
                </Form>
            </Modal>
        </>
    );
}
