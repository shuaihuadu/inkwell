import { CloudUploadOutlined } from "@ant-design/icons";
import { Attachments, Sender, type AttachmentsProps } from "@ant-design/x";
import type { GetProp } from "antd";

// ─── 共用的 Sender.Header 上传附件面板（UI-005 对话页 / UI-004 内嵌面板共用） ────────
// 两处的 <Sender.Header> + <Attachments> 结构此前逐字重复；收进这一个组件后，以后真正
// 接入多模态解析（真实上传/预览/删除），只需要改这一处，两个入口自动同步。
// beforeUpload 固定拦截真实上传，本原型只做视觉展示。

export function ChatAttachmentsHeader({
    open,
    onOpenChange,
    files,
    onFilesChange,
}: {
    open: boolean;
    onOpenChange: (open: boolean) => void;
    files: GetProp<AttachmentsProps, "items">;
    onFilesChange: (files: GetProp<AttachmentsProps, "items">) => void;
}) {
    return (
        <Sender.Header
            title="上传文件"
            styles={{ content: { padding: 0 } }}
            open={open}
            onOpenChange={onOpenChange}
            forceRender
        >
            <Attachments
                beforeUpload={() => false}
                items={files}
                onChange={({ fileList }) => onFilesChange(fileList)}
                placeholder={(type) =>
                    type === "drop"
                        ? { title: "拖拽文件到此处" }
                        : {
                              icon: <CloudUploadOutlined />,
                              title: "上传文件",
                              description: "点击或拖拽文件到此区域上传",
                          }
                }
            />
        </Sender.Header>
    );
}
