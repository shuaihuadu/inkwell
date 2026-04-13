import { useMemo, useState } from "react";
import {
  Typography,
  Card,
  List,
  Button,
  Tag,
  Space,
  Spin,
  message,
} from "antd";
import {
  ApartmentOutlined,
  PlayCircleOutlined,
  EyeOutlined,
} from "@ant-design/icons";
import WorkflowTopologyModal from "../../components/workflow-topology-modal";
import WorkflowRunModal from "../../components/workflow-run-modal";
import { useAguiConversationController } from "../../hooks/use-agui-conversation-controller";
import { useApiList } from "../../hooks/use-api-list";
import { useWorkflowTopology } from "../../hooks/use-workflow-topology";

interface WorkflowInfo {
  id: string;
  name: string;
  description: string;
}

export default function WorkflowPage() {
  const { items: workflows, loading } = useApiList<WorkflowInfo>({
    endpoint: "/api/workflows",
    onError: () => {
      message.error("加载 Workflow 列表失败");
    },
  });

  const {
    visible: topoVisible,
    loading: topoLoading,
    data: topoData,
    openTopology,
    closeTopology,
  } = useWorkflowTopology(() => {
    message.error("加载拓扑图失败");
  });

  // 运行弹窗
  const [runVisible, setRunVisible] = useState(false);
  const [runWorkflow, setRunWorkflow] = useState<WorkflowInfo | null>(null);
  const runConversation = useAguiConversationController("/api/agui/writer");

  const workflowRouteOptions = useMemo(
    () =>
      workflows.map((workflow) => ({
        value: `/api/agui/workflow-${workflow.id}`,
        label: workflow.name,
        workflow,
      })),
    [workflows],
  );

  const openRunModal = (workflow: WorkflowInfo) => {
    setRunWorkflow(workflow);
    runConversation.changeRoute(`/api/agui/workflow-${workflow.id}`);
    setRunVisible(true);
  };

  if (loading) {
    return <Spin size="large" style={{ display: "block", marginTop: 100 }} />;
  }

  return (
    <div>
      <Typography.Title level={3}>
        <ApartmentOutlined /> Workflow 管理
      </Typography.Title>

      <List
        grid={{ gutter: 16, column: 2 }}
        dataSource={workflows}
        renderItem={(item) => (
          <List.Item>
            <Card
              title={item.name}
              extra={
                <Space>
                  <Button
                    size="small"
                    icon={<EyeOutlined />}
                    onClick={() => {
                      void openTopology(item.id);
                    }}
                  >
                    拓扑
                  </Button>
                  <Button
                    size="small"
                    type="primary"
                    icon={<PlayCircleOutlined />}
                    onClick={() => openRunModal(item)}
                  >
                    运行
                  </Button>
                </Space>
              }
            >
              <Typography.Text type="secondary">
                {item.description}
              </Typography.Text>
              <br />
              <Tag color="blue" style={{ marginTop: 8 }}>
                {item.id}
              </Tag>
            </Card>
          </List.Item>
        )}
      />

      <WorkflowTopologyModal
        visible={topoVisible}
        loading={topoLoading}
        data={topoData}
        onClose={closeTopology}
      />

      <WorkflowRunModal
        visible={runVisible}
        title={runWorkflow?.name ?? ""}
        options={workflowRouteOptions}
        conversation={runConversation}
        onRouteChange={(route) => {
          const selected = workflowRouteOptions.find(
            (o) => o.value === route,
          )?.workflow;
          setRunWorkflow(selected ?? null);
        }}
        onClose={() => setRunVisible(false)}
      />
    </div>
  );
}
