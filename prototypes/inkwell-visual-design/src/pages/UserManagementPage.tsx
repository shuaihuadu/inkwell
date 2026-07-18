import { useState } from "react";
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
import {
    CheckCircleOutlined,
    KeyOutlined,
    LockOutlined,
    PlusOutlined,
    SettingOutlined,
    StopOutlined,
    UnlockOutlined,
} from "@ant-design/icons";
import ResourceListPage, { ResourceRowAction } from "../components/ResourceListPage";

interface UserItem {
    key: string;
    username: string;
    role: "Admin" | "Member";
    status: "active" | "locked" | "disabled";
    lastLogin: string;
    createdTime: string;
}

interface CreateUserFormValues {
    username: string;
    role: UserItem["role"];
}

interface CreatedAccount {
    username: string;
    temporaryPassword: string;
}

interface IssuedCredential {
    username: string;
    temporaryPassword: string;
}

const INITIAL_USERS: UserItem[] = [
    { key: "alice", username: "alice", role: "Admin", status: "active", lastLogin: "2026-07-18 14:56", createdTime: "2026-05-01 09:00" },
    { key: "bob", username: "bob", role: "Member", status: "locked", lastLogin: "2026-07-17 18:20", createdTime: "2026-05-03 10:15" },
    { key: "carol", username: "carol", role: "Member", status: "active", lastLogin: "2026-07-18 11:32", createdTime: "2026-05-08 13:40" },
    ...Array.from({ length: 22 }, (_, index): UserItem => ({
        key: `member-${index + 1}`,
        username: `member-${String(index + 1).padStart(2, "0")}`,
        role: index % 11 === 0 ? "Admin" : "Member",
        status: index % 13 === 0 ? "disabled" : index % 7 === 0 ? "locked" : "active",
        lastLogin: index % 6 === 0 ? "从未登录" : `2026-07-${String(17 - (index % 12)).padStart(2, "0")} 09:20`,
        createdTime: `2026-06-${String((index % 25) + 1).padStart(2, "0")} 10:00`,
    })),
];

