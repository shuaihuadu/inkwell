import { CheckCircleFilled, LoadingOutlined } from "@ant-design/icons";
import { Flex, Typography, theme as antdTheme } from "antd";

// ─── Harness 风格的任务清单展示（对应 agent-framework Harness 示例里
// TodoCompletionLoopEvaluator 驱动的 "plan → execute" 循环维护的任务清单） ──────────
// pending 是灰色（空心圆点 + 灰色文字，还没开始，弱化显示）；in-progress 用 loading 图标
// （主题色，正在处理中）；done 是绿色勾选 + 绿色文字（做完了才"点亮"）——不用删除线，
// 删除线容易让人误读成"划掉/取消"，颜色从灰到绿的渐进更符合"任务逐步点亮完成"的直觉。
// 纯视觉展示，状态由调用方（src/chat/harnessDemo.ts）驱动更新，本组件不含任何状态逻辑。

export type TodoStatus = "pending" | "in-progress" | "done";

export interface TodoItem {
    key: string;
    label: string;
    status: TodoStatus;
}

export function TodosPanel({ items }: { items: TodoItem[] }) {
    const { token } = antdTheme.useToken();

    return (
        <Flex vertical gap={6} style={{ minWidth: 260 }}>
            {items.map((item) => (
                <Flex key={item.key} gap={8} align="center">
                    {item.status === "done" ? (
                        <CheckCircleFilled style={{ color: token.colorSuccess, flexShrink: 0 }} />
                    ) : item.status === "in-progress" ? (
                        <LoadingOutlined style={{ color: token.colorPrimary, flexShrink: 0 }} />
                    ) : (
                        <span
                            style={{
                                width: 12,
                                height: 12,
                                borderRadius: "50%",
                                border: `1.5px solid ${token.colorBorder}`,
                                flexShrink: 0,
                                display: "inline-block",
                            }}
                        />
                    )}
                    <Typography.Text
                        style={{
                            fontSize: 13,
                            color:
                                item.status === "done"
                                    ? token.colorSuccess
                                    : item.status === "in-progress"
                                      ? token.colorText
                                      : token.colorTextTertiary,
                        }}
                    >
                        {item.label}
                    </Typography.Text>
                </Flex>
            ))}
        </Flex>
    );
}
