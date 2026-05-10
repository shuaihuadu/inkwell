import {
  Alert,
  Button,
  Card,
  DatePicker,
  Empty,
  Form,
  Input,
  Modal,
  Select,
  Space,
  Table,
  Tabs,
  Tag,
  Typography
} from 'antd';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import StateSwitcher from '../components/StateSwitcher';
import { useStateQuery } from '../hooks/useStateQuery';
import { MOCK_AUDIT, MOCK_LOCKED_ACCOUNTS, MOCK_SHARED_ROWS } from '../mocks/index';
import { useAppContext } from '../AppContext';

/**
 * UI-009 · Admin 管理页（仅 is_super=true）
 * 三档 tab：账号 / 共享 / 审计日志（REQ-017 / NFR-004）
 * v1 不提供导出（OQ-020 closed），仅查询 + 详情弹层
 */

type State = 'data' | 'loading' | 'empty' | 'error';
const ALLOWED: readonly State[] = ['data', 'loading', 'empty', 'error'];
type TabKey = 'accounts' | 'shares' | 'audit';

export default function UI009Admin() {
  const [state, setState] = useStateQuery<State>('data', ALLOWED);
  const [tab, setTab] = useState<TabKey>('accounts');
  const [detail, setDetail] = useState<any>(null);
  const { role } = useAppContext();
  const navigate = useNavigate();

  if (!role.isSuper) {
    return (
      <div style={{ padding: 32 }}>
        <Empty description="无权限：仅 Admin（is_super=true）可见管理页" image={Empty.PRESENTED_IMAGE_SIMPLE}>
          <Button type="primary" onClick={() => navigate('/ui-003')}>
            返回 Agent 库
          </Button>
        </Empty>
      </div>
    );
  }

  return (
    <div style={{ padding: 16 }}>
      <StateSwitcher
        current={state}
        options={[
          { value: 'data', label: '有数据' },
          { value: 'loading', label: '加载中' },
          { value: 'empty', label: '空' },
          { value: 'error', label: '出错' }
        ]}
        onChange={setState}
      />

      <Card title="管理（Admin · UI-009）">
        <Tabs
          activeKey={tab}
          onChange={(k) => setTab(k as TabKey)}
          items={[
            { key: 'accounts', label: '账号' },
            { key: 'shares', label: '共享' },
            { key: 'audit', label: '审计日志' }
          ]}
        />

        {state === 'error' && (
          <Alert type="error" showIcon message="加载失败，请重试" style={{ marginBottom: 12 }} />
        )}

        {tab === 'accounts' && (
          <AccountsTab loading={state === 'loading'} empty={state === 'empty'} />
        )}
        {tab === 'shares' && (
          <SharesTab loading={state === 'loading'} empty={state === 'empty'} />
        )}
        {tab === 'audit' && (
          <AuditTab
            loading={state === 'loading'}
            empty={state === 'empty'}
            onDetail={setDetail}
          />
        )}
      </Card>

      <Modal
        title="审计条目详情"
        open={!!detail}
        onCancel={() => setDetail(null)}
        footer={null}
      >
        <pre style={{ fontSize: 12 }}>{JSON.stringify(detail, null, 2)}</pre>
      </Modal>
    </div>
  );
}

function AccountsTab({ loading, empty }: { loading: boolean; empty: boolean }) {
  return (
    <>
      <Space style={{ marginBottom: 12 }}>
        <Input.Search placeholder="按用户名搜索" style={{ width: 240 }} />
        <Select
          placeholder="状态"
          style={{ width: 120 }}
          options={[
            { value: 'locked', label: '已锁' },
            { value: 'normal', label: '正常' }
          ]}
        />
      </Space>
      <Table
        rowKey="username"
        loading={loading}
        dataSource={empty ? [] : MOCK_LOCKED_ACCOUNTS}
        locale={{ emptyText: '当前没有被锁定的账号' }}
        columns={[
          { title: '用户名', dataIndex: 'username' },
          {
            title: '状态',
            dataIndex: 'status',
            render: (s: string) => (
              <Tag color={s === '已锁' ? 'red' : 'green'}>{s}</Tag>
            )
          },
          { title: '最近活跃', dataIndex: 'lastActiveAt' },
          {
            title: '操作',
            render: (_, r: any) => (
              <Button
                size="small"
                onClick={() =>
                  Modal.confirm({
                    title: `将解封 ${r.username}，是否继续？`,
                    onOk: () => {}
                  })
                }
              >
                解封
              </Button>
            )
          }
        ]}
      />
    </>
  );
}

