import { useEffect, useState } from "react";
import { Card, Col, Row, Statistic, Typography, Empty, Spin } from "antd";
import {
  FileTextOutlined,
  ThunderboltOutlined,
  CheckCircleOutlined,
} from "@ant-design/icons";

const API_BASE = "http://localhost:5000";

interface DashboardStats {
  totalRuns: number;
  publishedArticles: number;
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
      <Row gutter={16} style={{ marginBottom: 24 }}>
        <Col span={8}>
          <Card>
            <Statistic
              title="流水线运行次数"
              value={stats?.totalRuns ?? 0}
              prefix={<ThunderboltOutlined />}
            />
          </Card>
        </Col>
        <Col span={8}>
          <Card>
            <Statistic
              title="已发布文章"
              value={stats?.publishedArticles ?? 0}
              prefix={<FileTextOutlined />}
            />
          </Card>
        </Col>
        <Col span={8}>
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
      <Card title="最近运行">
        {runs.length === 0 ? (
          <Empty description="暂无运行记录" />
        ) : (
          <table style={{ width: "100%" }}>
            <thead>
              <tr>
                <th style={{ textAlign: "left", padding: 8 }}>主题</th>
                <th style={{ textAlign: "left", padding: 8 }}>状态</th>
                <th style={{ textAlign: "left", padding: 8 }}>开始时间</th>
              </tr>
            </thead>
            <tbody>
              {runs.map((run) => (
                <tr key={run.id}>
                  <td style={{ padding: 8 }}>{run.topic}</td>
                  <td style={{ padding: 8 }}>{run.status}</td>
                  <td style={{ padding: 8 }}>
                    {new Date(run.startedAt).toLocaleString("zh-CN")}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
    </div>
  );
}
