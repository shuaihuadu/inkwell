import { XMarkdown } from "@ant-design/x-markdown";
import "@ant-design/x-markdown/themes/dark.css";
import "@ant-design/x-markdown/themes/light.css";
import { theme } from "antd";
import { memo, type CSSProperties } from "react";

interface MarkdownContentProps {
    appearance: "light" | "dark";
    content: string;
    streaming?: boolean;
}

export const MarkdownContent = memo(function MarkdownContent({
    appearance,
    content,
    streaming = false,
}: MarkdownContentProps) {
    const { token } = theme.useToken();
    const compactStyles = {
        "--primary-color": token.colorPrimary,
        "--primary-color-hover": token.colorPrimaryHover,
        "--margin-block": "0 0 8px 0",
        "--margin-ul-ol": "0 0 8px 22px",
        "--margin-li": "0 0 2px 0",
        "--table-margin": "0 0 8px 0",
        "--margin-pre": "0 0 8px 0",
        "--padding-code": "10px 12px",
    } as CSSProperties;

    return (
        <XMarkdown
            className={
                appearance === "dark" ? "x-markdown-dark" : "x-markdown-light"
            }
            content={content}
            openLinksInNewTab
            streaming={{ hasNextChunk: streaming }}
            style={compactStyles}
        />
    );
});
