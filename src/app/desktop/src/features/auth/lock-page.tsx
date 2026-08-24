import { LockOutlined, UserOutlined } from "@ant-design/icons";
import { useQueryClient } from "@tanstack/react-query";
import { Alert, Avatar, Button, Form, Input, Space, Typography } from "antd";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { desktopApi } from "../../shared/network/desktop-api";
import type { UnlockFailureCode } from "../../shared/network/contracts";
import { useAuthStore } from "./auth-store";

const unlockErrorKeys: Record<UnlockFailureCode, string> = {
    "invalid-password": "auth.lock.errors.invalidPassword",
    "account-locked": "auth.lock.errors.accountLocked",
    offline: "auth.lock.errors.offline",
    unknown: "auth.lock.errors.unknown",
};

interface UnlockForm {
    password: string;
}

export function LockPage() {
    const { t } = useTranslation();
    const queryClient = useQueryClient();
    const identity = useAuthStore((state) => state.identity);
    const setSnapshot = useAuthStore((state) => state.setSnapshot);
    const [error, setError] = useState<string | null>(null);
    const [submitting, setSubmitting] = useState(false);

    const unlock = async ({ password }: UnlockForm): Promise<void> => {
        setSubmitting(true);
        setError(null);
        try {
            const result = await desktopApi.unlock(password);
            if (result.ok) {
                setSnapshot({
                    status: "authenticated",
                    identity: result.identity,
                });
            } else if (result.code !== "account-locked") {
                // 账号锁定时主进程已将全局状态跳转到匿名态，界面会切换到登录页，无需在本组件继续展示错误。
                setError(t(unlockErrorKeys[result.code]));
            }
        } catch {
            setError(t(unlockErrorKeys.unknown));
        } finally {
            setSubmitting(false);
        }
    };

    const logout = async (): Promise<void> => {
        await desktopApi.logout();
        queryClient.clear();
        setSnapshot({ status: "anonymous", identity: null });
    };

    return (
        <main className="lock-page">
            <section className="lock-panel">
                <Avatar
                    size={64}
                    icon={<UserOutlined />}
                    className="lock-avatar"
                />
                <div className="lock-heading">
                    <Typography.Title level={4}>
                        {t("auth.lock.title")}
                    </Typography.Title>
                    <Typography.Text type="secondary">
                        {t("auth.lock.continueAs", {
                            username: identity?.username,
                        })}
                    </Typography.Text>
                </div>
                {error && <Alert type="error" showIcon message={error} />}
                <Form<UnlockForm>
                    layout="vertical"
                    onFinish={unlock}
                    requiredMark={false}
                >
                    <Form.Item
                        name="password"
                        rules={[
                            {
                                required: true,
                                message: t("auth.lock.passwordRequired"),
                            },
                        ]}
                    >
                        <Input.Password
                            autoFocus
                            size="large"
                            prefix={<LockOutlined />}
                            autoComplete="current-password"
                            placeholder={t("auth.lock.passwordPlaceholder")}
                        />
                    </Form.Item>
                    <Button
                        block
                        size="large"
                        type="primary"
                        htmlType="submit"
                        loading={submitting}
                    >
                        {t("auth.lock.unlock")}
                    </Button>
                </Form>
                <Space size={16}>
                    <Button
                        type="link"
                        size="small"
                        onClick={() => void logout()}
                    >
                        {t("auth.lock.switchAccount")}
                    </Button>
                    <Button
                        type="link"
                        size="small"
                        danger
                        onClick={() => void logout()}
                    >
                        {t("auth.lock.logout")}
                    </Button>
                </Space>
            </section>
        </main>
    );
}
