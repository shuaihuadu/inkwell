import { useCallback, useEffect, useMemo } from "react";
import { Flex, Select, Space, Typography } from "antd";
import AguiConversationShell from "../../components/agui-conversation-shell";
import SessionSidebar from "../../components/session-sidebar";
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

  // Extract agentId from route (e.g., "/api/agui/writer" -> "writer")
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

  // Refresh session list after each message completes
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
    <Flex style={{ height: "100%" }}>
      <SessionSidebar
        sessions={sessions}
        loading={sessionsLoading}
        activeSessionId={activeSessionId}
        onSelect={(id) => void handleSelectSession(id)}
        onDelete={(id) => void handleDeleteSession(id)}
        onRename={renameSession}
      />

      <Flex vertical style={{ flex: 1, height: "100%" }} gap={0}>
        <AguiConversationShell
          leftExtra={
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
          onClear={handleNewSession}
          clearDisabled={loading}
          clearText={pipelineConversationPreset.clearText}
          messages={messages}
          loading={loading}
          inputValue={inputValue}
          onInputChange={setInputValue}
          onSubmit={submit}
          placeholder={pipelineConversationPreset.placeholder}
          emptyText={pipelineConversationPreset.emptyText}
          streamingText={pipelineConversationPreset.streamingText}
          statusText={pipelineConversationPreset.getStatusText(loading)}
          shellStyle={{ flex: 1, minHeight: 0 }}
        />
      </Flex>
    </Flex>
  );
}
