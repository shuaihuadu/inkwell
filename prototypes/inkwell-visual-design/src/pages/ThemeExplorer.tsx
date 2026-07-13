import { Fragment, useState } from "react";
import {
    ConfigProvider,
    theme as antdTheme,
    Typography,
    Segmented,
    Switch,
    Space,
    Divider,
    Button,
    Input,
    Select,
    Tag,
    Alert,
    Table,
    Badge,
    Tooltip,
    Card,
    Row,
    Col,
    Progress,
    Slider,
    Rate,
} from "antd";
import {
    CheckOutlined,
    CloseOutlined,
    InfoCircleOutlined,
    ExclamationCircleOutlined,
    UserOutlined,
    RobotOutlined,
    SaveOutlined,
    EditOutlined,
} from "@ant-design/icons";
import { THEMES, THEME_NAMES, type ThemeName } from "../tokens/themes";
import inkwellMark from "../../assets/logos/inkwell-mark.svg?no-inline";

const INKWELL_MARK = inkwellMark;

const TABLE_DATA = [
    {
        key: "1",
        name: "GPT-4o-mini",
        status: "可用",
        calls: 1240,
        latency: 320,
    },
    { key: "2", name: "Claude-3.5", status: "可用", calls: 847, latency: 410 },
    { key: "3", name: "Gemini 1.5", status: "受限", calls: 203, latency: 880 },
];

const TABLE_COLS = [
    { title: "模型名称", dataIndex: "name", key: "name" },
    {
        title: "状态",
        dataIndex: "status",
        key: "status",
        render: (s: string) => (
            <Tag color={s === "可用" ? "success" : "warning"}>{s}</Tag>
        ),
    },
    { title: "调用次数", dataIndex: "calls", key: "calls" },
    { title: "平均延迟(ms)", dataIndex: "latency", key: "latency" },
];

