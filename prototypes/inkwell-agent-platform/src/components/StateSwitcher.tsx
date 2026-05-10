import { Segmented, Space, Tag } from 'antd';

export interface StateOption<T extends string> {
  value: T;
  label: string;
}

interface Props<T extends string> {
  current: T;
  options: StateOption<T>[];
  onChange: (s: T) => void;
}

/**
 * 顶部状态选择条：让评审者可在同一页面切换演示不同状态。
 * 同时保持 ?state=xxx 深链可分享、可截图。
 */
export default function StateSwitcher<T extends string>(props: Props<T>) {
  return (
    <Space style={{ marginBottom: 12 }} wrap>
      <Tag color="default">当前状态</Tag>
      <Segmented
        size="small"
        value={props.current}
        onChange={(v) => props.onChange(v as T)}
        options={props.options.map((o) => ({ label: o.label, value: o.value }))}
      />
      <span style={{ fontSize: 12, color: '#888' }}>
        深链：?state=&lt;value&gt;
      </span>
    </Space>
  );
}
