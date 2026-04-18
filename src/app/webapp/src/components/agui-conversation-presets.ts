export interface AguiConversationPreset {
  title: string;
  clearText: string;
  placeholder: string;
  emptyText: string;
  streamingText: string;
  getStatusText: (loading: boolean) => string | undefined;
}

export const pipelineConversationPreset: AguiConversationPreset = {
  title: "对话",
  clearText: "新会话",
  placeholder: "输入文章主题开始创作...",
  emptyText: "输入文章主题开始创作，例如：AI 在医疗健康领域的未来",
  streamingText: "思考中...",
  // 流式状态已通过 Sender 的 loading 图标 + 气泡 typing 效果体现，不再额外显示文案
  getStatusText: () => undefined,
};

export const workflowRunConversationPreset: AguiConversationPreset = {
  title: "Workflow 运行",
  clearText: "新会话",
  placeholder: "输入内容，运行当前 Workflow...",
  emptyText: "输入内容，运行当前 Workflow",
  streamingText: "执行中...",
  getStatusText: () => undefined,
};
