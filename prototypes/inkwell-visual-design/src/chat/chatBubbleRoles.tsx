import { useMemo } from "react";
import { Flex, Typography } from "antd";
import { BarChartOutlined, BulbOutlined, SyncOutlined } from "@ant-design/icons";
import { Actions, Think, type BubbleListProps, type BubbleItemType } from "@ant-design/x";
import type { ChatMessage } from "./useMockChat";
import { ChatMarkdown } from "./ChatMarkdown";
import { HarnessThoughtChain } from "./HarnessThoughtChain";
import { TodosPanel } from "./TodosPanel";

// ─── 共用 Bubble.List 角色配置（UI-005 / UI-004 内嵌面板共用） ─────────────────────
// user/ai 是常规消息气泡。harness/todos 对应 Harness 演示场景（见
// harnessDemo.ts），分别用 ThoughtChain 承载多步骤自主循环、用自定义 TodosPanel 承载任务清单。
// ai 气泡底部挂"重新生成 / 复制 / 点赞点踩"三个动作（参考 ultramodern / copilot playground 的
// Bubble 角色 footer 配置）。流式输出模拟不靠 Ant Design X 自带的 `typing` 动画（它基于
// useLayoutEffect + rAF 循环实现，在 React StrictMode 下会因为没有 effect cleanup 而被双重调用
// 打断，卡在第一帧不再推进），而是所有场景统一走 AG-UI 事件管线（见 ../chat/agui/），本文件
// 不需要为此额外配置任何东西。
// 注意：官方两个 playground 都没有为 role 设置 `avatar`（都没显示头像），本文件也保持
// 一致不设 `avatar`（Bubble 的 `avatar` 默认不渲染任何内容，不需要显式传 `false`）。

export function createChatBubbleRoles(onRetry?: () => void): BubbleListProps["role"] {
    return {
        user: {
            placement: "end",
            variant: "filled",
            // 用户/AI 气泡的正文改成按 Markdown 解析展示（加粗/列表/代码块/表格/链接等），
            // 而不是纯文本——真实场景里模型回复几乎总会带一些 Markdown 格式，不支持解析的话
            // 这些标记符会原样露出来。触发回调见 `ChatMarkdown.tsx`。
            contentRender: (content) => <ChatMarkdown content={String(content ?? "")} />,
        },
        ai: {
            placement: "start",
            variant: "outlined",
            contentRender: (content) => <ChatMarkdown content={String(content ?? "")} />,
            // 用量统计（对应 UsageDisplayObserver）不再单独占一条气泡展示在回复前面，
            // 而是作为这条回复消息自己的一部分，显示在正文下方、Actions 上方（消息最后面）。
            footer: (content, info) =>
                content ? (
                    <Flex vertical gap={4}>
                        {info.extraInfo?.usage ? (
                            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                                <BarChartOutlined style={{ marginInlineEnd: 4 }} />
                                {String(info.extraInfo.usage)}
                            </Typography.Text>
                        ) : null}
                        <Actions
                            items={[
                                {
                                    key: "retry",
                                    label: "重新生成",
                                    icon: <SyncOutlined />,
                                    onItemClick: () => onRetry?.(),
                                },
                                {
                                    key: "copy",
                                    actionRender: <Actions.Copy text={String(content)} />,
                                },
                                {
                                    key: "feedback",
                                    actionRender: <Actions.Feedback />,
                                },
                            ]}
                        />
                    </Flex>
                ) : null,
        },
        harness: (data: BubbleItemType) => ({
            placement: "start",
            variant: "borderless",
            avatar: false,
            contentRender: () => <HarnessThoughtChain steps={data.extraInfo?.harnessSteps ?? []} />,
        }),
        loop: (data: BubbleItemType) => ({
            placement: "start",
            variant: "borderless",
            avatar: false,
            // Agent Loop（`LoopAgent` + `LoopEvaluator` 驱动的"第 N 轮迭代 → 评估器给 continue/stop
            // 反馈"循环）跟 Harness 的 plan→execute 步骤链视觉形态完全一致，直接复用同一个
            // ThoughtChain 承载组件，只是步骤内容语义不同（迭代轮次 + 评估反馈，而不是工具调用）。
            contentRender: () => <HarnessThoughtChain steps={data.extraInfo?.loopSteps ?? []} />,
        }),
        todos: (data: BubbleItemType) => ({
            placement: "start",
            variant: "filled",
            avatar: false,
            contentRender: () => <TodosPanel items={data.extraInfo?.todos ?? []} />,
        }),
        reasoning: (data: BubbleItemType) => ({
            placement: "start",
            variant: "borderless",
            avatar: false,
            contentRender: () => (
                <Think
                    title={data.extraInfo?.reasoningLoading ? "深度思考中" : "已完成思考"}
                    icon={<BulbOutlined />}
                    loading={Boolean(data.extraInfo?.reasoningLoading)}
                    defaultExpanded={false}
                >
                    {String(data.content ?? "")}
                </Think>
            ),
        }),
    };
}

/** 把 useMockChat 的消息列表转换成 Bubble.List 需要的 items 结构；replying 为 true 时
 * 额外追加一条内容为空、loading 的 ai 气泡，展示 Ant Design X 原生的加载态动画。 */
export function toBubbleItems(
    messages: ChatMessage[],
    replying = false,
): BubbleItemType[] {
    const items: BubbleItemType[] = messages.map((m) => ({
        key: m.id,
        role: m.role,
        content: m.content,
        extraInfo:
            m.role === "harness"
                ? { harnessSteps: m.harnessSteps }
                : m.role === "loop"
                  ? { loopSteps: m.loopSteps }
                  : m.role === "todos"
                    ? { todos: m.todos }
                    : m.role === "reasoning"
                      ? { reasoningLoading: m.reasoningLoading }
                      : m.role === "ai"
                        ? { usage: m.usage }
                        : undefined,
    }));
    if (replying) {
        items.push({ key: "__loading__", role: "ai", content: "", loading: true });
    }
    return items;
}

/** 两处页面里 `useMemo(() => createChatBubbleRoles(...), [...])` 的包装完全一致，
 * 收进一个 hook 里，调用点只需要传 onRetry。 */
export function useChatBubbleRoles(onRetry?: () => void): BubbleListProps["role"] {
    return useMemo(() => createChatBubbleRoles(onRetry), [onRetry]);
}
