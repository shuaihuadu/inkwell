import { useEffect, useMemo } from "react";
import { Flex, Select } from "antd";
import AguiConversationShell from "../../components/agui-conversation-shell";
import { pipelineConversationPreset } from "../../components/agui-conversation-presets";
import { useAguiConversationController } from "../../hooks/use-agui-conversation-controller";
import { useApiList } from "../../hooks/use-api-list";

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
    setInputValue,
    submit,
    clear,
    changeRoute,
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

  return (
    <Flex vertical style={{ height: "100%" }} gap={16}>
      <AguiConversationShell
        title={pipelineConversationPreset.title}
        leftExtra={
          <Select
            value={selectedRoute}
            onChange={changeRoute}
            style={{ width: 200 }}
            options={agentOptions}
            placeholder="选择 Agent"
            loading={agentsLoading}
          />
        }
        onClear={clear}
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
  );
}
