import { Layout, Menu, Typography } from "antd";
import {
  DashboardOutlined,
  MessageOutlined,
  ApartmentOutlined,
  BookOutlined,
} from "@ant-design/icons";
import { Outlet, useNavigate, useLocation } from "react-router-dom";

const { Header, Sider, Content } = Layout;

const menuItems = [
  {
    key: "/",
    icon: <DashboardOutlined />,
    label: "Dashboard",
  },
  {
    key: "/chat",
    icon: <MessageOutlined />,
    label: "Agent 对话",
  },
  {
    key: "/workflows",
    icon: <ApartmentOutlined />,
    label: "Workflow 管理",
  },
  {
    key: "/knowledge",
    icon: <BookOutlined />,
    label: "知识库",
  },
];

export default function AppLayout() {
  const navigate = useNavigate();
  const location = useLocation();

  return (
    <Layout style={{ minHeight: "100vh" }}>
      <Sider theme="dark" width={220}>
        <div
          style={{
            height: 64,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <Typography.Title level={4} style={{ color: "#fff", margin: 0 }}>
            Inkwell
          </Typography.Title>
        </div>
        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={[location.pathname]}
          items={menuItems}
          onClick={({ key }) => navigate(key)}
        />
      </Sider>
      <Layout>
        <Header
          style={{
            background: "#fff",
            padding: "0 24px",
            display: "flex",
            alignItems: "center",
          }}
        >
          <Typography.Text type="secondary">AI 内容生产平台</Typography.Text>
        </Header>
        <Content
          style={{
            margin: 16,
            padding: 24,
            background: "#fff",
            borderRadius: 8,
          }}
        >
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
}
