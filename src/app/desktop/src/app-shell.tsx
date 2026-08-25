import { Modal, Spin } from "antd";
import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { AgentEditor } from "./features/agent-library/agent-editor";
import { AgentWorkspace } from "./features/agent-library/agent-workspace";
import { useAuthStore } from "./features/auth/auth-store";
import { ChangePasswordModal } from "./features/auth/change-password-modal";
import { LockPage } from "./features/auth/lock-page";
import { LoginPage } from "./features/auth/login-page";
import { ChatPanel } from "./features/chat/chat-panel";
import { GlobalApiAlert } from "./features/shell/global-api-alert";
import { WorkspaceShell } from "./features/shell/workspace-shell";
import { useNetworkStore } from "./features/shell/network-store";
import { desktopApi } from "./shared/network/desktop-api";
import type { AgentListItem } from "./shared/network/contracts";

type AgentView =
    | { kind: "space" }
    | { kind: "editor"; agentId: string | null }
    | { kind: "chat"; agent: AgentListItem; returnAgentId: string };

export default function AppShell() {
    const { t } = useTranslation();
    const status = useAuthStore((state) => state.status);
    const identity = useAuthStore((state) => state.identity);
    const setSnapshot = useAuthStore((state) => state.setSnapshot);
    const setConnection = useNetworkStore((state) => state.setConnection);
    const setGlobalError = useNetworkStore((state) => state.setGlobalError);
    const [agentView, setAgentView] = useState<AgentView>({ kind: "space" });
    const [agentEditorDirty, setAgentEditorDirty] = useState(false);

    const leaveAgentEditor = (onLeave: () => void): void => {
        if (agentView.kind !== "editor" || !agentEditorDirty) {
            setAgentEditorDirty(false);
            onLeave();
            return;
        }

        Modal.confirm({
            title: t("editor.unsavedTitle"),
            content: t("editor.unsavedContent"),
            okText: t("editor.leave"),
            okButtonProps: { danger: true },
            cancelText: t("editor.continueEditing"),
            onOk: () => {
                setAgentEditorDirty(false);
                onLeave();
            },
        });
    };

    const returnToAgentSpace = (): void => {
        leaveAgentEditor(() => setAgentView({ kind: "space" }));
    };

    useEffect(() => {
        const unsubscribeAuth = desktopApi.onAuthStateChanged(setSnapshot);
        const unsubscribeConnection =
            desktopApi.onConnectionStateChanged(setConnection);
        const unsubscribeGlobalError =
            desktopApi.onGlobalApiError(setGlobalError);
        void desktopApi.restoreAuth().then(setSnapshot);
        void desktopApi.getConnectionState().then(setConnection);

        let lastReportedAt = 0;
        const reportActivity = (): void => {
            const now = Date.now();
            if (now - lastReportedAt >= 30_000) {
                lastReportedAt = now;
                desktopApi.reportActivity();
            }
        };
        const activityEvents: Array<keyof WindowEventMap> = [
            "keydown",
            "pointerdown",
            "wheel",
            "touchstart",
        ];
        for (const eventName of activityEvents)
            window.addEventListener(eventName, reportActivity, {
                passive: true,
            });

        return () => {
            unsubscribeAuth();
            unsubscribeConnection();
            unsubscribeGlobalError();
            for (const eventName of activityEvents)
                window.removeEventListener(eventName, reportActivity);
        };
    }, [setConnection, setGlobalError, setSnapshot]);

    useEffect(() => {
        if (status !== "offline") return;

        const retry = window.setInterval(() => {
            void desktopApi.restoreAuth().then(setSnapshot);
        }, 5_000);

        return () => window.clearInterval(retry);
    }, [setSnapshot, status]);

    if (status === "restoring") {
        return (
            <>
                <GlobalApiAlert />
                <main className="auth-state-page">
                    <Spin size="large" />
                </main>
            </>
        );
    }

    if (status === "offline") {
        return (
            <>
                <GlobalApiAlert />
                <LoginPage initiallyOffline />
            </>
        );
    }

    if (status === "authenticated" || status === "locked") {
        if (identity?.mustChangePassword) {
            return (
                <>
                    <GlobalApiAlert />
                    <main className="auth-state-page">
                        <ChangePasswordModal open required />
                    </main>
                </>
            );
        }

        return (
            <>
                <GlobalApiAlert />
                <WorkspaceShell
                    onNavigate={(navigate) =>
                        leaveAgentEditor(() => {
                            setAgentView({ kind: "space" });
                            navigate();
                        })
                    }
                >
                    {agentView.kind === "space" && (
                        <AgentWorkspace
                            onCreateAgent={() =>
                                setAgentView({ kind: "editor", agentId: null })
                            }
                            onEditAgent={(agentId) =>
                                setAgentView({ kind: "editor", agentId })
                            }
                        />
                    )}
                    {agentView.kind === "editor" && (
                        <AgentEditor
                            agentId={agentView.agentId}
                            onBack={returnToAgentSpace}
                            onClone={(agentId) => {
                                setAgentEditorDirty(false);
                                setAgentView({ kind: "editor", agentId });
                            }}
                            onDirtyChange={setAgentEditorDirty}
                        />
                    )}
                    {agentView.kind === "chat" && (
                        <main className="agent-chat-workspace">
                            <ChatPanel
                                agent={agentView.agent}
                                onClose={() =>
                                    setAgentView({
                                        kind: "editor",
                                        agentId: agentView.returnAgentId,
                                    })
                                }
                            />
                        </main>
                    )}
                </WorkspaceShell>
                {status === "locked" && <LockPage />}
            </>
        );
    }
    return (
        <>
            <GlobalApiAlert />
            <LoginPage />
        </>
    );
}
