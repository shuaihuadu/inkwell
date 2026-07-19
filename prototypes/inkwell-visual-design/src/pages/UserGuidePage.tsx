import {
    ArrowRightOutlined,
    CheckCircleOutlined,
    CopyOutlined,
    EditOutlined,
    MessageOutlined,
    RocketOutlined,
    SearchOutlined,
    ShareAltOutlined,
} from "@ant-design/icons";
import {
    Alert,
    Button,
    Divider,
    Empty,
    Input,
    Space,
    Steps,
    Tag,
    Typography,
    theme as antdTheme,
} from "antd";
import { useDeferredValue, useState } from "react";

export type GuideSection =
    | "quick-start"
    | "create"
    | "publish"
    | "share"
    | "faq";

const GUIDE_SECTIONS: Array<{
    key: GuideSection;
    label: string;
    description: string;
}> = [
    {
        key: "quick-start",
        label: "快速开始",
        description: "从创建到共享，完成第一个 Agent。",
    },
    {
        key: "create",
        label: "创建与配置",
        description: "设置基础信息、Instructions、模型、Tools 和 Skills。",
    },
    {
        key: "publish",
        label: "保存与发布",
        description: "理解草稿、已发布版本和未发布修改。",
    },
    {
        key: "share",
        label: "共享与复制",
        description: "把已发布 Agent 交给团队使用，或复制独立副本。",
    },
    {
        key: "faq",
        label: "常见问题",
        description: "快速处理版本、共享和权限相关疑问。",
    },
];

export default function UserGuidePage({
    onStartQuickGuide,
    onGoToAgentSpace,
    initialSection = "quick-start",
}: {
    onStartQuickGuide: () => void;
    onGoToAgentSpace: () => void;
    initialSection?: GuideSection;
}) {
    const { token } = antdTheme.useToken();
    const [activeSection, setActiveSection] =
        useState<GuideSection>(initialSection);
    const [searchText, setSearchText] = useState("");
    const deferredSearch = useDeferredValue(searchText.trim().toLowerCase());
    const visibleSections = GUIDE_SECTIONS.filter((section) =>
        `${section.label} ${section.description}`
            .toLowerCase()
            .includes(deferredSearch),
    );

    return (
        <main
            style={{
                height: "100%",
                minHeight: 0,
                overflow: "hidden",
                background: token.colorBgContainer,
                display: "grid",
                gridTemplateColumns: "200px minmax(0, 1fr)",
            }}
        >
            <aside
                style={{
                    borderRight: `1px solid ${token.colorBorderSecondary}`,
                    background: token.colorFillQuaternary,
                    overflow: "hidden",
                    display: "flex",
                    flexDirection: "column",
                }}
            >
                <div
                    style={{
                        minHeight: 48,
                        padding: "0 12px",
                        display: "flex",
                        alignItems: "center",
                        borderBottom: `1px solid ${token.colorBorderSecondary}`,
                    }}
                >
                    <Typography.Text strong style={{ fontSize: 13 }}>
                        使用指南
                    </Typography.Text>
                </div>
                <div style={{ padding: "10px 8px 0" }}>
                    <Input
                        allowClear
                        prefix={<SearchOutlined />}
                        placeholder="搜索指南"
                        value={searchText}
                        onChange={(event) => setSearchText(event.target.value)}
                    />
                </div>
                <nav
                    aria-label="使用指南章节"
                    style={{ flex: 1, overflow: "auto", padding: "10px 8px" }}
                >
                    {visibleSections.length ? (
                        visibleSections.map((section) => {
                            const active = section.key === activeSection;
                            return (
                                <button
                                    key={section.key}
                                    type="button"
                                    onClick={() =>
                                        setActiveSection(section.key)
                                    }
                                    style={{
                                        width: "100%",
                                        border: 0,
                                        borderRadius: 6,
                                        padding: "9px 10px",
                                        marginBottom: 3,
                                        textAlign: "left",
                                        cursor: "pointer",
                                        fontFamily: "inherit",
                                        fontSize: 13,
                                        color: active
                                            ? token.colorPrimary
                                            : token.colorText,
                                        background: active
                                            ? token.colorPrimaryBg
                                            : "transparent",
                                        fontWeight: active ? 600 : 400,
                                        transition:
                                            "background-color 0.15s, color 0.15s",
                                    }}
                                    aria-current={active ? "page" : undefined}
                                >
                                    {section.label}
                                </button>
                            );
                        })
                    ) : (
                        <Empty
                            image={Empty.PRESENTED_IMAGE_SIMPLE}
                            description="没有匹配的指南"
                        />
                    )}
                </nav>
            </aside>

            <section
                style={{
                    padding: "26px 36px 40px",
                    maxWidth: 920,
                    width: "100%",
                    overflow: "auto",
                }}
            >
                <GuideContent
                    section={activeSection}
                    onStartQuickGuide={onStartQuickGuide}
                    onGoToAgentSpace={onGoToAgentSpace}
                />
            </section>
        </main>
    );
}