function ComponentShowcase({ dark }: { dark: boolean }) {
    const { token } = antdTheme.useToken();
    const softTag = (background: string, color: string) => ({
        background,
        color,
        borderColor: "transparent",
    });
    const logoMask = {
        WebkitMaskImage: `url(${INKWELL_MARK})`,
        maskImage: `url(${INKWELL_MARK})`,
        WebkitMaskPosition: "center",
        maskPosition: "center",
        WebkitMaskRepeat: "no-repeat",
        maskRepeat: "no-repeat",
        WebkitMaskSize: "contain",
        maskSize: "contain",
    };

    return (
        <div
            style={{
                background: token.colorBgLayout,
                borderRadius: token.borderRadiusLG,
                padding: 20,
                display: "flex",
                flexDirection: "column",
                gap: 16,
            }}
        >
            <section>
                <Typography.Text
                    type="secondary"
                    style={{ fontSize: 11, display: "block", marginBottom: 8 }}
                >
                    Logo 色彩适配 — Inkwell 品牌标记
                </Typography.Text>
                <Space wrap size={12}>
                    <div
                        style={{
                            width: 72,
                            height: 72,
                            padding: 14,
                            borderRadius: 8,
                            background: token.colorBgContainer,
                            border: `1px solid ${token.colorBorder}`,
                        }}
                    >
                        <div
                            style={{
                                ...logoMask,
                                width: "100%",
                                height: "100%",
                                background: token.colorPrimary,
                            }}
                        />
                    </div>
                    <div
                        style={{
                            width: 72,
                            height: 72,
                            padding: 14,
                            borderRadius: 8,
                            background: token.colorPrimary,
                        }}
                    >
                        <div
                            style={{
                                ...logoMask,
                                width: "100%",
                                height: "100%",
                                background: "#FFFFFF",
                            }}
                        />
                    </div>
                    <div
                        style={{
                            width: 72,
                            height: 72,
                            padding: 14,
                            borderRadius: 8,
                            background: dark ? "#FFFFFF" : "#171717",
                        }}
                    >
                        <div
                            style={{
                                ...logoMask,
                                width: "100%",
                                height: "100%",
                                background: dark ? "#171717" : "#FFFFFF",
                            }}
                        />
                    </div>
                </Space>
            </section>

            <Divider style={{ margin: "4px 0" }} />

            {/* Buttons */}
            <section>
                <Typography.Text
                    type="secondary"
                    style={{ fontSize: 11, display: "block", marginBottom: 8 }}
                >
                    按钮 — Button
                </Typography.Text>
                <Space wrap>
                    <Button type="primary">主要操作</Button>
                    <Button>默认</Button>
                    <Button type="dashed">虚线</Button>
                    <Button type="primary" ghost>
                        幽灵
                    </Button>
                    <Button danger>危险</Button>
                    <Button type="link">链接</Button>
                    <Button type="primary" loading>
                        加载中
                    </Button>
                    <Button type="primary" icon={<SaveOutlined />}>
                        保存
                    </Button>
                </Space>
            </section>

            <Divider style={{ margin: "4px 0" }} />

            {/* Inputs */}
            <section>
                <Typography.Text
                    type="secondary"
                    style={{ fontSize: 11, display: "block", marginBottom: 8 }}
                >
                    输入 — Input / Select
                </Typography.Text>
                <Row gutter={[12, 8]}>
                    <Col xs={24} sm={12}>
                        <Input
                            prefix={<UserOutlined />}
                            placeholder="账号输入框"
                        />
                    </Col>
                    <Col xs={24} sm={12}>
                        <Select
                            style={{ width: "100%" }}
                            placeholder="选择模型"
                            options={[
                                { value: "gpt4o", label: "GPT-4o" },
                                { value: "claude", label: "Claude 3.5" },
                            ]}
                        />
                    </Col>
                    <Col xs={24} sm={12}>
                        <Input.Search placeholder="搜索 Agent" />
                    </Col>
                    <Col xs={24} sm={12}>
                        <Input.Password placeholder="密码" />
                    </Col>
                </Row>
            </section>

            <Divider style={{ margin: "4px 0" }} />

            {/* Tags & Badges */}
            <section>
                <Typography.Text
                    type="secondary"
                    style={{ fontSize: 11, display: "block", marginBottom: 8 }}
                >
                    标签 / 徽标 — Tag / Badge
                </Typography.Text>
                <Space wrap>
                    <Tag
                        style={softTag(
                            token.colorFillSecondary,
                            token.colorTextSecondary,
                        )}
                    >
                        默认
                    </Tag>
                    <Tag
                        style={softTag(
                            token.colorPrimaryBg,
                            token.colorPrimaryText,
                        )}
                    >
                        处理中
                    </Tag>
                    <Tag
                        style={softTag(
                            token.colorSuccessBg,
                            token.colorSuccessText,
                        )}
                    >
                        成功
                    </Tag>
                    <Tag
                        style={softTag(
                            token.colorWarningBg,
                            token.colorWarningText,
                        )}
                    >
                        警告
                    </Tag>
                    <Tag
                        style={softTag(
                            token.colorErrorBg,
                            token.colorErrorText,
                        )}
                    >
                        错误
                    </Tag>
                    <Tag
                        icon={<CheckOutlined />}
                        style={softTag(
                            token.colorSuccessBg,
                            token.colorSuccessText,
                        )}
                    >
                        已保存
                    </Tag>
                    <Tag
                        icon={<CloseOutlined />}
                        style={softTag(
                            token.colorErrorBg,
                            token.colorErrorText,
                        )}
                    >
                        失败
                    </Tag>
                    <Badge count={5} color={token.colorPrimary}>
                        <Button>通知</Button>
                    </Badge>
                    <Badge dot>
                        <RobotOutlined style={{ fontSize: 18 }} />
                    </Badge>
                </Space>
            </section>

            <Divider style={{ margin: "4px 0" }} />

            {/* Alerts */}
            <section>
                <Typography.Text
                    type="secondary"
                    style={{ fontSize: 11, display: "block", marginBottom: 8 }}
                >
                    提示 — Alert
                </Typography.Text>
                <Space direction="vertical" style={{ width: "100%" }} size={8}>
                    <Alert
                        message="账号或密码错误，请重试"
                        type="error"
                        showIcon
                        closable
                    />
                    <Alert
                        message="登录过于频繁，请稍后重试"
                        type="warning"
                        showIcon
                        description="请等待 60 秒后重试"
                        icon={<ExclamationCircleOutlined />}
                    />
                    <Alert
                        message="已保存为 v3，操作成功"
                        type="success"
                        showIcon
                    />
                    <Alert
                        message="该 Agent 已被管理员撤销共享"
                        type="info"
                        showIcon
                        icon={<InfoCircleOutlined />}
                    />
                </Space>
            </section>

            <Divider style={{ margin: "4px 0" }} />

            {/* Table */}
            <section>
                <Typography.Text
                    type="secondary"
                    style={{ fontSize: 11, display: "block", marginBottom: 8 }}
                >
                    表格 — Table
                </Typography.Text>
                <Table
                    dataSource={TABLE_DATA}
                    columns={TABLE_COLS}
                    size="small"
                    pagination={false}
                />
            </section>

            <Divider style={{ margin: "4px 0" }} />

            {/* Progress & Slider */}
            <section>
                <Typography.Text
                    type="secondary"
                    style={{ fontSize: 11, display: "block", marginBottom: 8 }}
                >
                    进度 / 滑块 — Progress / Slider
                </Typography.Text>
                <Space direction="vertical" style={{ width: "100%" }}>
                    <Progress percent={72} status="active" />
                    <Progress percent={100} />
                    <Progress percent={35} status="exception" />
                    <Slider
                        defaultValue={0.7}
                        min={0}
                        max={1}
                        step={0.01}
                        tooltip={{ formatter: (v) => `temperature: ${v}` }}
                    />
                </Space>
            </section>

            {/* Color tokens display */}
            <Divider style={{ margin: "4px 0" }} />
            <section>
                <Typography.Text
                    type="secondary"
                    style={{ fontSize: 11, display: "block", marginBottom: 8 }}
                >
                    语义色 Token — Semantic Colors
                </Typography.Text>
                <Space wrap>
                    {[
                        { label: "Primary", color: token.colorPrimary },
                        { label: "Info", color: token.colorInfo },
                        { label: "Success", color: token.colorSuccess },
                        { label: "Warning", color: token.colorWarning },
                        { label: "Error", color: token.colorError },
                        { label: "Text", color: token.colorText },
                        { label: "Secondary", color: token.colorTextSecondary },
                        { label: "Border", color: token.colorBorder },
                        { label: "BgContainer", color: token.colorBgContainer },
                        { label: "BgLayout", color: token.colorBgLayout },
                    ].map(({ label, color }) => (
                        <Tooltip key={label} title={color}>
                            <div
                                style={{
                                    display: "flex",
                                    flexDirection: "column",
                                    alignItems: "center",
                                    gap: 4,
                                }}
                            >
                                <div
                                    style={{
                                        width: 36,
                                        height: 36,
                                        borderRadius: 8,
                                        background: color,
                                        border: `1px solid ${token.colorBorder}`,
                                    }}
                                />
                                <Typography.Text
                                    style={{
                                        fontSize: 9,
                                        whiteSpace: "nowrap",
                                    }}
                                >
                                    {label}
                                </Typography.Text>
                            </div>
                        </Tooltip>
                    ))}
                </Space>
            </section>

            {/* Rating for fun */}
            <Divider style={{ margin: "4px 0" }} />
            <section style={{ display: "flex", alignItems: "center", gap: 16 }}>
                <Typography.Text type="secondary" style={{ fontSize: 11 }}>
                    评星示例：
                </Typography.Text>
                <Rate defaultValue={4} />
                <EditOutlined style={{ color: token.colorPrimary }} />
            </section>

            <div
                style={{
                    fontSize: 10,
                    color: token.colorTextQuaternary,
                    textAlign: "right",
                }}
            >
                dark={String(dark)} · borderRadius={token.borderRadius} ·
                primaryColor={token.colorPrimary}
            </div>
        </div>
    );
}

