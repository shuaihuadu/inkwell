import { LockOutlined, UserOutlined } from '@ant-design/icons'
import { Alert, Button, ConfigProvider, Form, Input, Typography } from 'antd'
import type { InputRef } from 'antd'
import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { desktopApi } from '../../shared/network/desktop-api'
import type { LoginFailureCode, LoginRequest } from '../../shared/network/contracts'
import { useNetworkStore } from '../shell/network-store'
import { useAuthStore } from './auth-store'

const loginErrorKeys: Record<LoginFailureCode, string> = {
    'invalid-credentials': 'auth.errors.invalidCredentials',
    'account-locked': 'auth.errors.accountLocked',
    'rate-limited': 'auth.errors.rateLimited',
    offline: 'auth.errors.offline',
    unknown: 'auth.errors.unknown',
};

interface LoginPageProps {
    initiallyOffline?: boolean;
}

export function LoginPage({ initiallyOffline = false }: LoginPageProps) {
    const { t } = useTranslation();
    const [form] = Form.useForm<LoginRequest>();
    const passwordInputRef = useRef<InputRef>(null);
    const setSnapshot = useAuthStore((state) => state.setSnapshot);
    const connectionStatus = useNetworkStore((state) => state.status);
    const [failure, setFailure] = useState<LoginFailureCode | null>(
        initiallyOffline ? "offline" : null,
    );
    const [submitting, setSubmitting] = useState(false);
    const fieldsDisabled = submitting;
    const submitDisabled =
        fieldsDisabled ||
        failure === "account-locked" ||
        connectionStatus !== "online";

    useEffect(() => {
        if (failure === "invalid-credentials")
            passwordInputRef.current?.focus();
    }, [failure]);

    const login = async (values: LoginRequest): Promise<void> => {
        setSubmitting(true);
        setFailure(null);

        try {
            const result = await desktopApi.login(values);
            if (result.ok) {
                setSnapshot({
                    status: "authenticated",
                    identity: result.identity,
                });
            } else {
                setFailure(result.code);
                if (result.code === "invalid-credentials") {
                    form.setFieldValue("password", "");
                }
                if (result.code === "offline")
                    setSnapshot({ status: "offline", identity: null });
            }
        } catch {
            setFailure("unknown");
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <ConfigProvider
            theme={{ token: { colorPrimary: "#68469c", colorInfo: "#68469c" } }}
        >
            <main className="login-page">
                <section className="login-brand" aria-label="Inkwell">
                    <Typography.Title>Inkwell</Typography.Title>
                    <div className="login-brand-rings" aria-hidden="true" />
                </section>

                <section className="login-form-panel">
                    <div className="login-grid" aria-hidden="true" />
                    <div className="login-form-wrap">
                        <header className="login-heading">
                            <img
                                src="./logo.svg"
                                alt="Inkwell"
                                width="64"
                                height="64"
                            />
                            <Typography.Title level={4}>
                                {t("auth.platform")}
                            </Typography.Title>
                        </header>

                        {failure && failure !== "offline" && (
                            <Alert
                                className="login-alert"
                                type={
                                    failure === "rate-limited"
                                        ? "warning"
                                        : "error"
                                }
                                showIcon
                                message={t(loginErrorKeys[failure])}
                            />
                        )}

                        <Form<LoginRequest>
                            form={form}
                            layout="vertical"
                            size="large"
                            className="login-form"
                            initialValues={{ username: "", password: "" }}
                            onFinish={login}
                            requiredMark={false}
                        >
                            <Form.Item
                                name="username"
                                rules={[
                                    {
                                        required: true,
                                        message: t("auth.usernameRequired"),
                                    },
                                    {
                                        max: 64,
                                        message: t("auth.usernameTooLong"),
                                    },
                                ]}
                            >
                                <Input
                                    prefix={<UserOutlined />}
                                    placeholder={t("auth.usernamePlaceholder")}
                                    autoComplete="username"
                                    autoFocus
                                    disabled={fieldsDisabled}
                                />
                            </Form.Item>
                            <Form.Item
                                name="password"
                                rules={[
                                    {
                                        required: true,
                                        message: t("auth.passwordRequired"),
                                    },
                                ]}
                            >
                                <Input.Password
                                    ref={passwordInputRef}
                                    prefix={<LockOutlined />}
                                    placeholder={t("auth.passwordPlaceholder")}
                                    autoComplete="current-password"
                                    disabled={fieldsDisabled}
                                />
                            </Form.Item>
                            <Button
                                block
                                type="primary"
                                htmlType="submit"
                                loading={submitting}
                                disabled={submitDisabled}
                            >
                                {submitting
                                    ? t("auth.loggingIn")
                                    : t("auth.login")}
                            </Button>
                        </Form>

                        <Typography.Paragraph
                            className="login-help"
                            type="secondary"
                        >
                            {t("auth.help")}
                        </Typography.Paragraph>
                    </div>
                </section>
            </main>
        </ConfigProvider>
    );
}
