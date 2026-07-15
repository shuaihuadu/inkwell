import { useState } from "react";
import {
    Alert,
    Button,
    Form,
    Input,
    Select,
    Space,
    Switch,
    Tag,
    Typography,
    theme as antdTheme,
    Divider,
    Row,
    Col,
} from "antd";
import {
    LockOutlined,
    UserOutlined,
    GlobalOutlined,
    WarningOutlined,
} from "@ant-design/icons";
import { useDesign } from "../context/DesignContext";
import logo from "../../assets/logos/logo.svg?no-inline";

/** 严格按 ui-spec.md §1.2 / §1.5 定义 */
type LoginState =
    | "default"
    | "submitting"
    | "failed-401"
    | "failed-locked"
    | "failed-rate"
    | "offline";

const ERROR_MAP: Record<
    Exclude<LoginState, "default" | "submitting">,
    { msg: string; type: "error" | "warning" }
> = {
    "failed-401": { msg: "账号或密码错误，请重试", type: "error" },
    "failed-locked": { msg: "账号已被锁定，请联系系统管理员", type: "error" },
    "failed-rate": { msg: "登录过于频繁，请稍后重试", type: "warning" },
    offline: { msg: "网络异常，已断开。请检查网络连接", type: "warning" },
};

const STATE_OPTIONS: { value: LoginState; label: string }[] = [
    { value: "default", label: "默认" },
    { value: "submitting", label: "提交中" },
    { value: "failed-401", label: "账号或密码错误" },
    { value: "failed-locked", label: "账号已锁" },
    { value: "failed-rate", label: "速率超限" },
    { value: "offline", label: "离线" },
];

/** 登录表单 */
function LoginForm({
    state,
    showTopError = true,
}: {
    state: LoginState;
    showTopError?: boolean;
}) {
    const isError = state in ERROR_MAP;
    const isDisabled = state === "submitting" || state === "offline";
    const err = isError ? ERROR_MAP[state as keyof typeof ERROR_MAP] : null;

    return (
        <div>
            {/* App branding */}
            <div style={{ textAlign: "center", marginBottom: 24 }}>
                <img
                    src={logo}
                    alt="Inkwell"
                    width={64}
                    height={64}
                    style={{ display: "block", margin: "0 auto 14px" }}
                />
                <Typography.Title level={4} style={{ margin: "0 0 2px" }}>
                    Inkwell Agent 平台
                </Typography.Title>
            </div>

            {/* Global offline bar */}
            {state === "offline" && showTopError && (
                <Alert
                    type="warning"
                    showIcon
                    message="网络异常，已断开。请检查网络连接"
                    icon={<GlobalOutlined />}
                    style={{ marginBottom: 12 }}
                />
            )}

            {/* Form-level error (401 / locked / rate) */}
            {err && state !== "offline" && showTopError && (
                <Alert
                    type={err.type}
                    showIcon
                    message={err.msg}
                    style={{ marginBottom: 12 }}
                />
            )}

            {/* Form */}
            <Form
                layout="vertical"
                size="large"
                initialValues={{ username: "", password: "" }}
                style={{ display: "grid", gap: 16 }}
            >
                <Form.Item
                    name="username"
                    style={{ marginBottom: 0 }}
                    rules={[
                        { required: true, message: "请输入账号" },
                        { max: 64, message: "账号长度不超过 64" },
                    ]}
                >
                    <Input
                        prefix={<UserOutlined />}
                        placeholder="请输入账号"
                        autoFocus
                        disabled={isDisabled}
                    />
                </Form.Item>

                <Form.Item
                    name="password"
                    style={{ marginBottom: 0 }}
                    rules={[{ required: true, message: "请输入密码" }]}
                >
                    <Input.Password
                        prefix={<LockOutlined />}
                        placeholder="请输入密码"
                        disabled={isDisabled}
                    />
                </Form.Item>

                <Button
                    type="primary"
                    htmlType="submit"
                    block
                    loading={state === "submitting"}
                    disabled={state === "offline" || state === "failed-locked"}
                >
                    {state === "submitting" ? "登录中…" : "登录"}
                </Button>
            </Form>

            <Typography.Paragraph
                type="secondary"
                style={{
                    textAlign: "center",
                    fontSize: 12,
                    marginTop: 36,
                    marginBottom: 0,
                }}
            >
                如忘记密码或需要开通账号，请联系系统管理员
            </Typography.Paragraph>
        </div>
    );
}

