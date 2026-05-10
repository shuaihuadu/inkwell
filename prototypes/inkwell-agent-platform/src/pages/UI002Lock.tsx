import { Alert, Button, Card, Space, Typography } from 'antd';
import { LockOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useAppContext } from '../AppContext';

/**
 * UI-002 · 锁定页（独立路由 /ui-002 演示用）
 * 真实运行中锁定遮罩由 LockOverlay 在任意页面之上渲染。
 * 本页用于 PrototypeReviewer 截屏锁定页 UI；含 §2.5 在途任务特例说明（OQ-017 closed）。
 */
export default function UI002Lock() {
  const { setLocked, role } = useAppContext();
  const navigate = useNavigate();
  return (
    <div
      style={{
        minHeight: '100vh',
        background: 'rgba(0,0,0,0.55)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: 32
      }}
    >
      <Card
        style={{ width: 480 }}
        title={
          <Space>
            <LockOutlined />
            会话已锁定（演示页 · UI-002）
          </Space>
        }
      >
        <Alert
          type="info"
          showIcon
          message="此为独立演示页；正式锁定遮罩由 AppLayout 内的 LockOverlay 全屏覆盖。"
          style={{ marginBottom: 12 }}
        />
        <Typography.Paragraph>
          客户端连续 5 分钟无操作或失焦后自动跳入本页（NFR-003 / EX-006）。
        </Typography.Paragraph>
        <Typography.Paragraph type="secondary" style={{ fontSize: 12 }}>
          <strong>OQ-017 closed 2026-05-08 在途任务特例</strong>
          ：锁定触发瞬间正在进行的录音 / 文件上传 / 流式回复保留至完成或失败，结果在锁屏背后累积；解锁后用户回到对应页面看到结果。
          锁定期间禁止新发起任何写操作。
        </Typography.Paragraph>
        <Typography.Paragraph type="secondary" style={{ fontSize: 12 }}>
          当前用户：<code>{role.username}</code>
        </Typography.Paragraph>
        <Space>
          <Button
            type="primary"
            onClick={() => {
              setLocked(false);
              navigate('/ui-003');
            }}
          >
            模拟解锁 → 回 UI-003
          </Button>
          <Button
            onClick={() => {
              setLocked(true);
              navigate('/ui-003');
            }}
          >
            触发真实锁定遮罩（在 UI-003 上）
          </Button>
          <Button
            danger
            onClick={() => {
              setLocked(false);
              navigate('/ui-001');
            }}
          >
            登出
          </Button>
        </Space>
      </Card>
    </div>
  );
}
