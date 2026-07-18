import { Button, Form, Input, Modal, message } from "antd";
import { useState } from "react";
import { desktopApi } from "../../shared/network/desktop-api";
import type { ChangePasswordRequest } from "../../shared/network/contracts";
import { useAuthStore } from "./auth-store";

interface ChangePasswordFormValues extends ChangePasswordRequest {
    confirmPassword: string;
}

interface ChangePasswordModalProps {
    open: boolean;
    required?: boolean;
    onClose?: () => void;
}

export function ChangePasswordModal({
    open,
    required = false,
    onClose,
}: ChangePasswordModalProps) {
    const [form] = Form.useForm<ChangePasswordFormValues>();
    const [submitting, setSubmitting] = useState(false);
    const setSnapshot = useAuthStore((state) => state.setSnapshot);
    const [messageApi, contextHolder] = message.useMessage();

    const close = (): void => {
        form.resetFields();
        onClose?.();
    };

    const changePassword = async (
        values: ChangePasswordFormValues,
    ): Promise<void> => {
        setSubmitting(true);
        try {
            const identity = await desktopApi.changePassword({
                currentPassword: values.currentPassword,
                newPassword: values.newPassword,
            });
            setSnapshot({ status: "authenticated", identity });
            messageApi.success("密码已修改");
            close();
        } catch (reason) {
            messageApi.error(
                `修改失败：${reason instanceof Error ? reason.message : "未知错误"}`,
            );
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <>
            {contextHolder}
            <Modal
                open={open}
                onCancel={required ? undefined : close}
                closable={!required}
                maskClosable={false}
                keyboard={!required}
                footer={null}
                centered
                width={440}
                title="修改密码"
            >
                <Form<ChangePasswordFormValues>
                    form={form}
                    layout="vertical"
                    onFinish={(values) => void changePassword(values)}
                    requiredMark="optional"
                >
                    <Form.Item
                        name="currentPassword"
                        label="当前密码"
                        rules={[{ required: true, message: "请输入当前密码" }]}
                    >
                        <Input.Password
                            autoComplete="current-password"
                            placeholder="输入当前密码"
                            autoFocus
                        />
                    </Form.Item>
                    <Form.Item
                        name="newPassword"
                        label="新密码"
                        extra="使用 8–128 个字符，且不能与当前密码相同。"
                        dependencies={["currentPassword"]}
                        rules={[
                            { required: true, message: "请输入新密码" },
                            {
                                min: 8,
                                max: 128,
                                message: "密码长度应为 8–128 个字符",
                            },
                            ({ getFieldValue }) => ({
                                validator: (_, value: string | undefined) =>
                                    value &&
                                    value === getFieldValue("currentPassword")
                                        ? Promise.reject(
                                              new Error(
                                                  "新密码不能与当前密码相同",
                                              ),
                                          )
                                        : Promise.resolve(),
                            }),
                        ]}
                    >
                        <Input.Password
                            autoComplete="new-password"
                            placeholder="输入新密码"
                        />
                    </Form.Item>
                    <Form.Item
                        name="confirmPassword"
                        label="确认新密码"
                        dependencies={["newPassword"]}
                        rules={[
                            { required: true, message: "请再次输入新密码" },
                            ({ getFieldValue }) => ({
                                validator: (_, value: string | undefined) =>
                                    value &&
                                    value !== getFieldValue("newPassword")
                                        ? Promise.reject(
                                              new Error(
                                                  "两次输入的新密码不一致",
                                              ),
                                          )
                                        : Promise.resolve(),
                            }),
                        ]}
                    >
                        <Input.Password
                            autoComplete="new-password"
                            placeholder="再次输入新密码"
                        />
                    </Form.Item>
                    <div
                        style={{ display: "flex", justifyContent: "flex-end" }}
                    >
                        <Button
                            type="primary"
                            htmlType="submit"
                            loading={submitting}
                        >
                            修改密码
                        </Button>
                    </div>
                </Form>
            </Modal>
        </>
    );
}
