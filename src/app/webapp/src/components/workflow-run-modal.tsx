import { Modal, Select, Space, Tag } from "antd";
import AguiConversationShell from "./agui-conversation-shell";
import { workflowRunConversationPreset } from "./agui-conversation-presets";
import type { UseAguiConversationControllerReturn } from "../hooks/use-agui-conversation-controller";

interface WorkflowOption {
  value: string;
  label: string;
}

interface WorkflowRunModalProps {
  visible: boolean;
  title: string;
  options: WorkflowOption[];
  conversation: UseAguiConversationControllerReturn;
  onRouteChange?: (route: string) => void;
  onClose: () => void;
}

export default function WorkflowRunModal({
  visible,
  title,
  options,
  conversation,
  onRouteChange,
  onClose,
}: WorkflowRunModalProps) {
  const {
    route,
    inputValue,
    loading,
    messages,
    setInputValue,
    submit,
    clear,
    changeRoute,
  } = conversation;

  const handleRouteChange = (nextRoute: string) => {
    changeRoute(nextRoute);
    onRouteChange?.(nextRoute);
  };

  return (
    <Modal
      title={`运行 — ${title}`}
      open={visible}
      onCancel={() => {
        onClose();
        clear();
      }}
      footer={null}
      width={700}
    >
      <AguiConversationShell
        leftExtra={
          <Space>
            <Select
              value={route}
              style={{ width: 260 }}
              options={options}
              onChange={handleRouteChange}
            />
            <Tag color="blue">AGUI</Tag>
          </Space>
        }
        onClear={clear}
        clearDisabled={loading}
        clearText={workflowRunConversationPreset.clearText}
        messages={messages}
        loading={loading}
        inputValue={inputValue}
        onInputChange={setInputValue}
        onSubmit={submit}
        placeholder={workflowRunConversationPreset.placeholder}
        emptyText={workflowRunConversationPreset.emptyText}
        streamingText={workflowRunConversationPreset.streamingText}
        statusText={workflowRunConversationPreset.getStatusText(loading)}
        shellStyle={{ height: 460 }}
        messageContainerStyle={{
          background: "#fafafa",
          padding: 12,
          borderRadius: 8,
        }}
        submitType="enter"
      />
    </Modal>
  );
}
