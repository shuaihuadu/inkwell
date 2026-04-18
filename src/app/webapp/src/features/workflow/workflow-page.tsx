import {
  Typography,
  Card,
  Row,
  Col,
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
import { useNavigate } from "react-router-dom";
import WorkflowTopologyModal from "../../components/workflow-topology-modal";
import { useApiList } from "../../hooks/use-api-list";
import { useWorkflowTopology } from "../../hooks/use-workflow-topology";

interface WorkflowInfo {
  id: string;
  name: string;
  description: string;
}

/**
 * Workflow 列表页
 * 点击“运行”跳转至 /workflows/:id/run 独立运行页，
 * 运行页内含 Run 历史侧栏与 AG-UI 聊天窗（与 Agent 对话体验一致）
 */
export default function WorkflowPage() {
  const navigate = useNavigate();

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

  if (loading) {
    return <Spin size="large" style={{ display: "block", marginTop: 100 }} />;
  }

  return (
    <div>
      <Typography.Title level={3}>
        <ApartmentOutlined /> Workflow 管理
      </Typography.Title>

      <Row gutter={[16, 16]}>
        {workflows.map((item) => (
          <Col key={item.id} xs={24} sm={12}>
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
                    onClick={() => navigate(`/workflows/${item.id}/run`)}
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
          </Col>
        ))}
      </Row>

      <WorkflowTopologyModal
        visible={topoVisible}
        loading={topoLoading}
        data={topoData}
        onClose={closeTopology}
      />
    </div>
  );
}
