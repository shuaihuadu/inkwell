import {
    CheckCircleFilled,
    CloseCircleFilled,
    MinusCircleFilled,
    LoadingOutlined,
} from "@ant-design/icons";
import { ThoughtChain, type ThoughtChainItemType } from "@ant-design/x";
import { Flex, Typography, theme } from "antd";
import type { SkillRunActivity } from "../../shared/network/contracts";

interface SkillActivityChainProps {
    activities: SkillRunActivity[];
}

const getActivityTitle = (activity: SkillRunActivity): string => {
    switch (activity.type) {
        case "skill-loaded":
            return `加载 Skill：${activity.skillName}`;
        case "skill-resource-read":
            return `读取资源：${activity.targetName ?? activity.skillName}`;
        case "skill-script-run":
            return `运行脚本：${activity.targetName ?? activity.skillName}`;
    }
};

const getActivityDescription = (activity: SkillRunActivity): string => {
    switch (activity.type) {
        case "skill-loaded":
            return "读取 Skill 指令";
        case "skill-resource-read":
            return `来自 ${activity.skillName}`;
        case "skill-script-run":
            return `来自 ${activity.skillName}`;
    }
};

const formatArguments = (argumentsJson: string): string => {
    try {
        return JSON.stringify(JSON.parse(argumentsJson), null, 2);
    } catch {
        return argumentsJson;
    }
};

export function SkillActivityChain({ activities }: SkillActivityChainProps) {
    const { token } = theme.useToken();
    const activeActivity = activities.find(
        (activity) => activity.status === "loading",
    );
    const failedCount = activities.filter(
        (activity) => activity.status === "error",
    ).length;
    const abortedCount = activities.filter(
        (activity) => activity.status === "abort",
    ).length;
    const completedCount = activities.filter(
        (activity) => activity.status === "success",
    ).length;
    const items: ThoughtChainItemType[] = activities.map((activity) => {
        return {
            key: activity.callId,
            title: getActivityTitle(activity),
            description:
                activity.status === "loading"
                    ? "调用中"
                    : activity.status === "error"
                      ? "调用失败"
                      : activity.status === "abort"
                        ? "已停止"
                        : activity.type === "skill-loaded"
                          ? "加载成功"
                          : "调用成功",
            content: (
                <Flex vertical gap={8} className="skill-activity-detail">
                    <Typography.Text type="secondary">
                        {getActivityDescription(activity)}
                    </Typography.Text>
                    <Flex vertical gap={3}>
                        <Typography.Text type="secondary">参数</Typography.Text>
                        <Typography.Text className="skill-activity-parameters">
                            {formatArguments(activity.argumentsJson)}
                        </Typography.Text>
                    </Flex>
                    {activity.error && (
                        <Typography.Text type="danger">
                            {activity.error}
                        </Typography.Text>
                    )}
                </Flex>
            ),
            status: activity.status,
            icon:
                activity.status === "loading" ? <LoadingOutlined /> : undefined,
            collapsible: true,
        };
    });

    return (
        <Flex className="skill-activity-chain" vertical gap={10}>
            <Flex align="center" gap={7}>
                {activeActivity ? (
                    <LoadingOutlined style={{ color: token.colorPrimary }} />
                ) : failedCount > 0 ? (
                    <CloseCircleFilled style={{ color: token.colorError }} />
                ) : abortedCount > 0 ? (
                    <MinusCircleFilled style={{ color: token.colorWarning }} />
                ) : (
                    <CheckCircleFilled style={{ color: token.colorSuccess }} />
                )}
                <Typography.Text strong>工具调用</Typography.Text>
                <Typography.Text type="secondary" className="activity-summary">
                    {activeActivity
                        ? getActivityTitle(activeActivity)
                        : failedCount > 0
                          ? `${failedCount} 项失败`
                          : abortedCount > 0
                            ? `${abortedCount} 项已停止`
                            : `${completedCount} 项已完成`}
                </Typography.Text>
            </Flex>
            <ThoughtChain items={items} />
        </Flex>
    );
}
