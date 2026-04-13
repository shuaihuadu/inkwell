import {
  DeleteOutlined,
  DownloadOutlined,
  EditOutlined,
  MessageOutlined,
  SearchOutlined,
} from "@ant-design/icons";
import { Button, Input, Popconfirm, Space, Spin, Typography } from "antd";
import { useMemo, useState } from "react";
import type { SessionInfo } from "../hooks/use-session-list";
import { API_BASE } from "../services/api";

interface SessionSidebarProps {
  sessions: SessionInfo[];
  loading: boolean;
  activeSessionId: string | null;
  onSelect: (sessionId: string) => void;
  onDelete: (sessionId: string) => void;
  onRename: (sessionId: string, title: string) => void;
}

export default function SessionSidebar({
  sessions,
  loading,
  activeSessionId,
  onSelect,
  onDelete,
  onRename,
}: SessionSidebarProps) {
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editTitle, setEditTitle] = useState("");
  const [searchText, setSearchText] = useState("");

  const filteredSessions = useMemo(() => {
    if (!searchText.trim()) return sessions;
    return sessions.filter(
      (s) =>
        s.title?.toLowerCase().includes(searchText.toLowerCase()) ?? false,
    );
  }, [sessions, searchText]);

  const startEdit = (session: SessionInfo) => {
    setEditingId(session.id);
    setEditTitle(session.title ?? "");
  };

  const confirmEdit = () => {
    if (editingId && editTitle.trim()) {
      onRename(editingId, editTitle.trim());
    }
    setEditingId(null);
    setEditTitle("");
  };

  return (
    <div
      style={{
        width: 260,
        borderRight: "1px solid #f0f0f0",
        display: "flex",
        flexDirection: "column",
        height: "100%",
      }}
    >
      <div
        style={{
          padding: "0 16px",
          height: 56,
          borderBottom: "1px solid #f0f0f0",
          display: "flex",
          alignItems: "center",
        }}
      >
        <Typography.Text strong>历史会话</Typography.Text>
      </div>

      <div style={{ padding: "8px 12px" }}>
        <Input
          placeholder="搜索会话..."
          prefix={<SearchOutlined style={{ color: "#bbb" }} />}
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
          allowClear
          size="small"
        />
      </div>

      <div style={{ flex: 1, overflow: "auto", padding: "0 0 8px" }}>
        {loading ? (
          <div style={{ textAlign: "center", padding: 24 }}>
            <Spin size="small" />
          </div>
        ) : sessions.length === 0 ? (
          <Typography.Text
            type="secondary"
            style={{ display: "block", textAlign: "center", padding: 24 }}
          >
            暂无历史会话
          </Typography.Text>
        ) : filteredSessions.length === 0 ? (
          <Typography.Text
            type="secondary"
            style={{ display: "block", textAlign: "center", padding: 24 }}
          >
            无匹配会话
          </Typography.Text>
        ) : (
          filteredSessions.map((session) => (
            <div
              key={session.id}
              onClick={() => onSelect(session.id)}
              style={{
                padding: "8px 16px",
                cursor: "pointer",
                background:
                  activeSessionId === session.id ? "#e6f4ff" : "transparent",
                borderLeft:
                  activeSessionId === session.id
                    ? "3px solid #1677ff"
                    : "3px solid transparent",
                transition: "all 0.2s",
              }}
              onMouseEnter={(e) => {
                if (activeSessionId !== session.id) {
                  e.currentTarget.style.background = "#fafafa";
                }
              }}
              onMouseLeave={(e) => {
                if (activeSessionId !== session.id) {
                  e.currentTarget.style.background = "transparent";
                }
              }}
            >
              {editingId === session.id ? (
                <Input
                  size="small"
                  value={editTitle}
                  onChange={(e) => setEditTitle(e.target.value)}
                  onPressEnter={confirmEdit}
                  onBlur={confirmEdit}
                  autoFocus
                  onClick={(e) => e.stopPropagation()}
                />
              ) : (
                <>
                  <div
                    style={{
                      display: "flex",
                      justifyContent: "space-between",
                      alignItems: "center",
                    }}
                  >
                    <Typography.Text ellipsis style={{ flex: 1, fontSize: 13 }}>
                      <MessageOutlined
                        style={{ marginRight: 6, opacity: 0.5 }}
                      />
                      {session.title || "未命名会话"}
                    </Typography.Text>
                    <Space
                      size={4}
                      onClick={(e) => e.stopPropagation()}
                      style={{ opacity: 0.5, flexShrink: 0 }}
                    >
                      <Button
                        type="text"
                        size="small"
                        icon={<EditOutlined />}
                        onClick={() => startEdit(session)}
                        style={{ padding: "0 4px" }}
                      />
                      <Button
                        type="text"
                        size="small"
                        icon={<DownloadOutlined />}
                        onClick={() => {
                          window.open(
                            `${API_BASE}/api/sessions/${session.id}/export`,
                            "_blank",
                          );
                        }}
                        style={{ padding: "0 4px" }}
                      />
                      <Popconfirm
                        title="确定删除此会话？"
                        onConfirm={() => onDelete(session.id)}
                        okText="删除"
                        cancelText="取消"
                      >
                        <Button
                          type="text"
                          size="small"
                          danger
                          icon={<DeleteOutlined />}
                          style={{ padding: "0 4px" }}
                        />
                      </Popconfirm>
                    </Space>
                  </div>
                  <Typography.Text type="secondary" style={{ fontSize: 11 }}>
                    {session.messageCount} 条消息
                  </Typography.Text>
                </>
              )}
            </div>
          ))
        )}
      </div>
    </div>
  );
}
