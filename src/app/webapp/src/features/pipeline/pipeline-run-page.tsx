import { useCallback, useEffect, useMemo } from "react";
import { Select, Space, Typography } from "antd";
import ConversationWorkspace from "../../components/conversation-workspace";
import { pipelineConversationPreset } from "../../components/agui-conversation-presets";
import { useAguiConversationController } from "../../hooks/use-agui-conversation-controller";
import { useApiList } from "../../hooks/use-api-list";
import { useSessionList } from "../../hooks/use-session-list";

interface AgentInfo {
  id: string;
  name: string;
  description: string;
  aguiRoute: string;
}

export default function PipelineRunPage() {
  const { items: agents, loading: agentsLoading } = useApiList<AgentInfo>({
    endpoint: "/api/agents",
  });

  const {
    route,
    inputValue,
    loading,
    messages,
    threadId,
    setInputValue,
    submit,
    clear,
    changeRoute,
    switchSession,
    respondHitl,
  } = useAguiConversationController("/api/agui/writer");

  const agentOptions = useMemo(
    () =>
      agents.map((agent) => ({
        value: agent.aguiRoute,
        label: agent.name,
      })),
    [agents],
  );

  const selectedRoute = useMemo(() => {
    if (agentOptions.some((option) => option.value === route)) {
      return route;
    }
    return agentOptions[0]?.value ?? route;
  }, [agentOptions, route]);

  useEffect(() => {
    if (selectedRoute !== route) {
      changeRoute(selectedRoute);
    }
  }, [changeRoute, route, selectedRoute]);

  const currentAgentId = useMemo(() => {
    const parts = route.split("/");
    return parts[parts.length - 1] || "writer";
  }, [route]);

  const {
    sessions,
    loading: sessionsLoading,
    activeSessionId,
    setActiveSessionId,
    refresh: refreshSessions,
    deleteSession,
    renameSession,
  } = useSessionList(currentAgentId);

  useEffect(() => {
    if (!loading && messages.length > 0) {
      void refreshSessions();
      setActiveSessionId(threadId);
    }
  }, [loading, messages.length, refreshSessions, threadId, setActiveSessionId]);

  const handleSelectSession = useCallback(
    async (sessionId: string) => {
      setActiveSessionId(sessionId);
      await switchSession(sessionId);
    },
    [setActiveSessionId, switchSession],
  );

  const handleNewSession = useCallback(() => {
    clear();
    setActiveSessionId(null);
  }, [clear, setActiveSessionId]);

  const handleDeleteSession = useCallback(
    async (sessionId: string) => {
      await deleteSession(sessionId);
      if (activeSessionId === sessionId) {
        clear();
      }
    },
    [deleteSession, activeSessionId, clear],
  );

  return (
    <ConversationWorkspace
      sessions={sessions}
      sessionsLoading={sessionsLoading}
      activeSessionId={activeSessionId}
      onSelectSession={(id) => void handleSelectSession(id)}
      onDeleteSession={(id) => void handleDeleteSession(id)}
      onRenameSession={renameSession}
      messages={messages}
      loading={loading}
      inputValue={inputValue}
      onInputChange={setInputValue}
      onSubmit={submit}
      onNewSession={handleNewSession}
      preset={pipelineConversationPreset}
      onHitlDecision={respondHitl}
      shellLeftExtra={
        <Space>
          <Typography.Text strong>选择 Agent</Typography.Text>
          <Select
            value={selectedRoute}
            onChange={changeRoute}
            style={{ width: 200 }}
            options={agentOptions}
            placeholder="选择 Agent"
            loading={agentsLoading}
          />
        </Space>
      }
    />
  );
}
