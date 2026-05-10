import { Alert, Button, Card, Form, Input, Space, Typography } from 'antd';
import { useNavigate } from 'react-router-dom';
import StateSwitcher from '../components/StateSwitcher';
import { useStateQuery } from '../hooks/useStateQuery';

/**
 * UI-001 · 登录页
 * 状态：default / submitting / failed-401 / failed-locked / failed-rate / offline
 * 文案与 ui-spec.md §1.5 一字一致；OQ-005 closed → 无自助注册 / 重置入口（仅展示提示文字）
 */
type State =
  | 'default'
  | 'submitting'
  | 'failed-401'
  | 'failed-locked'
  | 'failed-rate'
  | 'offline';

const ALLOWED: readonly State[] = [
  'default',
  'submitting',
  'failed-401',
  'failed-locked',
  'failed-rate',
  'offline'
];

const ERROR_TEXT: Record<Exclude<State, 'default' | 'submitting'>, string> = {
  'failed-401': '账号或密码错误，请重试',
  'failed-locked': '账号已被锁定，请联系系统管理员',
  'failed-rate': '登录过于频繁，请稍后重试',
  offline: '网络异常，已断开。请检查网络连接'
};

export default function UI001Login() {
  const [state, setState] = useStateQuery<State>('default', ALLOWED);
  const navigate = useNavigate();

  const isError = state.startsWith('failed-') || state === 'offline';

  return (
    <div
      style={{
        minHeight: '100vh',
        background: '#f5f5f5',
        padding: 32
      }}
    >
      <div style={{ maxWidth: 720, margin: '0 auto' }}>
        <StateSwitcher
          current={state}
          options={[
            { value: 'default', label: '默认' },
            { value: 'submitting', label: '提交中' },
            { value: 'failed-401', label: '账号或密码错误' },
            { value: 'failed-locked', label: '账号已锁' },
            { value: 'failed-rate', label: '速率超限' },
            { value: 'offline', label: '离线' }
          ]}
          onChange={setState}
        />
      </div>

      <Card
        style={{ width: 400, margin: '40px auto', textAlign: 'center' }}
        styles={{ body: { padding: 32 } }}
      >
        <Typography.Title level={3} style={{ marginBottom: 4 }}>
          Inkwell Agent 平台
        </Typography.Title>
        <Typography.Text type="secondary" style={{ fontSize: 12 }}>
          团队 / 企业内部 LLM Agent 平台
        </Typography.Text>

        {isError && (
          <Alert
            type={state === 'offline' ? 'warning' : 'error'}
            message={ERROR_TEXT[state as keyof typeof ERROR_TEXT]}
            style={{ margin: '16px 0', textAlign: 'left' }}
            showIcon
          />
        )}

        <Form
          layout="vertical"
          style={{ textAlign: 'left', marginTop: 16 }}
          initialValues={{ username: 'owner-bob', password: '' }}
          onFinish={() => {
            // 演示：默认成功 → UI-003；其他状态保持
            if (state === 'default') navigate('/ui-003');
          }}
        >
          <Form.Item
            label="账号"
            name="username"
            rules={[
              { required: true, message: '请输入账号' },
              { max: 64, message: '账号长度不超过 64' }
            ]}
          >
            <Input
              placeholder="账号"
              autoFocus
              disabled={state === 'submitting' || state === 'offline'}
            />
          </Form.Item>
          <Form.Item
            label="密码"
            name="password"
            rules={[{ required: true, message: '请输入密码' }]}
          >
            <Input.Password
              placeholder="密码"
              disabled={state === 'submitting' || state === 'offline'}
            />
          </Form.Item>
          <Button
            type="primary"
            htmlType="submit"
            block
            loading={state === 'submitting'}
            disabled={state === 'offline'}
          >
            登录
          </Button>
          <Typography.Paragraph
            type="secondary"
            style={{ fontSize: 12, marginTop: 12, marginBottom: 0 }}
          >
            如忘记密码或需要开通账号，请联系系统管理员
          </Typography.Paragraph>
        </Form>

        <Typography.Text
          type="secondary"
          style={{ fontSize: 11, display: 'block', marginTop: 16 }}
        >
          v0.1 · build 2026-05-09
        </Typography.Text>
      </Card>

      <div style={{ textAlign: 'center', marginTop: 12 }}>
        <Space>
          <Button type="link" onClick={() => navigate('/ui-003')}>
            跳过登录直接进 UI-003（演示用）
          </Button>
        </Space>
      </div>
    </div>
  );
}
