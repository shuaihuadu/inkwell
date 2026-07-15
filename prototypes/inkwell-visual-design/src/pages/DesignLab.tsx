import { useNavigate } from "react-router-dom";
import {
    Card,
    Col,
    Row,
    Typography,
    Button,
    Space,
    theme as antdTheme,
    Tag,
    Badge,
} from "antd";
import {
    BgColorsOutlined,
    PictureOutlined,
    LoginOutlined,
    LayoutOutlined,
    ArrowRightOutlined,
} from "@ant-design/icons";

const LAB_CARDS = [
    {
        path: "/themes",
        icon: <BgColorsOutlined style={{ fontSize: 32 }} />,
        title: "主题色系",
        description:
            "探索 3 套完整主题方向，每套含 light / dark 两组 Token，实时组件预览。",
        meta: ["3 套主题", "Light + Dark", "Token 可视化"],
        badge: "3",
        color: "#0D9488",
    },
    {
        path: "/logos",
        icon: <PictureOutlined style={{ fontSize: 32 }} />,
        title: "品牌标记",
        description: "Inkwell 涟漪标记，覆盖多尺寸展示与应用场景模拟。",
        meta: ["正式标记", "16 ~ 96px", "浅底 + 深底"],
        badge: "1",
        color: "#C2410C",
    },
    {
        path: "/login",
        icon: <LoginOutlined style={{ fontSize: 32 }} />,
        title: "登录页方案",
        description:
            "沉浸式工作台分栏，严格复用 UI-001 全部字段与状态，含桌面/窄窗口验证。",
        meta: ["工作台分栏", "6 个状态", "UI-001 对齐"],
        badge: "1",
        color: "#1D4ED8",
    },
    {
        path: "/shell",
        icon: <LayoutOutlined style={{ fontSize: 32 }} />,
        title: "AppShell 外壳",
        description:
            "顶栏 + 左侧 nav + 主区，左侧为两级可展开分组（工作区 / 资源中心 / 系统管理）；点击 Agent 空间卡片可钻入完整的 UI-004 设计页或直达对话页，同屏看到壳 + 页面的整体效果。后续新页面也会接入这里，不再新开顶导。",
        meta: ["OQ-011 对齐", "可折叠 Sider", "内嵌 UI-004"],
        badge: "2",
        color: "#4338CA",
    },
];

export default function DesignLab() {
    const navigate = useNavigate();
    const { token } = antdTheme.useToken();

    return (
        <div
            className="prototype-page"
            style={{
                maxWidth: 1200,
            }}
        >
            {/* Hero */}
            <div style={{ textAlign: "center", marginBottom: 48 }}>
                <Typography.Title
                    level={1}
                    style={{
                        fontSize: 36,
                        marginTop: 0,
                        marginBottom: 8,
                        letterSpacing: -0.5,
                    }}
                >
                    Inkwell Visual Design Lab
                </Typography.Title>
                <Typography.Paragraph
                    style={{
                        fontSize: 16,
                        color: token.colorTextSecondary,
                        maxWidth: 520,
                        margin: "0 auto 24px",
                    }}
                >
                    H1
                    视觉设计评审平台。选择下方任意产物类型进入评审，右上角可即时切换主题和亮暗模式。
                </Typography.Paragraph>
                <Space>
                    <Tag color={token.colorPrimary}>antd 6.5.1</Tag>
                    <Tag>React 19</Tag>
                    <Tag>Vite 8</Tag>
                    <Tag>H1 Prototype</Tag>
                </Space>
            </div>

            {/* Cards Grid */}
            <Row gutter={[24, 24]}>
                {LAB_CARDS.map((card) => (
                    <Col key={card.path} xs={24} sm={24} md={12} xl={12}>
                        <Card
                            hoverable
                            style={{
                                height: "100%",
                                borderRadius: token.borderRadiusLG,
                                cursor: "pointer",
                                transition: "transform 0.18s, box-shadow 0.18s",
                            }}
                            styles={{
                                body: { padding: 28 },
                            }}
                            onClick={() => navigate(card.path)}
                        >
                            <div
                                style={{
                                    display: "flex",
                                    gap: 20,
                                    alignItems: "flex-start",
                                }}
                            >
                                {/* Icon Block */}
                                <div
                                    style={{
                                        width: 64,
                                        height: 64,
                                        borderRadius: 14,
                                        display: "flex",
                                        alignItems: "center",
                                        justifyContent: "center",
                                        background: `${card.color}18`,
                                        color: card.color,
                                        flexShrink: 0,
                                    }}
                                >
                                    {card.icon}
                                </div>

                                <div style={{ flex: 1, minWidth: 0 }}>
                                    <div
                                        style={{
                                            display: "flex",
                                            alignItems: "center",
                                            gap: 8,
                                            marginBottom: 6,
                                        }}
                                    >
                                        <Typography.Title
                                            level={4}
                                            style={{ margin: 0, fontSize: 17 }}
                                        >
                                            {card.title}
                                        </Typography.Title>
                                        <Badge
                                            count={card.badge}
                                            style={{
                                                background: card.color,
                                                fontSize: 11,
                                            }}
                                        />
                                    </div>
                                    <Typography.Paragraph
                                        style={{
                                            color: token.colorTextSecondary,
                                            marginBottom: 14,
                                            lineHeight: 1.6,
                                            fontSize: 13,
                                        }}
                                    >
                                        {card.description}
                                    </Typography.Paragraph>
                                    <Space wrap size={6}>
                                        {card.meta.map((m) => (
                                            <Tag
                                                key={m}
                                                style={{
                                                    borderColor: `${card.color}40`,
                                                    color: card.color,
                                                    background: `${card.color}0c`,
                                                    fontSize: 11,
                                                }}
                                            >
                                                {m}
                                            </Tag>
                                        ))}
                                    </Space>
                                </div>
                            </div>

                            <div style={{ marginTop: 18, textAlign: "right" }}>
                                <Button
                                    type="primary"
                                    size="small"
                                    icon={<ArrowRightOutlined />}
                                    style={{
                                        background: token.colorPrimary,
                                        borderColor: token.colorPrimary,
                                    }}
                                    onClick={(e) => {
                                        e.stopPropagation();
                                        navigate(card.path);
                                    }}
                                >
                                    进入
                                </Button>
                            </div>
                        </Card>
                    </Col>
                ))}
            </Row>

            {/* Footer Note */}
            <div
                style={{
                    textAlign: "center",
                    marginTop: 48,
                    color: token.colorTextQuaternary,
                    fontSize: 12,
                }}
            >
                此原型仅用于 H1 视觉评审，不进入产品代码。所有字段与状态严格依据
                ui-spec.md reviewed 版本。
            </div>
        </div>
    );
}
