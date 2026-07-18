import { useState } from "react";
import {
    Modal,
    Select,
    Tag,
    Typography,
    message,
} from "antd";
import {
    LockOutlined,
    UnlockOutlined,
} from "@ant-design/icons";
import ResourceListPage, { ResourceRowAction } from "../components/ResourceListPage";

interface UserItem {
    key: string;
    username: string;
    role: "Admin" | "Member";
    status: "active" | "locked";
    lastLogin: string;
    createdTime: string;
}

const INITIAL_USERS: UserItem[] = [
    { key: "alice", username: "alice", role: "Admin", status: "active", lastLogin: "2026-07-18 14:56", createdTime: "2026-05-01 09:00" },
    { key: "bob", username: "bob", role: "Member", status: "locked", lastLogin: "2026-07-17 18:20", createdTime: "2026-05-03 10:15" },
    { key: "carol", username: "carol", role: "Member", status: "active", lastLogin: "2026-07-18 11:32", createdTime: "2026-05-08 13:40" },
    ...Array.from({ length: 22 }, (_, index): UserItem => ({
        key: `member-${index + 1}`,
        username: `member-${String(index + 1).padStart(2, "0")}`,
        role: index % 11 === 0 ? "Admin" : "Member",
        status: index % 7 === 0 ? "locked" : "active",
        lastLogin: index % 6 === 0 ? "从未登录" : `2026-07-${String(17 - (index % 12)).padStart(2, "0")} 09:20`,
        createdTime: `2026-06-${String((index % 25) + 1).padStart(2, "0")} 10:00`,
    })),
];

export default function UserManagementPage() {
    const [users, setUsers] = useState(INITIAL_USERS);
    const [searchText, setSearchText] = useState("");
    const [status, setStatus] = useState("all");
    const [role, setRole] = useState("all");
    const [messageApi, contextHolder] = message.useMessage();
    const [modalApi, modalContextHolder] = Modal.useModal();

    const filteredUsers = users.filter((user) => {
        const matchesText = user.username.toLowerCase().includes(searchText.trim().toLowerCase());
        const matchesStatus = status === "all" || user.status === status;
        const matchesRole = role === "all" || user.role === role;
        return matchesText && matchesStatus && matchesRole;
    });

    const unlock = (user: UserItem) => {
        modalApi.confirm({
            title: `解封账号 ${user.username}`,
            icon: <UnlockOutlined />,
            content: "解封后，该用户可以立即重新尝试登录。",
            okText: "确认解封",
            cancelText: "取消",
            onOk: () => {
                setUsers((current) =>
                    current.map((item) =>
                        item.key === user.key ? { ...item, status: "active" } : item,
                    ),
                );
                messageApi.success(`${user.username} 已解封`);
            },
        });
    };

    return (
        <ResourceListPage<UserItem>
            title="用户管理"
            description="查看部署内账号与锁定状态。账号创建、删除、密码和角色变更由运维流程处理。"
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
                        <Tag color={value === "locked" ? "error" : "success"} icon={value === "locked" ? <LockOutlined /> : undefined}>
                            {value === "locked" ? "已锁定" : "正常"}
                        </Tag>
                    ),
                },
                { title: "最后登录", dataIndex: "lastLogin", width: 180 },
                { title: "创建时间", dataIndex: "createdTime", width: 180 },
                {
                    title: "操作",
                    key: "actions",
                    width: 92,
                    fixed: "right",
                    align: "center",
                    className: "inkwell-action-column",
                    render: (_, user) =>
                        user.status === "locked" ? (
                            <ResourceRowAction
                                label={`解封 ${user.username}`}
                                text="解封"
                                icon={<UnlockOutlined />}
                                onClick={() => unlock(user)}
                            />
                        ) : (
                            <Typography.Text type="secondary">-</Typography.Text>
                        ),
                },
            ]}
        >
            {contextHolder}
            {modalContextHolder}
        </ResourceListPage>
    );
}
