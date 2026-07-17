---
id: H5-005-A
title: Agent Conversation 数据与 REST 基座 · AI 任务简报
stage: H5
document_type: task-brief
status: reviewed
implementation_state: superseded
authors:
  - name: GitHub Copilot
    role: agent
reviewers: [Inkwell]
created: 2026-07-16
updated: 2026-07-16
upstream:
  - REQ-010
  - REQ-018
  - NFR-005
  - ADR-004
  - ADR-011
  - ADR-012
  - ADR-017
  - ADR-021
  - ADR-022
  - HD-017
  - HD-021
tests: []
downstream:
  - H5-005-B
---

<!-- markdownlint-disable MD025 -->

# H5-005-A · Agent Conversation 数据与 REST 基座任务简报

> **当前状态**：本简报对应的三模型、Repository、Service、REST 和双 Provider Mapping 已经实现。HD-017 于 2026-07-17 移除了 Run Lease 与 fencing，实际代码也不再包含相关字段和端口；因此本文件仅保留为历史任务输入，不得再次交给编码 Agent 执行。当前事实与剩余验证见同目录 `implementation-record.md`。
>
> 本文件严格按照 `docs/_templates/implementation-task-brief.template.md` 编写，可在人工确认范围后交给 `h5-coding-executor`。
>
> `status` / `reviewers` 由 Owner 人工维护，Agent 不代签。

## 1. 任务目标

用 `AgentConversation`、`AgentChatMessage`、`AgentSessionState` 三模型替换旧 `AgentSessionDefinition` 单表设计，交付 Repository、Service、产品 REST、双 Provider EF Core Mapping 与测试基座。当前设计阶段不生成 Migration；待整体数据库设计稳定后再为两个 Provider 生成全新 Initial Migration。同一 Conversation 的 Run 占用、消息写入和破坏性操作由数据库租约与 fencing 保护。

## 2. 不做范围

- 不实现 MAF `AgentSessionStore`、`ChatHistoryProvider`、AG-UI endpoint filter、SSE 生命周期或 StateBag Run Context；归 H5-005-B。
- 不修改 Electron、`@ag-ui/client`、Ant Design X、锁屏恢复或前端会话 UI；归 H5-005-C/D。
- 不新增 `AgentRun` Model、Entity、表、REST DTO 或自建 AG-UI Run/SSE DTO。
- 不实现多模态、Trace、审计日志、消息自动裁剪或 v2 编排能力。

## 3. 上游设计引用

- `AGENTS.md` §3.1～3.3、§5：三层拓扑、Provider 依赖、v1 禁区、EF Migration 与签字位规则。
- `.github/copilot-instructions.md`：WebApi/Service/Repository 分层、C# 命名、原生 JSON 列和测试规范。
- `docs/01-requirements/requirements.md` REQ-010、REQ-018、NFR-005、§8.4、§13 决策更新 33：会话真值、用户数据所有权及 Agent 硬删除级联历史。
- `docs/01-requirements/ui-spec.md` §5.4、§5.8：单条删除、清空、Conversation Owner 与 Agent Owner 的权限区别。
- `docs/03-architecture/adr/ADR-004-data-store-provider-switchable-ef-core.md`、`ADR-017-backend-module-topology-ports-and-adapters.md`、`ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md`、`ADR-022-entity-domain-mapper-selection.md`。
- `docs/04-detailed-design/Inkwell.Core/HD-017-Inkwell.Core.Conversations.md` §0：当前且优先的模型、端口、租约、事务、权限和恢复契约。
- `docs/04-detailed-design/Inkwell.WebApi/HD-021-Inkwell.WebApi-authentication-and-agent-routing.md` §0.1：产品 Conversation REST 契约与 HTTP 映射。
- `docs/04-detailed-design/database-design.md` Conversations、Messages、Session State 及“从旧 agent_session 单表迁移”：字段、复合外键、JSON 类型和升级规则。
- `docs/06-implementation/H5-005-agent-conversation/scope.md` §6～7：工程单元边界和阻塞约束。

冲突时以上文件中的“当前契约”或日期更晚的决策更新优先于 HD-017 下方旧章节；不得恢复旧 `AgentSessionDefinition` 语义。

## 4. 测试引用

暂无独立 H4 TC；临时以 `acceptance-criteria.md` AC-036、AC-051、AC-060～AC-064、AC-084 及本简报 §9 为验证依据。H4 测试矩阵缺口记录于 §12，但不免除本任务的单元、Provider 集成与 WebApi 测试。

