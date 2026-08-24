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
import type { TFunction } from "i18next";
import { useTranslation } from "react-i18next";
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

const formatDateTime = (
    value: string | null,
    locale: string,
    t: TFunction,
): string =>
    value
        ? new Intl.DateTimeFormat(locale, {
              year: "numeric",
              month: "2-digit",
              day: "2-digit",
              hour: "2-digit",
              minute: "2-digit",
              hour12: false,
          }).format(new Date(value))
        : t("users.neverLoggedIn");

const getStatus = (account: UserListItem): Exclude<AccountStatus, "all"> =>
    account.isDisabled ? "disabled" : account.isLocked ? "locked" : "active";

export function UserManagement() {
    const { t, i18n } = useTranslation();
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
        accountsQuery.data?.find(
            (account) => account.userId === managedUserId,
        ) ?? null;

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
                t("users.errors.add", {
                    message:
                        reason instanceof Error
                            ? reason.message
                            : t("common.unknownError"),
                }),
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
            messageApi.success(
                t(`users.actionSuccess.${action}`, {
                    username: account.username,
                }),
            );
        },
        onError: (reason) =>
            messageApi.error(
                t("users.errors.action", {
                    message:
                        reason instanceof Error
                            ? reason.message
                            : t("common.unknownError"),
                }),
            ),
    });
    const resetPasswordMutation = useMutation({
        mutationFn: (account: UserListItem) =>
            desktopApi.resetAccountPassword(account.userId),
        onSuccess: setIssuedCredential,
        onError: (reason) =>
            messageApi.error(
                t("users.errors.reset", {
                    message:
                        reason instanceof Error
                            ? reason.message
                            : t("common.unknownError"),
                }),
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
                title: t("users.confirmations.unlock.title", {
                    username: account.username,
                }),
                content: t("users.confirmations.unlock.content"),
                okText: t("users.confirmations.unlock.confirm"),
                icon: <UnlockOutlined />,
                danger: false,
            },
            disable: {
                title: t("users.confirmations.disable.title", {
                    username: account.username,
                }),
                content: t("users.confirmations.disable.content"),
                okText: t("users.confirmations.disable.confirm"),
                icon: <StopOutlined />,
                danger: true,
            },
            enable: {
                title: t("users.confirmations.enable.title", {
                    username: account.username,
                }),
                content: t("users.confirmations.enable.content"),
                okText: t("users.confirmations.enable.confirm"),
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
            cancelText: t("common.cancel"),
            onOk: () => statusMutation.mutateAsync({ account, action }),
        });
    };

    const confirmResetPassword = (account: UserListItem): void => {
        modalApi.confirm({
            title: t("users.confirmations.reset.title", {
                username: account.username,
            }),
            icon: <KeyOutlined />,
            content: t("users.confirmations.reset.content"),
            okText: t("users.confirmations.reset.confirm"),
            cancelText: t("common.cancel"),
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
                    ? t("users.status.disabled")
                    : accountStatus === "locked"
                      ? t("users.status.locked")
                      : t("users.status.active")}
            </Tag>
        );
    };

    return (
        <DataListPage<UserListItem>
            title={t("users.title")}
            description={t("users.description")}
            primaryAction={
                <Button
                    type="primary"
                    icon={<PlusOutlined />}
                    onClick={() => setCreateOpen(true)}
                >
                    {t("users.add")}
                </Button>
            }
            filters={
                <>
                    <Select<AccountStatus>
                        aria-label={t("users.filters.statusLabel")}
                        value={status}
                        onChange={setStatus}
                        style={{ width: 132 }}
                        options={[
                            {
                                value: "all",
                                label: t("users.filters.allStatus"),
                            },
                            {
                                value: "active",
                                label: t("users.status.active"),
                            },
                            {
                                value: "locked",
                                label: t("users.status.locked"),
                            },
                            {
                                value: "disabled",
                                label: t("users.status.disabled"),
                            },
                        ]}
                    />
                    <Select<AccountRole>
                        aria-label={t("users.filters.roleLabel")}
                        value={role}
                        onChange={setRole}
                        style={{ width: 132 }}
                        options={[
                            {
                                value: "all",
                                label: t("users.filters.allRoles"),
                            },
                            { value: "Admin", label: "Admin" },
                            { value: "Member", label: "Member" },
                        ]}
                    />
                </>
            }
            refreshLabel={t("users.refreshLabel")}
            onRefresh={() => void accountsQuery.refetch()}
            refreshing={accountsQuery.isFetching && !accountsQuery.isLoading}
            searchValue={searchText}
            searchPlaceholder={t("users.searchPlaceholder")}
            searchMaxLength={100}
            onSearchChange={setSearchText}
            paginationResetKey={`${searchText}:${status}:${role}`}
            dataSource={accounts}
            rowKey="userId"
            tableScrollX={800}
            loading={accountsQuery.isLoading}
            errorMessage={
                accountsQuery.isError ? t("users.loadFailed") : undefined
            }
            onRetry={() => void accountsQuery.refetch()}
            emptyText={t("users.empty")}
            filteredEmptyText={t("users.filteredEmpty")}
            isFiltered={isFiltered}
            columns={[
                {
                    title: t("users.columns.username"),
                    dataIndex: "username",
                    width: 200,
                },
                {
                    title: t("users.columns.role"),
                    dataIndex: "isAdmin",
                    width: 120,
                    render: (isAdmin: boolean) => (
                        <Tag color={isAdmin ? "blue" : "default"}>
                            {isAdmin ? "Admin" : "Member"}
                        </Tag>
                    ),
                },
                {
                    title: t("users.columns.status"),
                    key: "status",
                    width: 120,
                    render: (_, account) => renderStatusTag(account),
                },
                {
                    title: t("users.columns.lastLogin"),
                    dataIndex: "lastLoginTime",
                    width: 180,
                    render: (value: string | null) =>
                        formatDateTime(value, i18n.language, t),
                },
                {
                    title: t("users.columns.createdTime"),
                    dataIndex: "createdTime",
                    width: 180,
                    render: (value: string | null) =>
                        formatDateTime(value, i18n.language, t),
                },
                {
                    title: t("users.columns.actions"),
                    key: "actions",
                    width: 100,
                    fixed: "right",
                    align: "center",
                    className: "inkwell-action-column",
                    render: (_, account) => (
                        <DataListRowAction
                            label={t("users.manageLabel", {
                                username: account.username,
                            })}
                            text={t("users.manage")}
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
                title={
                    createdAccount
                        ? t("users.create.successTitle")
                        : t("users.create.title")
                }
                open={createOpen}
                width={520}
                closable={!createdAccount}
                maskClosable={false}
                onCancel={closeCreate}
                footer={
                    createdAccount ? (
                        <Button type="primary" onClick={closeCreate}>
                            {t("common.finish")}
                        </Button>
                    ) : undefined
                }
                okText={t("users.add")}
                cancelText={t("common.cancel")}
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
                            title={t("users.create.created", {
                                username: createdAccount.username,
                            })}
                            description={t(
                                "users.create.temporaryPasswordNotice",
                            )}
                        />
                        <div>
                            <Typography.Text type="secondary">
                                {t("users.create.temporaryPassword")}
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
                            {t("users.create.mustChangePassword")}
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
                            label={t("users.columns.username")}
                            rules={[
                                {
                                    required: true,
                                    whitespace: true,
                                    message: t("users.create.usernameRequired"),
                                },
                                {
                                    max: 100,
                                    message: t("users.create.usernameTooLong"),
                                },
                                {
                                    validator: (
                                        _,
                                        value: string | undefined,
                                    ) =>
                                        value &&
                                        accountsQuery.data?.some(
                                            (account) =>
                                                account.username.toLocaleLowerCase() ===
                                                value
                                                    .trim()
                                                    .toLocaleLowerCase(),
                                        )
                                            ? Promise.reject(
                                                  new Error(
                                                      t(
                                                          "users.create.usernameExists",
                                                      ),
                                                  ),
                                              )
                                            : Promise.resolve(),
                                },
                            ]}
                        >
                            <Input
                                autoFocus
                                placeholder={t(
                                    "users.create.usernamePlaceholder",
                                )}
                                autoComplete="off"
                            />
                        </Form.Item>
                        <Form.Item
                            name="role"
                            label={t("users.create.role")}
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
                                        title={t("users.create.adminWarning")}
                                    />
                                ) : null
                            }
                        </Form.Item>
                        <Typography.Paragraph
                            type="secondary"
                            style={{ margin: "20px 0 0" }}
                        >
                            {t("users.create.passwordHint")}
                        </Typography.Paragraph>
                    </Form>
                )}
            </Modal>
            <Modal
                title={
                    managedUser
                        ? t("users.management.titleWithName", {
                              username: managedUser.username,
                          })
                        : t("users.management.title")
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
                                title={t("users.management.resetSuccess", {
                                    username: issuedCredential.username,
                                })}
                                description={
                                    <div>
                                        <Typography.Paragraph
                                            style={{ margin: "8px 0" }}
                                        >
                                            {t("users.management.resetNotice")}
                                        </Typography.Paragraph>
                                        <Typography.Title
                                            level={4}
                                            copyable
                                            style={{ margin: 0 }}
                                        >
                                            {issuedCredential.temporaryPassword}
                                        </Typography.Title>
                                        <Typography.Text type="secondary">
                                            {t(
                                                "users.management.nextLoginChange",
                                            )}
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
                                            {t("users.management.password")}
                                        </Typography.Title>
                                        <Typography.Text type="secondary">
                                            {t(
                                                "users.management.passwordDescription",
                                            )}
                                        </Typography.Text>
                                    </div>
                                    <Button
                                        icon={<KeyOutlined />}
                                        onClick={() =>
                                            confirmResetPassword(managedUser)
                                        }
                                    >
                                        {t("users.management.resetPassword")}
                                    </Button>
                                </div>
                                <Divider style={{ margin: 0 }} />
                                <div>
                                    <Typography.Title
                                        level={5}
                                        style={{ margin: "0 0 12px" }}
                                    >
                                        {t("users.management.loginStatus")}
                                    </Typography.Title>
                                    {managedUser.userId === identity?.userId ? (
                                        <Alert
                                            type="info"
                                            showIcon
                                            title={t(
                                                "users.management.currentAccount",
                                            )}
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
                                                    title={t(
                                                        "users.management.autoLocked",
                                                    )}
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
                                                            {t(
                                                                "users.management.unlock",
                                                            )}
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
                                                        {t(
                                                            "users.management.enable",
                                                        )}
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
                                                        {t(
                                                            "users.management.disable",
                                                        )}
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
