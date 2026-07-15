import { useState } from "react";
import type { GetProp } from "antd";
import type { AttachmentsProps } from "@ant-design/x";

// ─── 共用的"回形针 → Sender.Header 展开上传面板"状态（UI-005 / UI-004 内嵌面板共用） ──
// 两处入口视觉布局不同，但"是否展开面板 + 已选文件列表"这两个状态和交互完全一致，抽成
// 这个 hook 避免每处都重复写 useState；配合 ChatAttachmentsHeader 组件一起用。
// 目前仅做视觉展示（beforeUpload 拦截真实上传），是为后续接入多模态解析预留的统一入口。

export function useAttachments() {
    const [attachmentsOpen, setAttachmentsOpen] = useState(false);
    const [files, setFiles] = useState<GetProp<AttachmentsProps, "items">>([]);

    return { attachmentsOpen, setAttachmentsOpen, files, setFiles };
}
