import { Alert, Button, Card, Form, Input, Modal, Radio, Select, Space, Switch, Tag, Typography } from 'antd';
import { PlusOutlined, ThunderboltOutlined, HistoryOutlined } from '@ant-design/icons';
import { useState } from 'react';
import {
  Background,
  Controls,
  Edge,
  MarkerType,
  MiniMap,
  Node,
  ReactFlow
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import StateSwitcher from '../components/StateSwitcher';
import { useStateQuery } from '../hooks/useStateQuery';
import { MOCK_EDGES, MOCK_NODES } from '../mocks/index';

/**
 * UI-006 · 编排视图
 * - 可视化 DAG 画布（OQ-013 closed → React Flow / @xyflow/react）
 * - 触发器：cron / webhook，webhook Secret 一次性显示
 * - EX-007 死循环 / 超时 强制终止
 */

type State =
  | 'editing'
  | 'loading'
  | 'empty'
  | 'validation-failed'
  | 'running'
  | 'timeout-terminated'
  | 'error';
const ALLOWED: readonly State[] = [
  'editing',
  'loading',
  'empty',
  'validation-failed',
  'running',
  'timeout-terminated',
  'error'
];

const initialNodes: Node[] = MOCK_NODES.map((n, i) => ({
  id: n.id,
  position: { x: 60 + i * 220, y: 80 + (i % 2) * 60 },
  data: {
    label: (
      <Space direction="vertical" size={0} style={{ alignItems: 'flex-start' }}>
        <strong>{n.label}</strong>
        <Typography.Text style={{ fontSize: 11 }} type="secondary">
          {n.agentName} · {n.lockedVersion}
        </Typography.Text>
      </Space>
    )
  },
  style: {
    background: '#fff',
    border: '1px solid #1677ff',
    padding: 8,
    width: 200,
    borderRadius: 6
  }
}));

const initialEdges: Edge[] = MOCK_EDGES.map((e) => ({
  id: e.id,
  source: e.source,
  target: e.target,
  markerEnd: { type: MarkerType.ArrowClosed },
  animated: true
}));

export default function UI006Orchestration() {
  const [state, setState] = useStateQuery<State>('editing', ALLOWED);
  const [nodes] = useState(initialNodes);
  const [edges] = useState(initialEdges);
  const [triggerOpen, setTriggerOpen] = useState(false);
  const [webhookSecret, setWebhookSecret] = useState<string | null>(null);
  const [secretAck, setSecretAck] = useState(false);

  return (
    <div style={{ padding: 16 }}>
      <StateSwitcher
        current={state}
        options={[
          { value: 'editing', label: '编辑中' },
          { value: 'loading', label: '加载中' },
          { value: 'empty', label: '空' },
          { value: 'validation-failed', label: '校验失败（含循环）' },
          { value: 'running', label: '运行中' },
          { value: 'timeout-terminated', label: '已超限终止 EX-007' },
          { value: 'error', label: '出错' }
        ]}
        onChange={setState}
      />

      <Card
        title={
          <Space>
            <Typography.Title level={4} style={{ margin: 0 }}>
              客服质检 → 合同摘要 → 回复初稿
            </Typography.Title>
            <Tag color="green">已发布 · v3</Tag>
          </Space>
        }
        extra={
          <Space>
            <Button icon={<HistoryOutlined />}>历史</Button>
            <Button
              icon={<ThunderboltOutlined />}
              onClick={() => setTriggerOpen(true)}
            >
              触发器
            </Button>
            <Button onClick={() => setState('running')}>手动运行</Button>
            <Button type="primary" onClick={() => setState('editing')}>
              保存
            </Button>
          </Space>
        }
      >
        {state === 'validation-failed' && (
          <Alert
            banner
            type="error"
            showIcon
            message="编排存在循环：n-1 → n-2 → n-1，请修复；或节点 n-3 未绑定 Agent"
            style={{ marginBottom: 12 }}
          />
        )}
        {state === 'timeout-terminated' && (
          <Alert
            banner
            type="warning"
            showIcon
            message="已超过最大执行步数 / 时长，已强制终止（EX-007）。事件已入审计日志。"
            style={{ marginBottom: 12 }}
          />
        )}
        {state === 'error' && (
          <Alert
            banner
            type="error"
            showIcon
            message="编排执行失败：节点 n-2 模型不可用（EX-002）"
            style={{ marginBottom: 12 }}
          />
        )}

        {state === 'empty' ? (
          <div style={{ textAlign: 'center', padding: 60 }}>
            <Typography.Paragraph type="secondary">
              还没有节点
            </Typography.Paragraph>
            <Button type="primary" icon={<PlusOutlined />}>
              添加节点
            </Button>
          </div>
        ) : state === 'loading' ? (
          <div style={{ padding: 60, textAlign: 'center', color: '#888' }}>
            画布加载中…
          </div>
        ) : (
          <div className="dag-canvas">
            <ReactFlow
              nodes={nodes}
              edges={edges}
              fitView
              attributionPosition="bottom-right"
            >
              <MiniMap />
              <Controls />
              <Background gap={16} />
            </ReactFlow>
          </div>
        )}

        <Card
          size="small"
          title="选中节点：识别工单类型 · n-1"
          style={{ marginTop: 12 }}
        >
          <Form layout="vertical">
            <Space wrap size="middle">
              <Form.Item label="节点名称" style={{ marginBottom: 0 }}>
                <Input defaultValue="识别工单类型" style={{ width: 200 }} />
              </Form.Item>
              <Form.Item label="绑定 Agent" style={{ marginBottom: 0 }}>
                <Select
                  defaultValue="agent-001"
                  style={{ width: 200 }}
                  options={[
                    { value: 'agent-001', label: '客服质检助手' },
                    { value: 'agent-002', label: '合同条款摘要' }
                  ]}
                />
              </Form.Item>
              <Form.Item label="锁定版本" style={{ marginBottom: 0 }}>
                <Select
                  defaultValue="v3"
                  style={{ width: 100 }}
                  options={[
                    { value: 'v3', label: 'v3' },
                    { value: 'v2', label: 'v2' },
                    { value: 'v1', label: 'v1' },
                    { value: 'latest', label: 'latest（不推荐）' }
                  ]}
                />
              </Form.Item>
            </Space>
          </Form>
        </Card>
      </Card>

      {/* 触发器弹窗 */}
      <Modal
        title="新增触发器"
        open={triggerOpen}
        onCancel={() => setTriggerOpen(false)}
        onOk={() => {
          // 模拟 webhook 保存后一次性显示 Secret
          setWebhookSecret('whsec_demo_only_d4c2_b18a_77f0_1234567890');
          setSecretAck(false);
          setTriggerOpen(false);
        }}
        okText="保存"
        cancelText="取消"
      >
        <Form layout="vertical" initialValues={{ type: 'cron' }}>
          <Form.Item label="类型" name="type">
            <Radio.Group>
              <Radio value="cron">cron</Radio>
              <Radio value="webhook">webhook</Radio>
            </Radio.Group>
          </Form.Item>
          <Form.Item
            label="cron 表达式"
            help="示例：0 9 * * *（每天 09:00）"
          >
            <Input defaultValue="0 9 * * *" />
          </Form.Item>
          <Form.Item label="时区">
            <Select
              defaultValue="UTC+8"
              options={[
                { value: 'UTC+8', label: 'UTC+8（默认）' },
                { value: 'UTC', label: 'UTC' }
              ]}
            />
          </Form.Item>
          <Form.Item label="启用">
            <Switch />
          </Form.Item>
        </Form>
      </Modal>

      {/* webhook Secret 一次性弹层 */}
      <Modal
        title="Webhook Secret 已生成（仅显示一次）"
        open={!!webhookSecret}
        closable={false}
        keyboard={false}
        maskClosable={false}
        footer={[
          <Button
            key="ok"
            type="primary"
            disabled={!secretAck}
            onClick={() => setWebhookSecret(null)}
          >
            完成
          </Button>
        ]}
      >
        <Alert
          type="warning"
          showIcon
          message="关闭后页面立刻刷为掩码态，不可再次显示完整值。"
        />
        <Typography.Paragraph
          code
          copyable={{ text: webhookSecret ?? '' }}
          style={{ marginTop: 12 }}
        >
          {webhookSecret}
        </Typography.Paragraph>
        <Typography.Paragraph
          code
          copyable={{ text: 'https://inkwell.example.com/api/orchestrations/orch-007/trigger' }}
        >
          入站 URL: https://inkwell.example.com/api/orchestrations/orch-007/trigger
        </Typography.Paragraph>
        <Switch
          checked={secretAck}
          onChange={setSecretAck}
        />{' '}
        我已妥善保存此 Secret
      </Modal>
    </div>
  );
}
