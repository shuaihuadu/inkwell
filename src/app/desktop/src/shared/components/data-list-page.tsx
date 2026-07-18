import { ReloadOutlined, SearchOutlined } from "@ant-design/icons";
import {
    Alert,
    Button,
    Empty,
    Input,
    Pagination,
    Skeleton,
    Space,
    Table,
    Tooltip,
    Typography,
    type ButtonProps,
    type TableColumnsType,
    type TableProps,
} from "antd";
import { useState, type ReactNode } from "react";

const pageSize = 20;

interface DataListRowActionProps {
    label: string;
    text: string;
    icon: ReactNode;
    onClick: () => void;
    loading?: boolean;
    danger?: boolean;
    disabled?: boolean;
}

export function DataListRowActions({ children }: { children: ReactNode }) {
    return <span className="inkwell-row-actions">{children}</span>;
}

export function DataListRowAction({
    label,
    text,
    icon,
    onClick,
    loading,
    danger,
    disabled,
}: DataListRowActionProps) {
    const buttonProps: ButtonProps = {
        "aria-label": label,
        className: "inkwell-row-action",
        type: "default",
        size: "small",
        icon,
        onClick,
        loading,
        danger,
        disabled,
    };

    return (
        <Tooltip title={label}>
            <Button {...buttonProps}>{text}</Button>
        </Tooltip>
    );
}

interface DataListPageProps<ItemType extends object> {
    title: string;
    description: ReactNode;
    primaryAction?: ReactNode;
    filters?: ReactNode;
    refreshLabel: string;
    onRefresh: () => void;
    refreshing?: boolean;
    searchValue: string;
    searchPlaceholder: string;
    searchMaxLength?: number;
    onSearchChange: (value: string) => void;
    paginationResetKey: string;
    dataSource: ItemType[];
    columns: TableColumnsType<ItemType>;
    rowKey: TableProps<ItemType>["rowKey"];
    tableScrollX: number;
    totalLabel: (total: number) => ReactNode;
    loading?: boolean;
    errorMessage?: string;
    onRetry?: () => void;
    emptyText: ReactNode;
    filteredEmptyText: ReactNode;
    isFiltered: boolean;
    children?: ReactNode;
}

export default function DataListPage<ItemType extends object>({
    title,
    description,
    primaryAction,
    filters,
    refreshLabel,
    onRefresh,
    refreshing,
    searchValue,
    searchPlaceholder,
    searchMaxLength,
    onSearchChange,
    paginationResetKey,
    dataSource,
    columns,
    rowKey,
    tableScrollX,
    totalLabel,
    loading,
    errorMessage,
    onRetry,
    emptyText,
    filteredEmptyText,
    isFiltered,
    children,
}: DataListPageProps<ItemType>) {
    const [pagination, setPagination] = useState({
        resetKey: paginationResetKey,
        page: 1,
    });
    const requestedPage =
        pagination.resetKey === paginationResetKey ? pagination.page : 1;
    const maximumPage = Math.max(1, Math.ceil(dataSource.length / pageSize));
    const currentPage = Math.min(requestedPage, maximumPage);
    const visibleItems = dataSource.slice(
        (currentPage - 1) * pageSize,
        currentPage * pageSize,
    );

    return (
        <main className="inkwell-data-list-page">
            <header className="inkwell-data-list-header">
                <div>
                    <Typography.Title level={4}>{title}</Typography.Title>
                    <Typography.Text type="secondary">
                        {description}
                    </Typography.Text>
                </div>
                {primaryAction}
            </header>

            {errorMessage && (
                <Alert
                    type="error"
                    showIcon
                    message={errorMessage}
                    action={
                        onRetry && (
                            <Button size="small" onClick={onRetry}>
                                重试
                            </Button>
                        )
                    }
                />
            )}

            <div className="inkwell-data-list-toolbar">
                <Space wrap>{filters}</Space>
                <Space className="inkwell-data-list-actions">
                    <Tooltip title="刷新">
                        <Button
                            aria-label={refreshLabel}
                            icon={<ReloadOutlined />}
                            loading={refreshing}
                            onClick={onRefresh}
                        />
                    </Tooltip>
                    <Input
                        allowClear
                        prefix={<SearchOutlined />}
                        placeholder={searchPlaceholder}
                        value={searchValue}
                        maxLength={searchMaxLength}
                        onChange={(event) =>
                            onSearchChange(event.target.value)
                        }
                    />
                </Space>
            </div>

            <div className="inkwell-data-list-table">
                {loading ? (
                    <Skeleton
                        active
                        title={false}
                        paragraph={{ rows: 8, width: "100%" }}
                    />
                ) : (
                    <Table<ItemType>
                        rowKey={rowKey}
                        dataSource={visibleItems}
                        scroll={{ x: tableScrollX }}
                        pagination={false}
                        columns={columns}
                        locale={{
                            emptyText: (
                                <Empty
                                    image={Empty.PRESENTED_IMAGE_SIMPLE}
                                    description={
                                        isFiltered
                                            ? filteredEmptyText
                                            : emptyText
                                    }
                                />
                            ),
                        }}
                    />
                )}
            </div>

            {!loading && (
                <div className="inkwell-data-list-pagination">
                    <Typography.Text type="secondary">
                        {totalLabel(dataSource.length)}
                    </Typography.Text>
                    <Pagination
                        size="small"
                        current={currentPage}
                        pageSize={pageSize}
                        total={dataSource.length}
                        showSizeChanger={false}
                        onChange={(page) =>
                            setPagination({
                                resetKey: paginationResetKey,
                                page,
                            })
                        }
                    />
                </div>
            )}

            {children}
        </main>
    );
}