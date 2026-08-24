import { XMarkdown } from "@ant-design/x-markdown";
import "@ant-design/x-markdown/themes/dark.css";
import "@ant-design/x-markdown/themes/light.css";
import { theme } from "antd";
import {
    memo,
    useCallback,
    useEffect,
    useRef,
    useState,
    type CSSProperties,
} from "react";
import { getNextStreamingContent } from "./smooth-streaming-content";

const streamingFrameMilliseconds = 16;

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
    const [displayedContent, setDisplayedContent] = useState(content);
    const displayedContentRef = useRef(content);
    const targetContentRef = useRef(content);
    const streamingRef = useRef(streaming);
    const smoothingActiveRef = useRef(streaming);
    const revealTimerRef = useRef<number | undefined>(undefined);

    const revealNextFrame: () => void = useCallback(() => {
        const nextContent = getNextStreamingContent(
            displayedContentRef.current,
            targetContentRef.current,
        );
        displayedContentRef.current = nextContent;
        setDisplayedContent(nextContent);
        if (nextContent !== targetContentRef.current) {
            revealTimerRef.current = window.setTimeout(
                revealNextFrame,
                streamingFrameMilliseconds,
            );
        } else {
            revealTimerRef.current = undefined;
            if (!streamingRef.current) smoothingActiveRef.current = false;
        }
    }, []);

    useEffect(() => {
        targetContentRef.current = content;
        streamingRef.current = streaming;
        if (streaming) smoothingActiveRef.current = true;
        if (!smoothingActiveRef.current) {
            displayedContentRef.current = content;
            setDisplayedContent(content);
            return;
        }

        if (!content.startsWith(displayedContentRef.current)) {
            displayedContentRef.current = content;
            setDisplayedContent(content);
            smoothingActiveRef.current = streaming;
            return;
        }

        if (
            displayedContentRef.current !== content &&
            revealTimerRef.current === undefined
        ) {
            revealTimerRef.current = window.setTimeout(
                revealNextFrame,
                streamingFrameMilliseconds,
            );
        } else if (!streaming) {
            smoothingActiveRef.current = false;
        }
    }, [content, revealNextFrame, streaming]);

    useEffect(() => {
        return () => {
            if (revealTimerRef.current !== undefined) {
                window.clearTimeout(revealTimerRef.current);
            }
        };
    }, []);

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
    const hasPendingStreamingContent =
        streaming || displayedContent !== content;

    return (
        <div
            className="markdown-content-frame"
            data-stream-content-length={displayedContent.length}
        >
            <XMarkdown
                className={
                    appearance === "dark"
                        ? "x-markdown-dark"
                        : "x-markdown-light"
                }
                content={displayedContent}
                openLinksInNewTab
                streaming={{ hasNextChunk: hasPendingStreamingContent }}
                style={compactStyles}
            />
        </div>
    );
});
