import { GlobalOutlined, LockOutlined, UserOutlined } from '@ant-design/icons'
import { Alert, Button, ConfigProvider, Form, Input, Typography } from 'antd'
import type { InputRef } from 'antd'
import { useEffect, useRef, useState } from 'react'
import { desktopApi } from '../../shared/network/desktop-api'
import type { LoginFailureCode, LoginRequest } from '../../shared/network/contracts'
import { useAuthStore } from './auth-store'

const loginErrors: Record<LoginFailureCode, string> = {
    'invalid-credentials': '账号或密码错误，请重试',
    'account-locked': '账号已被锁定，请联系系统管理员',
    'rate-limited': '登录过于频繁，请稍后重试',
    offline: '网络异常，已断开。请检查网络连接',
    unknown: '登录失败，请稍后重试',
}

interface LoginPageProps {
    initiallyOffline?: boolean
}

export function LoginPage({ initiallyOffline = false }: LoginPageProps) {
    const [form] = Form.useForm<LoginRequest>()
    const passwordInputRef = useRef<InputRef>(null)
    const setSnapshot = useAuthStore((state) => state.setSnapshot)
    const [failure, setFailure] = useState<LoginFailureCode | null>(initiallyOffline ? 'offline' : null)
    const [submitting, setSubmitting] = useState(false)
    const fieldsDisabled = submitting || failure === 'offline'
    const submitDisabled = fieldsDisabled || failure === 'account-locked'

    useEffect(() => {
        if (failure === 'invalid-credentials') passwordInputRef.current?.focus()
    }, [failure])

    const login = async (values: LoginRequest): Promise<void> => {
        setSubmitting(true)
        setFailure(null)

        try {
            const result = await desktopApi.login(values)
            if (result.ok) {
                setSnapshot({ status: 'authenticated', identity: result.identity })
            } else {
                setFailure(result.code)
                if (result.code === 'invalid-credentials') {
                    form.setFieldValue('password', '')
                }
                if (result.code === 'offline') setSnapshot({ status: 'offline', identity: null })
            }
        } catch {
            setFailure('unknown')
        } finally {
            setSubmitting(false)
        }
    }

    return (
        <ConfigProvider theme={{ token: { colorPrimary: '#68469c', colorInfo: '#68469c' } }}>
            <main className="login-page">
            <section className="login-brand" aria-label="Inkwell">
                <Typography.Title>Inkwell</Typography.Title>
                <div className="login-brand-rings" aria-hidden="true" />
            </section>

            <section className="login-form-panel">
                <div className="login-grid" aria-hidden="true" />
                <div className="login-form-wrap">
                    <header className="login-heading">
                        <img src="./logo.svg" alt="Inkwell" width="64" height="64" />
                        <Typography.Title level={4}>Inkwell Agent 平台</Typography.Title>
                    </header>

                    {failure && (
                        <Alert
                            className="login-alert"
                            type={failure === 'rate-limited' || failure === 'offline' ? 'warning' : 'error'}
                            showIcon
                            icon={failure === 'offline' ? <GlobalOutlined /> : undefined}
                            message={loginErrors[failure]}
                        />
                    )}

                    <Form<LoginRequest>
                        form={form}
                        layout="vertical"
                        size="large"
                        className="login-form"
                        initialValues={{ username: '', password: '' }}
                        onFinish={login}
                        requiredMark={false}
                    >
                        <Form.Item
                            name="username"
                            rules={[
                                { required: true, message: '请输入账号' },
                                { max: 64, message: '账号长度不超过 64' },
                            ]}
                        >
                            <Input
                                prefix={<UserOutlined />}
                                placeholder="请输入账号"
                                autoComplete="username"
                                autoFocus
                                disabled={fieldsDisabled}
                            />
                        </Form.Item>
                        <Form.Item name="password" rules={[{ required: true, message: '请输入密码' }]}>
                            <Input.Password
                                ref={passwordInputRef}
                                prefix={<LockOutlined />}
                                placeholder="请输入密码"
                                autoComplete="current-password"
                                disabled={fieldsDisabled}
                            />
                        </Form.Item>
                        <Button block type="primary" htmlType="submit" loading={submitting} disabled={submitDisabled}>
                            {submitting ? '登录中…' : '登录'}
                        </Button>
                    </Form>

                    <Typography.Paragraph className="login-help" type="secondary">
                        如忘记密码或需要开通账号，请联系系统管理员
                    </Typography.Paragraph>
                </div>
            </section>
            </main>
        </ConfigProvider>
    )
}
