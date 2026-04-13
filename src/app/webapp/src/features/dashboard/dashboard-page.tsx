import { useEffect, useState } from "react";
import { Card, Col, Row, Statistic, Typography, Empty, Spin, Table, Tag } from "antd";
import {
  FileTextOutlined,
  ThunderboltOutlined,
  CheckCircleOutlined,
  RobotOutlined,
  ApartmentOutlined,
} from "@ant-design/icons";
import { API_BASE } from "../../services/api";

interface DashboardStats {
  agentCount: number;
  workflowCount: number;
  totalRuns: number;
  publishedArticles: number;
  totalArticles: number;
  completedRuns: number;
  approvalRate: number;
}

interface PipelineRun {
  id: string;
  topic: string;
  status: string;
  startedAt: string;
  completedAt?: string;
}

export default function DashboardPage() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [runs, setRuns] = useState<PipelineRun[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function fetchData() {
      try {
        const [statsRes, runsRes] = await Promise.all([
          fetch(`${API_BASE}/api/dashboard/stats`),
          fetch(`${API_BASE}/api/pipeline/runs?count=10`),
        ]);

        if (statsRes.ok) {
          setStats(await statsRes.json());
        }
        if (runsRes.ok) {
          setRuns(await runsRes.json());
        }
      } catch {
        // 后端未启动时静默失败
      } finally {
        setLoading(false);
      }
    }

    fetchData();
  }, []);

  if (loading) {
    return <Spin size="large" style={{ display: "block", marginTop: 100 }} />;
  }

  return (
    <div>
      <Typography.Title level={3}>Dashboard</Typography.Title>

      {/* 第一行：Agent / Workflow / 运行次数 */}
      <Row gutter={16} style={{ marginBottom: 16 }}>
        <Col span={6}>
          <Card>
            <Statistic
              title="Agent 数量"
              value={stats?.agentCount ?? 0}
              prefix={<RobotOutlined />}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title="Workflow 数量"
              value={stats?.workflowCount ?? 0}
              prefix={<ApartmentOutlined />}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title="流水线运行"
              value={stats?.totalRuns ?? 0}
              prefix={<ThunderboltOutlined />}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title="审核通过率"
              value={stats?.approvalRate ?? 0}
              suffix="%"
              prefix={<CheckCircleOutlined />}
            />
          </Card>
        </Col>
      </Row>

      {/* 第二行：文章统计 */}
      <Row gutter={16} style={{ marginBottom: 24 }}>
        <Col span={8}>
          <Card>
            <Statistic
              title="文章总数"
              value={stats?.totalArticles ?? 0}
              prefix={<FileTextOutlined />}
            />
          </Card>
        </Col>
        <Col span={8}>
          <Card>
            <Statistic
              title="已发布文章"
              value={stats?.publishedArticles ?? 0}
              valueStyle={{ color: "#3f8600" }}
              prefix={<FileTextOutlined />}
            />
          </Card>
        </Col>
        <Col span={8}>
          <Card>
            <Statistic
              title="已完成运行"
              value={stats?.completedRuns ?? 0}
              prefix={<CheckCircleOutlined />}
            />
          </Card>
        </Col>
      </Row>
      <Card title="最近运行">
        {runs.length === 0 ? (
          <Empty description="暂无运行记录" />
        ) : (
          <Table
            dataSource={runs}
            rowKey="id"
            pagination={false}
            size="small"
            columns={[
              {
                title: "主题",
                dataIndex: "topic",
                key: "topic",
                render: (text: string) => <strong>{text}</strong>,
              },
              {
                title: "状态",
                dataIndex: "status",
                key: "status",
                width: 120,
                render: (status: string) => (
                  <Tag
                    color={
                      status === "Completed"
                        ? "green"
                        : status === "Running"
                        ? "blue"
                        : status === "Failed"
                        ? "red"
                        : status === "Cancelled"
                        ? "orange"
                        : "default"
                    }
                  >
                    {status}
                  </Tag>
                ),
              },
              {
                title: "开始时间",
                dataIndex: "startedAt",
                key: "startedAt",
                width: 180,
                render: (date: string) =>
                  new Date(date).toLocaleString("zh-CN"),
              },
            ]}
          />
        )}
      </Card>
    </div>
  );
}
