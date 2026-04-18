import {
  CheckCircleOutlined,
  CloseCircleOutlined,
  LoadingOutlined,
  PlayCircleOutlined,
  RobotOutlined,
} from "@ant-design/icons";
import { ThoughtChain } from "@ant-design/x";
import { XMarkdown } from "@ant-design/x-markdown";
import { Typography } from "antd";
import type { CSSProperties, ReactNode } from "react";

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

  // 以"连续空行 + [xx]"为切分点
  const blocks = content.split(/\n\s*\n(?=\s*\[)/g).map((b) => b.trim()).filter(Boolean);
  if (blocks.length === 0) return null;

  const steps: WorkflowStep[] = [];
  for (let i = 0; i < blocks.length; i++) {
    const firstLineEnd = blocks[i].indexOf("\n");
    const headLine = firstLineEnd === -1 ? blocks[i] : blocks[i].slice(0, firstLineEnd);
    const match = headLine.match(STEP_HEAD_REGEX);
    if (!match) {
      // 首块不是 [xxx] 格式 => 整体不走步骤化渲染
      return null;
    }

    const label = match[1].trim();
    const headTail = match[2].trim();
    const rest = firstLineEnd === -1 ? "" : blocks[i].slice(firstLineEnd + 1).trim();
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

function toIcon(step: WorkflowStep, status: ReturnType<typeof toStatus>): ReactNode {
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
 * Workflow 步骤视图
 * 把 assistant 聚合文本按 [xxx] 段落渲染为 Ant Design X 的 ThoughtChain
 */
export default function WorkflowStepsView({
  content,
  streaming,
  style,
}: WorkflowStepsViewProps) {
  const steps = parseWorkflowSteps(content);
  if (!steps || steps.length === 0) {
    // 无结构化内容，直接 Markdown
    return <XMarkdown content={content} />;
  }

  const items = steps.map((step, i) => {
    const isLast = i === steps.length - 1;
    const status = toStatus(step, isLast, streaming);
    return {
      key: step.key,
      title: <Typography.Text strong>{prettyTitle(step)}</Typography.Text>,
      icon: toIcon(step, status),
      status,
      content: step.body ? <XMarkdown content={step.body} /> : undefined,
      collapsible: step.body.length > 120,
    };
  });

  return (
    <div style={style}>
      <ThoughtChain items={items} />
    </div>
  );
}