function ThemePanel({
    name,
    mode,
}: {
    name: ThemeName;
    mode: "light" | "dark";
}) {
    const def = THEMES[name];
    const tok = mode === "light" ? def.light : def.dark;
    const isDark = mode === "dark";

    return (
        <ConfigProvider
            theme={{
                algorithm: isDark
                    ? antdTheme.darkAlgorithm
                    : antdTheme.defaultAlgorithm,
                token: tok,
            }}
        >
            <Card
                size="small"
                style={{
                    borderRadius: 12,
                    overflow: "hidden",
                    background: isDark
                        ? (def.dark?.colorBgLayout as string)
                        : (def.light?.colorBgLayout as string),
                }}
                styles={{ body: { padding: 16 } }}
            >
                <div
                    style={{
                        display: "flex",
                        justifyContent: "space-between",
                        alignItems: "flex-start",
                        marginBottom: 12,
                    }}
                >
                    <div>
                        <Typography.Title level={5} style={{ margin: 0 }}>
                            {def.label}
                            <span
                                style={{
                                    fontSize: 11,
                                    fontWeight: 400,
                                    marginLeft: 8,
                                    opacity: 0.6,
                                }}
                            >
                                {mode === "light" ? "亮色" : "暗色"}
                            </span>
                        </Typography.Title>
                        <Typography.Text
                            type="secondary"
                            style={{ fontSize: 11 }}
                        >
                            {def.tagline}
                        </Typography.Text>
                    </div>
                    <div
                        style={{
                            width: 20,
                            height: 20,
                            borderRadius: "50%",
                            background:
                                mode === "light"
                                    ? (def.light?.colorPrimary as string)
                                    : (def.dark?.colorPrimary as string),
                        }}
                    />
                </div>
                <ComponentShowcase dark={isDark} />
            </Card>
        </ConfigProvider>
    );
}

