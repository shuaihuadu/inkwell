import { Modal, Spin } from "antd";
import { useEffect, useState } from "react";
import { AgentEditor } from "./features/agent-library/agent-editor";
import { AgentWorkspace } from "./features/agent-library/agent-workspace";
import { useAuthStore } from "./features/auth/auth-store";
import { ChangePasswordModal } from "./features/auth/change-password-modal";
import { LockPage } from "./features/auth/lock-page";
import { LoginPage } from "./features/auth/login-page";
import { ChatPanel } from "./features/chat/chat-panel";
import { WorkspaceShell } from "./features/shell/workspace-shell";
import { desktopApi } from "./shared/network/desktop-api";
import type { AgentListItem } from "./shared/network/contracts";

type AgentView =
    | { kind: "space" }
    | { kind: "editor"; agentId: string | null }
    | { kind: "chat"; agent: AgentListItem; returnAgentId: string };

export default function AppShell() {
    const status = useAuthStore((state) => state.status);
    const identity = useAuthStore((state) => state.identity);
    const setSnapshot = useAuthStore((state) => state.setSnapshot);
    const [agentView, setAgentView] = useState<AgentView>({ kind: "space" });
    const [agentEditorDirty, setAgentEditorDirty] = useState(false);

    const leaveAgentEditor = (onLeave: () => void): void => {
        if (agentView.kind !== "editor" || !agentEditorDirty) {
            setAgentEditorDirty(false);
            onLeave();
            return;
        }

        Modal.confirm({
            title: "有未保存的修改",
            content: "离开后，本次修改将丢失。",
            okText: "仍然离开",
            okButtonProps: { danger: true },
            cancelText: "继续编辑",
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
        const unsubscribe = desktopApi.onAuthStateChanged(setSnapshot);
        void desktopApi.restoreAuth().then(setSnapshot);

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
            unsubscribe();
            for (const eventName of activityEvents)
                window.removeEventListener(eventName, reportActivity);
        };
    }, [setSnapshot]);

    useEffect(() => {
        if (status !== "offline") return;

        const retry = window.setInterval(() => {
            void desktopApi.restoreAuth().then(setSnapshot);
        }, 5_000);

        return () => window.clearInterval(retry);
    }, [setSnapshot, status]);

    if (status === "restoring") {
        return (
            <main className="auth-state-page">
                <Spin size="large" />
            </main>
        );
    }

    if (status === "offline") {
        return <LoginPage initiallyOffline />;
    }

    if (status === "authenticated" || status === "locked") {
        if (identity?.mustChangePassword) {
            return (
                <main className="auth-state-page">
                    <ChangePasswordModal open required />
                </main>
            );
        }

        return (
            <>
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
                                setAgentView({ kind: "editor", agentId })
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
    return <LoginPage />;
}
