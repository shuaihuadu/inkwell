import { Modal, Spin } from "antd";
import MermaidDiagram from "./mermaid-diagram";
import type { WorkflowTopologyData } from "../hooks/use-workflow-topology";

interface WorkflowTopologyModalProps {
  visible: boolean;
  loading: boolean;
  data: WorkflowTopologyData | null;
  onClose: () => void;
}

export default function WorkflowTopologyModal({
  visible,
  loading,
  data,
  onClose,
}: WorkflowTopologyModalProps) {
  return (
    <Modal
      title={`拓扑图 — ${data?.name ?? ""}`}
      open={visible}
      onCancel={onClose}
      footer={null}
      width={700}
    >
      {loading ? (
        <Spin />
      ) : (
        <MermaidDiagram
          chart={
            data?.topology ??
            'graph LR\n  empty["\u65E0\u62D3\u6251\u6570\u636E"]'
          }
        />
      )}
    </Modal>
  );
}
