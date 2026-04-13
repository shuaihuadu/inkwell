import { useEffect, useRef, useState } from "react";
import mermaid from "mermaid";
import { Spin, Alert } from "antd";

// 初始化 mermaid
mermaid.initialize({
  startOnLoad: false,
  theme: "default",
  securityLevel: "loose",
});

interface MermaidDiagramProps {
  chart: string;
}

let mermaidId = 0;

/**
 * Mermaid 图表渲染组件
 * 将 Mermaid 定义文本渲染为 SVG 图表
 */
export default function MermaidDiagram({ chart }: MermaidDiagramProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!chart || !containerRef.current) return;

    const id = `mermaid-${++mermaidId}`;
    setLoading(true);
    setError(null);

    mermaid
      .render(id, chart)
      .then(({ svg }) => {
        if (containerRef.current) {
          containerRef.current.innerHTML = svg;
        }
        setLoading(false);
      })
      .catch((err) => {
        setError(String(err));
        setLoading(false);
      });
  }, [chart]);

  if (error) {
    return (
      <div>
        <Alert type="warning" message="Mermaid 渲染失败" description={error} />
        <pre
          style={{
            background: "#f5f5f5",
            padding: 12,
            borderRadius: 8,
            marginTop: 8,
            fontSize: 12,
            overflow: "auto",
          }}
        >
          {chart}
        </pre>
      </div>
    );
  }

  return (
    <div>
      {loading && <Spin size="small" />}
      <div ref={containerRef} style={{ overflow: "auto", maxHeight: 500 }} />
    </div>
  );
}
