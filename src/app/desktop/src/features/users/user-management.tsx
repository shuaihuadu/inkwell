import {
    CheckCircleOutlined,
    KeyOutlined,
    LockOutlined,
    PlusOutlined,
    SettingOutlined,
    StopOutlined,
    UnlockOutlined,
} from "@ant-design/icons";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
    Alert,
    Button,
    Divider,
    Form,
    Input,
    Modal,
    Select,
    Space,
    Tag,
    Typography,
    message,
} from "antd";
import { useState } from "react";
import DataListPage, {
    DataListRowAction,
} from "../../shared/components/data-list-page";
import { desktopApi } from "../../shared/network/desktop-api";
import type {
    CreateAccountRequest,
    IssuedCredential,
    UserListItem,
} from "../../shared/network/contracts";
import { useAuthStore } from "../auth/auth-store";

type AccountStatus = "all" | "active" | "locked" | "disabled";
type AccountRole = "all" | "Admin" | "Member";
type StatusAction = "unlock" | "disable" | "enable";

interface CreateAccountFormValues {
    username: string;
    role: Exclude<AccountRole, "all">;
}

const formatDateTime = (value: string | null): string =>
    value
        ? new Intl.DateTimeFormat("zh-CN", {
              year: "numeric",
              month: "2-digit",
              day: "2-digit",
              hour: "2-digit",
              minute: "2-digit",
              hour12: false,
          }).format(new Date(value))
        : "从未登录";

const getStatus = (
    account: UserListItem,
): Exclude<AccountStatus, "all"> =>
    account.isDisabled ? "disabled" : account.isLocked ? "locked" : "active";

