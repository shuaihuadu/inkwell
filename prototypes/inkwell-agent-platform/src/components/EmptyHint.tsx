import { Button, Empty, Skeleton, Space, Typography } from 'antd';

interface SkeletonListProps {
  rows?: number;
}

/**
 * 列表加载骨架（OQ-021 closed：骨架 + 文案，不使用插画）
 */
export function SkeletonList({ rows = 4 }: SkeletonListProps) {
  return (
    <Space direction="vertical" style={{ width: '100%' }} size="middle">
      {Array.from({ length: rows }).map((_, i) => (
        <Skeleton key={i} active title paragraph={{ rows: 2 }} />
      ))}
    </Space>
  );
}

interface EmptyHintProps {
  title: string;
  description?: string;
  actionText?: string;
  onAction?: () => void;
}

/**
 * 空状态：文案 + 主动作按钮（OQ-021 closed：不使用插画）
 */
export function EmptyHint({
  title,
  description,
  actionText,
  onAction
}: EmptyHintProps) {
  return (
    <Empty
      image={Empty.PRESENTED_IMAGE_SIMPLE}
      description={
        <Space direction="vertical" align="center">
          <Typography.Text>{title}</Typography.Text>
          {description && (
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              {description}
            </Typography.Text>
          )}
        </Space>
      }
    >
      {actionText && onAction && (
        <Button type="primary" onClick={onAction}>
          {actionText}
        </Button>
      )}
    </Empty>
  );
}
