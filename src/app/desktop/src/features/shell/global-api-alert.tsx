import { Alert } from "antd";
import { useTranslation } from "react-i18next";
import { useNetworkStore } from "./network-store";

export function GlobalApiAlert() {
    const { t } = useTranslation();
    const connectionStatus = useNetworkStore((state) => state.status);
    const globalError = useNetworkStore((state) => state.globalError);
    const clearGlobalError = useNetworkStore((state) => state.clearGlobalError);

    if (connectionStatus !== "online") {
        return (
            <Alert
                banner
                showIcon
                closable={false}
                className="global-api-alert"
                type="warning"
                message={t(
                    connectionStatus === "offline"
                        ? "shell.errors.offline"
                        : "shell.errors.reconnecting",
                )}
            />
        );
    }

    if (!globalError) return null;

    const messageKey =
        globalError.code === "rate-limited" &&
        globalError.retryAfterSeconds !== null
            ? "shell.errors.rateLimitedWithRetry"
            : `shell.errors.${globalError.code}`;

    return (
        <Alert
            banner
            showIcon
            closable
            className="global-api-alert"
            type={globalError.code === "rate-limited" ? "warning" : "error"}
            message={t(messageKey, {
                seconds: globalError.retryAfterSeconds,
            })}
            onClose={clearGlobalError}
        />
    );
}
