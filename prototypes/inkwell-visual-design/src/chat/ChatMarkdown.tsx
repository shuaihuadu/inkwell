import { memo, type CSSProperties } from "react";
import { XMarkdown } from "@ant-design/x-markdown";
import "@ant-design/x-markdown/themes/light.css";
import "@ant-design/x-markdown/themes/dark.css";
import { theme as antdTheme } from "antd";
import { useDesign } from "../context/DesignContext";

// ─── 消息气泡内容的 Markdown 渲染 ────────────────────────────────────────────
// user/ai 气泡的 `content` 按 Markdown 解析展示（加粗/列表/代码块/表格/链接等），而不是
// 纯文本——真实场景里模型回复几乎总会带一些 Markdown 格式（列表、代码、加粗强调），不支持
// 解析的话这些标记符会原样露出来，看起来很不专业。用 Ant Design X 官方配套的
// `@ant-design/x-markdown`（跟已经在用的 `@ant-design/x` 同一个团队维护、版本号也对齐），
// 不是随便挑一个第三方 markdown 库——`XMarkdown` 本身就是为"大模型流式输出"场景设计的
// （用 marked 做底层解析、默认不解释原始 HTML，不需要 `dangerouslySetInnerHTML`，安全默认
// 值跟社区里常见的 react-markdown 是同一个思路，但作为官方配套组件，跟 Bubble/主题体系
// 天然更贴合，不需要自己再手搭一套样式）。
// 流式输出期间内容会不断增长，中途出现"没闭合的 ** 或代码块"是正常的、预期内的短暂现象，
// `XMarkdown` 对不完整语法很宽容，闭合之前就先按纯文本展示那一小段，不会报错/崩溃。

/** 内置主题（light.css/dark.css）默认的行间距是给"文档阅读"场景调的，在气泡这种窄而
 * 紧凑的容器里显得偏松散；照官方文档"基于内置主题类叠加自定义类、覆盖 CSS 变量"的方式
 * 收紧间距，不改内置主题本身。CSS 变量直接通过 `style` 内联设置（而不是拼一个 `<style>`
 * 标签塞进 DOM），每条消息各自的变量值互相独立，也不会在页面里堆一堆重复的样式标签。
 * 链接色顺带同步成当前应用的主色（而不是 antd 默认蓝），跟三套主题（曜石紫/朱砂橙/碧海青）
 * 切换时保持一致。 */
export const ChatMarkdown = memo(function ChatMarkdown({ content }: { content: string }) {
    const { token } = antdTheme.useToken();
    const { isDark } = useDesign();

    const compactVars = {
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
            content={content}
            className={isDark ? "x-markdown-dark" : "x-markdown-light"}
            style={compactVars}
            openLinksInNewTab
        />
    );
});


