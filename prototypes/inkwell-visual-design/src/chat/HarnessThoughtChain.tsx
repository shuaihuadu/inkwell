import { LoadingOutlined } from "@ant-design/icons";
import { ThoughtChain, type ThoughtChainItemType } from "@ant-design/x";

// ─── Harness 自主循环的步骤链（对应 agent-framework Harness 示例里
// LoopAgentOptions / plan-execute 双模式驱动的多轮自主循环，每一轮在 UI 上表现为一个
// 可折叠的思维链节点） ──────────────────────────────────────────────────────────
// 用 Ant Design X 的 ThoughtChain 组件承载：https://ant-design-x.antgroup.com/components/thought-chain-cn
// status 沿用 ThoughtChain 自身的 'loading' | 'success' | 'error' | 'abort' 词汇。

export type HarnessStepStatus = "loading" | "success" | "error" | "abort";

export interface HarnessStep {
    key: string;
    title: string;
    /** 常驻显示的一句话描述（如"调用 网页搜索"） */
    description?: string;
    /** 折叠展开后才显示的详细内容（如工具调用参数/返回摘要） */
    detail?: string;
    status: HarnessStepStatus;
}

export function HarnessThoughtChain({ steps }: { steps: HarnessStep[] }) {
    const items: ThoughtChainItemType[] = steps.map((step) => ({
        key: step.key,
        title: step.title,
        description: step.description,
        content: step.detail,
        status: step.status,
        collapsible: Boolean(step.detail),
        icon: step.status === "loading" ? <LoadingOutlined /> : undefined,
    }));

    return (
        <ThoughtChain
            items={items}
            defaultExpandedKeys={steps.filter((s) => s.detail).map((s) => s.key)}
        />
    );
}
