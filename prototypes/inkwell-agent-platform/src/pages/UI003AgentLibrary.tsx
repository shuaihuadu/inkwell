import { Button, Card, Col, Input, Row, Space, Tabs, Tag, Typography } from 'antd';
import { PlusOutlined, ReloadOutlined, SearchOutlined } from '@ant-design/icons';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { MOCK_AGENTS } from '../mocks/agents';
import StateSwitcher from '../components/StateSwitcher';
import { SkeletonList, EmptyHint } from '../components/EmptyHint';
import AvatarFallback from '../components/AvatarFallback';
import { useStateQuery } from '../hooks/useStateQuery';

/**
 * UI-003 · Agent 库
 * - 三档 tab（OQ-010 closed）：我的 / 团队共享 / 我使用过
 * - 卡片网格 3–4 列响应式（OQ-012 closed）
 * - 头像未上传 → AvatarFallback 首字母 + 哈希色（OQ-019 closed）
 * - 加载 = 骨架；空 = 文案 + 主动作（OQ-021 closed）
 */
type State = 'data' | 'loading' | 'empty-mine' | 'empty-shared' | 'empty-used' | 'error';
const ALLOWED: readonly State[] = [
  'data',
  'loading',
  'empty-mine',
  'empty-shared',
  'empty-used',
  'error'
];

type Tab = 'mine' | 'shared' | 'used';

export default function UI003AgentLibrary() {
  const [state, setState] = useStateQuery<State>('data', ALLOWED);
  const [tab, setTab] = useState<Tab>('mine');
  const [keyword, setKeyword] = useState('');
  const navigate = useNavigate();

  const list = useMemo(() => {
    return MOCK_AGENTS.filter((a) =>
      tab === 'mine'
        ? a.visibility.includes('mine')
        : tab === 'shared'
        ? a.visibility.includes('shared')
        : a.visibility.includes('used')
    ).filter((a) => (keyword ? a.name.includes(keyword) : true));
  }, [tab, keyword]);

  const isEmpty =
    (state === 'empty-mine' && tab === 'mine') ||
    (state === 'empty-shared' && tab === 'shared') ||
    (state === 'empty-used' && tab === 'used');

  return (
    <div style={{ padding: 16 }}>
      <StateSwitcher
        current={state}
        options={[
          { value: 'data', label: '有数据' },
          { value: 'loading', label: '加载中' },
          { value: 'empty-mine', label: '空（我的）' },
          { value: 'empty-shared', label: '空（团队共享）' },
          { value: 'empty-used', label: '空（我使用过）' },
          { value: 'error', label: '出错' }
        ]}
        onChange={setState}
      />

      <Card
        title={
          <Space>
            <Typography.Title level={4} style={{ margin: 0 }}>
              Agent 库
            </Typography.Title>
            <Tag color="blue">{list.length} 项</Tag>
          </Space>
        }
        extra={
          <Space>
            <Input
              placeholder="按名称搜索"
              prefix={<SearchOutlined />}
              allowClear
              value={keyword}
              onChange={(e) => setKeyword(e.target.value)}
              style={{ width: 200 }}
            />
            <Button icon={<ReloadOutlined />}>刷新</Button>
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => navigate('/ui-004?state=new-draft')}
            >
              新建 Agent
            </Button>
          </Space>
        }
      >
        <Tabs
          activeKey={tab}
          onChange={(k) => setTab(k as Tab)}
          items={[
            { key: 'mine', label: '我的' },
            { key: 'shared', label: '团队共享' },
            { key: 'used', label: '我使用过' }
          ]}
        />

        {state === 'loading' && <SkeletonList rows={3} />}
        {state === 'error' && (
          <EmptyHint
            title="加载失败，请检查网络后重试"
            actionText="重试"
            onAction={() => setState('data')}
          />
        )}
        {isEmpty && (
          <EmptyHint
            title={
              tab === 'mine'
                ? '还没有自己的 Agent，点击"新建 Agent"开始'
                : tab === 'shared'
                ? '团队成员还没有共享 Agent'
                : '还没有使用过任何 Agent，先去「团队共享」看看吧'
            }
            actionText={tab === 'mine' ? '新建 Agent' : undefined}
            onAction={tab === 'mine' ? () => navigate('/ui-004?state=new-draft') : undefined}
          />
        )}
        {state === 'data' && !isEmpty && (
          <Row gutter={[16, 16]}>
            {list.map((a) => (
              <Col key={a.id} xs={24} sm={12} md={8} lg={8} xl={6}>
                <Card
                  hoverable
                  onClick={() => navigate(`/ui-004?id=${a.id}`)}
                  styles={{ body: { padding: 16 } }}
                >
                  <Space align="start" size="middle">
                    <AvatarFallback name={a.name} size={40} />
                    <div style={{ minWidth: 0, flex: 1 }}>
                      <Space>
                        <Typography.Text strong ellipsis>
                          {a.name}
                        </Typography.Text>
                        {a.shared && <Tag color="green">已共享</Tag>}
                      </Space>
                      <Typography.Paragraph
                        type="secondary"
                        style={{
                          fontSize: 12,
                          marginTop: 4,
                          marginBottom: 4
                        }}
                        ellipsis={{ rows: 2 }}
                      >
                        {a.description.slice(0, 60)}
                      </Typography.Paragraph>
                      <Space size={4} wrap>
                        <Tag>所有者：{a.ownerName}</Tag>
                        <Tag color="default">{a.lastUsedAt}</Tag>
                      </Space>
                    </div>
                  </Space>
                  <Space style={{ marginTop: 12 }}>
                    <Button
                      size="small"
                      type="link"
                      onClick={(e) => {
                        e.stopPropagation();
                        navigate(`/ui-005?id=${a.id}`);
                      }}
                    >
                      开始对话
                    </Button>
                    {tab === 'mine' && (
                      <>
                        <Button
                          size="small"
                          type="link"
                          onClick={(e) => {
                            e.stopPropagation();
                            navigate(`/ui-004?id=${a.id}`);
                          }}
                        >
                          编辑
                        </Button>
                        <Button
                          size="small"
                          type="link"
                          danger
                          onClick={(e) => e.stopPropagation()}
                        >
                          删除
                        </Button>
                      </>
                    )}
                  </Space>
                </Card>
              </Col>
            ))}
          </Row>
        )}
      </Card>
    </div>
  );
}
