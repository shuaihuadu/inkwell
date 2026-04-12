import { Routes, Route, Navigate } from "react-router-dom";
import AppLayout from "./components/layout/app-layout";
import DashboardPage from "./features/dashboard/dashboard-page";
import PipelineRunPage from "./features/pipeline/pipeline-run-page";

function App() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/pipeline/run" element={<PipelineRunPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}

export default App;
