import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Alert, Button, Flex, Space, Spin, Tag, Typography } from "antd";
import { ApartmentOutlined, ArrowLeftOutlined } from "@ant-design/icons";
import AguiConversationShell from "../../components/agui-conversation-shell";
import SessionSidebar from "../../components/session-sidebar";
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
 *
 * 布局与 PipelineRunPage 高度对齐：
 *   左栏 SessionSidebar —— 展示当前 Workflow 的运行历史（复用 Agent 的 session 持久化）
 *   右栏 AguiConversationShell —— 执行 Workflow、展示流式输出
 *
 * 关键复用：
 *   - useWorkflowRun 封装了 agentId=`workflow-{id}` 的 session 约定
 *   - /api/agui/workflow-{id} 与 Agent 走同一条 AG-UI 管线
 *   - HITL 节点由 WorkflowChatClient 自动批准，UI 仅展示 "[系统] 已自动批准" 文本
 *     （后续 P2 可在此位置渲染批准/退回按钮，驱动后端 SendResponseAsync）
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

      <Flex style={{ flex: 1, minHeight: 0 }}>
        <SessionSidebar
          sessions={runs}
          loading={runsLoading}
          activeSessionId={activeRunId}
          onSelect={(runId) => void selectRun(runId)}
          onDelete={(runId) => void deleteRun(runId)}
          onRename={renameRun}
        />

        <Flex vertical style={{ flex: 1, height: "100%" }} gap={0}>
          <AguiConversationShell
            onClear={newRun}
            clearDisabled={loading}
            clearText="新运行"
            messages={messages}
            loading={loading}
            inputValue={inputValue}
            onInputChange={setInputValue}
            onSubmit={submit}
            placeholder={workflowRunConversationPreset.placeholder}
            emptyText={workflowRunConversationPreset.emptyText}
            streamingText={workflowRunConversationPreset.streamingText}
            statusText={workflowRunConversationPreset.getStatusText(loading)}
            shellStyle={{ flex: 1, minHeight: 0 }}
          />
        </Flex>
      </Flex>
    </Flex>
  );
}
