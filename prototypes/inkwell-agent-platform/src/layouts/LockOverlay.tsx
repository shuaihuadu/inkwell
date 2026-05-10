import { Button, Card, Form, Input, Space, Typography } from 'antd';
import { LockOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useAppContext } from '../AppContext';

/**
 * 锁定遮罩：当 AppContext.locked 为 true 时全屏显示，遮挡上一页内容（NFR-003 / EX-006 / UI-002）
 * 同时提示 §2.5 在途任务特例（OQ-017 closed 2026-05-08）。
 */
export default function LockOverlay() {
  const { locked, setLocked, role } = useAppContext();
  const navigate = useNavigate();
  if (!locked) return null;

  return (
    <div className="lock-overlay">
      <Card
        style={{ width: 360 }}
        title={
          <Space>
            <LockOutlined />
            会话已锁定
          </Space>
        }
      >
        <Typography.Paragraph type="secondary" style={{ fontSize: 12 }}>
          已检测到 5 分钟无操作，按 NFR-003 自动锁定。锁定触发前发起的录音 /
          上传 / 流式回复保留至完成（OQ-017 closed）。
        </Typography.Paragraph>
        <Form
          layout="vertical"
          onFinish={() => setLocked(false)}
          autoComplete="off"
        >
          <Form.Item label={`当前用户：${role.username}`}>
            <Input.Password placeholder="请输入密码解锁" />
          </Form.Item>
          <Form.Item>
            <Space>
              <Button type="primary" htmlType="submit">
                解锁
              </Button>
              <Button
                onClick={() => {
                  setLocked(false);
                  navigate('/ui-001');
                }}
              >
                登出
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
}