## 5. 当前基线与问题

### 5.1 当前实现

- `Inkwell.Abstractions/Agent/AgentSessionDefinition.cs` 与 `AgentSessionSummary.cs` 混合产品会话元数据和 MAF 状态。
- `IAgentSessionRepository` / `IAgentSessionMessageRepository`、EF Entity/Mapping/Configuration/Repository 及 `AgentSessionRuntime` 仍围绕旧模型。
- 双 Provider Migration、Designer 与 ModelSnapshot 已按 Owner 2026-07-16 决定全部删除，当前只维护模型与 Mapping。
- WebApi 只有 Agent/Version 产品端点与 AG-UI 协议入口，没有独立 Conversation REST Controller。

### 5.2 待解决问题

1. 产品 Conversation、规范消息和可丢弃 MAF 检查点必须分表、分模型并遵守原生 JSON 列约束。
2. 多副本 WebApi 下的单 Run、续租、提交、单删、清空和删除必须使用数据库原子条件与 fencing，不能依赖进程锁。
3. SQL Server 必须只有 `Agent → AgentVersion → AgentConversation` 一条级联路径，同时数据库保证 Conversation 的版本属于该 Agent。
4. 后续生成全新 Initial Migration 前，必须重新确认部署基线和数据升级要求，当前阶段不实现迁移路径。
5. REST 必须显式传入 Claims 提取的用户 ID，Service 执行业务授权，Repository 不接触 HTTP 或 Claims。

## 6. 允许修改的文件

仅允许修改或新建以下路径：

- `src/core/Inkwell.Abstractions/Agent/**`
- `src/core/Inkwell.Abstractions/Persistence/**`
- `src/core/Inkwell.Abstractions/DependencyInjection/**`
- `src/core/Inkwell.Core/Conversations/**`
- `src/core/Inkwell.Core/AgentRuntime/Sessions/AgentSessionRuntime.cs`（仅删除旧运行时或移除其注册引用）
- `src/core/Inkwell.Core/DependencyInjection/**`
- `src/core/providers/Persistence/Inkwell.Persistence.EFCore/Entities/**`
- `src/core/providers/Persistence/Inkwell.Persistence.EFCore/Configurations/**`
- `src/core/providers/Persistence/Inkwell.Persistence.EFCore/Mapping/**`
- `src/core/providers/Persistence/Inkwell.Persistence.EFCore/Repositories/**`
- `src/core/providers/Persistence/Inkwell.Persistence.EFCore/InkwellDbContext.cs`
- `src/core/providers/Persistence/Inkwell.Persistence.EFCore/DependencyInjection/**`
- `src/core/Inkwell.WebApi/Conversations/**`
- `src/core/Inkwell.WebApi/Controllers/**`
- `src/core/Inkwell.WebApi/Program.cs`（仅 Conversation REST 注册）
- `tests/Inkwell.Abstractions.Tests/**`
- `tests/Inkwell.Core.Tests/Conversations/**`
- `tests/Inkwell.Providers.Contract/**`
- `tests/Inkwell.WebApi.Tests/Controllers/**`

若实际 DI 或 DbContext 文件路径与上述目录不同，只允许对直接拥有旧 Session 注册/DbSet 的现存文件做最小修改，并在交付偏差中逐项说明。

## 7. 禁止修改

- `docs/01-requirements/**`、`docs/03-architecture/**`、`docs/04-detailed-design/**` 的 `status` / `reviewers` 及 `AGENTS.md` §1/§3。
- `src/core/providers/Persistence/Inkwell.Persistence.EFCore.SqlServer/Migrations/**` 与 `src/core/providers/Persistence/Inkwell.Persistence.EFCore.Postgres/Migrations/**`；当前阶段不得生成 Migration、Designer 或 ModelSnapshot。
- `src/core/Inkwell.WebApi/Protocols/**`、MAF AG-UI Hosting、RoutingAgent、SessionStore/HistoryProvider 新实现。
- `src/app/**`、`prototypes/**`、Traces、Multimodal、Tools、Skills 与无关 Provider。
- 不新增包或升级依赖；若编译证明当前依赖无法实现已锁定设计，阻塞返回并附证据。
- 不把 Repository 注入业务 Service；Service 必须注入 `IPersistenceProvider` 并按作用域获取具名 Repository。
- 不手写或生成 Migration/Designer/Snapshot；未来恢复 Migration 时必须使用 EF Core CLI。

