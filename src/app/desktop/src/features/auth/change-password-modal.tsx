import { Button, Form, Input, Modal, message } from "antd";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { desktopApi } from "../../shared/network/desktop-api";
import type { ChangePasswordRequest } from "../../shared/network/contracts";
import { useNetworkStore } from "../shell/network-store";
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
    const { t } = useTranslation();
    const [form] = Form.useForm<ChangePasswordFormValues>();
    const [submitting, setSubmitting] = useState(false);
    const setSnapshot = useAuthStore((state) => state.setSnapshot);
    const connectionStatus = useNetworkStore((state) => state.status);
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
            messageApi.success(t("auth.changePassword.success"));
            close();
        } catch (reason) {
            messageApi.error(
                t("auth.changePassword.failed", {
                    message:
                        reason instanceof Error
                            ? reason.message
                            : t("common.unknownError"),
                }),
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
                title={t("auth.changePassword.title")}
            >
                <Form<ChangePasswordFormValues>
                    form={form}
                    layout="vertical"
                    onFinish={(values) => void changePassword(values)}
                    requiredMark="optional"
                    disabled={connectionStatus !== "online"}
                >
                    <Form.Item
                        name="currentPassword"
                        label={t("auth.changePassword.currentPassword")}
                        rules={[
                            {
                                required: true,
                                message: t(
                                    "auth.changePassword.currentPasswordRequired",
                                ),
                            },
                        ]}
                    >
                        <Input.Password
                            autoComplete="current-password"
                            placeholder={t(
                                "auth.changePassword.currentPasswordPlaceholder",
                            )}
                            autoFocus
                        />
                    </Form.Item>
                    <Form.Item
                        name="newPassword"
                        label={t("auth.changePassword.newPassword")}
                        extra={t("auth.changePassword.newPasswordHelp")}
                        dependencies={["currentPassword"]}
                        rules={[
                            {
                                required: true,
                                message: t(
                                    "auth.changePassword.newPasswordRequired",
                                ),
                            },
                            {
                                min: 8,
                                max: 128,
                                message: t(
                                    "auth.changePassword.passwordLength",
                                ),
                            },
                            ({ getFieldValue }) => ({
                                validator: (_, value: string | undefined) =>
                                    value &&
                                    value === getFieldValue("currentPassword")
                                        ? Promise.reject(
                                              new Error(
                                                  t(
                                                      "auth.changePassword.passwordUnchanged",
                                                  ),
                                              ),
                                          )
                                        : Promise.resolve(),
                            }),
                        ]}
                    >
                        <Input.Password
                            autoComplete="new-password"
                            placeholder={t(
                                "auth.changePassword.newPasswordPlaceholder",
                            )}
                        />
                    </Form.Item>
                    <Form.Item
                        name="confirmPassword"
                        label={t("auth.changePassword.confirmPassword")}
                        dependencies={["newPassword"]}
                        rules={[
                            {
                                required: true,
                                message: t(
                                    "auth.changePassword.confirmPasswordRequired",
                                ),
                            },
                            ({ getFieldValue }) => ({
                                validator: (_, value: string | undefined) =>
                                    value &&
                                    value !== getFieldValue("newPassword")
                                        ? Promise.reject(
                                              new Error(
                                                  t(
                                                      "auth.changePassword.passwordMismatch",
                                                  ),
                                              ),
                                          )
                                        : Promise.resolve(),
                            }),
                        ]}
                    >
                        <Input.Password
                            autoComplete="new-password"
                            placeholder={t(
                                "auth.changePassword.confirmPasswordPlaceholder",
                            )}
                        />
                    </Form.Item>
                    <div
                        style={{ display: "flex", justifyContent: "flex-end" }}
                    >
                        <Button
                            type="primary"
                            htmlType="submit"
                            loading={submitting}
                            disabled={connectionStatus !== "online"}
                        >
                            {t("auth.changePassword.submit")}
                        </Button>
                    </div>
                </Form>
            </Modal>
        </>
    );
}
