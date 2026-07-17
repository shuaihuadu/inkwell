---
id: HD-002
title: Inkwell.Abstractions 详细设计 — Persistence Port（IPersistenceProvider facade + IRepository base + IUnitOfWork）
stage: H3
status: reviewed
reviewers: [Inkwell]
upstream:
  - REQ-002
  - REQ-009
  - REQ-010
  - REQ-013
  - REQ-014
  - REQ-015
  - NFR-005
  - ADR-002
  - ADR-004
  - ADR-017
  - ADR-019
  - ADR-021
  - HD-001
---

> **错误处理约定**（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) accepted by Inkwell 2026-05-11，含 [errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) 废错误码、[errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 删 `Result<T>` / `Error` 抽象）：端口层与业务层统一采用裸 `Task<T>` + .NET BCL 异常。Inkwell **不自建 `Result<T>` / `Error` 抽象** / 不自建错误码机制 / 不自建端口层异常基类；仅保留 `InkwellConfigurationException` / `InkwellBuilderException` 两个程序错误子类用于 DI 装配期校验。具体错误语义走 BCL 异常类型表达 + OTel [`exception.*` 五字段](https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/)，详 [HD-001 §5.3 BCL 对照表](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)。
>
> **2026-05-11 errata·第二+三+四轮**（[ADR-023 三轮翻新](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) accepted by Inkwell）：本 HD 同步翻签名 / 删错误码库 / 删 `Result<T>` 引用 / 重写 BCL 对照表。受影响章节：§1.1 / §1.2 / §1.3 Q6 / §2 / §3.1 / §3.2 / §3.3 / §3.7 / §3.8 / §3.9 / §3.10 整段删除 / §4.1.3 示例 / §4.2 / §4.3 / §4.4 / §7 / §9。详 §13 errata。§3 文件数从 9 个减为 8 个（§3.10 作为“已废”锁位保留不重用，§3.1 ~ §3.9 编号不调整以保追溯不断）。
>
> **范围切片**：本 HD 覆盖 `Inkwell.Abstractions/Persistence/` 子层——`IPersistenceProvider` facade、`IRepository<TModel, TKey>` marker base（§3.2）、`IUnitOfWork`、interface mixin（`IHasTimestamps` / `IHasRowVersion`）、`PagedResult<T>`、`PersistenceOptions` + Validator。**不**定义具名 `IXxxRepository` 与业务 Model（由各业务命名空间 HD 起草时在 `Abstractions/Persistence/<Module>/` 追加），**不**定义 Entity 类（由 [HD-009 `providers/Persistence/Inkwell.Persistence.EFCore`](../) 起草，[ADR-021](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 锁定 Entity 居所）。
>
> **拓扑张力**：[ADR-021 §备选 B](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 锁定 Entity 在 `providers/`；[AGENTS.md §3.2](../../../AGENTS.md) 锁定业务命名空间只能依赖 `Inkwell.Abstractions` + BCL。**二者叠加**：业务命名空间无法直接 query Entity 类。本 HD 选 **picker Q1=C facade only** 路径——`IPersistenceProvider` 仅暴露事务 / SaveChanges 高层 facade；具名 `IXxxRepository`（每个返回 Model，Model 也在 Abstractions）由各业务 HD 起草，`providers/Persistence/Inkwell.Persistence.EFCore` 同时实现 `IPersistenceProvider` 与所有具名 Repository（HD-009）。
>
> **2026-05-11 errata（F6 + ADR-022 + B 路径）**：§3.2 `IRepository<TModel, TKey>` 重定位为纯 marker interface（零成员）；具名 `IXxxRepository` 不再继承泛型 CRUD，独立定义具名动词方法（§4.1.3 动词白名单）；Model 默认无后缀（§4.1.2），撞名场景降级 `XxxDefinition`。

## 1. 模块概述

### 1.1 职责

`Inkwell.Abstractions/Persistence/` 是后端持久化的端口契约层：

- 顶层 facade `IPersistenceProvider`：提供事务包装 + SaveChanges + Repository 工厂查询能力
- `IRepository<TModel, TKey>` marker base（§3.2）：所有具名 Repository 接口必须继承该 marker（零成员，仅锁 `TModel` / `TKey` 类型参数 + 与 BannedSymbols 配套为禁 `Store` / `Dao` / `Gateway` 后缀的正向锚点）；具名动词方法由具名接口独立定义（§4.1.3）
- `IUnitOfWork`：事务作用域抽象
- 三个 mixin interface（`IHasTimestamps` / `IHasRowVersion` / `IHasOwner`）：Model 按需实现，`providers/Persistence/Inkwell.Persistence.EFCore` 在 `OnModelCreating` 中按 mixin 自动配置 EF 行为
- `PagedResult<T>`：列表分页返回 DTO
- `PersistenceOptions` + `PersistenceOptionsValidator`：配置入口

### 1.2 范围（Q1=C facade only）

本 HD 范围：

| 类别            | 文件清单（位于 `src/core/Inkwell.Abstractions/Persistence/`）                   |
| --------------- | ------------------------------------------------------------------------------- |
| Facade          | `IPersistenceProvider.cs`                                                       |
| Repository base | `IRepository.cs` / `PagedResult.cs`                                             |
| 事务            | `IUnitOfWork.cs`                                                                |
| Mixin           | `Mixins/IHasTimestamps.cs` / `Mixins/IHasRowVersion.cs` / `Mixins/IHasOwner.cs` |
| Options         | `PersistenceOptions.cs` / `PersistenceOptionsValidator.cs`                      |

不在本 HD 范围（拆到后续 HD）：

- 具名 `IAgentRepository` / `IConversationRepository` / `IRunRepository` / 等 ~13 个具名 Repository → 各业务命名空间 HD 起草（`Inkwell.Core.Agents` / `.Conversations` / 等）
- 业务 Model（默认无后缀、撞名降级 `XxxDefinition`，详§4.1.2）→ 各业务 HD 起草
- Entity 类（`AgentEntity` / `ConversationEntity` / 等）→ HD-009 `providers/Persistence/Inkwell.Persistence.EFCore`
- `InkwellDbContext` / `OnModelCreating` / `IEntityTypeConfiguration<>` → HD-009
- `EfCorePersistenceProvider` 实现 → HD-009
- 具名 Mapping 扩展（`AgentMappingExtensions` / 等 ~30 个）→ HD-009 `providers/Persistence/Inkwell.Persistence.EFCore/Mapping/`（[ADR-022](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 锁手写 Extensions 模式）
- `InkwellSeeder` → HD-009
- `MigrationRunner` → HD-009
- 两 final adapter csproj（`Inkwell.Persistence.EFCore.{SqlServer,Postgres}`）→ HD-011 / HD-012

### 1.3 关键决策摘要

| ID  | 决策                                                                                                                                                                                                                                                                                                 | 来源                                                                                                                                                                                                                                                        |
| --- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Q1  | `IPersistenceProvider` = facade + `GetRepository<TRepository>()` 泛型工厂入口；具名 `IXxxRepository` 由业务 HD 起草，业务经 `provider.GetRepository<IXxxRepository>()`（事务作用域外）/ `uow.GetRepository<...>()`（事务作用域内）双入口取（2026-05-18 errata·第五轮，原 facade only 已 superseded） | picker 2026-05-10；[ADR-021 + AGENTS.md §3.2](../../../AGENTS.md) 拓扑张力                                                                                                                                                                                  |
| Q2  | 主键 = `Guid` v7（[`Guid.CreateVersion7()`](https://learn.microsoft.com/dotnet/api/system.guid.createversion7)，.NET 9+ 内置）                                                                                                                                                                       | picker 2026-05-10；[ADR-004 §最小公倍数](../../03-architecture/adr/ADR-004-data-store-provider-switchable-ef-core.md)                                                                                                                                       |
| Q3  | Base Model = interface mixin 按需组合（`IHasTimestamps` / `IHasRowVersion` / `IHasOwner`）                                                                                                                                                                                                           | picker 2026-05-10；agui_run_events 不需所有字段                                                                                                                                                                                                             |
| Q4  | 事务 = `ExecuteInTransactionAsync<T>` 包装；不暴露显式 commit/rollback                                                                                                                                                                                                                               | picker 2026-05-10；避免业务忘 commit                                                                                                                                                                                                                        |
| Q5  | 软删除 = **不提供**；v1 全部硬删                                                                                                                                                                                                                                                                     | picker 2026-05-10；HD-002 不引入 `ISoftDeletable` mixin                                                                                                                                                                                                     |
| Q6  | 并发 = `IHasRowVersion` mixin → EFCore `IsRowVersion()` 自动 token；冲突包装为 [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)（message 前缀 `"Optimistic concurrency conflict"`，inner = EF Core `DbUpdateConcurrencyException`）            | picker 2026-05-10 + [ADR-023 errata·01 BCL 对照](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流)；[REQ-002](../../01-requirements/requirements.md) Agent 配置防丢头 |

## 2. 文件结构

```text
src/core/Inkwell.Abstractions/
  Persistence/                               # （新增子目录）
    IPersistenceProvider.cs                  # 顶层 facade
    IRepository.cs                           # IRepository<TModel, TKey> marker base
    IUnitOfWork.cs                           # 事务作用域
    PagedResult.cs                           # record class PagedResult<T>(IReadOnlyList<T>, long Total, Pagination)
    PersistenceOptions.cs                    # ConnectionString + 超时 + AutoSeedOnStartup
    PersistenceOptionsValidator.cs           # IValidateOptions<PersistenceOptions>
    Mixins/
      IHasTimestamps.cs                      # CreatedAtUtc + UpdatedAtUtc
      IHasRowVersion.cs                      # byte[] RowVersion
      IHasOwner.cs                           # Guid OwnerUserId
```

> **csproj 依赖白名单**：HD-002 不引入新依赖，仍仅 [HD-001 §2 锁定的](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) `Microsoft.Extensions.{DependencyInjection,Configuration,Options,Logging}.Abstractions` + `Microsoft.Extensions.VectorData.Abstractions`（HD-008 起用）。**严禁**因本 HD 引入 `Microsoft.EntityFrameworkCore.Abstractions` / 任何 EF 包（违反 [ADR-017 零外部包约束](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) + [RISK-002 切换抽象漏出](../../03-architecture/risk-analysis.md)）。

## 3. 程序文件设计（10 字段 × 8 文件）

> **2026-05-11 errata·第二+三+四轮**：原 §3.10 `ErrorCodes.Persist.cs` 整段删除（[ADR-023 errata·01 废错误码机制](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流)）；§3.1 ~ §3.9 编号保持不变以保追溯不断（§3.10 作为“已废”锁位保留不重用）。

### 3.1 `Persistence/IPersistenceProvider.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Persistence/IPersistenceProvider.cs`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               |
| 职责         | 顶层持久化 facade；提供事务包装 + SaveChanges + **Repository 工厂查询入口**（`GetRepository<TRepository>()`）；事务作用域外读路径走 facade 工厂、域内读/写走 `IUnitOfWork.GetRepository<T>`，二者签名同款；具名 `IXxxRepository` 仍由业务 HD 各自定义（2026-05-18 errata·第五轮 Q1=A2）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| 对外接口     | `public interface IPersistenceProvider { Task<T> ExecuteInTransactionAsync<T>(Func<IUnitOfWork, CancellationToken, Task<T>> operation, CancellationToken ct = default); Task ExecuteInTransactionAsync(Func<IUnitOfWork, CancellationToken, Task> operation, CancellationToken ct = default); TRepository GetRepository<TRepository>() where TRepository : class; Task<int> SaveChangesAsync(CancellationToken ct = default); }`（`GetRepository<T>` 签名与 §3.3 `IUnitOfWork.GetRepository<T>` 完全对齐）                                                                                                                                                                                                                                                                                                                                        |
| 内部函数或类 | 接口本身；实现由 HD-009 `EfCorePersistenceProvider` 提供                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| 输入数据     | `Func<IUnitOfWork, CancellationToken, Task<T>>` 业务操作 lambda                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| 输出数据     | `Task<T>` / `Task` / `Task<int>`（裸泛型，[ADR-023 核心边界 · 第 1 条](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| 依赖模块     | `IUnitOfWork.cs` / System.Threading.Tasks（删 `Common/Result.cs` / `Common/Error.cs` 依赖）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| 错误处理     | 业务 lambda 抛 BCL 异常 → 自动 rollback 后重抛（不包装）；业务 lambda 抛 [`OperationCanceledException`](https://learn.microsoft.com/dotnet/api/system.operationcanceledexception) → rollback 后重抛（遵 BCL 惯例，inner Token 保留）；并发冲突（EF Core 抛 [`DbUpdateConcurrencyException`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbupdateconcurrencyexception)）→ facade 在 catch 处包装为 `throw new InvalidOperationException("Optimistic concurrency conflict: <entity> id=<id>", innerDbUpdateConcurrencyException)`后 rollback；DB 超时 → `TimeoutException` rollback 后重抛；DB 连接失败 → `IOException` rollback 后重抛；`GetRepository<TRepository>()` 类型未注册 → `InvalidOperationException`（message 前缀 `"Required repository type not registered:"`，与 §3.3 `IUnitOfWork.GetRepository<T>` 一致） |
| 日志要求     | 实现层（HD-009）在 `ExecuteInTransactionAsync` 入口 / 出口写 OTel span `db.transaction`，字段：`db.transaction.scope_id` / `db.transaction.outcome=committed/rolled_back/cancelled` / OTel [`exception.*` 五字段](https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/)（失败时）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| 测试要求     | `tests/core/Inkwell.Abstractions.Tests/Persistence/IPersistenceProviderContractTests.cs`：契约测试（接口形态 ABI 锁定 via [`PublicApiAnalyzers`](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md)）；行为测试在 HD-009 起草                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |

### 3.2 `Persistence/IRepository.cs`

> **2026-05-11 errata（F6 + ADR-022 + B 路径）**：`IRepository<TModel, TKey>` 改为**纯 marker interface**——删除 6 个泛型 CRUD 方法（`AddAsync` / `UpdateAsync` / `GetByIdAsync` / `ListAsync` / `RemoveAsync` / `ExistsAsync`）；具名 `IXxxRepository`（业务 HD 起草）继承本 marker 后**独立定义**具名方法 `AddXxx` / `UpdateXxx` / `GetXxx` / `DeleteXxx` / `ListXxxs` / `FindXxxsByYyy`（见 §4.1.3 动词白名单）。泛型 base 仅承担 (1) where 约束 + (2) 与 mixin 联动的反射目标。CI BannedSymbols 禁出现 `IXxxRepository.AddAsync` / `UpdateAsync` / 等泛型同名方法。

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Persistence/IRepository.cs`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| 职责         | Generic Repository **marker 基类**；具名 `IXxxRepository`（业务 HD 起草）继承本 marker 锁定 (1) `TModel` / `TKey` 类型参数 + (2) 凡声明 `IXxxRepository` 必符合 Repository 角色（与 BannedSymbols 配套作为禁 `Store` / `Dao` / `Gateway` 后缀的正向锚点）；**不**强制 CRUD 方法名 / 签名，全部由具名 Repo 独立定义具名方法（§4.1.3）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| 对外接口     | `public interface IRepository<TModel, TKey> where TModel : class where TKey : notnull { }`（marker，无成员）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| 内部函数或类 | 接口本身；具名子接口由业务 HD 定义。**示例**：`public interface IAgentRepository : IRepository<AgentDefinition, Guid> { Task<AgentDefinition> AddAgent(AgentDefinition agent, CancellationToken ct = default); Task UpdateAgent(AgentDefinition agent, CancellationToken ct = default); Task<AgentDefinition> GetAgent(Guid id, CancellationToken ct = default); Task<bool> DeleteAgent(Guid id, CancellationToken ct = default); Task<PagedResult<AgentDefinition>> ListAgents(Pagination pagination, SortOrder sort, CancellationToken ct = default); Task<IReadOnlyList<AgentDefinition>> FindAgentsByOwner(Guid ownerUserId, CancellationToken ct = default); }`                                                                                                                                                                                                                                                                                                                                                |
| 输入数据     | 类型参数 `TModel` / `TKey`；具名 Repo 方法参数由各业务 HD 锁定                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| 输出数据     | 类型参数 `TModel`；具名 Repo 返回裸 `Task<TModel>` / `Task<PagedResult<TModel>>` / `Task<IReadOnlyList<TModel>>` / `Task` / `Task<bool>` 等（按 [HD-001 §5.2](HD-001-Inkwell.Abstractions-foundation.md#52-method-签名) 风格）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| 依赖模块     | 无（纯 marker，零成员）；具名 Repo 通过 `Common/Pagination.cs` / `Common/SortOrder.cs` / `PagedResult.cs` 拼装                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| 错误处理     | 本 marker 自身无错误处理；具名 Repo 遵 BCL 对照表（[ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) + [HD-001 §5.3](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)）：`GetXxx` 找不到 → [`KeyNotFoundException`](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)；`AddXxx` 唯一约束冲突 → [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)（message 前缀 `"Duplicate key:"`）；`UpdateXxx` 找不到 → `KeyNotFoundException`；`UpdateXxx` 并发冲突（Model 实现 `IHasRowVersion`）→ `InvalidOperationException`（message 前缀 `"Optimistic concurrency conflict:"`，inner = `DbUpdateConcurrencyException`）；命令超时 → [`TimeoutException`](https://learn.microsoft.com/dotnet/api/system.timeoutexception)；`DeleteXxx` 幂等 → 返回 `Task<bool>`（`true` = 实际删 / `false` = 本不存在） |
| 日志要求     | 实现层（HD-009）每个具名方法写 OTel span：`db.repository.<entity>.<verb>`（如 `db.repository.agent.add` / `db.repository.agent.find_by_owner`），字段 `db.entity_type` / `db.key` / OTel [`exception.*` 五字段](https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/)（失败时）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               |
| 测试要求     | `IRepositoryContractTests.cs`：契约测试 = marker shape（仅 where 约束、零方法）；`BannedSymbolsTests.cs` 验证 `Inkwell.Abstractions.Persistence.IXxxRepository` 任何 `*Async` 命名出现 → 编译阻塞；具名 Repo 行为测试由各业务 HD + HD-009 起草                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |

### 3.3 `Persistence/IUnitOfWork.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Persistence/IUnitOfWork.cs`                                                                                                                                                                                                                                                                                                                                                                                                                 |
| 职责         | 事务作用域；仅在 `IPersistenceProvider.ExecuteInTransactionAsync` 内部生命周期可见；不允许业务持有跨事务边界                                                                                                                                                                                                                                                                                                                                                               |
| 对外接口     | `public interface IUnitOfWork { TRepository GetRepository<TRepository>() where TRepository : class; Task<int> SaveChangesAsync(CancellationToken ct = default); }`                                                                                                                                                                                                                                                                                                         |
| 内部函数或类 | 接口本身；实现由 HD-009 `EfCoreUnitOfWork` 提供（包装当前事务的 `InkwellDbContext`）                                                                                                                                                                                                                                                                                                                                                                                       |
| 输入数据     | `TRepository` 类型（generic）—— 业务在 lambda 内 `var agents = uow.GetRepository<IAgentRepository>();`                                                                                                                                                                                                                                                                                                                                                                     |
| 输出数据     | `TRepository` 实例 / `int` 影响行数                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| 依赖模块     | System.\*                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| 错误处理     | `GetRepository<T>` 类型未注册 → 抛 [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)（message 前缀 `"Required repository type not registered:"`，含请求的类型名）；`SaveChangesAsync` 并发冲突 → `InvalidOperationException`（message 前缀 `"Optimistic concurrency conflict"`，inner = `DbUpdateConcurrencyException`）；超时 → [`TimeoutException`](https://learn.microsoft.com/dotnet/api/system.timeoutexception) |
| 日志要求     | 实现层（HD-009）在 `GetRepository<T>` 命中时写 debug 日志 `unit_of_work.repository_resolved`，字段 `repository_type`                                                                                                                                                                                                                                                                                                                                                       |
| 测试要求     | `IUnitOfWorkContractTests.cs`：契约测试；行为测试在 HD-009                                                                                                                                                                                                                                                                                                                                                                                                                 |

### 3.4 `Persistence/PagedResult.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                            |
| ------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Persistence/PagedResult.cs`                                                                                                                                                                                                                                                                                                                                                      |
| 职责         | 列表分页返回 DTO；包含数据 + 总条数 + 当前分页参数                                                                                                                                                                                                                                                                                                                                                              |
| 对外接口     | `public sealed record PagedResult<T>(IReadOnlyList<T> Items, long TotalCount, Pagination Pagination) { public int TotalPages => (int)Math.Ceiling((double)TotalCount / Pagination.PageSize); public bool HasNextPage => Pagination.Page < TotalPages; public bool HasPreviousPage => Pagination.Page > 1; public static PagedResult<T> Empty(Pagination pagination) => new(Array.Empty<T>(), 0, pagination); }` |
| 内部函数或类 | 派生属性 `TotalPages` / `HasNextPage` / `HasPreviousPage`；构造期校验 `TotalCount >= 0`、`Items` 非 null（违反 → `ArgumentException`）                                                                                                                                                                                                                                                                          |
| 输入数据     | `IReadOnlyList<T>` / `long TotalCount` / `Pagination`                                                                                                                                                                                                                                                                                                                                                           |
| 输出数据     | `PagedResult<T>` 实例                                                                                                                                                                                                                                                                                                                                                                                           |
| 依赖模块     | `Common/Pagination.cs` / System.*                                                                                                                                                                                                                                                                                                                                                                               |
| 错误处理     | `TotalCount < 0` → `ArgumentOutOfRangeException`；`Items` 为 null → `ArgumentNullException`                                                                                                                                                                                                                                                                                                                     |
| 日志要求     | DTO 自身不做日志；调用方在 List 日志输出 `result.total_count` / `result.items_returned`                                                                                                                                                                                                                                                                                                                         |
| 测试要求     | `PagedResultTests.cs`：边界（空列表 / 单页 / 多页 / 越页）、`Empty` 工厂、派生属性正确性                                                                                                                                                                                                                                                                                                                        |

### 3.5 `Persistence/PersistenceOptions.cs`

> **2026-05-10 errata（F9 形态 C）**：删除 `Provider` 字段——Provider 选择上移到 [HD-001 §3.11.1 `InkwellProvidersOptions`](HD-001-Inkwell.Abstractions-foundation.md) `Inkwell:Providers:Persistence`。`PersistenceOptions` 仅承载详细连接 / 超时 / Seed / SensitiveLogging 配置。

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| ------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Persistence/PersistenceOptions.cs`                                                                                                                                                                                                                                                                                                                                                                                   |
| 职责         | 持久化端口详细配置；从 `appsettings.json` `"Inkwell:Persistence"` 段绑定                                                                                                                                                                                                                                                                                                                                                                            |
| 对外接口     | `public sealed class PersistenceOptions { [Required] public string ConnectionString { get; init; } = string.Empty; [Range(1, 300)] public int CommandTimeoutSeconds { get; init; } = 30; [Range(60, 1800)] public int MigrationTimeoutSeconds { get; init; } = 300; public bool AutoSeedOnStartup { get; init; } = true; public bool EnableSensitiveDataLogging { get; init; } = false; public bool EnableDetailedErrors { get; init; } = false; }` |
| 内部函数或类 | DataAnnotations 校验；**不**再承载 Provider 白名单（上移至 `InkwellProvidersOptions`）                                                                                                                                                                                                                                                                                                                                                              |
| 输入数据     | 由 `IConfiguration` 绑定                                                                                                                                                                                                                                                                                                                                                                                                                            |
| 输出数据     | `PersistenceOptions` 实例（DI 通过 `IOptions<PersistenceOptions>` 注入）                                                                                                                                                                                                                                                                                                                                                                            |
| 依赖模块     | `System.ComponentModel.DataAnnotations`                                                                                                                                                                                                                                                                                                                                                                                                             |
| 错误处理     | DataAnnotations 校验失败 → `OptionsValidationException`，host 兜底；Provider 不一致由 Builder DSL 抓 `InkwellBuilderException`                                                                                                                                                                                                                                                                                                                      |
| 日志要求     | DI 启动期 `IValidateOptions` 失败时输出 OTel `exception.type=Microsoft.Extensions.Options.OptionsValidationException`（HD-001 §5.3）                                                                                                                                                                                                                                                                                                                |
| 测试要求     | `PersistenceOptionsTests.cs`：默认值、`appsettings.json` 绑定、`CommandTimeoutSeconds` 边界（1 / 300 / 越界）、`AutoSeedOnStartup` 默认 true（[ADR-021 D2](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）、未再承载 Provider 字段                                                                                                                                                                |

### 3.6 `Persistence/PersistenceOptionsValidator.cs`

> **2026-05-10 errata（F9 形态 C）**：删除 Provider 白名单逻辑——Provider 选择校验上移至 Builder DSL 装配期。Validator 仅保留 DataAnnotations + ConnectionString / 超时跨字段校验。
>
> **2026-05-11 errata·第三轮补说**（[ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流)）：Builder DSL 装配期废错误码 `INK-CORE-006`，改为抛 `InkwellBuilderException(message)`（message 含具体冲突描述）。

| 字段         | 内容                                                                                                                                                                                                                                                                             |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Persistence/PersistenceOptionsValidator.cs`                                                                                                                                                                                                       |
| 职责         | `IValidateOptions<PersistenceOptions>` 实现；DataAnnotations + 跨字段校验；**不**负责 Provider 白名单校验                                                                                                                                                                        |
| 对外接口     | `internal sealed class PersistenceOptionsValidator : IValidateOptions<PersistenceOptions> { public ValidateOptionsResult Validate(string? name, PersistenceOptions options); }`                                                                                                  |
| 内部函数或类 | (1) `Validator.TryValidateObject` DataAnnotations；(2) `MigrationTimeoutSeconds >= CommandTimeoutSeconds`；(3) ConnectionString 跨 Provider 表现 由 Builder DSL 装配期交叉校验（如 `Inkwell:Providers:Persistence` 为任何已知值时不允许空 `ConnectionString`）——不在本 Validator |
| 输入数据     | `PersistenceOptions` 实例                                                                                                                                                                                                                                                        |
| 输出数据     | `ValidateOptionsResult.Success` / `Fail(IEnumerable<string>)`                                                                                                                                                                                                                    |
| 依赖模块     | `Microsoft.Extensions.Options` / `System.ComponentModel.DataAnnotations`                                                                                                                                                                                                         |
| 错误处理     | 同 HD-001 §3.12，校验失败 → `Fail` 含全部消息                                                                                                                                                                                                                                    |
| 日志要求     | 失败由 `OptionsValidationException` 抛出，host 打 fatal                                                                                                                                                                                                                          |
| 测试要求     | `PersistenceOptionsValidatorTests.cs`：(1) DataAnnotations 边界合格、`MigrationTimeoutSeconds < CommandTimeoutSeconds` 拒；(2) **本 Validator 不再拒 `Provider="MySQL"`**，该测试上移至 Builder DSL 单元测试                                                                     |

### 3.7 `Persistence/Mixins/IHasTimestamps.cs`

> **2026-05-10 errata（F2）**：字段名 `CreatedAtUtc` / `UpdatedAtUtc` → `CreatedTime` / `UpdatedTime`；类型保持 `DateTimeOffset` UTC；列名按 F5 解释 A 直接 `CreatedTime` / `UpdatedTime`（PascalCase 直入，不转 snake_case）。

| 字段         | 内容                                                                                                                                                                                                                                                      |
| ------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Persistence/Mixins/IHasTimestamps.cs`                                                                                                                                                                                      |
| 职责         | 标记 mixin；Model 实现该接口表示需要 `CreatedTime` / `UpdatedTime` 字段；`providers/Persistence/Inkwell.Persistence.EFCore` 在 `OnModelCreating`（HD-009）扫描该 mixin 自动配置 EF 行为（`SaveChangesInterceptor` 自动填充 / 更新）                                   |
| 对外接口     | `public interface IHasTimestamps { DateTimeOffset CreatedTime { get; } DateTimeOffset UpdatedTime { get; } }`                                                                                                                                             |
| 内部函数或类 | 仅 readonly 属性；setter 由具名 Model 实现决定（`init` 或 `private set`）                                                                                                                                                                                 |
| 输入数据     | 实现方：Provider 写入时设置 `CreatedTime = DateTimeOffset.UtcNow`，更新时设置 `UpdatedTime = DateTimeOffset.UtcNow`                                                                                                                                       |
| 输出数据     | 时间戳值（[`DateTimeOffset`](https://learn.microsoft.com/dotnet/api/system.datetimeoffset)）                                                                                                                                                              |
| 依赖模块     | System.*                                                                                                                                                                                                                                                  |
| 错误处理     | mixin 不抛错；Provider 在 `SaveChangesAsync` 时间戳未填 → 抛 [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)（message `"Timestamp not initialized: CreatedTime/UpdatedTime missing on <entity>"`） |
| 日志要求     | 不直接打日志；HD-009 实现层在 OTel span `db.save_changes` 中输出 `entity.created_time` / `entity.updated_time`                                                                                                                                            |
| 测试要求     | `IHasTimestampsContractTests.cs`：契约测试；行为测试在 HD-009                                                                                                                                                                                             |

### 3.8 `Persistence/Mixins/IHasRowVersion.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                           |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 文件路径     | `src/core/Inkwell.Abstractions/Persistence/Mixins/IHasRowVersion.cs`                                                                                                                                                                                                                                                                                                           |
| 职责         | 标记 mixin；Model 实现该接口表示需要乐观并发控制；`providers/Persistence/Inkwell.Persistence.EFCore` 在 `OnModelCreating`（HD-009）自动调 [`IsRowVersion()`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.metadatabuilders.propertybuilder.isrowversion)                                                                                                           |
| 对外接口     | `public interface IHasRowVersion { byte[] RowVersion { get; } }`                                                                                                                                                                                                                                                                                                               |
| 内部函数或类 | 仅 readonly 属性；`byte[]` 由 EF Core 自动管理（SqlServer = `rowversion`；Postgres = 应用层拦截器手动递增）                                                                                                                                                                                                                                                                    |
| 输入数据     | 实现方：Provider SaveChanges 时 EF Core 自动填充与校验                                                                                                                                                                                                                                                                                                                         |
| 输出数据     | `byte[]` row version 值                                                                                                                                                                                                                                                                                                                                                        |
| 依赖模块     | System.*                                                                                                                                                                                                                                                                                                                                                                       |
| 错误处理     | 并发冲突由 EF Core 抛 [`DbUpdateConcurrencyException`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbupdateconcurrencyexception) → Provider 包装为 [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)（message 前缀 `"Optimistic concurrency conflict"`，inner = `DbUpdateConcurrencyException`） |
| 日志要求     | 不直接打日志；HD-009 在并发冲突时 OTel span `db.concurrency_conflict` 输出 `entity.type` / `entity.id` / [`exception.type`](https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/) = `System.InvalidOperationException`                                                                                                                                   |
| 测试要求     | `IHasRowVersionContractTests.cs`：契约测试；并发场景行为测试在 HD-009                                                                                                                                                                                                                                                                                                          |

### 3.9 `Persistence/Mixins/IHasOwner.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                        |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Persistence/Mixins/IHasOwner.cs`                                                                                                                                                                                                                             |
| 职责         | 标记 mixin；Model 实现该接口表示需要 `OwnerUserId` 字段（[REQ-002 Agent 创建者](../../01-requirements/requirements.md) / [REQ-006 对话归属](../../01-requirements/requirements.md)）；`providers/Persistence/Inkwell.Persistence.EFCore` 在 `OnModelCreating`（HD-009）自动配置该列为 `NOT NULL` + 索引 |
| 对外接口     | `public interface IHasOwner { Guid OwnerUserId { get; } }`                                                                                                                                                                                                                                  |
| 内部函数或类 | 仅 readonly 属性                                                                                                                                                                                                                                                                            |
| 输入数据     | 实现方：业务在 `AddXxx` 前设置；Provider 不自动填充（强制业务显式赋值，避免错误归属）                                                                                                                                                                                                       |
| 输出数据     | `Guid` 用户 ID（v7）                                                                                                                                                                                                                                                                        |
| 依赖模块     | System.*                                                                                                                                                                                                                                                                                    |
| 错误处理     | mixin 不抛错；业务忘填（`Guid.Empty`）→ 由具体 Repository 实现校验 → 抛 [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception)（message `"OwnerUserId cannot be Guid.Empty"`，`paramName` = `"OwnerUserId"`）                                               |
| 日志要求     | 不直接打日志；HD-009 在 List/Get 时输出 `entity.owner_user_id` 维度                                                                                                                                                                                                                         |
| 测试要求     | `IHasOwnerContractTests.cs`：契约测试                                                                                                                                                                                                                                                       |

### 3.10 `ErrorCodes.Persist.cs`【已废锁位】

> **2026-05-11 errata·第二+三+四轮废止**（[ADR-023 errata·01 废错误码机制](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流)）：`ErrorCodes.Persist.cs` 不起草、不创建。原定义的 13 个常量（INK-PERSIST-001 ~ INK-PERSIST-013）全部作废；错误语义走 [.NET BCL 异常类型](https://learn.microsoft.com/dotnet/standard/exceptions/)表达 + OTel [`exception.*` 五字段](https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/)跨服务 trace。详 [HD-001 §5.3 BCL 对照表](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)。§3.1 ~ §3.9 编号不调整以保追溯不断；§3.10 作为“已废”锁位保留不重用。

## 4. 接口公共约定（落 HD-001 §5 通用规则）

### 4.1 命名

#### 4.1.1 总体命名（HD-002 锁定）

- 具名 Repository = `I<TypeName>Repository`，必须继承 `IRepository<TModel, TKey>` marker（不强制 `where TKey = Guid`，但 Q2 决策推荐 `Guid`）
- Model = `<TypeName>` 默认无后缀（如 `Conversation` / `Skill` / `KnowledgeBase` / `MemoryItem`）；与外部库 type 撞名时按 §4.1.2 降级 `XxxDefinition`
- Options = `PersistenceOptions`（HD-002 锁定）；具名 sub-Options（如某 Provider 特殊配置）由各业务 HD 决定

#### 4.1.2 Model 类命名规则（2026-05-11 errata（F6 + ADR-022））

[ADR-022 §决策](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 锁定本规则。

**默认**：业务 Model 类**无后缀**——`Conversation` / `Skill` / `KnowledgeBase` / `MemoryItem` / `Trigger` 等直接作为 Model 类名。

**降级规则**（仅命名冲突场景使用，按下表枚举判定）：

| Inkwell Model                                                                                                                                                                        | 外部冲突源                                                                                                                      | 降级后              |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------- | ------------------- |
| `Agent`                                                                                                                                                                              | [`Microsoft.Agents.AI.AIAgent`](https://learn.microsoft.com/dotnet/ai/) / `Microsoft.Agents.AI.Agent`                           | `AgentDefinition`   |
| `Tool`                                                                                                                                                                               | [`Microsoft.Agents.AI.AIFunction`](https://learn.microsoft.com/dotnet/ai/)（语义近似）                                          | `ToolDefinition`    |
| `Skill`                                                                                                                                                                              | 业界 Skill / Plugin 概念广义重名 + Inkwell 内 [REQ-008 Skill 动态加载语义](../../01-requirements/requirements.md)强烈"配置"取向 | `SkillDefinition`   |
| `Trigger`                                                                                                                                                                            | 业界 Trigger 概念广义重名 + Inkwell 内 [REQ-011 Trigger fan-out 配置语义](../../01-requirements/requirements.md)                | `TriggerDefinition` |
| `Conversation` / `Message` / `KnowledgeBase` / `KbDocument` / `KbChunk` / `MemoryItem` / `Orchestration` / `OrchestrationRun` / `Trace` / `User` / `PublicApiToken` / `AguiRunEvent` | 无冲突                                                                                                                          | 保持无后缀          |

**判定原则**：

1. **代码生成、反射、动态加载场景**（[REQ-002 Agent 配置](../../01-requirements/requirements.md) / [REQ-007 Tool 配置](../../01-requirements/requirements.md) / [REQ-008 Skill 配置](../../01-requirements/requirements.md) / [REQ-011 Trigger 配置](../../01-requirements/requirements.md)）的"**配置元数据**"类型——`XxxDefinition` 作为后缀（语义=配置定义，与 runtime instance 区分）
2. **运行期数据载体类型**（如 `Conversation` / `Message` / `MemoryItem`）—— 保持无后缀
3. **`XxxModel` 后缀禁用**——该后缀语义广义且与"Model-View-Controller"印象冲突，强制走 §4.1.2 上表的精确语义后缀
4. **冷僻场景后缀**（仅在有明确语义需求时使用，不作默认降级）：
   - `XxxRecord`：不可变快照（如版本快照、历史记录）
   - `XxxSnapshot`：某一时点状态快照
   - `XxxOptions`：配置选项绑定 [`IOptions<T>`](https://learn.microsoft.com/dotnet/core/extensions/options)（已在 HD-001 §3.11 锁定）

**CI 强制**：`BannedSymbols.txt` 补：

```text
T:Inkwell.Abstractions.Persistence.AgentModel; Use Agent or AgentDefinition per HD-002 §4.1.2
T:Inkwell.Abstractions.Persistence.ConversationModel; Use Conversation per HD-002 §4.1.2
T:Inkwell.Abstractions.Persistence.SkillModel; Use SkillDefinition per HD-002 §4.1.2
T:Inkwell.Abstractions.Persistence.ToolModel; Use ToolDefinition per HD-002 §4.1.2
T:Inkwell.Abstractions.Persistence.TriggerModel; Use TriggerDefinition per HD-002 §4.1.2
# regex 兜底（H4 用例验证）：任何 *Model 类型（除 *Options）出现在 Inkwell.Abstractions.Persistence.* 命名空间均触发 BannedSymbols 报错
```

#### 4.1.3 Repository 方法动词白名单（2026-05-11 errata（F6 + ADR-022））

[ADR-022 §决策](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 锁定全部业务命名空间下具名 Repository 方法命名一致性。

**白名单动词**（具名 Repo 必须从中挑选）：

| 动词                       | 语义                | 示例                                                                                |
| -------------------------- | ------------------- | ----------------------------------------------------------------------------------- |
| `Add<Type>`                | 新增单条            | `AddAgent(AgentDefinition agent, CancellationToken ct = default)`                   |
| `Update<Type>`             | 更新单条            | `UpdateAgent(AgentDefinition agent, CancellationToken ct = default)`                |
| `Get<Type>`                | 按主键单条精确查询  | `GetAgent(Guid id, CancellationToken ct = default)`                                 |
| `Delete<Type>`             | 按主键删除          | `DeleteAgent(Guid id, CancellationToken ct = default)`                              |
| `List<Type>s`              | 分页 + 排序批量查询 | `ListAgents(Pagination pagination, SortOrder sort, CancellationToken ct = default)` |
| `Find<Type>sBy<Condition>` | 按业务条件批量查询  | `FindAgentsByOwner(Guid ownerUserId, CancellationToken ct = default)`               |

**禁用动词**（CI 走 [`Microsoft.CodeAnalysis.BannedApiAnalyzers`](https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.BannedApiAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers.md) 强制）：

- `Query<Type>` / `Fetch<Type>` / `Retrieve<Type>`：与 `Get` / `Find` 语义重叠，造成动词漂移
- `AddAsync` / `UpdateAsync` / `GetByIdAsync` / `ListAsync` / `RemoveAsync` / `ExistsAsync`：泛型 base 方法名禁出现在具名 `IXxxRepository` 上（参 §3.2 errata + B 路径）
- `Save<Type>`：与 `Add` / `Update` 二义，禁用（如需 upsert 语义，独立命名 `UpsertAgent` 并在具名 Repo 显式标注）

**冷僻动词例外**（仅在 H3 / H4 评审通过后使用，独立 picker 拍板）：

- `Upsert<Type>` / `BulkAdd<Type>s` / `Archive<Type>` / `Restore<Type>`：业务真正需要时按字面语义独立命名

**示例补全**（沿用 §3.2 IAgentRepository 完整签名）：

```csharp
public interface IAgentRepository : IRepository<AgentDefinition, Guid>
{
    Task<AgentDefinition>                AddAgent     (AgentDefinition agent, CancellationToken ct = default);
    Task                                 UpdateAgent  (AgentDefinition agent, CancellationToken ct = default);
    Task<AgentDefinition>                GetAgent     (Guid id, CancellationToken ct = default);
    Task<bool>                           DeleteAgent  (Guid id, CancellationToken ct = default);
    Task<PagedResult<AgentDefinition>>   ListAgents   (Pagination pagination, SortOrder sort, CancellationToken ct = default);
    Task<IReadOnlyList<AgentDefinition>> FindAgentsByOwner(Guid ownerUserId, CancellationToken ct = default);
}
```

#### 4.1.4 文件名 = 类名约定（2026-05-11 errata（F6 + ADR-022））

[ADR-022 §决策](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 锁定。

- 每个 Model 类一文件；文件名 = 类名 + `.cs`（如 `Agent` → `Agent.cs`；`AgentDefinition` → `AgentDefinition.cs`）
- 每个 `IXxxRepository` 接口一文件；文件名 = 类名 + `.cs`（如 `IAgentRepository` → `IAgentRepository.cs`）
- 同业务模块 Model + Repository 共置于 `Inkwell.Abstractions/Persistence/<Module>/`（如 `Inkwell.Abstractions/Persistence/Agents/Agent.cs` + `Inkwell.Abstractions/Persistence/Agents/IAgentRepository.cs`）—— 子目录天然按 Module 物理消歧，命名空间 `Inkwell.Abstractions.Persistence.Agents`

#### 4.1.5 Repository 命名禁用清单（2026-05-10 errata（F7））

为保证全仓 Repository 接口名字一致、避免不同业务 HD 采用不同后缀导致语义漂移，**本 HD 锁定 `IXxxRepository` 为唯一同义名**，以下后缀被禁用：

- `IXxxStore` —— 与 VectorStore 词汇冲突（[ADR-020](../../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) 复用 `Microsoft.Extensions.VectorData.VectorStore` 名，主动遇名会产生误读）
- `IXxxDao` —— Java 不动产术语，与 .NET / EFCore 生态不谐
- `IXxxGateway` —— 与外部服务网关（如 LLM provider gateway）词汇冲突

**CI 强制**：在 `src/core/Inkwell.Abstractions.Tests/.editorconfig` 或 csproj 级 [`Microsoft.CodeAnalysis.BannedApiAnalyzers`](https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.BannedApiAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers.md) `BannedSymbols.txt` 补：

```text
T:Inkwell.Abstractions.Persistence.IXxxStore; Use I<TypeName>Repository instead per HD-002 §4.1.5
T:Inkwell.Abstractions.Persistence.IXxxDao;   Use I<TypeName>Repository instead per HD-002 §4.1.5
T:Inkwell.Abstractions.Persistence.IXxxGateway; Use I<TypeName>Repository instead per HD-002 §4.1.5
```

实际表现为：任何 `IXxxStore` / `IXxxDao` / `IXxxGateway` 在 `Inkwell.Abstractions.Persistence.*` 命名空间下出现 → CI 编译报错；H4 起草时补对应测试用例验证 BannedSymbols 生效。

### 4.2 Method 签名（强约束）

- 所有异步方法：裸 `Task<T>` / `Task` / `Task<bool>` / `IAsyncEnumerable<T>` + `CancellationToken ct = default`（[HD-001 §5.2](HD-001-Inkwell.Abstractions-foundation.md#52-method-签名) + [ADR-023 核心边界 · 第 1 条](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)）——但**具名 Repository 方法不加 `Async` 后缀**（§4.1.3 动词白名单：`AddAgent` / `GetAgent` / 等）；该例外仅限定于继承 `IRepository<TModel, TKey>` marker 的具名 `IXxxRepository`。其他路径（`IPersistenceProvider` / `IUnitOfWork` / 业务 service）仍遵循 `Async` 后缀规范。
- List 方法返回裸 `Task<PagedResult<TModel>>`，**不**返回 `Task<IEnumerable<TModel>>` / `Task<List<TModel>>`（强制分页）
- Add 返回新增的 Model（含 Provider 生成的 timestamps / RowVersion），方便业务取最新状态
- Update 返回裸 `Task`（不带 value）；Delete 返回 `Task<bool>`（幂等：`true` = 实际删 / `false` = 本不存在）

### 4.3 错误处理（细化 HD-001 §5.3 BCL 对照表）

业务失败 / 程序错误全走 [.NET BCL 异常类型](https://learn.microsoft.com/dotnet/standard/exceptions/)：

| 业务语义                                             | BCL 异常                                                                                                         | message 约定                                                                                                                    |
| ---------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| 实体不存在（`GetXxx` / `UpdateXxx`）                 | [`KeyNotFoundException`](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception) | `"<EntityType> not found: id=<id>"`                                                                                             |
| 唯一约束冲突（`AddXxx`）                             | [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)           | message 前缀 `"Duplicate key:"`                                                                                                 |
| 并发冲突（`UpdateXxx`、Model 实现 `IHasRowVersion`） | `InvalidOperationException`                                                                                      | message 前缀 `"Optimistic concurrency conflict:"`，inner = `DbUpdateConcurrencyException`                                       |
| 命令超时                                             | [`TimeoutException`](https://learn.microsoft.com/dotnet/api/system.timeoutexception)                             | `"Command timeout: <seconds>s"`                                                                                                 |
| 连接失败 / IO 故障                                   | [`IOException`](https://learn.microsoft.com/dotnet/api/system.io.ioexception)                                    | `"Persistence connection failed: <provider>"`                                                                                   |
| Owner 未填（`Guid.Empty`）                           | [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception)                           | `paramName="OwnerUserId"`、`"OwnerUserId cannot be Guid.Empty"`                                                                 |
| 事务回滚                                             | `InvalidOperationException`                                                                                      | message 前缀 `"Transaction rolled back:"`，inner = 原异常                                                                       |
| 取消                                                 | [`OperationCanceledException`](https://learn.microsoft.com/dotnet/api/system.operationcanceledexception)         | 直接重抛（遵 BCL 惯例，不包装）                                                                                                 |
| DI 装配失败                                          | `InkwellConfigurationException`                                                                                  | message 含具体不一致点（[HD-001 §3.3](HD-001-Inkwell.Abstractions-foundation.md#33-commoninkwellexceptioncs) 锁定的两子类之一） |
| Builder 链冲突                                       | `InkwellBuilderException`                                                                                        | message 含具体冲突描述（另一个保留子类）                                                                                        |

### 4.4 OTel 字段（细化 HD-001 §4.2）

| 字段                                                                                                                                             | 来源                                                                          | 何时输出                                                                                                                                                                  |
| ------------------------------------------------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `db.system`                                                                                                                                      | Provider 类型（`mssql` / `postgresql`）                                       | 每个 db 操作 span                                                                                                                                                         |
| `db.statement_summary`                                                                                                                           | EF Core 生成 SQL 的脱敏摘要（`SELECT ... FROM agents WHERE id = @id`）        | 仅在 `EnableSensitiveDataLogging=false` 时使用脱敏摘要；true 时直接输出原 SQL                                                                                             |
| `db.transaction.scope_id`                                                                                                                        | `IUnitOfWork` 实例 hashcode                                                   | `ExecuteInTransactionAsync` 入口                                                                                                                                          |
| `db.transaction.outcome`                                                                                                                         | `committed` / `rolled_back` / `cancelled`                                     | `ExecuteInTransactionAsync` 出口                                                                                                                                          |
| `db.entity_type`                                                                                                                                 | `TModel.GetType().Name`                                                       | Repository 操作 span                                                                                                                                                      |
| `db.repository.<entity>.<verb>`                                                                                                                  | span 名（如 `db.repository.agent.add` / `db.repository.agent.find_by_owner`） | 每个具名 Repository 方法                                                                                                                                                  |
| [`exception.type` / `.message` / `.stacktrace` / `.escaped` / `.id`](https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/) | OTel 标准五字段（取代 `error.code`）                                          | 异常报发时（[ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流)） |

## 5. 拓扑约束（重复 HD-001 §5.4 + Persist 特定）

- **`Provider` 选择上移到 `Inkwell:Providers:Persistence`**（F9）：二值白名单 `"SqlServer"` / `"PostgreSQL"` 由 [HD-001 §3.11.1 `InkwellProvidersOptions`](HD-001-Inkwell.Abstractions-foundation.md) 锁定（[ADR-004 §决策](../../03-architecture/adr/ADR-004-data-store-provider-switchable-ef-core.md)）—— v1 范围严格；新增 Provider 须新 ADR
- **`AutoSeedOnStartup` 默认 true**（[ADR-021 D2](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）—— Builder DSL `.AutoSeedOnStartup(false)` 由 final adapter csproj 提供（HD-011 / HD-012）
- **`Inkwell.Abstractions/Persistence/` 不得**引入任何 EF Core 包；EF 实现严格隔离在 `providers/Persistence/Inkwell.Persistence.EFCore/`（[ADR-021](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）
- **业务命名空间不得**直接 inject `IUnitOfWork`；只能通过 `IPersistenceProvider.ExecuteInTransactionAsync` 在 lambda 内访问—— `IUnitOfWork` 实例不允许跨事务边界外漏

### 5.1 EFCore 配置公开约定（2026-05-10 errata）

以下两条公开约定锁定端口套同 HD-009 EFCore base 实现之间的衔接语义（实现细节仅在 HD-009 ship，本 HD 只锁定语义）：

#### 5.1.1 enum 存储约定（F4）

- 所有 enum 类型 Model 属性销售到底层存储时，必须由 HD-009 EFCore base 统一以 `HasConversion<string>().HasMaxLength(64)` 配置为 `string` 列（而非 `int`），以保证两 Provider 存储一致（SqlServer / Postgres 不依赖数字枚举偏移）。
- 上限 64 字符足以容纳全仓现有 enum 名称，超过上限的枚举需在提交前重命名。
- 本约定不锁定 `HasConversion<string>()` 的具体实现位置（项目起步阶段可能是 `IModelCustomizer` 或 `IEntityTypeConfiguration<>` 扫描）—— HD-009 起草时锁定。

#### 5.1.2 Attribute / Fluent API 切分边界（F8）

- **单字段约束**（如 `[Required]` / `[MaxLength(N)]` / `[Range(...)]`）允许在 Model / Entity 上使用 DataAnnotations Attribute，同时服务 `IValidateOptions` 校验与 EFCore 列类型推断。
- **跨字段约束**（复合唯一索引 / 外键 / 列类型覆写 / 表名 / index include / Provider-specific 行为）一律走 EFCore Fluent API（`IEntityTypeConfiguration<TEntity>.Configure(EntityTypeBuilder<TEntity>)`）在 HD-009 EFCore base 的 `OnModelCreating` 中锁定。
- 不允许在 Attribute 上表达跨字段 / Provider-specific 逻辑（如 `[Index(...)]` 在多个列上的争引 / `[Column(TypeName = "jsonb")]` Postgres specific 类型）。
- 本约定不锁定 HD-009 中 Attribute / Fluent API 的具体实现顺序与表现—— HD-009 起草时锁定例子 + 调用点。

## 6. Builder DSL 衔接（HD-001 §6.3）

HD-002 不直接定义 `Use*` 扩展方法；`UseSqlServer` / `UsePostgres` 由 final adapter csproj 提供（HD-011 / HD-012）。本 HD 锁定它们的契约：

```csharp
// providers/Persistence/Inkwell.Persistence.EFCore.SqlServer/DependencyInjection/UseSqlServer.cs (HD-011)
public static IInkwellBuilder UseSqlServer(
    this IInkwellBuilder builder,
    string connectionString,
    Action<PersistenceOptions>? configure = null);

// providers/Persistence/Inkwell.Persistence.EFCore/DependencyInjection/AutoSeedOnStartup.cs (HD-009)
public static IInkwellBuilder AutoSeedOnStartup(
    this IInkwellBuilder builder,
    bool enabled);
```

每个 `Use*` 实现必须：(1) 调 `builder.Services.AddDbContext<InkwellDbContext>(o => o.UseXxx(connectionString))`；(2) 注册 `EfCorePersistenceProvider` 为 `IPersistenceProvider` 单例；(3) 注册 `PersistenceOptionsValidator`；(4) 返回 `builder`。

## 7. 性能 / 安全 / 可观测性

- **性能预算**：本 HD 不锁定具体 SLA 数字（业务 HD 起草时按 [requirements.md NFR](../../01-requirements/requirements.md) 锁定）。**建议默认值**（HD-009 / 业务 HD 可覆写）：单实体 `GetByIdAsync` p99 < 30ms（缓存命中 < 5ms，[ADR-016](../../03-architecture/adr/ADR-016-cache-provider-redis.md)）；`ListAsync` 默认 PageSize=20 时 p99 < 100ms；`ExecuteInTransactionAsync` 包装的事务平均 < 200ms
- **安全**：`PersistenceOptions.ConnectionString` 是 secret 字段；不入日志（OTel `db.connection_string` **永远脱敏为** `"<redacted>"`）；通过 [Kubernetes Secret](https://kubernetes.io/docs/concepts/configuration/secret/) / [Docker `.env`](https://docs.docker.com/compose/environment-variables/set-environment-variables/) 注入（[OQ-A006 closed §B](../../03-architecture/open-questions-arch.md)）
- **`EnableSensitiveDataLogging`**：默认 false；prod **不允许** true（HD-009 启动校验：若 `Environment="prod"` 且 `EnableSensitiveDataLogging=true`，抛 `new InkwellConfigurationException("EnableSensitiveDataLogging=true is forbidden in prod")`）——不再占用错误码名额，与 [ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) 保留两子类的决策一致
- **可观测性**：OTel 字段见 §4.4；Grafana 告警建议（HD-009 起草时锁定）：`exception.type="System.InvalidOperationException"` 且 `exception.message` 含 `"Optimistic concurrency conflict"` 5min 内 > 10 次 → P3；`exception.type="System.TimeoutException"` 且 OTel span 名 `db.*` 1min 内 > 5 次 → P2；`db.transaction.outcome=rolled_back` 比例 5min 内 > 5% → P3

## 8. 测试要求

### 8.1 单元测试

- 测试项目：`tests/core/Inkwell.Abstractions.Tests/Persistence/`（与 HD-001 同 csproj，[MSTest.Sdk 4.x](https://github.com/microsoft/testfx) + MTP runner）
- 9 个文件 × 至少 1 个 `*Tests.cs`（见 §3 各小节"测试要求"）
- 覆盖率门槛：`Persistence/Mixins/*` ≥ 95%（接口体积小、易达）；`PagedResult.cs` ≥ 95%；`PersistenceOptions*` ≥ 90%；`IPersistenceProvider` / `IRepository` / `IUnitOfWork` 仅契约测试，行为测试在 HD-009

### 8.2 契约测试

- `IPersistenceProvider` / `IRepository<,>` / `IUnitOfWork` 三个接口的 ABI 用 [`PublicApiAnalyzers`](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) 锁定
- 接口形态变更（add/remove/rename method）→ 需新建 ADR + 影响 HD-009 + 业务 HD

### 8.3 跨 Provider 行为契约测试（前置 HD-009 + HD-011 + HD-012 起草后启动）

- 公共契约用例包：`tests/core/Inkwell.Providers.Contract/Persistence/`（[RISK-002 + RISK-011](../../03-architecture/risk-analysis.md) 缓解地）
- CI matrix：SqlServer + Postgres 两套 Provider（Testcontainers 真实实例）跑同一套契约用例（断言两 Provider 行为一致）
- 用例覆盖：CRUD 基本流 / 并发冲突（`IHasRowVersion`）/ 事务回滚 / 命令超时 / DataSeed 幂等 / Migration 启动

## 9. 部署 / 配置

- `appsettings.json` 示例（**形态 C**：Provider 选择集中在 `Inkwell:Providers`，详细独立在 `Inkwell:Persistence`）：

```json
{
  "Inkwell": {
    "Providers": {
      "Persistence": "PostgreSQL"
    },
    "Persistence": {
      "ConnectionString": "Host=postgres;Database=inkwell;Username=inkwell;Password=<from-env>",
      "CommandTimeoutSeconds": 30,
      "MigrationTimeoutSeconds": 300,
      "AutoSeedOnStartup": true,
      "EnableSensitiveDataLogging": false,
      "EnableDetailedErrors": false
    }
  }
}
```

- dev `docker-compose` `appsettings.Development.json` 用 `Inkwell:Providers:Persistence = "PostgreSQL"` + Testcontainers/真实实例 `ConnectionString`；prod K8s ConfigMap 注入 `Inkwell:Providers:Persistence` 和 `Inkwell:Persistence:ConnectionString`（[ADR-005](../../03-architecture/adr/ADR-005-deployment-docker-compose-aks.md)）
- **Builder DSL 装配期交叉校验**（F9）：`.UseSqlServer(...)` / `.UsePostgres(...)` 装配时读取 `Inkwell:Providers:Persistence`：取值不一致或同一调用两次 → 抛 `new InkwellBuilderException("Provider registration conflict for Persistence: configured=<x>, called=<y>")`（[HD-001 §3.3](HD-001-Inkwell.Abstractions-foundation.md#33-commoninkwellexceptioncs) 锁定的保留子类之一，message 含具体不一致点）

## 10. 决策记录（Picker 拍板）

| 字段                         | 选定值                                                                 | Picker 时间 | 选项来源证据                                                                                                                                     |
| ---------------------------- | ---------------------------------------------------------------------- | ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| Q1 IPersistenceProvider 形态 | C 只做 facade，具名 Repository 推迟到业务 HD                           | 2026-05-10  | [ADR-021](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) + [AGENTS.md §3.2](../../../AGENTS.md) |
| Q2 主键策略                  | A `Guid` v7（`Guid.CreateVersion7()`）                                 | 2026-05-10  | .NET 9+ 内置 + [ADR-004 三 Provider 最小公倍数](../../03-architecture/adr/ADR-004-data-store-provider-switchable-ef-core.md)                     |
| Q3 Base Model                | A interface mixin（`IHasTimestamps` / `IHasRowVersion` / `IHasOwner`） | 2026-05-10  | [agui_run_events 不需所有字段](../../03-architecture/architecture.md)                                                                            |
| Q4 事务暴露                  | A `ExecuteInTransactionAsync<T>` 包装                                  | 2026-05-10  | 避免业务忘 commit                                                                                                                                |
| Q5 软删除                    | D 不提供软删，全部硬删                                                 | 2026-05-10  | v1 不引入软删除机制                                                                                                                              |
| Q6 并发                      | A `IHasRowVersion` mixin + EFCore `IsRowVersion()` 自动 token          | 2026-05-10  | [REQ-002](../../01-requirements/requirements.md) Agent 配置防丢头                                                                                |

## 11. 待补 / 后续 HD 衔接

- **对 HD-001 §3.11 InkwellOptions.cs 的精化**：HD-001 §3.11 内的占位类 `public sealed class PersistenceOptions { }` 由本 HD §3.5 完整定义并搬到 `Persistence/PersistenceOptions.cs`；`InkwellOptions.cs` 引用从 `using Inkwell.Abstractions.Persistence;` 解析。**对 HD-001 reviewer**：本 HD 并非"覆盖" HD-001，而是 HD-001 §3.11 显式让出的"由 HD-002 ~ HD-007 各自补全"职责的兑现
- 具名 `IXxxRepository` 接口由各业务命名空间 HD 起草（每个业务 HD 在自己的 §X 同步追加 `Inkwell.Abstractions/Persistence/<Module>/IXxxRepository.cs` + `<Module>/<TypeName>.cs`，`<TypeName>` 默认与模块同根字（如 `Agent.cs`），撞名时降级 `<TypeName>Definition.cs`，详见 §4.1.2）
- Entity 类（`AgentEntity` 等）+ DbContext + EfCorePersistenceProvider + Seeder + MigrationRunner → HD-009 `providers/Persistence/Inkwell.Persistence.EFCore` shared base
- 两 final adapter（`UseSqlServer` / `UsePostgres`）→ HD-011 / HD-012
- 跨 Provider 契约用例包 `tests/core/Inkwell.Providers.Contract/Persistence/` → 起草于 HD-013（独立 HD，覆盖全部 4 端口家族契约测试）
- **本 HD 不锁定业务命名空间错误语义**：错误处理全走 [.NET BCL 异常类型](https://learn.microsoft.com/dotnet/standard/exceptions/)表达 + OTel [`exception.*` 五字段](https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/)（§4.3 BCL 对照表）；零 `Result<T>` / `Error` 抽象、零错误码表；需返回多项错误场景（如批量校验）走 [`ValidationResult`](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations.validationresult) / `IEnumerable<string>` 等 BCL 对症抽象
- **Soft delete 在 v2 重新评估**——若 v1 上线后需求暴露（如“误删恢复”）促发回烉，HD-002 v2 可加入 `ISoftDeletable` mixin + Global Filter

## 12. 同步追加跨模块文件

- [`docs/04-detailed-design/file-structure.md`](../file-structure.md) — 追加 Inkwell.Abstractions/Persistence/ 子目录文件清单
- [`docs/04-detailed-design/database-design.md`](../database-design.md) — **首次创建**，追加 `## Inkwell.Abstractions（Persistence 端口）` 章节，详见该文件

## 13. Errata 记录（2026-05-10）

本 HD `status: draft` 期间，根据 [`design-review-report.md` §6.4](../design-review-report.md) Owner 二次答复一次性落以下变更（已嵌入 §3 / §4 / §5 / §9，本节是变更摘要）：

- **B2 INK-PERSIST 计数 1 ~ 12 vs 13 不一致**：§3.10 `ErrorCodes.Persist` 补 `EnableSensitiveDataLoggingForbiddenInProd = "INK-PERSIST-013"` 常量；计数区间从 001 ~ 012 扩到 001 ~ 013，与 §7 “EnableSensitiveDataLogging=true is forbidden in prod” 逻辑对齐
- **F2 时间字段命名**：§3.7 `IHasTimestamps.CreatedAtUtc` / `UpdatedAtUtc` → `CreatedTime` / `UpdatedTime`；类型保持 `DateTimeOffset` UTC；日志字段 `entity.created_at` / `entity.updated_at` 同步为 `entity.created_time` / `entity.updated_time`
- **F4 enum 存储公开约定**：§5.1.1 锁定所有 enum 存储走 `HasConversion<string>().HasMaxLength(64)`，实现在 HD-009 EFCore base
- **F7 IXxxRepository 命名约束 + 禁用后缀清单**：§4.1.1 锁定 `IXxxStore` / `IXxxDao` / `IXxxGateway` 为禁用后缀，CI 通过 `BannedSymbols.txt` 强制
- **F8 Attribute / Fluent API 切分边界**：§5.1.2 锁定单字段走 DataAnnotations Attribute、跨字段 / Provider-specific 走 EFCore Fluent API，实现在 HD-009 EFCore base
- **F9 InkwellOptions 形态 C**：
  - §3.5 `PersistenceOptions` **删除** `Provider` 字段——上移至 [HD-001 §3.11.1 `InkwellProvidersOptions`](HD-001-Inkwell.Abstractions-foundation.md) `Inkwell:Providers:Persistence`
  - §3.6 `PersistenceOptionsValidator` 删除 Provider 三值白名单逻辑——上移至 Builder DSL 装配期（`INK-CORE-006`）
  - §5 拓扑约束项目项“三值白名单”描述从 `PersistenceOptions.Provider` 改指向 `InkwellProvidersOptions`
  - §9 appsettings.json 示例改写为 `Inkwell:Providers:Persistence` 选择器 + `Inkwell:Persistence:*` 详细段并列结构
  - §9 补 Builder DSL 装配期交叉校验说明

### 13.1 2026-05-11 errata（ADR-022 + 命名 + Repository 形态）

本批 errata 落地 [ADR-022 entity-domain-mapper-selection](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 锁定的术语 + Repository 形态 + 命名规则：

- **术语 Domain → Model 全文翻面**：全 H3 / `database-design.md` / `file-structure.md` 中 `XxxDomain` / `<Entity>Domain` / `TDomain` / `BaseDomain` 一律改为 `Xxx` / `<TypeName>` / `TModel` / `Base Model`。Mapping 扩展方法 `ToDomain()` 改为 `ToModel()`；selector `SelectAsDomain()` 改为 `SelectAsModel()`。`Inkwell.Abstractions/Persistence/<Module>/` 子目录下的 Model 文件命名从 `XxxDomain.cs` 改为 `<TypeName>.cs`（默认与模块同根字，如 `Agent.cs`）。
- **F6 Mapping 扩展模式锁定**（[ADR-022 §决策](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md)）：所有 Entity ↔ Model 转换走手写 `XxxMappingExtensions` 静态类的扩展方法三件套（`ToModel()` / `ToEntity()` / `SelectAsModel()`）；禁止引入 AutoMapper / Mapster / Riok.Mapperly 等运行时或 SourceGen 框架。具名 Mapping 扩展物理位置：`providers/Persistence/Inkwell.Persistence.EFCore/Mapping/`（HD-009 起草）。
- **§4.1.2 Model 命名规则**：业务 Model 默认无后缀（`Agent` / `Conversation` / `KnowledgeBase` / 等）；与 MAF `Microsoft.Agents.AI.AIAgent` 等外部类型撞名时降级为 `<TypeName>Definition`。当前已知降级：`AgentDefinition` / `ToolDefinition` / `SkillDefinition` / `TriggerDefinition`（语义上是"配置元数据 / 反射加载 / 代码生成"类型，与运行时实例区分）。**禁止** `XxxModel` 后缀（与 ML / MVVM 业界惯例冲突）；冷僻语义 `XxxRecord` / `XxxSnapshot` / `XxxOptions` 仅在显式语义匹配时允许。
- **§3.2 IRepository<TModel, TKey> 形态 = 纯 marker**：`IRepository<TModel, TKey>` 退化为零成员的标记接口，仅锁定 `TModel : class` / `TKey : notnull` 类型参数 + 配合 `BannedSymbols.txt` 禁用 `IXxxStore` / `IXxxDao` / `IXxxGateway` 等后缀的正向锚点。**取消**泛型 CRUD 方法（原 `AddAsync` / `UpdateAsync` / `GetAsync` / `DeleteAsync` / `ListAsync`）；具名 `IXxxRepository` 接口在自己内独立声明 6 个具名动词方法（详 §4.1.3）。本变更动机：业务方法签名差异大（Agent 按 owner 找、Conversation 按 user 找、Trace 按 traceId 找），泛型 CRUD 在实战中几乎全部需要 overload，反而成噪音。
- **§4.1.3 Repository 动词白名单**：所有具名 `IXxxRepository` 方法名必须以 `Add` / `Update` / `Get` / `Delete` / `List` / `Find` 之一开头，禁用 `Query` / `Fetch` / `Retrieve` / `Save` / `Persist` 等同义词；CI 通过 Roslyn analyzer 或代码评审强制（实现细节由 HD-013 起草）。**异步后缀例外**：具名 Repository 方法**不**带 `Async` 后缀（因 6 个动词本身已足够清晰、且全 Repository 方法都是异步、`Async` 重复信息）；其他抽象路径（`IPersistenceProvider` / `IUnitOfWork` / 业务 service）仍遵循 `Async` 后缀规约。
- **§4.4 OTel 字段对齐**：`db.entity_type` 来源从 `TDomain.GetType().Name` 改为 `TModel.GetType().Name`；具名 Repository span 名从 `db.repository.<name>.<op>` 细化为 `db.repository.<entity>.<verb>`（如 `db.repository.agent.add` / `db.repository.agent.find_by_owner`），便于 Grafana panel 按 entity / verb 维度切分。

### 13.2 2026-05-11 errata·第二轮（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) 端口签名裸 `Task<T>` + 异常）

[ADR-023 主决策](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) accepted by Inkwell 2026-05-11：端口层签名从 `Task<Result<T>>` 翻为裸 `Task<T>` + .NET BCL 异常。本 HD 同批翻新：

- **§3.1 IPersistenceProvider 接口签名**：`Task<Result<T>> ExecuteInTransactionAsync<T>(Func<IUnitOfWork, CancellationToken, Task<Result<T>>> ...)` → `Task<T> ExecuteInTransactionAsync<T>(Func<IUnitOfWork, CancellationToken, Task<T>> ...)`；双重载同步翻；SaveChangesAsync `Task<Result<int>>` → `Task<int>`
- **§3.2 IRepository 示例与输出**：`Task<Result<T>>` × 6 方法全部翻为裸 `Task<T>` / `Task<bool>` / `Task<PagedResult<T>>` / `Task<IReadOnlyList<T>>`；取消 update / delete 返回 `Result`——update 返回 `Task`，delete 返回 `Task<bool>`（幂等）
- **§3.3 IUnitOfWork 接口签名**：SaveChangesAsync `Task<Result<int>>` → `Task<int>`
- **§4.2 Method 签名约束**：全书表述由 `Task<Result<T>> XxxAsync` 重写为裸 `Task<T>` + `CancellationToken`；List 返回 `Task<PagedResult<TModel>>` 不再是 `Result<PagedResult<TModel>>`
- **§4.1.3 示例代码块**：IAgentRepository 参考示例 6 方法签名同步翻

### 13.3 2026-05-11 errata·第三轮（[ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) 废错误码机制）

[ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) 废 `ErrorCodes` 机制：

- **§3.10 `ErrorCodes.Persist.cs` 整段废止**：不起草、不创建；INK-PERSIST-001 ~ INK-PERSIST-013 全部作废；§3 文件数从 9 减为 8；§3.1 ~ §3.9 编号不调整以保追溯不断（§3.10 锁位保留不重用）
- **§2 文件树**：删 `ErrorCodes.Persist.cs` 行；§1.2 文件表删“错误码”行
- **§1.1 职责**：删 “`ErrorCodes.Persist`：错误码段（INK-PERSIST-001 ~ INK-PERSIST-099）” 行
- **§3.1 / §3.2 / §3.3 / §3.7 / §3.8 / §3.9 错误处理 / 日志要求**：`Result.Failure(ErrorCodes.Persist.<Name>)` / `InkwellException(ErrorCodes.Persist.<Name>)` / `InkwellException(ErrorCodes.Core.MissingRequiredProvider)` / `error.code=INK-PERSIST-003` 等表述全部翻为 BCL 异常类型 + OTel `exception.*` 五字段（§4.3 集中 BCL 对照表）
- **§4.3 错误处理**：重写为 BCL 对照表（KeyNotFoundException / InvalidOperationException / TimeoutException / IOException / ArgumentException / OperationCanceledException + InkwellConfigurationException / InkwellBuilderException 两子类）
- **§4.4 OTel 字段**：`error.code`（失败时）表述翻为 OTel 标准五字段（`exception.type` / `.message` / `.stacktrace` / `.escaped` / `.id`）
- **§1.3 Q6 关键决策**：并发冲突表述从 `Result.Failure(ErrorCodes.Persist.ConcurrencyConflict)` 翻为 `InvalidOperationException`（message 前缀 `"Optimistic concurrency conflict"`，inner = `DbUpdateConcurrencyException`）
- **§7 性能 / 安全**：删 INK-PERSIST-013 名额占用；InkwellConfigurationException 仅 message 不带 code 参数；Grafana 告警门限从 `error.code=INK-PERSIST-*` 翻为 `exception.type` + `exception.message` 匹配模式
- **§9 部署配置**：Builder DSL 装配期交叉校验从 `InkwellBuilderException(ErrorCodes.Core.ProviderRegistrationConflict)` 翻为 `new InkwellBuilderException("Provider registration conflict for Persistence: configured=<x>, called=<y>")`（保留子类，message 含具体不一致点）
- **§8 测试要求**：越服务契约测试模式：`result.IsSuccess.Should().BeTrue()` → `await act.Should().NotThrowAsync()`；`result.Error.Code.Should().Be(...)` → `(await act.Should().ThrowAsync<KeyNotFoundException>()).Which.Message.Should().StartWith("AgentDefinition not found:")` 等模式

### 13.4 2026-05-11 errata·第四轮（[ADR-023 errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 删 `Result<T>` / `Error` 抽象）

[ADR-023 errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 删 `Common/Result.cs` + `Common/Error.cs` 抽象：

- **§3.1 IPersistenceProvider 依赖模块**：删 `Common/Result.cs` / `Common/Error.cs` 依赖列表
- **§3.2 IRepository 依赖模块**：删 `Common/Result.cs` 依赖列表
- **§3.3 IUnitOfWork 依赖模块**：删 `Common/Result.cs` 依赖列表
- **§11 业务命名空间错误语义条**：从 “业务可选使用 `Result<T>` / `Error`” 翻为 “错误处理全走 BCL 异常类型表达 + OTel `exception.*` 五字段”；多项错误场景推荐 `ValidationResult` / `IEnumerable<string>`
- **本轮补齐 §0 callout 指向上游 errata·01 / errata·02 锥点**

**上游证据链**：

- [HD-001 §0 callout + §5.3 BCL 对照表 + §13 第三 / 第四轮 errata](HD-001-Inkwell.Abstractions-foundation.md#13-errata-记录)
- [ADR-023 主决策 + errata·01 + errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)
- [design-review-report §8.8 第三轮 + §8.9 第四轮纪要](../design-review-report.md#88-第三轮规约翻转纪录-2026-05-11-同会话叠加)

**下游待办**：

- [HD-003 §1.3 picker Q1/Q2 标 superseded + §1.4 偏离表大幅缩减 + frontmatter 加第二+三+四轮 errata callout](HD-003-Inkwell.Abstractions-file-storage-port.md)——下一会话切片
- [ADR-015 二次 errata 块追加](../../03-architecture/adr/ADR-015-object-storage-provider-switchable.md)——需 h2-architect-advisor agent 或 Owner 手动
- [HD-004 ~ HD-008 起草](.)——直接用新规约落地
- 业务命名空间各 HD 起草具名 Repository 时同步遵 §4.3 BCL 对照表

### 13.5 2026-05-18 errata·第五轮（Q1 = A2 picker 落地：`GetRepository<TRepository>()` 泛型工厂入口）

**决策上下文**：[design-review-report.md §13.2](../design-review-report.md) Owner 在 chat 中 picker **A2（has-a 泛型工厂）**。根因是 [§13.1 reviewer 复盘](../design-review-report.md)：本 HD [§1.1 职责](#11-职责) 早已声明 `IPersistenceProvider` 提供「事务包装 + SaveChanges + **Repository 工厂查询能力**」，但 §3.1 接口字面漏写 `GetRepository<TRepository>()`，第五轮验收 §12.2 一致性扫描未 catch。本轮 errata 是**修复 §1.1 ↔ §3.1 内部不一致**，非 picker 回翻，**不需要回 H2 走 ADR**。

**本 HD 修改清单**：

- **§1.3 Q1 决议描述翻新**：从「`IPersistenceProvider` = facade only；具名 Repository 推迟到业务 HD」翻为「facade + `GetRepository<TRepository>()` 泛型工厂入口；业务经 `provider.GetRepository<IXxxRepository>()`（事务作用域外）/ `uow.GetRepository<...>()`（事务作用域内）双入口取」；来源行加 A2 errata·第五轮 + [ADR-020 工厂同款](../../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md)
- **§3.1 `IPersistenceProvider.cs` 接口形态**：「对外接口」字段在 `ExecuteInTransactionAsync` 双重载后、`SaveChangesAsync` 前追加 `TRepository GetRepository<TRepository>() where TRepository : class;`（签名与 [§3.3 `IUnitOfWork.GetRepository<T>`](#33-persistenceiunitofworkcs) 完全对齐）；「职责」字段补 Repository 工厂查询入口语义 + 事务作用域内外双入口说明；「错误处理」字段补 `GetRepository<T>` 类型未注册 → [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)（message 前缀 `"Required repository type not registered:"`，与 §3.3 一致）

**设计语义对齐**：[Microsoft.Extensions.VectorData.VectorStore.GetCollection](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-vector-data)（[ADR-020](../../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md)）同款——facade 持工厂入口、具名窄接口由工厂返回；业务调用侧只 inject `IPersistenceProvider` 一个对象、Provider 实现侧只实现 `IPersistenceProvider` 一个接口（具名 `IXxxRepository` 实现被工厂内部托管，委托 [`IServiceProvider.GetRequiredService<TRepository>()`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.serviceproviderserviceextensions.getrequiredservice)）。

**下游联动**：

- [HD-009 §3.2 `EfCorePersistenceProvider`](../Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) 同步追加 `GetRepository<TRepository>()` 实现（委托 `GetRequiredService`）+ §13.5 errata
- [AGENTS.md §3.1 / §3.2](../../../AGENTS.md) 拓扑 + 依赖规则同步（**由 Owner / 默认 Agent 落，author 模式不写 AGENTS.md**）
- 业务命名空间各 HD（尚未起草）起草时按 A2 形态写，不需要回炉
