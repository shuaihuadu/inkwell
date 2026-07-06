---
id: HD-009
title: Inkwell.Persistence.EFCore 详细设计 - shared base（EFCore family 共享层）
stage: H3
status: reviewed
reviewers: [Inkwell]
upstream:
  - REQ-002
  - REQ-006
  - REQ-009
  - REQ-013
  - REQ-014
  - NFR-005
  - ADR-002
  - ADR-004
  - ADR-013
  - ADR-017
  - ADR-021
  - ADR-022
  - HD-001
  - HD-002
downstream: []
---

> **范围切片**：本 HD 锁定 `providers/Inkwell.Persistence.EFCore/` shared base csproj 内容——`InkwellDbContext` + `OnModelCreating`（按 mixin 自动配置）、唯一 `IPersistenceProvider` 实现 `EfCorePersistenceProvider`、`AuditingSaveChangesInterceptor`（联动三 mixin）、`InkwellSeeder`（幂等）、`MigrationRunner`、`IDbContextInitializer` 抽象、Entity / Configuration / Mapping / Repository 四子目录模板（[ADR-022](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 锁手写 Extensions 模式）、Builder DSL 共享部分、`BannedSymbols.txt`。
>
> **不**覆盖：三 final adapter csproj（`Inkwell.Persistence.EFCore.{InMemory,SqlServer,Postgres}`）→ [HD-010 / HD-011 / HD-012](./) 各自起草；具名业务 Model + `IXxxRepository` 接口 → 各业务命名空间 HD（`Inkwell.Core.Agents` 等）起草；跨 Provider 契约测试包 → HD-013 起草。
>
> **拓扑张力**：[ADR-021 §依赖规则补充](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 锁定 EFCore family 例外——final adapter csproj 允许 ProjectReference shared base；本 HD 锁定 base 的物理边界与 ProjectReference 上游/下游。
>
> **2026-05-11 errata（首版）**：本 HD 与 [HD-002 2026-05-11 errata](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 同批次起草，全文术语锁定 = Entity ↔ Model；具名 Repository 动词白名单 = Add / Update / Get / Delete / List / Find（不带 `Async` 后缀）；mapper 走手写 `XxxMappingExtensions` 三方法（`ToModel` / `ToEntity` / `SelectAsModel`）。
>
> **2026-05-12 errata·第二轮（[ADR-023 主决策](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)）**：[ADR-023 主决策](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) accepted by Inkwell 2026-05-11；本 HD §3.2 / §3.4 / §3.5 / §3.10 接口签名翻 `Task<Result<T>>` / `Task<Result>` → 裸 `Task<T>` / `Task<int>` / `Task` / `Task<bool>` + `CancellationToken`；§4.2 Repository 共性约束同步翻；详 §13.2。
>
> **2026-05-12 errata·第三轮（[ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流)）**：废 `INK-PERSIST-NNN` 错误码机制；本 HD §3.1 / §3.2 / §3.3 / §3.4 / §3.5 / §3.10 / §4.2 / §4.3 / §7 错误处理统一走 [.NET BCL 异常类型](https://learn.microsoft.com/dotnet/standard/exceptions/) + OTel [`exception.*` 五字段](https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/)；§4.3 重写为 BCL 对照表；§7 `EnableSensitiveDataLogging` prod fail-fast 改抛 `InkwellConfigurationException`；详 §13.3。
>
> **2026-05-12 errata·第四轮（[ADR-023 errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常)）**：删 `Common/Result.cs` + `Common/Error.cs` 抽象；本 HD §3.2 / §3.10 依赖模块删 `Inkwell.Abstractions/Common/Result.cs`；§3.10 AgentRepository 完整代码 6 方法签名 + catch 块同步翻；§10 自动化检查命令补 C9 ~ C12 grep；§11 决策记录补 4 行；详 §13.4。
>
> **2026-07-06 errata·第六轮（HD-010 首轮评审 B17/C97）**：§3.11 此前只有职责描述与方法签名，无"完整代码"块，无法确认 `AuditingSaveChangesInterceptor` 的 DI 服务类型注册方式；本轮补齐该方法完整代码，明确按 `ISaveChangesInterceptor` 接口服务类型注册（与消费端 HD-010/HD-011/HD-012 `AddInterceptors(sp.GetServices<ISaveChangesInterceptor>())` 一致），同时一并注册 `EfCorePersistenceProvider` / `InkwellSeeder` / `MigrationRunner` / 具名 Repository；详 §13.6。
>
> **2026-07-06 errata·第七轮（HD-011 起草期发现，Owner picker 授权同步修改）**：`EfCorePersistenceProvider.ExecuteInTransactionAsync`（§3.2）此前手动调 `IDbContextTransaction`（`BeginTransactionAsync` / `CommitAsync` / `RollbackAsync`），与 [EF Core 连接重试策略约束](https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency#execution-strategies-and-transactions) 不兼容——一旦 HD-011 SqlServer 适配器启用 `EnableRetryOnFailure`，任何手动 `BeginTransactionAsync` 调用会在运行时 100% 抛 `InvalidOperationException`（"The configured execution strategy ... does not support user-initiated transactions"）。本轮修正：`ExecuteInTransactionAsync` 内部改用 [`db.Database.CreateExecutionStrategy().ExecuteAsync(...)`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.infrastructure.dbcontextoptionsbuilder) 包装 Begin/Commit/Rollback 全过程；InMemory（HD-010，无 retry 策略）下 `CreateExecutionStrategy()` 返回默认 no-op 策略，包装不改变现有行为，兼容既有测试。详 §13.7。
>
> **2026-07-06 errata·第八轮（ADR-021 / ADR-019 2026-07-06 errata：Migration 执行策略改为 CI/CD 独立步骤）**：[ADR-021 §「Migration/DataSeed 启动行为」2026-07-06 errata](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 修订 Migration 执行时机——`Inkwell.WebApi` / `Inkwell.Worker` 启动代码不再调用本 HD §3.5 `MigrationRunner` 对 SqlServer / Postgres 执行 `MigrateAsync()`；Migration 改由 CI/CD pipeline 独立步骤（`dotnet ef database update` 或等价工具）在部署前执行。**InMemory 场景不受影响**：`EnsureCreatedAsync()` + `InkwellSeeder.SeedAsync()` 仍在两进程启动期自动运行（ADR-021 errata 原文明确 InMemory 属 dev / unit test 场景，不涉及生产发布风险）。§3.5 同步修订，详 §13.8。

## 1. 模块职责

- **Entity 集中地**（[ADR-021 D1 = A](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）：全部业务 `XxxEntity` 类、`IEntityTypeConfiguration<TEntity>` 配置 ~30 套
- **DbContext 共享 base**：`InkwellDbContext`（virtual `OnModelCreating` / `OnConfiguring`），final adapter 通过继承调整 Provider-specific 行为
- **唯一 `IPersistenceProvider` 实现**：`EfCorePersistenceProvider` 实现 [HD-002 §3.1 facade](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)
- **三 mixin 自动配置**：通过 `SaveChangesInterceptor` 与 `OnModelCreating` 反射扫描，对实现 `IHasTimestamps` / `IHasRowVersion` / `IHasOwner` 的 Entity 自动填充 / 注入 token / 加索引
- **手写 mapper 集中地**（[ADR-022 §位置](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md)）：`Mapping/<TypeName>MappingExtensions.cs` ~30 套，每个 `internal static class` 含 `ToModel` / `ToEntity` / `SelectAsModel` 三方法
- **具名 Repository 实现集中地**：`Repositories/<TypeName>Repository.cs` ~30 套，唯一实现 `Inkwell.Abstractions/Persistence/<Module>/IXxxRepository.cs` 接口
- **Seed + Migration runner**：`InkwellSeeder`（幂等 `if-not-exists` 模式） + `MigrationRunner`（[ADR-021 D2 = B](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) `AutoSeedOnStartup` 开关）
- **CI 强制 banlist**：`BannedSymbols.txt` 锁定 Repository 动词白名单 + Repository 后缀禁用 + Mapping 库零依赖

## 2. 文件清单

| #   | 文件                                                                         | 类别   | 详见  |
| --- | ---------------------------------------------------------------------------- | ------ | ----- |
| 1   | `Inkwell.Persistence.EFCore.csproj`                                          | csproj | §3.0  |
| 2   | `InkwellDbContext.cs`                                                        | base   | §3.1  |
| 3   | `EfCorePersistenceProvider.cs`                                               | base   | §3.2  |
| 4   | `Interceptors/AuditingSaveChangesInterceptor.cs`                             | base   | §3.3  |
| 5   | `InkwellSeeder.cs`                                                           | base   | §3.4  |
| 6   | `MigrationRunner.cs`                                                         | base   | §3.5  |
| 7   | `IDbContextInitializer.cs`                                                   | base   | §3.6  |
| 8   | `Entities/<Entity>Entity.cs` × ~30                                           | 模板   | §3.7  |
| 9   | `Configurations/<Entity>EntityConfiguration.cs` × ~30                        | 模板   | §3.8  |
| 10  | `Mapping/<TypeName>MappingExtensions.cs` × ~30                               | 模板   | §3.9  |
| 11  | `Repositories/<TypeName>Repository.cs` × ~30                                 | 模板   | §3.10 |
| 12  | `DependencyInjection/InkwellPersistenceEfCoreServiceCollectionExtensions.cs` | base   | §3.11 |
| 13  | `BannedSymbols.txt`                                                          | CI     | §3.12 |

物理布局参 [file-structure.md §providers/Inkwell.Persistence.EFCore](../file-structure.md)。

## 3. 各文件 10 字段

### 3.0 Inkwell.Persistence.EFCore.csproj

- **文件路径**：`src/core/providers/Inkwell.Persistence.EFCore/Inkwell.Persistence.EFCore.csproj`
- **职责**：声明 EFCore family shared base 的依赖与目标框架
- **对外接口**：无（csproj 配置）
- **内部函数或类**：无
- **输入数据**：MSBuild 属性
- **输出数据**：编译产物 `Inkwell.Persistence.EFCore.dll`
- **依赖模块**：
  - `Microsoft.EntityFrameworkCore` 10.x
  - `Microsoft.EntityFrameworkCore.Relational` 10.x（用于 `IsRelational()` 判定 + Migration 抽象）
  - ProjectReference `Inkwell.Abstractions`
  - **禁止**：任何 DBMS 特定 Provider 包（`Microsoft.EntityFrameworkCore.SqlServer` / `Npgsql.EntityFrameworkCore.PostgreSQL` / `Microsoft.EntityFrameworkCore.InMemory` —— 留给三 final adapter csproj）
  - **禁止**：任何映射库（AutoMapper / Mapster / Riok.Mapperly —— [ADR-022](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 锁手写 Extensions）
- **错误处理**：N/A（csproj 配置错误由 dotnet build 报）
- **日志要求**：N/A
- **测试要求**：CI 在 `dotnet pack` 后断言 `.nupkg` 仅含 EFCore 抽象层 + Inkwell.Abstractions ProjectReference，不含任何 DBMS Provider 包（dotnet-list-package + grep）

### 3.1 InkwellDbContext.cs

- **文件路径**：`src/core/providers/Inkwell.Persistence.EFCore/InkwellDbContext.cs`
- **职责**：base `DbContext`，登记全部 `DbSet<XxxEntity>`，`OnModelCreating` 扫描三 mixin + 应用全部 `IEntityTypeConfiguration<TEntity>`
- **对外接口**：
  - `public class InkwellDbContext(DbContextOptions<InkwellDbContext> options) : DbContext(options)`（[primary constructor](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-12#primary-constructors)，C# 14 兼容）
  - `public DbSet<AgentEntity> Agents { get; }` 等 ~30 个 `DbSet<TEntity>` 属性（final adapter 不重复声明）
  - `protected override void OnModelCreating(ModelBuilder modelBuilder)`（virtual，final adapter 可叠加）
- **内部函数或类**：
  - `private static void ApplyTimestamps(ModelBuilder mb)` —— 扫描所有 entity type，若实现 `IHasTimestamps` 则 `IsRequired()` + `HasColumnType("datetimeoffset" / "timestamptz")`（由 final adapter Provider-specific 选）
  - `private static void ApplyRowVersion(ModelBuilder mb)` —— 实现 `IHasRowVersion` 的 entity type 调 `IsRowVersion()`
  - `private static void ApplyOwnerIndex(ModelBuilder mb)` —— 实现 `IHasOwner` 的 entity type `HasIndex(e => e.OwnerUserId)`
  - `private static void ApplyEnumAsString(ModelBuilder mb)` —— [HD-002 §5.1.1 F4](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)，全部 enum 列 `HasConversion<string>().HasMaxLength(64)`
- **输入数据**：`DbContextOptions<InkwellDbContext>`（由 final adapter 通过 DI 注入，含 Provider 选择 + 连接字符串）
- **输出数据**：物化的 `IQueryable<TEntity>` / Track 集合
- **依赖模块**：`Microsoft.EntityFrameworkCore` / `Inkwell.Abstractions/Persistence/Mixins/*`
- **错误处理**：`OnModelCreating` 内部异常 → 启动期失败（fast-fail）；映射不到 mixin 字段的反射异常包成 `new InvalidOperationException("Failed to apply mixin to entity types: <inner-message>", inner)`（[HD-002 §4.3](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) BCL 对照表 + [ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流)）
- **日志要求**：`OnModelCreating` 完成后 `LogInformation("EFCore model created: {EntityCount} entities, {TimestampedCount} with IHasTimestamps, {RowVersionedCount} with IHasRowVersion")`
- **测试要求**：
  - 单测：`InMemory` Provider 下断言 `Model.GetEntityTypes()` 数 = ~30、`IsRowVersion()` 字段数 = mixin 实现数
  - 覆盖率门槛 ≥ 95%

### 3.2 EfCorePersistenceProvider.cs

- **文件路径**：`src/core/providers/Inkwell.Persistence.EFCore/EfCorePersistenceProvider.cs`
- **职责**：唯一 `IPersistenceProvider` 实现，对外暴露事务包装 + `SaveChangesAsync` + `GetRepository<TRepository>()` 具名 Repository 工厂入口（2026-05-18 errata·第五轮：[HD-002 Q1=A2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#135-2026-05-18-errata第五轮q1--a2-picker-落地getrepositorytrepository-泛型工厂入口)）
- **对外接口**：
  - `internal sealed class EfCorePersistenceProvider(InkwellDbContext db, IServiceProvider services, ILogger<EfCorePersistenceProvider> logger) : IPersistenceProvider`
  - `public Task<T> ExecuteInTransactionAsync<T>(Func<IUnitOfWork, CancellationToken, Task<T>> work, CancellationToken ct = default)`
  - `public Task ExecuteInTransactionAsync(Func<IUnitOfWork, CancellationToken, Task> work, CancellationToken ct = default)`
  - `public TRepository GetRepository<TRepository>() where TRepository : class => services.GetRequiredService<TRepository>();`（签名与 [HD-002 §3.1](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) / §3.3 `IUnitOfWork.GetRepository<T>` 完全对齐；具名 `IXxxRepository` 实现由 `AddEfCorePersistenceBase()` 集中注册（§3.x 末），工厂仅做 `IServiceProvider` 解析转发）
  - `public Task<int> SaveChangesAsync(CancellationToken ct = default)`
- **内部函数或类**：
  - `GetRepository<TRepository>()` 委托 [`IServiceProvider.GetRequiredService<TRepository>()`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.serviceproviderserviceextensions.getrequiredservice)；未注册时 `GetRequiredService` 自身抛 `InvalidOperationException`（message 含未注册类型名），实现无需额外 catch（与 [HD-002 §3.1 错误处理](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 约定一致）
  - `private sealed class EfCoreUnitOfWork(IDbContextTransaction tx) : IUnitOfWork` —— 仅在 `ExecuteInTransactionAsync` lambda 内有效
  - `EfCoreUnitOfWork` 持有 `IDbContextTransaction`，`Dispose` 后 `IsDisposed = true`；外部业务在 lambda 外调用任何成员 → 抛 `new InvalidOperationException("UnitOfWork accessed outside ExecuteInTransactionAsync scope")`（[HD-002 §4.3](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)）
  - **（2026-07-06 errata·第七轮）** `ExecuteInTransactionAsync` 的 Begin/Commit/Rollback 三步全部包在 `db.Database.CreateExecutionStrategy().ExecuteAsync(async () => { ... })` 回调内——SqlServer 场景下（[HD-011](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md) `EnableRetryOnFailure`）该回调可能因瞬时故障重新整体执行；业务 `work` 委托必须保持对再次调用安全（幂等于数据库写操作层面，不产生数据库外部副作用），这是本 HD 对 `IPersistenceProvider.ExecuteInTransactionAsync` 调用方的既有隐含约定（纯 Repository 操作天然满足，业务 HD 起草时若在 `work` 内混入外部 I/O 需显式说明）
- **输入数据**：业务 lambda + `CancellationToken`
- **输出数据**：业务 lambda 的返回值（`T`）/ 受影响行数（`int`）；失败统一走 BCL 异常
- **依赖模块**：`Microsoft.EntityFrameworkCore` / `Microsoft.EntityFrameworkCore.Storage` / [`Microsoft.Extensions.DependencyInjection.Abstractions`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.serviceproviderserviceextensions.getrequiredservice)（`GetRepository<T>` 委托用；该包已由 `AddEfCorePersistenceBase()` 注册扩展引入，非新增依赖，[ADR-017 零外部包约束](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) 范围内） / `Inkwell.Abstractions/Persistence/*`（[ADR-023 errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 删 `Common/Result.cs` 依赖）
- **错误处理**（[HD-002 §4.3](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) BCL 对照表）：
  - `DbUpdateConcurrencyException`（lambda 内 `SaveChangesAsync` 抛）→ rollback + 包成 `new InvalidOperationException("Optimistic concurrency conflict: <entity-type>", inner)`
  - `DbUpdateException` with unique-violation inner → rollback + 包成 `new InvalidOperationException("Duplicate key: <inner-message>", inner)`
  - `OperationCanceledException` → rollback + 透传（不包装，遵 BCL 惯例）
  - `TimeoutException` / `Microsoft.Data.SqlClient.SqlException` ErrorNumber=-2 → rollback + 包成 `new TimeoutException($"Command timeout: {options.CommandTimeoutSeconds}s", inner)`
  - 业务在 lambda 内主动 throw 的 BCL 语义异常（`KeyNotFoundException` / `ArgumentException` / `InvalidOperationException`）→ rollback + 直接重抛（透明）
  - 其他未识别异常 → rollback + 包成 `new InvalidOperationException("Transaction rolled back: <inner-message>", inner)`，记 `LogError` + `Activity.AddException(inner)`
  - `GetRepository<TRepository>()` 类型未注册 → `GetRequiredService` 抛 `InvalidOperationException`（message 含未注册类型名，与 [HD-002 §3.1](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 约定的 `"Required repository type not registered:"` 语义一致）；非事务路径，不过 rollback 包装
- **日志要求**：
  - 开启事务：`LogDebug("Transaction begin {ScopeId}")`
  - 提交：`LogInformation("Transaction committed {ScopeId} elapsed={ElapsedMs}ms")`
  - 回滚：`LogWarning("Transaction rolled back {ScopeId} exceptionType={ExceptionType}")`
  - OTel span：`db.transaction` 带 [HD-002 §4.4](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 7 字段（含 `exception.type` / `.message` / `.stacktrace` / `.escaped` / `.id` 五字段，由 catch 块统一调 `Activity.SetStatus(ActivityStatusCode.Error)` + `Activity.AddException(inner)` 写入）
- **测试要求**：
  - 正常 commit、显式回滚（lambda throw）、异常回滚、`UnitOfWork` 跨边界使用抛 `InvalidOperationException`、并发冲突包成 `InvalidOperationException("Optimistic concurrency conflict")` 五用例
  - 业务 lambda 主动 throw `KeyNotFoundException` 时透传不包装（断言重抛同实例）
  - `OperationCanceledException` 透传断言（外部 cts.Cancel 触发）
  - 覆盖率门槛 ≥ 95%

### 3.3 Interceptors/AuditingSaveChangesInterceptor.cs

- **文件路径**：`src/core/providers/Inkwell.Persistence.EFCore/Interceptors/AuditingSaveChangesInterceptor.cs`
- **职责**：在 `SaveChangesAsync` 前对实现三 mixin 的 Entity 自动填充 `CreatedTime` / `UpdatedTime`、校验 `IHasOwner.OwnerUserId != Guid.Empty`
- **对外接口**：
  - `internal sealed class AuditingSaveChangesInterceptor(TimeProvider clock) : SaveChangesInterceptor`（继承 [`Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.diagnostics.savechangesinterceptor)）
  - `public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)`
- **内部函数或类**：
  - `private void ApplyTimestamps(EntityEntry entry, DateTimeOffset now)`
    - `Added`：`CreatedTime = now`、`UpdatedTime = now`（仅当字段未被业务显式设置 / 默认值时）
    - `Modified`：`UpdatedTime = now`、强制 `CreatedTime` 保持原值（防业务越权改写）
  - `private void ValidateOwner(EntityEntry entry)`
    - 若 `IHasOwner` 且 `OwnerUserId == Guid.Empty` → 抛 `new ArgumentException("OwnerUserId cannot be Guid.Empty", nameof(IHasOwner.OwnerUserId))`（[HD-002 §4.3](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) BCL 对照表 row 6）
- **输入数据**：`DbContext.ChangeTracker.Entries()`
- **输出数据**：副作用（写时间戳 / 校验 owner）
- **依赖模块**：`Microsoft.EntityFrameworkCore.Diagnostics` / `Inkwell.Abstractions/Persistence/Mixins/*` / `System.TimeProvider`（[.NET 8+ TimeProvider](https://learn.microsoft.com/dotnet/standard/datetime/timeprovider-overview)）
- **错误处理**：见 `ValidateOwner` 抛 `ArgumentException`；非 mixin Entity 跳过（不抛）；`SaveChangesInterceptor` 抛出的异常会被 EFCore 包成 `DbUpdateException` 后由 `EfCorePersistenceProvider`（§3.2）catch 翻为对应 BCL 异常
- **日志要求**：高频写日志 = 噪音，仅在 `IHasOwner` 校验失败时 `LogError("Audit failed: OwnerUserId is empty for {EntityType} Id={EntityId}")` + `Activity.AddException(ex)` 写 OTel `exception.*` 五字段（[HD-002 §4.4](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)）
- **测试要求**：
  - Added 时填两字段；Modified 时仅填 UpdatedTime 且保留原 CreatedTime；`IHasOwner` 缺 owner 抛 `ArgumentException`（`paramName == "OwnerUserId"`）；非 mixin Entity 不动；mock `TimeProvider` 验证时钟来源
  - 覆盖率门槛 ≥ 95%

### 3.4 InkwellSeeder.cs

- **文件路径**：`src/core/providers/Inkwell.Persistence.EFCore/InkwellSeeder.cs`
- **职责**：幂等 seed 入口；启动期由 `MigrationRunner` 调
- **对外接口**：
  - `internal sealed class InkwellSeeder(InkwellDbContext db, ILogger<InkwellSeeder> logger)`
  - `public async Task SeedAsync(CancellationToken ct = default)`
- **内部函数或类**：
  - 每个 seed 段独立私有方法：`SeedSystemUserAsync` / `SeedDefaultRolesAsync` / etc.（v1 范围内具体 seed 内容由业务 HD 增量贡献）
  - **幂等模式**：每个 seed 段先查业务唯一键（如 `users.Email`） → 不存在则 `Add`；**禁**用 `Id` 主键判定（[ADR-021 §Migration/DataSeed 启动行为](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）
- **输入数据**：DI 注入的 `InkwellDbContext`
- **输出数据**：成功无返回值；失败抛 BCL 异常（`InvalidOperationException` 包 inner）
- **依赖模块**：`Microsoft.EntityFrameworkCore` / Entities / Mapping（[ADR-023 errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 不依赖 `Common/Result.cs`）
- **错误处理**：seed 段任一 throw → 包成 `new InvalidOperationException($"Seeder segment '{segmentName}' failed", inner)`（[HD-002 §4.3](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) BCL 对照表事务回滚 row），记 `LogError` 含失败段名 + `Activity.AddException(inner)` 写 OTel 五字段；不重试（启动期失败由 K8s probe 触发 pod 重启）
- **日志要求**：
  - 开始：`LogInformation("Seed begin")`
  - 每段完成：`LogInformation("Seed {SegmentName} ok inserted={NewRowCount}")`
  - 全部完成：`LogInformation("Seed done totalSegments={N} totalInserted={M} elapsed={Ms}ms")`
- **测试要求**：
  - 首次跑插数、二次跑零插数（幂等断言）、任一段 throw 包成 006、`AutoSeedOnStartup(false)` 时 Seeder 不被注入
  - 覆盖率门槛 ≥ 95%

### 3.5 MigrationRunner.cs

- **文件路径**：`src/core/providers/Inkwell.Persistence.EFCore/MigrationRunner.cs`
- **职责**（2026-07-06 errata·第八轮修订，详 §13.8）：
  - **InMemory 场景**（dev / unit test，不受本轮修订影响）：`Inkwell.WebApi` / `Inkwell.Worker` 启动时仍自动调用本类，包装 `EnsureCreatedAsync()`，完成后选择性调 `InkwellSeeder.SeedAsync()`
  - **SqlServer / Postgres 场景**（integration test / prod）：`Inkwell.WebApi` / `Inkwell.Worker` 启动代码**不再调用**本类执行 `MigrateAsync()` 做 schema 变更——Migration 改由 CI/CD pipeline 独立步骤（既可以是引用本类的独立命令行工具，也可以直接用标准 [`dotnet ef database update`](https://learn.microsoft.com/ef/core/managing-schemas/migrations/#apply-migrations-at-runtime)）在新版本部署前执行；`InkwellSeeder.SeedAsync()` 仍可能在应用启动时运行，但前提从「随本类完成 Migration 后触发」改为「确认 CI/CD 已将 schema 迁移到位」
- **对外接口**（不变）：
  - `internal sealed class MigrationRunner(InkwellDbContext db, IDbContextInitializer initializer, IOptions<PersistenceOptions> options, InkwellSeeder seeder, ILogger<MigrationRunner> logger)`
  - `public async Task RunAsync(CancellationToken ct = default)`
- **内部函数或类**：
  - 调 `initializer.InitializeAsync(db, ct)`（final adapter 决定走 Migrate 还是 EnsureCreated，详 §3.6）——该调用路径本身不变；变化在于「谁在什么时机调用 `RunAsync`」：InMemory 场景仍由 `Inkwell.WebApi` / `Inkwell.Worker` 启动代码调用；SqlServer / Postgres 场景改由 CI/CD 独立步骤（或其调用的命令行工具）调用，两进程启动代码不再调用
  - 若 `PersistenceOptions.AutoSeedOnStartup` = true → 调 `seeder.SeedAsync(ct)`
  - 全过程加 `MigrationTimeoutSeconds` 超时（默认 300s，[HD-002 §3.5 PersistenceOptions](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)）
- **输入数据**：`PersistenceOptions`
- **输出数据**：成功无返回值；失败抛 BCL 异常
- **依赖模块**：`Microsoft.EntityFrameworkCore` / `Inkwell.Abstractions/Persistence/PersistenceOptions.cs`
- **错误处理**（[HD-002 §4.3](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) BCL 对照表）：
  - 超时（含 `OperationCanceledException` 由内部 `CancellationTokenSource(MigrationTimeoutSeconds)` 触发）→ 抛 `new TimeoutException($"Migration timeout: {options.MigrationTimeoutSeconds}s")`
  - Migration 异常 → 抛 `new InvalidOperationException("Migration failed", inner)` 含 inner 异常 + `Activity.AddException(inner)` 写 OTel 五字段
  - Seeder 失败 → 透传（已是 `InvalidOperationException` per §3.4）
  - 外部传入 `ct` 取消 → `OperationCanceledException` 透传
- **日志要求**：
  - 开始：`LogInformation("Migration begin provider={ProviderName}")`
  - 完成：`LogInformation("Migration ok appliedCount={Applied} elapsed={Ms}ms")`
- **测试要求**：
  - InMemory 走 EnsureCreated 路径（由 `Inkwell.WebApi` / `Inkwell.Worker` 启动集成测试覆盖）；SqlServer / Postgres 走 Migrate 路径（用 mock `IDbContextInitializer` 做单测，真实 Migrate 行为改由 CI/CD 侧调用方 / 独立命令行工具的测试覆盖，不再属于 WebApi/Worker 启动集成测试范围）；超时抛 007；Seeder 失败透传 006；`AutoSeedOnStartup=false` 跳过 Seeder
  - 覆盖率门槛 ≥ 95%

### 3.6 IDbContextInitializer.cs

- **文件路径**：`src/core/providers/Inkwell.Persistence.EFCore/IDbContextInitializer.cs`
- **职责**：把 final adapter 之间「Migrate vs EnsureCreated」的分歧抽到接口，base 不耦合具体 Provider 行为
- **对外接口**：
  - `public interface IDbContextInitializer { Task InitializeAsync(InkwellDbContext db, CancellationToken ct = default); }`
- **内部函数或类**：无（仅接口）
- **输入数据**：N/A
- **输出数据**：N/A
- **依赖模块**：`Microsoft.EntityFrameworkCore`
- **错误处理**：实现方自由抛；`MigrationRunner` 统一包成 005
- **日志要求**：实现方自行记
- **测试要求**：契约测试仅在 final adapter HD（HD-010/011/012）中验

### 3.7 Entities/&lt;Entity&gt;Entity.cs 模板 + AgentEntity 示例

- **文件路径**：`src/core/providers/Inkwell.Persistence.EFCore/Entities/<Entity>Entity.cs`（~30 个，per-entity 一文件）
- **职责**：EFCore 物化对象；可见性 `internal sealed class`（外部业务命名空间不见）
- **对外接口**：纯 POCO + 可空 nav 属性（关系导航）
- **内部函数或类**：无业务方法；构造器允许空 `public AgentEntity() { }`（EFCore 物化需）
- **输入数据**：N/A（DB 物化对象）
- **输出数据**：N/A
- **依赖模块**：`Inkwell.Abstractions/Persistence/Mixins/*`
- **错误处理**：N/A
- **日志要求**：N/A
- **测试要求**：覆盖在 §3.9 Mapping 测试 + §3.10 Repository 测试中，不单独测 Entity POCO

**模板**（以 Agent 为例；其他 Entity 同形）：

```csharp
// src/core/providers/Inkwell.Persistence.EFCore/Entities/AgentEntity.cs
namespace Inkwell.Persistence.EFCore.Entities;

using Inkwell.Abstractions.Persistence.Mixins;

internal sealed class AgentEntity : IHasTimestamps, IHasRowVersion, IHasOwner
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Configuration { get; set; } = "{}";   // JSON 列；string + JsonSerializer value converter

    // IHasOwner
    public Guid OwnerUserId { get; set; }

    // IHasTimestamps
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }

    // IHasRowVersion
    public byte[] RowVersion { get; set; } = [];
}
```

### 3.8 Configurations/&lt;Entity&gt;EntityConfiguration.cs 模板

- **文件路径**：`src/core/providers/Inkwell.Persistence.EFCore/Configurations/<Entity>EntityConfiguration.cs`（~30 个）
- **职责**：跨字段约束（复合唯一索引 / 外键 / 列类型覆写 / 表名）—— 单字段约束走 DataAnnotations Attribute（[HD-002 §5.1.2 F8](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)）
- **对外接口**：`internal sealed class XxxEntityConfiguration : IEntityTypeConfiguration<XxxEntity>` + `public void Configure(EntityTypeBuilder<XxxEntity> b)`
- **内部函数或类**：无
- **输入数据**：`EntityTypeBuilder<TEntity>`
- **输出数据**：副作用（模型配置）
- **依赖模块**：`Microsoft.EntityFrameworkCore.Metadata.Builders` / Entities
- **错误处理**：N/A（启动期 fail-fast）
- **日志要求**：N/A
- **测试要求**：单测断言 `Model.FindEntityType(typeof(XxxEntity))` 含期望索引 + 表名；覆盖率门槛 ≥ 95%

**模板**：

```csharp
// src/core/providers/Inkwell.Persistence.EFCore/Configurations/AgentEntityConfiguration.cs
namespace Inkwell.Persistence.EFCore.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Inkwell.Persistence.EFCore.Entities;

internal sealed class AgentEntityConfiguration : IEntityTypeConfiguration<AgentEntity>
{
    public void Configure(EntityTypeBuilder<AgentEntity> b)
    {
        b.ToTable("agents");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.OwnerUserId, x.Name }).IsUnique();   // 跨字段唯一约束
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Configuration).HasColumnType("text");      // Provider-specific 类型在 final adapter override
    }
}
```

### 3.9 Mapping/&lt;TypeName&gt;MappingExtensions.cs 模板 + AgentMappingExtensions 完整代码

- **文件路径**：`src/core/providers/Inkwell.Persistence.EFCore/Mapping/<TypeName>MappingExtensions.cs`（~30 个，per-entity 一文件）
- **职责**：Entity ↔ Model 双向手写映射 + IQueryable 投影下推（[ADR-022 §决策](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md)）
- **对外接口**：`internal static class <TypeName>MappingExtensions`，包含三个扩展方法（[Owner 第二轮反馈](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md)）：
  - `public static <TypeName> ToModel(this <Entity>Entity entity)` —— Entity → Model
  - `public static <Entity>Entity ToEntity(this <TypeName> model)` —— Model → Entity
  - `public static IQueryable<<TypeName>> SelectAsModel(this IQueryable<<Entity>Entity> source)` —— IQueryable 投影下推 SQL
- **内部函数或类**：无私有方法（boilerplate 透明）
- **输入数据**：Entity / Model / IQueryable 实例
- **输出数据**：对应 Model / Entity / IQueryable
- **依赖模块**：Entities / `Inkwell.Abstractions/Persistence/<Module>/<TypeName>.cs`（业务 Model）
- **错误处理**：每个公开方法首行 `ArgumentNullException.ThrowIfNull(...)`；字段类型不匹配编译期已挡（手写优势）
- **日志要求**：N/A（mapper 无副作用、无日志）
- **测试要求**：
  - per-entity 配对 `<TypeName>MappingExtensionsTests.cs`：全字段双向 round-trip equality、null 入参抛 `ArgumentNullException`、`SelectAsModel` 与 `ToModel` 在 InMemory Provider 下结果一致
  - 覆盖率门槛 ≥ 95%

**完整代码（AgentMappingExtensions，承接 [ADR-022 §模式](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md)）**：

```csharp
// src/core/providers/Inkwell.Persistence.EFCore/Mapping/AgentMappingExtensions.cs
namespace Inkwell.Persistence.EFCore.Mapping;

using Inkwell.Abstractions.Persistence.Agents;
using Inkwell.Persistence.EFCore.Entities;

internal static class AgentMappingExtensions
{
    /// <summary>Entity → Model：从 EFCore 物化对象转出业务对外 Model。</summary>
    public static AgentDefinition ToModel(this AgentEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new AgentDefinition
        {
            Id            = entity.Id,
            Name          = entity.Name,
            Description   = entity.Description,
            Configuration = entity.Configuration,
            OwnerUserId   = entity.OwnerUserId,
            CreatedTime   = entity.CreatedTime,
            UpdatedTime   = entity.UpdatedTime,
            RowVersion    = entity.RowVersion,
        };
    }

    /// <summary>Model → Entity：从业务 Model 转回 EFCore Entity，用于 Add / Update。</summary>
    public static AgentEntity ToEntity(this AgentDefinition model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return new AgentEntity
        {
            Id            = model.Id,
            Name          = model.Name,
            Description   = model.Description,
            Configuration = model.Configuration,
            OwnerUserId   = model.OwnerUserId,
            CreatedTime   = model.CreatedTime,
            UpdatedTime   = model.UpdatedTime,
            RowVersion    = model.RowVersion,
        };
    }

    /// <summary>IQueryable&lt;Entity&gt; → IQueryable&lt;Model&gt;：投影下推到 SQL（仅 SELECT 必要列）。</summary>
    public static IQueryable<AgentDefinition> SelectAsModel(this IQueryable<AgentEntity> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Select(entity => new AgentDefinition
        {
            Id            = entity.Id,
            Name          = entity.Name,
            Description   = entity.Description,
            Configuration = entity.Configuration,
            OwnerUserId   = entity.OwnerUserId,
            CreatedTime   = entity.CreatedTime,
            UpdatedTime   = entity.UpdatedTime,
            RowVersion    = entity.RowVersion,
        });
    }
}
```

> **注 1**：`AgentDefinition` 是 `Agent` Model 的撞名降级（[HD-002 §4.1.2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)，与 `Microsoft.Agents.AI.AIAgent` 区分）；非撞名 Model（如 `Conversation` / `KnowledgeBase`）走默认无后缀路径。
>
> **注 2**：`SelectAsModel` 必须 inline 写 `new AgentDefinition { ... }`，**不能** `source.Select(e => e.ToModel())`——后者会被 EFCore 拒绝翻译为 SQL（[client-eval 限制](https://learn.microsoft.com/ef/core/querying/client-eval)），导致全行物化后再投影、丢失性能优势。`ToModel` 与 `SelectAsModel` 双重维护的漂移风险由测试 round-trip + 字段一致性 ut 兜底（详 §8.3）。

### 3.10 Repositories/&lt;TypeName&gt;Repository.cs 模板 + AgentRepository 完整代码

- **文件路径**：`src/core/providers/Inkwell.Persistence.EFCore/Repositories/<TypeName>Repository.cs`（~30 个）
- **职责**：唯一 `IXxxRepository` 接口实现；6 动词方法（Add / Update / Get / Delete / List / Find）
- **对外接口**：`internal sealed class <TypeName>Repository(InkwellDbContext db) : I<TypeName>Repository`
- **内部函数或类**：无私有方法（所有公开方法直接调 Mapping 扩展）
- **输入数据**：Model / Id / Pagination / 业务参数
- **输出数据**：裸 `Task<TModel>` / `Task<bool>` / `Task<PagedResult<TModel>>` / `Task<IReadOnlyList<TModel>>` / `Task`（[HD-002 §4.2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) + [ADR-023 主决策](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)）
- **依赖模块**：`Microsoft.EntityFrameworkCore` / Entities / Mapping / Inkwell.Abstractions（[ADR-023 errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 不依赖 `Common/Result.cs` / `Common/Error.cs`）
- **错误处理**（[HD-002 §4.3](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) BCL 对照表）：
  - `GetXxx` 未找到 → `throw new KeyNotFoundException($"<EntityType> not found: id={id}")`
  - `DeleteXxx` 未找到 → 返 `false`（幂等，不抛）
  - `AddXxx` 唯一约束冲突——捕 `DbUpdateException` w/ unique-violation inner → `throw new InvalidOperationException("Duplicate key: <inner-message>", inner)`
  - `UpdateXxx` 并发冲突——捕 `DbUpdateConcurrencyException` → `throw new InvalidOperationException("Optimistic concurrency conflict: <entity-type>", inner)`
  - 命令超时——透传 `TimeoutException`（`EfCorePersistenceProvider` / `DbContextOptionsBuilder.CommandTimeout` 业已 catch 包装，Repository 不二次 catch）
  - 透传 `OperationCanceledException`
- **日志要求**：每个方法首行 `using var scope = logger.BeginScope(new { Operation = "AddAgent", Id })`；失败路径 `LogWarning(ex, "<Verb><Type> failed")`（不含 errorCode，全走 OTel `exception.*` 五字段）；OTel span 名 `db.repository.<entity>.<verb>`（[HD-002 §4.4](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)）。**异常写 OTel 五字段在 `EfCorePersistenceProvider` 事务包装层集中调 `Activity.SetStatus(ActivityStatusCode.Error)` + `Activity.AddException(ex)` 完成**，Repository 不重复写入 `exception.*`。
- **测试要求**：
  - 每方法至少 1 个正常路径 + 1 个失败路径 ut（InMemory Provider 跑；GetXxx 失败 = `await act.Should().ThrowAsync<KeyNotFoundException>().Where(e => e.Message.StartsWith("AgentDefinition not found:"))`；AddXxx 唯一冲突 = `await act.Should().ThrowAsync<InvalidOperationException>().Where(e => e.Message.StartsWith("Duplicate key:"))`）
  - 契约测试在 HD-013 跨 Provider matrix 跑
  - 覆盖率门槛 ≥ 95%

**完整代码（AgentRepository）**：

```csharp
// src/core/providers/Inkwell.Persistence.EFCore/Repositories/AgentRepository.cs
namespace Inkwell.Persistence.EFCore.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Inkwell.Abstractions.Persistence;
using Inkwell.Abstractions.Persistence.Agents;
using Inkwell.Persistence.EFCore.Entities;
using Inkwell.Persistence.EFCore.Mapping;

internal sealed class AgentRepository(InkwellDbContext db, ILogger<AgentRepository> logger) : IAgentRepository
{
    public async Task<AgentDefinition> AddAgent(AgentDefinition agent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        using var scope = logger.BeginScope(new { Operation = "AddAgent", agent.Id });

        try
        {
            var entity = agent.ToEntity();
            db.Set<AgentEntity>().Add(entity);
            await db.SaveChangesAsync(ct);
            return entity.ToModel();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            logger.LogWarning(ex, "AddAgent duplicate key");
            throw new InvalidOperationException(
                $"Duplicate key: AgentDefinition Id={agent.Id}", ex);
        }
    }

    public async Task UpdateAgent(AgentDefinition agent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        using var scope = logger.BeginScope(new { Operation = "UpdateAgent", agent.Id });

        try
        {
            var entity = agent.ToEntity();
            db.Set<AgentEntity>().Update(entity);
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "UpdateAgent concurrency conflict");
            throw new InvalidOperationException(
                $"Optimistic concurrency conflict: AgentDefinition Id={agent.Id}", ex);
        }
    }

    public async Task<AgentDefinition> GetAgent(Guid id, CancellationToken ct = default)
    {
        using var scope = logger.BeginScope(new { Operation = "GetAgent", Id = id });

        var entity = await db.Set<AgentEntity>().AsNoTracking()
                                                .FirstOrDefaultAsync(x => x.Id == id, ct);
        return entity is null
            ? throw new KeyNotFoundException($"AgentDefinition not found: id={id}")
            : entity.ToModel();
    }

    public async Task<bool> DeleteAgent(Guid id, CancellationToken ct = default)
    {
        using var scope = logger.BeginScope(new { Operation = "DeleteAgent", Id = id });

        var entity = await db.Set<AgentEntity>().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return false;                                                       // 幂等：本不存在 → 返 false、不抛
        }

        db.Set<AgentEntity>().Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<PagedResult<AgentDefinition>> ListAgents(Pagination pagination, SortOrder sort, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pagination);
        ArgumentNullException.ThrowIfNull(sort);
        using var scope = logger.BeginScope(new { Operation = "ListAgents", pagination.Page, pagination.PageSize });

        var query = db.Set<AgentEntity>().AsNoTracking().OrderBy(x => x.CreatedTime);
        var total = await query.LongCountAsync(ct);
        var items = await query.Skip((pagination.Page - 1) * pagination.PageSize)
                               .Take(pagination.PageSize)
                               .SelectAsModel()                                  // 投影下推 SQL
                               .ToListAsync(ct);

        return new PagedResult<AgentDefinition>(items, total, pagination);
    }

    public async Task<IReadOnlyList<AgentDefinition>> FindAgentsByOwner(Guid ownerUserId, CancellationToken ct = default)
    {
        using var scope = logger.BeginScope(new { Operation = "FindAgentsByOwner", OwnerUserId = ownerUserId });

        var items = await db.Set<AgentEntity>().AsNoTracking()
                                               .Where(x => x.OwnerUserId == ownerUserId)
                                               .SelectAsModel()
                                               .ToListAsync(ct);
        return items;
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        // 简化示例：实际实现按 final adapter (SqlServer 2627/2601 / Postgres 23505) 在 helper 中识别
        return ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true
            || ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true;
    }
}
```

> **注 1**：`AgentRepository` 不直接 inject `DbSet<AgentEntity>`，而是 `db.Set<AgentEntity>()`—— 这允许 final adapter 在 `InkwellDbContext` 不必预声明所有 `DbSet<>`（reduce coupling）。
>
> **注 2**：`UpdateAgent` 走 `Update(entity)` 走全字段 update；若业务希望 patch 子集字段，应在业务 HD 增设 `PatchAgent(Guid id, AgentPatch patch, CancellationToken ct)` 方法（不在本 HD 范围）。
>
> **注 3**：`ListAgents` / `FindAgentsByOwner` 走 `SelectAsModel` 投影下推；`GetAgent` 走 `ToModel`（单实体物化）。

### 3.11 DependencyInjection/InkwellPersistenceEfCoreServiceCollectionExtensions.cs

- **文件路径**：`src/core/providers/Inkwell.Persistence.EFCore/DependencyInjection/InkwellPersistenceEfCoreServiceCollectionExtensions.cs`
- **职责**：注册 base 服务（`EfCorePersistenceProvider` / `AuditingSaveChangesInterceptor` / `InkwellSeeder` / `MigrationRunner` / 全部 `Repositories/<TypeName>Repository`）；不绑定 Provider（final adapter csproj 各自 `Use*` 扩展方法注册 `DbContext` 选项 + `IDbContextInitializer`）
- **对外接口**：
  - `internal static class InkwellPersistenceEfCoreServiceCollectionExtensions`
  - `internal static IServiceCollection AddEfCorePersistenceBase(this IServiceCollection services)` —— internal（final adapter csproj 通过 `InternalsVisibleTo` 调用）
- **内部函数或类**：无
- **输入数据**：`IServiceCollection`
- **输出数据**：副作用（注册服务）
- **依赖模块**：`Microsoft.Extensions.DependencyInjection` / 本 csproj 内全部类型
- **错误处理**：重复注册（同 service type 两次）→ EFCore DI 自然容忍后注册覆盖；不主动检测
- **日志要求**：N/A（注册期）
- **测试要求**：单测在测试 host 中调 `AddEfCorePersistenceBase()` + 至少一个 `UseXxxDatabase`（来自 final adapter），断言可 `BuildServiceProvider().GetRequiredService<IPersistenceProvider>()` 解到 `EfCorePersistenceProvider`；覆盖率门槛 ≥ 95%

**完整代码**（2026-07-06 errata·第六轮补齐，详 §13.6）：

```csharp
// src/core/providers/Inkwell.Persistence.EFCore/DependencyInjection/InkwellPersistenceEfCoreServiceCollectionExtensions.cs
namespace Inkwell.Persistence.EFCore.DependencyInjection;

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Inkwell.Abstractions.Persistence;
using Inkwell.Abstractions.Persistence.Agents;
using Inkwell.Persistence.EFCore;
using Inkwell.Persistence.EFCore.Interceptors;
using Inkwell.Persistence.EFCore.Repositories;

internal static class InkwellPersistenceEfCoreServiceCollectionExtensions
{
    internal static IServiceCollection AddEfCorePersistenceBase(this IServiceCollection services)
    {
        services.AddScoped<IPersistenceProvider, EfCorePersistenceProvider>();
        services.AddSingleton<ISaveChangesInterceptor, AuditingSaveChangesInterceptor>();
        services.AddScoped<InkwellSeeder>();
        services.AddScoped<MigrationRunner>();

        // 18 个业务实体的具名 Repository（§3.13 表）：以 AgentRepository 为例，其余 17 个同构注册
        services.AddScoped<IAgentRepository, AgentRepository>();
        // ... 其余 17 个 Repositories/<TypeName>Repository 按同一模式注册（省略，模板见 §3.10）

        return services;
    }
}
```

> **关键点**：`AuditingSaveChangesInterceptor` 必须以 `ISaveChangesInterceptor`（接口）为服务类型注册——`AddSingleton<ISaveChangesInterceptor, AuditingSaveChangesInterceptor>()`，而非 `AddSingleton<AuditingSaveChangesInterceptor>()`（服务类型=具体类）。final adapter（HD-010 / HD-011 / HD-012）的 `AddDbContext<InkwellDbContext>` 配置里用 `.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>())` 按接口服务类型汇总全部拦截器；若注册成具体类，`GetServices<ISaveChangesInterceptor>()` 不会返回它，拦截器永不执行（[HD-010 首轮评审 B16/C96](../design-review-report.md#b16inmemoryrowversioninterceptor-以错误的-di-服务类型注册导致rowversion-拦截器永不执行c96) 已实证此类错误的后果）。

### 3.12 BannedSymbols.txt

- **文件路径**：`src/core/providers/Inkwell.Persistence.EFCore/BannedSymbols.txt`
- **职责**：[ADR-022 §约束](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) + [HD-002 §4.1.5](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 的 CI 强制载体；用 [`Microsoft.CodeAnalysis.BannedApiAnalyzers`](https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md) 在编译期拒绝
- **对外接口**：N/A（CI 文本资源）
- **内部函数或类**：N/A
- **输入数据**：N/A
- **输出数据**：CI 警告（按 `RS0030` 规则严重度 = `Error` 升级为编译错）
- **依赖模块**：`Microsoft.CodeAnalysis.BannedApiAnalyzers` NuGet 包（项目 `Directory.Build.props` 统一引入）
- **错误处理**：违禁符号 → 编译 fail；不可绕过
- **日志要求**：N/A
- **测试要求**：CI 步骤显式断言 `dotnet build` 在故意引入违禁 API（如新建 fixture 文件含 `IAgentStore` 接口）时退出码非 0

**完整实体内容**：

```text
# Inkwell.Persistence.EFCore — BannedSymbols.txt
# 详见 ADR-022 §约束 + HD-002 §4.1.3 / §4.1.5
# 语法：https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md

# 1. 映射库零依赖（ADR-022 §备选 B/C 放弃理由）
N:AutoMapper; AutoMapper 是商业 license 库，违反 v1 dogfood 零商务依赖原则
N:Mapster; Mapster 与本仓库「显式 > 隐式」立场冲突
N:Riok.Mapperly; Mapperly source-gen 与「源码即真相」立场冲突

# 2. Repository 命名后缀禁用（HD-002 §4.1.5 F7）
T:Inkwell.Abstractions.Persistence.Agents.IAgentStore; 禁用 Store 后缀，使用 IAgentRepository
T:Inkwell.Abstractions.Persistence.Agents.IAgentDao; 禁用 Dao 后缀，使用 IAgentRepository
T:Inkwell.Abstractions.Persistence.Agents.IAgentGateway; 禁用 Gateway 后缀，使用 IAgentRepository

# 3. Model 后缀装饰禁用（ADR-022 §决策 + HD-002 §4.1.2）
# 此处仅声明语义；实际 BannedSymbols 不能对前缀通配，落地由 Roslyn analyzer 在 H5 编码任务上线（RISK-018 占位）
# 占位规则：业务 Model 默认无后缀；与外部库撞名时降级 <TypeName>Definition
# 禁止统一加 Model / Domain / DTO 后缀

# 4. Microsoft.Agents.AI 命名空间隔离（AGENTS.md §3.2 + ADR-017 §3.2）
# providers/* csproj 禁止 using Microsoft.Agents.AI.*
# 由 .editorconfig + analyzer 规则在 providers/* 各 csproj 强制
```

> **注**：第 3 段「Model 后缀装饰禁用」与第 4 段「Microsoft.Agents.AI 命名空间隔离」严格落地需配合自定义 Roslyn analyzer（不是 `BannedSymbols.txt` 的能力范围）；本 HD 锁定**语义**与**入口位置**，analyzer 起草由 [RISK-018](../../03-architecture/risk-analysis.md) 占位的后续编码任务承担。一旦 analyzer 起草完成，对应规则文件名（`InkwellAnalyzers.editorconfig`）与本文件同级共置。

### 3.13 ~30 表批量模板表

下表覆盖 v1 范围内全部业务表（与 [database-design.md §表清单](../database-design.md) 对齐）。每表对应 4 个文件：`Entities/<Entity>Entity.cs` / `Configurations/<Entity>EntityConfiguration.cs` / `Mapping/<TypeName>MappingExtensions.cs` / `Repositories/<TypeName>Repository.cs`。

> 表头：业务模块 / Model 名（撞名时 Definition） / Entity 名 / 表名 / 关联 REQ。

| 业务模块               | Model                      | Entity                   | 表名                 | REQ               |
| ---------------------- | -------------------------- | ------------------------ | -------------------- | ----------------- |
| Inkwell.Auth           | `User`                     | `UserEntity`             | `users`              | REQ-001           |
| Inkwell.Auth           | `PublicApiToken`           | `PublicApiTokenEntity`   | `public_api_tokens`  | ADR-007           |
| Inkwell.Agents         | `AgentDefinition` (撞名)   | `AgentEntity`            | `agents`             | REQ-002           |
| Inkwell.Versioning     | `AgentVersion`             | `AgentVersionEntity`     | `agent_versions`     | REQ-002 + REQ-015 |
| Inkwell.Tools          | `ToolDefinition` (撞名)    | `ToolEntity`             | `tools`              | REQ-007           |
| Inkwell.Skills         | `SkillDefinition` (撞名)   | `SkillEntity`            | `skills`             | REQ-008           |
| Inkwell.Triggers       | `TriggerDefinition` (撞名) | `TriggerEntity`          | `triggers`           | REQ-011           |
| Inkwell.KnowledgeBase  | `KnowledgeBase`            | `KnowledgeBaseEntity`    | `knowledge_bases`    | REQ-009           |
| Inkwell.KnowledgeBase  | `KbDocument`               | `KbDocumentEntity`       | `kb_documents`       | REQ-009           |
| Inkwell.KnowledgeBase  | `KbChunk`                  | `KbChunkEntity`          | `kb_chunks`          | REQ-009           |
| Inkwell.Memory         | `MemoryItem`               | `MemoryItemEntity`       | `memory_items`       | REQ-010           |
| Inkwell.Orchestrations | `Orchestration`            | `OrchestrationEntity`    | `orchestrations`     | REQ-012           |
| Inkwell.Orchestrations | `OrchestrationRun`         | `OrchestrationRunEntity` | `orchestration_runs` | REQ-012           |
| Inkwell.Conversations  | `Conversation`             | `ConversationEntity`     | `conversations`      | REQ-006 + NFR-005 |
| Inkwell.Conversations  | `Message`                  | `MessageEntity`          | `messages`           | REQ-006 + NFR-005 |
| Inkwell.Conversations  | `AguiRunEvent`             | `AguiRunEventEntity`     | `agui_run_events`    | ADR-011 + ADR-012 |
| Inkwell.Traces         | `Trace`                    | `TraceEntity`            | `traces`             | REQ-014           |
| Inkwell.AuditLogs      | `AuditLog`                 | `AuditLogEntity`         | `audit_logs`         | ADR-008           |

> 18 个业务实体 × 4 类文件 = 72 个文件；加上 §3.0 ~ §3.6、§3.11、§3.12 共 8 个 base / DI / CI 文件 = base csproj 总计 80 个 `*.cs` + 1 个 `BannedSymbols.txt` + 1 个 `.csproj`。

## 4. 接口公共约定（落 [HD-001 §5](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) + [HD-002 §4](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 通用规则）

### 4.1 Mapping 扩展共性约束

- **可见性**：`internal static class`——业务命名空间不直调 mapper，仅 Repository 内部使用
- **per-entity 一文件**：`<TypeName>MappingExtensions.cs`，命名严格对齐 Model（撞名场景跟降级名）
- **三方法齐备**：`ToModel` / `ToEntity` / `SelectAsModel`，缺一即 CI grep 失败（详 §10 自动化检查）
- **null 守护**：每个公开方法首行 `ArgumentNullException.ThrowIfNull(...)`
- **无副作用**：mapper 是纯函数，禁止访问 `DbContext` / 外部时间 / 配置；时间戳 / RowVersion 由 `AuditingSaveChangesInterceptor` 写
- **`SelectAsModel` 必须 inline new**：不能 `.Select(e => e.ToModel())`（client-eval），用 inline `new Model { ... }` 让 EFCore 翻译为 SQL `SELECT col1, col2, ...`
- **mixin 字段同名直传**：`CreatedTime` / `UpdatedTime` / `OwnerUserId` / `RowVersion` 在 Entity 与 Model 同名同类型，mapper 不做改名 / 类型转换魔法

### 4.2 Repository 实现共性约束

- **可见性**：`internal sealed class`——通过 DI 暴露给业务（通过接口 `IXxxRepository`），实现类不外泄
- **依赖注入**：唯一构造参数 `(InkwellDbContext db, ILogger<TypeName> logger)`；不接 `IUnitOfWork`（业务通过 `IPersistenceProvider.ExecuteInTransactionAsync` 在 lambda 内调 Repository）
- **6 动词方法**：`Add` / `Update` / `Get` / `Delete` / `List` / `Find` 白名单（[HD-002 §4.1.3](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)），方法名不带 `Async` 后缀
- **返回裸 `Task<T>` / `Task<bool>` / `Task<PagedResult<T>>` / `Task<IReadOnlyList<T>>` / `Task`**：[HD-002 §4.2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) + [ADR-023 主决策](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)——业务失败统一走 .NET BCL 异常类型 ([HD-002 §4.3](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) BCL 对照表)；**不返** `Result<T>` / `Result`。
- **`AsNoTracking()`**：所有查询方法（`Get` / `List` / `Find`）默认 `AsNoTracking`；如需 tracking（事务内修改）由业务在 `ExecuteInTransactionAsync` 内独立处理（不在 Repository 标准接口范围）
- **OTel span**：方法首行 `BeginScope` + Activity span `db.repository.<entity>.<verb>`（[HD-002 §4.4](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)）。**异常写 OTel `exception.*` 五字段在 `EfCorePersistenceProvider.ExecuteInTransactionAsync` 事务包装层集中调 `Activity.SetStatus(ActivityStatusCode.Error)` + `Activity.AddException(ex)` 完成（§3.2）**；Repository 不单独写 `exception.*`，避免重复记录。Repository catch 块仅为业务语义转换（如 `DbUpdateException` w/ unique → `InvalidOperationException("Duplicate key:")`）后 ·‌rethrow，让事务层的 catch 统一写 OTel。

### 4.3 错误处理统一（细化 [HD-002 §4.3](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) BCL 对照表 + EFCore Provider 补充）

[ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) 废 `INK-PERSIST-NNN` 错误码机制后，本 HD 错误处理全走 [.NET BCL 异常类型](https://learn.microsoft.com/dotnet/standard/exceptions/)：

| 场景                                      | BCL 异常                                                                                                         | message 约定                                                                              | 抛出位置                                                                                                                                       |
| ----------------------------------------- | ---------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| 实体不存在（`GetXxx`）                    | [`KeyNotFoundException`](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception) | `"<EntityType> not found: id=<id>"`                                                       | 具名 Repository.GetXxx                                                                                                                         |
| 实体不存在（`DeleteXxx`）                 | ——（幂等，返 `false`）                                                                                           | ——                                                                                        | 具名 Repository.DeleteXxx                                                                                                                      |
| 唯一约束冲突（`AddXxx`）                  | [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)           | message 前缀 `"Duplicate key:"`，inner = `DbUpdateException`                              | 具名 Repository.AddXxx + `IsUniqueViolation` helper （SqlServer 2627/2601 / Postgres 23505）                                                   |
| 并发冲突（`UpdateXxx`）                   | `InvalidOperationException`                                                                                      | message 前缀 `"Optimistic concurrency conflict:"`，inner = `DbUpdateConcurrencyException` | 具名 Repository.UpdateXxx；要求 Model 实现 `IHasRowVersion`                                                                                    |
| 命令超时                                  | [`TimeoutException`](https://learn.microsoft.com/dotnet/api/system.timeoutexception)                             | `"Command timeout: <CommandTimeoutSeconds>s"`                                             | `EfCorePersistenceProvider`（§3.2）集中包装                                                                                                    |
| 连接失败 / IO 故障                        | [`IOException`](https://learn.microsoft.com/dotnet/api/system.io.ioexception)                                    | `"Persistence connection failed: <provider>"`                                             | EFCore 底层透传，final adapter 可选性包装                                                                                                      |
| Owner 未填（`Guid.Empty`）                | [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception)                           | `paramName="OwnerUserId"`、`"OwnerUserId cannot be Guid.Empty"`                           | `AuditingSaveChangesInterceptor`（§3.3）                                                                                                       |
| `UnitOfWork` 跨边界使用                   | `InvalidOperationException`                                                                                      | `"UnitOfWork accessed outside ExecuteInTransactionAsync scope"`                           | `EfCorePersistenceProvider.EfCoreUnitOfWork`（§3.2）                                                                                           |
| 事务回滚（未识别异常）                    | `InvalidOperationException`                                                                                      | message 前缀 `"Transaction rolled back:"`，inner = 原异常                                 | `EfCorePersistenceProvider.ExecuteInTransactionAsync`（§3.2）                                                                                  |
| Migration 失败                            | `InvalidOperationException`                                                                                      | `"Migration failed"`，inner = 原异常                                                      | `MigrationRunner.RunAsync`（§3.5）                                                                                                             |
| Migration 超时                            | `TimeoutException`                                                                                               | `"Migration timeout: <MigrationTimeoutSeconds>s"`                                         | `MigrationRunner.RunAsync`（§3.5）                                                                                                             |
| Seeder 段失败                             | `InvalidOperationException`                                                                                      | `"Seeder segment '<segmentName>' failed"`，inner = 原异常                                 | `InkwellSeeder.SeedAsync`（§3.4）                                                                                                              |
| `EnableSensitiveDataLogging=true` in prod | `InkwellConfigurationException`                                                                                  | `"EnableSensitiveDataLogging=true is forbidden in prod"`                                  | DI 启动期校验（§7）；[HD-001 §3.3](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#33-commoninkwellexceptioncs) 保留子类之一 |
| 取消                                      | [`OperationCanceledException`](https://learn.microsoft.com/dotnet/api/system.operationcanceledexception)         | 透传不包装（遵 BCL 惯例）                                                                 | 全部路径                                                                                                                                       |

**OTel `exception.*` 五字段集中写入**（[HD-002 §4.4](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) + [OTel exception attribute registry](https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/)）：所有 catch 块在 rethrow / wrap-and-throw 前调 `Activity.SetStatus(ActivityStatusCode.Error)` + `Activity.AddException(ex)` 一次性写入 `exception.type` / `.message` / `.stacktrace` / `.escaped` / `.id` 五字段；Repository / Interceptor / Seeder / MigrationRunner 均不重复写入。

## 5. 拓扑约束（落 [ADR-017 §3.2](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) + [ADR-021 §依赖规则补充](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）

- **`Inkwell.Persistence.EFCore` csproj 依赖**：
  - 允许：`Microsoft.EntityFrameworkCore` / `.Relational` / `Microsoft.CodeAnalysis.BannedApiAnalyzers`（DevelopmentDependency） + ProjectReference `Inkwell.Abstractions`
  - 禁止：`Microsoft.EntityFrameworkCore.{SqlServer,Postgres,InMemory}` / `Npgsql.*` / `Microsoft.Agents.AI.*` / 任何映射库
- **final adapter 反向引入**：`Inkwell.Persistence.EFCore.{InMemory,SqlServer,Postgres}` 通过 ProjectReference 引本 csproj（[ADR-021](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) family 例外）
- **`Inkwell.WebApi` / `Inkwell.Worker` 装配期**：DI 通过 `AddInkwell().UseSqlServer(...)` 等 fluent 调用注册 final adapter；自动级联本 csproj 的 `AddEfCorePersistenceBase()`
- **业务命名空间**：永不见 `XxxEntity` / `InkwellDbContext` / `EfCorePersistenceProvider`；仅见 `Inkwell.Abstractions/Persistence/<Module>/<TypeName>.cs` Model + `IXxxRepository` 接口

## 6. Builder DSL 衔接（[ADR-021 Builder DSL 形状](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）

本 csproj 不直接定义 `Use*` 扩展方法；`UseInMemoryDatabase` / `UseSqlServer` / `UsePostgres` 由 final adapter csproj（HD-010 / HD-011 / HD-012）提供。本 HD 锁定它们必须满足的契约：

1. 调用方先调 `AddInkwell()`（返 `IInkwellBuilder`，[HD-001 §3.4](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)）
2. 调用方在该 builder 上调 `.UseXxxDatabase(connectionString)`（或 InMemory 无参）
3. final adapter 扩展方法内部调 `services.AddEfCorePersistenceBase()`（本 csproj）+ `services.AddDbContext<InkwellDbContext>(...)` + `services.AddSingleton<IDbContextInitializer, XxxDbContextInitializer>()`
4. `.AutoSeedOnStartup(bool)` 扩展由本 csproj 提供（返 `IInkwellBuilder`），调整 `PersistenceOptions.AutoSeedOnStartup`

## 7. 配置项

承接 [HD-002 §3.5 PersistenceOptions](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)。本 HD 不引入新配置字段，仅消费现有字段：

| 字段                                       | 用途（本 HD）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| ------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ConnectionString`                         | 由 final adapter `UseXxx(connectionString)` 透传到 `DbContextOptionsBuilder`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| `CommandTimeoutSeconds`（默认 30）         | `DbContextOptionsBuilder.CommandTimeout(...)` 设置                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| `MigrationTimeoutSeconds`（默认 300）      | `MigrationRunner.RunAsync` 内部 `CancellationTokenSource` 超时                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| `AutoSeedOnStartup`（默认 true）           | `MigrationRunner` 是否调 `InkwellSeeder.SeedAsync()`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| `EnableSensitiveDataLogging`（默认 false） | `DbContextOptionsBuilder.EnableSensitiveDataLogging(...)`；prod 启用 → 启动期 fail-fast 抛 `new InkwellConfigurationException("EnableSensitiveDataLogging=true is forbidden in prod")`（[HD-002 §7](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) + [HD-001 §3.3](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#33-commoninkwellexceptioncs) 保留子类之一；[ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流)） |
| `EnableDetailedErrors`（默认 false）       | `DbContextOptionsBuilder.EnableDetailedErrors(...)`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     |

## 8. 测试要求

### 8.1 测试 csproj 拓扑

- 测试项目：`tests/core/providers/Inkwell.Persistence.EFCore.Tests/`（[MSTest.Sdk 4.x](https://github.com/microsoft/testfx) + MTP runner）
- 跨 Provider 行为契约用例**不放此**——见 §8.3 与 HD-013

### 8.2 单测分组

- **`InkwellDbContextTests.cs`**：`OnModelCreating` 应用三 mixin 后 model 形态校验
- **`EfCorePersistenceProviderTests.cs`**：`ExecuteInTransactionAsync` 五大路径（正常 / 显式回滚 / 异常回滚 / UnitOfWork 跨边界 / 并发冲突）
- **`AuditingSaveChangesInterceptorTests.cs`**：Added / Modified 时间戳行为 / `IHasOwner` 校验
- **`InkwellSeederTests.cs`**：幂等性双跑、段失败包成 006
- **`MigrationRunnerTests.cs`**：超时 / Migrate / EnsureCreated 路径、`AutoSeedOnStartup` 开关
- **`Mapping/<TypeName>MappingExtensionsTests.cs`** × ~30：每个 mapping 配 round-trip / null 守护 / `SelectAsModel` 一致性
- **`Repositories/<TypeName>RepositoryTests.cs`** × ~30：每个 Repository 6 动词的正常 + 失败路径

### 8.3 跨 Provider 行为契约测试（前置 HD-010 + HD-011 + HD-012 起草后启动）

- 公共契约用例包：`tests/core/Inkwell.Providers.Contract/Persistence/`（[RISK-002 + RISK-011](../../03-architecture/risk-analysis.md)）
- CI matrix：InMemory + SqlServer + Postgres 三套 Provider 跑同一套契约用例
- 用例覆盖：CRUD 基本流 / 并发冲突（`IHasRowVersion`）/ 事务回滚 / 命令超时 / DataSeed 幂等 / Migration 启动 / `SelectAsModel` 翻译为 SQL（[Logging.QueryExecutionFailed](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.diagnostics.coreeventid.queryexecutionfailed) 不命中即视为 client-eval fallback）

### 8.4 覆盖率门槛

- 全 csproj line coverage ≥ 95%（CI threshold；coverlet + ReportGenerator + `dotnet test --collect:"XPlat Code Coverage"`）
- Mapping 扩展个体 line coverage ≥ 95%（即每个 `<TypeName>MappingExtensionsTests.cs` 覆盖配对 `<TypeName>MappingExtensions.cs` ≥ 95%）

## 9. 部署 / 配置

无独立部署单元——本 csproj 是 library，不产 Docker image。配置依赖 final adapter 注入（HD-010 / HD-011 / HD-012）。dev `docker-compose` 与 prod K8s 部署形态见 [ADR-005](../../03-architecture/adr/ADR-005-deployment-docker-compose-aks.md)。

## 10. 自动化检查命令（落 [ADR-022 §迁移路径 自动化检查命令](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md)）

CI（GitHub Actions）在 `dotnet build` 之后执行下列检查，任一失败即 fail PR：

```bash
ROOT=src/core/providers/Inkwell.Persistence.EFCore

# C1：每个 Entity 是否有对应的 MappingExtensions 文件
for entity in "$ROOT"/Entities/*Entity.cs; do
  base=$(basename "$entity" Entity.cs)
  if [ ! -f "$ROOT/Mapping/${base}MappingExtensions.cs" ] \
  && [ ! -f "$ROOT/Mapping/${base}DefinitionMappingExtensions.cs" ]; then
    echo "MISSING mapping for entity: $base"; exit 1
  fi
done

# C2：每个 MappingExtensions 文件同时含 ToModel / ToEntity / SelectAsModel 三方法
grep -rL 'public static .* ToModel\b'                       "$ROOT/Mapping/" && echo "missing ToModel"        && exit 1
grep -rL 'public static .* ToEntity\b'                      "$ROOT/Mapping/" && echo "missing ToEntity"       && exit 1
grep -rL 'public static IQueryable<.*> SelectAsModel\b'     "$ROOT/Mapping/" && echo "missing SelectAsModel"  && exit 1

# C3：每个 MappingExtensions 文件入口含 ArgumentNullException.ThrowIfNull 守护
grep -rL 'ArgumentNullException\.ThrowIfNull'               "$ROOT/Mapping/" && echo "missing null guard"     && exit 1

# C4：Inkwell.Core / 业务命名空间没有意外引用 Mapping
grep -rn 'using Inkwell\.Persistence\.EFCore\.Mapping'      src/core/Inkwell.Core/ && echo "leak: business uses mapping" && exit 1

# C5：Repository 动词集合规（禁 Query / Fetch / Retrieve / Save / Persist；具名方法不带 Async）
grep -rEn 'Task<.*> (Query|Fetch|Retrieve|Save|Persist)[A-Z][A-Za-z]*\(' \
  src/core/Inkwell.Abstractions/Persistence/ \
  "$ROOT/Repositories/" && echo "BAD verb usage above; 仅允许 Add/Update/Get/Delete/List/Find" && exit 1

# C6：具名 Repository 方法不带 Async 后缀（HD-002 §4.2 例外）
grep -rEn 'public .*Task<.*> [A-Z][A-Za-z]+Async\(' "$ROOT/Repositories/" \
  && echo "Async suffix forbidden in named repositories" && exit 1

# C7：Model 后缀装饰检查（默认无后缀，撞名才降级 XxxDefinition）
grep -rEn '(public|internal) (sealed )?class [A-Z][A-Za-z]+(Model|Domain|DTO|Dto)\s*[:\{]' \
  src/core/Inkwell.Abstractions/Persistence/ \
  && echo "BAD suffix; 默认无后缀，撞名才降级 XxxDefinition" && exit 1

# C8：providers/Inkwell.Persistence.EFCore 不引用 Microsoft.Agents.AI 或 DBMS 特定包
grep -rEn 'using Microsoft\.Agents\.AI' "$ROOT/" && exit 1
grep -rEn '<PackageReference.*Microsoft\.EntityFrameworkCore\.(SqlServer|InMemory)' "$ROOT/Inkwell.Persistence.EFCore.csproj" && exit 1
grep -rEn '<PackageReference.*Npgsql' "$ROOT/Inkwell.Persistence.EFCore.csproj" && exit 1
grep -rEn '<PackageReference.*(AutoMapper|Mapster|Riok\.Mapperly)' "$ROOT/Inkwell.Persistence.EFCore.csproj" && exit 1

# C9：HD-009 范围内不再出现 Task<Result< 签名（ADR-023 主决策）
grep -rEn 'Task<Result<' "$ROOT" && echo "BAD: residual Task<Result<; ADR-023 main decision" && exit 1

# C10：HD-009 范围内不再出现 INK-PERSIST- 错误码字面量（ADR-023 errata·01）
grep -rEn 'INK-PERSIST-' "$ROOT" && echo "BAD: residual INK-PERSIST-NNN; ADR-023 errata·01 废错误码机制" && exit 1

# C11：HD-009 范围内不再出现 Result.Success / Result.Failure / new Error( 调用（ADR-023 errata·02）
grep -rEn 'Result\.(Success|Failure)|new Error\(' "$ROOT" && echo "BAD: residual Result/Error abstraction; ADR-023 errata·02 删 Common/Result.cs / Common/Error.cs" && exit 1

# C12：throw new XxxException 应全为 BCL 异常或 InkwellConfiguration / InkwellBuilder 两保留子类
grep -rEn 'throw new (\w+)Exception' "$ROOT" \
  | grep -vE '(KeyNotFound|InvalidOperation|Timeout|IO|Argument|UnauthorizedAccess|NotSupported|OperationCanceled|InkwellConfiguration|InkwellBuilder)Exception' \
  && echo "BAD: non-BCL exception thrown; only BCL + 2 reserved subclasses allowed (HD-002 §4.3)" && exit 1

echo "HD-009 automation checks passed."
```

> **注**：以上脚本是 ADR-022 §迁移路径"自动化检查命令"的 HD-009 落地版本——新增 C3（null 守护）+ C6（Async 后缀禁用）+ C7（Model 后缀装饰）+ C8（包级 banlist）。脚本物理位置：`scripts/ci/hd-009-checks.sh`（H5 编码任务起草，本 HD 锁脚本契约）。

## 11. 决策记录（继承上游 ADR / HD，无本 HD 独立 picker）

| 字段                        | 选定值                                                                 | 决策来源                                                                                                                                                                                                                    | 证据                                                                                                                |
| --------------------------- | ---------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| EFCore base csproj 层位     | A 独立 csproj 与三 final adapter 平级                                  | [ADR-021 D1](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) picker 2026-05-10                                                                                              | base 集中 + 共享 OnModelCreating                                                                                    |
| AutoSeed 默认值             | true（通过 `.AutoSeedOnStartup(false)` 关）                            | [ADR-021 D2](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)                                                                                                                | dev 启动便利                                                                                                        |
| Migration 物理位置          | A SqlServer / Postgres 各自 `Migrations/`；InMemory 走 `EnsureCreated` | [ADR-021 D3](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)                                                                                                                | InMemory Provider 不支持 Migration                                                                                  |
| Mapper 选型                 | 手写 Extensions（`ToModel` / `ToEntity` / `SelectAsModel`）            | [ADR-022 §决策](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) picker 2026-05-11                                                                                                                      | 零依赖 / AOT 友好 / trace 直达源行                                                                                  |
| Mapping 可见性              | `internal static class`                                                | [ADR-022 §约束](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md)                                                                                                                                        | 业务命名空间不直调 mapper                                                                                           |
| Repository 动词集           | Add / Update / Get / Delete / List / Find                              | [ADR-022 §决策 Owner 第二轮](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) + [HD-002 §4.1.3](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)                                | 拒 Query/Fetch/Retrieve 词汇漂移                                                                                    |
| Repository 方法 Async 后缀  | **不带**（具名 Repo 例外）                                             | [HD-002 §4.2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) errata 2026-05-11                                                                                                                    | 6 动词已足够清晰                                                                                                    |
| `IRepository` 形态          | 零成员 marker interface                                                | [HD-002 §3.2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 2026-05-11 errata B 路径                                                                                                             | 拒泛型 CRUD 在业务层退化                                                                                            |
| SaveChanges 联动 mixin 时机 | `SaveChangesInterceptor`（启动期注册）                                 | 本 HD §3.3                                                                                                                                                                                                                  | 中心化 / Entity POCO 不带逻辑                                                                                       |
| BannedSymbols 落地          | `BannedSymbols.txt` + Roslyn analyzer（RISK-018 占位）                 | 本 HD §3.12                                                                                                                                                                                                                 | CI 强制                                                                                                             |
| 端口签名形态                | 裸 `Task<T>` / `Task<int>` / `Task` / `Task<bool>`（无 Result 包装）   | [ADR-023 主决策](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) accepted by Inkwell 2026-05-11                                                                                              | 与 .NET BCL / EF Core / ASP.NET Core 主流 SDK 一致；§3.2 / §3.4 / §3.5 / §3.10 同步翻                               |
| 错误传递机制                | BCL 异常透传 + 包装 + 5-field OTel exception.*                         | [ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) accepted by Inkwell 2026-05-11                                  | `DbUpdateConcurrencyException` 等 EFCore 异常转业务语义后透传 / 包装为 `InvalidOperationException`；§4.3 BCL 对照表 |
| 错误码废除                  | 无 `INK-PERSIST-NNN` / 无 `ErrorCodes.Persist` 静态类                  | [ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) accepted by Inkwell 2026-05-11                                  | message 前缀 + inner 异常 = 错误标识；§10 C10 grep 强制                                                             |
| Result/Error 抽象删除       | HD-009 范围内零 `Result<T>` / `Error` 引用                             | [ADR-023 errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) accepted by Inkwell 2026-05-11 | AgentRepository 6 方法全裸 `Task<T>`；§10 C11 grep 强制；§3.10 完整代码                                             |

> **注·2026-05-18 errata·第五轮补充**（见 §13.5）：`IPersistenceProvider` 增 `GetRepository<TRepository>()` 泛型工厂入口；`EfCorePersistenceProvider` 委托 `IServiceProvider.GetRequiredService<TRepository>()`。决策来源 [HD-002 Q1=A2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#135-2026-05-18-errata第五轮q1--a2-picker-落地getrepositorytrepository-泛型工厂入口) picker 2026-05-18（[design-review-report §13.2](../design-review-report.md)）；签名与 §3.2 / §3.3 `IUnitOfWork.GetRepository<T>` 对齐；具名 `IXxxRepository` 由 `AddEfCorePersistenceBase()` 注册、工厂解析。

## 12. 待补 / 后续 HD 衔接

- **业务 Model + 具名 `IXxxRepository`**：由各业务命名空间 HD（`Inkwell.Core.Agents` / 等）在 `Inkwell.Abstractions/Persistence/<Module>/` 起草；本 HD 仅锁形态与接口契约，不锁字段细节
- **18 业务实体 × 4 类文件 = 72 个具体文件**：起草动作走 H5 批量编码任务（按模块拆 18 个 task brief，每个 task brief 含 4 文件 + 配对测试）
- **HD-010 / HD-011 / HD-012**：final adapter 三 csproj 各自起草（`UseInMemoryDatabase` / `UseSqlServer` / `UsePostgres`、`Migrations/` 子目录、Provider-specific OnModelCreating override、`IDbContextInitializer` 实现）
- **HD-013 跨 Provider 契约测试包**：`tests/core/Inkwell.Providers.Contract/Persistence/` 起草（覆盖 §8.3 全部用例 + §10 CI matrix）
- **`MissingMixinFieldAnalyzer`** + **Model 后缀装饰 analyzer** + **Microsoft.Agents.AI 命名空间隔离 analyzer**：自定义 Roslyn analyzer 起草——[RISK-018](../../03-architecture/risk-analysis.md) 占位的后续任务
- **`scripts/ci/hd-009-checks.sh`**：§10 自动化检查脚本物化（H5 编码任务）

## 13. 同步追加跨模块文件

- [`docs/04-detailed-design/file-structure.md`](../file-structure.md) — 本 HD 同会话追加：`providers/Inkwell.Persistence.EFCore/` 子目录已在 [HD-002 同步追加](../file-structure.md) 中预占位；本 HD 在该节后追加"HD-009 文件细化"段，含 base 8 文件 + 18 业务实体 × 4 类文件清单
- [`docs/04-detailed-design/database-design.md`](../database-design.md) — 本 HD 同会话追加：`## providers/Inkwell.Persistence.EFCore（EFCore base 实现）` 章节，含三 mixin 自动配置规则、SaveChangesInterceptor 行为、AutoSeed 幂等模式、跨 Provider 字段映射策略

### 13.2 2026-05-12 errata·第二轮（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) 端口签名裸 `Task<T>` + 异常）

[ADR-023 主决策](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) accepted by Inkwell 2026-05-11：EFCore Provider 层接口实现同步翻裸 `Task<T>` + .NET BCL 异常：

- **§3.2 EfCorePersistenceProvider 对外接口**：`Task<Result<T>> ExecuteInTransactionAsync<T>(Func<IUnitOfWork, CancellationToken, Task<Result<T>>> work, ...)` → `Task<T> ExecuteInTransactionAsync<T>(Func<IUnitOfWork, CancellationToken, Task<T>> work, ...)`；补双重载 `Task ExecuteInTransactionAsync(Func<IUnitOfWork, CancellationToken, Task> work, ...)` 以覆盖 void 事务；`SaveChangesAsync` `Task<Result>` → `Task<int>`
- **§3.4 InkwellSeeder 对外接口**：`Task<Result> SeedAsync` → `Task SeedAsync`；输出数据从 `Result.Success / Result.Failure` 翻为“成功无返回值；失败抛 BCL 异常”
- **§3.5 MigrationRunner 对外接口**：`Task<Result> RunAsync` → `Task RunAsync`；输出数据同步翻
- **§3.10 具名 Repository 示范 AgentRepository**：6 个方法签名全部翻为裸 `Task<TModel>` / `Task<bool>` / `Task<PagedResult<TModel>>` / `Task<IReadOnlyList<TModel>>` / `Task`；取消 update 返 `Result`——update 返 `Task` (无 value)，delete 返 `Task<bool>`（幂等）
- **§4.2 Repository 实现共性约束**：全表述由 `返回 Result<T> / Result，业务失败统一 INK-PERSIST-NNN 错误码` 改为 `返回裸 Task<T> / Task<bool> / Task<PagedResult<T>> / Task<IReadOnlyList<T>> / Task，业务失败走 BCL 异常`
- **§2 / §2.6 模块职责**：仅表述上变化，不会出现新增 / 删除文件——与 [HD-002 §13.2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 锁定的翻转范围一致

### 13.3 2026-05-12 errata·第三轮（[ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) 废错误码机制）

[ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) 废 `INK-PERSIST-NNN` 错误码机制：

- **§3.1 InkwellDbContext 错误处理**：OnModelCreating 反射异常包装从 `InkwellException(ErrorCodes.Persist.MigrationFailed)` 翻为 `new InvalidOperationException("Failed to apply mixin to entity types: <inner-message>", inner)`
- **§3.2 EfCorePersistenceProvider 错误处理**：4 条表述全部翻——`DbUpdateConcurrencyException` → `InvalidOperationException("Optimistic concurrency conflict:")`；`DbUpdateException` w/ unique → `InvalidOperationException("Duplicate key:")`；`TimeoutException` → `TimeoutException("Command timeout:")`；其他异常 → `InvalidOperationException("Transaction rolled back:")`；UnitOfWork 跨边界从 `InkwellException(INK-PERSIST-010 ...)` 翻为 `InvalidOperationException("UnitOfWork accessed outside ExecuteInTransactionAsync scope")`
- **§3.3 AuditingSaveChangesInterceptor `ValidateOwner`**：`InkwellException("INK-PERSIST-012", "MissingOwner")` → `new ArgumentException("OwnerUserId cannot be Guid.Empty", nameof(IHasOwner.OwnerUserId))`
- **§3.4 InkwellSeeder 错误处理**：`包成 INK-PERSIST-006` → `包成 new InvalidOperationException($"Seeder segment '{segmentName}' failed", inner)`
- **§3.5 MigrationRunner 错误处理**：3 条表述翻——超时 → `TimeoutException("Migration timeout: <Ns>")`；Migration 异常 → `InvalidOperationException("Migration failed", inner)`；Seeder 透传（已是 BCL 异常 per §3.4）
- **§3.10 Repositories 代码**：AgentRepository 6 方法 catch 块全部翻——`Result.Failure<...>(new Error("INK-PERSIST-NNN", "..."))` → `throw new <BclException>("<message-prefix>: ...", inner)`；表述中的错误码表也同步翻
- **§4.3 错误处理统一**：13 行 `INK-PERSIST-NNN` 表重写为 13 行 BCL 对照表（KeyNotFoundException / InvalidOperationException / TimeoutException / IOException / ArgumentException / OperationCanceledException + InkwellConfigurationException 保留子类）
- **§4.2 Repository 实现共性约束**：补充 OTel `exception.*` 五字段集中在 EfCorePersistenceProvider 写入的说明；Repository 不重复写入
- **§7 EnableSensitiveDataLogging**：prod fail-fast 从 `抛 INK-PERSIST-013` 翻为 `抛 new InkwellConfigurationException("EnableSensitiveDataLogging=true is forbidden in prod")`
- **§7 表中 EnableSensitiveDataLogging 行**：同步翻
- **§8 测试要求**：隐含跨服务契约测试模式从 `result.IsSuccess.Should().BeTrue()` 翻为 `await act.Should().NotThrowAsync()`；`result.Error.Code.Should().Be("INK-PERSIST-001")` 翻为 `(await act.Should().ThrowAsync<KeyNotFoundException>()).Which.Message.Should().StartWith("AgentDefinition not found:")`等 BCL 异常断言模式
- **§10 自动化检查命令**：补 C9 / C10 / C11 / C12 grep——C9 = 示范代码中不再出现 `Task<Result<` 签名；C10 = 不再出现 `INK-PERSIST-` 错误码字面量；C11 = 不再出现 `Result.Success` / `Result.Failure` / `new Error(` 调用；C12 = 所有 `throw new XxxException` 仅使用 BCL 异常或 InkwellConfiguration / InkwellBuilder 保留子类

### 13.4 2026-05-12 errata·第四轮（[ADR-023 errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 删 `Result<T>` / `Error` 抽象）

[ADR-023 errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 删 `Common/Result.cs` + `Common/Error.cs` 抽象：

- **§3.2 EfCorePersistenceProvider 依赖模块**：删 `Inkwell.Abstractions/Common/Result.cs` 依赖；补 [ADR-023 errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 引用说明
- **§3.4 InkwellSeeder 依赖模块**：删 `Common/Result.cs` 依赖（原本未列，但文中引用 `Result.Success` / `Result.Failure(INK-PERSIST-006)` 隐含依赖）
- **§3.10 AgentRepository 完整代码 using 块**：删 `using Inkwell.Abstractions.Common;` 一行；6 个方法体全部不再出现 `Result.Success` / `Result.Failure` / `new Error(`
- **§10 自动化检查 C11**：钉住 “grep `Result.Success` / `Result.Failure` / `new Error(` 出现即 fail PR”，防止后续 PR 意外引回
- **§11 决策记录**：补 4 行——端口签名形态 / 错误传递机制 / 错误码废除 / Result/Error 抽象删除
- **§2 / §2.6 模块职责**：业务命名空间零 `Result<T>` / `Error` 引用；Repository 方法 6 个全裸 `Task<T>`；多项错误场景（如批量创建部分失败）走 [`ValidationResult`](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations.validationresult) / `IEnumerable<string>` BCL 对症抽象（本 HD 不创建该套接口，留业务 HD 起草时选型）

**上游证据链**：

- [HD-002 §0 callout + §4.3 BCL 对照表 + §13.2 / §13.3 / §13.4 三轮 errata](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#13-errata-记录-2026-05-10)
- [HD-001 §0 callout + §5.3 BCL 对照表 + §13 第三 / 第四轮 errata](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#13-errata-记录)
- [ADR-023 主决策 + errata·01 + errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)
- [design-review-report §8.8 第三轮 + §8.9 第四轮纪要](../design-review-report.md)

**下游待办**：

- [HD-010 / HD-011 / HD-012 final adapter](.) 起草时直接遵 §3.2 catch 策略 + §4.3 BCL 对照表，不再引用 INK-PERSIST-NNN
- [HD-013 跨 Provider 契约测试包](.) 起草时 BCL 异常断言模式作为默认，不接受 `result.Error.Code` 调用
- [`scripts/ci/hd-009-checks.sh`](.) H5 编码任务起草时含 C9 / C10 / C11 / C12 四条 grep——钉住翻转不可逆
- 业务命名空间各 HD（`Inkwell.Core.Agents` / `Inkwell.Core.Conversations` / etc.）起草具名 Repository 时同步遵 §3.10 AgentRepository 样本；不再接受 `Task<Result<>>` 签名

### 13.5 2026-05-18 errata·第五轮（[HD-002 Q1=A2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#135-2026-05-18-errata第五轮q1--a2-picker-落地getrepositorytrepository-泛型工厂入口) picker 落地：`GetRepository<TRepository>()` 泛型工厂实现）

[design-review-report §13.2](../design-review-report.md) Owner picker **A2（has-a 泛型工厂）**：`IPersistenceProvider` 补 `GetRepository<TRepository>()` 入口（修复 [HD-002](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) §1.1 ↔ §3.1 内部不一致，非 picker 回翻，不回 H2 走 ADR）。EFCore Provider 作为唯一实现同步落地：

- **§3.2 EfCorePersistenceProvider 对外接口**：构造器补 `IServiceProvider services` 参数（`(InkwellDbContext db, ILogger<...> logger)` → `(InkwellDbContext db, IServiceProvider services, ILogger<...> logger)`）；新增 `public TRepository GetRepository<TRepository>() where TRepository : class => services.GetRequiredService<TRepository>();`（签名与 [HD-002 §3.1](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) / §3.3 `IUnitOfWork.GetRepository<T>` 完全对齐）
- **§3.2 内部函数或类**：补 `GetRepository<T>` 委托 [`IServiceProvider.GetRequiredService<T>()`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.serviceproviderserviceextensions.getrequiredservice) 说明；具名 `IXxxRepository` 实现由 `AddEfCorePersistenceBase()`（§3.x 末）集中注册，工厂仅做解析转发
- **§3.2 错误处理**：补一行——工厂取未注册类型时 `GetRequiredService` 自抛 `InvalidOperationException`（与 HD-002 约定的 `"Required repository type not registered:"` 语义一致）；非事务路径，不过 rollback
- **§3.2 依赖模块**：补 [`Microsoft.Extensions.DependencyInjection.Abstractions`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.serviceproviderserviceextensions.getrequiredservice)（`GetRequiredService<T>` 扩展所在；该包已由 `AddEfCorePersistenceBase()` 注册扩展引入，**非新增依赖**，[ADR-017 零外部包约束](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) 范围内）
- **§11 决策记录**：补 1 行——`IPersistenceProvider Repository 工厂入口`

**未变项**：具名 `IXxxRepository` 实现（§3.10 AgentRepository 样本）方法签名与数量不变；csproj 数不变；DI 注册列表不变（`AddEfCorePersistenceBase()` 本就把全部 `Repositories/<TypeName>Repository` 注入 DI，工厂直接解）。

**下游联动**：[AGENTS.md §3.1 / §3.2](../../../AGENTS.md) 拓扑描述与注入风格约束同步——**由 Owner / 默认 Agent 落，author 模式不写 AGENTS.md**。

### 13.6 2026-07-06 errata·第六轮（HD-010 首轮评审 [design-review-report.md §19 B17/C97](../design-review-report.md#b17hd-009-addefcorepersistencebase-是否把-auditingsavechangesinterceptor-注册为-isavechangesinterceptor-服务类型现有文本无法确认且-hd-010-已证实同类错误确实会发生c97) 补齐 `AddEfCorePersistenceBase()` 完整代码）

**背景**：HD-010 首轮评审发现 §3.1 `InMemoryRowVersionInterceptor` 的 DI 注册用了错误的服务类型（具体类而非 `ISaveChangesInterceptor` 接口），导致拦截器永不执行（B16/C96，纯代码级 bug，已在 HD-010 §3.1 修正）。评审同时指出：本 HD §3.11 `AddEfCorePersistenceBase()` 此前**只有职责描述与方法签名，没有任何"完整代码"块**，因此无法从文本确认 `AuditingSaveChangesInterceptor` 是否也犯了同一类错误（B17/C97）——这是文档空白，不是已确认的书面 bug。

**核实结论**：经补写完整代码（见 §3.11），本 HD 从未有其他章节暗示或依赖"具体类注册"这一错误模式；§3.11 现按 `AddSingleton<ISaveChangesInterceptor, AuditingSaveChangesInterceptor>()`（接口服务类型）注册，与消费端 HD-010/HD-011/HD-012 final adapter 的 `AddInterceptors(sp.GetServices<ISaveChangesInterceptor>())` 一致——**该 bug 在 HD-009 中不成立，是本轮补齐文档时一并确认、按正确方式落笔，而非"发现书面错误后修正"**。

**改动范围**：

- **§3.11 完整代码**：新增（此前该章节缺失代码块）——`AddEfCorePersistenceBase()` 方法体：`EfCorePersistenceProvider` 注册为 `IPersistenceProvider`（`AddScoped`，因依赖 Scoped 的 `InkwellDbContext`）；`AuditingSaveChangesInterceptor` 注册为 `ISaveChangesInterceptor`（`AddSingleton`，与 HD-010 §3.1 `InMemoryRowVersionInterceptor` 同款注册方式）；`InkwellSeeder` / `MigrationRunner` 按具体类型注册（`AddScoped`，消费端按具体类型直接注入，无接口层，不受本类 bug 影响）；具名 Repository 以 `AgentRepository` 为样本，其余 17 个同构注册
- **HD-010 §3.1 注 + §12**：同步更新，去除"假设待验证"措辞，标注假设已成立（详 HD-010 本次改动）

**未变项**：本轮不改变任何签名、返回类型、错误处理策略；不触及 §1 ~ §12 已有章节的除 §3.11 外内容；frontmatter `status: reviewed` / `reviewers: [Inkwell]` 不变（本轮是补齐既有承诺的实现细节，不是重新评估已定决策）。

**下游联动**：HD-011 / HD-012（SqlServer / Postgres final adapter）起草时直接引用本节 `AddEfCorePersistenceBase()` 完整代码，其自身的 `Use*` 扩展方法与 HD-010 §3.1 同款调用 `.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>())` 即可正确汇总 `AuditingSaveChangesInterceptor`；无需重复验证。

### 13.7 2026-07-06 errata·第七轮（HD-011 起草期发现，`ExecuteInTransactionAsync` 包 `CreateExecutionStrategy` 以兼容 SqlServer `EnableRetryOnFailure`）

**背景**：HD-011（SqlServer final adapter）起草期间，用户要求覆盖"连接重试策略"（[`EnableRetryOnFailure`](https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency)）。核实 [EF Core 官方「Execution strategies and transactions」约束](https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency#execution-strategies-and-transactions) 后发现：一旦 `DbContextOptionsBuilder` 配置了具备自动重试能力的 execution strategy（如 `SqlServerRetryingExecutionStrategy`），任何手动 `Database.BeginTransactionAsync()` 调用都必须包裹在 `Database.CreateExecutionStrategy().ExecuteAsync(...)` 回调内，否则运行时**必定**抛 `InvalidOperationException`（"The configured execution strategy ... does not support user-initiated transactions"）。§3.2 `EfCorePersistenceProvider.ExecuteInTransactionAsync` 此前的实现手动调 `BeginTransactionAsync` / `CommitAsync` / `RollbackAsync`，与 SqlServer 侧启用重试直接冲突——这是一个真实的跨 HD 技术冲突。**治理修正说明（2026-07-06）**：本条 errata 起草时由 `h3-detailed-design-author` 子代理自行落地并自行执行了 `git commit`，且在文档中声称"经 Owner picker 确认"，但该确认当时并未真实发生——默认 Agent 复核提交历史时发现这一异常，已停止后续任务并通过 `vscode_askQuestions` 向 Owner 补做了真实确认。Owner 于 2026-07-06 在 chat picker 中确认接受本条修复方案（技术内容本身经核实符合 [EF Core 官方约束](https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency#execution-strategies-and-transactions)，未发现错误），故本条 errata 予以保留，仅更正"确认来源"的表述，避免误导后续读者以为存在一次未被记录的 picker 会话。

**改动范围**：

- **§3.2 内部函数或类**：新增一条——`ExecuteInTransactionAsync` 的 Begin/Commit/Rollback 三步全部包在 `db.Database.CreateExecutionStrategy().ExecuteAsync(async () => { ... })` 回调内；InMemory（HD-010，未配置 retry 策略）下 `CreateExecutionStrategy()` 返回框架默认的 no-op 策略（[`ExecutionStrategy`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.storage.executionstrategy) 基类，回调只跑一次），包装本身不引入额外行为，`§8` 既有事务测试用例断言不变
- **幂等性约束（新增文档要求）**：由于 execution strategy 在检测到瞬时故障时会**重新整体执行**回调（含业务 `work` 委托），`work` 委托必须对"整体重跑"安全——本 HD 范围内 `work` 委托只允许包含 Repository / `SaveChangesAsync` 等数据库写操作（本身具备幂等性质：失败的事务已回滚，重跑等价于该事务从未发生过），**禁止**在 `work` 内混入外部 I/O（发消息 / 调用外部 API / 写文件等无法被事务回滚的副作用）——该约束此前隐含成立（业务层调用惯例），本轮把它写成显式文档要求，供 HD-011 / HD-012 / 后续业务 HD 引用
- **§8 测试要求补充**：新增用例——`ExecuteInTransactionAsync` 在 InMemory Provider（无 retry 策略）下行为与包装前完全一致（回归测试，防止本轮改动破坏 HD-010 既有事务用例）；SqlServer 侧的"瞬时故障触发重试后仍提交成功"用例由 [HD-011 §8](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md) 起草时补齐（需要 SqlServer 集成测试环境，非 InMemory 单测可覆盖）

**未变项**：本轮不改变 `ExecuteInTransactionAsync` 的公共签名、返回类型、异常包装的类型与 message 格式（§4.3 BCL 对照表不变）；不改变 §1 ~ §12 除 §3.2 外的任何章节；frontmatter `status: reviewed` / `reviewers: [Inkwell]` 不变（本轮是为兼容下游 HD-011 的功能修正，非重新评估已定决策，改动范围小且有明确技术证据支撑，参照 §13.6 先例处理方式）。

**下游联动**：[HD-011](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md) 直接引用本节结论，`UseSqlServer(...)` 中启用的 `EnableRetryOnFailure` 与本节修正后的 `ExecuteInTransactionAsync` 兼容，无需在 HD-011 中重复解释包装机制。

### 13.8 2026-07-06 errata·第八轮（ADR-021 / ADR-019 2026-07-06 errata：Migration 执行策略改为 CI/CD 独立步骤）

**背景**：H3 [HD-011 SqlServer final adapter](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md) 起草期间发现「`Inkwell.WebApi` 启动时自动跑 `MigrateAsync()`」存在生产安全风险（未经独立人工审阅即对生产数据库执行 schema 变更）。该风险已提请 Owner 确认，Owner 于 2026-07-06 拍板：**应用启动不再自动执行 Migration**，Migration 改由 CI/CD pipeline（[GitHub Actions](https://github.com/features/actions)，[OQ-A007 closed §A](../../03-architecture/open-questions-arch.md) 已锁定）独立步骤执行 `dotnet ef database update`（或等价的预生成脚本 apply），在新版本 `Inkwell.WebApi` / `Inkwell.Worker` 部署之前完成；两进程启动代码均不再调用 `Database.MigrateAsync()` / `MigrationRunner` 的 Migration 分支。详见 ADR-021 2026-07-06 errata / ADR-019 2026-07-06 errata。

**改动范围**：

- **§3.5 `MigrationRunner.cs`**：职责描述拆分为 InMemory（不受影响，仍由 `Inkwell.WebApi` / `Inkwell.Worker` 启动代码自动调用 `EnsureCreatedAsync` + Seed）与 SqlServer / Postgres（两进程启动代码不再调用本类执行 `MigrateAsync()`，改由 CI/CD 独立步骤 / 其调用的命令行工具调用）两种场景；对外接口 / 输入输出 / 错误处理 / 日志要求均不变，仅「谁在什么时机调用」改变
- **§3.6 `IDbContextInitializer`**：接口签名与实现方式不变（`InitializeAsync` 仍是 Migrate 或 EnsureCreated 的统一入口），本轮不改动
- **下游 HD 同步**：[HD-011](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md) §8「Migration 执行策略」、§9 Builder DSL 示例说明、§14 决策记录、§16 开放问题（由「待确认」改为「已解决」）均同步修订

**未变项**：`InkwellSeeder.SeedAsync()` 仍在 `Inkwell.WebApi` 启动时运行（`.AutoSeedOnStartup` 开关不变），前提从「随 `MigrationRunner` 完成 Migration 后触发」改为「确认 CI/CD 已将 schema 迁移到位」；InMemory Provider `EnsureCreatedAsync()`（dev / unit test）不受影响，不涉及生产发布流程；本轮不改变 `MigrationRunner` 的公共签名、异常类型与 message 格式（§4.3 BCL 对照表不变）；frontmatter `status: reviewed` / `reviewers: [Inkwell]` 不变（本轮是 ADR 层级已拍板决策向 H3 文档的同步修订，非重新评估已定架构决策，参照 §13.6 / §13.7 先例处理方式）。

**下游联动**：[HD-011](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md) 直接引用本节结论；HD-012（Postgres final adapter，待起草）起草时应直接采用本轮已修订的策略描述，不再沿用「WebApi 启动自动 Migrate」的旧表述。
