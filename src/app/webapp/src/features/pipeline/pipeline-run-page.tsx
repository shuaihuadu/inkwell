import { useState, useCallback } from "react";
import {
  Layout,
  Typography,
  Input,
  Button,
  Card,
  Space,
  Tag,
  Timeline,
  Result,
  Flex,
} from "antd";
import {
  SendOutlined,
  CheckOutlined,
  CloseOutlined,
  ThunderboltOutlined,
} from "@ant-design/icons";
import { usePipelineStore } from "../../stores/pipeline-store";
import type { PipelineEvent } from "./types/pipeline.types";

const { Sider, Content } = Layout;

/** 模拟事件（Phase 1 无后端 API 时的 Mock 演示） */
function createMockEvents(topic: string): PipelineEvent[] {
  const now = Date.now();
  return [
    {
      id: "1",
      type: "info",
      executor: "InputDispatch",
      content: `主题: ${topic}`,
      timestamp: now,
    },
    {
      id: "2",
      type: "executor_complete",
      executor: "MarketAnalysis",
      content: "市场趋势分析完成",
      timestamp: now + 1000,
    },
    {
      id: "3",
      type: "executor_complete",
      executor: "CompetitorAnalysis",
      content: "竞品内容分析完成",
      timestamp: now + 1200,
    },
    {
      id: "4",
      type: "analysis_complete",
      executor: "AnalysisAggregation",
      content: `[选题分析完成] 主题: ${topic}`,
      timestamp: now + 1500,
    },
    {
      id: "5",
      type: "executor_complete",
      executor: "Writer",
      content: "初稿撰写完成",
      timestamp: now + 3000,
    },
    {
      id: "6",
      type: "critic_decision",
      executor: "Critic",
      content: "审核通过 (Score: 8/10)",
      timestamp: now + 4000,
    },
  ];
}

export default function PipelineRunPage() {
  const [topic, setTopic] = useState("");
  const {
    status,
    events,
    reviewData,
    publishedContent,
    startRun,
    addEvent,
    setReviewing,
    complete,
    reset,
  } = usePipelineStore();

  /** 启动流水线（Mock 版本） */
  const handleRun = useCallback(async () => {
    if (!topic.trim()) return;
    startRun();

    const mockEvents = createMockEvents(topic);
    for (const evt of mockEvents) {
      await new Promise((r) => setTimeout(r, 800));
      addEvent(evt);
    }

    // 模拟人工审核
    await new Promise((r) => setTimeout(r, 500));
    setReviewing({
      title: topic,
      content: `这是一篇关于「${topic}」的高质量文章...\n\n(模拟内容，实际由 AI 生成)`,
      revision: 1,
      status: "Approved",
    });
  }, [topic, startRun, addEvent, setReviewing]);

  /** 处理人工审核 */
  const handleReview = useCallback(
    (approved: boolean) => {
      if (approved) {
        addEvent({
          id: String(events.length + 1),
          type: "published",
          content: `[已发布] ${reviewData?.title}`,
          timestamp: Date.now(),
        });
        complete(reviewData?.content ?? "");
      } else {
        addEvent({
          id: String(events.length + 1),
          type: "info",
          content: "人工审核退回，触发重写...",
          timestamp: Date.now(),
        });
        startRun();
      }
    },
    [events, reviewData, addEvent, complete, startRun],
  );

  return (
    <Layout style={{ height: "100%", background: "transparent" }}>
      <Sider
        width={300}
        style={{
          background: "#fafafa",
          padding: 16,
          borderRadius: 8,
          marginRight: 16,
        }}
      >
        <Typography.Title level={5}>执行时间线</Typography.Title>
        {events.length === 0 ? (
          <Typography.Text type="secondary">输入主题后点击运行</Typography.Text>
        ) : (
          <Timeline
            items={events.map((evt) => ({
              color:
                evt.type === "published"
                  ? "green"
                  : evt.type === "critic_decision"
                    ? "blue"
                    : "gray",
              children: (
                <div>
                  {evt.executor && (
                    <Tag color="processing" style={{ marginBottom: 4 }}>
                      {evt.executor}
                    </Tag>
                  )}
                  <div style={{ fontSize: 13 }}>{evt.content}</div>
                </div>
              ),
            }))}
          />
        )}
      </Sider>

      <Content>
        <Flex vertical gap={16} style={{ height: "100%" }}>
          {/* 输入区 */}
          <Card size="small">
            <Space.Compact style={{ width: "100%" }}>
              <Input
                placeholder="输入文章主题，如: The Future of AI in Healthcare"
                value={topic}
                onChange={(e) => setTopic(e.target.value)}
                onPressEnter={handleRun}
                disabled={status === "running"}
                size="large"
              />
              <Button
                type="primary"
                icon={
                  status === "running" ? (
                    <ThunderboltOutlined spin />
                  ) : (
                    <SendOutlined />
                  )
                }
                onClick={handleRun}
                loading={status === "running"}
                size="large"
              >
                {status === "running" ? "运行中..." : "运行"}
              </Button>
            </Space.Compact>
          </Card>

          {/* 人工审核卡片 */}
          {status === "reviewing" && reviewData && (
            <Card
              title={
                <Space>
                  <Typography.Text strong>人工审核</Typography.Text>
                  <Tag color="orange">第 {reviewData.revision} 稿</Tag>
                </Space>
              }
              extra={
                <Space>
                  <Button
                    type="primary"
                    icon={<CheckOutlined />}
                    onClick={() => handleReview(true)}
                  >
                    批准发布
                  </Button>
                  <Button
                    danger
                    icon={<CloseOutlined />}
                    onClick={() => handleReview(false)}
                  >
                    退回修改
                  </Button>
                </Space>
              }
            >
              <Typography.Title level={5}>{reviewData.title}</Typography.Title>
              <Typography.Paragraph
                style={{
                  maxHeight: 300,
                  overflow: "auto",
                  whiteSpace: "pre-wrap",
                  background: "#f5f5f5",
                  padding: 16,
                  borderRadius: 8,
                }}
              >
                {reviewData.content}
              </Typography.Paragraph>
            </Card>
          )}

          {/* 发布成功 */}
          {status === "completed" && publishedContent && (
            <Result
              status="success"
              title="文章已发布"
              extra={
                <Button type="primary" onClick={reset}>
                  开始新的创作
                </Button>
              }
            >
              <Card style={{ textAlign: "left" }}>
                <Typography.Paragraph style={{ whiteSpace: "pre-wrap" }}>
                  {publishedContent}
                </Typography.Paragraph>
              </Card>
            </Result>
          )}
        </Flex>
      </Content>
    </Layout>
  );
}
