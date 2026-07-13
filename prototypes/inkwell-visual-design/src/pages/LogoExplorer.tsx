import {
    App,
    Card,
    Col,
    Divider,
    Row,
    Space,
    Tag,
    Tooltip,
    Typography,
    theme as antdTheme,
} from "antd";
import { CheckCircleOutlined, CopyOutlined } from "@ant-design/icons";
import inkwellMark from "../../assets/logos/inkwell-mark.svg?no-inline";

const INKWELL_MARK = inkwellMark;

const SIZES = [16, 24, 32, 64, 96];

function LogoAtSize({
    size,
    background,
}: {
    size: number;
    background: "light" | "dark" | "primary";
}) {
    const { token } = antdTheme.useToken();
    const backgrounds = {
        light: token.colorBgContainer,
        dark: "#171717",
        primary: token.colorPrimary,
    };

    return (
        <Tooltip title={`${size}px · ${background}`}>
            <div
                style={{
                    width: Math.max(size, 32) + 16,
                    height: Math.max(size, 32) + 16,
                    display: "grid",
                    placeItems: "center",
                    flexShrink: 0,
                    background: backgrounds[background],
                    border: `1px solid ${token.colorBorderSecondary}`,
                    borderRadius: 8,
                }}
            >
                <img
                    src={INKWELL_MARK}
                    alt={`Inkwell 品牌标记 ${size}px`}
                    width={size}
                    height={size}
                    style={{
                        display: "block",
                        filter:
                            background === "light"
                                ? "none"
                                : "brightness(0) invert(1)",
                    }}
                />
            </div>
        </Tooltip>
    );
}

function AppIcon() {
    const { token } = antdTheme.useToken();

    return (
        <div
            style={{
                width: 80,
                height: 80,
                display: "grid",
                placeItems: "center",
                flexShrink: 0,
                background: "#F6F5F8",
                border: `1px solid ${token.colorBorderSecondary}`,
                borderRadius: 18,
                boxShadow: `0 12px 28px ${token.colorPrimary}38`,
            }}
        >
            <img
                src={INKWELL_MARK}
                alt="Inkwell 应用图标"
                width={50}
                height={50}
                style={{ display: "block" }}
            />
        </div>
    );
}

export default function LogoExplorer() {
    const { message } = App.useApp();
    const { token } = antdTheme.useToken();

    const copyAssetName = async () => {
        await navigator.clipboard.writeText("assets/logos/inkwell-mark.svg");
        message.success("文件名已复制");
    };

    return (
        <div className="prototype-page" style={{ maxWidth: 1200 }}>
            <div style={{ marginBottom: 24 }}>
                <Space size={10} wrap>
                    <Typography.Title level={2} style={{ margin: 0 }}>
                        Inkwell 品牌标记
                    </Typography.Title>
                    <Tag color="success" icon={<CheckCircleOutlined />}>
                        保留方向
                    </Tag>
                </Space>
                <Typography.Paragraph
                    type="secondary"
                    style={{ margin: "8px 0 0", maxWidth: 720 }}
                >
                    以三层同心涟漪表达知识沉淀、扩散与持续影响。统一使用 public
                    目录中的正式标记，不再保留其他 Logo 探索方向。
                </Typography.Paragraph>
            </div>

            <Row gutter={[16, 16]}>
                <Col xs={24} lg={15}>
                    <Card title="尺寸与背景验证" style={{ height: "100%" }}>
                        <Typography.Text type="secondary">
                            浅色背景
                        </Typography.Text>
                        <div
                            style={{
                                display: "flex",
                                alignItems: "flex-end",
                                flexWrap: "wrap",
                                gap: 12,
                                marginTop: 12,
                            }}
                        >
                            {SIZES.map((size) => (
                                <div
                                    key={size}
                                    style={{
                                        display: "grid",
                                        justifyItems: "center",
                                        gap: 6,
                                    }}
                                >
                                    <LogoAtSize
                                        size={size}
                                        background="light"
                                    />
                                    <Typography.Text type="secondary">
                                        {size}px
                                    </Typography.Text>
                                </div>
                            ))}
                        </div>

                        <Divider />

                        <Typography.Text type="secondary">
                            深色与主题色背景（单色适配）
                        </Typography.Text>
                        <Space wrap size={12} style={{ marginTop: 12 }}>
                            <LogoAtSize size={32} background="dark" />
                            <LogoAtSize size={64} background="dark" />
                            <LogoAtSize size={32} background="primary" />
                            <LogoAtSize size={64} background="primary" />
                        </Space>
                    </Card>
                </Col>

                <Col xs={24} lg={9}>
                    <Card title="品牌组合" style={{ height: "100%" }}>
                        <Space size={16} align="center">
                            <AppIcon />
                            <div>
                                <Typography.Title
                                    level={3}
                                    style={{ margin: 0, letterSpacing: 0 }}
                                >
                                    Inkwell
                                </Typography.Title>
                                <Typography.Text type="secondary">
                                    Agent Platform
                                </Typography.Text>
                            </div>
                        </Space>

                        <Divider />

                        <Space direction="vertical" size={10}>
                            <div>
                                <Typography.Text strong>
                                    图形结构
                                </Typography.Text>
                                <Typography.Paragraph
                                    type="secondary"
                                    style={{ margin: "4px 0 0" }}
                                >
                                    两层描边圆环与中心实心圆形成由外向内的三级视觉层次。
                                </Typography.Paragraph>
                            </div>
                            <div>
                                <Typography.Text strong>
                                    小尺寸策略
                                </Typography.Text>
                                <Typography.Paragraph
                                    type="secondary"
                                    style={{ margin: "4px 0 0" }}
                                >
                                    16px
                                    保留中心圆与两层涟漪，轮廓无需额外简化。
                                </Typography.Paragraph>
                            </div>
                            <Tag
                                icon={<CopyOutlined />}
                                style={{
                                    cursor: "pointer",
                                    width: "fit-content",
                                }}
                                onClick={copyAssetName}
                            >
                                assets/logos/inkwell-mark.svg
                            </Tag>
                        </Space>
                    </Card>
                </Col>
            </Row>

            <Card
                style={{
                    marginTop: 16,
                    background: token.colorFillQuaternary,
                }}
            >
                <Space wrap size={8}>
                    <Tag>涟漪意象</Tag>
                    <Tag>同心结构</Tag>
                    <Tag>单色适配</Tag>
                    <Tag>正式标记</Tag>
                    <Tag>小尺寸稳定</Tag>
                </Space>
            </Card>
        </div>
    );
}
