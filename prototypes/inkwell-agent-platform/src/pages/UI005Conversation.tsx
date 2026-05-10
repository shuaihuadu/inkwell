import {
  Alert,
  Avatar,
  Button,
  Card,
  Drawer,
  Input,
  List,
  Space,
  Tag,
  Tooltip,
  Typography,
  Upload
} from 'antd';
import {
  AudioOutlined,
  FileTextOutlined,
  PaperClipOutlined,
  PictureOutlined,
  PlusOutlined,
  SendOutlined,
  StopOutlined
} from '@ant-design/icons';
import { useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import StateSwitcher from '../components/StateSwitcher';
import { useStateQuery } from '../hooks/useStateQuery';
import { MOCK_CONVERSATIONS, MOCK_MESSAGES } from '../mocks/conversations';
import AvatarFallback from '../components/AvatarFallback';
import { SkeletonList } from '../components/EmptyHint';
import { useAppContext } from '../AppContext';

/**
 * UI-005 · Agent 对话页
 * - 左侧历史会话侧栏（OQ-014 closed）：可折叠，"+ 新建会话"在顶部
 * - 多模态输入：文本 / 图片 / 语音 / 文档
 * - 锁定中的在途任务特例（OQ-017 closed）由 §2.5 / §5.5 落账，本页输入区禁用即代表"锁定后不可新发起"
 */

type State =
  | 'data'
  | 'loading-history'
  | 'empty-new'
  | 'streaming'
  | 'tool-calling'
  | 'recording'
  | 'transcribing'
  | 'file-parsing'
  | 'error-net'
  | 'error-model'
  | 'image-incompat';

const ALLOWED: readonly State[] = [
  'data',
  'loading-history',
  'empty-new',
  'streaming',
  'tool-calling',
  'recording',
  'transcribing',
  'file-parsing',
  'error-net',
  'error-model',
  'image-incompat'
];

export default function UI005Conversation() {
  const [state, setState] = useStateQuery<State>('data', ALLOWED);
  const [params] = useSearchParams();
  const [siderOpen, setSiderOpen] = useState(true);
  const [text, setText] = useState('');
  const navigate = useNavigate();
  const { role, locked } = useAppContext();
  const agentId = params.get('id') ?? 'agent-001';
  const agentName = '客服质检助手';

  const showHistory = state !== 'loading-history';
  const isError = state === 'error-net' || state === 'error-model';
  const writeDisabled = locked || isError;

  return (
    <div style={{ padding: 16 }}>
      <StateSwitcher
        current={state}
        options={[
          { value: 'data', label: '有数据' },
          { value: 'loading-history', label: '加载历史中' },
          { value: 'empty-new', label: '空（新会话）' },
          { value: 'streaming', label: '流式回复中' },
          { value: 'tool-calling', label: '工具调用中' },
          { value: 'recording', label: '录音中' },
          { value: 'transcribing', label: '转写中' },
          { value: 'file-parsing', label: '文件解析中' },
          { value: 'error-net', label: '网络异常 EX-001' },
          { value: 'error-model', label: '模型故障 EX-002' },
          { value: 'image-incompat', label: '图片不兼容 EX-004' }
        ]}
        onChange={setState}
      />

      <Card
        styles={{ body: { padding: 0 } }}
        title={
          <Space>
            <AvatarFallback name={agentName} />
            <Typography.Title level={5} style={{ margin: 0 }}>
              {agentName}
            </Typography.Title>
            <Tag color="blue">Azure OpenAI · GPT-4o</Tag>
            <Button size="small" onClick={() => setSiderOpen(!siderOpen)}>
              {siderOpen ? '折叠侧栏' : '展开侧栏'}
            </Button>
          </Space>
        }
        extra={
          <Space>
            {role.key === 'owner' && (
              <Button onClick={() => navigate('/ui-007')}>调试</Button>
            )}
            <Button onClick={() => navigate(`/ui-004?id=${agentId}`)}>返回</Button>
          </Space>
        }
      >
        {isError && (
          <Alert
            banner
            type="error"
            message={
              state === 'error-net'
                ? '网络异常，已断开。请检查网络连接（EX-001）'
                : '模型不可用：MODEL-503，请稍后重试（EX-002）'
            }
          />
        )}

        <div style={{ display: 'flex', height: 560 }}>
          {/* 历史会话侧栏（OQ-014 closed） */}
          {siderOpen && (
            <div
              style={{
                width: 240,
                borderRight: '1px solid #f0f0f0',
                display: 'flex',
                flexDirection: 'column'
              }}
            >
              <div style={{ padding: 12, borderBottom: '1px solid #f0f0f0' }}>
                <Button
                  type="primary"
                  block
                  icon={<PlusOutlined />}
                  onClick={() => setState('empty-new')}
                >
                  新建会话
                </Button>
              </div>
              <List
                size="small"
                dataSource={MOCK_CONVERSATIONS}
                style={{ flex: 1, overflowY: 'auto' }}
                renderItem={(c) => (
                  <List.Item style={{ cursor: 'pointer', padding: 12 }}>
                    <Space direction="vertical" size={2} style={{ width: '100%' }}>
                      <Typography.Text ellipsis style={{ fontSize: 13 }}>
                        {c.title}
                      </Typography.Text>
                      <Typography.Text
                        type="secondary"
                        style={{ fontSize: 11 }}
                      >
                        {c.startedAt}
                      </Typography.Text>
                    </Space>
                  </List.Item>
                )}
              />
            </div>
          )}

          {/* 主消息流 */}
          <div style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
            {state === 'loading-history' ? (
              <div style={{ padding: 16 }}>
                <SkeletonList rows={3} />
              </div>
            ) : state === 'empty-new' ? (
              <div
                style={{
                  flex: 1,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  color: '#888'
                }}
              >
                开始与 {agentName} 的对话。你可以输入文字、上传图片 /
                文档，或点麦克风进行语音输入。
              </div>
            ) : (
              showHistory && (
                <div className="conv-list">
                  {MOCK_MESSAGES.map((m) => (
                    <div
                      key={m.id}
                      className={`msg-bubble msg-${m.role}`}
                      style={{
                        alignSelf:
                          m.role === 'user'
                            ? 'flex-end'
                            : m.role === 'system'
                            ? 'center'
                            : 'flex-start'
                      }}
                    >
                      {m.role === 'tool' && '🛠 '}
                      {m.role === 'system' && '⚠ '}
                      {m.content}
                      {m.status === '流式中' && state !== 'streaming' && (
                        <Tag style={{ marginLeft: 8 }}>已完成</Tag>
                      )}
                    </div>
                  ))}
                  {state === 'streaming' && (
                    <div className="msg-bubble msg-agent">
                      <Space>
                        <span>正在生成回复…</span>
                        <Button
                          size="small"
                          icon={<StopOutlined />}
                          onClick={() => setState('data')}
                        >
                          停止生成
                        </Button>
                      </Space>
                    </div>
                  )}
                  {state === 'tool-calling' && (
                    <div className="msg-bubble msg-tool">
                      🛠 调用 search_compliance_rule，参数 {'{keyword: "利率"}'} → 等待返回…
                    </div>
                  )}
                </div>
              )
            )}

            {/* 输入区 */}
            <div
              style={{
                borderTop: '1px solid #f0f0f0',
                padding: 12,
                background: '#fff'
              }}
            >
              {state === 'image-incompat' && (
                <Alert
                  type="warning"
                  showIcon
                  message="当前模型不支持图像输入（EX-004）"
                  style={{ marginBottom: 8 }}
                />
              )}
              {state === 'recording' && (
                <Alert
                  type="info"
                  showIcon
                  message="录音中 · 00:12 / 单段最长 60s"
                  action={
                    <Space>
                      <Button size="small" onClick={() => setState('data')}>
                        取消
                      </Button>
                      <Button
                        size="small"
                        type="primary"
                        onClick={() => setState('transcribing')}
                      >
                        完成
                      </Button>
                    </Space>
                  }
                  style={{ marginBottom: 8 }}
                />
              )}
              {state === 'transcribing' && (
                <Alert
                  type="info"
                  showIcon
                  message="转写中…（Azure Speech）转写完成后回填到输入框"
                  style={{ marginBottom: 8 }}
                />
              )}
              {state === 'file-parsing' && (
                <Alert
                  type="info"
                  showIcon
                  message="文件解析中：合同条款汇编.pdf"
                  style={{ marginBottom: 8 }}
                />
              )}

              <Space.Compact style={{ width: '100%' }}>
                <Tooltip title="上传图片">
                  <Upload showUploadList={false} beforeUpload={() => false}>
                    <Button icon={<PictureOutlined />} disabled={writeDisabled} />
                  </Upload>
                </Tooltip>
                <Tooltip title="上传文档">
                  <Upload showUploadList={false} beforeUpload={() => false}>
                    <Button
                      icon={<PaperClipOutlined />}
                      disabled={writeDisabled}
                    />
                  </Upload>
                </Tooltip>
                <Tooltip title="语音输入（点击进入录音态）">
                  <Button
                    icon={<AudioOutlined />}
                    onClick={() => setState('recording')}
                    disabled={writeDisabled}
                  />
                </Tooltip>
                <Input.TextArea
                  value={text}
                  onChange={(e) => setText(e.target.value)}
                  placeholder={
                    locked
                      ? '会话已锁定，请先解锁'
                      : 'Enter 发送（Shift+Enter 换行；macOS Cmd ↔ Windows Ctrl 等价，OQ-022 closed）'
                  }
                  autoSize={{ minRows: 1, maxRows: 4 }}
                  disabled={writeDisabled}
                />
                <Button
                  type="primary"
                  icon={<SendOutlined />}
                  onClick={() => {
                    setText('');
                    setState('streaming');
                  }}
                  disabled={writeDisabled || !text}
                >
                  发送
                </Button>
              </Space.Compact>
            </div>
          </div>
        </div>
      </Card>
    </div>
  );
}
