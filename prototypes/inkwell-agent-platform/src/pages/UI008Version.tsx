import {
  Alert,
  Button,
  Card,
  Col,
  Descriptions,
  List,
  Modal,
  Row,
  Space,
  Tag,
  Typography
} from 'antd';
import { useState } from 'react';
import StateSwitcher from '../components/StateSwitcher';
import { useStateQuery } from '../hooks/useStateQuery';
import { MOCK_VERSIONS } from '../mocks/agents';

/**
 * UI-008 · 版本视图
 * 列表 / diff / 回滚（REQ-015 / 场景 S7）
 */

type State = 'default' | 'loading' | 'rolling-back' | 'rollback-success' | 'rollback-failed';
const ALLOWED: readonly State[] = [
  'default',
  'loading',
  'rolling-back',
  'rollback-success',
  'rollback-failed'
];

export default function UI008Version() {
  const [state, setState] = useStateQuery<State>('default', ALLOWED);
  const [selected, setSelected] = useState<string>(MOCK_VERSIONS[0].version);
  const [confirm, setConfirm] = useState(false);

  const ver = MOCK_VERSIONS.find((v) => v.version === selected) ?? MOCK_VERSIONS[0];

  return (
    <div style={{ padding: 16 }}>
      <StateSwitcher
        current={state}
        options={[
          { value: 'default', label: '默认' },
          { value: 'loading', label: '加载中' },
          { value: 'rolling-back', label: '回滚中' },
          { value: 'rollback-success', label: '回滚成功' },
          { value: 'rollback-failed', label: '回滚失败' }
        ]}
        onChange={setState}
      />

      <Card title="版本列表">
        {state === 'rollback-success' && (
          <Alert
            type="success"
            showIcon
            message={`已回滚到 ${selected}，新生成版本 v${
              parseInt(MOCK_VERSIONS[0].version.slice(1)) + 1
            }`}
            style={{ marginBottom: 12 }}
          />
        )}
        {state === 'rollback-failed' && (
          <Alert
            type="error"
            showIcon
            message="回滚失败：NET-503，未生成新版本"
            style={{ marginBottom: 12 }}
          />
        )}

        <Row gutter={16}>
          <Col span={8}>
            <List
              loading={state === 'loading'}
              dataSource={MOCK_VERSIONS}
              renderItem={(v, i) => (
                <List.Item
                  onClick={() => setSelected(v.version)}
                  style={{
                    cursor: 'pointer',
                    background: v.version === selected ? '#e6f4ff' : 'transparent',
                    padding: 12
                  }}
                >
                  <Space direction="vertical" size={2}>
                    <Space>
                      <Tag color={i === 0 ? 'blue' : 'default'}>{v.version}</Tag>
                      {i === 0 && <Tag color="green">latest</Tag>}
                    </Space>
                    <Typography.Text style={{ fontSize: 12 }} type="secondary">
                      {v.savedAt} · {v.savedBy}
                    </Typography.Text>
                    <Typography.Text style={{ fontSize: 12 }}>
                      {v.note}
                    </Typography.Text>
                  </Space>
                </List.Item>
              )}
            />
          </Col>
          <Col span={16}>
            <Card
              size="small"
              title={
                <Space>
                  <span>{ver.version} 详情</span>
                  <Button size="small">diff 上一版</Button>
                  <Button size="small">diff latest</Button>
                </Space>
              }
              extra={
                <Button
                  type="primary"
                  loading={state === 'rolling-back'}
                  onClick={() => setConfirm(true)}
                  disabled={ver.version === MOCK_VERSIONS[0].version}
                >
                  回滚到本版
                </Button>
              }
            >
              <Descriptions size="small" column={2}>
                <Descriptions.Item label="版本">{ver.version}</Descriptions.Item>
                <Descriptions.Item label="保存时间">{ver.savedAt}</Descriptions.Item>
                <Descriptions.Item label="保存人">{ver.savedBy}</Descriptions.Item>
                <Descriptions.Item label="备注">{ver.note}</Descriptions.Item>
              </Descriptions>

              <Card
                size="small"
                title="字段差异（mock）"
                style={{ marginTop: 12 }}
              >
                <pre style={{ margin: 0, fontSize: 12 }}>
{`Instructions:
- 你是一名客服质检助手……
+ 你是一名客服质检助手，请按以下规则……

模型参数:
- temperature: 0.5
+ temperature: 0.7`}
                </pre>
              </Card>
            </Card>
          </Col>
        </Row>
      </Card>

      <Modal
        title="回滚到本版"
        open={confirm}
        onOk={() => {
          setConfirm(false);
          setState('rollback-success');
        }}
        onCancel={() => setConfirm(false)}
      >
        回滚后将生成新版本 v
        {parseInt(MOCK_VERSIONS[0].version.slice(1)) + 1}
        ，引用 latest 的编排 / 公开 API 仍指向 latest。确认？
      </Modal>
    </div>
  );
}