function SharesTab({ loading, empty }: { loading: boolean; empty: boolean }) {
  return (
    <>
      <Space style={{ marginBottom: 12 }}>
        <Input.Search placeholder="按 Agent 名 / Owner" style={{ width: 240 }} />
      </Space>
      <Table
        rowKey="agentId"
        loading={loading}
        dataSource={empty ? [] : MOCK_SHARED_ROWS}
        locale={{ emptyText: '当前没有共享中的 Agent' }}
        columns={[
          { title: 'Agent', dataIndex: 'agentName' },
          { title: 'Owner', dataIndex: 'ownerName' },
          { title: '共享开始时间', dataIndex: 'sharedAt' },
          {
            title: '操作',
            render: (_, r: any) => (
              <Button
                size="small"
                danger
                onClick={() =>
                  Modal.confirm({
                    title: `撤销 ${r.agentName} 的共享后，已使用的成员将无法继续访问。确认？`,
                    onOk: () => {}
                  })
                }
              >
                撤销共享
              </Button>
            )
          }
        ]}
      />
    </>
  );
}

function AuditTab({
  loading,
  empty,
  onDetail
}: {
  loading: boolean;
  empty: boolean;
  onDetail: (e: any) => void;
}) {
  const [eventType, setEventType] = useState<string | undefined>();
  const data = useMemo(
    () => (empty ? [] : MOCK_AUDIT.filter((a) => !eventType || a.eventType === eventType)),
    [empty, eventType]
  );

  return (
    <>
      <Form layout="inline" style={{ marginBottom: 12 }}>
        <Form.Item label="用户">
          <Input style={{ width: 140 }} />
        </Form.Item>
        <Form.Item label="Agent">
          <Input style={{ width: 160 }} />
        </Form.Item>
        <Form.Item label="时间范围">
          <DatePicker.RangePicker />
        </Form.Item>
        <Form.Item label="事件类型">
          <Select
            allowClear
            style={{ width: 200 }}
            value={eventType}
            onChange={setEventType}
            options={[
              { value: '登录', label: '登录' },
              { value: 'Agent CRUD', label: 'Agent CRUD' },
              { value: 'Agent 调用', label: 'Agent 调用' },
              { value: '版本回滚', label: '版本回滚' },
              { value: '公开 API 调用', label: '公开 API 调用' },
              { value: 'admin_unlock_account', label: 'admin_unlock_account' },
              { value: 'admin_revoke_share', label: 'admin_revoke_share' }
            ]}
          />
        </Form.Item>
      </Form>
      <Typography.Paragraph type="secondary" style={{ fontSize: 12 }}>
        v1 不提供导出（OQ-020 closed）；合规导出场景由后端运维兜底（SQL / 后台脚本）。
      </Typography.Paragraph>
      <Table
        rowKey="id"
        loading={loading}
        dataSource={data}
        locale={{ emptyText: '在所选条件内没有审计记录' }}
        columns={[
          { title: '时间', dataIndex: 'time' },
          { title: '操作者', dataIndex: 'actor' },
          { title: '事件', dataIndex: 'eventType' },
          { title: 'Agent', dataIndex: 'agentName' },
          { title: '详情', dataIndex: 'detail', ellipsis: true },
          {
            title: '操作',
            render: (_, r: any) => (
              <Button size="small" type="link" onClick={() => onDetail(r)}>
                查看
              </Button>
            )
          }
        ]}
      />
    </>
  );
}