export default function ThemeExplorer() {
    const [activeTheme, setActiveTheme] = useState<ThemeName>("amethyst");
    const [viewMode, setViewMode] = useState<"single" | "compare">("single");
    const [singleMode, setSingleMode] = useState<"light" | "dark">("light");

    return (
        <div
            className="prototype-page"
            style={{
                maxWidth: 1600,
            }}
        >
            {/* Header */}
            <div
                style={{
                    marginBottom: 24,
                    display: "flex",
                    alignItems: "center",
                    gap: 16,
                    flexWrap: "wrap",
                }}
            >
                <Typography.Title level={3} style={{ margin: 0 }}>
                    主题色系
                </Typography.Title>
                <Segmented
                    value={viewMode}
                    onChange={(v) => setViewMode(v as "single" | "compare")}
                    options={[
                        { value: "single", label: "单主题详览" },
                        { value: "compare", label: "三主题对比" },
                    ]}
                />
                {viewMode === "single" && (
                    <>
                        <Segmented
                            value={activeTheme}
                            onChange={(v) => setActiveTheme(v as ThemeName)}
                            options={THEME_NAMES.map((n) => ({
                                value: n,
                                label: (
                                    <Space size={4}>
                                        <span
                                            style={{
                                                display: "inline-block",
                                                width: 8,
                                                height: 8,
                                                borderRadius: "50%",
                                                background:
                                                    THEMES[n].primaryColor,
                                            }}
                                        />
                                        {THEMES[n].label}
                                    </Space>
                                ),
                            }))}
                        />
                        <Switch
                            checked={singleMode === "dark"}
                            onChange={(v) =>
                                setSingleMode(v ? "dark" : "light")
                            }
                            checkedChildren="暗"
                            unCheckedChildren="亮"
                        />
                    </>
                )}
            </div>

            {viewMode === "single" ? (
                <ThemePanel name={activeTheme} mode={singleMode} />
            ) : (
                <Row gutter={[16, 16]}>
                    {THEME_NAMES.map((n) => (
                        <Fragment key={n}>
                            <Col xs={24} xl={12}>
                                <ThemePanel name={n} mode="light" />
                            </Col>
                            <Col xs={24} xl={12}>
                                <ThemePanel name={n} mode="dark" />
                            </Col>
                        </Fragment>
                    ))}
                </Row>
            )}
        </div>
    );
}
