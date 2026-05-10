import { ProLayout } from '@ant-design/pro-components';
import {
  AppstoreOutlined,
  ApartmentOutlined,
  SafetyCertificateOutlined,
  UserOutlined
} from '@ant-design/icons';
import { Badge, Dropdown, Space, Tag, Switch } from 'antd';
import { Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useAppContext, ROLES, ROLE_OPTIONS } from '../AppContext';

/**
 * 应用外壳：顶栏 + 左侧 nav + 主区（依 OQ-011 closed 2026-05-08）
 * - 顶栏：应用标识 + 网络状态徽标 + 当前用户菜单（含「管理」入口仅 is_super=true 可见）
 * - 左 nav：Agent 库 / 编排 / Admin（Admin 仅 is_super=true 显示）
 * - 主区：<Outlet />
 */
export default function AppLayout() {
  const navigate = useNavigate();
  const location = useLocation();
  const { role, setRoleKey, locked, setLocked, offline, setOffline } =
    useAppContext();

  return (
    <ProLayout
      title="Inkwell Agent 平台"
      logo={false}
      layout="mix"
      fixSiderbar
      fixedHeader
      location={{ pathname: location.pathname }}
      siderWidth={208}
      route={{
        path: '/',
        routes: [
          {
            path: '/ui-003',
            name: 'Agent 库',
            icon: <AppstoreOutlined />
          },
          {
            path: '/ui-006',
            name: '编排',
            icon: <ApartmentOutlined />
          },
          ...(role.isSuper
            ? [
                {
                  path: '/ui-009',
                  name: '管理（Admin）',
                  icon: <SafetyCertificateOutlined />
                }
              ]
            : [])
        ]
      }}
      menuItemRender={(item, dom) => (
        <a
          onClick={() => {
            if (item.path) navigate(item.path);
          }}
        >
          {dom}
        </a>
      )}
      headerTitleRender={(_, __, props) => (
        <Space>
          <span style={{ fontWeight: 600 }}>Inkwell Agent 平台</span>
          <Tag color="default">v0.1 · H1 原型</Tag>
        </Space>
      )}
      rightContentRender={() => (
        <Space size="middle">
          <Space size={4}>
            <span style={{ fontSize: 12, color: '#888' }}>离线演示</span>
            <Switch
              size="small"
              checked={offline}
              onChange={setOffline}
              title="EX-001 离线状态徽标演示"
            />
          </Space>
          <Space size={4}>
            <span style={{ fontSize: 12, color: '#888' }}>锁定演示</span>
            <Switch
              size="small"
              checked={locked}
              onChange={setLocked}
              title="NFR-003 / EX-006 锁定页演示"
            />
          </Space>
          <Badge
            status={offline ? 'error' : 'success'}
            text={offline ? '离线' : '在线'}
          />
          <Dropdown
            menu={{
              items: [
                ...ROLE_OPTIONS.map((k) => ({
                  key: k,
                  label: ROLES[k].label,
                  onClick: () => setRoleKey(k)
                })),
                { type: 'divider' as const },
                {
                  key: 'logout',
                  label: '登出（回 UI-001）',
                  onClick: () => navigate('/ui-001')
                },
                ...(role.isSuper
                  ? [
                      {
                        key: 'admin',
                        label: '管理（UI-009）',
                        onClick: () => navigate('/ui-009')
                      }
                    ]
                  : [])
              ]
            }}
          >
            <Space style={{ cursor: 'pointer' }}>
              <UserOutlined />
              <span>{role.username}</span>
              <Tag color={role.isSuper ? 'volcano' : 'blue'}>
                {role.isSuper ? 'Admin' : 'Member'}
              </Tag>
            </Space>
          </Dropdown>
        </Space>
      )}
    >
      <Outlet />
    </ProLayout>
  );
}