若完成任务必须越出允许范围，`h5-coding-executor` 必须阻塞返回，不得自行扩大范围。

## 8. 实现要求

### 8.1 模型与端口

- 新增独立公共 Model：`AgentConversation`、`AgentChatMessage`、`AgentSessionState`；字段、nullable、RowVersion、时间和命名完全遵循 HD-017 §0 与 database-design。
- 新增 `IAgentConversationRepository`、`IAgentChatMessageRepository`、`IAgentSessionStateRepository`，方法使用已批准动词并提供原子租约、续租、释放、消息批次提交、fenced 单删/清空/删除和 SessionState CAS 的稳定结果。
- Service 操作结果使用 `{Concept}{Action}Result`，API 输入/输出使用分层命名；不得以 API Request/Response 代替 Model 或 Service Result。
- 删除旧 `AgentSessionDefinition`、`AgentSessionSummary`、旧 Repository 接口与 `AgentSessionRuntime`；清除其 DI、实现和测试引用。不得删除身份认证的 SessionToken 类型。

### 8.2 Service 与事务

- `AgentConversationService` 通过 `IPersistenceProvider` 获取 Repository，创建时解析并永久锁定当前可调用 `AgentVersionId`；列表、历史、单删、清空和删除均验证 `OwnerUserId + AgentId + ConversationId`。
- `OwnerUserId` 是 Conversation 所属参与用户，不是 Agent Owner。共享参与用户可操作自己的 Conversation；Agent Owner 不能因此读写他人的 Conversation。
- Run 租约的 acquire/renew/release 及所有 fenced 写入使用 `TimeProvider` 和数据库条件更新；有效租约竞争返回稳定冲突，不使用内存锁、Redis 或先读后写。
- 消息批次以 `(ConversationId, RunId, RunMessageIndex)` 幂等，连续分配 SequenceNumber；同键同内容重试成功，不同内容冲突。成功批次同步更新 `LastCommittedRunId`、标题和活动时间。
- 单条消息删除、清空、Conversation 删除各自先以新的服务端操作 ID 占用租约。单删在同一事务删除消息与 SessionState、清空 `LastCommittedRunId`、按剩余消息重算 Title/LastActivityTime，保留原 SequenceNumber；清空删除全部消息/状态并重置派生字段；Conversation 删除依赖数据库级联。
- SessionState 保存采用 `Revision + RowVersion` CAS，并再次校验当前未过期租约。该任务只实现产品端口和持久化行为，不实现 MAF 序列化调用方。

### 8.3 EF Core Mapping

- 一个文件一个 Entity/Configuration/Mapping/Repository；手写 `ToModel` / `ToEntity` / `SelectAsModel`，不引入 Mapper 库。
- SQL Server JSON 属性显式映射 `json`，PostgreSQL 显式映射 `jsonb`；属性和列使用 `Message`、`SessionState` 等业务名，不带 `Json` 后缀。
- `AgentVersion` 建立 `(AgentId, Id)` 唯一候选键；Conversation 仅以 `(AgentId, AgentVersionId)` 复合外键指向该键并级联，不增加独立 `AgentId → Agent` FK。Conversation 级联 Message/SessionState。
- 当前阶段不生成或维护 Migration、Designer 与 ModelSnapshot，只验证两个 Provider 的最终模型配置和 Repository 行为。
- 待整体数据库设计稳定后，分别使用 SQL Server/PostgreSQL 的 `dotnet ef migrations add` 生成全新 Initial Migration；Designer/Snapshot 不手写。
- 旧数据升级规则暂作为设计输入保留，不在当前阶段通过临时 Migration 固化。

### 8.4 WebApi REST

- 实现创建、列表、消息分页、单条消息删除、清空和 Conversation 删除端点，路由及语义遵循 HD-021 §0.1。
- Controller 只提取 Claims/路由/请求数据并调用 Service。缺失身份 401、已认证越权或 Agent 不匹配 403、资源/消息不存在 404、有效租约冲突 409；不要在 Controller 调 Repository。
- Response 不暴露 `SessionKey`、RowVersion、租约、SerializedState 或 MAF Session。

### 8.5 依赖版本策略

- 本任务不新增或升级依赖。使用当前 Central Package Management 中的 EF Core、MSTest 和 Testcontainers 版本。

## 9. 测试要求