function WorkstationLogin({
    state,
    narrow,
}: {
    state: LoginState;
    narrow: boolean;
}) {
    const { token } = antdTheme.useToken();
    const { isDark } = useDesign();

    return (
        <div
            style={{
                minHeight: 580,
                display: "flex",
                borderRadius: token.borderRadiusLG,
                border: `1px solid ${token.colorBorderSecondary}`,
                overflow: "hidden",
                flexDirection: narrow ? "column" : "row",
                position: "relative",
                background: isDark ? "#121114" : "#F6F5F8",
                boxShadow: `0 24px 70px ${token.colorPrimary}18`,
            }}
        >
            {/* Left brand panel */}
            <div
                style={{
                    flex: narrow ? "0 0 210px" : "0 0 clamp(260px, 44%, 430px)",
                    width: narrow ? "100%" : undefined,
                    background: isDark
                        ? `radial-gradient(circle at 18% 18%, ${token.colorPrimary}70 0%, transparent 38%),
                           radial-gradient(circle at 85% 78%, ${token.colorInfo}24 0%, transparent 42%),
                           linear-gradient(145deg, #1D1924 0%, #121114 100%)`
                        : `radial-gradient(circle at 18% 18%, ${token.colorPrimary}A8 0%, transparent 38%),
                           radial-gradient(circle at 85% 78%, ${token.colorInfo}2E 0%, transparent 42%),
                           linear-gradient(145deg, #3D285E 0%, #21172F 100%)`,
                    display: "flex",
                    flexDirection: "column",
                    justifyContent: "center",
                    alignItems: "flex-start",
                    padding: narrow ? "40px 32px" : "56px 48px",
                    minWidth: 0,
                    position: "relative",
                    overflow: "hidden",
                }}
            >
                <div
                    style={{
                        position: "absolute",
                        width: 300,
                        height: 300,
                        right: -110,
                        bottom: -100,
                        border: "1px solid rgba(255,255,255,0.12)",
                        borderRadius: "50%",
                        boxShadow:
                            "0 0 0 42px rgba(255,255,255,0.035), 0 0 0 86px rgba(255,255,255,0.018)",
                    }}
                />
                <div style={{ position: "relative", zIndex: 1 }}>
                    <Typography.Title
                        level={1}
                        style={{
                            color: "#fff",
                            margin: 0,
                            fontSize: narrow ? 48 : 62,
                            lineHeight: 1,
                            fontWeight: 600,
                            letterSpacing: 0,
                        }}
                    >
                        Inkwell
                    </Typography.Title>
                </div>
            </div>

            {/* Right form panel */}
            <div
                style={{
                    flex: 1,
                    background: token.colorBgContainer,
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    padding: narrow ? "40px 24px 44px" : "48px 44px",
                    minWidth: 0,
                    position: "relative",
                    overflow: "hidden",
                }}
            >
                <div
                    style={{
                        position: "absolute",
                        inset: 0,
                        opacity: isDark ? 0.13 : 0.32,
                        backgroundImage: `linear-gradient(${token.colorPrimary}12 1px, transparent 1px), linear-gradient(90deg, ${token.colorPrimary}12 1px, transparent 1px)`,
                        backgroundSize: "36px 36px",
                        maskImage:
                            "linear-gradient(135deg, transparent 10%, #000 55%, transparent 92%)",
                        WebkitMaskImage:
                            "linear-gradient(135deg, transparent 10%, #000 55%, transparent 92%)",
                        pointerEvents: "none",
                    }}
                />
                <div
                    style={{
                        position: "absolute",
                        width: narrow ? 240 : 380,
                        height: narrow ? 240 : 380,
                        right: narrow ? -150 : -210,
                        top: narrow ? -150 : -190,
                        border: `1px solid ${token.colorPrimary}18`,
                        borderRadius: "50%",
                        boxShadow: `0 0 0 38px ${token.colorPrimary}0B, 0 0 0 82px ${token.colorPrimary}07`,
                        pointerEvents: "none",
                    }}
                />
                <div
                    style={{
                        width: "100%",
                        maxWidth: 360,
                        position: "relative",
                        zIndex: 1,
                    }}
                >
                    <LoginForm state={state} />
                </div>
            </div>
        </div>
    );
}

