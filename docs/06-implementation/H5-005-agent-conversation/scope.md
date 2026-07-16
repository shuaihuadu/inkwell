---
id: H5-005
title: Agent 会话 · 实施范围
stage: H5
document_type: scope
status: draft
authors:
  - name: GitHub Copilot
    role: agent
reviewers: []
created: 2026-07-15
updated: 2026-07-15
upstream:
  - REQ-010
  - REQ-018
  - NFR-003
  - NFR-005
  - ADR-011
  - ADR-012
downstream:
  - H5-005-A
  - H5-005-B
  - H5-005-C
  - H5-005-D
---

<!-- markdownlint-disable MD025 -->

# H5-005 · Agent 会话范围

> 本文件按照 `docs/_templates/implementation-scope.template.md` 编写，用于拆分工程单元，不可直接交给 `h5-coding-executor`。`status` / `reviewers` 由 Owner 人工维护。

## 1. 目标

以后端 MAF AG-UI 为第一优先级，实现 UI-005 的真实会话、消息流和官方 TypeScript AG-UI 客户端，并使用 Ant Design X 渲染协议事件；不把原型 mock 回放逻辑带入产品。

> **2026-07-15 决策更新**：H5-005 必须按 [HD-017 §0](../../04-detailed-design/Inkwell.Core/HD-017-Inkwell.Core.Conversations.md#0-2026-07-15-当前契约替代下方冲突章节) 实施：产品 `AgentConversation` 与 MAF `AgentSession` 分离；Conversation 创建时永久锁定 `AgentVersionId`，MAF Session 首次 Run 懒建；同一 Conversation 同时只允许一个 Run，竞争请求返回 `409 Conflict`。本更新不改变本文件 `status` / `reviewers`。
>
> **2026-07-16 Session 实施暂缓**：`RoutingAgentSession` / `RoutingAgentSessionState` 已删除，实际 Agent Session 的创建、持久化和恢复留待后续讨论。当前 `RoutingAgent` 使用空占位 Session，每次 Run 只解析 Route / Claims 并调用 Core `IAgentBuildService`；本临时行为不改变产品 Conversation 已保存的 `AgentVersionId`，也不应被视为最终会话连续性方案。
>
> **2026-07-15 技术方向**：AG-UI 直接使用 MAF `MapAGUI`；不实现 Inkwell 自建 Run DTO、协议状态机或 SSE 编码器。具体挂载路径仍待 ADR-012 errata 或 Owner 拍板；WebApi 通过 endpoint filter 在 MAF handler 前完成认证、授权和租约占用，`ThreadId` 继续走 MAF 原生 Hosting → `InkwellAgentSessionStore` 链路。
>
> **2026-07-16 协议核验修正**：`@ag-ui/client@0.0.57` 的 `HttpAgent` 对标准 `RunAgentInput` 直接 `JSON.stringify`，发送 `threadId`、`runId`、`state`、完整 `messages` 快照、`tools`、`context`、`forwardedProps` 与可选 `parentRunId` / `resume`。后端直接绑定 MAF/`AGUI.Abstractions` 的标准 Model，不自建接收 DTO；撤销 `AsyncLocal` Run Context accessor 和“客户端只发送本轮新增消息”的设计。
>
> **MAF API 版本提示**：Inkwell 当前锁定包与生产代码调用 `MapAGUI`，同工作区最新 MAF `main` 已改为 `MapAGUIServer`。H5-005-B 升级依赖后必须以实际恢复版本的编译 API 为准；两者都是官方 Hosting 映射入口，本差异不改变 wire contract，也不得通过自建 endpoint 绕过。
>
> **DTO / AgentRun 结论**：AG-UI 入口 DTO 已锁定为实际 MAF 版本提供的 `AGUI.Abstractions.RunAgentInput`，输出为 AG-UI 标准 SSE 事件；Inkwell 不新增 `AgentRunRequest`、`AgentRunResponse`、`AgentRunEvent` 或产品 `AgentRun` Model / Entity / REST 资源。单次执行只用服务端 `ExecutionId` 关联租约、fencing、消息幂等和 trace，执行结束后不形成独立产品聚合。模型选择与显式调用参数由 `AgentSnapshot.ModelOptions` 固化，未设置参数使用底层 SDK 默认值；取消与超时由调用链 `CancellationToken` 和宿主策略控制。MAF `Microsoft.Agents.AI.AgentRunOptions` 是框架调用选项，不是传输 DTO，也不代表产品 `AgentRun`。

## 2. 上游依据

- `docs/01-requirements/requirements.md` REQ-010、REQ-018、NFR-003、NFR-005。
- `docs/01-requirements/ui-spec.md` §5 UI-005。
- `docs/01-requirements/acceptance-criteria.md` AC-036、AC-051、AC-060～064、AC-079、AC-084、AC-089。
- `docs/03-architecture/adr/ADR-011-auto-lock-with-inflight-task-survival.md`。
- `docs/03-architecture/adr/ADR-012-client-server-protocol-rest-agui.md`。

## 3. 当前基线

- **已有**：单 Agent 内存消息列表和 main process Chat Completions SSE 文本增量。
- **已有**：WebApi 通过 MAF `MapAGUI("/agent/{agentId}", agent)` 挂载 AG-UI。
- **已有但需替换**：当前没有持久 `AgentSessionStore`；会话状态不能跨请求可靠恢复。
- **缺失**：后端 AG-UI 契约闭环测试、状态兜底端点、服务端会话历史、TypeScript SDK、工具/Activity、取消和恢复。
- **偏差**：消息不跨设备，Renderer 数据模型只支持纯文本。
- **偏差**：真实后端直接使用 MAF `MapAGUI`，与 ADR-012 的 `/api/runs` 描述不一致；直接使用官方 Hosting 是当前技术方向，但具体挂载路径尚未拍板，后续应由有权修改 H2 ADR 的流程补充 ADR-012 errata。

## 4. 范围

- UI-005 的会话列表、消息历史、流式事件、输入区、错误、重试和锁屏恢复。

## 5. 不做范围

- 多模态三条链路归 H5-010；Trace 详情归 H5-007。

## 6. 建议工程单元

| 子任务   | 单一交付目标                                                           | 前置依赖           | 主要验证                                                                                 |
| -------- | ---------------------------------------------------------------------- | ------------------ | ---------------------------------------------------------------------------------------- |
| H5-005-A | Conversation 三模型、Repository、Service、REST 与双 Provider Mapping | HD-017 当前契约    | 三表关系、原子租约、消息批次、级联删除、双数据库并发测试                               |
| H5-005-B | 真实 TypeScript SDK 发包 → MAF AG-UI、SessionStore 与 HistoryProvider  | H5-005-A           | 九字段 JSON、opaque SessionKey、完整快照对账、StateBag Run Context、单 Run 409、真实 SSE |
| H5-005-C | Electron SDK + Ant Design X/XMarkdown + 服务端会话恢复                 | H5-005-B、H5-002-A | REST 历史、SDK reducer、第二轮仅新增持久化、跨设备恢复、Electron E2E                     |
| H5-005-D | 取消、错误、重试、断线和锁屏恢复                                       | H5-005-C           | 部分回复持久化、状态重建、AC-079、AC-089                                                 |

每个子任务执行前必须根据 `implementation-task-brief.template.md` 创建独立 `ai-task-brief.md`。

## 7. 契约与设计缺口

- 明确 AG-UI 端点输入、认证方式、事件集合和取消语义。
- 使用直接 `MapAGUI` 技术方向消除协议实现漂移，并通过 ADR errata 流程拍板具体路径、同步 ADR-012；H5 不新增自建 Run DTO 或 SSE 编码器。
- 补齐 Conversations REST API；当前 WebApi 只有协议端点，没有产品会话 Controller。
- 新增 `agent_conversations` / `agent_chat_messages` / `agent_session_states` 三表对应 Model、Repository 与 EFCore Mapping；不得继续实现 `AgentSessionDefinition` 单表模型。设计阶段不保留 Migration/Designer/Snapshot，待整体数据库设计稳定后再通过 EF Core CLI 为两个 Provider 生成全新 Initial Migration。
- 不新增 `agent_run` 表或 Inkwell `AgentRun` Model。进行中的执行由 `AgentConversation.ActiveRunId`（值为服务端 `ExecutionId`）与 `RunLeaseExpiresTime` 表示；完成消息归 `AgentChatMessage`，MAF 检查点归 `AgentSessionState`，可观测事件归后续 `Inkwell.Core.Traces`，不得再复制一份 Run 聚合状态。
- 新增 keyed singleton `InkwellAgentSessionStore` 与 `InkwellChatHistoryProvider`；Store 按 conversation key 和数据库绑定版本工作，Provider 从 MAF `ChatOptions` 中的标准 `RunAgentInput.forwardedProps.inkwell` 读取 filter 覆盖后的显式服务端上下文。两者按操作创建业务 scope，不使用 `AsyncLocal` / `IHttpContextAccessor`，不把 `threadId` 或客户端 `runId` 当授权凭证。
- 单 Run 使用数据库原子租约；endpoint filter 生成服务端 `ExecutionId`，授权后覆盖保留的 `forwardedProps.inkwell`，SSE 结果包装只负责在 `finally` 用独立清理令牌释放租约，不负责上下文传播。长 Run 只能在原租约有效时续租；所有消息/状态写入再次校验持有者与未过期条件，阻止旧 Run 在过期、清空或接管后回写。
- H5-005-B 必须先用真实 `@ag-ui/client` `HttpAgent` 发包验证 ASP.NET Core 对标准 `RunAgentInput` 的绑定，固定 camelCase 九字段、可选字段、Bearer Header、完整消息快照及 SSE Accept Header；禁止先按后端假设手写请求 JSON 再声称兼容 SDK。
- 消息批次用 `(ConversationId, RunId, RunMessageIndex)` 幂等，并在成功或停止批次提交时同步更新 `LastCommittedRunId` 与 `LastActivityTime`；SessionState 用 `Revision + RowVersion` CAS，只为完整成功 Run 保存。两条保存链路不宣称同事务，仅当两个标记均非空且 `LastRunId == LastCommittedRunId` 才恢复状态。
- H5-005-A 必须删除或替换 `AgentSessionDefinition`、`AgentSessionSummary`、`IAgentSessionRepository`、`AgentSessionRepository`、`AgentSessionEntity`、对应 Configuration / Mapping / DbSet / DI、`AgentSessionRuntime` 与旧测试；当前双 Provider Migration 及 Snapshot 全部删除，设计稳定前不重复生成。
- Agent 硬删除按 Owner 2026-07-16 决定级联删除所有 Conversation、消息与 SessionState；删除确认必须明确其跨用户且不可恢复的影响。
- Conversation 只通过 `(AgentId, AgentVersionId) → AgentVersion(AgentId, Id)` 复合外键形成 Agent → Version → Conversation 唯一级联路径，禁止同时增加 `AgentId → Agent` 直接级联 FK；双 Provider 必须验证 SQL Server 不出现 multiple cascade paths。
- 旧数据迁移规则保留为后续正式 Migration 的设计输入，但不属于当前阶段交付；正式生成全新 Initial Migration 前另行确认是否仍存在需要升级的已部署数据库。
- H5-005-A 包含单条消息 DELETE REST/Service/Repository：仅 Conversation 所属用户可操作；操作必须占用租约并在同一事务删除消息与 SessionState、清空 `LastCommittedRunId`、从剩余消息重算标题和活动时间，且不重编号 `SequenceNumber`。
- 明确锁定期间 main process 如何继续持有流并把结果缓冲给 Renderer。
- 前端使用官方 npm 包 `@ag-ui/client` 和 `@ag-ui/core`；截至 2026-07-15 最新稳定版均为 `0.0.57`，执行时必须重新查询。
- UI 使用最新稳定 `@ant-design/x` 与 `@ant-design/x-markdown`；截至 2026-07-15 均为 `2.8.0`，执行时必须重新查询。

## 8. 风险与待确认项

以下原型代码不可复用到产品运行逻辑：

- 硬编码触发词和 mock 回复。
- `setTimeout` 时间线。
- 手写 SSE 编解码演示器和静态种子历史。

原型中的气泡、Sender、Markdown 和 Activity 视觉组件可以迁移，但数据必须来自真实协议事件。

- 不允许把当前 Chat Completions `choices[].delta.content` 解析器扩展成正式状态机；它只能作为被 AG-UI 替换前的现状基线。

## 9. 功能域完成定义

- 后端 AG-UI 契约先通过真实集成测试；前端使用官方 TypeScript SDK 消费同一契约，以 Ant Design X 渲染消息、工具和 Activity；会话来自服务端并支持跨设备恢复，锁屏和错误路径不丢结果。
