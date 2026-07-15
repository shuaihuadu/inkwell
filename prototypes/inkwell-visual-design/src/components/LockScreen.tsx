import { useRef, useState } from "react";
import {
    Alert,
    Avatar,
    Button,
    Form,
    Input,
    Space,
    Typography,
    theme as antdTheme,
    type InputRef,
} from "antd";
import { GlobalOutlined, LockOutlined, UserOutlined } from "@ant-design/icons";

// ─── UI-002 · 锁定页（ui-spec.md §2） ──────────────────────────────────────────
// 客户端累计 5 分钟无操作或主窗口失焦后强制跳入的全屏遮罩，解锁前禁止任何对后端的写操作
// （NFR-003 / EX-006）。本组件覆盖 ui-spec.md §2.2 定义的 4 种状态：
// 默认 / 解锁中 / 解锁失败 / 离线；"多次解锁失败"（AC-080）额外做了失败计数——连续错 3 次
// 密码后账号临时锁定，跟登录页（LoginExplorer）"账号已锁"状态用的是同一句文案，解锁表单
// 直接禁用，只能走"登出"联系管理员解封（对齐 UF-012）。
// 密码校验是本原型自己模拟的：随便输入非空密码都会成功解锁，输入"wrong"专门用来演示
// "解锁失败"这条路径，方便评审时不用真的记一个"正确密码"。

interface LockScreenProps {
    /** 当前登录用户名，展示在头像下方 */
    username: string;
    /** 由外层"设计评审"控制条的后台连通性模拟驱动（network !== "online" 时为 true），
     * 对应 ui-spec.md §2.2 的"离线"状态（EX-001） */
    offline?: boolean;
    /** 解锁成功 */
    onUnlock: () => void;
    /** 点击"切换账号"（终止当前 session，回登录页） */
    onSwitchAccount: () => void;
    /** 点击"登出"（终止当前 session，回登录页，不保留草稿） */
    onLogout: () => void;
}

interface UnlockFormValues {
    password: string;
}

const MAX_ATTEMPTS_BEFORE_LOCK = 3;

export function LockScreen({
    username,
    offline = false,
    onUnlock,
    onSwitchAccount,
    onLogout,
}: LockScreenProps) {
    const { token } = antdTheme.useToken();
    const [form] = Form.useForm<UnlockFormValues>();
    const [status, setStatus] = useState<"default" | "unlocking" | "failed">(
        "default",
    );
    const [failCount, setFailCount] = useState(0);
    const inputRef = useRef<InputRef | null>(null);

    const accountLocked = failCount >= MAX_ATTEMPTS_BEFORE_LOCK;

    const handleUnlock = ({ password }: UnlockFormValues) => {
        if (offline || accountLocked) return;
        setStatus("unlocking");
        window.setTimeout(() => {
            if (password.trim().toLowerCase() === "wrong") {
                setFailCount((count) => count + 1);
                setStatus("failed");
                form.resetFields();
                inputRef.current?.focus();
            } else {
                onUnlock();
            }
        }, 700);
    };

    return (
        <div
            style={{
                position: "absolute",
                inset: 0,
                zIndex: 1000,
                display: "grid",
                placeItems: "center",
                background: token.colorBgLayout,
            }}
        >
            <div
                style={{
                    display: "flex",
                    flexDirection: "column",
                    alignItems: "center",
                    gap: 16,
                    width: 360,
                    padding: "40px 36px",
                    borderRadius: token.borderRadiusLG,
                    background: token.colorBgContainer,
                    boxShadow: token.boxShadowSecondary,
                }}
            >
                <Avatar
                    size={64}
                    icon={<UserOutlined />}
                    style={{ background: token.colorPrimary }}
                />
                <div style={{ textAlign: "center" }}>
                    <Typography.Title level={4} style={{ margin: 0 }}>
                        Inkwell 已锁定
                    </Typography.Title>
                    <Typography.Text type="secondary">
                        {username}，请输入密码继续
                    </Typography.Text>
                </div>

                {offline ? (
                    <Alert
                        type="warning"
                        showIcon
                        icon={<GlobalOutlined />}
                        title="网络异常，已断开。请检查网络连接"
                        style={{ width: "100%" }}
                    />
                ) : accountLocked ? (
                    <Alert
                        type="error"
                        showIcon
                        title="多次解锁失败，账号已临时锁定。请联系管理员"
                        style={{ width: "100%" }}
                    />
                ) : status === "failed" ? (
                    <Alert
                        type="error"
                        showIcon
                        title="密码错误，请重试"
                        style={{ width: "100%" }}
                    />
                ) : null}

                <Form<UnlockFormValues>
                    form={form}
                    layout="vertical"
                    onFinish={handleUnlock}
                    requiredMark={false}
                    style={{ width: "100%" }}
                >
                    <Form.Item
                        name="password"
                        style={{ marginBottom: 12 }}
                        rules={[{ required: true, message: "请输入密码" }]}
                    >
                        <Input.Password
                            ref={inputRef}
                            autoFocus
                            size="large"
                            prefix={<LockOutlined />}
                            placeholder="密码"
                            autoComplete="current-password"
                            disabled={
                                offline ||
                                accountLocked ||
                                status === "unlocking"
                            }
                        />
                    </Form.Item>
                    <Button
                        block
                        size="large"
                        type="primary"
                        htmlType="submit"
                        loading={status === "unlocking"}
                        disabled={offline || accountLocked}
                    >
                        解锁
                    </Button>
                </Form>

                <Space size={16}>
                    <Button type="link" size="small" onClick={onSwitchAccount}>
                        切换账号
                    </Button>
                    <Button type="link" size="small" danger onClick={onLogout}>
                        登出
                    </Button>
                </Space>
            </div>
        </div>
    );
}
