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

> **范围切片**：本 HD 锁定 `providers/Persistence/Inkwell.Persistence.EFCore/` shared base csproj 内容——`InkwellDbContext` + `OnModelCreating`（按 mixin 自动配置）、唯一 `IPersistenceProvider` 实现 `EfCorePersistenceProvider`、`AuditingSaveChangesInterceptor`（联动三 mixin）、`InkwellSeeder`（幂等）、`MigrationRunner`、`IDbContextInitializer` 抽象、Entity / Configuration / Mapping / Repository 四子目录模板（[ADR-022](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 锁手写 Extensions 模式）、Builder DSL 共享部分、`BannedSymbols.txt`。
>
> **不**覆盖：两 final adapter csproj（`Inkwell.Persistence.EFCore.{SqlServer,Postgres}`）→ [HD-011 / HD-012](./) 各自起草；具名业务 Model + `IXxxRepository` 接口 → 各业务命名空间 HD（`Inkwell.Core.Agents` 等）起草；跨 Provider 契约测试包 → HD-013 起草。
>
> **拓扑张力**：[ADR-021 §依赖规则补充](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 锁定 EFCore family 例外——final adapter csproj 允许 ProjectReference shared base；本 HD 锁定 base 的物理边界与 ProjectReference 上游/下游。
>
> 全文术语锁定 = Entity ↔ Model；具名 Repository 动词白名单 = Add / Update / Get / Delete / List / Find（不带 `Async` 后缀）；mapper 走手写 `XxxMappingExtensions` 三方法（`ToModel` / `ToEntity` / `SelectAsModel`）；全部签名走裸 `Task<T>` + BCL 异常（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)）。
>
> **2026-07-15 替代性 errata（Conversations 三实体）**：原 §3.13 的 `AgentSessionDefinition` / `AgentSessionEntity` 把产品会话与 MAF Session 状态混为一体。当前 EFCore family 必须按 [HD-017 §0](../Inkwell.Core/HD-017-Inkwell.Core.Conversations.md#0-2026-07-15-当前契约替代下方冲突章节) 实现 `AgentConversation`、`AgentChatMessage`、`AgentSessionState` 三套 Entity / Configuration / Mapping / Repository；本 errata 只替代 Conversations 对应模板行，其余已 reviewed 内容保持不变。
>
> **2026-07-20 Session checkpoint 删除 errata**：Conversations 持久化现收敛为 `AgentConversation` 与 `AgentChatMessage` 两套实体。`AgentSessionStateEntity` 及其 Configuration、Mapping、Repository、DI 注册和导航关系已删除；双数据库 `RemoveAgentSessionState` migration 删除旧状态表。下方三实体计数和状态实体配置要求仅保留为历史设计。

## 1. 模块概述

- **Entity 集中地**（[ADR-021 D1 = A](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）：全部业务 `XxxEntity` 类、`IEntityTypeConfiguration<TEntity>` 配置 ~30 套
- **DbContext 共享 base**：`InkwellDbContext`（virtual `OnModelCreating` / `OnConfiguring`），final adapter 通过继承调整 Provider-specific 行为
- **唯一 `IPersistenceProvider` 实现**：`EfCorePersistenceProvider` 实现 [HD-002 §3.1 facade](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)
- **三 mixin 自动配置**：通过 `SaveChangesInterceptor` 与 `OnModelCreating` 反射扫描，对实现 `IHasTimestamps` / `IHasRowVersion` / `IHasOwner` 的 Entity 自动填充 / 注入 token / 加索引
- **手写 mapper 集中地**（[ADR-022 §位置](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md)）：`Mapping/<TypeName>MappingExtensions.cs` ~30 套，每个 `internal static class` 含 `ToModel` / `ToEntity` / `SelectAsModel` 三方法
- **具名 Repository 实现集中地**：`Repositories/<TypeName>Repository.cs` ~30 套，唯一实现 `Inkwell.Abstractions/Persistence/<Module>/IXxxRepository.cs` 接口
- **Seed + Migration runner**：`InkwellSeeder`（幂等 `if-not-exists` 模式） + `MigrationRunner`（[ADR-021 D2 = B](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) `AutoSeedOnStartup` 开关）
- **CI 强制 banlist**：`BannedSymbols.txt` 锁定 Repository 动词白名单 + Repository 后缀禁用 + Mapping 库零依赖

## 2. 文件结构

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

物理布局参 [file-structure.md §providers/Persistence/Inkwell.Persistence.EFCore](../file-structure.md)。

## 3. 程序文件设计（10 字段 × 13 文件）

### 3.0 Inkwell.Persistence.EFCore.csproj

- **文件路径**：`src/core/providers/Persistence/Inkwell.Persistence.EFCore/Inkwell.Persistence.EFCore.csproj`
- **职责**：声明 EFCore family shared base 的依赖与目标框架
- **对外接口**：无（csproj 配置）
- **内部函数或类**：无
- **输入数据**：MSBuild 属性
- **输出数据**：编译产物 `Inkwell.Persistence.EFCore.dll`
- **依赖模块**：
  - `Microsoft.EntityFrameworkCore` 10.x
  - `Microsoft.EntityFrameworkCore.Relational` 10.x（用于 `IsRelational()` 判定 + Migration 抽象）
  - ProjectReference `Inkwell.Abstractions`
  - `<InternalsVisibleTo>` 声明：`<ItemGroup><InternalsVisibleTo Include="Inkwell.Persistence.EFCore.SqlServer" /><InternalsVisibleTo Include="Inkwell.Persistence.EFCore.Postgres" /></ItemGroup>`——两个 final adapter csproj 借此访问本 csproj 的 `internal` 类型（`AddEfCorePersistenceBase()` / `MigrationRunner` / `EfCorePersistenceProvider` 等）
  - **禁止**：任何 DBMS 特定 Provider 包（`Microsoft.EntityFrameworkCore.SqlServer` / `Npgsql.EntityFrameworkCore.PostgreSQL` —— 留给两 final adapter csproj）
  - **禁止**：任何映射库（AutoMapper / Mapster / Riok.Mapperly —— [ADR-022](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 锁手写 Extensions）
- **错误处理**：N/A（csproj 配置错误由 dotnet build 报）
- **日志要求**：N/A
- **测试要求**：CI 在 `dotnet pack` 后断言 `.nupkg` 仅含 EFCore 抽象层 + Inkwell.Abstractions ProjectReference，不含任何 DBMS Provider 包（dotnet-list-package + grep）

### 3.1 InkwellDbContext.cs

- **文件路径**：`src/core/providers/Persistence/Inkwell.Persistence.EFCore/InkwellDbContext.cs`
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
  - 单测：通过 Testcontainers 起的 SqlServer 或 Postgres 实例断言 `Model.GetEntityTypes()` 数 = ~30、`IsRowVersion()` 字段数 = mixin 实现数
  - 覆盖率门槛 ≥ 95%

### 3.2 EfCorePersistenceProvider.cs

- **文件路径**：`src/core/providers/Persistence/Inkwell.Persistence.EFCore/EfCorePersistenceProvider.cs`
- **职责**：唯一 `IPersistenceProvider` 实现，对外暴露事务包装 + `SaveChangesAsync` + `GetRepository<TRepository>()` 具名 Repository 工厂入口（决策来源 [HD-002 Q1=A2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#135-2026-05-18-errata第五轮q1--a2-picker-落地getrepositorytrepository-泛型工厂入口) picker）
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
  - `ExecuteInTransactionAsync` 的 Begin/Commit/Rollback 三步全部包在 `db.Database.CreateExecutionStrategy().ExecuteAsync(async () => { ... })` 回调内——SqlServer 场景下（[HD-011](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md) `EnableRetryOnFailure`）该回调可能因瞬时故障重新整体执行；业务 `work` 委托必须保持对再次调用安全（幂等于数据库写操作层面，不产生数据库外部副作用），这是本 HD 对 `IPersistenceProvider.ExecuteInTransactionAsync` 调用方的既有隐含约定（纯 Repository 操作天然满足，业务 HD 起草时若在 `work` 内混入外部 I/O 需显式说明）
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

- **文件路径**：`src/core/providers/Persistence/Inkwell.Persistence.EFCore/Interceptors/AuditingSaveChangesInterceptor.cs`
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

- **文件路径**：`src/core/providers/Persistence/Inkwell.Persistence.EFCore/InkwellSeeder.cs`
- **职责**：幂等 seed 入口；启动期由 `MigrationRunner` 调；v1 落地一个具体 seed 段——默认管理员账号（`admin`）
- **对外接口**：
  - `internal sealed class InkwellSeeder(InkwellDbContext db, IOptions<PersistenceOptions> options, ILogger<InkwellSeeder> logger)`
  - `public async Task SeedAsync(CancellationToken ct = default)`
- **内部函数或类**：
  - `private async Task<int> SeedDefaultAdminAsync(CancellationToken ct)`——v1 唯一落地 seed 段，创建用户名 `admin` 的默认超级管理员账号；从 `Inkwell:Persistence:Seed:AdminPassword` 读取首次 Seed 密码（默认 `admin`），使用随机 16 字节盐和 PBKDF2-HMACSHA256 600,000 次迭代生成 `PasswordHash`，不记录明文密码
  - `private static string HashPassword(string password)`——仅使用 BCL 生成与 `Inkwell.Core.Auth.PasswordHasher` 相同格式的自描述哈希；Provider 不引用 `Inkwell.Core.Auth`，保持 [AGENTS.md §3.2](../../../AGENTS.md) 依赖边界
  - 预留其他 seed 段扩展点：`SeedSystemUserAsync` / `SeedDefaultRolesAsync` / etc.（v1 范围外，未来业务 HD 按需增量贡献）
  - **幂等模式**：每个 seed 段先查业务唯一键（如 `users.Username`） → 不存在则 `Add`；**禁**用 `Id` 主键判定（[ADR-021 §Migration/DataSeed 启动行为](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）
- **输入数据**：DI 注入的 `InkwellDbContext` + `PersistenceOptions.Seed.AdminPassword`；AppHost 通过 secret Parameter 注入，直接运行 Migrator 时可通过 `Inkwell__Persistence__Seed__AdminPassword` 覆盖
- **输出数据**：成功无返回值；失败抛 BCL 异常（`InvalidOperationException` 包 inner）
- **依赖模块**：`Microsoft.EntityFrameworkCore` / `Entities/UserEntity.cs`（[§3.7](#37-entitiesentityentitycs-模板--agententity-示例)） / Mapping（不依赖 `Common/Result.cs`）；**不**依赖 `Inkwell.Core.Auth.PasswordHasher`（跨层依赖，[AGENTS.md §3.2](../../../AGENTS.md) 禁止）
- **错误处理**：seed 段任一 throw → 包成 `new InvalidOperationException($"Seeder segment '{segmentName}' failed", inner)`（[HD-002 §4.3](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) BCL 对照表事务回滚 row），记 `LogError` 含失败段名 + `Activity.AddException(inner)` 写 OTel 五字段；不重试（启动期失败由 K8s probe 触发 pod 重启）
- **日志要求**：
  - 开始：`LogInformation("Seed begin")`
  - 每段完成：`LogInformation("Seed {SegmentName} ok inserted={NewRowCount}")`
  - 全部完成：`LogInformation("Seed done totalSegments={N} totalInserted={M} elapsed={Ms}ms")`
- **测试要求**：
  - 首次跑插数、二次跑零插数（幂等断言）、任一段 throw 包成 `InvalidOperationException`、`AutoSeedOnStartup(false)` 时 Seeder 不被注入
  - `SeedDefaultAdminAsync`：首次运行插入 `Username="admin"` 且 `IsAdmin=true` 一条记录；配置的非默认密码可通过哈希校验、默认密码不可通过；二次运行零插入（幂等断言，按 `Username` 而非 `Id` 判定）
  - 覆盖率门槛 ≥ 95%

**核心代码**：

```csharp
internal sealed class InkwellSeeder(
  InkwellDbContext db,
  IOptions<PersistenceOptions> options,
  ILogger<InkwellSeeder> logger)
{
  private static string HashPassword(string password)
    {
    byte[] salt = RandomNumberGenerator.GetBytes(16);
    byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 600_000, HashAlgorithmName.SHA256, 32);

    return $"PBKDF2$600000${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
}
```

> **安全提示**：`admin` / `admin` 仅作为本地开发默认值。部署时必须通过 Secret 设置 `Inkwell:Persistence:Seed:AdminPassword`；该配置只在 `admin` 账号不存在时生效，幂等 Seeder 不会重置已有账号密码。Seed 初始 Admin 不强制改密；通过 UI-009 创建或由 Admin 重置密码的账号设置 `MustChangePassword=true`，登录后只能完成当前用户改密，成功后才能进入工作区。

### 3.5 MigrationRunner.cs

- **文件路径**：`src/core/providers/Persistence/Inkwell.Persistence.EFCore/MigrationRunner.cs`
- **职责**：
  - **SqlServer / Postgres 场景**（integration test / prod）：`Inkwell.WebApi` / `Inkwell.Worker` 启动代码**只调用** `SeedAsync(ct)`——不调用 `MigrateAsync(ct)`（其内部触发的 `IDbContextInitializer.InitializeAsync` 已改由 CI/CD pipeline 独立步骤在部署前执行）；`MigrateAsync(ct)` 仅由 CI/CD 独立调用工具 / 集成测试环境直接调用
- **对外接口**：
  - `internal sealed class MigrationRunner(InkwellDbContext db, IDbContextInitializer initializer, IOptions<PersistenceOptions> options, InkwellSeeder seeder, ILogger<MigrationRunner> logger)`
  - `public async Task MigrateAsync(CancellationToken ct = default)` —— 仅负责 schema 初始化（Migrate / EnsureCreated），不触发 Seed
  - `public async Task SeedAsync(CancellationToken ct = default)` —— 仅负责按 `PersistenceOptions.AutoSeedOnStartup` 开关执行 Seed，不触发 schema 初始化
- **内部函数或类**：
  - `MigrateAsync(ct)`：调 `initializer.InitializeAsync(db, ct)`（final adapter 决定走 Migrate 还是 EnsureCreated，详 §3.6），全过程加 `MigrationTimeoutSeconds` 超时（默认 300s，[HD-002 §3.5 PersistenceOptions](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)）
  - `SeedAsync(ct)`：若 `PersistenceOptions.AutoSeedOnStartup` = true → 调 `seeder.SeedAsync(ct)`；否则直接返回（no-op），不加额外超时包装（超时已由 §3.4 InkwellSeeder 自身职责覆盖，本类不重复包装）
  - 两方法的调用顺序与是否调用均由调用方（`Inkwell.WebApi` / `Inkwell.Worker` 启动代码）决定，本类不再内部耦合「先 Migrate 后 Seed」的固定顺序
- **输入数据**：`PersistenceOptions`
- **输出数据**：两方法均成功无返回值；失败抛 BCL 异常
- **依赖模块**：`Microsoft.EntityFrameworkCore` / `Inkwell.Abstractions/Persistence/PersistenceOptions.cs`
- **错误处理**（[HD-002 §4.3](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) BCL 对照表）：
  - `MigrateAsync(ct)`：
    - 超时（含 `OperationCanceledException` 由内部 `CancellationTokenSource(MigrationTimeoutSeconds)` 触发）→ 抛 `new TimeoutException($"Migration timeout: {options.MigrationTimeoutSeconds}s")`
    - Migration 异常 → 抛 `new InvalidOperationException("Migration failed", inner)` 含 inner 异常 + `Activity.AddException(inner)` 写 OTel 五字段
    - 外部传入 `ct` 取消 → `OperationCanceledException` 透传
  - `SeedAsync(ct)`：
    - Seeder 失败 → 透传（已是 `InvalidOperationException` per §3.4）
    - 外部传入 `ct` 取消 → `OperationCanceledException` 透传
    - `AutoSeedOnStartup=false` → no-op，不抛异常
- **日志要求**：
  - `MigrateAsync(ct)`：开始 `LogInformation("Migration begin provider={ProviderName}")`；完成 `LogInformation("Migration ok elapsed={Ms}ms")`
  - `SeedAsync(ct)`：`AutoSeedOnStartup=true` 时开始 `LogInformation("Seed begin")`、完成 `LogInformation("Seed ok elapsed={Ms}ms")`；`AutoSeedOnStartup=false` 时不记录日志（no-op 不产生噪音）
- **测试要求**：
  - `MigrateAsync(ct)`：SqlServer / Postgres 走 Migrate 路径（用 mock `IDbContextInitializer` 做单测，真实 Migrate 行为改由 CI/CD 侧调用方 / 独立命令行工具的集成测试覆盖，不再属于 WebApi/Worker 启动集成测试范围）；超时抛 `TimeoutException`；`ct` 预取消抛 `OperationCanceledException`
  - `SeedAsync(ct)`：`AutoSeedOnStartup=true` 时调用 `seeder.SeedAsync`（mock 验证被调用一次）；`AutoSeedOnStartup=false` 时不调用 `seeder.SeedAsync`（mock 验证零调用）；Seeder 失败透传
  - 覆盖率门槛 ≥ 95%

### 3.6 IDbContextInitializer.cs

- **文件路径**：`src/core/providers/Persistence/Inkwell.Persistence.EFCore/IDbContextInitializer.cs`
- **职责**：把 final adapter 之间「Migrate vs EnsureCreated」的分歧抽到接口，base 不耦合具体 Provider 行为
- **对外接口**：
  - `public interface IDbContextInitializer { Task InitializeAsync(InkwellDbContext db, CancellationToken ct = default); }`
- **内部函数或类**：无（仅接口）
- **输入数据**：N/A
- **输出数据**：N/A
- **依赖模块**：`Microsoft.EntityFrameworkCore`
- **错误处理**：实现方自由抛；`MigrationRunner` 统一包成 005
- **日志要求**：实现方自行记
- **测试要求**：契约测试仅在 final adapter HD（HD-011/012）中验

### 3.7 Entities/&lt;Entity&gt;Entity.cs 模板 + AgentEntity 示例

- **文件路径**：`src/core/providers/Persistence/Inkwell.Persistence.EFCore/Entities/<Entity>Entity.cs`（~30 个，per-entity 一文件）
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
// src/core/providers/Persistence/Inkwell.Persistence.EFCore/Entities/AgentEntity.cs
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

**第二示例（UserEntity，遵同一模板；实现 [HD-014 §3.6 `User` Model](../Inkwell.Core/HD-014-Inkwell.Core.Auth.md#36-persistenceauthusercs)）**：

```csharp
// src/core/providers/Persistence/Inkwell.Persistence.EFCore/Entities/UserEntity.cs
namespace Inkwell.Persistence.EFCore.Entities;

internal sealed class UserEntity : IHasTimestamps
{
  public Guid Id { get; init; }
  public string Username { get; init; } = "";
  public string PasswordHash { get; init; } = "";
  public bool IsAdmin { get; init; }
  public bool IsLocked { get; init; }
  public bool IsDisabled { get; init; }
  public bool MustChangePassword { get; init; }
  public int SessionVersion { get; init; }
  public int FailedUnlockAttempts { get; init; }
  public DateTimeOffset? LastLoginTime { get; init; }

    // IHasTimestamps
  public DateTimeOffset CreatedTime { get; init; }
  public DateTimeOffset UpdatedTime { get; init; }

}
```

  > `UserEntity` 仅实现 `IHasTimestamps`，不实现 `IHasRowVersion` 或 `IHasOwner`；当前 `users` 表没有 `RowVersion` 列。

### 3.8 Configurations/&lt;Entity&gt;EntityConfiguration.cs 模板

- **文件路径**：`src/core/providers/Persistence/Inkwell.Persistence.EFCore/Configurations/<Entity>EntityConfiguration.cs`（~30 个）
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
// src/core/providers/Persistence/Inkwell.Persistence.EFCore/Configurations/AgentEntityConfiguration.cs
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

**第二示例（UserEntityConfiguration）**：

```csharp
// src/core/providers/Persistence/Inkwell.Persistence.EFCore/Configurations/UserEntityConfiguration.cs
namespace Inkwell.Persistence.EFCore.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Inkwell.Persistence.EFCore.Entities;

internal sealed class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.Username).IsUnique();      // 登录查找 + 唯一性约束（HD-014 §5）
        b.Property(x => x.Username).IsRequired().HasMaxLength(100);
        b.Property(x => x.PasswordHash).IsRequired();
        b.HasIndex(x => x.IsLocked);                  // UI-009 账号 tab 默认过滤已锁账号（HD-014 §5）
    }
}
```

> **RowVersion 并发列不在本文件配置**：由 base `InkwellDbContext.ApplyRowVersion`（[§3.1](#31-inkwelldbcontextcs)）反射扫描 `IHasRowVersion` 自动对全部实现该 mixin 的 Entity 调 `.IsRowVersion()`，与 `AgentEntityConfiguration` 完全一致——本文件不重复设置，避免与自动化扫描冲突。

### 3.9 Mapping/&lt;TypeName&gt;MappingExtensions.cs 模板 + AgentMappingExtensions 完整代码

- **文件路径**：`src/core/providers/Persistence/Inkwell.Persistence.EFCore/Mapping/<TypeName>MappingExtensions.cs`（~30 个，per-entity 一文件）
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
  - per-entity 配对 `<TypeName>MappingExtensionsTests.cs`：全字段双向 round-trip equality、null 入参抛 `ArgumentNullException`、`SelectAsModel` 与 `ToModel` 在 Testcontainers 起的实例下结果一致
  - 覆盖率门槛 ≥ 95%

**完整代码（AgentMappingExtensions，承接 [ADR-022 §模式](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md)）**：

```csharp
// src/core/providers/Persistence/Inkwell.Persistence.EFCore/Mapping/AgentMappingExtensions.cs
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

**第二示例（UserMappingExtensions，实现 [HD-014 §3.6 `User` Model](../Inkwell.Core/HD-014-Inkwell.Core.Auth.md#36-persistenceauthusercs)）**：

```csharp
// src/core/providers/Persistence/Inkwell.Persistence.EFCore/Mapping/UserMappingExtensions.cs
namespace Inkwell.Persistence.EFCore.Mapping;

internal static class UserMappingExtensions
{
    /// <summary>Entity → Model：从 EFCore 物化对象转出业务对外 Model。</summary>
    public static User ToModel(this UserEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new User
        {
            Id                   = entity.Id,
            Username             = entity.Username,
            PasswordHash         = entity.PasswordHash,
            IsAdmin              = entity.IsAdmin,
            IsLocked             = entity.IsLocked,
            IsDisabled           = entity.IsDisabled,
            MustChangePassword   = entity.MustChangePassword,
            SessionVersion       = entity.SessionVersion,
            FailedUnlockAttempts = entity.FailedUnlockAttempts,
            LastLoginTime        = entity.LastLoginTime,
            CreatedTime          = entity.CreatedTime,
            UpdatedTime          = entity.UpdatedTime,
        };
    }

    /// <summary>Model → Entity：从业务 Model 转回 EFCore Entity，用于 Add / Update。</summary>
    public static UserEntity ToEntity(this User model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return new UserEntity
        {
            Id                   = model.Id,
            Username             = model.Username,
            PasswordHash         = model.PasswordHash,
            IsAdmin              = model.IsAdmin,
            IsLocked             = model.IsLocked,
            IsDisabled           = model.IsDisabled,
            MustChangePassword   = model.MustChangePassword,
            SessionVersion       = model.SessionVersion,
            FailedUnlockAttempts = model.FailedUnlockAttempts,
            LastLoginTime        = model.LastLoginTime,
            CreatedTime          = model.CreatedTime,
            UpdatedTime          = model.UpdatedTime,
        };
    }

    /// <summary>IQueryable&lt;Entity&gt; → IQueryable&lt;Model&gt;：投影下推到 SQL（仅 SELECT 必要列）。</summary>
    public static IQueryable<User> SelectAsModel(this IQueryable<UserEntity> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Select(entity => new User
        {
            Id                   = entity.Id,
            Username             = entity.Username,
            PasswordHash         = entity.PasswordHash,
            IsAdmin              = entity.IsAdmin,
            IsLocked             = entity.IsLocked,
            IsDisabled           = entity.IsDisabled,
            MustChangePassword   = entity.MustChangePassword,
            SessionVersion       = entity.SessionVersion,
            FailedUnlockAttempts = entity.FailedUnlockAttempts,
            LastLoginTime        = entity.LastLoginTime,
            CreatedTime          = entity.CreatedTime,
            UpdatedTime          = entity.UpdatedTime,
        });
    }
}
```

### 3.10 Repositories/&lt;TypeName&gt;Repository.cs 模板 + AgentRepository 完整代码

- **文件路径**：`src/core/providers/Persistence/Inkwell.Persistence.EFCore/Repositories/<TypeName>Repository.cs`（~30 个）
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
  - 每方法至少 1 个正常路径 + 1 个失败路径 ut（在 Testcontainers 起的实例上跑；GetXxx 失败 = `await act.Should().ThrowAsync<KeyNotFoundException>().Where(e => e.Message.StartsWith("AgentDefinition not found:"))`；AddXxx 唯一冲突 = `await act.Should().ThrowAsync<InvalidOperationException>().Where(e => e.Message.StartsWith("Duplicate key:"))`）
  - 契约测试在 HD-013 跨 Provider matrix 跑
  - 覆盖率门槛 ≥ 95%

**完整代码（AgentRepository）**：

```csharp
// src/core/providers/Persistence/Inkwell.Persistence.EFCore/Repositories/AgentRepository.cs
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

**第二示例（UserRepository，实现 [HD-014 §3.7 `IUserRepository`](../Inkwell.Core/HD-014-Inkwell.Core.Auth.md#37-persistenceiuserrepositorycs) 全部 6 方法；不含 `DeleteUser`）**：

```csharp
// src/core/providers/Persistence/Inkwell.Persistence.EFCore/Repositories/UserRepository.cs
namespace Inkwell.Persistence.EFCore.Repositories;

using Inkwell.Persistence.EFCore.Mapping;

internal sealed class UserRepository(InkwellDbContext db) : IUserRepository
{
    public async Task<User> AddUser(User user, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        try
        {
          UserEntity entity = user.ToEntity();
            db.Set<UserEntity>().Add(entity);
          await db.SaveChangesAsync(ct).ConfigureAwait(false);

            return entity.ToModel();
        }
        catch (DbUpdateException ex)
        {
          throw new InvalidOperationException($"Duplicate key: Username={user.Username}", ex);
        }
    }

    public async Task UpdateUser(User user, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        db.Set<UserEntity>().Update(user.ToEntity());
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<User> GetUser(Guid id, CancellationToken ct = default)
    {
        UserEntity? entity = await db.Set<UserEntity>().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false);

        return entity?.ToModel() ?? throw new KeyNotFoundException($"User not found: id={id}");
    }

    public async Task<User> GetUserByUsername(string username, CancellationToken ct = default)
    {
        UserEntity? entity = await db.Set<UserEntity>().AsNoTracking().FirstOrDefaultAsync(x => x.Username == username, ct).ConfigureAwait(false);

        return entity?.ToModel() ?? throw new KeyNotFoundException($"User not found: username={username}");
    }

    public async Task<PagedResult<User>> ListUsers(Pagination pagination, SortOrder sort, CancellationToken ct = default)
    {
        IOrderedQueryable<UserEntity> query = db.Set<UserEntity>().AsNoTracking().ApplySort(sort, FieldSelector);
        long total = await query.LongCountAsync(ct).ConfigureAwait(false);
        List<User> items = await query.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize).SelectAsModel().ToListAsync(ct).ConfigureAwait(false);

        return new PagedResult<User>(items, total, pagination);
    }

    public async Task<IReadOnlyList<User>> FindUsersByLockedStatus(bool isLocked, CancellationToken ct = default) =>
      await db.Set<UserEntity>().AsNoTracking().Where(x => x.IsLocked == isLocked).SelectAsModel().ToListAsync(ct).ConfigureAwait(false);

    private static System.Linq.Expressions.Expression<Func<UserEntity, object?>> FieldSelector(string field) => field switch
    {
      nameof(UserEntity.Username) => x => x.Username,
      nameof(UserEntity.UpdatedTime) => x => x.UpdatedTime,
      _ => x => x.CreatedTime,
    };
}
```

### 3.11 DependencyInjection/InkwellPersistenceEfCoreServiceCollectionExtensions.cs

- **文件路径**：`src/core/providers/Persistence/Inkwell.Persistence.EFCore/DependencyInjection/InkwellPersistenceEfCoreServiceCollectionExtensions.cs`
- **职责**：注册 base 服务（`EfCorePersistenceProvider` / `AuditingSaveChangesInterceptor` / `InkwellSeeder` / `MigrationRunner` / 全部 `Repositories/<TypeName>Repository`）；不绑定 Provider（final adapter csproj 各自 `Use*` 扩展方法注册 `DbContext` 选项 + `IDbContextInitializer`）
- **对外接口**：
  - `internal static class InkwellPersistenceEfCoreServiceCollectionExtensions`
  - `internal static IServiceCollection AddEfCorePersistenceBase(this IServiceCollection services)` —— internal（final adapter csproj 通过 `InternalsVisibleTo` 调用）
- **内部函数或类**：无
- **输入数据**：`IServiceCollection`
- **输出数据**：副作用（注册服务）
- **依赖模块**：`Microsoft.Extensions.DependencyInjection` / [`Microsoft.Extensions.Options.ConfigurationExtensions`](https://www.nuget.org/packages/Microsoft.Extensions.Options.ConfigurationExtensions/)（`AddOptions<T>().BindConfiguration()`） / `Inkwell.Abstractions/Persistence/PersistenceOptions.cs` / 本 csproj 内全部类型
- **错误处理**：重复注册（同 service type 两次）→ EFCore DI 自然容忍后注册覆盖；不主动检测
- **日志要求**：N/A（注册期）
- **测试要求**：单测在测试 host 中调 `AddEfCorePersistenceBase()` + 至少一个 `UseXxxDatabase`（来自 final adapter），断言可 `BuildServiceProvider().GetRequiredService<IPersistenceProvider>()` 解到 `EfCorePersistenceProvider`；覆盖率门槛 ≥ 95%

**完整代码**：

```csharp
// src/core/providers/Persistence/Inkwell.Persistence.EFCore/DependencyInjection/InkwellPersistenceEfCoreServiceCollectionExtensions.cs
namespace Inkwell.Persistence.EFCore.DependencyInjection;

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Inkwell.Abstractions.Persistence;
using Inkwell.Abstractions.Persistence.Agents;
using Inkwell.Abstractions.Persistence.Auth;
using Inkwell.Persistence.EFCore;
using Inkwell.Persistence.EFCore.Interceptors;
using Inkwell.Persistence.EFCore.Repositories;

internal static class InkwellPersistenceEfCoreServiceCollectionExtensions
{
    internal static IServiceCollection AddEfCorePersistenceBase(this IServiceCollection services)
    {
        services.AddOptions<PersistenceOptions>().BindConfiguration("Inkwell:Persistence");

        services.AddScoped<IPersistenceProvider, EfCorePersistenceProvider>();
        services.AddSingleton<ISaveChangesInterceptor, AuditingSaveChangesInterceptor>();
        services.AddScoped<InkwellSeeder>();
        services.AddScoped<MigrationRunner>();

        // 18 个业务实体的具名 Repository：以 AgentRepository / UserRepository 为例，其余 16 个同构注册
        services.AddScoped<IAgentRepository, AgentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        // ... 其余 16 个 Repositories/<TypeName>Repository 按同一模式注册（省略，模板见 §3.10）

        return services;
    }
}
```

> **关键点**：`AuditingSaveChangesInterceptor` 必须以 `ISaveChangesInterceptor`（接口）为服务类型注册——`AddSingleton<ISaveChangesInterceptor, AuditingSaveChangesInterceptor>()`，而非 `AddSingleton<AuditingSaveChangesInterceptor>()`（服务类型=具体类）。final adapter（HD-011 / HD-012）的 `AddDbContext<InkwellDbContext>` 配置里用 `.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>())` 按接口服务类型汇总全部拦截器；若注册成具体类，`GetServices<ISaveChangesInterceptor>()` 不会返回它，拦截器永不执行（[HD-010 首轮评审 B16/C96](../design-review-report.md#b16inmemoryrowversioninterceptor-以错误的-di-服务类型注册导致rowversion-拦截器永不执行c96) 已实证此类错误的后果；HD-010 现已移除，但该历史评审记录作为事实予以保留）。
>
> **配置绑定顺序说明**：`services.AddOptions<PersistenceOptions>().BindConfiguration("Inkwell:Persistence")` 必须先于各 final adapter 自身的 `services.Configure<PersistenceOptions>(...)` 调用注册（`AddEfCorePersistenceBase()` 是 [HD-011 §3.1 `UseSqlServer(...)`](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#31-dependencyinjectioninkwellpersistenceefcoresqlserverservicecollectionextensionscs) 方法体第一行调用）。[.NET Options 模式](https://learn.microsoft.com/dotnet/core/extensions/options) 按**注册顺序**依次对同一个选项实例执行全部 `Configure` 委托，每个委托只覆盖它显式触碰的字段，不会重置其余已绑定的字段；因此 final adapter 之后再调 `Configure<PersistenceOptions>(o => o.ConnectionString = connectionString)` 只覆盖 `ConnectionString` 一个字段，`CommandTimeoutSeconds` 等其余字段仍保留本行绑定的配置值，二者不冲突。

### 3.12 BannedSymbols.txt

- **文件路径**：`src/core/providers/Persistence/Inkwell.Persistence.EFCore/BannedSymbols.txt`
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

<!-- markdownlint-disable MD060 -->

| 业务模块               | Model                      | Entity                    | 表名                   | REQ               |
| ---------------------- | -------------------------- | ------------------------- | ---------------------- | ----------------- |
| Inkwell.Auth           | `User`                     | `UserEntity`              | `users`                | REQ-001           |
| Inkwell.Auth           | `PublicApiToken`           | `PublicApiTokenEntity`    | `public_api_tokens`    | ADR-007           |
| Inkwell.Agents         | `AgentDefinition` (撞名)   | `AgentEntity`             | `agents`               | REQ-002           |
| Inkwell.Versioning     | `AgentVersion`             | `AgentVersionEntity`      | `agent_versions`       | REQ-002 + REQ-015 |
| Inkwell.Tools          | `ToolDefinition` (撞名)    | `ToolEntity`              | `agent_tools`          | REQ-007           |
| Inkwell.Skills         | `SkillDefinition` (撞名)   | `SkillEntity`             | `agent_skills`         | REQ-008           |
| Inkwell.Triggers       | `TriggerDefinition` (撞名) | `TriggerEntity`           | `triggers`             | REQ-011           |
| Inkwell.KnowledgeBase  | `KnowledgeBase`            | `KnowledgeBaseEntity`     | `knowledge_bases`      | REQ-009           |
| Inkwell.KnowledgeBase  | `KbDocument`               | `KbDocumentEntity`        | `kb_documents`         | REQ-009           |
| Inkwell.KnowledgeBase  | `KbChunk`                  | `KbChunkEntity`           | `kb_chunks`            | REQ-009           |
| Inkwell.Memory         | `MemoryItem`               | `MemoryItemEntity`        | `memory_items`         | REQ-010           |
| Inkwell.Orchestrations | `Orchestration`            | `OrchestrationEntity`     | `orchestrations`       | REQ-012           |
| Inkwell.Orchestrations | `OrchestrationRun`         | `OrchestrationRunEntity`  | `orchestration_runs`   | REQ-012           |
| Inkwell.Conversations  | `AgentConversation`        | `AgentConversationEntity` | `agent_conversations`  | REQ-010 + NFR-005 |
| Inkwell.Conversations  | `AgentChatMessage`         | `AgentChatMessageEntity`  | `agent_chat_messages`  | REQ-010 + NFR-005 |
| Inkwell.Conversations  | `AgentSessionState`        | `AgentSessionStateEntity` | `agent_session_states` | REQ-010 + NFR-005 |
| Inkwell.Conversations  | `AguiRunEvent`             | `AguiRunEventEntity`      | `agui_run_events`      | ADR-011 + ADR-012 |
| Inkwell.Traces         | `Trace`                    | `TraceEntity`             | `traces`               | REQ-014           |

<!-- markdownlint-enable MD060 -->

> **2026-07-15 计数 errata**：Conversations 从 2 个实体调整为 3 个后，本表共 18 个业务实体 × 4 类文件 = 72 个文件；加上 §3.0 ~ §3.6、§3.11、§3.12 共 8 个 base / DI / CI 文件 = base csproj 总计 80 个 `*.cs` + 1 个 `BannedSymbols.txt` + 1 个 `.csproj`。`AgentConversationEntityConfiguration` 配置消息与状态 `ON DELETE CASCADE`、版本外键 `RESTRICT`、Run 租约 CHECK；`AgentChatMessageEntityConfiguration` 配置会话序号唯一索引与 Run 幂等过滤/部分唯一索引；`AgentSessionStateEntityConfiguration` 配置 `ConversationId` 同时为 PK/FK。

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
| Migration 失败                            | `InvalidOperationException`                                                                                      | `"Migration failed"`，inner = 原异常                                                      | `MigrationRunner.MigrateAsync`（§3.5）                                                                                                         |
| Migration 超时                            | `TimeoutException`                                                                                               | `"Migration timeout: <MigrationTimeoutSeconds>s"`                                         | `MigrationRunner.MigrateAsync`（§3.5）                                                                                                         |
| Seeder 段失败                             | `InvalidOperationException`                                                                                      | `"Seeder segment '<segmentName>' failed"`，inner = 原异常                                 | `InkwellSeeder.SeedAsync`（§3.4）                                                                                                              |
| `EnableSensitiveDataLogging=true` in prod | `InkwellConfigurationException`                                                                                  | `"EnableSensitiveDataLogging=true is forbidden in prod"`                                  | DI 启动期校验（§7）；[HD-001 §3.3](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#33-commoninkwellexceptioncs) 保留子类之一 |
| 取消                                      | [`OperationCanceledException`](https://learn.microsoft.com/dotnet/api/system.operationcanceledexception)         | 透传不包装（遵 BCL 惯例）                                                                 | 全部路径                                                                                                                                       |

**OTel `exception.*` 五字段集中写入**（[HD-002 §4.4](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) + [OTel exception attribute registry](https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/)）：所有 catch 块在 rethrow / wrap-and-throw 前调 `Activity.SetStatus(ActivityStatusCode.Error)` + `Activity.AddException(ex)` 一次性写入 `exception.type` / `.message` / `.stacktrace` / `.escaped` / `.id` 五字段；Repository / Interceptor / Seeder / MigrationRunner 均不重复写入。

## 5. 拓扑约束（落 [ADR-017 §3.2](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) + [ADR-021 §依赖规则补充](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）

- **`Inkwell.Persistence.EFCore` csproj 依赖**：
  - 允许：`Microsoft.EntityFrameworkCore` / `.Relational` / `Microsoft.CodeAnalysis.BannedApiAnalyzers`（DevelopmentDependency） + ProjectReference `Inkwell.Abstractions`
  - 禁止：`Microsoft.EntityFrameworkCore.{SqlServer,Postgres}` / `Npgsql.*` / `Microsoft.Agents.AI.*` / 任何映射库
- **final adapter 反向引入**：`Inkwell.Persistence.EFCore.{SqlServer,Postgres}` 通过 ProjectReference 引本 csproj（[ADR-021](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) family 例外）
- **`Inkwell.WebApi` / `Inkwell.Worker` 装配期**：DI 通过 `AddInkwell().UseSqlServer(...)` 等 fluent 调用注册 final adapter；自动级联本 csproj 的 `AddEfCorePersistenceBase()`
- **业务命名空间**：永不见 `XxxEntity` / `InkwellDbContext` / `EfCorePersistenceProvider`；仅见 `Inkwell.Abstractions/Persistence/<Module>/<TypeName>.cs` Model + `IXxxRepository` 接口

## 6. Builder DSL 衔接（[ADR-021 Builder DSL 形状](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）

本 csproj 不直接定义 `Use*` 扩展方法；`UseSqlServer` / `UsePostgres` 由 final adapter csproj（HD-011 / HD-012）提供。本 HD 锁定它们必须满足的契约：

1. 调用方先调 `AddInkwell()`（返 `IInkwellBuilder`，[HD-001 §3.4](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)）
2. 调用方在该 builder 上调 `.UseXxxDatabase(connectionString)`
3. final adapter 扩展方法内部调 `services.AddEfCorePersistenceBase()`（本 csproj）+ `services.AddDbContext<InkwellDbContext>(...)` + `services.AddSingleton<IDbContextInitializer, XxxDbContextInitializer>()`
4. `.AutoSeedOnStartup(bool)` 扩展由本 csproj 提供（返 `IInkwellBuilder`），调整 `PersistenceOptions.AutoSeedOnStartup`

## 7. 配置项

承接 [HD-002 §3.5 PersistenceOptions](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)。本 HD 不引入新配置字段，仅消费现有字段：

| 字段                                       | 用途（本 HD）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| ------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ConnectionString`                         | 由 final adapter `UseXxx(connectionString)` 透传到 `DbContextOptionsBuilder`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| `CommandTimeoutSeconds`（默认 30）         | `DbContextOptionsBuilder.CommandTimeout(...)` 设置                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| `MigrationTimeoutSeconds`（默认 300）      | `MigrationRunner.MigrateAsync` 内部 `CancellationTokenSource` 超时                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| `AutoSeedOnStartup`（默认 true）           | `MigrationRunner` 是否调 `InkwellSeeder.SeedAsync()`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| `EnableSensitiveDataLogging`（默认 false） | `DbContextOptionsBuilder.EnableSensitiveDataLogging(...)`；prod 启用 → 启动期 fail-fast 抛 `new InkwellConfigurationException("EnableSensitiveDataLogging=true is forbidden in prod")`（[HD-002 §7](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) + [HD-001 §3.3](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#33-commoninkwellexceptioncs) 保留子类之一；[ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流)） |
| `EnableDetailedErrors`（默认 false）       | `DbContextOptionsBuilder.EnableDetailedErrors(...)`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     |

## 8. 测试要求

### 8.1 测试 csproj 拓扑

- 测试项目：`tests/core/providers/Persistence/Inkwell.Persistence.EFCore.Tests/`（[MSTest.Sdk 4.x](https://github.com/microsoft/testfx) + MTP runner）
- 跨 Provider 行为契约用例**不放此**——见 §8.3 与 HD-013

### 8.2 单测分组

- **`InkwellDbContextTests.cs`**：`OnModelCreating` 应用三 mixin 后 model 形态校验
- **`EfCorePersistenceProviderTests.cs`**：`ExecuteInTransactionAsync` 五大路径（正常 / 显式回滚 / 异常回滚 / UnitOfWork 跨边界 / 并发冲突）
- **`AuditingSaveChangesInterceptorTests.cs`**：Added / Modified 时间戳行为 / `IHasOwner` 校验
- **`InkwellSeederTests.cs`**：幂等性双跑、段失败包成 006
- **`MigrationRunnerTests.cs`**：超时 / Migrate 路径、`AutoSeedOnStartup` 开关
- **`Mapping/<TypeName>MappingExtensionsTests.cs`** × ~30：每个 mapping 配 round-trip / null 守护 / `SelectAsModel` 一致性
- **`Repositories/<TypeName>RepositoryTests.cs`** × ~30：每个 Repository 6 动词的正常 + 失败路径

### 8.3 跨 Provider 行为契约测试（前置 HD-011 + HD-012 起草后启动）

- 公共契约用例包：`tests/core/Inkwell.Providers.Contract/Persistence/`（[RISK-002 + RISK-011](../../03-architecture/risk-analysis.md)）
- CI matrix：SqlServer + Postgres 两套 Provider（通过 Testcontainers 起真实实例）跑同一套契约用例
- 用例覆盖：CRUD 基本流 / 并发冲突（`IHasRowVersion`）/ 事务回滚 / 命令超时 / DataSeed 幂等 / Migration 启动 / `SelectAsModel` 翻译为 SQL（[Logging.QueryExecutionFailed](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.diagnostics.coreeventid.queryexecutionfailed) 不命中即视为 client-eval fallback）

### 8.4 覆盖率门槛

- 全 csproj line coverage ≥ 95%（CI threshold；coverlet + ReportGenerator + `dotnet test --collect:"XPlat Code Coverage"`）
- Mapping 扩展个体 line coverage ≥ 95%（即每个 `<TypeName>MappingExtensionsTests.cs` 覆盖配对 `<TypeName>MappingExtensions.cs` ≥ 95%）

## 9. 部署 / 配置

无独立部署单元——本 csproj 是 library，不产 Docker image。配置依赖 final adapter 注入（HD-011 / HD-012）。dev `docker-compose` 与 prod K8s 部署形态见 [ADR-005](../../03-architecture/adr/ADR-005-deployment-docker-compose-aks.md)。

## 10. 自动化检查命令（落 [ADR-022 §迁移路径 自动化检查命令](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md)）

CI（GitHub Actions）在 `dotnet build` 之后执行下列检查，任一失败即 fail PR：

```bash
ROOT=src/core/providers/Persistence/Inkwell.Persistence.EFCore

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

# C8：providers/Persistence/Inkwell.Persistence.EFCore 不引用 Microsoft.Agents.AI 或 DBMS 特定包
grep -rEn 'using Microsoft\.Agents\.AI' "$ROOT/" && exit 1
grep -rEn '<PackageReference.*Microsoft\.EntityFrameworkCore\.SqlServer' "$ROOT/Inkwell.Persistence.EFCore.csproj" && exit 1
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

| 字段                        | 选定值                                                               | 决策来源                                                                                                                                                                                                                    | 证据                                                                                                                |
| --------------------------- | -------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| EFCore base csproj 层位     | A 独立 csproj 与两 final adapter 平级                                | [ADR-021 D1](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) picker 2026-05-10                                                                                              | base 集中 + 共享 OnModelCreating                                                                                    |
| AutoSeed 默认值             | true（通过 `.AutoSeedOnStartup(false)` 关）                          | [ADR-021 D2](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)                                                                                                                | dev 启动便利                                                                                                        |
| Migration 物理位置          | A SqlServer / Postgres 各自 `Migrations/`                            | [ADR-021 D3](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)                                                                                                                | 两 Provider 均走真实 Migration，dev/测试改用 Testcontainers 真实实例（2026-07-08 InMemory 移除）                    |
| Mapper 选型                 | 手写 Extensions（`ToModel` / `ToEntity` / `SelectAsModel`）          | [ADR-022 §决策](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) picker 2026-05-11                                                                                                                      | 零依赖 / AOT 友好 / trace 直达源行                                                                                  |
| Mapping 可见性              | `internal static class`                                              | [ADR-022 §约束](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md)                                                                                                                                        | 业务命名空间不直调 mapper                                                                                           |
| Repository 动词集           | Add / Update / Get / Delete / List / Find                            | [ADR-022 §决策 Owner 第二轮](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) + [HD-002 §4.1.3](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)                                | 拒 Query/Fetch/Retrieve 词汇漂移                                                                                    |
| Repository 方法 Async 后缀  | **不带**（具名 Repo 例外）                                           | [HD-002 §4.2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) errata 2026-05-11                                                                                                                    | 6 动词已足够清晰                                                                                                    |
| `IRepository` 形态          | 零成员 marker interface                                              | [HD-002 §3.2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 2026-05-11 errata B 路径                                                                                                             | 拒泛型 CRUD 在业务层退化                                                                                            |
| SaveChanges 联动 mixin 时机 | `SaveChangesInterceptor`（启动期注册）                               | 本 HD §3.3                                                                                                                                                                                                                  | 中心化 / Entity POCO 不带逻辑                                                                                       |
| BannedSymbols 落地          | `BannedSymbols.txt` + Roslyn analyzer（RISK-018 占位）               | 本 HD §3.12                                                                                                                                                                                                                 | CI 强制                                                                                                             |
| 端口签名形态                | 裸 `Task<T>` / `Task<int>` / `Task` / `Task<bool>`（无 Result 包装） | [ADR-023 主决策](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) accepted by Inkwell 2026-05-11                                                                                              | 与 .NET BCL / EF Core / ASP.NET Core 主流 SDK 一致；§3.2 / §3.4 / §3.5 / §3.10 同步翻                               |
| 错误传递机制                | BCL 异常透传 + 包装 + 5-field OTel exception.*                       | [ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) accepted by Inkwell 2026-05-11                                  | `DbUpdateConcurrencyException` 等 EFCore 异常转业务语义后透传 / 包装为 `InvalidOperationException`；§4.3 BCL 对照表 |
| 错误码废除                  | 无 `INK-PERSIST-NNN` / 无 `ErrorCodes.Persist` 静态类                | [ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) accepted by Inkwell 2026-05-11                                  | message 前缀 + inner 异常 = 错误标识；§10 C10 grep 强制                                                             |
| Result/Error 抽象删除       | HD-009 范围内零 `Result<T>` / `Error` 引用                           | [ADR-023 errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) accepted by Inkwell 2026-05-11 | AgentRepository 6 方法全裸 `Task<T>`；§10 C11 grep 强制；§3.10 完整代码                                             |

> **注**：`IPersistenceProvider` 增 `GetRepository<TRepository>()` 泛型工厂入口；`EfCorePersistenceProvider` 委托 `IServiceProvider.GetRequiredService<TRepository>()`。决策来源 [HD-002 Q1=A2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#135-2026-05-18-errata第五轮q1--a2-picker-落地getrepositorytrepository-泛型工厂入口) picker 2026-05-18（[design-review-report §13.2](../design-review-report.md)）；签名与 §3.2 / §3.3 `IUnitOfWork.GetRepository<T>` 对齐；具名 `IXxxRepository` 由 `AddEfCorePersistenceBase()` 注册、工厂解析。

## 12. 待补 / 后续 HD 衔接

- **业务 Model + 具名 `IXxxRepository`**：由各业务命名空间 HD（`Inkwell.Core.Agents` / 等）在 `Inkwell.Abstractions/Persistence/<Module>/` 起草；本 HD 仅锁形态与接口契约，不锁字段细节
- **18 业务实体 × 4 类文件 = 72 个具体文件**：起草动作走 H5 批量编码任务（按模块拆 18 个 task brief，每个 task brief 含 4 文件 + 配对测试）
- **HD-011 / HD-012**：final adapter 两 csproj 各自起草（`UseSqlServer` / `UsePostgres`、`Migrations/` 子目录、Provider-specific OnModelCreating override、`IDbContextInitializer` 实现）
- **HD-013 跨 Provider 契约测试包**：`tests/core/Inkwell.Providers.Contract/Persistence/` 起草（覆盖 §8.3 全部用例 + §10 CI matrix）
- **`MissingMixinFieldAnalyzer`** + **Model 后缀装饰 analyzer** + **Microsoft.Agents.AI 命名空间隔离 analyzer**：自定义 Roslyn analyzer 起草——[RISK-018](../../03-architecture/risk-analysis.md) 占位的后续任务
- **`scripts/ci/hd-009-checks.sh`**：§10 自动化检查脚本物化（H5 编码任务）

## 13. 同步追加跨模块文件

- [`docs/04-detailed-design/file-structure.md`](../file-structure.md) — 本 HD 同会话追加：`providers/Persistence/Inkwell.Persistence.EFCore/` 子目录已在 [HD-002 同步追加](../file-structure.md) 中预占位；本 HD 在该节后追加"HD-009 文件细化"段，含 base 8 文件 + 18 业务实体 × 4 类文件清单
- [`docs/04-detailed-design/database-design.md`](../database-design.md) — 本 HD 同会话追加：`## providers/Persistence/Inkwell.Persistence.EFCore（EFCore base 实现）` 章节，含三 mixin 自动配置规则、SaveChangesInterceptor 行为、AutoSeed 幂等模式、跨 Provider 字段映射策略
