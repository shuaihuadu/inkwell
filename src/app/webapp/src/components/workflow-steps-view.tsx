import {
  ApiOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  LoadingOutlined,
  PlayCircleOutlined,
  RobotOutlined,
} from "@ant-design/icons";
import { ThoughtChain } from "@ant-design/x";
import { XMarkdown } from "@ant-design/x-markdown";
import { Tag, Typography } from "antd";
import type { CSSProperties, ReactNode } from "react";
import { useState } from "react";
import type { ToolCallInfo } from "../hooks/use-agui-agent";
import {
  HITL_MARKER,
  TOOL_CALL_MARKER,
  TOOL_RESULT_MARKER,
  extractMarkers,
} from "../services/stream-markers";

/**
 * 把 Hook 存下来的"原始文本"（含 markers）剥成干净正文
 * Hook 出于跨 SSE delta 兼容的原因保留了 markers，这里统一在渲染前剥一次
 */
function stripAllMarkers(raw: string): string {
  return extractMarkers(raw, [
    HITL_MARKER,
    TOOL_CALL_MARKER,
    TOOL_RESULT_MARKER,
  ]).stripped;
}

/**
 * 单个步骤项
 */
interface WorkflowStep {
  key: string;
  label: string;
  body: string;
  kind: "system-start" | "system-end" | "system-error" | "executor";
}

interface WorkflowStepsViewProps {
  /** 助手气泡的完整累积内容 */
  content: string;
  /** 是否还在流式接收（影响最后一项状态） */
  streaming: boolean;
  /** 本条 assistant 气泡里发生过的工具调用 */
  toolCalls?: ToolCallInfo[];
  style?: CSSProperties;
}

// 匹配形如  [系统] / [错误] / [AnalysisAggregation] / [已发布] 等段落头
// 段落之间以"空行 + [xxx]"为分隔
const STEP_HEAD_REGEX = /^\s*\[([^\]\n]+)\]\s*(.*)$/;

/**
 * 把 Workflow 聚合输出的文本拆成若干步骤
 * 规则：
 *   - 每段以 `[xxx]` 开头的块视为一个步骤
 *   - 多块之间用空行分隔
 *   - 如果没有任何步骤标记，返回 null（调用方回退到 Markdown 渲染）
 */
