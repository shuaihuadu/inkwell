import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  Alert,
  Button,
  Collapse,
  Flex,
  Space,
  Spin,
  Tag,
  Tooltip,
  Typography,
  message,
} from "antd";
import {
  ApartmentOutlined,
  ArrowLeftOutlined,
  BulbOutlined,
  ThunderboltOutlined,
} from "@ant-design/icons";
import ConversationWorkspace from "../../components/conversation-workspace";
import { workflowRunConversationPreset } from "../../components/agui-conversation-presets";
import { useApiList } from "../../hooks/use-api-list";
import { useWorkflowRun } from "../../hooks/use-workflow-run";
import { API_BASE } from "../../services/api";

interface WorkflowInfo {
  id: string;
  name: string;
  description: string;
  tags?: string[];
  supportsHumanInLoop?: boolean;
}

interface WorkflowDocumentation {
  purpose?: string;
  inputHint?: string;
  inputExample?: string;
  outputHint?: string;
  tags?: string[];
}

interface WorkflowDetail extends WorkflowInfo {
  documentation?: WorkflowDocumentation | null;
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

  // 单独拉一次详情拿 documentation：列表接口出于体积考虑只返回 tags
  const [detail, setDetail] = useState<WorkflowDetail | null>(null);

  useEffect(() => {
    if (!workflowId) {
      return;
    }

    let cancelled = false;
    void (async () => {
      try {
        const res = await fetch(`${API_BASE}/api/workflows/${workflowId}`);
        if (!res.ok) {
          return;
        }
        const data = (await res.json()) as WorkflowDetail;
        if (!cancelled) {
          setDetail(data);
        }
      } catch {
        // 详情失败不阻塞运行页，最多没有使用说明
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [workflowId]);

  const documentation = detail?.documentation ?? null;

  const {
    inputValue,
    setInputValue,
    messages,
    loading,
    submit,
    respondHitl,
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
      <Flex align="center" justify="space-between" wrap="wrap" gap={8}>
        <Space wrap>
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
          {currentWorkflow?.supportsHumanInLoop && (
            <Tag color="orange">HITL</Tag>
          )}
          {(documentation?.tags ?? currentWorkflow?.tags ?? []).map((tag) => (
            <Tag key={tag} color="geekblue">
              {tag}
            </Tag>
          ))}
        </Space>
        <Typography.Text type="secondary">
          {documentation?.purpose ?? currentWorkflow?.description}
        </Typography.Text>
      </Flex>

      {/* 使用说明面板：默认展开，给用户一个 "我该输入什么 / 会得到什么" 的预期 */}
      {documentation && (
        <Collapse
          size="small"
          defaultActiveKey={["doc"]}
          items={[
            {
              key: "doc",
              label: (
                <Space>
                  <BulbOutlined />
                  <span>使用说明</span>
                </Space>
              ),
              children: (
                <Space direction="vertical" size={8} style={{ width: "100%" }}>
                  {documentation.purpose && (
                    <div>
                      <Typography.Text strong>用途：</Typography.Text>
                      <Typography.Text>{documentation.purpose}</Typography.Text>
                    </div>
                  )}
                  {documentation.inputHint && (
                    <div>
                      <Typography.Text strong>输入：</Typography.Text>
                      <Typography.Text>
                        {documentation.inputHint}
                      </Typography.Text>
                    </div>
                  )}
                  {documentation.outputHint && (
                    <div>
                      <Typography.Text strong>输出：</Typography.Text>
                      <Typography.Text>
                        {documentation.outputHint}
                      </Typography.Text>
                    </div>
                  )}
                  {documentation.inputExample && (
                    <Space align="start" wrap>
                      <Tooltip title="把示例文本填入下方输入框">
                        <Button
                          size="small"
                          type="primary"
                          ghost
                          icon={<ThunderboltOutlined />}
                          onClick={() => {
                            setInputValue(documentation.inputExample ?? "");
                            void message.success("已填入示例输入");
                          }}
                        >
                          填入示例
                        </Button>
                      </Tooltip>
                      <Typography.Text
                        type="secondary"
                        style={{ whiteSpace: "pre-wrap" }}
                      >
                        {documentation.inputExample}
                      </Typography.Text>
                    </Space>
                  )}
                </Space>
              ),
            },
          ]}
        />
      )}

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
        preset={{
          ...workflowRunConversationPreset,
          clearText: "新运行",
          // 用工作流自定义的输入提示替代通用 placeholder
          placeholder:
            documentation?.inputHint ?? workflowRunConversationPreset.placeholder,
        }}
        onHitlDecision={respondHitl}
        containerStyle={{ flex: 1, minHeight: 0 }}
      />
    </Flex>
  );
}