1. Abstractions：三 Model JSON/record 往返、nullable 与稳定结果；旧 Session 公共类型不再可用由编译覆盖。
2. Core Service：创建锁版本；Conversation Owner 授权；共享用户只能操作自己的历史；不存在/越权；标题生成与重算；单删不重编号；清空/删除事务后果。
3. 租约并发：同一 Conversation 两个执行仅一个 acquire；同 Run 重试；过期接管；旧/过期持有者不能 renew、release、提交、单删、清空、删除或保存状态。
4. 消息幂等：相同 Run 批次重试不重复；同键不同内容冲突；并发序号唯一；提交同步更新 LastCommittedRunId/Title/LastActivityTime。
5. SessionState：0..1 约束、Revision/RowVersion CAS、租约丢失与并发冲突结果可区分、删除幂等。
6. 双 Provider Mapping：验证表名、原生 JSON 类型、复合外键、唯一索引和级联关系的模型元数据。
7. 双 Provider Repository：Conversation 版本必须属于同 Agent；删除 Conversation 级联两子表；删除 Agent 沿 Version 唯一路径级联全部 Conversation 历史。
8. WebApi：六类 REST 路由、DTO 边界、401/403/404/409 映射，并验证 Controller 未绕过 Service 业务授权。

测试必须触达真实被测代码。Provider 测试使用仓库现有 Testcontainers 模式；没有可用 Docker 时明确阻塞，不得用 InMemory Provider 冒充双数据库验证。

## 10. 验收命令

从仓库根目录按顺序执行：

```shell
dotnet test tests/Inkwell.Abstractions.Tests/Inkwell.Abstractions.Tests.csproj --no-restore
dotnet test tests/Inkwell.Core.Tests/Inkwell.Core.Tests.csproj --no-restore
dotnet test tests/Inkwell.WebApi.Tests/Inkwell.WebApi.Tests.csproj --no-restore
dotnet test tests/Inkwell.Providers.Contract/Inkwell.Providers.Contract.csproj --no-restore
dotnet build Inkwell.slnx --no-restore
```

执行前先确认 Docker 可用且双数据库容器测试没有被过滤或跳过。当前阶段不得执行 `dotnet ef migrations add`。

## 11. 完成标准

- 旧 Session 公共/持久化/运行时链路被三模型完整替换，两个 Provider 的 Migration 目录不包含 Migration、Designer 或 ModelSnapshot。
- REST、Service 和 Repository 的权限、冲突、未找到与幂等行为符合 §8。
- 两个 Provider 的模型配置一致且 SQL Server 无多重级联路径；级联与复合 FK 测试通过。
- 单条删除、清空、删除 Conversation 和删除 Agent 的数据后果符合设计，且旧 Run 无法回写。
- §10 全部命令通过，无新增 warning；不得使用 skip、空断言或降低 warning/analyzer 级别换取通过。
- 实际修改文件是 §6 的子集，§7 保持未修改。
- H5 Agent 返回修改文件、验证摘要、偏差和六字段提交信息草稿，但不运行 git 提交命令。

## 12. 风险、假设与待确认项

### 12.1 已知风险

- 正式 Initial Migration 延后生成；在其生成前不能验证最终建库脚本和部署升级路径。
- 原子租约/批次提交可能需要 Provider 特定条件更新实现；公共 Repository 结果和业务语义必须一致，差异限制在 EF Adapter 内。
- 旧 SessionState 被主动丢弃；消息真值完整保留，H5-005-B 首次 Run 懒重建 Session。
- H4 尚无独立 TC 编号；本任务先落可执行覆盖，后续 H4 文档必须补映射而不改变已锁定语义。

### 12.2 实施假设

- 当前 PackageReference 足以实现本任务，不需要依赖升级。
- 当前不存在 Migration 基线；整体数据库设计稳定后为两个 Provider 生成全新 Initial Migration。
- 旧 `AgentChatMessage.Message` 的 JSON 格式可由新 `AgentChatMessage` Model 原样读取；若真实样本反证，停止并报告具体 JSON 差异，不做静默数据修补。

### 12.3 待 Owner 确认

无。设计阻塞项已于 2026-07-16 完成 H3 focused review；本文件 `status` / `reviewers` 仍由 Owner 人工维护。

## 13. H5 交付格式

完成后必须在对话中提供：

1. 修改文件清单。
2. 实际执行的 EF CLI、测试、构建命令与输出摘要。
3. 与本简报的偏差及原因；无偏差则明确写“无”。
4. 六字段提交信息草稿：`Design / Tests / Verify / Docs / Risk / Task`。