export function parseWorkflowSteps(content: string): WorkflowStep[] | null {
  if (!content) return null;

  // 先把 HITL / 工具调用等 markers 从原始文本里剥掉，避免它们干扰段落切分
  const cleaned = stripAllMarkers(content);
  if (!cleaned.trim()) return null;

  // 以"连续空行 + [xx]"为切分点
  const blocks = cleaned
    .split(/\n\s*\n(?=\s*\[)/g)
    .map((b) => b.trim())
    .filter(Boolean);
  if (blocks.length === 0) return null;

  const steps: WorkflowStep[] = [];
  for (let i = 0; i < blocks.length; i++) {
    const firstLineEnd = blocks[i].indexOf("\n");
    const headLine =
      firstLineEnd === -1 ? blocks[i] : blocks[i].slice(0, firstLineEnd);
    const match = headLine.match(STEP_HEAD_REGEX);
    if (!match) {
      // 首块不是 [xxx] 格式 => 整体不走步骤化渲染
      return null;
    }

    const label = match[1].trim();
    const headTail = match[2].trim();
    const rest =
      firstLineEnd === -1 ? "" : blocks[i].slice(firstLineEnd + 1).trim();
    const body = [headTail, rest].filter(Boolean).join("\n");

    const isSystem = label === "系统";
    const isError = label === "错误";
    const kind: WorkflowStep["kind"] = isError
      ? "system-error"
      : isSystem
        ? body.includes("启动") || body.includes("开始")
          ? "system-start"
          : "system-end"
        : "executor";

    steps.push({
      key: `${i}-${label}`,
      label,
      body,
      kind,
    });
  }

  return steps.length >= 1 ? steps : null;
}

/**
 * 根据 kind 和流式状态返回 ThoughtChain item 的 status
 */
function toStatus(
  step: WorkflowStep,
  isLast: boolean,
  streaming: boolean,
): "success" | "error" | "loading" {
  if (step.kind === "system-error") return "error";
  if (isLast && streaming) return "loading";
  return "success";
}

function toIcon(
  step: WorkflowStep,
  status: ReturnType<typeof toStatus>,
): ReactNode {
  if (status === "loading") return <LoadingOutlined />;
  if (status === "error") return <CloseCircleOutlined />;
  if (step.kind === "system-start") return <PlayCircleOutlined />;
  if (step.kind === "executor") return <RobotOutlined />;
  return <CheckCircleOutlined />;
}

/**
 * 给 executor 步骤起个友好标题
 */
function prettyTitle(step: WorkflowStep): string {
  if (step.kind === "system-start") return "Workflow 已启动";
  if (step.kind === "system-end") return "Workflow 已完成";
  if (step.kind === "system-error") return "执行异常";

  // executor label -> 中文
  const mapping: Record<string, string> = {
    AnalysisAggregation: "选题分析汇总",
    MarketAnalysis: "市场趋势分析",
    CompetitorAnalysis: "竞品分析",
    Writer: "内容写作",
    LoopWriter: "内容写作",
    TopicBootstrap: "主题准备",
    Critic: "内容审核",
    ReviewGate: "审核结果处理",
    已发布: "已发布",
    TranslationAggregation: "翻译汇总",
    RankAggregator: "评分排序",
    LoopCompletion: "循环结束",
    SimpleWriter: "快速写作",
  };
  return mapping[step.label] ?? step.label;
}

/**
 * 把工具调用列表拼成一个可展开的 ThoughtChain item
 */
function buildToolCallItem(toolCalls: ToolCallInfo[]) {
  const hasRunning = toolCalls.some((tc) => tc.status === "running");
  const hasError = toolCalls.some((tc) => tc.status === "error");
  const status: "success" | "error" | "loading" = hasError
    ? "error"
    : hasRunning
      ? "loading"
      : "success";

  const body = (
    <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
      {toolCalls.map((tc) => {
        const color =
          tc.status === "error"
            ? "red"
            : tc.status === "running"
              ? "processing"
              : "green";
        const label =
          tc.status === "error"
            ? "失败"
            : tc.status === "running"
              ? "调用中"
              : "完成";
        return (
          <div
            key={tc.callId}
            style={{
              border: "1px solid #f0f0f0",
              borderRadius: 6,
              padding: 8,
              background: "#fafafa",
            }}
          >
            <div
              style={{
                display: "flex",
                gap: 8,
                alignItems: "center",
                marginBottom: 4,
              }}
            >
              <Typography.Text strong>{tc.name ?? tc.callId}</Typography.Text>
              <Tag color={color}>{label}</Tag>
              {tc.executor && (
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  {tc.executor}
                </Typography.Text>
              )}
            </div>
            {tc.arguments && (
              <div>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  参数
                </Typography.Text>
                <pre
                  style={{
                    margin: "2px 0 6px",
                    padding: 6,
                    background: "#fff",
                    border: "1px solid #f0f0f0",
                    borderRadius: 4,
                    fontSize: 12,
                    whiteSpace: "pre-wrap",
                    wordBreak: "break-all",
                    maxHeight: 160,
                    overflow: "auto",
                  }}
                >
                  {tc.arguments}
                </pre>
              </div>
            )}
            {(tc.result || tc.error) && (
              <div>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  {tc.error ? "异常" : "返回"}
                </Typography.Text>
                <pre
                  style={{
                    margin: "2px 0 0",
                    padding: 6,
                    background: "#fff",
                    border: "1px solid #f0f0f0",
                    borderRadius: 4,
                    fontSize: 12,
                    whiteSpace: "pre-wrap",
                    wordBreak: "break-all",
                    maxHeight: 160,
                    overflow: "auto",
                  }}
                >
                  {tc.error ?? tc.result}
                </pre>
              </div>
            )}
          </div>
        );
      })}
    </div>
  );

  const titleText = `工具调用（${toolCalls.length}）`;
  return {
    key: "__tool_calls__",
    title: <Typography.Text strong>{titleText}</Typography.Text>,
    icon:
      status === "loading" ? (
        <LoadingOutlined />
      ) : status === "error" ? (
        <CloseCircleOutlined />
      ) : (
        <ApiOutlined />
      ),
    status,
    content: body,
    collapsible: true,
  };
}

/**
 * 审核结果的结构化 payload（与后端 ReviewDecision 对齐）
 */
interface ReviewDecisionPayload {
  approved: boolean;
  feedback?: string;
  score?: number;
}

/**
 * 尝试把文本解析为 Critic 返回的 JSON 结构
 * 兼容：纯 JSON、被 ```json``` 包裹的代码块、尾部/首部夹杂零散字符的情况
 */
function tryParseReviewDecision(body: string): ReviewDecisionPayload | null {
  if (!body) return null;
  const trimmed = body.trim();
  // 去掉可能的 markdown 代码围栏
  const stripped = trimmed
    .replace(/^```(?:json)?\s*/i, "")
    .replace(/```$/i, "")
    .trim();
  // 提取第一个 { ... } 块
  const start = stripped.indexOf("{");
  const end = stripped.lastIndexOf("}");
  if (start < 0 || end <= start) return null;
  const jsonText = stripped.slice(start, end + 1);
  try {
    const parsed = JSON.parse(jsonText) as ReviewDecisionPayload;
    if (typeof parsed.approved !== "boolean") return null;
    return parsed;
  } catch {
    return null;
  }
}

/**
 * 审核结果卡片：比原始 JSON 更直观
 */
function ReviewDecisionCard({ payload }: { payload: ReviewDecisionPayload }) {
  const approved = payload.approved;
  const score = typeof payload.score === "number" ? payload.score : undefined;
  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        gap: 10,
        padding: 12,
        borderRadius: 8,
        border: `1px solid ${approved ? "#b7eb8f" : "#ffccc7"}`,
        background: approved ? "#f6ffed" : "#fff2f0",
      }}
    >
      <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
        <Tag color={approved ? "success" : "error"}>
          {approved ? "通过" : "需修改"}
        </Tag>
        {score !== undefined && (
          <Tag color={score >= 8 ? "green" : score >= 6 ? "orange" : "red"}>
            评分 {score}/10
          </Tag>
        )}
      </div>
      {payload.feedback && (
        <div>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            审核意见
          </Typography.Text>
          <Typography.Paragraph
            style={{ margin: "4px 0 0", whiteSpace: "pre-wrap" }}
          >
            {payload.feedback}
          </Typography.Paragraph>
        </div>
      )}
    </div>
  );
}

