import { HashRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AppProvider } from './AppContext';
import AppLayout from './layouts/AppLayout';
import LockOverlay from './layouts/LockOverlay';
import UI001Login from './pages/UI001Login';
import UI002Lock from './pages/UI002Lock';
import UI003AgentLibrary from './pages/UI003AgentLibrary';
import UI004AgentConfig from './pages/UI004AgentConfig';
import UI005Conversation from './pages/UI005Conversation';
import UI006Orchestration from './pages/UI006Orchestration';
import UI007TraceDebug from './pages/UI007TraceDebug';
import UI008Version from './pages/UI008Version';
import UI009Admin from './pages/UI009Admin';

export default function App() {
  return (
    <AppProvider>
      <HashRouter>
        <LockOverlay />
        <Routes>
          {/* 登录页 / 锁定页 不走 ProLayout 外壳 */}
          <Route path="/ui-001" element={<UI001Login />} />
          <Route path="/ui-002" element={<UI002Lock />} />

          {/* 主应用外壳：顶栏 + 左侧 nav + 主区（OQ-011 closed） */}
          <Route element={<AppLayout />}>
            <Route path="/ui-003" element={<UI003AgentLibrary />} />
            <Route path="/ui-004" element={<UI004AgentConfig />} />
            <Route path="/ui-005" element={<UI005Conversation />} />
            <Route path="/ui-006" element={<UI006Orchestration />} />
            <Route path="/ui-007" element={<UI007TraceDebug />} />
            <Route path="/ui-008" element={<UI008Version />} />
            <Route path="/ui-009" element={<UI009Admin />} />
          </Route>

          <Route path="/" element={<Navigate to="/ui-001" replace />} />
          <Route path="*" element={<Navigate to="/ui-001" replace />} />
        </Routes>
      </HashRouter>
    </AppProvider>
  );
}
