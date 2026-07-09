---
id: ADR-006-orchestration-canvas-react-flow
stage: H2
status: reviewed
authors:
  - name: H2-ArchitectAdvisor
    role: agent
reviewers: [ Inkwell ]
created: 2026-05-08
updated: 2026-05-08
upstream:
  - REQ-inkwell-agent-platform
  - repo-impact-map-inkwell-agent-platform
  - ADR-001
  - ADR-003
  - OQ-013
downstream: []
---

# ADR-006 编排画布：React Flow + Microsoft Agent Framework Workflows

> **2026-07-09 决策更新（v1 推迟至下一期 v2）**：Owner 决定 v1 不做触发器（REQ-011）与多 Agent 协作 / 编排（REQ-012）功能（详见 [requirements.md §13 第 28 条](../../01-requirements/requirements.md)）。本 ADR 的选型结论（React Flow + MAF Workflows + DurableTask）保留不删，作为 v2 重新立项时的候选输入；v1 不实施本 ADR 描述的任何设计。

## 上下文

[REQ-012 多 Agent 协作 / 编排](../../01-requirements/requirements.md) + [UI-006 编排画布](../../01-requirements/ui-spec.md) 要求：

- 拖拽节点（Agent / GroupChat / Loop）
- 节点之间画连线，连线带条件表达式，扇入扇出，条件判断
- 画布缩放、平移、对齐、撤销 / 重做
- 节点版本锁定（[REQ-012 §3](../../01-requirements/requirements.md)）
- Agent 节点必须显示当前快照版本（不会因模型 / Skill 升级而行为漂移）

[OQ-013 closed §A](../../01-requirements/open-questions.md) 已锁"画布交互参考 React Flow"。后端编排执行引擎在 [ADR-003](./ADR-003-agent-engine-microsoft-agent-framework.md) 锁定为 Microsoft Agent Framework Workflows。

## 决策

**前端使用 [React Flow](https://reactflow.dev/)（NPM 包名 [`@xyflow/react`](https://www.npmjs.com/package/@xyflow/react) 12+，截至 2026-05 最新稳定版）渲染编排画布；后端使用 [Microsoft.Agents.AI.Workflows](../../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.Workflows/) + [Microsoft.Agents.AI.DurableTask](../../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.DurableTask/) 执行编排 DAG。**

- 前端：React Flow + 自定义 NodeTypes（Agent / Tool / Skill / Branch / Loop / Output）+ EdgeTypes（条件边 / 默认边）。
- 中间表示（IR）：客户端把画布序列化为 [`Inkwell.Orchestrations.GraphDefinition`](../../01-requirements/repo-impact-map.md) DTO（节点列表 + 边列表 + Agent 版本锁定字段）。
- 后端：[`Inkwell.Orchestrations.WorkflowCompiler`](../../01-requirements/repo-impact-map.md) 把 GraphDefinition 翻译为 MAF Workflows 的 `Workflow<TInput, TOutput>`，使用 `WorkflowBuilder` 构造执行图。
- 持久化：每次执行创建一个 `Inkwell.Orchestrations.RunRecord`（DurableTask history + UI 快照），失败重启从最近 checkpoint 续作。
- Agent 版本锁定：节点保存的是"Agent ID + Snapshot Hash"，运行时由后端按快照取 Agent 配置（不是当前最新配置）。

## 备选项

### 备选 A：自研 SVG / Canvas 画布

- **放弃理由**：(1) 拖拽 / 连线 / 缩放 / 平移 / 撤销 等基础能力工程量大，与 [OQ-006](../../01-requirements/open-questions.md) v1 范围风险冲突；(2) 与 [OQ-013 closed §A](../../01-requirements/open-questions.md) 决策冲突；(3) React Flow 在性能（300+ 节点流畅）上经过社区验证。

### 备选 B：DSL / YAML 文件编辑（无可视化画布）

- **放弃理由**：(1) [UI-006](../../01-requirements/ui-spec.md) 明确画布交互；(2) v1 用户群偏向"低代码 / 无代码"，纯 YAML 路径门槛过高。

### 备选 C：用 [BPMN.js](https://bpmn.io/toolkit/bpmn-js/)（业务流程引擎）

- **放弃理由**：(1) BPMN 标准节点类型与"Agent / Tool / Skill"语义错位；(2) BPMN.io 的视觉风格偏传统企业应用，与 [OQ-011 closed §A](../../01-requirements/open-questions.md) Ant Design Pro 风格不协调；(3) 节点扩展机制对 React 19 + Server Components 模型不友好。

### 备选 D：用 LangGraph 替代 MAF Workflows 后端

- **放弃理由**：与 [ADR-003](./ADR-003-agent-engine-microsoft-agent-framework.md) 决策冲突；引入 Python 跨语言调用。

## 后果

### 正面

- React Flow + MAF Workflows 各司其职：前端只负责画布交互 + 序列化，后端只负责 DAG 执行，IR 是清晰的契约边界。
- DurableTask 提供跨 Pod 重启续作能力 → AKS HPA 弹性伸缩不会丢失正在执行的编排。
- React Flow（`@xyflow/react`）12+ 内置选区、撤销 / 重做、对齐网格、迷你地图等高级特性，节省 H5 工期。
- Agent 节点的"快照版本锁定"完全由后端按 ID + Hash 落库，前端不需要复杂的版本管理代码。

### 负面

- React Flow 商业版才支持完整的"自动布局算法"；v1 使用社区版 + 简单 dagre 自动布局，复杂 DAG 视觉效果可能需要手动调整。通过 H3 详细设计裁剪节点 / 边复杂度缓解。
- IR 与后端 Workflow 之间的"翻译器"是潜在 bug 富集点 — 把 IR JSON Schema 写死并加 contract test 缓解（H5 任务）。

### 中性

- 节点类型扩展（v1 固定 6 种 NodeType）走配置 + 代码同步，不做 plugin 化（与 [ADR-010](./ADR-010-skill-loading-static-only-v1.md) 一致）。
- 画布的"撤销 / 重做"边界仅限单次会话，不持久化到服务端（与 [REQ-006 历史会话](../../01-requirements/requirements.md) 不交叉）。

## 状态

- **状态**：accepted
- **首次发布**：2026-05-08
- **关联**：supersedes 无；上游 [ADR-001](./ADR-001-client-runtime-electron-react.md) / [ADR-003](./ADR-003-agent-engine-microsoft-agent-framework.md) / [OQ-013](../../01-requirements/open-questions.md)
- **置信度**：high（OQ-013 已 closed；MAF Workflows 在 microsoft/agent-framework 已有完整示例）