/**
 * GitHub Copilot 风格的"思考中"闪烁文本
 * 用浅色 → 深色渐变在横向滚动，视觉上像一条光带从文字上划过
 */
function ShimmerText({ children }: { children: ReactNode }) {
  return (
    <span
      style={{
        display: "inline-block",
        backgroundImage:
          "linear-gradient(90deg, rgba(0,0,0,0.35) 0%, rgba(0,0,0,0.35) 35%, rgba(22,119,255,1) 50%, rgba(0,0,0,0.35) 65%, rgba(0,0,0,0.35) 100%)",
        backgroundSize: "200% 100%",
        WebkitBackgroundClip: "text",
        backgroundClip: "text",
        WebkitTextFillColor: "transparent",
        color: "transparent",
        animation: "inkwell-text-shimmer 1.8s linear infinite",
        fontWeight: 500,
      }}
    >
      <style>
        {`@keyframes inkwell-text-shimmer {
          0% { background-position: 100% 0; }
          100% { background-position: -100% 0; }
        }`}
      </style>
      {children}
    </span>
  );
}

/**
 * Workflow 步骤视图
 * 把 assistant 聚合文本按 [xxx] 段落渲染为 Ant Design X 的 ThoughtChain
 */
export default function WorkflowStepsView({
  content,
  streaming,
  toolCalls,
  style,
}: WorkflowStepsViewProps) {
  const steps = parseWorkflowSteps(content);
  const hasTools = !!toolCalls && toolCalls.length > 0;

  // 记录用户"手动折叠"过的节点 key（语义与 expandedKeys 相反）。
  // 设计动机：默认所有节点都保持展开，只有当用户亲手点击收起时才记录到这里。
  // 如果用 userExpanded 存"已展开"列表，新出现的节点（streaming 过程中不断追加）
  // 就需要额外逻辑追加进去；而用"已折叠"列表则无需额外处理——未被收起的天然是展开的。
  const [userCollapsed, setUserCollapsed] = useState<string[]>([]);

  if ((!steps || steps.length === 0) && !hasTools) {
    // 无结构化内容，直接 Markdown
    return <XMarkdown content={content} />;
  }

  const stepItems = (steps ?? []).map((step, i) => {
    const isLast = i === (steps?.length ?? 0) - 1;
    const status = toStatus(step, isLast, streaming);
    const isSystemStart = step.kind === "system-start";
    const isCritic = step.label === "Critic";
    const reviewPayload = isCritic ? tryParseReviewDecision(step.body) : null;

    // 按优先级选择 body 渲染：
    //   1) Critic 且 body 可解析成 ReviewDecision → 结构化卡片
    //   2) 系统启动块：
    //        streaming 中 → Copilot 风格闪烁的"正在执行..."
    //        已完成 → 不显示 body（保留 title + 绿色 icon 即可，不让"正在执行..."这种文案残留）
    //   3) 其他 → Markdown
    let bodyNode: ReactNode;
    if (reviewPayload) {
      bodyNode = <ReviewDecisionCard payload={reviewPayload} />;
    } else if (isSystemStart) {
      bodyNode = streaming ? (
        <ShimmerText>{step.body || "Workflow 已启动，正在执行..."}</ShimmerText>
      ) : undefined;
    } else if (step.body) {
      bodyNode = <XMarkdown content={step.body} />;
    } else {
      bodyNode = undefined;
    }

    // 折叠阈值：系统启动块在完成后没有 body，显式关掉 collapsible 避免产生"空可展开项"
    const collapsible =
      isSystemStart && !streaming ? false : step.body.length > 0;

    return {
      key: step.key,
      title: <Typography.Text strong>{prettyTitle(step)}</Typography.Text>,
      icon: toIcon(step, status),
      status,
      content: bodyNode,
      collapsible,
    };
  });

  const items = hasTools
    ? [buildToolCallItem(toolCalls!), ...stepItems]
    : stepItems;

  // 计算当前应该展开的节点：
  //   - streaming 时全部展开（并忽略用户操作，避免流式输出过程中被折叠看不到增量）
  //   - 非 streaming 时默认全部展开，只有被用户明确收起的节点例外
  const allKeys = items.map((it) => it.key as string);
  const expandedKeys = streaming
    ? allKeys
    : allKeys.filter((k) => !userCollapsed.includes(k));

  // HITL 通过之后，Publisher Agent 还会继续跑一轮工具调用，
  // 此时"已发布"这一项看起来像终态，但整条 Workflow 其实还在流式输出。
  // 为了避免误导，在仍然 streaming 时顶部挂一条醒目的"执行中"横幅。
  const runningBanner = streaming ? (
    <div
      style={{
        position: "sticky",
        top: 0,
        zIndex: 2,
        marginBottom: 12,
        padding: "10px 14px",
        borderRadius: 8,
        color: "#fff",
        background:
          "linear-gradient(90deg, #1677ff 0%, #4096ff 50%, #69b1ff 100%)",
        backgroundSize: "200% 100%",
        animation: "inkwell-running-shimmer 2s linear infinite",
        display: "flex",
        alignItems: "center",
        gap: 10,
        boxShadow: "0 2px 8px rgba(22,119,255,0.25)",
        fontWeight: 500,
      }}
    >
      <style>
        {`@keyframes inkwell-running-shimmer {
          0% { background-position: 0% 0; }
          100% { background-position: -200% 0; }
        }`}
      </style>
      <LoadingOutlined style={{ fontSize: 18 }} spin />
      <span>工作流正在执行中</span>
      <Typography.Text style={{ color: "rgba(255,255,255,0.9)", fontSize: 12 }}>
        后续步骤（发布 / 归档 / 循环下一轮）仍在处理，请稍候…
      </Typography.Text>
    </div>
  ) : null;

  return (
    <div style={style}>
      {runningBanner}
      <ThoughtChain
        items={items}
        expandedKeys={expandedKeys}
        onExpand={(keys) => {
          // streaming 期间不允许折叠，忽略回调
          if (streaming) return;
          // keys 是用户操作后"当前展开"的列表；反推得到"当前折叠"的列表存起来
          const collapsed = allKeys.filter((k) => !keys.includes(k));
          setUserCollapsed(collapsed);
        }}
      />
    </div>
  );
}
