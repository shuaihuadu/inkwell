import { Card, Col, Row, Statistic, Typography, Empty } from "antd";
import {
  FileTextOutlined,
  ThunderboltOutlined,
  CheckCircleOutlined,
} from "@ant-design/icons";

export default function DashboardPage() {
  return (
    <div>
      <Typography.Title level={3}>Dashboard</Typography.Title>
      <Row gutter={16} style={{ marginBottom: 24 }}>
        <Col span={8}>
          <Card>
            <Statistic
              title="流水线运行次数"
              value={0}
              prefix={<ThunderboltOutlined />}
            />
          </Card>
        </Col>
        <Col span={8}>
          <Card>
            <Statistic
              title="已发布文章"
              value={0}
              prefix={<FileTextOutlined />}
            />
          </Card>
        </Col>
        <Col span={8}>
          <Card>
            <Statistic
              title="审核通过率"
              value={0}
              suffix="%"
              prefix={<CheckCircleOutlined />}
            />
          </Card>
        </Col>
      </Row>
      <Card title="最近运行">
        <Empty description="暂无运行记录" />
      </Card>
    </div>
  );
}