function GuideContent({
    section,
    onStartQuickGuide,
    onGoToAgentSpace,
}: {
    section: GuideSection;
    onStartQuickGuide: () => void;
    onGoToAgentSpace: () => void;
}) {
    if (section === "quick-start") {
        return (
            <>
                <GuideHeading
                    eyebrow="大约 5 分钟"
                    title="创建并发布第一个 Agent"
                    description="沿着一条完整路径认识 Inkwell。每一步都可以稍后返回，不会强制锁定操作顺序。"
                />
                <Steps
                    orientation="vertical"
                    current={1}
                    items={[
                        {
                            title: "创建 Agent",
                            content: "填写名称和用途，建立一个未发布草稿。",
                        },
                        {
                            title: "完成核心配置",
                            content: "补充 Instructions，并选择运行模型。",
                        },
                        {
                            title: "试运行",
                            content: "用真实问题检查回答是否符合预期。",
                        },
                        {
                            title: "发布版本",
                            content: "把当前草稿固化为可用于对话的正式版本。",
                        },
                        {
                            title: "按需共享",
                            content: "允许团队成员只读查看和使用已发布版本。",
                        },
                    ]}
                />
                <Space style={{ marginTop: 18 }}>
                    <Button
                        type="primary"
                        icon={<RocketOutlined />}
                        onClick={onStartQuickGuide}
                    >
                        打开快速开始
                    </Button>
                    <Button
                        icon={<ArrowRightOutlined />}
                        onClick={onGoToAgentSpace}
                    >
                        前往 Agent 空间
                    </Button>
                </Space>
            </>
        );
    }

    if (section === "create") {
        return (
            <>
                <GuideHeading
                    eyebrow="Agent 配置"
                    title="先定义职责，再补充能力"
                    description="建议先用最小配置完成一次试运行，再逐步挂载 Tools 与 Skills。"
                />
                <GuideList
                    items={[
                        [
                            "基础信息",
                            "用清晰名称和简短描述说明 Agent 的职责边界。",
                        ],
                        [
                            "Instructions",
                            "写明目标、约束、输出格式和无法完成时的处理方式。",
                        ],
                        [
                            "模型与参数",
                            "选择运行模型；没有明确原因时保留默认生成参数。",
                        ],
                        [
                            "Tools 与 Skills",
                            "只挂载任务真正需要的能力，减少不确定行为。",
                        ],
                    ]}
                />
                <Alert
                    type="info"
                    showIcon
                    title="试运行使用当前已保存配置；正式对话始终使用已发布版本。"
                />
            </>
        );
    }

    if (section === "publish") {
        return (
            <>
                <GuideHeading
                    eyebrow="版本生命周期"
                    title="保存草稿不等于发布"
                    description="草稿用于继续编辑，发布用于生成新的正式版本。两者不会互相替代。"
                />
                <GuideList
                    items={[
                        [
                            "保存",
                            "保存当前配置为草稿，不影响已发布版本和进行中的对话。",
                        ],
                        [
                            "发布",
                            "把草稿固化成新版本；新的对话轮次开始使用该版本。",
                        ],
                        [
                            "有未发布的修改",
                            "说明已发布版本仍可用，但当前草稿包含更新。",
                        ],
                    ]}
                />
                <Divider />
                <Space size={8} wrap>
                    <Tag color="warning">未发布的草稿</Tag>
                    <Tag color="processing">有未发布的修改</Tag>
                    <Tag color="success">已发布</Tag>
                </Space>
            </>
        );
    }

    if (section === "share") {
        return (
            <>
                <GuideHeading
                    eyebrow="团队协作"
                    title="共享使用权，不共享编辑权"
                    description="团队成员可以查看和使用已发布版本，但不能修改 Owner 的 Agent。"
                />
                <GuideList
                    items={[
                        [
                            "共享",
                            "只暴露当前已发布版本；未发布修改不会提前生效。",
                        ],
                        [
                            "撤销共享",
                            "团队成员失去访问权限，Owner 原件和配置不会被删除。",
                        ],
                        [
                            "复制为我的 Agent",
                            "创建独立副本，复制者成为新副本的 Owner。",
                        ],
                    ]}
                    icons={[
                        <ShareAltOutlined key="share" />,
                        <EditOutlined key="revoke" />,
                        <CopyOutlined key="copy" />,
                    ]}
                />
            </>
        );
    }

    return (
        <>
            <GuideHeading
                eyebrow="常见问题"
                title="快速找到当前状态的含义"
                description="这些问题覆盖 Agent 创建、版本和团队共享中的常见困惑。"
            />
            <GuideList
                items={[
                    [
                        "为什么卡片点击进入对话？",
                        "已发布 Agent 的主要任务是使用；通过卡片操作进入编辑或只读详情。",
                    ],
                    [
                        "为什么团队成员不能编辑共享 Agent？",
                        "共享只授予查看和使用权限；需要修改时请复制为自己的 Agent。",
                    ],
                    [
                        "撤销共享会删除 Agent 吗？",
                        "不会。它只移除团队可见性，Owner 的原件和版本历史保持不变。",
                    ],
                    [
                        "修改后为什么对话没有变化？",
                        "保存草稿不会影响正式对话，需要发布新版本后才会生效。",
                    ],
                ]}
                icons={Array.from({ length: 4 }, (_, index) => (
                    <MessageOutlined key={index} />
                ))}
            />
        </>
    );
}

