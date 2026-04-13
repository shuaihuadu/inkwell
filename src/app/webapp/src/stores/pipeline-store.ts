import { create } from "zustand";
import type {
  PipelineEvent,
  PipelineStatus,
  HumanReviewData,
} from "../features/pipeline/types/pipeline.types";

interface PipelineStore {
  /** 当前运行状态 */
  status: PipelineStatus;
  /** 事件列表 */
  events: PipelineEvent[];
  /** 人工审核数据（当 status === 'reviewing' 时有值） */
  reviewData: HumanReviewData | null;
  /** 最终发布的文章内容 */
  publishedContent: string | null;

  /** 开始运行 */
  startRun: () => void;
  /** 添加事件 */
  addEvent: (event: PipelineEvent) => void;
  /** 设置审核状态 */
  setReviewing: (data: HumanReviewData) => void;
  /** 完成运行 */
  complete: (content: string) => void;
  /** 重置 */
  reset: () => void;
}

export const usePipelineStore = create<PipelineStore>((set) => ({
  status: "idle",
  events: [],
  reviewData: null,
  publishedContent: null,

  startRun: () =>
    set({
      status: "running",
      events: [],
      reviewData: null,
      publishedContent: null,
    }),

  addEvent: (event) => set((state) => ({ events: [...state.events, event] })),

  setReviewing: (data) => set({ status: "reviewing", reviewData: data }),

  complete: (content) =>
    set({ status: "completed", publishedContent: content }),

  reset: () =>
    set({
      status: "idle",
      events: [],
      reviewData: null,
      publishedContent: null,
    }),
}));