export default function LoginExplorer() {
    const { token } = antdTheme.useToken();
    const [loginState, setLoginState] = useState<LoginState>("default");
    const [narrowMode, setNarrowMode] = useState(false);

    return (
        <div
            className="prototype-page"
            style={{
                maxWidth: 1200,
            }}
        >
            {/* Controls */}
            <div
                style={{
                    background: token.colorBgContainer,
                    border: `1px solid ${token.colorBorderSecondary}`,
                    borderRadius: token.borderRadiusLG,
                    padding: "16px 20px",
                    marginBottom: 24,
                    display: "flex",
                    gap: 24,
                    flexWrap: "wrap",
                    alignItems: "center",
                }}
            >
                <div>
                    <Typography.Text
                        type="secondary"
                        style={{
                            fontSize: 11,
                            display: "block",
                            marginBottom: 6,
                        }}
                    >
                        登录状态 (ui-spec §1.2)
                    </Typography.Text>
                    <Select
                        value={loginState}
                        onChange={setLoginState}
                        style={{ width: 160 }}
                        options={STATE_OPTIONS}
                    />
                </div>
                <div>
                    <Typography.Text
                        type="secondary"
                        style={{
                            fontSize: 11,
                            display: "block",
                            marginBottom: 6,
                        }}
                    >
                        窄窗口模拟
                    </Typography.Text>
                    <Switch
                        checked={narrowMode}
                        onChange={setNarrowMode}
                        checkedChildren="390px"
                        unCheckedChildren="桌面"
                    />
                </div>
                <div style={{ marginLeft: "auto" }}>
                    <Tag
                        icon={<WarningOutlined />}
                        style={{
                            fontSize: 11,
                            color: token.colorPrimaryText,
                            background: token.colorPrimaryBg,
                            borderColor: "transparent",
                        }}
                    >
                        所有字段来自 ui-spec.md §1 · 无自助注册/重置入口
                    </Tag>
                </div>
            </div>

            {/* Current variant description */}
            <div
                style={{
                    marginBottom: 16,
                    display: "flex",
                    gap: 12,
                    alignItems: "center",
                }}
            >
                <Typography.Title level={4} style={{ margin: 0 }}>
                    工作台分栏
                </Typography.Title>
                <Typography.Text type="secondary" style={{ fontSize: 13 }}>
                    沉浸式品牌背景 + 专注登录表单
                </Typography.Text>
                <Tag color={token.colorPrimary}>
                    状态:{" "}
                    {STATE_OPTIONS.find((o) => o.value === loginState)?.label}
                </Tag>
            </div>

            {/* Preview container */}
            <div
                style={{
                    margin: "0 auto",
                    width: narrowMode ? "min(390px, 100%)" : "100%",
                    transition: "width 0.3s ease",
                }}
            >
                <WorkstationLogin state={loginState} narrow={narrowMode} />
            </div>

            {/* Spec reminder */}
            <Divider />
            <Row gutter={16}>
                <Col xs={24} md={12}>
                    <Typography.Title level={5}>
                        字段清单（ui-spec §1.3 / §1.4）
                    </Typography.Title>
                    <Space direction="vertical" size={4}>
                        {[
                            "账号 Input — 必填，1–64 字符",
                            "密码 Input.Password — 必填，含显示/隐藏切换",
                            "登录按钮 — 提交中时 loading，离线/锁定时置灰",
                            '"如忘记密码…联系系统管理员" 提示文字',
                        ].map((f) => (
                            <Typography.Text key={f} style={{ fontSize: 12 }}>
                                ✓ {f}
                            </Typography.Text>
                        ))}
                    </Space>
                </Col>
                <Col xs={24} md={12}>
                    <Typography.Title level={5}>
                        状态覆盖（ui-spec §1.2 / §1.5）
                    </Typography.Title>
                    <Space direction="vertical" size={4}>
                        {[
                            "default — 账号框获焦，按钮可用",
                            "submitting — 按钮 loading，字段只读",
                            'failed-401 — 错误条"账号或密码错误，请重试"',
                            'failed-locked (423) — "账号已被锁定，请联系系统管理员"',
                            'failed-rate (429) — "登录过于频繁，请稍后重试"',
                            "offline — 全局 warning 条 + 按钮置灰",
                        ].map((s) => (
                            <Typography.Text key={s} style={{ fontSize: 12 }}>
                                ✓ {s}
                            </Typography.Text>
                        ))}
                    </Space>
                </Col>
            </Row>
        </div>
    );
}