function GuideHeading({
    eyebrow,
    title,
    description,
}: {
    eyebrow: string;
    title: string;
    description: string;
}) {
    return (
        <header style={{ marginBottom: 28 }}>
            <Typography.Text type="secondary">{eyebrow}</Typography.Text>
            <Typography.Title level={2} style={{ margin: "5px 0 8px" }}>
                {title}
            </Typography.Title>
            <Typography.Paragraph type="secondary" style={{ fontSize: 15 }}>
                {description}
            </Typography.Paragraph>
        </header>
    );
}

function GuideList({
    items,
    icons,
}: {
    items: Array<[string, string]>;
    icons?: React.ReactNode[];
}) {
    return (
        <div style={{ marginBottom: 24 }}>
            {items.map(([title, description], index) => (
                <div
                    key={title}
                    style={{
                        display: "grid",
                        gridTemplateColumns: "32px minmax(0, 1fr)",
                        gap: 12,
                        padding: "14px 0",
                        borderBottom:
                            "1px solid var(--ant-color-border-secondary)",
                    }}
                >
                    <span style={{ fontSize: 17, paddingTop: 2 }}>
                        {icons?.[index] ?? <CheckCircleOutlined />}
                    </span>
                    <div>
                        <Typography.Text strong>{title}</Typography.Text>
                        <Typography.Paragraph
                            type="secondary"
                            style={{ margin: "3px 0 0" }}
                        >
                            {description}
                        </Typography.Paragraph>
                    </div>
                </div>
            ))}
        </div>
    );
}
