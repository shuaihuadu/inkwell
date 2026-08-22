import {
    CheckCircleFilled,
    CloseCircleFilled,
    LoadingOutlined,
} from "@ant-design/icons";
import { ThoughtChain, type ThoughtChainItemType } from "@ant-design/x";
import { Flex, Typography, theme as antdTheme } from "antd";
import { useEffect, useState } from "react";

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
    /** 工具调用参数；调用中默认展开。 */
    parameters?: string;
    /** 工具调用成功后的结果摘要。 */
    result?: string;
    /** 工具调用失败原因。 */
    error?: string;
    /** 原型演示需要在完成态持续展示详情时使用。 */
    defaultExpanded?: boolean;
    status: HarnessStepStatus;
}

function StepDetail({ step }: { step: HarnessStep }) {
    const { token } = antdTheme.useToken();

    if (!step.parameters && !step.result && !step.error) return step.detail;

    return (
        <Flex vertical gap={8} style={{ maxWidth: 560 }}>
            {step.parameters ? (
                <Flex vertical gap={3}>
                    <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                        参数
                    </Typography.Text>
                    <Typography.Text
                        style={{
                            display: "block",
                            padding: "6px 8px",
                            color: token.colorText,
                            background: token.colorFillSecondary,
                            border: `1px solid ${token.colorBorderSecondary}`,
                            borderRadius: token.borderRadiusSM,
                            fontFamily: token.fontFamilyCode,
                            fontSize: 12,
                            whiteSpace: "pre-wrap",
                            wordBreak: "break-word",
                        }}
                    >
                        {step.parameters}
                    </Typography.Text>
                </Flex>
            ) : null}
            {step.result ? (
                <Flex vertical gap={3}>
                    <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                        结果
                    </Typography.Text>
                    <Typography.Text>{step.result}</Typography.Text>
                </Flex>
            ) : null}
            {step.error ? (
                <Flex vertical gap={3}>
                    <Typography.Text type="danger" style={{ fontSize: 12 }}>
                        失败原因
                    </Typography.Text>
                    <Typography.Text type="danger">
                        {step.error}
                    </Typography.Text>
                </Flex>
            ) : null}
        </Flex>
    );
}

export function HarnessThoughtChain({
    steps,
    title,
}: {
    steps: HarnessStep[];
    title: "工具调用" | "Agent Loop";
}) {
    const { token } = antdTheme.useToken();
    const activeStep = steps.find((step) => step.status === "loading");
    const failedSteps = steps.filter((step) => step.status === "error");
    const completedCount = steps.filter(
        (step) => step.status === "success",
    ).length;
    const statusKey = steps
        .map((step) => `${step.key}:${step.status}`)
        .join("|");
    const [expandedKeys, setExpandedKeys] = useState<string[]>([]);

    useEffect(() => {
        setExpandedKeys(
            steps
                .filter(
                    (step) =>
                        Boolean(
                            step.detail ||
                            step.parameters ||
                            step.result ||
                            step.error,
                        ) &&
                        (step.defaultExpanded ||
                            step.status === "loading" ||
                            step.status === "error"),
                )
                .map((step) => step.key),
        );
    }, [statusKey]);

    const items: ThoughtChainItemType[] = steps.map((step) => ({
        key: step.key,
        title: step.title,
        description: step.description,
        content: <StepDetail step={step} />,
        status: step.status,
        collapsible: Boolean(
            step.detail || step.parameters || step.result || step.error,
        ),
        icon: step.status === "loading" ? <LoadingOutlined /> : undefined,
    }));

    return (
        <Flex vertical gap={10} style={{ minWidth: 280 }}>
            <Flex align="center" gap={7}>
                {activeStep ? (
                    <LoadingOutlined style={{ color: token.colorPrimary }} />
                ) : failedSteps.length > 0 ? (
                    <CloseCircleFilled style={{ color: token.colorError }} />
                ) : (
                    <CheckCircleFilled style={{ color: token.colorSuccess }} />
                )}
                <Typography.Text strong>{title}</Typography.Text>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                    {activeStep
                        ? activeStep.title
                        : failedSteps.length > 0
                          ? `${failedSteps.length} 项失败`
                          : `${completedCount} 项已完成`}
                </Typography.Text>
            </Flex>
            <ThoughtChain
                items={items}
                expandedKeys={expandedKeys}
                onExpand={setExpandedKeys}
            />
        </Flex>
    );
}
