import { LockOutlined, UnlockOutlined } from "@ant-design/icons";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Modal, Select, Tag, Typography, message } from "antd";
import { useState } from "react";
import DataListPage, {
    DataListRowAction,
} from "../../shared/components/data-list-page";
import { desktopApi } from "../../shared/network/desktop-api";
import type { UserListItem } from "../../shared/network/contracts";

type AccountStatus = "all" | "active" | "locked";
type AccountRole = "all" | "Admin" | "Member";

const formatDateTime = (value: string | null): string =>
    value
        ? new Intl.DateTimeFormat("zh-CN", {
              dateStyle: "medium",
              timeStyle: "short",
              hour12: false,
          }).format(new Date(value))
        : "从未登录";

export function UserManagement() {
    const queryClient = useQueryClient();
    const [searchText, setSearchText] = useState("");
    const [status, setStatus] = useState<AccountStatus>("all");
    const [role, setRole] = useState<AccountRole>("all");
    const [messageApi, messageContext] = message.useMessage();
    const [modalApi, modalContext] = Modal.useModal();
    const accountsQuery = useQuery({
        queryKey: ["accounts"],
        queryFn: desktopApi.listAccounts,
    });
    const unlockMutation = useMutation({
        mutationFn: (account: UserListItem) =>
            desktopApi.unlockAccount(account.userId),
        onSuccess: async (_, account) => {
            await queryClient.invalidateQueries({ queryKey: ["accounts"] });
            messageApi.success(`${account.username} 已解封`);
        },
        onError: (reason) =>
            messageApi.error(
                `解封失败：${reason instanceof Error ? reason.message : "未知错误"}`,
            ),
    });
    const normalizedSearch = searchText.trim().toLocaleLowerCase();
    const accounts = (accountsQuery.data ?? []).filter((account) => {
        const matchesText = account.username
            .toLocaleLowerCase()
            .includes(normalizedSearch);
        const matchesStatus =
            status === "all" ||
            (status === "locked" ? account.isLocked : !account.isLocked);
        const matchesRole =
            role === "all" ||
            (role === "Admin" ? account.isSuper : !account.isSuper);
        return matchesText && matchesStatus && matchesRole;
    });
    const isFiltered =
        normalizedSearch.length > 0 || status !== "all" || role !== "all";

    const confirmUnlock = (account: UserListItem): void => {
        modalApi.confirm({
            title: `解封账号 ${account.username}`,
            icon: <UnlockOutlined />,
            content: `将解封 ${account.username}，是否继续？`,
            okText: "确认解封",
            cancelText: "取消",
            onOk: () => unlockMutation.mutateAsync(account),
        });
    };

    return (
        <DataListPage<UserListItem>
            title="用户管理"
            description="查看部署内账号与锁定状态。账号创建、删除、密码和角色变更由运维流程处理。"
            filters={
                <>
                    <Select<AccountStatus>
                        aria-label="筛选账号状态"
                        value={status}
                        onChange={setStatus}
                        options={[
                            { value: "all", label: "全部状态" },
                            { value: "active", label: "正常" },
                            { value: "locked", label: "已锁定" },
                        ]}
                    />
                    <Select<AccountRole>
                        aria-label="筛选账号角色"
                        value={role}
                        onChange={setRole}
                        options={[
                            { value: "all", label: "全部角色" },
                            { value: "Admin", label: "Admin" },
                            { value: "Member", label: "Member" },
                        ]}
                    />
                </>
            }
            refreshLabel="刷新用户"
            onRefresh={() => void accountsQuery.refetch()}
            refreshing={accountsQuery.isFetching && !accountsQuery.isLoading}
            searchValue={searchText}
            searchPlaceholder="搜索用户名"
            searchMaxLength={64}
            onSearchChange={setSearchText}
            paginationResetKey={`${searchText}:${status}:${role}`}
            dataSource={accounts}
            rowKey="userId"
            tableScrollX={800}
            totalLabel={(total) => `共 ${total} 个用户`}
            loading={accountsQuery.isLoading}
            errorMessage={
                accountsQuery.isError ? "用户列表加载失败，请重试" : undefined
            }
            onRetry={() => void accountsQuery.refetch()}
            emptyText="当前没有用户"
            filteredEmptyText="在所选条件内没有结果，请清除筛选"
            isFiltered={isFiltered}
            columns={[
                { title: "用户名", dataIndex: "username", width: 200 },
                {
                    title: "角色",
                    dataIndex: "isSuper",
                    width: 120,
                    render: (isSuper: boolean) => (
                        <Tag color={isSuper ? "blue" : "default"}>
                            {isSuper ? "Admin" : "Member"}
                        </Tag>
                    ),
                },
                {
                    title: "状态",
                    dataIndex: "isLocked",
                    width: 120,
                    render: (isLocked: boolean) => (
                        <Tag
                            color={isLocked ? "error" : "success"}
                            icon={isLocked ? <LockOutlined /> : undefined}
                        >
                            {isLocked ? "已锁定" : "正常"}
                        </Tag>
                    ),
                },
                {
                    title: "最后登录",
                    dataIndex: "lastLoginTime",
                    width: 180,
                    render: formatDateTime,
                },
                {
                    title: "创建时间",
                    dataIndex: "createdTime",
                    width: 180,
                    render: formatDateTime,
                },
                {
                    title: "操作",
                    key: "actions",
                    width: 100,
                    fixed: "right",
                    align: "center",
                    className: "inkwell-action-column",
                    render: (_, account) =>
                        account.isLocked ? (
                            <DataListRowAction
                                label={`解封 ${account.username}`}
                                text="解封"
                                icon={<UnlockOutlined />}
                                loading={
                                    unlockMutation.isPending &&
                                    unlockMutation.variables.userId ===
                                        account.userId
                                }
                                onClick={() => confirmUnlock(account)}
                            />
                        ) : (
                            <Typography.Text type="secondary">
                                -
                            </Typography.Text>
                        ),
                },
            ]}
        >
            {messageContext}
            {modalContext}
        </DataListPage>
    );
}