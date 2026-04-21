import {
  CheckCircleTwoTone,
  CloseCircleTwoTone,
  CheckOutlined,
  CloseOutlined,
  ExclamationCircleOutlined,
} from "@ant-design/icons";
import { XMarkdown } from "@ant-design/x-markdown";
import { Alert, Button, Card, Flex, Space, Tag, Typography } from "antd";
import { useMemo } from "react";
import type { ChatMessage } from "../hooks/use-agui-agent";

const { Text, Title } = Typography;

/**
 * HITL 负载（后端 Article 对象序列化后的结构，字段保持 PascalCase）
 */
interface HitlArticlePayload {
  Id?: string;
  Topic?: string;
  Title?: string;
  Content?: string;
  Revision?: number;
  Status?: number | string;
}

/**
 * 人工审核卡片 —— 当 assistant 消息携带 hitl 字段时展示
 * 视觉上以 Alert 风格强调"等待您的决策"，内容区做高度限制并内部滚动
 */
export default function HitlReviewCard({
  message,
  onDecide,
}: {
  message: ChatMessage;
  onDecide: (approved: boolean) => void;
}) {
  const hitl = message.hitl;

  const article = useMemo(() => {
    if (!hitl?.payload) return null;
    return hitl.payload as HitlArticlePayload;
  }, [hitl]);

  if (!hitl) return null;

  // 已决策状态：展示结果 Banner，代替按钮区
  if (hitl.decided) {
    const approved = hitl.approvedValue === true;
    return (
      <Card
        size="small"
        style={{
          marginTop: 8,
          borderColor: approved ? "#b7eb8f" : "#ffa39e",
          background: approved ? "#f6ffed" : "#fff1f0",
        }}
        styles={{ body: { padding: 12 } }}
      >
        <Flex align="center" gap={10}>
          {approved ? (
            <CheckCircleTwoTone
              twoToneColor="#52c41a"
              style={{ fontSize: 20 }}
            />
          ) : (
            <CloseCircleTwoTone
              twoToneColor="#ff4d4f"
              style={{ fontSize: 20 }}
            />
          )}
          <div>
            <Text strong>
              {approved
                ? "已通过，文章已发布"
                : "已退回，工作流将基于您的判断继续修订"}
            </Text>
            {article?.Title && (
              <div>
                <Text type="secondary" style={{ fontSize: 12 }}>
                  {article.Title}
                </Text>
              </div>
            )}
          </div>
        </Flex>
      </Card>
    );
  }

  const submitting = hitl.submitting === true;
  const hasContent = !!article?.Content?.trim();
  const title = article?.Title?.trim();
  const topic = article?.Topic?.trim();
  const revision = article?.Revision;

  return (
    <Card
      size="small"
      style={{
        marginTop: 8,
        borderColor: "#ffd666",
        background: "#fffdf5",
        boxShadow: "0 2px 8px rgba(250, 173, 20, 0.12)",
      }}
      styles={{ body: { padding: 0 } }}
    >
      {/* 头部：明确的"等待您决策"提示 */}
      <Alert
        type="warning"
        showIcon
        icon={<ExclamationCircleOutlined />}
        message={
          <Flex align="center" justify="space-between" gap={8}>
            <Text strong>等待您的审核决策</Text>
            <Space size={4}>
              {typeof revision === "number" && revision > 0 && (
                <Tag color="geekblue" style={{ margin: 0 }}>
                  第 {revision} 稿
                </Tag>
              )}
              {topic && (
                <Tag color="default" style={{ margin: 0, maxWidth: 160 }}>
                  <Text ellipsis style={{ fontSize: 12 }}>
                    {topic}
                  </Text>
                </Tag>
              )}
            </Space>
          </Flex>
        }
        style={{ border: "none", borderRadius: 0, background: "#fffbe6" }}
      />

      {/* 正文预览区 */}
      <div style={{ padding: "12px 16px" }}>
        {title && (
          <Title level={5} style={{ marginTop: 0, marginBottom: 8 }}>
            {title}
          </Title>
        )}

        {hasContent ? (
          <div
            style={{
              maxHeight: 320,
              overflow: "auto",
              padding: "8px 12px",
              background: "#fff",
              border: "1px solid #f0f0f0",
              borderRadius: 4,
              fontSize: 13,
              lineHeight: 1.7,
            }}
          >
            <XMarkdown content={article!.Content!} />
          </div>
        ) : (
          <Text type="secondary" style={{ fontSize: 12 }}>
            （没有可预览的正文内容）
          </Text>
        )}
      </div>

      {/* 操作区 */}
      <Flex
        justify="flex-end"
        gap={8}
        style={{
          padding: "10px 16px",
          borderTop: "1px solid #f5f5f5",
          background: "#fafafa",
        }}
      >
        <Button
          icon={<CloseOutlined />}
          danger
          disabled={submitting}
          onClick={() => onDecide(false)}
        >
          退回修订
        </Button>
        <Button
          type="primary"
          icon={<CheckOutlined />}
          loading={submitting}
          onClick={() => onDecide(true)}
        >
          通过并发布
        </Button>
      </Flex>
    </Card>
  );
}