export function UserManagement() {
    const identity = useAuthStore((state) => state.identity);
    const queryClient = useQueryClient();
    const [createForm] = Form.useForm<CreateAccountFormValues>();
    const [searchText, setSearchText] = useState("");
    const [status, setStatus] = useState<AccountStatus>("all");
    const [role, setRole] = useState<AccountRole>("all");
    const [createOpen, setCreateOpen] = useState(false);
    const [createdAccount, setCreatedAccount] =
        useState<IssuedCredential | null>(null);
    const [managedUserId, setManagedUserId] = useState<string | null>(null);
    const [issuedCredential, setIssuedCredential] =
        useState<IssuedCredential | null>(null);
    const [messageApi, messageContext] = message.useMessage();
    const [modalApi, modalContext] = Modal.useModal();
    const accountsQuery = useQuery({
        queryKey: ["accounts"],
        queryFn: desktopApi.listAccounts,
    });
    const managedUser =
        accountsQuery.data?.find((account) => account.userId === managedUserId) ??
        null;

    const refreshAccounts = async (): Promise<void> => {
        await queryClient.invalidateQueries({ queryKey: ["accounts"] });
    };

    const createMutation = useMutation({
        mutationFn: (request: CreateAccountRequest) =>
            desktopApi.createAccount(request),
        onSuccess: async (credential) => {
            await refreshAccounts();
            setCreatedAccount(credential);
        },
        onError: (reason) =>
            messageApi.error(
                `添加失败：${reason instanceof Error ? reason.message : "未知错误"}`,
            ),
    });
    const statusMutation = useMutation({
        mutationFn: async ({
            account,
            action,
        }: {
            account: UserListItem;
            action: StatusAction;
        }) => {
            const operations: Record<
                StatusAction,
                (userId: string) => Promise<void>
            > = {
                unlock: desktopApi.unlockAccount,
                disable: desktopApi.disableAccount,
                enable: desktopApi.enableAccount,
            };
            await operations[action](account.userId);
        },
        onSuccess: async (_, { account, action }) => {
            await refreshAccounts();
            const labels: Record<StatusAction, string> = {
                unlock: "解锁",
                disable: "禁用",
                enable: "启用",
            };
            messageApi.success(`${account.username} 已${labels[action]}`);
        },
        onError: (reason) =>
            messageApi.error(
                `操作失败：${reason instanceof Error ? reason.message : "未知错误"}`,
            ),
    });
    const resetPasswordMutation = useMutation({
        mutationFn: (account: UserListItem) =>
            desktopApi.resetAccountPassword(account.userId),
        onSuccess: setIssuedCredential,
        onError: (reason) =>
            messageApi.error(
                `重置失败：${reason instanceof Error ? reason.message : "未知错误"}`,
            ),
    });

    const normalizedSearch = searchText.trim().toLocaleLowerCase();
    const accounts = (accountsQuery.data ?? []).filter((account) => {
        const matchesText = account.username
            .toLocaleLowerCase()
            .includes(normalizedSearch);
        const matchesStatus = status === "all" || getStatus(account) === status;
        const matchesRole =
            role === "all" ||
            (role === "Admin" ? account.isAdmin : !account.isAdmin);
        return matchesText && matchesStatus && matchesRole;
    });
    const isFiltered =
        normalizedSearch.length > 0 || status !== "all" || role !== "all";

    const closeCreate = (): void => {
        setCreateOpen(false);
        setCreatedAccount(null);
        createForm.resetFields();
    };

    const createUser = async (): Promise<void> => {
        const values = await createForm.validateFields();
        await createMutation.mutateAsync({
            username: values.username.trim(),
            isAdmin: values.role === "Admin",
        });
    };

    const confirmStatusChange = (
        account: UserListItem,
        action: StatusAction,
    ): void => {
        const config = {
            unlock: {
                title: `解锁账号 ${account.username}`,
                content: "解锁后，该用户可以立即重新尝试登录。",
                okText: "确认解锁",
                icon: <UnlockOutlined />,
                danger: false,
            },
            disable: {
                title: `禁用账号 ${account.username}`,
                content:
                    "禁用是持续的管理状态。该用户将无法登录，且需要管理员重新启用。",
                okText: "确认禁用",
                icon: <StopOutlined />,
                danger: true,
            },
            enable: {
                title: `启用账号 ${account.username}`,
                content: "启用后，该用户可以使用原密码登录。",
                okText: "确认启用",
                icon: <CheckCircleOutlined />,
                danger: false,
            },
        }[action];

        modalApi.confirm({
            title: config.title,
            icon: config.icon,
            content: config.content,
            okText: config.okText,
            okButtonProps: { danger: config.danger },
            cancelText: "取消",
            onOk: () => statusMutation.mutateAsync({ account, action }),
        });
    };

    const confirmResetPassword = (account: UserListItem): void => {
        modalApi.confirm({
            title: `重置 ${account.username} 的密码`,
            icon: <KeyOutlined />,
            content: "重置后，原密码立即失效。系统将生成仅显示一次的临时密码。",
            okText: "确认重置",
            cancelText: "取消",
            onOk: () => resetPasswordMutation.mutateAsync(account),
        });
    };

    const renderStatusTag = (account: UserListItem) => {
        const accountStatus = getStatus(account);
        return (
            <Tag
                color={
                    accountStatus === "disabled"
                        ? "default"
                        : accountStatus === "locked"
                          ? "warning"
                          : "success"
                }
                icon={
                    accountStatus === "disabled" ? (
                        <StopOutlined />
                    ) : accountStatus === "locked" ? (
                        <LockOutlined />
                    ) : undefined
                }
            >
                {accountStatus === "disabled"
                    ? "已禁用"
                    : accountStatus === "locked"
                      ? "已锁定"
                      : "正常"}
            </Tag>
        );
    };

    return (
        <DataListPage<UserListItem>
            title="用户管理"
            description="添加用户，重置密码，并管理账号的锁定或禁用状态。"
            primaryAction={
                <Button
                    type="primary"
                    icon={<PlusOutlined />}
                    onClick={() => setCreateOpen(true)}
                >
                    添加用户
                </Button>
            }
            filters={
                <>
                    <Select<AccountStatus>
                        aria-label="筛选账号状态"
                        value={status}
                        onChange={setStatus}
                        style={{ width: 132 }}
                        options={[
                            { value: "all", label: "全部状态" },
                            { value: "active", label: "正常" },
                            { value: "locked", label: "已锁定" },
                            { value: "disabled", label: "已禁用" },
                        ]}
                    />
                    <Select<AccountRole>
                        aria-label="筛选账号角色"
                        value={role}
                        onChange={setRole}
                        style={{ width: 132 }}
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
            searchMaxLength={100}
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
                    dataIndex: "isAdmin",
                    width: 120,
                    render: (isAdmin: boolean) => (
                        <Tag color={isAdmin ? "blue" : "default"}>
                            {isAdmin ? "Admin" : "Member"}
                        </Tag>
                    ),
                },
                {
                    title: "状态",
                    key: "status",
                    width: 120,
                    render: (_, account) => renderStatusTag(account),
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
                    render: (_, account) => (
                        <DataListRowAction
                            label={`管理 ${account.username}`}
                            text="管理"
                            icon={<SettingOutlined />}
                            onClick={() => {
                                setManagedUserId(account.userId);
                                setIssuedCredential(null);
                            }}
                        />
                    ),
                },
            ]}
        >
            {messageContext}
            {modalContext}
            <Modal
                title={createdAccount ? "用户已添加" : "添加用户"}
                open={createOpen}
                width={520}
                closable={!createdAccount}
                maskClosable={false}
                onCancel={closeCreate}
                footer={
                    createdAccount ? (
                        <Button type="primary" onClick={closeCreate}>
                            完成
                        </Button>
                    ) : undefined
                }
                okText="添加用户"
                cancelText="取消"
                confirmLoading={createMutation.isPending}
                onOk={createdAccount ? undefined : () => void createUser()}
            >
                {createdAccount ? (
                    <Space
                        orientation="vertical"
                        size="large"
                        style={{ width: "100%" }}
                    >
                        <Alert
                            type="success"
                            showIcon
                            icon={<CheckCircleOutlined />}
                            title={`${createdAccount.username} 已创建`}
                            description="请立即将临时密码交给该用户。关闭此窗口后，临时密码将不再显示。"
                        />
                        <div>
                            <Typography.Text type="secondary">
                                临时密码
                            </Typography.Text>
                            <Typography.Title
                                level={4}
                                copyable
                                style={{ margin: "8px 0 0" }}
                            >
                                {createdAccount.temporaryPassword}
                            </Typography.Title>
                        </div>
                        <Typography.Text type="secondary">
                            用户首次登录后必须设置新密码。
                        </Typography.Text>
                    </Space>
                ) : (
                    <Form<CreateAccountFormValues>
                        form={createForm}
                        layout="vertical"
                        initialValues={{ role: "Member" }}
                        requiredMark="optional"
                    >
                        <Form.Item
                            name="username"
                            label="用户名"
                            rules={[
                                {
                                    required: true,
                                    whitespace: true,
                                    message: "请输入用户名",
                                },
                                {
                                    max: 100,
                                    message: "用户名不能超过 100 个字符",
                                },
                                {
                                    validator: (_, value: string | undefined) =>
                                        value &&
                                        accountsQuery.data?.some(
                                            (account) =>
                                                account.username.toLocaleLowerCase() ===
                                                value
                                                    .trim()
                                                    .toLocaleLowerCase(),
                                        )
                                            ? Promise.reject(
                                                  new Error("用户名已存在"),
                                              )
                                            : Promise.resolve(),
                                },
                            ]}
                        >
                            <Input
                                autoFocus
                                placeholder="输入用户名"
                                autoComplete="off"
                            />
                        </Form.Item>
                        <Form.Item
                            name="role"
                            label="角色"
                            rules={[{ required: true }]}
                        >
                            <Select
                                options={[
                                    { value: "Member", label: "Member" },
                                    { value: "Admin", label: "Admin" },
                                ]}
                            />
                        </Form.Item>
                        <Form.Item
                            noStyle
                            shouldUpdate={(previous, current) =>
                                previous.role !== current.role
                            }
                        >
                            {({ getFieldValue }) =>
                                getFieldValue("role") === "Admin" ? (
                                    <Alert
                                        type="warning"
                                        showIcon
                                        title="Admin 可以管理部署内的用户和共享资源，请仅授予可信人员。"
                                    />
                                ) : null
                            }
                        </Form.Item>
                        <Typography.Paragraph
                            type="secondary"
                            style={{ margin: "20px 0 0" }}
                        >
                            系统会生成一次性临时密码，并在创建成功后显示。
                        </Typography.Paragraph>
                    </Form>
                )}
            </Modal>
            <Modal
                title={
                    managedUser
                        ? `管理用户 · ${managedUser.username}`
                        : "管理用户"
                }
                open={managedUser !== null}
                width={560}
                footer={null}
                onCancel={() => {
                    setManagedUserId(null);
                    setIssuedCredential(null);
                }}
            >
                {managedUser && (
                    <Space
                        orientation="vertical"
                        size="large"
                        style={{ width: "100%" }}
                    >
                        <Space>
                            <Tag
                                color={managedUser.isAdmin ? "blue" : "default"}
                            >
                                {managedUser.isAdmin ? "Admin" : "Member"}
                            </Tag>
                            {renderStatusTag(managedUser)}
                        </Space>
                        {issuedCredential ? (
                            <Alert
                                type="success"
                                showIcon
                                title={`${issuedCredential.username} 的密码已重置`}
                                description={
                                    <div>
                                        <Typography.Paragraph
                                            style={{ margin: "8px 0" }}
                                        >
                                            请立即将临时密码交给该用户。关闭窗口后将不再显示。
                                        </Typography.Paragraph>
                                        <Typography.Title
                                            level={4}
                                            copyable
                                            style={{ margin: 0 }}
                                        >
                                            {issuedCredential.temporaryPassword}
                                        </Typography.Title>
                                        <Typography.Text type="secondary">
                                            用户下次登录时必须设置新密码。
                                        </Typography.Text>
                                    </div>
                                }
                            />
                        ) : (
                            <>
                                <div
                                    style={{
                                        display: "flex",
                                        alignItems: "center",
                                        justifyContent: "space-between",
                                        gap: 24,
                                    }}
                                >
                                    <div style={{ minWidth: 0 }}>
                                        <Typography.Title
                                            level={5}
                                            style={{ margin: "0 0 4px" }}
                                        >
                                            密码
                                        </Typography.Title>
                                        <Typography.Text type="secondary">
                                            生成一次性临时密码，并要求用户下次登录时设置新密码。
                                        </Typography.Text>
                                    </div>
                                    <Button
                                        icon={<KeyOutlined />}
                                        onClick={() =>
                                            confirmResetPassword(managedUser)
                                        }
                                    >
                                        重置密码
                                    </Button>
                                </div>
                                <Divider style={{ margin: 0 }} />
                                <div>
                                    <Typography.Title
                                        level={5}
                                        style={{ margin: "0 0 12px" }}
                                    >
                                        登录状态
                                    </Typography.Title>
                                    {managedUser.userId === identity?.userId ? (
                                        <Alert
                                            type="info"
                                            showIcon
                                            title="不能禁用当前登录账号。"
                                        />
                                    ) : (
                                        <Space
                                            orientation="vertical"
                                            size="middle"
                                            style={{ width: "100%" }}
                                        >
                                            {getStatus(managedUser) ===
                                            "locked" ? (
                                                <Alert
                                                    type="warning"
                                                    showIcon
                                                    title="该账号因登录失败次数过多被系统自动锁定。"
                                                    action={
                                                        <Button
                                                            size="small"
                                                            icon={
                                                                <UnlockOutlined />
                                                            }
                                                            onClick={() =>
                                                                confirmStatusChange(
                                                                    managedUser,
                                                                    "unlock",
                                                                )
                                                            }
                                                        >
                                                            解锁
                                                        </Button>
                                                    }
                                                />
                                            ) : null}
                                            {getStatus(managedUser) ===
                                            "disabled" ? (
                                                <div
                                                    style={{
                                                        display: "flex",
                                                        justifyContent:
                                                            "flex-end",
                                                    }}
                                                >
                                                    <Button
                                                        icon={
                                                            <CheckCircleOutlined />
                                                        }
                                                        onClick={() =>
                                                            confirmStatusChange(
                                                                managedUser,
                                                                "enable",
                                                            )
                                                        }
                                                    >
                                                        启用用户
                                                    </Button>
                                                </div>
                                            ) : (
                                                <div
                                                    style={{
                                                        display: "flex",
                                                        justifyContent:
                                                            "flex-end",
                                                    }}
                                                >
                                                    <Button
                                                        danger
                                                        icon={<StopOutlined />}
                                                        onClick={() =>
                                                            confirmStatusChange(
                                                                managedUser,
                                                                "disable",
                                                            )
                                                        }
                                                    >
                                                        禁用用户
                                                    </Button>
                                                </div>
                                            )}
                                        </Space>
                                    )}
                                </div>
                            </>
                        )}
                    </Space>
                )}
            </Modal>
        </DataListPage>
    );
}