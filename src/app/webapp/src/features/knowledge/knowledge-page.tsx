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
  Upload,
  Tabs,
  Collapse,
} from "antd";
import {
  BookOutlined,
  UploadOutlined,
  DeleteOutlined,
  FileTextOutlined,
  InboxOutlined,
  EyeOutlined,
} from "@ant-design/icons";
import type { UploadFile } from "antd";
import dayjs from "dayjs";
import { API_BASE } from "../../services/api";

interface KnowledgeDocument {
  id: string;
  title: string;
  fileType: string;
  sourceLink: string;
  chunkCount: number;
  addedAt: string;
  contentLength: number;
}

interface ChunkInfo {
  id: string;
  chunkIndex: number;
  contentLength: number;
  preview: string;
}

export default function KnowledgePage() {
  const [documents, setDocuments] = useState<KnowledgeDocument[]>([]);
  const [loading, setLoading] = useState(true);

  const [uploadVisible, setUploadVisible] = useState(false);
  const [uploadTitle, setUploadTitle] = useState("");
  const [uploadContent, setUploadContent] = useState("");
  const [uploading, setUploading] = useState(false);

  const [chunksVisible, setChunksVisible] = useState(false);
  const [chunks, setChunks] = useState<ChunkInfo[]>([]);
  const [chunksLoading, setChunksLoading] = useState(false);
  const [chunksDocTitle, setChunksDocTitle] = useState("");

  const fetchDocuments = async () => {
    try {
      const res = await fetch(`${API_BASE}/api/knowledge`);
      if (res.ok) {
        setDocuments(await res.json());
      }
    } catch {
      // ignore
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void fetchDocuments();
  }, []);

  const handleTextUpload = async () => {
    if (!uploadTitle.trim() || !uploadContent.trim()) {
      message.warning("标题和内容不能为空");
      return;
    }

    setUploading(true);
    try {
      const res = await fetch(`${API_BASE}/api/knowledge/upload`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ title: uploadTitle, content: uploadContent }),
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

  const handleFileUpload = async (file: UploadFile) => {
    const formData = new FormData();
    formData.append("file", file as unknown as File);

    try {
      const res = await fetch(`${API_BASE}/api/knowledge/upload-file`, {
        method: "POST",
        body: formData,
      });

      if (res.ok) {
        const data = await res.json();
        message.success(`文件上传成功: ${data.fileName} (${data.chunkCount} 个切片)`);
        setUploadVisible(false);
        await fetchDocuments();
      } else {
        const text = await res.text();
        message.error(`上传失败: ${text}`);
      }
    } catch (err) {
      message.error(`上传失败: ${(err as Error).message}`);
    }

    return false;
  };

  const handleDelete = async (id: string) => {
    try {
      const res = await fetch(`${API_BASE}/api/knowledge/${id}`, { method: "DELETE" });
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

  const showChunks = async (doc: KnowledgeDocument) => {
    setChunksDocTitle(doc.title);
    setChunksLoading(true);
    setChunksVisible(true);

    try {
      const res = await fetch(`${API_BASE}/api/knowledge/${doc.id}/chunks`);
      if (res.ok) {
        setChunks(await res.json());
      } else {
        setChunks([]);
      }
    } catch {
      setChunks([]);
    } finally {
      setChunksLoading(false);
    }
  };

  const columns = [
    {
      title: "标题",
      dataIndex: "title",
      key: "title",
      render: (text: string, record: KnowledgeDocument) => (
        <Space>
          <FileTextOutlined />
          <strong>{text}</strong>
          <Tag color={record.fileType === "md" ? "green" : "default"}>{record.fileType}</Tag>
        </Space>
      ),
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
      title: "切片",
      dataIndex: "chunkCount",
      key: "chunkCount",
      width: 80,
      render: (count: number) => <Tag color="purple">{count} 片</Tag>,
    },
    {
      title: "添加时间",
      dataIndex: "addedAt",
      key: "addedAt",
      width: 160,
      render: (date: string) => dayjs(date).format("YYYY-MM-DD HH:mm"),
    },
    {
      title: "操作",
      key: "action",
      width: 120,
      render: (_: unknown, record: KnowledgeDocument) => (
        <Space>
          <Button
            size="small"
            icon={<EyeOutlined />}
            onClick={() => void showChunks(record)}
          >
            切片
          </Button>
          <Popconfirm title="确定删除这个文档？" onConfirm={() => void handleDelete(record.id)}>
            <Button size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
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
        <Button type="primary" icon={<UploadOutlined />} onClick={() => setUploadVisible(true)}>
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

      <Modal
        title="上传文档"
        open={uploadVisible}
        onCancel={() => setUploadVisible(false)}
        footer={null}
        width={600}
      >
        <Tabs
          items={[
            {
              key: "text",
              label: "输入文本",
              children: (
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
                  <Button type="primary" onClick={() => void handleTextUpload()} loading={uploading}>
                    上传
                  </Button>
                </Space>
              ),
            },
            {
              key: "file",
              label: "上传文件",
              children: (
                <Upload.Dragger
                  accept=".txt,.text,.md,.markdown"
                  showUploadList={false}
                  customRequest={({ file }) => void handleFileUpload(file as UploadFile)}
                >
                  <p className="ant-upload-drag-icon">
                    <InboxOutlined />
                  </p>
                  <p className="ant-upload-text">点击或拖拽文件到此区域上传</p>
                  <p className="ant-upload-hint">支持 .txt / .md 文件，最大 5 MB</p>
                </Upload.Dragger>
              ),
            },
          ]}
        />
      </Modal>

      <Modal
        title={`切片预览 — ${chunksDocTitle}`}
        open={chunksVisible}
        onCancel={() => setChunksVisible(false)}
        footer={null}
        width={700}
      >
        {chunksLoading ? (
          <Spin />
        ) : chunks.length === 0 ? (
          <Typography.Text type="secondary">无切片数据</Typography.Text>
        ) : (
          <Collapse
            items={chunks.map((chunk) => ({
              key: chunk.id,
              label: (
                <Space>
                  <Tag>#{chunk.chunkIndex}</Tag>
                  <span>{chunk.contentLength} 字符</span>
                </Space>
              ),
              children: (
                <Typography.Paragraph
                  style={{ whiteSpace: "pre-wrap", fontSize: 13, margin: 0 }}
                >
                  {chunk.preview}
                </Typography.Paragraph>
              ),
            }))}
          />
        )}
      </Modal>
    </div>
  );
}
