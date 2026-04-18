import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Alert, Button, Flex, Space, Spin, Tag, Typography } from "antd";
import { ApartmentOutlined, ArrowLeftOutlined } from "@ant-design/icons";
import ConversationWorkspace from "../../components/conversation-workspace";
import { workflowRunConversationPreset } from "../../components/agui-conversation-presets";
import { useApiList } from "../../hooks/use-api-list";
import { useWorkflowRun } from "../../hooks/use-workflow-run";

interface WorkflowInfo {
  id: string;
  name: string;
  description: string;
}

/**
 * Workflow 独立运行页面
 * 外层：返回按钮 + Workflow 名称 / 描述
 * 内层：ConversationWorkspace —— 与 Agent 对话页共用的骨架（Session 侧栏 + 对话壳）
 *
 * HITL 节点由 WorkflowChatClient 自动批准，聊天流中会显示 "[系统] 已自动批准"
 * 后续 P2 可在 ConversationWorkspace 的 shellLeftExtra 位置注入批准/退回按钮
 */
export default function WorkflowRunPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const workflowId = id ?? "";

  const { items: workflows, loading: workflowsLoading } =
    useApiList<WorkflowInfo>({
      endpoint: "/api/workflows",
    });

  const currentWorkflow = useMemo(
    () => workflows.find((w) => w.id === workflowId),
    [workflows, workflowId],
  );

  const {
    inputValue,
    setInputValue,
    messages,
    loading,
    submit,
    runs,
    runsLoading,
    activeRunId,
    selectRun,
    newRun,
    deleteRun,
    renameRun,
  } = useWorkflowRun(workflowId);

  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    if (!workflowsLoading && workflows.length > 0 && !currentWorkflow) {
      setNotFound(true);
    }
  }, [workflowsLoading, workflows, currentWorkflow]);

  if (workflowsLoading) {
    return <Spin size="large" style={{ display: "block", marginTop: 100 }} />;
  }

  if (notFound) {
    return (
      <Alert
        type="error"
        message="Workflow 不存在"
        description={`未找到 ID 为 "${workflowId}" 的 Workflow`}
        action={
          <Button onClick={() => navigate("/workflows")}>返回列表</Button>
        }
      />
    );
  }

  return (
    <Flex vertical style={{ height: "100%" }} gap={12}>
      <Flex align="center" justify="space-between">
        <Space>
          <Button
            icon={<ArrowLeftOutlined />}
            onClick={() => navigate("/workflows")}
          >
            返回
          </Button>
          <Typography.Title level={4} style={{ margin: 0 }}>
            <ApartmentOutlined /> {currentWorkflow?.name ?? workflowId}
          </Typography.Title>
          <Tag color="blue">{workflowId}</Tag>
        </Space>
        <Typography.Text type="secondary">
          {currentWorkflow?.description}
        </Typography.Text>
      </Flex>

      <ConversationWorkspace
        sessions={runs}
        sessionsLoading={runsLoading}
        activeSessionId={activeRunId}
        onSelectSession={(runId) => void selectRun(runId)}
        onDeleteSession={(runId) => void deleteRun(runId)}
        onRenameSession={renameRun}
        messages={messages}
        loading={loading}
        inputValue={inputValue}
        onInputChange={setInputValue}
        onSubmit={submit}
        onNewSession={newRun}
        preset={{ ...workflowRunConversationPreset, clearText: "新运行" }}
        containerStyle={{ flex: 1, minHeight: 0 }}
      />
    </Flex>
  );
}
