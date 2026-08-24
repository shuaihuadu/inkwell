import {
    CheckCircleFilled,
    CloseCircleFilled,
    MinusCircleFilled,
    LoadingOutlined,
} from "@ant-design/icons";
import { ThoughtChain, type ThoughtChainItemType } from "@ant-design/x";
import { Flex, Typography, theme } from "antd";
import type { TFunction } from "i18next";
import { useTranslation } from "react-i18next";
import type { SkillRunActivity } from "../../shared/network/contracts";

interface SkillActivityChainProps {
    activities: SkillRunActivity[];
}

const getActivityTitle = (activity: SkillRunActivity, t: TFunction): string => {
    switch (activity.type) {
        case "skill-loaded":
            return t("chat.activity.title.loaded", {
                name: activity.skillName,
            });
        case "skill-resource-read":
            return t("chat.activity.title.resource", {
                name: activity.targetName ?? activity.skillName,
            });
        case "skill-script-run":
            return t("chat.activity.title.script", {
                name: activity.targetName ?? activity.skillName,
            });
        case "tool-called":
            return t("chat.activity.title.tool", {
                name: activity.functionName,
            });
    }
};

const getActivityDescription = (
    activity: SkillRunActivity,
    t: TFunction,
): string => {
    switch (activity.type) {
        case "skill-loaded":
            return t("chat.activity.readInstructions");
        case "skill-resource-read":
            return t("chat.activity.fromSkill", { name: activity.skillName });
        case "skill-script-run":
            return t("chat.activity.fromSkill", { name: activity.skillName });
        case "tool-called":
            return t("chat.activity.toolFunction", {
                name: activity.functionName,
            });
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
    const { t } = useTranslation();
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
            title: getActivityTitle(activity, t),
            description:
                activity.status === "loading"
                    ? t("chat.activity.status.loading")
                    : activity.status === "error"
                      ? t("chat.activity.status.failed")
                      : activity.status === "abort"
                        ? t("chat.activity.status.stopped")
                        : activity.type === "skill-loaded"
                          ? t("chat.activity.status.loaded")
                          : t("chat.activity.status.success"),
            content: (
                <Flex vertical gap={8} className="skill-activity-detail">
                    <Typography.Text type="secondary">
                        {getActivityDescription(activity, t)}
                    </Typography.Text>
                    <Flex vertical gap={3}>
                        <Typography.Text type="secondary">
                            {t("chat.activity.parameters")}
                        </Typography.Text>
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
                <Typography.Text strong>
                    {t("chat.activity.calls")}
                </Typography.Text>
                <Typography.Text type="secondary" className="activity-summary">
                    {activeActivity
                        ? getActivityTitle(activeActivity, t)
                        : failedCount > 0
                          ? t("chat.activity.failedCount", {
                                count: failedCount,
                            })
                          : abortedCount > 0
                            ? t("chat.activity.stoppedCount", {
                                  count: abortedCount,
                              })
                            : t("chat.activity.completedCount", {
                                  count: completedCount,
                              })}
                </Typography.Text>
            </Flex>
            <ThoughtChain items={items} />
        </Flex>
    );
}
