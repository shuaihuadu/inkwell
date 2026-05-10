import {
  Alert,
  Anchor,
  Button,
  Card,
  Checkbox,
  Col,
  Collapse,
  Form,
  Input,
  InputNumber,
  Modal,
  Radio,
  Row,
  Select,
  Space,
  Table,
  Tag,
  Typography
} from 'antd';
import { useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import StateSwitcher from '../components/StateSwitcher';
import { useStateQuery } from '../hooks/useStateQuery';
import {
  MOCK_KNOWLEDGE,
  MOCK_SKILLS,
  MOCK_TOOLS
} from '../mocks/index';
import { useAppContext } from '../AppContext';
import AvatarFallback from '../components/AvatarFallback';

/**
 * UI-004 · Agent 配置 / 详情页
 * 11 个区段（基础信息 / Instructions / 模型与参数 / 工具 / Skills / 知识库 /
 * 长期记忆 / 触发器 / 共享 / 公开 API / 版本与调试）
 * 状态：new-draft / editing / readonly / submitting / submit-failed / submit-success
 *
 * 关键决策落点：
 *  - OQ-016 closed → Token 一次性弹层勾选才能关闭（"完成"按钮 disabled）
 *  - OQ-019 closed → 头像 fallback
 *  - OQ-008 closed → 长期记忆三档（关 / 开-保留全文 / 开-摘要式）
 *  - REQ-008 / EX-008 → 上传含 scripts/ 的 Skill 前置拒收
 */

type State =
  | 'editing'
  | 'new-draft'
  | 'readonly'
  | 'submitting'
  | 'submit-failed'
  | 'submit-success';
const ALLOWED: readonly State[] = [
  'editing',
  'new-draft',
  'readonly',
  'submitting',
  'submit-failed',
  'submit-success'
];

const SECTIONS = [
  { key: 'basic', title: '基础信息' },
  { key: 'instructions', title: 'Instructions' },
  { key: 'model', title: '模型与参数' },
  { key: 'tools', title: '工具' },
  { key: 'skills', title: 'Skills' },
  { key: 'knowledge', title: '知识库' },
  { key: 'memory', title: '长期记忆' },
  { key: 'triggers', title: '触发器' },
  { key: 'share', title: '共享' },
  { key: 'api', title: '公开 API' },
  { key: 'version', title: '版本与调试' }
];

export default function UI004AgentConfig() {
  const [state, setState] = useStateQuery<State>('editing', ALLOWED);
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const { role } = useAppContext();
  const [tokenModal, setTokenModal] = useState(false);
  const [tokenAck, setTokenAck] = useState(false);
  const [confirmRotate, setConfirmRotate] = useState(false);

  const isReadonly = state === 'readonly';
  const isOwner = role.key === 'owner';
  const isNew = state === 'new-draft';
  const agentName = isNew
    ? '未命名草稿'
    : params.get('id') === 'agent-002'
    ? '合同条款摘要'
    : '客服质检助手';

  return (
    <div style={{ padding: 16 }}>
      <StateSwitcher
        current={state}
        options={[
          { value: 'editing', label: '编辑中' },
          { value: 'new-draft', label: '新建草稿' },
          { value: 'readonly', label: '只读（非 Owner）' },
          { value: 'submitting', label: '提交中' },
          { value: 'submit-failed', label: '提交失败' },
          { value: 'submit-success', label: '提交成功' }
        ]}
        onChange={setState}
      />

      <Card
        title={
          <Space>
            <AvatarFallback name={agentName} />
            <Typography.Title level={4} style={{ margin: 0 }}>
              {agentName}
            </Typography.Title>
            <Tag color={isNew ? 'orange' : 'green'}>
              {isNew ? '未保存的草稿' : 'v3 · 已发布'}
            </Tag>
            <Tag>所有者：owner-bob</Tag>
            {isReadonly && <Tag color="purple">只读（非 Owner）</Tag>}
          </Space>
        }
        extra={
          <Space>
            <Button
              type="primary"
              onClick={() => navigate(`/ui-005?id=${params.get('id') ?? 'agent-001'}`)}
            >
              开始对话
            </Button>
            {!isReadonly && (
              <Button
                loading={state === 'submitting'}
                disabled={state === 'submitting'}
                onClick={() => setState('submit-success')}
              >
                保存
              </Button>
            )}
            {isReadonly && !isOwner && (
              <Button>复制为我的 Agent</Button>
            )}
          </Space>
        }
      >
        {state === 'submit-failed' && (
          <Alert
            type="error"
            showIcon
            message="保存失败：NET-OFFLINE，已保留你的草稿（按 EX-001 / EX-002 提示）"
            style={{ marginBottom: 12 }}
          />
        )}
        {state === 'submit-success' && (
          <Alert
            type="success"
            showIcon
            message="已保存为 v4"
            action={
              <Button
                size="small"
                type="link"
                onClick={() => navigate('/ui-008')}
              >
                查看版本
              </Button>
            }
            style={{ marginBottom: 12 }}
          />
        )}

        <Row gutter={16}>
          <Col xs={24} md={5}>
            <Anchor
              affix={false}
              items={SECTIONS.map((s) => ({
                key: s.key,
                href: `#sec-${s.key}`,
                title: s.title
              }))}
            />
          </Col>
          <Col xs={24} md={19}>
            <Form layout="vertical" disabled={isReadonly || state === 'submitting'}>
              {/* 基础信息 */}
              <SectionCard id="basic" title="基础信息（REQ-003）">
                <Form.Item
                  label="名称"
                  required
                  rules={[
                    { required: true, message: '名称为必填' },
                    { max: 50, message: '名称长度不超过 50 字' }
                  ]}
                >
                  <Input
                    defaultValue={isNew ? '' : agentName}
                    placeholder="1–50 字符"
                  />
                </Form.Item>
                <Form.Item label="头像（未上传时按字符哈希分配背景色，OQ-019 closed）">
                  <Button>上传头像</Button>
                </Form.Item>
                <Form.Item label="描述（≤ 500 字）">
                  <Input.TextArea rows={3} maxLength={500} showCount />
                </Form.Item>
              </SectionCard>

              {/* Instructions */}
              <SectionCard id="instructions" title="Instructions（REQ-004）">
                <Form.Item label="System Prompt（无字数硬上限；超 32K 字符仅警告）">
                  <Input.TextArea
                    rows={6}
                    placeholder="你是一名……"
                    defaultValue="你是一名客服质检助手，请按以下规则……"
                  />
                </Form.Item>
              </SectionCard>

              {/* 模型与参数 */}
              <SectionCard id="model" title="模型与参数（REQ-005 / REQ-006）">
                <Row gutter={12}>
                  <Col span={12}>
                    <Form.Item label="模型" required>
                      <Select
                        defaultValue="azure-openai/gpt-4o"
                        options={[
                          {
                            value: 'azure-openai/gpt-4o',
                            label: 'Azure OpenAI · GPT-4o'
                          },
                          {
                            value: 'azure-openai/gpt-4o-mini',
                            label: 'Azure OpenAI · GPT-4o-mini'
                          },
                          {
                            value: 'unavailable',
                            label: 'Anthropic Claude（暂不可用）',
                            disabled: true
                          }
                        ]}
                      />
                    </Form.Item>
                  </Col>
                  <Col span={4}>
                    <Form.Item label="temperature">
                      <InputNumber min={0} max={2} step={0.1} defaultValue={0.7} />
                    </Form.Item>
                  </Col>
                  <Col span={4}>
                    <Form.Item label="top_p">
                      <InputNumber min={0} max={1} step={0.05} defaultValue={1} />
                    </Form.Item>
                  </Col>
                  <Col span={4}>
                    <Form.Item label="max_tokens">
                      <InputNumber min={1} defaultValue={2048} />
                    </Form.Item>
                  </Col>
                </Row>
              </SectionCard>

              {/* 工具 */}
              <SectionCard id="tools" title="工具（REQ-007 / Function Calling）">
                <Table
                  size="small"
                  rowKey="id"
                  pagination={false}
                  dataSource={MOCK_TOOLS}
                  columns={[
                    { title: '挂载', dataIndex: 'id', render: () => <Checkbox defaultChecked /> },
                    { title: '工具名', dataIndex: 'name' },
                    { title: '描述', dataIndex: 'description' },
                    {
                      title: '参数',
                      dataIndex: 'params',
                      render: (params: { name: string; required: boolean }[]) => (
                        <Space size={4} wrap>
                          {params.map((p) => (
                            <Tag key={p.name} color={p.required ? 'red' : 'default'}>
                              {p.name}
                              {p.required ? '*' : ''}
                            </Tag>
                          ))}
                        </Space>
                      )
                    }
                  ]}
                />
              </SectionCard>

              {/* Skills */}
              <SectionCard id="skills" title="Skills（REQ-008 · agentskills.io 格式）">
                <Alert
                  type="warning"
                  showIcon
                  style={{ marginBottom: 12 }}
                  message="v1 仅支持 SKILL.md + references/ + assets/。携带 scripts/ 的 Skill 在上传组件前置拒收。"
                />
                <Space>
                  <Button>上传 Skill（文件夹 / zip）</Button>
                  <Button
                    danger
                    onClick={() =>
                      Modal.error({
                        title: 'Skill 校验失败',
                        content:
                          'v1 不支持携带 scripts/ 的 Skill。请仅提供 SKILL.md / references/ / assets/。'
                      })
                    }
                  >
                    模拟上传含 scripts/ 的 Skill
                  </Button>
                </Space>
                <Table
                  size="small"
                  rowKey="id"
                  pagination={false}
                  dataSource={MOCK_SKILLS}
                  style={{ marginTop: 12 }}
                  columns={[
                    { title: '挂载', dataIndex: 'id', render: () => <Checkbox /> },
                    { title: 'Skill', dataIndex: 'name' },
                    { title: '版本', dataIndex: 'version' },
                    { title: '加载阶段', render: () => <Tag>Discovery / Activation / Execution</Tag> }
                  ]}
                />
              </SectionCard>

              {/* 知识库 */}
              <SectionCard id="knowledge" title="知识库 / RAG（REQ-009）">
                <Button style={{ marginBottom: 8 }}>上传文档</Button>
                <Table
                  size="small"
                  rowKey="id"
                  pagination={false}
                  dataSource={MOCK_KNOWLEDGE}
                  columns={[
                    { title: '文件名', dataIndex: 'filename' },
                    { title: '类型', dataIndex: 'type' },
                    { title: '大小 (KB)', dataIndex: 'sizeKb' },
                    { title: '上传时间', dataIndex: 'uploadedAt' },
                    {
                      title: '解析状态',
                      dataIndex: 'status',
                      render: (s: string, r: any) =>
                        s === '解析失败' ? (
                          <Tag color="red" title={r.failReason}>
                            {s}
                          </Tag>
                        ) : (
                          <Tag color={s === '解析成功' ? 'green' : 'blue'}>{s}</Tag>
                        )
                    }
                  ]}
                />
              </SectionCard>

              {/* 长期记忆 */}
              <SectionCard id="memory" title="长期记忆（REQ-010 · OQ-008 推 H3）">
                <Form.Item label="模式（具体语义由 H3 决定）">
                  <Radio.Group defaultValue="off">
                    <Radio value="off">关</Radio>
                    <Radio value="full">开（保留全文）</Radio>
                    <Radio value="summary">开（摘要式）</Radio>
                  </Radio.Group>
                </Form.Item>
              </SectionCard>

              {/* 触发器 */}
              <SectionCard id="triggers" title="触发器（REQ-011）">
                <Typography.Paragraph type="secondary" style={{ fontSize: 12 }}>
                  cron / webhook 触发器在 UI-006 编排视图统一管理。本区段仅显示当前 Agent 关联的触发器列表（mock）。
                </Typography.Paragraph>
                <Tag>cron · 每天 09:00</Tag>
                <Tag color="orange">webhook · 已创建</Tag>
              </SectionCard>

              {/* 共享 */}
              <SectionCard id="share" title="共享（REQ-002）">
                <Space>
                  <Checkbox defaultChecked>共享给团队</Checkbox>
                  <Tag color="green">已共享</Tag>
                </Space>
                {role.isSuper && (
                  <Alert
                    style={{ marginTop: 8 }}
                    type="warning"
                    showIcon
                    message="该共享已被管理员撤销（演示，依 REQ-017）"
                  />
                )}
              </SectionCard>

              {/* 公开 API */}
              <SectionCard id="api" title="公开 API（REQ-013 / OQ-004 / OQ-016）">
                <Space direction="vertical" style={{ width: '100%' }}>
                  <Space>
                    <Tag color="blue">当前 Token：tk_xxxx****a0c1</Tag>
                    <Button
                      onClick={() => setConfirmRotate(true)}
                      disabled={isReadonly}
                    >
                      生成新 Token
                    </Button>
                  </Space>
                  <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                    一个 Agent 同时仅 1 个有效 Token；新建即作废旧的（OQ-004 closed）。超过速率会被拒绝，错误码遵循 EX-005。
                  </Typography.Text>
                </Space>
              </SectionCard>

              {/* 版本与调试 */}
              <SectionCard id="version" title="版本与调试（REQ-014 / REQ-015）">
                <Space>
                  <Button onClick={() => navigate('/ui-008')}>查看版本</Button>
                  <Button onClick={() => navigate('/ui-007')}>调试 / Trace</Button>
                </Space>
              </SectionCard>
            </Form>
          </Col>
        </Row>
      </Card>

      {/* 二次确认：生成新 Token */}
      <Modal
        title="确认生成新 Token"
        open={confirmRotate}
        onOk={() => {
          setConfirmRotate(false);
          setTokenAck(false);
          setTokenModal(true);
        }}
        onCancel={() => setConfirmRotate(false)}
        okText="确认生成"
        cancelText="取消"
      >
        生成新 Token 将立即作废当前 Token，外部调用方需要重新配置。确认？
      </Modal>

      {/* 一次性显示 Token 弹层（OQ-016 closed：未勾选不能关） */}
      <Modal
        title="新 Token 已生成（仅显示一次）"
        open={tokenModal}
        closable={false}
        keyboard={false}
        maskClosable={false}
        footer={[
          <Button
            key="ok"
            type="primary"
            disabled={!tokenAck}
            onClick={() => setTokenModal(false)}
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
        <Input.TextArea
          readOnly
          rows={3}
          style={{ marginTop: 12 }}
          value="tk_live_8a4f1e92b6a37c0e_demo_only_a0c1"
        />
        <Typography.Paragraph
          code
          copyable={{ text: 'tk_live_8a4f1e92b6a37c0e_demo_only_a0c1' }}
          style={{ marginTop: 8 }}
        >
          点 [复制] 后请妥善保存
        </Typography.Paragraph>
        <Collapse
          ghost
          items={[
            {
              key: 'curl',
              label: 'curl 调用样例',
              children: (
                <Input.TextArea
                  readOnly
                  rows={4}
                  value={`curl -X POST https://inkwell.example.com/api/agents/agent-001/invoke \\\n  -H "Authorization: Bearer tk_live_..." \\\n  -H "Content-Type: application/json" \\\n  -d '{"input": "你好"}'`}
                />
              )
            }
          ]}
        />
        <Checkbox
          checked={tokenAck}
          onChange={(e) => setTokenAck(e.target.checked)}
          style={{ marginTop: 12 }}
        >
          我已妥善保存此 Token
        </Checkbox>
      </Modal>
    </div>
  );
}

function SectionCard({
  id,
  title,
  children
}: {
  id: string;
  title: string;
  children: React.ReactNode;
}) {
  return (
    <Card
      id={`sec-${id}`}
      title={title}
      size="small"
      style={{ marginBottom: 12 }}
    >
      {children}
    </Card>
  );
}
