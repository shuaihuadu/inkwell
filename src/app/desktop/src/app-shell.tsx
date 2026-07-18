import { Spin } from 'antd'
import { useEffect } from 'react'
import { AgentWorkspace } from './features/agent-library/agent-workspace'
import { useAuthStore } from './features/auth/auth-store'
import { ChangePasswordModal } from './features/auth/change-password-modal'
import { LockPage } from './features/auth/lock-page'
import { LoginPage } from './features/auth/login-page'
import { WorkspaceShell } from './features/shell/workspace-shell'
import { desktopApi } from './shared/network/desktop-api'

export default function AppShell() {
  const status = useAuthStore((state) => state.status)
  const identity = useAuthStore((state) => state.identity)
  const setSnapshot = useAuthStore((state) => state.setSnapshot)

  useEffect(() => {
    const unsubscribe = desktopApi.onAuthStateChanged(setSnapshot)
    void desktopApi.restoreAuth().then(setSnapshot)

    let lastReportedAt = 0
    const reportActivity = (): void => {
      const now = Date.now()
      if (now - lastReportedAt >= 30_000) {
        lastReportedAt = now
        desktopApi.reportActivity()
      }
    }
    const activityEvents: Array<keyof WindowEventMap> = ['keydown', 'pointerdown', 'wheel', 'touchstart']
    for (const eventName of activityEvents) window.addEventListener(eventName, reportActivity, { passive: true })

    return () => {
      unsubscribe()
      for (const eventName of activityEvents) window.removeEventListener(eventName, reportActivity)
    }
  }, [setSnapshot])

  useEffect(() => {
    if (status !== 'offline') return

    const retry = window.setInterval(() => {
      void desktopApi.restoreAuth().then(setSnapshot)
    }, 5_000)

    return () => window.clearInterval(retry)
  }, [setSnapshot, status])

  if (status === 'restoring') {
    return <main className="auth-state-page"><Spin size="large" /></main>
  }

  if (status === 'offline') {
    return <LoginPage initiallyOffline />
  }

  if (status === 'authenticated' || status === 'locked') {
    if (identity?.mustChangePassword) {
      return <main className="auth-state-page"><ChangePasswordModal open required /></main>
    }

    return (
      <>
        <WorkspaceShell>
          <AgentWorkspace />
        </WorkspaceShell>
        {status === 'locked' && <LockPage />}
      </>
    )
  }
  return <LoginPage />
}