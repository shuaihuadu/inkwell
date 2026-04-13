import { Routes, Route, Navigate } from "react-router-dom";
import AppLayout from "./components/layout/app-layout";
import DashboardPage from "./features/dashboard/dashboard-page";
import PipelineRunPage from "./features/pipeline/pipeline-run-page";
import WorkflowPage from "./features/workflow/workflow-page";
import KnowledgePage from "./features/knowledge/knowledge-page";

function App() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/chat" element={<PipelineRunPage />} />
        <Route path="/workflows" element={<WorkflowPage />} />
        <Route path="/knowledge" element={<KnowledgePage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}

export default App;
