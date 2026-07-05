---
id: HD-007
title: Inkwell.Abstractions 详细设计 — Audit Logger Port（IAuditLogger facade + DTO + Options）
stage: H3
status: reviewed
reviewers: [Inkwell]
upstream:
  - REQ-001
  - REQ-002
  - REQ-007
  - REQ-008
  - REQ-013
  - REQ-014
  - REQ-015
  - REQ-017
  - NFR-004
  - ADR-002
  - ADR-008
  - ADR-017
  - ADR-023
  - HD-001
  - HD-002
---

<!-- markdownlint-disable MD060 -->
<!-- 中文 + 英文混排长表格在 markdownlint 列宽计算下字面对齐 ≠ 视觉对齐（详 /memories/markdown-lint.md，与 HD-004 / HD-005 / HD-006 同处理方式），表格仍按 docs-style §3 视觉对齐维护，机械 MD060 不予执行。 -->

> **错误处理约定**（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)，含 [errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) 废错误码、[errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 删 `Result<T>` / `Error` 抽象）：端口层与业务层统一采用裸 `Task<T>` + .NET BCL 异常。本 HD 与 [HD-004](HD-004-Inkwell.Abstractions-cache-port.md) / [HD-005](HD-005-Inkwell.Abstractions-queue-port.md) / [HD-006](HD-006-Inkwell.Abstractions-agent-runtime-port.md) 同批次沿用最终态规约，全部签名从第一版直接采用裸 `Task` / `Task<T>` + BCL 异常，不存在"先 Result 后 errata"的历史包袱。
>
> **核心约束（[AGENTS.md §3.2](../../../AGENTS.md) 已锁）**：业务命名空间通过 `IAuditLogger`（本 HD，位于 `Inkwell.Abstractions`）写审计；**写入失败不得吞错，必须走 [ADR-008](../../03-architecture/adr/ADR-008-audit-log-store-and-query.md) 失败处理路径**（重试 3 次 + 磁盘 fallback 文件 + 触发告警）。本 HD §4.2 显式落地该约束：`LogAsync` 对调用方**不**因存储故障抛异常（[picker Q-write-failure-strategy=A](#13-决策记录)），"不吞错"通过 OTel `exception.*` 记录 + 磁盘 fallback 持久化 + 告警三件套满足，而非对业务调用方静默丢弃。
>
> **范围切片**：本 HD 覆盖 `Inkwell.Abstractions/Audit/` 子层——`IAuditLogger` facade（2 方法：`LogAsync` / `QueryAsync`，[picker Q-facade-scope=A](#13-决策记录)）、写入 / 查询 / 读取 DTO（`AuditLogRequest` / `AuditLogEntry` / `AuditLogQuery` + `AuditActorType` / `AuditResultCode` 两个封闭枚举）、`AuditLoggerOptions` + Validator。**不**实现 `Inkwell.Core.AuditLogs` 具体实现（`DefaultAuditLogger` 内部重试队列 / 磁盘 fallback 文件格式 / 后台清理任务 —— 独立 `Inkwell.Core` HD 起草）、**不**锁定 `audit_logs` 表的具体字段 / 索引 / 约束（`Persistence/AuditLogs/AuditLog.cs` + `IAuditLogRepository.cs` 由 `Inkwell.Core.AuditLogs` 业务 HD 起草，[file-structure.md 模板](../file-structure.md#inkwellabstractions)已预留路径）、**不**实现 UI-009 审计页的具体检索表单（[`Inkwell.WebApi`](../../../AGENTS.md) HD 起草，本 HD 仅保证 `AuditLogQuery` 字段可 1:1 映射到 UI-009 §9.3 表单字段（筛选条件）、`AuditLogEntry` 字段可映射到 UI-009 §9.4 查看条目详情（`PayloadJson` 展示）；[2026-07-06 errata](#11-待补--待评审) 更正原"§9.4 检索表单"引用有误）。
>
> **跨 HD 关联**：本 HD 复用 [HD-001 foundation](HD-001-Inkwell.Abstractions-foundation.md) 的 `Common/AuditContext.cs`（[2026-07-05 errata·第五轮](HD-001-Inkwell.Abstractions-foundation.md#37-commonauditcontextcs) `ActorUserId` 已改 `Guid`，详 [§13.1 Q-actor-user-id-type](#13-决策记录)）+ `Common/TimeRange.cs` + `Common/Pagination.cs`、[HD-002 §3.4](HD-002-Inkwell.Abstractions-persistence-port.md#34-persistencepagedresultcs) 的 `Persistence/PagedResult.cs`（`QueryAsync` 返回值直接复用，不重复定义分页返回形态）；`InkwellProvidersOptions`（[HD-001 §3.11.1](HD-001-Inkwell.Abstractions-foundation.md#3111-optionsinkwellprovidersoptionscsf9-新增)）**不**新增 `Audit` 选择器字段——[ADR-008 §决策](../../03-architecture/adr/ADR-008-audit-log-store-and-query.md) 已锁定审计日志为单一存储策略（主 DB 同 Provider，无需切换），与 6 个可切换端口（Persistence / FileStorage / Cache / Queue / VectorStore / AgentRuntime）不同构，详 [§1.3](#13-关键决策摘要)。

## 1. 模块概述

### 1.1 职责

- `IAuditLogger` facade（§3.1）：定义业务命名空间写入 / 查询审计日志的统一入口；`LogAsync`（写入，内部异步解耦 + 重试 + 磁盘 fallback，对调用方永不因存储故障抛异常）/ `QueryAsync`（分页查询，覆盖 [NFR-004](../../01-requirements/requirements.md) / [REQ-017](../../01-requirements/requirements.md) UI-009 审计页检索场景）2 个方法（[picker Q-facade-scope=A](#13-决策记录)）
- `AuditLogRequest` DTO（§3.2）：写入请求，包裹已在 [HD-001 §3.7](HD-001-Inkwell.Abstractions-foundation.md#37-commonauditcontextcs) 定义的 `AuditContext` + 审计专属字段（`ActorType` / `AgentId?` / `ResultCode` / `ErrorCode?`）
- `AuditLogEntry` DTO（§3.3）：查询结果的单条只读记录，字段对齐 [ADR-008 §决策](../../03-architecture/adr/ADR-008-audit-log-store-and-query.md) 表结构关键字段
- `AuditLogQuery` DTO（§3.4）：查询过滤条件，覆盖 [AC-069](../../01-requirements/acceptance-criteria.md) "按用户 / Agent / 时间范围 / 事件类型任一组合筛选"
- `AuditActorType` / `AuditResultCode` 封闭枚举（§3.5）：[ADR-008](../../03-architecture/adr/ADR-008-audit-log-store-and-query.md) 锁定的 `actor_type ∈ {user, token, system}` 三值闭集 + 审计结果二值闭集
- `AuditLoggerOptions` + Validator（§3.6 ~ §3.7）：保留期、查询时间窗口上限、分页边界、`EnableSensitiveDataLogging` 开关

### 1.2 范围

- **在内**：facade 接口 + 写入 / 查询 DTO + 枚举 + Options
- **不在内**：
  - `Inkwell.Core.AuditLogs.DefaultAuditLogger` 具体实现（内部重试队列 / 磁盘 fallback 文件格式 / 后台清理任务 [`BackgroundService`](https://learn.microsoft.com/dotnet/core/extensions/workers) —— 独立 `Inkwell.Core` HD 起草，不设独立 `providers/*` csproj，[picker Q-implementation-topology](#13-决策记录) 与 [HD-006 `Inkwell.Core.AgentRuntime` 单实现拓扑](HD-006-Inkwell.Abstractions-agent-runtime-port.md) 一致）
  - `audit_logs` 表的具体字段 / 索引 / 约束（`Persistence/AuditLogs/AuditLog.cs` + `IAuditLogRepository.cs` 由 `Inkwell.Core.AuditLogs` 业务 HD 起草并追加到 [database-design.md](../database-design.md) / [file-structure.md](../file-structure.md) 模板已预留段落）
  - `IAuditLogRepository` 的 6 个具名动词方法（[ADR-022](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 手写扩展方法三件套 + Repository 实现同样交给 `Inkwell.Core.AuditLogs` / `providers/Inkwell.Persistence.EFCore`）——`DefaultAuditLogger` 内部通过 `IPersistenceProvider.GetRepository<IAuditLogRepository>()`（[AGENTS.md §3.2 注入风格](../../../AGENTS.md)）访问
  - UI-009 审计页检索表单具体渲染（`Inkwell.WebApi` HD 起草时消费 `AuditLogQuery` / `AuditLogEntry`）
  - 后台清理任务（[ADR-008 §决策](../../03-architecture/adr/ADR-008-audit-log-store-and-query.md) 按 `RetentionDays` 定期删除过期记录）的具体调度实现——`Inkwell.Core.AuditLogs` 业务 HD 起草，本 HD 仅通过 `AuditLoggerOptions.RetentionDays` 提供配置值
  - 导出能力（[AGENTS.md §3.3 禁区](../../../AGENTS.md) 明确"不做审计导出"；`AuditLogQuery` / `AuditLogEntry` **不**设计任何 `Export*` 方法或 CSV/Excel 序列化字段）

### 1.3 关键决策摘要

> 全部由 2026-07-05 picker 拍板，决策证据见本节"出处"列；详细候选与放弃理由见 [§13 决策记录](#13-决策记录)。

- **Q-actor-user-id-type**：解决 [design-review-report §3.2 N2/C4](../design-review-report.md#n2auditcontextactoruserid-与-ihasownerowneruserid-类型分歧c4) 遗留分歧——`AuditContext.ActorUserId`（HD-001 §3.7）改为 `Guid`，与 [HD-002 §3.9](HD-002-Inkwell.Abstractions-persistence-port.md) `IHasOwner.OwnerUserId: Guid` 强一致；系统 actor（定时任务 / Trigger）用 `Guid.Empty` 表示；已同步给 HD-001 §3.7 加 [2026-07-05 errata·第五轮](HD-001-Inkwell.Abstractions-foundation.md#37-commonauditcontextcs)
- **Q-facade-scope**：`IAuditLogger` 仅 2 方法（`LogAsync` / `QueryAsync`），不设独立 `CountAsync`——`QueryAsync` 返回的 `PagedResult<AuditLogEntry>`（[HD-002 §3.4](HD-002-Inkwell.Abstractions-persistence-port.md#34-persistencepagedresultcs)）已内含 `long TotalCount`
- **Q-actor-type-model**：`AuditActorType` 是封闭 `enum { User, Token, System }`——[ADR-008 §决策](../../03-architecture/adr/ADR-008-audit-log-store-and-query.md) 已锁定三值闭集，强类型 + 编译期校验
- **Q-event-type-model**：`EventType` 是开放 `string`（不设 `enum AuditEventType`）——未来多个业务 HD（`Inkwell.Core.Agents` / `.Skills` / `.Tools` / `.PublicApi` / `.Admin` / `.Versioning`）会持续新增事件类型字面量（如 [AC-067](../../01-requirements/acceptance-criteria.md) `admin_unlock_account` / [AC-068](../../01-requirements/acceptance-criteria.md) `admin_revoke_share`），端口层若锁闭集会导致每次业务新增事件类型都要改 `Inkwell.Abstractions`，与 [HD-004 Q-key-convention](HD-004-Inkwell.Abstractions-cache-port.md#13-关键决策摘要) / [HD-005 Q-queuename-convention](HD-005-Inkwell.Abstractions-queue-port.md#13-关键决策摘要) "端口层保持薄、语义留业务侧" 风格一致
- **Q-target-kind-model**：`TargetKind` 同样是开放 `string`（不设 `enum AuditTargetKind`），理由同 Q-event-type-model
- **Q-result-code-model**：`AuditResultCode` 是封闭 `enum { Success, Failure }`——保留 [ADR-008](../../03-architecture/adr/ADR-008-audit-log-store-and-query.md) "result_code" 字面语义，二值闭集足够覆盖当前全部 AC 场景（成功 / 失败二元判断），强类型优于字符串
- **Q-write-failure-strategy**：`LogAsync` 对调用方**不**因存储写入持续失败（[ADR-008](../../03-architecture/adr/ADR-008-audit-log-store-and-query.md) 重试 3 次 + 磁盘 fallback 之后）抛异常——"不吞错"通过内部 OTel `exception.*` 记录 + 磁盘 fallback 持久化 + 触发告警三件套满足，与 ADR-008 后果"写入路径与业务事务可同步（确保业务成功必有审计）"的业务连续性意图一致（详 §4.2）
- **Q-query-time-window**：`AuditLoggerOptions.MaxQueryTimeRangeDays` 默认 `7`（[ADR-008 §决策](../../03-architecture/adr/ADR-008-audit-log-store-and-query.md) "v1 默认最大 7 天"字面值），可配置（与 [HD-004](HD-004-Inkwell.Abstractions-cache-port.md#13-决策记录) / [HD-005](HD-005-Inkwell.Abstractions-queue-port.md#13-决策记录) TTL 类字段可配置风格一致）
- **Q-retention-days-default**：`AuditLoggerOptions.RetentionDays` 默认 `180`（对齐 [requirements.md §8.3](../../01-requirements/requirements.md) "审计日志：至少保留 6 个月"硬性要求）——原与 [ADR-008 §决策](../../03-architecture/adr/ADR-008-audit-log-store-and-query.md) 字面"v1 默认保留 90 天"（90 < 180）存在冲突，已由 [ADR-008 2026-07-05 errata](../../03-architecture/adr/ADR-008-audit-log-store-and-query.md) 修订为 180 天，两文档现已一致（详 [§11 已完成事项](#11-待补--待评审)）
- **Q-pagination-bounds**：`AuditLoggerOptions.DefaultPageSize = 50`（对齐 ADR-008 "分页 50 条 / 页"字面值）/ `MaxPageSize = 200`（防止业务侧一次拉取过大页面）
- **Q-implementation-topology**：`IAuditLogger` 唯一实现 `Inkwell.Core.AuditLogs.DefaultAuditLogger`，不设独立 `providers/*` csproj、不在 `InkwellProvidersOptions` 新增 `Audit` 选择器字段——[ADR-008](../../03-architecture/adr/ADR-008-audit-log-store-and-query.md) 已锁定单一存储策略（复用 [ADR-004 EF Core Provider](../../03-architecture/adr/ADR-004-data-store-provider-switchable-ef-core.md)，不新增可切换维度），与 [HD-006 `Inkwell.Core.AgentRuntime` 单实现拓扑](HD-006-Inkwell.Abstractions-agent-runtime-port.md#13-关键决策摘要) 同款；此项由 H2 ADR-008 + `InkwellProvidersOptions` 现状（[HD-001 §3.11.1](HD-001-Inkwell.Abstractions-foundation.md#3111-optionsinkwellprovidersoptionscsf9-新增) 六字段中无 `Audit`）隐式锁定，本轮未重新 picker，仅在此记录一致性依据
- **Q-otel**：OTel span 命名 `audit.<verb>`（`log` / `query`）+ 私有字段 `audit.actor_user_id` / `audit.actor_type` / `audit.event_type` / `audit.target_kind` / `audit.result_code` / `audit.operation_outcome`——延续 [HD-004](HD-004-Inkwell.Abstractions-cache-port.md#13-关键决策摘要) / [HD-005](HD-005-Inkwell.Abstractions-queue-port.md#13-关键决策摘要) / [HD-006](HD-006-Inkwell.Abstractions-agent-runtime-port.md#13-关键决策摘要) 命名风格，非新 picker（机械延续）

### 1.4 `LogAsync` 异步解耦声明（呼应 ADR-008"业务事务不阻塞"设计意图）

[ADR-008 §决策](../../03-architecture/adr/ADR-008-audit-log-store-and-query.md) 原文"写入失败处理：使用本地内存队列 + 重试 3 次；3 次失败后写入磁盘 fallback 文件，并触发告警"暗示写入路径是**异步解耦**的：`LogAsync` 把 `AuditLogRequest` 快速放入 `Inkwell.Core.AuditLogs` 内部的本地内存队列（facade 层 `Task` 近乎立即完成），由后台消费者执行实际 DB 写入 + 重试 + fallback，从而不让审计写入的重试延迟传导到业务调用方的整体响应时间——这与 ADR-008 后果"写入路径与业务事务可同步（确保业务成功必有审计）"并不矛盾：**"同步"指业务 use case 在同一次请求处理中显式调用 `LogAsync`（时序上紧邻业务操作，不是"事后批处理"），而非要求 DB 写入本身走同一数据库事务**。

- 本 HD **不**要求 `LogAsync` 内部复用 [HD-005 `IQueueProvider`](HD-005-Inkwell.Abstractions-queue-port.md) 端口——审计写入是进程内单实例场景，不需要跨进程可靠队列语义（无需持久化 / 无需多消费者），复用 `IQueueProvider` 属过度设计；`Inkwell.Core.AuditLogs` 可自建轻量 [`System.Threading.Channels`](https://learn.microsoft.com/dotnet/core/extensions/channels) 内存队列，此实现细节留待独立 `Inkwell.Core` HD 起草
- `LogAsync` facade 层性能预算（§7.1）仅覆盖"入队"耗时，不含后台重试 / fallback 的实际持久化耗时

## 2. 文件结构

```text
src/core/Inkwell.Abstractions/
  Audit/                                  # （新增子目录）
    IAuditLogger.cs                       # 顶层 facade（2 方法：LogAsync/QueryAsync）
    AuditLogRequest.cs                    # record，写入请求（AuditContext + ActorType + AgentId? + ResultCode + ErrorCode?）
    AuditLogEntry.cs                      # record，查询结果单条记录（对齐 ADR-008 表结构关键字段）
    AuditLogQuery.cs                       # record，查询过滤条件（TimeRange + Pagination + 可选 ActorUserId/EventType/AgentId）
    AuditEnums.cs                         # AuditActorType（3 值闭集）+ AuditResultCode（2 值闭集）两个封闭枚举
    AuditLoggerOptions.cs                 # RetentionDays/MaxQueryTimeRangeDays/DefaultPageSize/MaxPageSize/EnableSensitiveDataLogging
    AuditLoggerOptionsValidator.cs        # IValidateOptions<AuditLoggerOptions>
```

> **csproj 依赖白名单**：HD-007 不引入新依赖，仍仅 [HD-001 §2 锁定的](HD-001-Inkwell.Abstractions-foundation.md) `Microsoft.Extensions.{DependencyInjection,Configuration,Options,Logging}.Abstractions` + `Microsoft.Extensions.VectorData.Abstractions`（HD-008 起用）+ [`System.Text.Json`](https://learn.microsoft.com/dotnet/api/system.text.json)（BCL 内置，`AuditContext.Metadata` → `AuditLogEntry.PayloadJson` 序列化）。**严禁**因本 HD 引入任何具体存储 SDK（[ADR-017 零外部包约束](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) + [RISK-011 三 Provider contract 漏出](../../03-architecture/risk-analysis.md) 同构风险）。

## 3. 程序文件设计（10 字段 × 7 文件）

### 3.1 `Audit/IAuditLogger.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Audit/IAuditLogger.cs`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| 职责         | 顶层审计门面；2 个方法覆盖写入 / 分页查询（[picker Q-facade-scope=A](#13-关键决策摘要)）；`LogAsync` 遵循 [ADR-008](../../03-architecture/adr/ADR-008-audit-log-store-and-query.md) "业务事务不阻塞"设计意图，内部异步解耦（详 [§1.4](#14-logasync-异步解耦声明呼应-adr-008业务事务不阻塞设计意图)），对调用方**不**因存储故障抛异常（[picker Q-write-failure-strategy=A](#13-关键决策摘要)）；全部签名走裸 `Task` / `Task<PagedResult<T>>` + BCL 异常（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)），不走 `Result<T>`                                                                                                                                 |
| 对外接口     | `public interface IAuditLogger { Task LogAsync(AuditLogRequest request, CancellationToken ct = default); Task<PagedResult<AuditLogEntry>> QueryAsync(AuditLogQuery query, CancellationToken ct = default); }`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| 内部函数或类 | 接口本身；实现由 `Inkwell.Core.AuditLogs.DefaultAuditLogger` 提供（唯一实现，[picker Q-implementation-topology](#13-关键决策摘要)），内部通过 `IPersistenceProvider.GetRepository<IAuditLogRepository>()`（[AGENTS.md §3.2](../../../AGENTS.md)）访问持久化                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| 输入数据     | `AuditLogRequest request`（`LogAsync`） / `AuditLogQuery query`（`QueryAsync`） / `CancellationToken ct`（全部方法）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| 输出数据     | `Task`（`LogAsync`，无返回值——ADR-008 保证不阻塞业务，调用方 `await` 仅代表"已入队"） / `Task<PagedResult<AuditLogEntry>>`（`QueryAsync`）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| 依赖模块     | `Audit/AuditLogRequest.cs` / `Audit/AuditLogEntry.cs` / `Audit/AuditLogQuery.cs` / [`Persistence/PagedResult.cs`](HD-002-Inkwell.Abstractions-persistence-port.md#34-persistencepagedresultcs)（HD-002，跨端口复用同一分页返回形态，不重复定义）                                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| 错误处理     | 全部上抛 BCL 异常（见 [§4.2 BCL 异常分类](#42-bcl-异常分类业务失败-vs-程序错误)）：`LogAsync` 仅对参数错误（`request` 为 null / `AuditLogRequest` 内部校验失败）抛 `ArgumentException` / `ArgumentNullException`，存储持续失败**不抛异常**（[§4.2](#42-bcl-异常分类业务失败-vs-程序错误)）；`QueryAsync` 参数错误（`query` 为 null / 时间窗口越界 / 分页越界）抛 `ArgumentException` / `ArgumentOutOfRangeException`，存储读取故障抛 `IOException` / `TimeoutException`；两方法取消均抛 `OperationCanceledException`                                                                                                                                                                                 |
| 日志要求     | 实现层在每个方法入口 / 出口写 OTel span，命名 `audit.<verb>`（`log` / `query`）；6 个 Inkwell 私有字段（`audit.actor_user_id` / `audit.actor_type` / `audit.event_type` / `audit.target_kind` / `audit.result_code` / `audit.operation_outcome`）+ 5 个 OTel 标准 `exception.*` 字段（详 §4.3）；`LogAsync` 存储持续失败时同样在内部（后台消费者）span 记录 `exception.*` 五字段并标记 `ActivityStatusCode.Error`，即使不向调用方抛异常（详 §4.2 / §4.3）                                                                                                                                                                                                                                            |
| 测试要求     | `tests/core/Inkwell.Abstractions.Tests/Audit/IAuditLoggerContractTests.cs`：契约测试（接口形态 ABI 锁定 via [`PublicApiAnalyzers`](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md)）；2 个方法签名 / 参数顺序 / 默认值 / 返回类型逐一验证；行为测试（含存储故障不抛异常路径）在 `tests/core/Inkwell.Providers.Contract/Audit/`（与 [HD-002 §8](HD-002-Inkwell.Abstractions-persistence-port.md) / [HD-004 §8.3](HD-004-Inkwell.Abstractions-cache-port.md) / [HD-005 §8.3](HD-005-Inkwell.Abstractions-queue-port.md) / [HD-006 §3.1](HD-006-Inkwell.Abstractions-agent-runtime-port.md) 拓扑一致），由 `Inkwell.Core.AuditLogs` HD 联合起草 |

### 3.2 `Audit/AuditLogRequest.cs`

| 字段         | 内容                                                                                                                                                                                                                                                   |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 文件路径     | `src/core/Inkwell.Abstractions/Audit/AuditLogRequest.cs`                                                                                                                                                                                               |
| 职责         | `LogAsync` 的写入请求 DTO；包裹 [HD-001 §3.7 `AuditContext`](HD-001-Inkwell.Abstractions-foundation.md#37-commonauditcontextcs) + 审计专属字段（不重复定义 `AuditContext` 已有字段）                                                                   |
| 对外接口     | `public sealed record AuditLogRequest(AuditContext Context, AuditActorType ActorType, AuditResultCode ResultCode, Guid? AgentId = null, string? ErrorCode = null)`                                                                                     |
| 内部函数或类 | 构造期校验：`Context` 非 null（`ArgumentNullException`）；`ResultCode == AuditResultCode.Success` 且 `ErrorCode != null` → `ArgumentException("ErrorCode must be null when ResultCode is Success", nameof(ErrorCode))`（跨字段一致性，防止业务侧误传） |
| 输入数据     | `Context`（必填） / `ActorType`（必填） / `ResultCode`（必填） / `AgentId`（可选，无关联 Agent 的事件如登录 / 管理页操作可省略） / `ErrorCode`（可选，仅 `ResultCode == Failure` 时通常填写）                                                          |
| 输出数据     | `AuditLogRequest` 实例                                                                                                                                                                                                                                 |
| 依赖模块     | `Common/AuditContext.cs`（HD-001） / `Audit/AuditEnums.cs`                                                                                                                                                                                             |
| 错误处理     | `Context == null` → `ArgumentNullException`；`ResultCode == Success && ErrorCode != null` → `ArgumentException`                                                                                                                                        |
| 日志要求     | DTO 自身不做日志；`LogAsync` 调用时实现层在 `audit.log` span 输出 §4.3 私有字段                                                                                                                                                                        |
| 测试要求     | `AuditLogRequestTests.cs`：(1) `Context == null` 抛异常；(2) `ResultCode = Success` + `ErrorCode` 非 null 抛异常；(3) 正常构造（含 `AgentId` / `ErrorCode` 均为 null 的最小合法请求）；(4) record equality                                             |

### 3.3 `Audit/AuditLogEntry.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                     |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 文件路径     | `src/core/Inkwell.Abstractions/Audit/AuditLogEntry.cs`                                                                                                                                                                                                                                                                                                                                                                   |
| 职责         | `QueryAsync` 的查询结果单条记录（只读投影）；字段对齐 [ADR-008 §决策](../../03-architecture/adr/ADR-008-audit-log-store-and-query.md) 表结构关键字段（`id` / `event_type` / `actor_type` / `actor_id` / `agent_id` / `target_kind` / `target_id` / `payload` / `result_code` / `error_code` / `request_id` / `created_at`）                                                                                              |
| 对外接口     | `public sealed record AuditLogEntry(Guid Id, string EventType, AuditActorType ActorType, Guid ActorUserId, Guid? AgentId, string TargetKind, string TargetId, string? PayloadJson, AuditResultCode ResultCode, string? ErrorCode, string RequestId, DateTimeOffset OccurredTime, DateTimeOffset CreatedTime)`                                                                                                            |
| 内部函数或类 | 纯只读投影 record，无构造期业务校验（由持久化层保证数据完整性，端口层不重复校验已落库的数据）；`EventType`/`TargetKind`/`TargetId`/`RequestId` 分别对应写入时 `AuditContext.ActionType`/`ResourceType`/`ResourceId`/`TraceId`；`PayloadJson` 对应 `AuditContext.Metadata` 序列化后的 JSON 文本（[`System.Text.Json`](https://learn.microsoft.com/dotnet/api/system.text.json) 序列化，由 `Inkwell.Core.AuditLogs` 落地） |
| 输入数据     | 由 `Inkwell.Core.AuditLogs.DefaultAuditLogger` 从持久化层读出后组装                                                                                                                                                                                                                                                                                                                                                      |
| 输出数据     | `AuditLogEntry` 实例（`QueryAsync` 结果集的元素类型）                                                                                                                                                                                                                                                                                                                                                                    |
| 依赖模块     | `Audit/AuditEnums.cs`                                                                                                                                                                                                                                                                                                                                                                                                    |
| 错误处理     | 无（只读投影，不由调用方构造，无需暴露校验异常）                                                                                                                                                                                                                                                                                                                                                                         |
| 日志要求     | `QueryAsync` 结果集不直接进 OTel（避免大结果集污染 trace）；仅 `audit.query` span 记录 `audit.operation_outcome` + 结果条数（详 §4.3）                                                                                                                                                                                                                                                                                   |
| 测试要求     | `AuditLogEntryTests.cs`：(1) record equality（同字段相等）；(2) `PayloadJson` 为 null 时可正常构造（元数据为空的写入场景）                                                                                                                                                                                                                                                                                               |

### 3.4 `Audit/AuditLogQuery.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| ------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Audit/AuditLogQuery.cs`                                                                                                                                                                                                                                                                                                                                                                                                                |
| 职责         | `QueryAsync` 查询过滤条件；覆盖 [AC-069](../../01-requirements/acceptance-criteria.md) "按用户 / Agent / 时间范围 / 事件类型任一组合筛选"                                                                                                                                                                                                                                                                                                                             |
| 对外接口     | `public sealed record AuditLogQuery(TimeRange TimeRange, Pagination Pagination, Guid? ActorUserId = null, string? EventType = null, Guid? AgentId = null)`                                                                                                                                                                                                                                                                                                            |
| 内部函数或类 | 构造期校验：`TimeRange` 非 null（`TimeRange.cs` 自身已校验 `Start <= End`，[HD-001 §3.6](HD-001-Inkwell.Abstractions-foundation.md#36-commontimerangecs)）；`Pagination` 非 null；`AuditLoggerOptions.MaxQueryTimeRangeDays` / `MaxPageSize` 的越界校验**不**在 DTO 构造期做（同 [HD-004 §3.2 `CacheEntryOptions` 先例](HD-004-Inkwell.Abstractions-cache-port.md#32-cachecacheentryoptionscs)，DTO 无法访问注入的 `AuditLoggerOptions`），由 `QueryAsync` 实现层校验 |
| 输入数据     | `TimeRange`（必填） / `Pagination`（必填） / `ActorUserId`（可选过滤） / `EventType`（可选过滤） / `AgentId`（可选过滤）                                                                                                                                                                                                                                                                                                                                              |
| 输出数据     | `AuditLogQuery` 实例                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| 依赖模块     | `Common/TimeRange.cs`（HD-001） / `Common/Pagination.cs`（HD-001）                                                                                                                                                                                                                                                                                                                                                                                                    |
| 错误处理     | `TimeRange == null` / `Pagination == null` → `ArgumentNullException`；越出 `MaxQueryTimeRangeDays` / `MaxPageSize` → `QueryAsync` 实现层抛 `ArgumentOutOfRangeException`（paramName=`"query"`，详 §4.2）                                                                                                                                                                                                                                                              |
| 日志要求     | 调用方在 `audit.query` span 输出 `range.start` / `range.end`（[HD-001 §3.6](HD-001-Inkwell.Abstractions-foundation.md#36-commontimerangecs)）+ `sort.field`/`sort.direction`（固定 `CreatedTime`/`Descending`，[picker Q-query-sort](#13-决策记录) 不设可变 `SortOrder` 参数）                                                                                                                                                                                        |
| 测试要求     | `AuditLogQueryTests.cs`：(1) `TimeRange == null` 抛异常；(2) `Pagination == null` 抛异常；(3) 全部可选过滤字段为 null 时合法（无过滤，仅按时间窗口查全部）；(4) record equality                                                                                                                                                                                                                                                                                       |

### 3.5 `Audit/AuditEnums.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                             |
| ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Audit/AuditEnums.cs`                                                                                                                                                                                                                                                              |
| 职责         | 两个 Audit 领域封闭枚举共置一文件（同 [HD-006 `AgentMessageContentPart.cs` 抽象+子类型族共置](HD-006-Inkwell.Abstractions-agent-runtime-port.md) 的紧耦合小类型分组惯例）：`AuditActorType`（[ADR-008](../../03-architecture/adr/ADR-008-audit-log-store-and-query.md) 三值闭集）+ `AuditResultCode`（二值闭集） |
| 对外接口     | `public enum AuditActorType { User, Token, System }` `public enum AuditResultCode { Success, Failure }`                                                                                                                                                                                                          |
| 内部函数或类 | 两个 `enum`，无成员方法                                                                                                                                                                                                                                                                                          |
| 输入数据     | 无（枚举值）                                                                                                                                                                                                                                                                                                     |
| 输出数据     | 无（枚举值）                                                                                                                                                                                                                                                                                                     |
| 依赖模块     | 无                                                                                                                                                                                                                                                                                                               |
| 错误处理     | 不适用（枚举本身不抛异常；越界值由 `AuditLogRequest` 构造期隐式由 C# 类型系统保证，不做额外范围校验）                                                                                                                                                                                                            |
| 日志要求     | 序列化为字符串写入 OTel 私有字段（`audit.actor_type` / `audit.result_code`），采用 `enum.ToString()` 默认命名（`"User"` / `"Token"` / `"System"` / `"Success"` / `"Failure"`）                                                                                                                                   |
| 测试要求     | `AuditEnumsTests.cs`：枚举值数量断言（`AuditActorType` 恰好 3 值、`AuditResultCode` 恰好 2 值），防止未来误增值破坏 ADR-008 闭集假设而未经 ADR 更新                                                                                                                                                              |

### 3.6 `Audit/AuditLoggerOptions.cs`

> [HD-001 §3.11](HD-001-Inkwell.Abstractions-foundation.md#311-optionsinkwelloptionscs) 根 `InkwellOptions.Audit` 槽位当前为占位 `AuditLoggerOptions` 类，本 HD 补全其字段。**不**在 `InkwellProvidersOptions`（[HD-001 §3.11.1](HD-001-Inkwell.Abstractions-foundation.md#3111-optionsinkwellprovidersoptionscsf9-新增)）新增 `Audit` 选择器字段——[§1.3 Q-implementation-topology](#13-关键决策摘要) 已声明理由。

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                                         |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 文件路径     | `src/core/Inkwell.Abstractions/Audit/AuditLoggerOptions.cs`                                                                                                                                                                                                                                                                                                                                                                                                  |
| 职责         | 审计端口详细配置；从 `appsettings.json` `"Inkwell:Audit"` 段绑定                                                                                                                                                                                                                                                                                                                                                                                             |
| 对外接口     | `public sealed class AuditLoggerOptions { [Range(1, 3650)] public int RetentionDays { get; init; } = 180; [Range(1, 90)] public int MaxQueryTimeRangeDays { get; init; } = 7; [Range(1, 500)] public int DefaultPageSize { get; init; } = 50; [Range(1, 1000)] public int MaxPageSize { get; init; } = 200; public bool EnableSensitiveDataLogging { get; init; } = false; }`                                                                                |
| 内部函数或类 | DataAnnotations 校验；`RetentionDays` 默认 `180`（[picker Q-retention-days-default=B](#13-关键决策摘要)，对齐 requirements.md "至少 6 个月"，**与 ADR-008 字面 90 天冲突**，详 [§11](#11-待补--待评审)）；`MaxQueryTimeRangeDays` 默认 `7`（ADR-008 字面值）；`DefaultPageSize`/`MaxPageSize` 默认 `50`/`200`（ADR-008 "50 条/页" + 防御性上限）；Provider 特定字段（无——本端口无可切换 Provider，[§1.3 Q-implementation-topology](#13-关键决策摘要)）不适用 |
| 输入数据     | 由 `IConfiguration` 绑定                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| 输出数据     | `AuditLoggerOptions` 实例（DI 通过 `IOptions<AuditLoggerOptions>` 注入）                                                                                                                                                                                                                                                                                                                                                                                     |
| 依赖模块     | `System.ComponentModel.DataAnnotations`                                                                                                                                                                                                                                                                                                                                                                                                                      |
| 错误处理     | DataAnnotations 校验失败 → `OptionsValidationException`，host 兜底                                                                                                                                                                                                                                                                                                                                                                                           |
| 日志要求     | DI 启动期 `IValidateOptions` 失败时输出 OTel `exception.type=Microsoft.Extensions.Options.OptionsValidationException`（[HD-001 §5.3](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)）                                                                                                                                                                                                                                                      |
| 测试要求     | `AuditLoggerOptionsTests.cs`：默认值（180 / 7 / 50 / 200 / false）、`appsettings.json` 绑定、`[Range]` 边界（1 / 上限 / 越界）                                                                                                                                                                                                                                                                                                                               |

### 3.7 `Audit/AuditLoggerOptionsValidator.cs`

| 字段         | 内容                                                                                                                                                                            |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Audit/AuditLoggerOptionsValidator.cs`                                                                                                            |
| 职责         | `IValidateOptions<AuditLoggerOptions>` 实现；DataAnnotations + 跨字段校验                                                                                                       |
| 对外接口     | `internal sealed class AuditLoggerOptionsValidator : IValidateOptions<AuditLoggerOptions> { public ValidateOptionsResult Validate(string? name, AuditLoggerOptions options); }` |
| 内部函数或类 | (1) `Validator.TryValidateObject` DataAnnotations；(2) 跨字段：`DefaultPageSize <= MaxPageSize`（默认值 50 / 200 落在合法范围内）                                               |
| 输入数据     | `AuditLoggerOptions` 实例                                                                                                                                                       |
| 输出数据     | `ValidateOptionsResult.Success` / `Fail(IEnumerable<string>)`                                                                                                                   |
| 依赖模块     | `Microsoft.Extensions.Options` / `System.ComponentModel.DataAnnotations`                                                                                                        |
| 错误处理     | 同 [HD-001 §3.12](HD-001-Inkwell.Abstractions-foundation.md#312-optionsinkwelloptionsvalidatorcs)，校验失败 → `Fail` 含全部消息                                                 |
| 日志要求     | 失败由 `OptionsValidationException` 抛出，host 打 fatal                                                                                                                         |
| 测试要求     | `AuditLoggerOptionsValidatorTests.cs`：(1) DataAnnotations 边界合格；(2) 跨字段：`DefaultPageSize = 300 / MaxPageSize = 200` 拒；(3) 默认值（180 / 7 / 50 / 200）通过           |

## 4. BCL 异常与日志（端口段补充 HD-001 §4）

> **错误处理路径**：本端口与业务命名空间统一采用裸 `Task` / `Task<T>` + .NET BCL 异常。Inkwell 不自建错误码常量集 / 不自建 `Result<T>` / `Error` 抽象 / 不自建端口异常基类，仅保留 `InkwellConfigurationException` / `InkwellBuilderException` 用于 DI 装配期校验。

### 4.1 错误码

本端口**不分配** `INK-AUDIT-NNN` 错误码。与 [HD-002](HD-002-Inkwell.Abstractions-persistence-port.md) / [HD-004](HD-004-Inkwell.Abstractions-cache-port.md) / [HD-005](HD-005-Inkwell.Abstractions-queue-port.md) / [HD-006](HD-006-Inkwell.Abstractions-agent-runtime-port.md) 最终态一致，错误语义全部走 BCL 异常类型表达 + OTel `exception.*` 五字段。

### 4.2 BCL 异常分类（业务失败 vs 程序错误）

按 [HD-001 §5.3 BCL 对照表](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表) 的分类语义：

- **`LogAsync`（写入路径，[picker Q-write-failure-strategy=A](#13-关键决策摘要)）**：
  - 参数错误 → `ArgumentNullException`（`request == null` / `request.Context == null`）；`ArgumentException`（`ResultCode == Success` 且 `ErrorCode != null`）
  - 取消 → `OperationCanceledException`（入队阶段响应 `ct`；一旦成功入队，后台重试/fallback 不再受调用方 `ct` 影响）
  - **存储持续失败（ADR-008 重试 3 次 + 磁盘 fallback 之后）→ 不抛异常给调用方**（[picker Q-write-failure-strategy=A](#13-关键决策摘要)）；实现层在内部后台 span 记录 `exception.*` 五字段 + `ActivityStatusCode.Error` + 触发告警指标（详 §4.3 / §7.3），满足 [AGENTS.md §3.2](../../../AGENTS.md) "不吞错"要求的方式是**可观测 + 持久化 fallback**，而非向业务调用方传播异常
  - **磁盘 fallback 本身也写入失败**（极端场景，如磁盘满）→ 同样不向 `LogAsync` 调用方抛异常（维持 ADR-008 业务连续性承诺），改为 P1 级进程内 fatal 日志 + OTel span `exception.*` 记录 + 告警升级（详 §7.3），此为实现层职责，本 HD 仅声明契约边界
- **`QueryAsync`（查询路径）**：
  - 参数错误 → `ArgumentNullException`（`query == null`）；`ArgumentOutOfRangeException`（`query.TimeRange` 跨度超过 `AuditLoggerOptions.MaxQueryTimeRangeDays`，paramName=`"query"`；`query.Pagination.PageSize` 超过 `MaxPageSize`，paramName=`"query"`）
  - 程序错误 / 失血告警：`IOException`（DB 连接失败）；`TimeoutException`（查询超时）
  - 取消 → `OperationCanceledException`

### 4.3 OTel span / 字段

每个方法在实现层（`Inkwell.Core.AuditLogs.DefaultAuditLogger`）按 [picker Q-otel](#13-关键决策摘要) 输出 span：

- `audit.log` ← `LogAsync`（前台"入队" span；若触发重试 / fallback，另在后台消费者建立子 span `audit.log.retry` / `audit.log.fallback_write`，与前台 span 通过 `traceparent` 关联）
- `audit.query` ← `QueryAsync`

**Inkwell 私有字段**（6 个）：

- `audit.actor_user_id`（`Guid.ToString()`）
- `audit.actor_type`（`AuditActorType` 字符串值）
- `audit.event_type`（`AuditContext.ActionType` / `AuditLogQuery.EventType`）
- `audit.target_kind`（`AuditContext.ResourceType`）
- `audit.result_code`（`AuditResultCode` 字符串值，仅 `LogAsync`）
- `audit.operation_outcome`：值域按方法区分——
  - `LogAsync`：`enqueued`（前台 span，永远此值，因不等待实际持久化结果）；后台子 span 用 `persisted` / `persisted_after_retry` / `fallback_file_written`
  - `QueryAsync`：`success` / `failed` / `cancelled`

**OTel 标准字段**（5 个，按 [`exception.*` 语义约定](https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/)，仅异常路径 / 内部失败路径填充）：

- `exception.type`（如 `System.IO.IOException` / `System.TimeoutException`）
- `exception.message`
- `exception.stacktrace`
- `exception.escaped`（`LogAsync` 后台失败路径固定为 `false`——异常未向调用方"逃逸"，仅内部记录）
- `exception.id`（`Guid.CreateVersion7().ToString()` 生成）

> **PII 提示**：`audit.actor_user_id` / `audit.event_type` / `audit.target_kind` / `audit.target_id` 允许进 OTel（Inkwell 自托管 Grafana 栈在边界内，同 [HD-004 §4.3](HD-004-Inkwell.Abstractions-cache-port.md#43-otel-span--字段) / [HD-006 §4.3](HD-006-Inkwell.Abstractions-agent-runtime-port.md#43-otel-span--字段) PII 处理方式）。**`AuditContext.Metadata`（即 `AuditLogEntry.PayloadJson`）原始内容不得进入任何 OTel 字段**——即使 `EnableSensitiveDataLogging=true` 也仅追加 `audit.payload_size_bytes`（长度而非内容），避免 trace 后端反向成为审计数据的第二份未受控副本。

## 5. 公共约定继承（HD-001）

### 5.1 命名

- `IAuditLogger` ↔ [HD-001 §5.1](HD-001-Inkwell.Abstractions-foundation.md#51-命名) 端口接口命名——本 HD 沿用非 `I<Capability>Provider` 命名（与 [HD-006 `IAgentRuntime` 同款既定命名例外](HD-006-Inkwell.Abstractions-agent-runtime-port.md#51-命名)，因 "Logger" 是领域惯用词，强行套 `IAuditProvider` 反而降低可读性）
- `LogAsync` / `QueryAsync` ↔ §5.1 异步方法以 `Async` 结尾
- `AuditLogRequest` / `AuditLogQuery` ↔ §5.1 `<Action><Entity>Request` DTO 命名模式；`AuditLogEntry` 是查询结果的只读投影（非请求/响应模式，参照 [HD-003 `FileObjectInfo`](HD-003-Inkwell.Abstractions-file-storage-port.md) 只读记录命名先例）
- `AuditLoggerOptions` ↔ §5.1 `<Provider>Options`（本端口沿用 `AuditLogger` 前缀而非 `Audit`，与接口名 `IAuditLogger` 对齐）

### 5.2 签名

- 2 个方法走裸 `Task` / `Task<PagedResult<AuditLogEntry>>` + BCL 异常，↔ [HD-001 §5.3 BCL 对照表](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)
- `LogAsync` 返回裸 `Task`（非 `Task<bool>`）——[picker Q-write-failure-strategy=A](#13-关键决策摘要) 已确定存储层面失败不向调用方传播，无需 `bool` 表达"是否持久化成功"这一中间态；`Task` 完成即代表"已合法入队"
- `CancellationToken ct = default` 全 2 方法必填 ↔ [HD-001 §4.3](HD-001-Inkwell.Abstractions-foundation.md#43-取消传播)

### 5.3 错误处理

- 参数错误 → `ArgumentException` / `ArgumentNullException` / `ArgumentOutOfRangeException`
- `LogAsync` 存储持续失败 → 不抛异常（[§4.2](#42-bcl-异常分类业务失败-vs-程序错误) 例外声明，全仓 6 个端口中唯一"故障不透传"的方法，需在 H4 测试用例中显式覆盖该行为差异）
- `QueryAsync` 程序错误 / 失血告警 → `IOException` / `TimeoutException`
- 取消 → `OperationCanceledException`
- 实现层用 [`ActivitySource.StartActivity`](https://learn.microsoft.com/dotnet/api/system.diagnostics.activitysource.startactivity) 创建 span 后，异常路径（含 `LogAsync` 内部吸收路径）用 `Activity.RecordException` 或 `Activity.SetStatus(ActivityStatusCode.Error, message)` 写入 `exception.*` 五字段（详 §4.3）

## 6. Builder DSL 钩子（给 `Inkwell.Core.AuditLogs` HD 的契约）

`IAuditLogger` 无可切换 Provider（[§1.3 Q-implementation-topology](#13-关键决策摘要)），`Inkwell.Core.AuditLogs` 提供**唯一**注册入口：

```csharp
// src/core/Inkwell.Core/AuditLogs/AuditLoggerBuilderExtensions.cs
public static class AuditLoggerBuilderExtensions
{
    public static IInkwellBuilder AddDefaultAuditLogger(
        this IInkwellBuilder builder);
}
```

该扩展方法**必须**：(1) 调用 `builder.Services.AddSingleton<IAuditLogger, DefaultAuditLogger>()`；(2) 注册 `IValidateOptions<AuditLoggerOptions>`；(3) 无需与 `InkwellProvidersOptions` 做交叉校验（无选择器字段，[§1.3](#13-关键决策摘要)）；(4) 返回 `builder`。[`InkwellBuilder.Build()`](HD-001-Inkwell.Abstractions-foundation.md#39-builderinkwellbuildercs) 检测必备端口时若发现 `IAuditLogger` 未注册且用户未显式调用 `AddDefaultAuditLogger()`，**自动**补注册默认实现（与 [Queue 端口 `ChannelsQueueProvider` 默认自动注册（ADR-018）](../file-structure.md#inkwellabstractionsqueue)同款零配置默认），避免每个宿主项目都要求显式一行样板代码。

## 7. 性能 / 安全 / 可观测性

### 7.1 性能预算

| 方法         | facade overhead P50 | facade overhead P99 | 备注                                                                                                                        |
| ------------ | ------------------- | ------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| `LogAsync`   | < 10ms              | < 50ms              | 仅覆盖"入队"耗时（[§1.4](#14-logasync-异步解耦声明呼应-adr-008业务事务不阻塞设计意图)），不含后台重试 / fallback 持久化耗时 |
| `QueryAsync` | < 100ms             | < 300ms             | 呼应 [ADR-008 §决策](../../03-architecture/adr/ADR-008-audit-log-store-and-query.md) "≤ 100 万条数据查询 < 200ms"目标       |

### 7.2 安全

- `AuditLoggerOptions.EnableSensitiveDataLogging` 默认 `false`；启用后仅追加 `audit.payload_size_bytes`（大小而非内容）——**`AuditContext.Metadata` / `AuditLogEntry.PayloadJson` 原始内容永不进入 OTel**（详 [§4.3 PII 提示](#43-otel-span--字段)）
- 审计日志本身**不做脱敏**（[NFR-006](../../01-requirements/requirements.md) 已声明"v1 不做敏感字段脱敏"），访问控制依赖 [REQ-017](../../01-requirements/requirements.md) 管理页鉴权（仅 `is_super=true` Member 可查全量）——本端口层不重复实现鉴权，由 `Inkwell.WebApi` 在管理页 API 层拦截
- 磁盘 fallback 文件（ADR-008 写入失败兜底）路径 / 权限由 `Inkwell.Core.AuditLogs` 实现层锁定，本 HD 不约束具体路径，仅要求该文件**不**暴露到任何公开可访问目录

### 7.3 可观测性

- 6 私有 + 5 OTel 标准 `exception.*` 字段进 OTel；本 HD 不锁告警规则（H4 [TestCaseAuthor](../../../.github/agents/h4-test-case-author.agent.md) 反推时锁），但建议告警维度：
  - `audit.operation_outcome = fallback_file_written` 速率 > 0/min → **P1**（审计写入已耗尽 3 次重试降级到磁盘兜底，直接关联 [AGENTS.md §3.2](../../../AGENTS.md) "不吞错"硬约束的运维响应义务）
  - 磁盘 fallback 本身写入失败（§4.2 极端场景）→ **P1**（最高优先级，审计数据面临丢失风险）
  - `QueryAsync` 的 `exception.type ∈ {IOException, TimeoutException}` 速率 > 5/min → **P2**（DB 读取路径异常，可能与 [RISK-011](../../03-architecture/risk-analysis.md) 相关）

## 8. 测试要求

### 8.1 单元测试（本 HD 范围内）

- 测试项目：`tests/core/Inkwell.Abstractions.Tests/Audit/`（与 HD-001 同 csproj，[MSTest.Sdk 4.x](https://github.com/microsoft/testfx) + MTP runner）
- 每个文件至少一个 `*Tests.cs` 配对（见 §3 各小节"测试要求"）
- 覆盖率门槛：`AuditLogRequest` / `AuditLogQuery` ≥ 95%；`AuditLoggerOptions` + Validator ≥ 90%；`IAuditLoggerContractTests` 仅锁 ABI ≥ 100%

### 8.2 契约测试

- 接口 ABI 用 [`PublicApiAnalyzers`](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) 锁定
- `IAuditLogger` 形态变更 → 需新建 ADR + 影响 `Inkwell.Core.AuditLogs` HD

### 8.3 集成测试

- 本 HD **不**起集成测试（端口层无外部依赖）
- `LogAsync` 存储持续失败不抛异常的行为在 `tests/core/Inkwell.Providers.Contract/Audit/` 验证（模拟 DB 持续故障，断言 `LogAsync` 正常完成 + 磁盘 fallback 文件生成 + 告警指标递增），由 `Inkwell.Core.AuditLogs` HD 联合起草

### 8.4 BannedSymbols（CI 强制）

- `Inkwell.Abstractions.Audit.*` 不涉及具体存储 SDK 引用，本节无额外 banlist 项（与 [HD-002](HD-002-Inkwell.Abstractions-persistence-port.md) 通过 `IPersistenceProvider` 间接访问一致，无需直接引用 EF Core）

## 9. 部署 / 配置

`Inkwell.Abstractions.csproj` 与端口层一同打镜像（无独立部署）。`appsettings.json` 顶层段：

```json
{
  "Inkwell": {
    "Audit": {
      "RetentionDays": 180,
      "MaxQueryTimeRangeDays": 7,
      "DefaultPageSize": 50,
      "MaxPageSize": 200,
      "EnableSensitiveDataLogging": false
    }
  }
}
```

> 无 `"Inkwell:Providers:Audit"` 段——本端口无可切换 Provider（[§1.3 Q-implementation-topology](#13-关键决策摘要)）。

## 10. CI 自检命令（grep 列表）

| 编号 | 检查项                                                                                                                   | 命令（CI [GitHub Actions](https://docs.github.com/actions) 工作流引用）                                                                                                                                  |
| ---- | ------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| C1   | `IAuditLogger` 接口签名稳定                                                                                              | [PublicApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) `PublicAPI.Shipped.txt` diff                                                |
| C2   | 端口层无 `Task<Result<` 残留（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)） | `rg -n -e 'Task<Result<' -e 'Task<Result>' src/core/Inkwell.Abstractions/Audit/` 期望 0 行                                                                                                               |
| C3   | 业务命名空间禁 `Result<T>` / `ErrorCodes` 引用                                                                           | `rg -n -e 'Common\.Result' -e 'Common\.Error' -e 'ErrorCodes\.' src/core/Inkwell.Core/AuditLogs/` 期望 0 行                                                                                              |
| C4   | 无导出相关方法（[AGENTS.md §3.3 禁区](../../../AGENTS.md)）                                                              | `rg -n -e '\bExport' -e '\bDownloadCsv' -e '\bDownloadExcel' src/core/Inkwell.Abstractions/Audit/` 期望 0 行                                                                                             |
| C5   | `AuditContext.Metadata` / `AuditLogEntry.PayloadJson` 原始内容不进 OTel（仅大小字段）                                    | `rg -n -e '"audit\.payload"' -e 'audit\.payload\s*=' src/core/ providers/` 期望 0 行（仅 `audit.payload_size_bytes` 允许）                                                                               |
| C6   | OTel span 字段名一致                                                                                                     | `rg -n -e '"audit\.actor_user_id"' -e '"audit\.actor_type"' -e '"audit\.event_type"' -e '"audit\.target_kind"' -e '"audit\.result_code"' -e '"audit\.operation_outcome"' src/core/` 期望全部在实现层覆盖 |
| C7   | `InkwellProvidersOptions` 未新增 `Audit` 字段（[§1.3 Q-implementation-topology](#13-关键决策摘要)）                      | `rg -n -e 'public string Audit' src/core/Inkwell.Abstractions/Options/InkwellProvidersOptions.cs` 期望 0 行                                                                                              |

## 11. 待补 / 待评审

- **RetentionDays 默认值与 ADR-008 字面冲突（已通过 errata 解决，2026-07-06）**（[picker Q-retention-days-default=B](#13-关键决策摘要)）：本 HD 采用 `180` 天对齐 [requirements.md §8.3](../../01-requirements/requirements.md) 硬性要求；[ADR-008 §决策](../../03-architecture/adr/ADR-008-audit-log-store-and-query.md) 正文原字面保留不改（仍是"90 天"），但已补充 [2026-07-05 errata](../../03-architecture/adr/ADR-008-audit-log-store-and-query.md#决策)："保留期由 90 天修订为 180 天（约 6 个月）"；[tech-selection.md §8](../../03-architecture/tech-selection.md) 已同步补充相同 errata。本 HD 的 `AuditLoggerOptions.RetentionDays = 180` 默认值现与 H2 文档（含 errata）完全对齐，此前记录的数字分歧已消除
- **磁盘 fallback 文件的具体格式 / 路径 / 恢复流程**：留 `Inkwell.Core.AuditLogs` 独立 HD 起草（本 HD 仅声明"存在该兜底机制"的契约边界，不锁具体实现）
- **后台清理任务（按 `RetentionDays` 删除过期记录）的调度实现**：留 `Inkwell.Core.AuditLogs` 独立 HD 起草（[`BackgroundService`](https://learn.microsoft.com/dotnet/core/extensions/workers) 或类似机制）
- **UI-009 审计页检索表单与 `AuditLogQuery` 字段的具体映射代码**：留 `Inkwell.WebApi` HD 起草

## 12. 跨模块章节贡献

本 HD 在以下跨模块文件中追加一级章节 `## Inkwell.Abstractions.Audit`：

- `docs/04-detailed-design/file-structure.md` — 新增 `Inkwell.Abstractions/Audit/` 子目录树
- `docs/04-detailed-design/database-design.md` — **不贡献**（端口层不直接接 DB；`audit_logs` 表清单行的"锁定 HD"列保持 `TBD`，留 `Inkwell.Core.AuditLogs` 业务 HD 填写）

> 跨模块章节追加由本 HD 起草后**立即同步**到对应文件（**只追加**不改其他模块章节）。

## 13. 决策记录

### 13.1 起草期 picker 决策（2026-07-05）

| 字段                     | 选定值                                                                                                      | picker 时间 |
| ------------------------ | ----------------------------------------------------------------------------------------------------------- | ----------- |
| Q-actor-user-id-type     | 选项 1：`AuditContext.ActorUserId` 改为 `Guid`（系统 actor 用 `Guid.Empty`），同步给 HD-001 §3.7 加 errata  | 2026-07-05  |
| Q-facade-scope           | A：2 方法（`LogAsync` + `QueryAsync`）                                                                      | 2026-07-05  |
| Q-actor-type-model       | A：`enum AuditActorType { User, Token, System }`                                                            | 2026-07-05  |
| Q-event-type-model       | A：`string EventType`（开放字符串）                                                                         | 2026-07-05  |
| Q-target-kind-model      | A：`string TargetKind`                                                                                      | 2026-07-05  |
| Q-result-code-model      | B：`enum AuditResultCode { Success, Failure }`                                                              | 2026-07-05  |
| Q-write-failure-strategy | A：`LogAsync` 不向调用方抛出存储失败异常                                                                    | 2026-07-05  |
| Q-query-time-window      | A：`AuditLoggerOptions.MaxQueryTimeRangeDays`（默认 7）可配置                                               | 2026-07-05  |
| Q-retention-days-default | B：默认 180 天（对齐 requirements.md "至少 6 个月"；与 ADR-008 字面 90 天冲突，详 [§11](#11-待补--待评审)） | 2026-07-05  |
| Q-pagination-bounds      | A：`DefaultPageSize=50`，`MaxPageSize=200`                                                                  | 2026-07-05  |

### 13.2 候选与放弃理由

- **Q-actor-user-id-type**：备选"保留 string，仅声明允许非 Guid 字面量"未选——Owner 判断与 `IHasOwner.OwnerUserId: Guid` 强一致的收益（FK 完整性 + 与其余 users.id 引用同型）大于"系统 actor 用 Guid.Empty 特例"的额外复杂度
- **Q-facade-scope**：备选 B（3 方法，含独立 `CountAsync`）被否决——`PagedResult<T>` 已含 `TotalCount`，独立计数接口是重复能力，增加实现层维护面
- **Q-event-type-model / Q-target-kind-model**：备选"闭集 enum"均被否决——理由同 [HD-004](HD-004-Inkwell.Abstractions-cache-port.md#132-候选与放弃理由) / [HD-005](HD-005-Inkwell.Abstractions-queue-port.md#132-候选与放弃理由) 已确立的"端口层保持薄，语义留业务侧"原则；审计事件类型会随业务模块（Agents/Skills/Tools/PublicApi/Admin/Versioning）持续增长，闭集会导致端口层被迫频繁修改
- **Q-result-code-model**：备选 A（`bool IsSuccess`）未选——虽更简单，但会丢失 ADR-008 "result_code"字面语义的可读性；备选 C（开放 `string`）未选——当前无场景需要超出成功/失败的第三态，闭集 enum 提供编译期保护
- **Q-write-failure-strategy**：备选 B（fallback 也失败后抛 `IOException`）被否决——会导致业务 use case 因审计副作用而失败，与 [ADR-008 后果](../../03-architecture/adr/ADR-008-audit-log-store-and-query.md) "写入路径与业务事务可同步（确保业务成功必有审计）"的业务连续性意图直接冲突；选 A 通过磁盘 fallback + OTel + 告警满足"不吞错"而不牺牲业务可用性
- **Q-query-time-window**：备选"硬编码 7 天不做配置项"未选——与 [HD-004](HD-004-Inkwell.Abstractions-cache-port.md) / [HD-005](HD-005-Inkwell.Abstractions-queue-port.md) TTL 类字段普遍做成可配置 Options 的风格保持一致，且不同部署环境可能需要调整（如 staging 环境放宽查询窗口便于排障）
- **Q-retention-days-default**：备选 A（默认 90 天，沿用 ADR-008 字面）未选——Owner 判断 requirements.md NFR-004 "必须"措辞的约束力高于 ADR-008 尚未 errata 的字面数字；选 B 后本 HD 显式记录该冲突及后续 errata 待办（[§11](#11-待补--待评审)），不静默掩盖分歧
- **Q-pagination-bounds**：备选 B（`MaxPageSize=100`）未选——200 提供更宽松的 UI 侧一次性加载空间，且不违反 ADR-008 "50 条/页默认展示"的字面（`MaxPageSize` 只是防御性上限，不是默认展示条数）

> 全部候选与放弃理由源自 2026-07-05 picker 会话；无历史 errata（本 HD 从起草第一天直接采用 ADR-023 最终态规约）。除本 HD 自身决策外，`AuditContext.ActorUserId` 类型变更已同步给 [HD-001 §3.7](HD-001-Inkwell.Abstractions-foundation.md#37-commonauditcontextcs) 加 2026-07-05 errata·第五轮。
