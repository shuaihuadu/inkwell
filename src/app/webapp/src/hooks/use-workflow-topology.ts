import { useCallback, useState } from "react";
import { API_BASE } from "../services/api";

export interface WorkflowTopologyData {
  id: string;
  name: string;
  format: string;
  topology: string;
}

export interface UseWorkflowTopologyReturn {
  visible: boolean;
  loading: boolean;
  data: WorkflowTopologyData | null;
  openTopology: (workflowId: string) => Promise<void>;
  closeTopology: () => void;
}

/**
 * Workflow 拓扑图状态管理：统一处理打开、关闭和加载逻辑
 */
export function useWorkflowTopology(
  onError?: (error: Error) => void,
): UseWorkflowTopologyReturn {
  const [visible, setVisible] = useState(false);
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState<WorkflowTopologyData | null>(null);

  const openTopology = useCallback(
    async (workflowId: string) => {
      setVisible(true);
      setLoading(true);

      try {
        const response = await fetch(
          `${API_BASE}/api/workflows/${workflowId}/topology`,
        );

        if (!response.ok) {
          throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const payload = (await response.json()) as WorkflowTopologyData;
        setData(payload);
      } catch (error) {
        onError?.(error as Error);
      } finally {
        setLoading(false);
      }
    },
    [onError],
  );

  const closeTopology = useCallback(() => {
    setVisible(false);
  }, []);

  return {
    visible,
    loading,
    data,
    openTopology,
    closeTopology,
  };
}
