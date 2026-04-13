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
  getStatusText: (loading) => (loading ? "正在流式生成回答..." : undefined),
};

export const workflowRunConversationPreset: AguiConversationPreset = {
  title: "AGUI 会话",
  clearText: "新会话",
  placeholder: "输入内容，使用 AGUI 执行当前 Workflow...",
  emptyText: "输入内容后通过 AGUI 运行 Workflow",
  streamingText: "执行中...",
  getStatusText: (loading) => (loading ? "Workflow 正在执行中..." : undefined),
};
