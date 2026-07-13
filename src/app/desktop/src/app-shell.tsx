import { AgentWorkspace } from './features/agent-library/agent-workspace'
import { useAuthStore } from './features/auth/auth-store'
import { LoginPage } from './features/auth/login-page'

export default function AppShell() {
  const session = useAuthStore((state) => state.session)
  return session ? <AgentWorkspace /> : <LoginPage />
}