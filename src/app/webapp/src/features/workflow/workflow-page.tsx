import { useEffect, useState } from "react";
import {
  Typography,
  Card,
  List,
  Button,
  Modal,
  Input,
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
import ReactMarkdown from "react-markdown";
import { API_BASE } from "../../services/api";

interface WorkflowInfo {
  id: string;
  name: string;
  description: string;
}

interface TopologyData {
  id: string;
  name: string;
  format: string;
  topology: string;
}

interface SSEEvent {
  type: string;
  workflowId?: string;
  executorId?: string;
  data?: string;
  message?: string;
  hasCheckpoint?: boolean;
}

export default function WorkflowPage() {
  const [workflows, setWorkflows] = useState<WorkflowInfo[]>([]);
  const [loading, setLoading] = useState(true);

  // 拓扑图弹窗
  const [topoVisible, setTopoVisible] = useState(false);
  const [topoData, setTopoData] = useState<TopologyData | null>(null);
  const [topoLoading, setTopoLoading] = useState(false);

  // 运行弹窗
  const [runVisible, setRunVisible] = useState(false);
  const [runWorkflow, setRunWorkflow] = useState<WorkflowInfo | null>(null);
  const [runInput, setRunInput] = useState("");
  const [runEvents, setRunEvents] = useState<SSEEvent[]>([]);
  const [running, setRunning] = useState(false);

  useEffect(() => {
    fetch(`${API_BASE}/api/workflows`)
      .then((res) => res.json())
      .then((data: WorkflowInfo[]) => setWorkflows(data))
      .catch(() => message.error("加载 Workflow 列表失败"))
      .finally(() => setLoading(false));
  }, []);

  const showTopology = async (id: string) => {
    setTopoLoading(true);
    setTopoVisible(true);
    try {
      const res = await fetch(`${API_BASE}/api/workflows/${id}/topology`);
      if (res.ok) {
        setTopoData(await res.json());
      }
    } catch {
      message.error("加载拓扑图失败");
    } finally {
      setTopoLoading(false);
    }
  };

  const startRun = async () => {
    if (!runWorkflow || !runInput.trim()) return;

    setRunning(true);
    setRunEvents([]);

    try {
      const res = await fetch(
        `${API_BASE}/api/workflows/${runWorkflow.id}/run`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ input: runInput }),
        },
      );

      const reader = res.body?.getReader();
      const decoder = new TextDecoder();
      let buffer = "";

      if (reader) {
        while (true) {
          const { done, value } = await reader.read();
          if (done) break;

          buffer += decoder.decode(value, { stream: true });
          const lines = buffer.split("\n");
          buffer = lines.pop() ?? "";

          for (const line of lines) {
            if (!line.startsWith("data: ")) continue;
            const data = line.slice(6).trim();
            if (!data) continue;

            try {
              const event: SSEEvent = JSON.parse(data);
              setRunEvents((prev) => [...prev, event]);
            } catch {
              // 忽略
            }
          }
        }
      }
    } catch (err) {
      message.error(`运行失败: ${(err as Error).message}`);
    } finally {
      setRunning(false);
    }
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
                    onClick={() => showTopology(item.id)}
                  >
                    拓扑
                  </Button>
                  <Button
                    size="small"
                    type="primary"
                    icon={<PlayCircleOutlined />}
                    onClick={() => {
                      setRunWorkflow(item);
                      setRunInput("");
                      setRunEvents([]);
                      setRunVisible(true);
                    }}
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

      {/* 拓扑图弹窗 */}
      <Modal
        title={`拓扑图 — ${topoData?.name ?? ""}`}
        open={topoVisible}
        onCancel={() => setTopoVisible(false)}
        footer={null}
        width={700}
      >
        {topoLoading ? (
          <Spin />
        ) : (
          <pre
            style={{
              background: "#f5f5f5",
              padding: 16,
              borderRadius: 8,
              overflow: "auto",
              maxHeight: 500,
              fontSize: 13,
            }}
          >
            {topoData?.topology ?? "无拓扑数据"}
          </pre>
        )}
      </Modal>

      {/* 运行弹窗 */}
      <Modal
        title={`运行 — ${runWorkflow?.name ?? ""}`}
        open={runVisible}
        onCancel={() => {
          setRunVisible(false);
          setRunning(false);
        }}
        footer={null}
        width={700}
      >
        <Space.Compact style={{ width: "100%", marginBottom: 16 }}>
          <Input
            placeholder="输入内容（如文章主题）"
            value={runInput}
            onChange={(e) => setRunInput(e.target.value)}
            onPressEnter={startRun}
            disabled={running}
          />
          <Button
            type="primary"
            onClick={startRun}
            loading={running}
            disabled={!runInput.trim()}
          >
            {running ? "运行中..." : "开始"}
          </Button>
        </Space.Compact>

        <div
          style={{
            maxHeight: 400,
            overflow: "auto",
            background: "#fafafa",
            padding: 12,
            borderRadius: 8,
          }}
        >
          {runEvents.length === 0 && !running && (
            <Typography.Text type="secondary">
              输入内容后点击"开始"运行 Workflow
            </Typography.Text>
          )}
          {runEvents.map((evt, i) => (
            <div
              key={i}
              style={{
                marginBottom: 8,
                borderBottom: "1px solid #f0f0f0",
                paddingBottom: 8,
              }}
            >
              <Tag
                color={
                  evt.type === "output"
                    ? "green"
                    : evt.type === "executor_complete"
                      ? "blue"
                      : evt.type === "checkpoint"
                        ? "orange"
                        : evt.type === "error"
                          ? "red"
                          : evt.type === "done"
                            ? "cyan"
                            : "default"
                }
              >
                {evt.type}
              </Tag>
              {evt.executorId && <Tag color="purple">{evt.executorId}</Tag>}
              {evt.data && (
                <div style={{ marginTop: 4 }}>
                  <ReactMarkdown>{evt.data}</ReactMarkdown>
                </div>
              )}
              {evt.message && (
                <Typography.Text type="danger">{evt.message}</Typography.Text>
              )}
            </div>
          ))}
        </div>
      </Modal>
    </div>
  );
}
