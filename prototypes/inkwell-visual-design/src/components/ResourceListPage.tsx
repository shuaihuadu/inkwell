import { useEffect, useState, type ReactNode } from "react";
import {
    Button,
    Input,
    Pagination,
    Space,
    Table,
    Tooltip,
    Typography,
    type ButtonProps,
    type TableColumnsType,
    type TableProps,
} from "antd";
import { ReloadOutlined, SearchOutlined } from "@ant-design/icons";

const PAGE_SIZE = 20;

interface ResourceRowActionProps {
    label: string;
    text: string;
    icon: ReactNode;
    onClick: () => void;
    loading?: boolean;
    danger?: boolean;
    disabled?: boolean;
}

export function ResourceRowActions({ children }: { children: ReactNode }) {
    return <span className="inkwell-row-actions">{children}</span>;
}

export function ResourceRowAction({
    label,
    text,
    icon,
    onClick,
    loading,
    danger,
    disabled,
}: ResourceRowActionProps) {
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

interface ResourceListPageProps<ItemType extends object> {
    title: string;
    description: ReactNode;
    primaryAction?: ReactNode;
    filters?: ReactNode;
    refreshLabel: string;
    onRefresh?: () => void;
    searchValue: string;
    searchPlaceholder: string;
    onSearchChange: (value: string) => void;
    paginationResetKey: string;
    dataSource: ItemType[];
    columns: TableColumnsType<ItemType>;
    rowKey: TableProps<ItemType>["rowKey"];
    tableScrollX: number;
    totalLabel: (total: number) => ReactNode;
    children?: ReactNode;
}

export default function ResourceListPage<ItemType extends object>({
    title,
    description,
    primaryAction,
    filters,
    refreshLabel,
    onRefresh,
    searchValue,
    searchPlaceholder,
    onSearchChange,
    paginationResetKey,
    dataSource,
    columns,
    rowKey,
    tableScrollX,
    totalLabel,
    children,
}: ResourceListPageProps<ItemType>) {
    const [page, setPage] = useState(1);

    useEffect(() => {
        setPage(1);
    }, [paginationResetKey]);

    const maximumPage = Math.max(1, Math.ceil(dataSource.length / PAGE_SIZE));
    const currentPage = Math.min(page, maximumPage);

    return (
        <div className="inkwell-resource-page">
            <header className="inkwell-resource-header">
                <div>
                    <Typography.Title level={4}>{title}</Typography.Title>
                    <Typography.Text type="secondary">{description}</Typography.Text>
                </div>
                {primaryAction}
            </header>

            <div className="inkwell-resource-toolbar">
                {filters}
                <Space className="inkwell-resource-actions">
                    <Tooltip title="刷新">
                        <Button
                            aria-label={refreshLabel}
                            icon={<ReloadOutlined />}
                            onClick={onRefresh}
                        />
                    </Tooltip>
                    <Input
                        allowClear
                        prefix={<SearchOutlined />}
                        placeholder={searchPlaceholder}
                        value={searchValue}
                        onChange={(event) => onSearchChange(event.target.value)}
                        style={{ width: 280 }}
                    />
                </Space>
            </div>

            <div className="inkwell-resource-table">
                <Table<ItemType>
                    rowKey={rowKey}
                    dataSource={dataSource.slice(
                        (currentPage - 1) * PAGE_SIZE,
                        currentPage * PAGE_SIZE,
                    )}
                    scroll={{ x: tableScrollX }}
                    pagination={false}
                    columns={columns}
                />
            </div>

            <div className="inkwell-resource-pagination">
                <Typography.Text type="secondary">
                    {totalLabel(dataSource.length)}
                </Typography.Text>
                <Pagination
                    size="small"
                    current={currentPage}
                    pageSize={PAGE_SIZE}
                    total={dataSource.length}
                    showSizeChanger={false}
                    onChange={setPage}
                />
            </div>

            {children}
        </div>
    );
}