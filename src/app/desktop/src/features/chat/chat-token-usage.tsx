import { BarChartOutlined } from "@ant-design/icons";
import { Typography } from "antd";
import { useTranslation } from "react-i18next";
import type { ChatTokenUsage } from "../../shared/network/contracts";
import { formatTokenCount } from "./token-count-format";

interface ChatTokenUsageProps {
    usage: ChatTokenUsage;
}

export function ChatTokenUsageSummary({ usage }: ChatTokenUsageProps) {
    const { t, i18n } = useTranslation();
    const items = [
        [t("chat.usage.input"), usage.inputTokenCount],
        [t("chat.usage.output"), usage.outputTokenCount],
        [t("chat.usage.total"), usage.totalTokenCount],
    ] as const;
    const visibleItems = items.filter(([, value]) => value !== null);
    if (visibleItems.length === 0) return null;

    return (
        <Typography.Text
            className="chat-token-usage"
            type="secondary"
            aria-label={t("chat.usage.label")}
        >
            <BarChartOutlined />
            {t("chat.usage.summaryLabel")} —{" "}
            {visibleItems.map(([label, value], index) => (
                <span key={label} className="chat-token-usage-item">
                    {index > 0 && " · "}
                    {label} {formatTokenCount(value!, i18n.language)}
                </span>
            ))}{" "}
            {t("chat.usage.tokens")}
        </Typography.Text>
    );
}
