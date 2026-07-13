import { LockOutlined, UserOutlined } from "@ant-design/icons";
import { Alert, Button, Form, Input, Typography } from "antd";
import { useState } from "react";
import { desktopApi } from "../../shared/network/desktop-api";
import type { LoginRequest } from "../../shared/network/contracts";
import { useAuthStore } from "./auth-store";

export function LoginPage() {
    const setSession = useAuthStore((state) => state.setSession);
    const [error, setError] = useState<string | null>(null);
    const [submitting, setSubmitting] = useState(false);

    const login = async (values: LoginRequest): Promise<void> => {
        setSubmitting(true);
        setError(null);
        try {
            setSession(await desktopApi.login(values));
        } catch (reason) {
            setError(
                reason instanceof Error
                    ? reason.message
                    : "登录失败，请检查服务状态。",
            );
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <main className="login-page">
            <section className="login-brand" aria-label="Inkwell 产品介绍">
                <div className="brand-mark">I</div>
                <div>
                    <Typography.Title>Inkwell</Typography.Title>
                    <Typography.Paragraph>
                        创建、管理并运行团队的智能 Agent。
                    </Typography.Paragraph>
                </div>
                <div className="login-pattern" aria-hidden="true" />
            </section>
            <section className="login-form-panel">
                <div className="login-form-wrap">
                    <Typography.Text className="eyebrow">
                        工作空间登录
                    </Typography.Text>
                    <Typography.Title level={2}>欢迎回来</Typography.Title>
                    <Typography.Paragraph type="secondary">
                        使用 Inkwell 账户继续进入 Agent 工作台。
                    </Typography.Paragraph>
                    {error && (
                        <Alert
                            type="error"
                            showIcon
                            message="无法登录"
                            description={error}
                        />
                    )}
                    <Form<LoginRequest>
                        layout="vertical"
                        initialValues={{
                            username: "admin",
                            password: "Admin@123456",
                        }}
                        onFinish={login}
                        requiredMark={false}
                    >
                        <Form.Item
                            label="用户名"
                            name="username"
                            rules={[
                                { required: true, message: "请输入用户名" },
                            ]}
                        >
                            <Input
                                size="large"
                                prefix={<UserOutlined />}
                                autoComplete="username"
                            />
                        </Form.Item>
                        <Form.Item
                            label="密码"
                            name="password"
                            rules={[{ required: true, message: "请输入密码" }]}
                        >
                            <Input.Password
                                size="large"
                                prefix={<LockOutlined />}
                                autoComplete="current-password"
                            />
                        </Form.Item>
                        <Button
                            block
                            size="large"
                            type="primary"
                            htmlType="submit"
                            loading={submitting}
                        >
                            登录
                        </Button>
                    </Form>
                </div>
            </section>
        </main>
    );
}
