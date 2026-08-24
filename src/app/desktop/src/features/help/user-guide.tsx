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
} from "antd";
import { useDeferredValue, useState, type ReactNode } from "react";
import { useTranslation } from "react-i18next";

export type GuideSection =
    | "quick-start"
    | "create"
    | "publish"
    | "share"
    | "faq";

const GuideSections: Array<{
    key: GuideSection;
    translationKey: "quickStart" | "create" | "publish" | "share" | "faq";
}> = [
    { key: "quick-start", translationKey: "quickStart" },
    { key: "create", translationKey: "create" },
    { key: "publish", translationKey: "publish" },
    { key: "share", translationKey: "share" },
    { key: "faq", translationKey: "faq" },
];

interface UserGuideProps {
    initialSection: GuideSection;
    onStartQuickGuide: () => void;
    onGoToAgentSpace: () => void;
}

export function UserGuide({
    initialSection,
    onStartQuickGuide,
    onGoToAgentSpace,
}: UserGuideProps) {
    const { t } = useTranslation();
    const [activeSection, setActiveSection] =
        useState<GuideSection>(initialSection);
    const [searchText, setSearchText] = useState("");
    const deferredSearch = useDeferredValue(searchText.trim().toLowerCase());
    const sections = GuideSections.map((section) => ({
        ...section,
        label: t(`guide.sections.${section.translationKey}.label`),
        description: t(
            `guide.sections.${section.translationKey}.description`,
        ),
    }));
    const visibleSections = sections.filter((section) =>
        `${section.label} ${section.description}`
            .toLowerCase()
            .includes(deferredSearch),
    );

    return (
        <main className="user-guide-page">
            <aside className="user-guide-sidebar">
                <div className="user-guide-sidebar-header">
                    <Typography.Text strong>{t("guide.title")}</Typography.Text>
                </div>
                <div className="user-guide-search">
                    <Input
                        allowClear
                        prefix={<SearchOutlined />}
                        placeholder={t("guide.searchPlaceholder")}
                        value={searchText}
                        onChange={(event) => setSearchText(event.target.value)}
                    />
                </div>
                <nav
                    className="user-guide-nav"
                    aria-label={t("guide.navigationLabel")}
                >
                    {visibleSections.length > 0 ? (
                        visibleSections.map((section) => (
                            <button
                                key={section.key}
                                type="button"
                                className={
                                    section.key === activeSection ? "active" : ""
                                }
                                aria-current={
                                    section.key === activeSection
                                        ? "page"
                                        : undefined
                                }
                                onClick={() => setActiveSection(section.key)}
                            >
                                {section.label}
                            </button>
                        ))
                    ) : (
                        <Empty
                            image={Empty.PRESENTED_IMAGE_SIMPLE}
                            description={t("guide.noMatches")}
                        />
                    )}
                </nav>
            </aside>

            <section className="user-guide-content">
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
    const { t } = useTranslation();
    if (section === "quick-start") {
        return (
            <>
                <GuideHeading
                    eyebrow={t("guide.quickStart.eyebrow")}
                    title={t("guide.quickStart.title")}
                    description={t("guide.quickStart.description")}
                />
                <Steps
                    orientation="vertical"
                    current={1}
                    items={[
                        {
                            title: t("guide.quickStart.steps.create.title"),
                            content: t("guide.quickStart.steps.create.content"),
                        },
                        {
                            title: t("guide.quickStart.steps.configure.title"),
                            content: t("guide.quickStart.steps.configure.content"),
                        },
                        {
                            title: t("guide.quickStart.steps.trial.title"),
                            content: t("guide.quickStart.steps.trial.content"),
                        },
                        {
                            title: t("guide.quickStart.steps.publish.title"),
                            content: t("guide.quickStart.steps.publish.content"),
                        },
                        {
                            title: t("guide.quickStart.steps.share.title"),
                            content: t("guide.quickStart.steps.share.content"),
                        },
                    ]}
                />
                <Space className="user-guide-actions">
                    <Button
                        type="primary"
                        icon={<RocketOutlined />}
                        onClick={onStartQuickGuide}
                    >
                        {t("guide.quickStart.open")}
                    </Button>
                    <Button
                        icon={<ArrowRightOutlined />}
                        onClick={onGoToAgentSpace}
                    >
                        {t("guide.quickStart.goToAgents")}
                    </Button>
                </Space>
            </>
        );
    }

    if (section === "create") {
        return (
            <>
                <GuideHeading
                    eyebrow={t("guide.create.eyebrow")}
                    title={t("guide.create.title")}
                    description={t("guide.create.description")}
                />
                <GuideList
                    items={[
                        [t("guide.create.items.basics.title"), t("guide.create.items.basics.description")],
                        [
                            t("guide.create.items.instructions.title"),
                            t("guide.create.items.instructions.description"),
                        ],
                        [
                            t("guide.create.items.model.title"),
                            t("guide.create.items.model.description"),
                        ],
                        [
                            t("guide.create.items.capabilities.title"),
                            t("guide.create.items.capabilities.description"),
                        ],
                    ]}
                />
                <Alert
                    type="info"
                    showIcon
                    title={t("guide.create.trialNote")}
                />
            </>
        );
    }

    if (section === "publish") {
        return (
            <>
                <GuideHeading
                    eyebrow={t("guide.publish.eyebrow")}
                    title={t("guide.publish.title")}
                    description={t("guide.publish.description")}
                />
                <GuideList
                    items={[
                        [
                            t("guide.publish.items.save.title"),
                            t("guide.publish.items.save.description"),
                        ],
                        [
                            t("guide.publish.items.publish.title"),
                            t("guide.publish.items.publish.description"),
                        ],
                        [
                            t("guide.publish.items.changes.title"),
                            t("guide.publish.items.changes.description"),
                        ],
                    ]}
                />
                <Divider />
                <Space size={8} wrap>
                    <Tag color="warning">{t("guide.publish.draft")}</Tag>
                    <Tag color="processing">{t("guide.publish.changed")}</Tag>
                    <Tag color="success">{t("guide.publish.published")}</Tag>
                </Space>
            </>
        );
    }

    if (section === "share") {
        return (
            <>
                <GuideHeading
                    eyebrow={t("guide.share.eyebrow")}
                    title={t("guide.share.title")}
                    description={t("guide.share.description")}
                />
                <GuideList
                    items={[
                        [
                            t("guide.share.items.share.title"),
                            t("guide.share.items.share.description"),
                        ],
                        [
                            t("guide.share.items.revoke.title"),
                            t("guide.share.items.revoke.description"),
                        ],
                        [
                            t("guide.share.items.copy.title"),
                            t("guide.share.items.copy.description"),
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
                eyebrow={t("guide.faq.eyebrow")}
                title={t("guide.faq.title")}
                description={t("guide.faq.description")}
            />
            <GuideList
                items={[
                    [
                        t("guide.faq.items.open.title"),
                        t("guide.faq.items.open.description"),
                    ],
                    [
                        t("guide.faq.items.edit.title"),
                        t("guide.faq.items.edit.description"),
                    ],
                    [
                        t("guide.faq.items.revoke.title"),
                        t("guide.faq.items.revoke.description"),
                    ],
                    [
                        t("guide.faq.items.changes.title"),
                        t("guide.faq.items.changes.description"),
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
        <header className="user-guide-heading">
            <Typography.Text type="secondary">{eyebrow}</Typography.Text>
            <Typography.Title level={2}>{title}</Typography.Title>
            <Typography.Paragraph type="secondary">
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
    icons?: ReactNode[];
}) {
    return (
        <div className="user-guide-list">
            {items.map(([title, description], index) => (
                <div className="user-guide-list-item" key={title}>
                    <span>{icons?.[index] ?? <CheckCircleOutlined />}</span>
                    <div>
                        <Typography.Text strong>{title}</Typography.Text>
                        <Typography.Paragraph type="secondary">
                            {description}
                        </Typography.Paragraph>
                    </div>
                </div>
            ))}
        </div>
    );
}