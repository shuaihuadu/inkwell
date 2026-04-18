import { useCallback, useEffect, useMemo } from "react";
import { useAguiConversationController } from "./use-agui-conversation-controller";
import { useSessionList } from "./use-session-list";

/**
 * Workflow 运行管理 hook
 *
 * 设计理念：Workflow 的每次运行 = 一条 Session
 *   - 复用 Agent 侧已有的 /api/sessions 基础设施（通过 agentId = `workflow-{id}` 命名约定归档）
 *   - 复用 useAguiConversationController 处理 SSE 流与消息状态
 *   - 复用 useSessionList 处理历史列表 / 删除 / 重命名
 *   - 对调用方暴露 Workflow 语义的字段名（runs / activeRunId / newRun）
 *
 * 这样整个前端 UI 与后端 API 对 Agent 和 Workflow 完全一致，
 * 差别只在于 agentId 前缀 `workflow-` —— 符合 Program.cs 里的注册约定
 */
export function useWorkflowRun(workflowId: string) {
  const route = useMemo(() => `/api/agui/workflow-${workflowId}`, [workflowId]);
  const runtimeAgentId = useMemo(() => `workflow-${workflowId}`, [workflowId]);

  const conversation = useAguiConversationController(route);
  const sessions = useSessionList(runtimeAgentId);

  // 切换 Workflow 时重置路由与消息
  useEffect(() => {
    conversation.changeRoute(route);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [route]);

  // 每次消息完成后刷新历史列表，并把当前 threadId 设为活跃 run
  useEffect(() => {
    if (!conversation.loading && conversation.messages.length > 0) {
      void sessions.refresh();
      sessions.setActiveSessionId(conversation.threadId);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    conversation.loading,
    conversation.messages.length,
    conversation.threadId,
  ]);

  const selectRun = useCallback(
    async (runId: string) => {
      sessions.setActiveSessionId(runId);
      await conversation.switchSession(runId);
    },
    [sessions, conversation],
  );

  const newRun = useCallback(() => {
    conversation.clear();
    sessions.setActiveSessionId(null);
  }, [conversation, sessions]);

  const deleteRun = useCallback(
    async (runId: string) => {
      await sessions.deleteSession(runId);
      if (sessions.activeSessionId === runId) {
        conversation.clear();
      }
    },
    [sessions, conversation],
  );

  return {
    // 运行中对话
    inputValue: conversation.inputValue,
    setInputValue: conversation.setInputValue,
    messages: conversation.messages,
    loading: conversation.loading,
    submit: conversation.submit,

    // 运行历史（本质是 agentId=workflow-{id} 的 sessions）
    runs: sessions.sessions,
    runsLoading: sessions.loading,
    activeRunId: sessions.activeSessionId,
    selectRun,
    newRun,
    deleteRun,
    renameRun: sessions.renameSession,
  };
}
