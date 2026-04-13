import { useEffect, useState } from "react";
import {
  Typography,
  Table,
  Button,
  Modal,
  Input,
  Space,
  Spin,
  message,
  Popconfirm,
  Tag,
} from "antd";
import {
  BookOutlined,
  UploadOutlined,
  DeleteOutlined,
} from "@ant-design/icons";
import dayjs from "dayjs";
import { API_BASE } from "../../services/api";

interface KnowledgeDocument {
  id: string;
  title: string;
  sourceLink: string;
  addedAt: string;
  contentLength: number;
}

export default function KnowledgePage() {
  const [documents, setDocuments] = useState<KnowledgeDocument[]>([]);
  const [loading, setLoading] = useState(true);

  // 上传弹窗
  const [uploadVisible, setUploadVisible] = useState(false);
  const [uploadTitle, setUploadTitle] = useState("");
  const [uploadContent, setUploadContent] = useState("");
  const [uploading, setUploading] = useState(false);

  const fetchDocuments = async () => {
    try {
      const res = await fetch(`${API_BASE}/api/knowledge`);
      if (res.ok) {
        setDocuments(await res.json());
      }
    } catch {
      // 静默失败
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDocuments();
  }, []);

  const handleUpload = async () => {
    if (!uploadTitle.trim() || !uploadContent.trim()) {
      message.warning("标题和内容不能为空");
      return;
    }

    setUploading(true);
    try {
      const res = await fetch(`${API_BASE}/api/knowledge/upload`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          title: uploadTitle,
          content: uploadContent,
        }),
      });

      if (res.ok) {
        message.success("文档上传成功");
        setUploadVisible(false);
        setUploadTitle("");
        setUploadContent("");
        await fetchDocuments();
      } else {
        message.error("上传失败");
      }
    } catch (err) {
      message.error(`上传失败: ${(err as Error).message}`);
    } finally {
      setUploading(false);
    }
  };

  const handleDelete = async (id: string) => {
    try {
      const res = await fetch(`${API_BASE}/api/knowledge/${id}`, {
        method: "DELETE",
      });

      if (res.ok) {
        message.success("文档已删除");
        await fetchDocuments();
      } else {
        message.error("删除失败");
      }
    } catch (err) {
      message.error(`删除失败: ${(err as Error).message}`);
    }
  };

  const columns = [
    {
      title: "标题",
      dataIndex: "title",
      key: "title",
      render: (text: string) => <strong>{text}</strong>,
    },
    {
      title: "大小",
      dataIndex: "contentLength",
      key: "contentLength",
      width: 100,
      render: (len: number) => (
        <Tag color="blue">{len > 1000 ? `${(len / 1000).toFixed(1)}K` : `${len}`} 字符</Tag>
      ),
    },
    {
      title: "添加时间",
      dataIndex: "addedAt",
      key: "addedAt",
      width: 180,
      render: (date: string) => dayjs(date).format("YYYY-MM-DD HH:mm"),
    },
    {
      title: "操作",
      key: "action",
      width: 80,
      render: (_: unknown, record: KnowledgeDocument) => (
        <Popconfirm
          title="确定删除这个文档？"
          onConfirm={() => handleDelete(record.id)}
        >
          <Button size="small" danger icon={<DeleteOutlined />} />
        </Popconfirm>
      ),
    },
  ];

  if (loading) {
    return <Spin size="large" style={{ display: "block", marginTop: 100 }} />;
  }

  return (
    <div>
      <Space style={{ marginBottom: 16, width: "100%", justifyContent: "space-between" }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          <BookOutlined /> 知识库
        </Typography.Title>
        <Button
          type="primary"
          icon={<UploadOutlined />}
          onClick={() => setUploadVisible(true)}
        >
          上传文档
        </Button>
      </Space>

      <Table
        dataSource={documents}
        columns={columns}
        rowKey="id"
        pagination={false}
        locale={{ emptyText: "知识库为空，点击「上传文档」添加内容" }}
      />

      {/* 上传弹窗 */}
      <Modal
        title="上传文档"
        open={uploadVisible}
        onCancel={() => setUploadVisible(false)}
        onOk={handleUpload}
        confirmLoading={uploading}
        okText="上传"
        cancelText="取消"
      >
        <Space direction="vertical" style={{ width: "100%" }}>
          <Input
            placeholder="文档标题"
            value={uploadTitle}
            onChange={(e) => setUploadTitle(e.target.value)}
          />
          <Input.TextArea
            placeholder="文档内容（Markdown / 纯文本）"
            rows={10}
            value={uploadContent}
            onChange={(e) => setUploadContent(e.target.value)}
          />
        </Space>
      </Modal>
    </div>
  );
}
