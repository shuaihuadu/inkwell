/** 流水线相关类型定义 */

/** 流水线事件类型 */
export type PipelineEventType =
  | "analysis_complete"
  | "writer_complete"
  | "critic_decision"
  | "human_review_request"
  | "published"
  | "executor_complete"
  | "info";

/** 流水线事件 */
export interface PipelineEvent {
  id: string;
  type: PipelineEventType;
  executor?: string;
  content: string;
  timestamp: number;
}

/** 人工审核数据 */
export interface HumanReviewData {
  title: string;
  content: string;
  revision: number;
  status: string;
}

/** 流水线运行状态 */
export type PipelineStatus =
  | "idle"
  | "running"
  | "reviewing"
  | "completed"
  | "error";
