import {
  Alert,
  Button,
  Card,
  Col,
  Descriptions,
  List,
  Row,
  Space,
  Tag,
  Tree,
  Typography
} from 'antd';
import { useState } from 'react';
import StateSwitcher from '../components/StateSwitcher';
import { useStateQuery } from '../hooks/useStateQuery';
import { MOCK_TRACES } from '../mocks/index';

/**
 * UI-007 · 调试 / Trace 视图
 *
 * 注意（OQ-018 closed 2026-05-08）：
 * 三种候选视觉形态（时序树 / 平铺列表 / 摘要 + 下钻）的最终选择"推迟到原型阶段决定"。
 * 本原型采用「时序树（嵌套缩进，可折叠）」作为候选 A 的具体实现，
 * 由 PrototypeReviewer 评审后回写到 ui-spec.md §7.1 / §7.2 / AC-052。
 */

type State = 'data' | 'loading' | 'empty' | 'replaying' | 'error';
const ALLOWED: readonly State[] = ['data', 'loading', 'empty', 'replaying', 'error'];

const TRACE_TREE = [
  {
    title: <strong>实际拼装 Prompt（system + user）</strong>,
    key: 'prompt',
    children: [
      { title: 'System：你是一名客服质检助手……', key: 'p-sys' },
      { title: 'User：帮我看看这通通话有没有违规……', key: 'p-user' }
    ]
  },
  {
    title: <strong>Skills 命中</strong>,
    key: 'skills',
    children: [
      { title: '✓ 客服质检 SOP v2.1（Discovery → Activation）', key: 's-1' },
      { title: '⚠ 加载失败（EX-008，本轮未注入）', key: 's-2' }
    ]
  },
  {
    title: <strong>知识库召回</strong>,
    key: 'rag',
    children: [
      { title: '客服话术红线.md · 段 #3', key: 'r-1' },
      { title: '合同条款汇编.pdf · 段 #11', key: 'r-2' }
    ]
  },
  {
    title: <strong>工具调用</strong>,
    key: 'tools',
    children: [
      {
        title: 'search_compliance_rule({keyword: "利率"}) → 3 条命中',
        key: 't-1'
      }
    ]
  },
  {
    title: <strong>模型响应（Azure OpenAI · GPT-4o）</strong>,
    key: 'model',
    children: [
      { title: 'tokens: 320 in / 184 out', key: 'm-1' },
      { title: 'temperature: 0.7 · top_p: 1.0', key: 'm-2' }
    ]
  }
];

export default function UI007TraceDebug() {
  const [state, setState] = useStateQuery<State>('data', ALLOWED);
  const [selected, setSelected] = useState<string>(MOCK_TRACES[0].id);

  const trace = MOCK_TRACES.find((t) => t.id === selected) ?? MOCK_TRACES[0];

  return (
    <div style={{ padding: 16 }}>
      <StateSwitcher
        current={state}
        options={[
          { value: 'data', label: '有数据' },
          { value: 'loading', label: '加载中' },
          { value: 'empty', label: '空' },
          { value: 'replaying', label: '回放中' },
          { value: 'error', label: '出错' }
        ]}
        onChange={setState}
      />

      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 12 }}
        message={
          <span>
            OQ-018 closed → 本原型采用 <strong>时序树（候选 A）</strong>{' '}
            作为 trace 视觉形态。最终决定由 PrototypeReviewer 评审后回写到
            ui-spec.md §7.1 / §7.2 / AC-052。
          </span>
        }
      />

      <Card>
        {state === 'error' ? (
          <Alert type="error" showIcon message="trace 加载失败，重试" />
        ) : state === 'empty' ? (
          <Typography.Paragraph type="secondary">
            暂无 trace，先去对话或运行编排吧
          </Typography.Paragraph>
        ) : (
          <Row gutter={16}>
            <Col span={8}>
              <Card size="small" title="trace 列表">
                <List
                  dataSource={MOCK_TRACES}
                  loading={state === 'loading'}
                  renderItem={(t) => (
                    <List.Item
                      onClick={() => setSelected(t.id)}
                      style={{
                        cursor: 'pointer',
                        background:
                          t.id === selected ? '#e6f4ff' : 'transparent',
                        padding: 8
                      }}
                    >
                      <Space direction="vertical" size={2}>
                        <Space>
                          <Tag color={t.status === '成功' ? 'green' : 'red'}>
                            {t.status}
                          </Tag>
                          <Typography.Text strong>{t.agentName}</Typography.Text>
                        </Space>
                        <Typography.Text style={{ fontSize: 12 }} type="secondary">
                          {t.startedAt} · {t.user} · 来源：{t.source}
                        </Typography.Text>
                      </Space>
                    </List.Item>
                  )}
                />
              </Card>
            </Col>

            <Col span={16}>
              <Card
                size="small"
                title={
                  <Space>
                    trace 详情
                    {state === 'replaying' && <Tag color="purple">回放中</Tag>}
                  </Space>
                }
                extra={
                  <Space>
                    <Button onClick={() => setState('replaying')}>回放</Button>
                    <Button>保存为样本</Button>
                  </Space>
                }
              >
                <Descriptions
                  size="small"
                  column={2}
                  style={{ marginBottom: 12 }}
                >
                  <Descriptions.Item label="trace ID">
                    {trace.id}
                  </Descriptions.Item>
                  <Descriptions.Item label="状态">
                    <Tag color={trace.status === '成功' ? 'green' : 'red'}>
                      {trace.status}
                    </Tag>
                  </Descriptions.Item>
                  <Descriptions.Item label="发起人">{trace.user}</Descriptions.Item>
                  <Descriptions.Item label="时间">{trace.startedAt}</Descriptions.Item>
                </Descriptions>
                <Tree
                  defaultExpandAll
                  showLine
                  treeData={TRACE_TREE}
                />
              </Card>
            </Col>
          </Row>
        )}
      </Card>
    </div>
  );
}
