import { useEffect, useState } from "react";
import {
  Typography,
  Table,
  Button,
  Modal,
  Space,
  Spin,
  message,
  Popconfirm,
  Tag,
  Input,
  Empty,
} from "antd";
import {
  FileTextOutlined,
  DeleteOutlined,
  EyeOutlined,
  EditOutlined,
} from "@ant-design/icons";
import dayjs from "dayjs";
import { API_BASE } from "../../services/api";
import { XMarkdown } from "@ant-design/x-markdown";

interface ArticleRecord {
  id: string;
  topic: string;
  title: string;
  content: string;
  status: string;
  revision: number;
  createdAt: string;
  updatedAt: string;
}

const statusColorMap: Record<string, string> = {
  Draft: "default",
  InReview: "processing",
  Approved: "success",
  Rejected: "error",
  Published: "green",
};

const statusLabelMap: Record<string, string> = {
  Draft: "草稿",
  InReview: "审核中",
  Approved: "已通过",
  Rejected: "已退回",
  Published: "已发布",
};

export default function ArticlesPage() {
  const [articles, setArticles] = useState<ArticleRecord[]>([]);
  const [loading, setLoading] = useState(false);
  const [viewArticle, setViewArticle] = useState<ArticleRecord | null>(null);
  const [editArticle, setEditArticle] = useState<ArticleRecord | null>(null);
  const [editTitle, setEditTitle] = useState("");
  const [editContent, setEditContent] = useState("");
  const [saving, setSaving] = useState(false);

  const fetchArticles = async () => {
    setLoading(true);
    try {
      const res = await fetch(`${API_BASE}/api/articles`);
      if (res.ok) {
        const data: ArticleRecord[] = await res.json();
        setArticles(
          data.sort(
            (a, b) =>
              dayjs(b.updatedAt).valueOf() - dayjs(a.updatedAt).valueOf(),
          ),
        );
      }
    } catch {
      message.error("获取文章列表失败");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchArticles();
  }, []);

  const handleDelete = async (id: string) => {
    try {
      await fetch(`${API_BASE}/api/articles/${id}`, { method: "DELETE" });
      message.success("文章已删除");
      fetchArticles();
    } catch {
      message.error("删除失败");
    }
  };

  const handleUpdateStatus = async (id: string, status: string) => {
    try {
      const res = await fetch(`${API_BASE}/api/articles/${id}/status`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ status }),
      });
      if (res.ok) {
        message.success("状态已更新");
        fetchArticles();
      }
    } catch {
      message.error("状态更新失败");
    }
  };

  const handleSaveEdit = async () => {
    if (!editArticle) return;
    setSaving(true);
    try {
      const res = await fetch(`${API_BASE}/api/articles/${editArticle.id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ title: editTitle, content: editContent }),
      });
      if (res.ok) {
        message.success("文章已更新");
        setEditArticle(null);
        fetchArticles();
      }
    } catch {
      message.error("更新失败");
    } finally {
      setSaving(false);
    }
  };

  const columns = [
    {
      title: "标题",
      dataIndex: "title",
      key: "title",
      ellipsis: true,
      width: 280,
    },
    {
      title: "主题",
      dataIndex: "topic",
      key: "topic",
      ellipsis: true,
      width: 200,
    },
    {
      title: "状态",
      dataIndex: "status",
      key: "status",
      width: 100,
      render: (status: string) => (
        <Tag color={statusColorMap[status] || "default"}>
          {statusLabelMap[status] || status}
        </Tag>
      ),
    },
    {
      title: "版本",
      dataIndex: "revision",
      key: "revision",
      width: 70,
      render: (v: number) => `v${v}`,
    },
    {
      title: "更新时间",
      dataIndex: "updatedAt",
      key: "updatedAt",
      width: 170,
      render: (t: string) => dayjs(t).format("YYYY-MM-DD HH:mm"),
    },
    {
      title: "操作",
      key: "actions",
      width: 280,
      render: (_: unknown, record: ArticleRecord) => (
        <Space size="small">
          <Button
            type="link"
            size="small"
            icon={<EyeOutlined />}
            onClick={() => setViewArticle(record)}
          >
            查看
          </Button>
          <Button
            type="link"
            size="small"
            icon={<EditOutlined />}
            onClick={() => {
              setEditArticle(record);
              setEditTitle(record.title);
              setEditContent(record.content);
            }}
          >
            编辑
          </Button>
          {record.status === "Draft" && (
            <Button
              type="link"
              size="small"
              onClick={() => handleUpdateStatus(record.id, "Published")}
            >
              发布
            </Button>
          )}
          {record.status === "Approved" && (
            <Button
              type="link"
              size="small"
              onClick={() => handleUpdateStatus(record.id, "Published")}
            >
              发布
            </Button>
          )}
          <Popconfirm
            title="确定删除？"
            onConfirm={() => handleDelete(record.id)}
          >
            <Button type="link" size="small" danger icon={<DeleteOutlined />}>
              删除
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <Typography.Title level={4}>
        <FileTextOutlined style={{ marginRight: 8 }} />
        文章管理
      </Typography.Title>
      <Typography.Paragraph type="secondary">
        管理 Agent 和 Workflow 产出的文章，查看内容、编辑和发布。
      </Typography.Paragraph>

      <Spin spinning={loading}>
        {articles.length === 0 && !loading ? (
          <Empty description="暂无文章，通过 Agent 对话或 Workflow 运行产出文章" />
        ) : (
          <Table
            dataSource={articles}
            columns={columns}
            rowKey="id"
            pagination={{ pageSize: 10 }}
            size="middle"
          />
        )}
      </Spin>

      {/* 查看文章 */}
      <Modal
        title={viewArticle?.title}
        open={!!viewArticle}
        onCancel={() => setViewArticle(null)}
        footer={null}
        width={720}
      >
        {viewArticle && (
          <div>
            <Space style={{ marginBottom: 16 }}>
              <Tag color={statusColorMap[viewArticle.status]}>
                {statusLabelMap[viewArticle.status] || viewArticle.status}
              </Tag>
              <Typography.Text type="secondary">
                v{viewArticle.revision} ·{" "}
                {dayjs(viewArticle.updatedAt).format("YYYY-MM-DD HH:mm")}
              </Typography.Text>
            </Space>
            <div style={{ maxHeight: 500, overflow: "auto" }}>
              <XMarkdown>{viewArticle.content}</XMarkdown>
            </div>
          </div>
        )}
      </Modal>

      {/* 编辑文章 */}
      <Modal
        title="编辑文章"
        open={!!editArticle}
        onCancel={() => setEditArticle(null)}
        onOk={handleSaveEdit}
        confirmLoading={saving}
        width={720}
      >
        <Space direction="vertical" style={{ width: "100%" }} size="middle">
          <Input
            placeholder="文章标题"
            value={editTitle}
            onChange={(e) => setEditTitle(e.target.value)}
          />
          <Input.TextArea
            placeholder="文章内容（支持 Markdown）"
            value={editContent}
            onChange={(e) => setEditContent(e.target.value)}
            rows={16}
          />
        </Space>
      </Modal>
    </div>
  );
}
