import { Flex } from "antd";
import type { CSSProperties, ReactNode } from "react";
import AguiConversationShell from "./agui-conversation-shell";
import SessionSidebar from "./session-sidebar";
import type { AguiConversationPreset } from "./agui-conversation-presets";
import type { ChatMessage } from "../hooks/use-agui-agent";
import type { SessionInfo } from "../hooks/use-session-list";

interface ConversationWorkspaceProps {
  // 会话侧栏
  sessions: SessionInfo[];
  sessionsLoading: boolean;
  activeSessionId: string | null;
  onSelectSession: (id: string) => void;
  onDeleteSession: (id: string) => void;
  onRenameSession: (id: string, title: string) => void;

  // 对话
  messages: ChatMessage[];
  loading: boolean;
  inputValue: string;
  onInputChange: (value: string) => void;
  onSubmit: () => void;
  onNewSession: () => void;

  // 文案预设（placeholder / empty / streaming / statusText）
  preset: AguiConversationPreset;

  // 可选：Shell 顶栏左侧插槽（例如 Agent 下拉选择器）
  shellLeftExtra?: ReactNode;

  // 可选：人工审核决策回调（Workflow HITL 场景）
  onHitlDecision?: (
    messageId: string,
    requestId: string,
    approved: boolean,
  ) => void;

  // 样式覆写
  containerStyle?: CSSProperties;
}

/**
 * 会话工作区（Session Workspace）
 *
 * 沉淀 Agent 对话页和 Workflow 运行页的共同骨架：
 *   左栏：SessionSidebar（会话列表 / 新建 / 删除 / 重命名）
 *   右栏：AguiConversationShell（流式对话 + 输入区）
 *
 * 上层页面仅负责接入不同的 hook（useAguiConversationController + useSessionList
 * 或 useWorkflowRun），以及提供 shellLeftExtra 传入页面特有的工具栏元素
 */
export default function ConversationWorkspace({
  sessions,
  sessionsLoading,
  activeSessionId,
  onSelectSession,
  onDeleteSession,
  onRenameSession,
  messages,
  loading,
  inputValue,
  onInputChange,
  onSubmit,
  onNewSession,
  preset,
  shellLeftExtra,
  onHitlDecision,
  containerStyle,
}: ConversationWorkspaceProps) {
  return (
    <Flex style={{ height: "100%", ...containerStyle }}>
      <SessionSidebar
        sessions={sessions}
        loading={sessionsLoading}
        activeSessionId={activeSessionId}
        onSelect={onSelectSession}
        onDelete={onDeleteSession}
        onRename={onRenameSession}
      />

      <Flex vertical style={{ flex: 1, height: "100%" }} gap={0}>
        <AguiConversationShell
          leftExtra={shellLeftExtra}
          onClear={onNewSession}
          clearDisabled={loading}
          clearText={preset.clearText}
          messages={messages}
          loading={loading}
          inputValue={inputValue}
          onInputChange={onInputChange}
          onSubmit={onSubmit}
          placeholder={preset.placeholder}
          emptyText={preset.emptyText}
          streamingText={preset.streamingText}
          statusText={preset.getStatusText(loading)}
          shellStyle={{ flex: 1, minHeight: 0 }}
          onHitlDecision={onHitlDecision}
        />
      </Flex>
    </Flex>
  );
}
