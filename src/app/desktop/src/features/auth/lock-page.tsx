import { LockOutlined, LogoutOutlined } from "@ant-design/icons";
import { useQueryClient } from "@tanstack/react-query";
import { Alert, Button, Form, Input, Typography } from "antd";
import { useState } from "react";
import { desktopApi } from "../../shared/network/desktop-api";
import type { UnlockFailureCode } from "../../shared/network/contracts";
import { useAuthStore } from "./auth-store";

const unlockErrors: Record<UnlockFailureCode, string> = {
    "invalid-password": "密码错误，请重试",
    "account-locked": "账号已被锁定，请联系系统管理员",
    offline: "网络异常，已断开。请检查网络连接",
    unknown: "解锁失败，请稍后重试",
};

interface UnlockForm {
    password: string;
}

export function LockPage() {
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
                setError(unlockErrors[result.code]);
            }
        } catch {
            setError(unlockErrors.unknown);
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
                <div className="brand-mark small">I</div>
                <LockOutlined className="lock-icon" />
                <Typography.Title level={2}>Inkwell 已锁定</Typography.Title>
                <Typography.Paragraph type="secondary">
                    {identity?.username}，请输入密码继续。
                </Typography.Paragraph>
                {error && (
                    <Alert
                        type="error"
                        showIcon
                        message="无法解锁"
                        description={error}
                    />
                )}
                <Form<UnlockForm>
                    layout="vertical"
                    onFinish={unlock}
                    requiredMark={false}
                >
                    <Form.Item
                        name="password"
                        rules={[{ required: true, message: "请输入密码" }]}
                    >
                        <Input.Password
                            autoFocus
                            size="large"
                            prefix={<LockOutlined />}
                            autoComplete="current-password"
                            placeholder="密码"
                        />
                    </Form.Item>
                    <Button
                        block
                        size="large"
                        type="primary"
                        htmlType="submit"
                        loading={submitting}
                    >
                        解锁
                    </Button>
                </Form>
                <Button
                    type="text"
                    icon={<LogoutOutlined />}
                    onClick={() => void logout()}
                >
                    退出登录
                </Button>
            </section>
        </main>
    );
}