export default function UserManagementPage() {
    const [users, setUsers] = useState(INITIAL_USERS);
    const [searchText, setSearchText] = useState("");
    const [status, setStatus] = useState("all");
    const [role, setRole] = useState("all");
    const [createOpen, setCreateOpen] = useState(false);
    const [createdAccount, setCreatedAccount] = useState<CreatedAccount | null>(null);
    const [managedUser, setManagedUser] = useState<UserItem | null>(null);
    const [issuedCredential, setIssuedCredential] = useState<IssuedCredential | null>(null);
    const [createForm] = Form.useForm<CreateUserFormValues>();
    const [messageApi, contextHolder] = message.useMessage();
    const [modalApi, modalContextHolder] = Modal.useModal();

    const filteredUsers = users.filter((user) => {
        const matchesText = user.username.toLowerCase().includes(searchText.trim().toLowerCase());
        const matchesStatus = status === "all" || user.status === status;
        const matchesRole = role === "all" || user.role === role;
        return matchesText && matchesStatus && matchesRole;
    });

    const updateUserStatus = (user: UserItem, nextStatus: UserItem["status"]) => {
        setUsers((current) =>
            current.map((item) =>
                item.key === user.key ? { ...item, status: nextStatus } : item,
            ),
        );
        setManagedUser({ ...user, status: nextStatus });
    };

    const confirmStatusChange = (user: UserItem, action: "unlock" | "disable" | "enable") => {
        const config = {
            unlock: { title: `解锁账号 ${user.username}`, content: "解锁后，该用户可以立即重新尝试登录。", okText: "确认解锁", status: "active" as const },
            disable: { title: `禁用账号 ${user.username}`, content: "禁用是持续的管理状态。该用户将无法登录，且需要管理员重新启用。", okText: "确认禁用", status: "disabled" as const },
            enable: { title: `启用账号 ${user.username}`, content: "启用后，该用户可以使用原密码登录。", okText: "确认启用", status: "active" as const },
        }[action];

        modalApi.confirm({
            title: config.title,
            icon: action === "disable" ? <StopOutlined /> : action === "unlock" ? <UnlockOutlined /> : <CheckCircleOutlined />,
            content: config.content,
            okText: config.okText,
            okButtonProps: { danger: action === "disable" },
            cancelText: "取消",
            onOk: () => {
                updateUserStatus(user, config.status);
                messageApi.success(`${user.username} 已${config.okText.slice(2)}`);
            },
        });
    };

    const confirmResetPassword = (user: UserItem) => {
        modalApi.confirm({
            title: `重置 ${user.username} 的密码`,
            icon: <KeyOutlined />,
            content: "重置后，原密码立即失效。系统将生成仅显示一次的临时密码。",
            okText: "确认重置",
            cancelText: "取消",
            onOk: () => setIssuedCredential({
                username: user.username,
                temporaryPassword: `Ink-${crypto.randomUUID().slice(0, 8)}!`,
            }),
        });
    };

    const closeCreate = () => {
        setCreateOpen(false);
        setCreatedAccount(null);
        createForm.resetFields();
    };

    const createUser = async () => {
        const values = await createForm.validateFields();
        const username = values.username.trim();
        const temporaryPassword = `Ink-${crypto.randomUUID().slice(0, 8)}!`;

        setUsers((current) => [
            {
                key: crypto.randomUUID(),
                username,
                role: values.role,
                status: "active",
                lastLogin: "从未登录",
                createdTime: "刚刚",
            },
            ...current,
        ]);
        setCreatedAccount({ username, temporaryPassword });
    };

    return (
        <ResourceListPage<UserItem>
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
            filters={(
                <>
                    <Select
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
                    <Select
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
            )}
            refreshLabel="刷新用户"
            searchValue={searchText}
            searchPlaceholder="搜索用户名"
            onSearchChange={setSearchText}
            paginationResetKey={`${searchText}:${status}:${role}`}
            dataSource={filteredUsers}
            rowKey="key"
            tableScrollX={800}
            totalLabel={(total) => `共 ${total} 个用户`}
            columns={[
                { title: "用户名", dataIndex: "username", width: 200 },
                {
                    title: "角色",
                    dataIndex: "role",
                    width: 120,
                    render: (value: UserItem["role"]) => (
                        <Tag color={value === "Admin" ? "blue" : "default"}>{value}</Tag>
                    ),
                },
                {
                    title: "状态",
                    dataIndex: "status",
                    width: 120,
                    render: (value: UserItem["status"]) => (
                        <Tag
                            color={value === "disabled" ? "default" : value === "locked" ? "warning" : "success"}
                            icon={value === "disabled" ? <StopOutlined /> : value === "locked" ? <LockOutlined /> : undefined}
                        >
                            {value === "disabled" ? "已禁用" : value === "locked" ? "已锁定" : "正常"}
                        </Tag>
                    ),
                },
                { title: "最后登录", dataIndex: "lastLogin", width: 180 },
                { title: "创建时间", dataIndex: "createdTime", width: 180 },
                {
                    title: "操作",
                    key: "actions",
                    width: 100,
                    fixed: "right",
                    align: "center",
                    className: "inkwell-action-column",
                    render: (_, user) => (
                        <ResourceRowAction
                            label={`管理 ${user.username}`}
                            text="管理"
                            icon={<SettingOutlined />}
                            onClick={() => {
                                setManagedUser(user);
                                setIssuedCredential(null);
                            }}
                        />
                    ),
                },
            ]}
        >
            {contextHolder}
            {modalContextHolder}
            <Modal
                title={createdAccount ? "用户已添加" : "添加用户"}
                open={createOpen}
                width={520}
                closable={!createdAccount}
                maskClosable={false}
                onCancel={closeCreate}
                footer={
                    createdAccount
                        ? (
                            <Button type="primary" onClick={closeCreate}>
                                完成
                            </Button>
                        )
                        : undefined
                }
                okText="添加用户"
                cancelText="取消"
                onOk={createdAccount ? undefined : createUser}
            >
                {createdAccount ? (
                    <Space orientation="vertical" size="large" style={{ width: "100%" }}>
                        <Alert
                            type="success"
                            showIcon
                            icon={<CheckCircleOutlined />}
                            title={`${createdAccount.username} 已创建`}
                            description="请立即将临时密码交给该用户。关闭此窗口后，临时密码将不再显示。"
                        />
                        <div>
                            <Typography.Text type="secondary">临时密码</Typography.Text>
                            <Typography.Title level={4} copyable style={{ margin: "8px 0 0" }}>
                                {createdAccount.temporaryPassword}
                            </Typography.Title>
                        </div>
                        <Typography.Text type="secondary">
                            用户首次登录后必须设置新密码。
                        </Typography.Text>
                    </Space>
                ) : (
                    <Form<CreateUserFormValues>
                        form={createForm}
                        layout="vertical"
                        initialValues={{ role: "Member" }}
                        requiredMark="optional"
                    >
                        <Form.Item
                            name="username"
                            label="用户名"
                            rules={[
                                { required: true, whitespace: true, message: "请输入用户名" },
                                { max: 100, message: "用户名不能超过 100 个字符" },
                                {
                                    validator: (_, value: string | undefined) =>
                                        value && users.some((user) => user.username.toLowerCase() === value.trim().toLowerCase())
                                            ? Promise.reject(new Error("用户名已存在"))
                                            : Promise.resolve(),
                                },
                            ]}
                        >
                            <Input autoFocus placeholder="输入用户名" autoComplete="off" />
                        </Form.Item>
                        <Form.Item name="role" label="角色" rules={[{ required: true }]}>
                            <Select
                                options={[
                                    { value: "Member", label: "Member" },
                                    { value: "Admin", label: "Admin" },
                                ]}
                            />
                        </Form.Item>
                        <Form.Item noStyle shouldUpdate={(previous, current) => previous.role !== current.role}>
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
                        <Typography.Paragraph type="secondary" style={{ margin: "20px 0 0" }}>
                            系统会生成一次性临时密码，并在创建成功后显示。
                        </Typography.Paragraph>
                    </Form>
                )}
            </Modal>
            <Modal
                title={managedUser ? `管理用户 · ${managedUser.username}` : "管理用户"}
                open={managedUser !== null}
                width={560}
                footer={null}
                onCancel={() => {
                    setManagedUser(null);
                    setIssuedCredential(null);
                }}
            >
                {managedUser && (
                    <Space orientation="vertical" size="large" style={{ width: "100%" }}>
                        <Space>
                            <Tag color={managedUser.role === "Admin" ? "blue" : "default"}>{managedUser.role}</Tag>
                            <Tag
                                color={managedUser.status === "disabled" ? "default" : managedUser.status === "locked" ? "warning" : "success"}
                            >
                                {managedUser.status === "disabled" ? "已禁用" : managedUser.status === "locked" ? "已锁定" : "正常"}
                            </Tag>
                        </Space>
                        {issuedCredential ? (
                            <Alert
                                type="success"
                                showIcon
                                title={`${issuedCredential.username} 的密码已重置`}
                                description={(
                                    <div>
                                        <Typography.Paragraph style={{ margin: "8px 0" }}>
                                            请立即将临时密码交给该用户。关闭窗口后将不再显示。
                                        </Typography.Paragraph>
                                        <Typography.Title level={4} copyable style={{ margin: 0 }}>
                                            {issuedCredential.temporaryPassword}
                                        </Typography.Title>
                                        <Typography.Text type="secondary">用户下次登录时必须设置新密码。</Typography.Text>
                                    </div>
                                )}
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
                                        <Typography.Title level={5} style={{ margin: "0 0 4px" }}>
                                            密码
                                        </Typography.Title>
                                        <Typography.Text type="secondary">
                                            生成一次性临时密码，并要求用户下次登录时设置新密码。
                                        </Typography.Text>
                                    </div>
                                    <Button icon={<KeyOutlined />} onClick={() => confirmResetPassword(managedUser)}>
                                        重置密码
                                    </Button>
                                </div>
                                <Divider style={{ margin: 0 }} />
                                <div>
                                    <Typography.Title level={5} style={{ margin: "0 0 12px" }}>
                                        登录状态
                                    </Typography.Title>
                                    {managedUser.username === "alice" ? (
                                        <Alert type="info" showIcon title="不能禁用当前登录账号。" />
                                    ) : (
                                        <Space orientation="vertical" size="middle" style={{ width: "100%" }}>
                                            {managedUser.status === "locked" ? (
                                                <Alert
                                                    type="warning"
                                                    showIcon
                                                    title="该账号因登录失败次数过多被系统自动锁定。"
                                                    action={(
                                                        <Button size="small" icon={<UnlockOutlined />} onClick={() => confirmStatusChange(managedUser, "unlock")}>
                                                            解锁
                                                        </Button>
                                                    )}
                                                />
                                            ) : null}
                                            {managedUser.status === "disabled" ? (
                                                <div style={{ display: "flex", justifyContent: "flex-end" }}>
                                                    <Button icon={<CheckCircleOutlined />} onClick={() => confirmStatusChange(managedUser, "enable")}>
                                                        启用用户
                                                    </Button>
                                                </div>
                                            ) : (
                                                <div style={{ display: "flex", justifyContent: "flex-end" }}>
                                                    <Button danger icon={<StopOutlined />} onClick={() => confirmStatusChange(managedUser, "disable")}>
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
        </ResourceListPage>
    );
}
