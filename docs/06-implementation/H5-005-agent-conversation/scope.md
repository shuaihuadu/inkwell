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
updated: 2026-07-21
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

> **当前契约**：H5-005 必须按 [HD-017 §0](../../04-detailed-design/Inkwell.Core/HD-017-Inkwell.Core.Conversations.md#0-2026-07-15-当前契约替代下方冲突章节) 实施：产品 `AgentConversation` 与 MAF `AgentSession` 分离，Conversation 创建时永久锁定 `AgentVersionId`。Run 租约与 fencing 已从当前设计移除，不得从旧任务简报恢复。
>
> **2026-07-20 Session 当前契约（已被 2026-07-26 契约取代）**：正式 Conversation 按已锁定的 `AgentVersionId` 构建 Agent，每轮创建新的 MAF `AgentSession`。`InkwellChatHistoryProvider` 从 `AgentChatMessage` 唯一事实源恢复跨轮历史并幂等提交本轮消息；不持久化 Session checkpoint，不实现 `AgentSessionStore`。
>
> **2026-07-26 Session 当前契约**：Session checkpoint 持久化重新引入，取代上一条契约。`agent_session_states` 表以 `session_key`（`AgentConversation.SessionKey`，`Guid` 的 `"N"` 格式）为唯一键，外键指向 `agent_conversations.session_key` 并级联删除。`InkwellAgentSessionStateStore`（继承 MAF `AgentSessionStore`）独占该表行生命周期：首次保存时建行、清空会话时删行，调用方不预建空行。跨轮历史仍由 `InkwellChatHistoryProvider` 从 `AgentChatMessage` 恢复，两者并存互不替代。该 Store 以**非 keyed** 方式注册且必须保持非 keyed：MAF Hosting 通过 `GetKeyedService<AgentSessionStore>(agent.Name)` 解析并强制包裹 `IsolationKeyScopedAgentSessionStore`，后者把 id 改写为 `"{isolationKey}::{sessionStoreId}"`，会同时违反 `session_key` 的 32 字符长度上限与外键约束。
>
> **2026-08-03 Session 当前契约**：`SessionKey` 整体移除，取代上一条契约中涉及该列与 Store 基类的部分。`agent_session_states` 以 `conversation_id`（`Guid`）为唯一键，外键直接指向 `agent_conversations.id` 主键并级联删除。`InkwellAgentSessionStateStore` **不再继承** MAF `AgentSessionStore`，按具体类型注册为 Scoped 服务，直接以 `Guid conversationId` 读写；因类型不再是 `AgentSessionStore`，MAF Hosting 已不可能解析并包裹它，上一条契约中「必须保持非 keyed」的防御性约束不再适用。行生命周期独占与「跨轮历史仍由 `InkwellChatHistoryProvider` 从 `AgentChatMessage` 恢复」两项保持不变。
>
> **技术方向**：AG-UI 直接使用 MAF `MapAGUIServer`；不实现 Inkwell 自建 Run DTO、协议状态机或 SSE 编码器。具体挂载路径仍待 ADR-012 errata 或 Owner 拍板；认证与授权必须在 MAF handler 前完成，`threadId` 只用于定位产品 Conversation，不作为授权凭证。
>
> **2026-07-16 协议核验修正**：`@ag-ui/client@0.0.57` 的 `HttpAgent` 对标准 `RunAgentInput` 直接 `JSON.stringify`，发送 `threadId`、`runId`、`state`、完整 `messages` 快照、`tools`、`context`、`forwardedProps` 与可选 `parentRunId` / `resume`。后端直接绑定 MAF/`AGUI.Abstractions` 的标准 Model，不自建接收 DTO；撤销 `AsyncLocal` Run Context accessor 和“客户端只发送本轮新增消息”的设计。
>
> **DTO / AgentRun 结论**：AG-UI 入口 DTO 使用实际 MAF 版本提供的 `AGUI.Abstractions.RunAgentInput`，输出为 AG-UI 标准 SSE 事件；Inkwell 不新增产品 `AgentRun` Model / Entity / REST 资源。服务端 `ExecutionId` 只关联消息幂等和 trace，执行结束后不形成独立产品聚合。

## 2. 上游依据

- `docs/01-requirements/requirements.md` REQ-010、REQ-018、NFR-003、NFR-005。
- `docs/01-requirements/ui-spec.md` §5 UI-005。
- `docs/01-requirements/acceptance-criteria.md` AC-036、AC-051、AC-060～064、AC-079、AC-084、AC-089。
- `docs/03-architecture/adr/ADR-011-auto-lock-with-inflight-task-survival.md`。
- `docs/03-architecture/adr/ADR-012-client-server-protocol-rest-agui.md`。

## 3. 当前基线

- **已有**：单 Agent 内存消息列表和 main process Chat Completions SSE 文本增量。
- **已有**：WebApi 通过 MAF `MapAGUIServer("/agent/{agentId}", agent)` 挂载 AG-UI。
- **已有**：正式 Conversation 每轮新建 Session，并由 `InkwellChatHistoryProvider` 从服务端消息恢复跨轮历史。
- **已有**：Conversation REST、Electron IPC、聊天页和跨设备消息恢复链路。
- **已有**：取消、结构化错误与重试、主进程内有界请求快照和锁屏恢复闭环。
- **缺失**：工具/Activity，以及系统 suspend/resume 后需要独立协议设计的断线重连。
- **偏差**：真实后端直接使用 MAF `MapAGUIServer`，与 ADR-012 的 `/api/runs` 描述不一致；直接使用官方 Hosting 是当前技术方向，但具体挂载路径尚未拍板，后续应由有权修改 H2 ADR 的流程补充 ADR-012 errata。

## 4. 范围

- UI-005 的会话列表、消息历史、流式事件、输入区、错误、重试和锁屏恢复。

## 5. 不做范围

- 多模态三条链路归 H5-010；Trace 详情归 H5-007。

## 6. 建议工程单元

- **H5-005-A · 已实现**：Conversation 与 Message 两模型、Repository、Service、REST、外部历史 Provider 与双 Provider Mapping；`RemoveAgentSessionState` migration 曾将持久化收敛为两表，现按 2026-07-26 Session 当前契约重新引入 `agent_session_states` 第三张表。
- **H5-005-B · 已实现**：使用正式 Conversation 路由验证标准消息、认证授权、真实 SSE 和外部历史恢复。
- **H5-005-C · 已实现**：接入 Electron IPC、Ant Design X/XMarkdown 和服务端消息历史，验证跨设备恢复和 Electron E2E。
- **H5-005-D · 已实现**：实现取消、错误、新 ID 重试、主进程存活期间的有界状态快照和锁屏恢复，并通过真实 Electron E2E 验证 AC-079 和 AC-089。

每个子任务执行前必须根据 `implementation-task-brief.template.md` 创建独立 `ai-task-brief.md`。

## 7. 契约与设计缺口

- 明确 AG-UI 端点输入、认证方式、事件集合和取消语义。
- 使用直接 `MapAGUIServer` 技术方向消除协议实现漂移，并通过 ADR errata 流程拍板具体路径、同步 ADR-012；H5 不新增自建 Run DTO 或 SSE 编码器。
- 补齐 Conversations REST API；当前 WebApi 只有协议端点，没有产品会话 Controller。
- Conversation 持久化包含 `agent_conversations` / `agent_chat_messages` / `agent_session_states` 三表及对应 Model、Repository 与 EFCore Mapping；不得恢复旧 `AgentSessionDefinition` 模型。
- 不新增 `agent_run` 表或 Inkwell `AgentRun` Model。完成消息归 `AgentChatMessage`，可观测事件归后续 `Inkwell.Core.Traces`。
- `InkwellChatHistoryProvider` 负责跨轮历史；`InkwellAgentSessionStateStore` 负责 Session checkpoint。两者职责不重叠：前者以 `AgentChatMessage` 为唯一事实源重建对话历史，后者只存取 MAF 序列化的 Session 状态。
- H5-005-B 必须先用真实 `@ag-ui/client` `HttpAgent` 发包验证 ASP.NET Core 对标准 `RunAgentInput` 的绑定，固定 camelCase 九字段、可选字段、Bearer Header、完整消息快照及 SSE Accept Header；禁止先按后端假设手写请求 JSON 再声称兼容 SDK。
- 消息批次用 `(ConversationId, RunId, RunMessageIndex)` 幂等，并在成功批次提交时同步更新 `LastCommittedRunId` 与 `LastActivityTime`；下一轮从持久消息恢复历史。
- H5-005-A 已删除或替换旧 `AgentSessionDefinition` 链路，并已生成双 Provider Initial Migration；后续不得把旧单表 Session 语义恢复进产品。
- Agent 硬删除按 Owner 2026-07-16 决定级联删除所有 Conversation 与消息；删除确认必须明确其跨用户且不可恢复的影响。
- Conversation 只通过 `(AgentId, AgentVersionId) → AgentVersion(AgentId, Id)` 复合外键形成 Agent → Version → Conversation 唯一级联路径，禁止同时增加 `AgentId → Agent` 直接级联 FK；双 Provider 必须验证 SQL Server 不出现 multiple cascade paths。
- 旧数据迁移规则保留为后续正式 Migration 的设计输入，但不属于当前阶段交付；正式生成全新 Initial Migration 前另行确认是否仍存在需要升级的已部署数据库。
- 单条消息 DELETE 仅允许 Conversation 所属用户操作；必须在同一事务删除消息、清空 `LastCommittedRunId`、从剩余消息重算标题和活动时间，且不重编号 `SequenceNumber`。
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
