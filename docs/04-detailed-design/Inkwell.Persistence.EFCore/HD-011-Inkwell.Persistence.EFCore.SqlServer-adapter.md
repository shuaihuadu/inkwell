---
id: HD-011
title: Inkwell.Persistence.EFCore.SqlServer 详细设计 - final adapter（integration test / prod 候选 Provider 之一）
stage: H3
status: reviewed
reviewers: [Inkwell]
upstream:
  - REQ-002
  - REQ-006
  - REQ-009
  - REQ-013
  - REQ-014
  - ADR-004
  - ADR-013
  - ADR-017
  - ADR-019
  - ADR-021
  - ADR-023
  - HD-001
  - HD-002
  - HD-009
downstream: []
---

> **范围切片**：本 HD 锁定 `providers/Persistence/Inkwell.Persistence.EFCore.SqlServer/` final adapter csproj——[`Microsoft.EntityFrameworkCore.SqlServer`](https://learn.microsoft.com/ef/core/providers/sql-server/) 的 `DbContextOptionsBuilder` 配置、Builder DSL 入口 `UseSqlServer(connectionString, configure?)`（签名已由 [HD-002 §6](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#6-builder-dsl-衔接hd-001-63) 锁定，本 HD 只实现不改签名）、`IDbContextInitializer` 的 `MigrateAsync` 实现、SqlServer 专属 `SqlServerPersistenceOptions`（连接重试参数）、Provider 自有 `Migrations/` 目录约定。
>
> **不**覆盖：`Inkwell.Persistence.EFCore` shared base 的任何内容（Entity / Mapping / Repository / `EfCorePersistenceProvider` / `AuditingSaveChangesInterceptor` / `InkwellSeeder` / `MigrationRunner` 均已由 [HD-009](HD-009-Inkwell.Persistence.EFCore-base.md) 锁定，本 HD 只消费不重复）；Postgres final adapter → HD-012（待起草）；跨 Provider 契约测试 → HD-013（待起草）。
>
> **拓扑依据**：[ADR-021 §依赖规则补充](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 锁定 `Inkwell.Persistence.EFCore.SqlServer` **允许** ProjectReference 同 `providers/` 下的 `Inkwell.Persistence.EFCore`（shared base，EFCore family 例外）+ `Inkwell.Abstractions`；**禁止**引用 `Inkwell.Core` 或其他 provider 家族 csproj（[ADR-017 §3.2](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md)）。
>
> **跨 HD 前置修正（2026-07-06，Owner picker 授权）**：起草本 HD 期间发现 §5 `EnableRetryOnFailure` 与 [HD-009 §3.2](HD-009-Inkwell.Persistence.EFCore-base.md#32-efcorepersistenceprovidercs) `ExecuteInTransactionAsync` 手动事务管理运行时不兼容（[EF Core 官方约束](https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency#execution-strategies-and-transactions)）；已在 [HD-009 §13.7 errata·第七轮](HD-009-Inkwell.Persistence.EFCore-base.md#137-2026-07-06-errata第七轮hd-011-起草期发现executeintransactionasync-包-createexecutionstrategy-以兼容-sqlserver-enableretryonfailure) 同步修正（`ExecuteInTransactionAsync` 改用 `CreateExecutionStrategy().ExecuteAsync` 包装），本 HD 直接消费修正后的行为，不重复解释机制。
>
> **2026-07-06 errata（Migration 执行策略改为 CI/CD 独立步骤）**：本 HD 起草期发现"应用启动时自动 `MigrateAsync()`"存在生产安全风险，已提请 Owner 确认；Owner 于 2026-07-06 拍板改为 CI/CD 独立步骤执行，[ADR-021](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) / [ADR-019](../../03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md) 已同步 errata；[HD-009](HD-009-Inkwell.Persistence.EFCore-base.md) §13.8 已同步修订。本 HD §3.3 `SqlServerDbContextInitializer.MigrateAsync` 实现本身不变，变化在于"由谁在何时调用"——详见 §8。
>
> **2026-07-06 errata·第二轮（design-review-report.md §20 首轮评审 B18/C103 + B19/C104 + N31/C107 + N32，Owner picker 选项 1 / 顺手处理）**：(1) §8 修订为"SqlServer 启动代码只调 `MigrationRunner.SeedAsync(ct)`，不调 `MigrationRunner.MigrateAsync(ct)`"的准确描述（原 §8 遗留"Seed 仍在 WebApi 启动时运行"的无条件表述与"不再自动执行 Migration"直接矛盾，详 [HD-009 §13.9](HD-009-Inkwell.Persistence.EFCore-base.md)）；(2) §3.3 两处 `MigrationRunner.RunAsync` 引用同步改为 `MigrationRunner.MigrateAsync`；(3) §3.1 补一条"appsettings.json 设置 `CommandTimeoutSeconds` 后生效"测试要求（详 [HD-009 §13.10](HD-009-Inkwell.Persistence.EFCore-base.md)）；(4) §3.0 依赖 [HD-009 §3.0 新增 `InternalsVisibleTo` 声明](HD-009-Inkwell.Persistence.EFCore-base.md)（N31，无需本 HD 自身改动）；(5) §12 补一句可观测性 deferral 措辞（N32）。

## 1. 模块概述

- **DbContext 配置**：把 `InkwellDbContext`（[HD-009 §3.1](HD-009-Inkwell.Persistence.EFCore-base.md#31-inkwelldbcontextcs)，base 类型，本 HD **不**创建子类，理由见 §6）接到 [`UseSqlServer(connectionString, sqlServerOptionsAction)`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.sqlserverdbcontextoptionsextensions.usesqlserver)
- **Builder DSL 入口**：`UseSqlServer(this IInkwellBuilder builder, string connectionString, Action<PersistenceOptions>? configure = null)`——签名由 [HD-002 §6](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#6-builder-dsl-衔接hd-001-63) 与 [ADR-021 §Builder DSL 形状](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 两处预先锁定，本 HD 只落地实现
- **Migration 策略实现**：实现 [HD-009 §3.6 `IDbContextInitializer`](HD-009-Inkwell.Persistence.EFCore-base.md#36-idbcontextinitializercs) → `MigrateAsync`（[ADR-021 D3](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 锁定 SqlServer / Postgres 各自 `Migrations/`）
- **连接重试策略**：[`EnableRetryOnFailure`](https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency)——应对 SQL Server 瞬时故障（网络抖动 / 主从切换 / 限流），参数经 `SqlServerPersistenceOptions` 可配（详 §5）
- **RowVersion 原生支持**：SqlServer 对 `.IsRowVersion()`（[HD-009 §3.1 `ApplyRowVersion`](HD-009-Inkwell.Persistence.EFCore-base.md#31-inkwelldbcontextcs)）提供**原生数据库自动生成**的并发令牌（[EF Core 官方「Native database-generated concurrency tokens」](https://learn.microsoft.com/ef/core/saving/concurrency#native-database-generated-concurrency-tokens)）——本 HD **不需要**任何 `SaveChangesInterceptor` 手动模拟 RowVersion 递增（详 §4）

## 2. 文件结构

- `Inkwell.Persistence.EFCore.SqlServer.csproj`（csproj）——详 §3.0
- `DependencyInjection/InkwellPersistenceEfCoreSqlServerServiceCollectionExtensions.cs`（DI）——详 §3.1
- `SqlServerPersistenceOptions.cs`（配置）——详 §3.2
- `SqlServerDbContextInitializer.cs`（适配）——详 §3.3
- `Migrations/`（目录，无 H3 阶段代码——详 §7）

物理布局参 [file-structure.md §providers/Persistence/Inkwell.Persistence.EFCore.SqlServer](../file-structure.md)。

## 3. 程序文件设计

### 3.0 Inkwell.Persistence.EFCore.SqlServer.csproj

- **文件路径**：`src/core/providers/Persistence/Inkwell.Persistence.EFCore.SqlServer/Inkwell.Persistence.EFCore.SqlServer.csproj`
- **职责**：声明 SqlServer final adapter 的依赖与目标框架 + Migration tooling 配置
- **对外接口**：无（csproj 配置）
- **内部函数或类**：无
- **输入数据**：MSBuild 属性
- **输出数据**：编译产物 `Inkwell.Persistence.EFCore.SqlServer.dll`
- **依赖模块**：
  - [`Microsoft.EntityFrameworkCore.SqlServer`](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer/) 10.x
  - [`Microsoft.EntityFrameworkCore.Design`](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Design/) 10.x（`PrivateAssets="all"`，仅 `dotnet ef migrations add` 工具期依赖，不随运行时发布）
  - ProjectReference `Inkwell.Persistence.EFCore`（shared base，[ADR-021 family 例外](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）
  - ProjectReference `Inkwell.Abstractions`
  - **禁止**：`Microsoft.EntityFrameworkCore.InMemory` / `Npgsql.EntityFrameworkCore.PostgreSQL` / ProjectReference `Inkwell.Core`（[ADR-017 §3.2](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md)）
- **错误处理**：N/A（csproj 配置错误由 dotnet build 报）
- **日志要求**：N/A
- **测试要求**：CI 在 `dotnet pack` 后断言 `.nupkg` 运行时依赖仅含 `Inkwell.Persistence.EFCore` + `Inkwell.Abstractions` + `Microsoft.EntityFrameworkCore.SqlServer`，不含 InMemory/Npgsql/`Inkwell.Core`；`Microsoft.EntityFrameworkCore.Design` 仅出现在 build-time 依赖（`PrivateAssets`），不进最终运行时依赖树（详 §10 C1/C2）

### 3.1 DependencyInjection/InkwellPersistenceEfCoreSqlServerServiceCollectionExtensions.cs

- **文件路径**：`src/core/providers/Persistence/Inkwell.Persistence.EFCore.SqlServer/DependencyInjection/InkwellPersistenceEfCoreSqlServerServiceCollectionExtensions.cs`
- **职责**：`IInkwellBuilder` 的唯一入口扩展——注册 SqlServer `DbContextOptions`（含重试 / 超时 / MigrationsAssembly）+ base 服务 + `IDbContextInitializer` 实现 + `SqlServerPersistenceOptions`
- **对外接口**：
  - `public static class InkwellPersistenceEfCoreSqlServerServiceCollectionExtensions`
  - `public static IInkwellBuilder UseSqlServer(this IInkwellBuilder builder, string connectionString, Action<PersistenceOptions>? configure = null)`（[HD-002 §6](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#6-builder-dsl-衔接hd-001-63) 锁定签名，不可变更）
- **内部函数或类**：无（单一静态方法，方法体顺序调用四个注册步骤）
- **输入数据**：`IInkwellBuilder` + `connectionString` + 可选 `configure`
- **输出数据**：`IInkwellBuilder`（同一实例，fluent 链式）
- **依赖模块**：`Microsoft.EntityFrameworkCore.SqlServer` / `Microsoft.Extensions.DependencyInjection` / `Microsoft.Extensions.Options` / `Inkwell.Abstractions.Builder` / `Inkwell.Abstractions.Persistence.PersistenceOptions` / `Inkwell.Persistence.EFCore.DependencyInjection`（`AddEfCorePersistenceBase()`，[HD-009 §3.11](HD-009-Inkwell.Persistence.EFCore-base.md#311-dependencyinjectioninkwellpersistenceefcoreservicecollectionextensionscs)）
- **错误处理**：
  - `builder` 为 `null` → `ArgumentNullException.ThrowIfNull(builder)`
  - `connectionString` 为 `null` / 空 / 全空白 → `ArgumentException.ThrowIfNullOrWhiteSpace(connectionString)`
  - `SqlServerPersistenceOptions` DataAnnotations 校验失败（`MaxRetryCount` / `MaxRetryDelaySeconds` 越界）→ 启动期 `.ValidateOnStart()` 触发 `OptionsValidationException`（不额外包装，遵循 [HD-002 §3.6](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 同类约定）
  - 无其他运行期失败路径（纯注册期代码）
- **日志要求**：N/A（注册期执行，`ILogger` 尚未可用）
- **测试要求**：
  - `UseSqlServer(cs)` 后 `Build()` 可解出 `IPersistenceProvider` → 实为 `EfCorePersistenceProvider`
  - `UseSqlServer(cs)` 后可解出 `IDbContextInitializer` → 实为 `SqlServerDbContextInitializer`
  - `UseSqlServer(cs)` 后 `IOptions<PersistenceOptions>.Value.ConnectionString` == 传入的 `cs`（验证 §5 "连接字符串单一来源"落地）
  - `UseSqlServer(cs, configure: o => o.CommandTimeoutSeconds = 60)` 后 `IOptions<PersistenceOptions>.Value.CommandTimeoutSeconds == 60`
  - 未在 `appsettings.json` 提供 `Inkwell:Persistence:SqlServer` 段时，`IOptions<SqlServerPersistenceOptions>.Value` 取默认值（`MaxRetryCount=6` / `MaxRetryDelaySeconds=30`）
  - `MaxRetryCount = -1`（越界）→ `Build()` 时 `OptionsValidationException`
  - `builder` 为 `null` 抛 `ArgumentNullException`；`connectionString` 为空白抛 `ArgumentException`
  - **appsettings.json 设置 `Inkwell:Persistence:CommandTimeoutSeconds = 60` 后 `IOptions<PersistenceOptions>.Value.CommandTimeoutSeconds == 60` 生效**（2026-07-06 errata，回应 design-review-report.md §20 B19/C104——验证 [HD-009 §3.11 `AddOptions<PersistenceOptions>().BindConfiguration(...)`](HD-009-Inkwell.Persistence.EFCore-base.md#311-dependencyinjectioninkwellpersistenceefcoreservicecollectionextensionscs) 配置绑定链路真实生效，且不被本方法 `Configure<PersistenceOptions>(o => o.ConnectionString = connectionString)` 覆盖）
  - 覆盖率门槛 ≥ 90%（DI 装配代码门槛对齐 [HD-001 §8.1](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#81-单元测试)）

**完整代码**：

```csharp
// src/core/providers/Persistence/Inkwell.Persistence.EFCore.SqlServer/DependencyInjection/InkwellPersistenceEfCoreSqlServerServiceCollectionExtensions.cs
namespace Inkwell.Persistence.EFCore.SqlServer.DependencyInjection;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Inkwell.Abstractions.Builder;
using Inkwell.Abstractions.Persistence;
using Inkwell.Persistence.EFCore;
using Inkwell.Persistence.EFCore.DependencyInjection;

public static class InkwellPersistenceEfCoreSqlServerServiceCollectionExtensions
{
    public static IInkwellBuilder UseSqlServer(
        this IInkwellBuilder builder,
        string connectionString,
        Action<PersistenceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        builder.Services.AddEfCorePersistenceBase();

        builder.Services.AddOptions<SqlServerPersistenceOptions>()
            .BindConfiguration("Inkwell:Persistence:SqlServer")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 连接字符串单一来源：UseSqlServer(connectionString) 参数同时透传给
        // DbContextOptionsBuilder 与 PersistenceOptions.ConnectionString，避免
        // appsettings.json 里出现两处独立配置键、值不一致时相互漂移（详 §5）。
        builder.Services.Configure<PersistenceOptions>(o => o.ConnectionString = connectionString);
        if (configure is not null)
        {
            builder.Services.PostConfigure(configure);
        }

        builder.Services.AddDbContext<InkwellDbContext>((sp, options) =>
        {
            var persistenceOptions = sp.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            var sqlServerOptions = sp.GetRequiredService<IOptions<SqlServerPersistenceOptions>>().Value;

            options
                .UseSqlServer(connectionString, sql => sql
                    .MigrationsAssembly("Inkwell.Persistence.EFCore.SqlServer")
                    .EnableRetryOnFailure(
                        sqlServerOptions.MaxRetryCount,
                        TimeSpan.FromSeconds(sqlServerOptions.MaxRetryDelaySeconds),
                        errorNumbersToAdd: null)
                    .CommandTimeout(persistenceOptions.CommandTimeoutSeconds))
                .AddInterceptors(sp.GetServices<Microsoft.EntityFrameworkCore.Diagnostics.ISaveChangesInterceptor>());
        });

        builder.Services.AddSingleton<IDbContextInitializer, SqlServerDbContextInitializer>();

        return builder;
    }
}
```

> **DI 服务类型核对**：本方法**不**新增任何 `SaveChangesInterceptor` 注册（SqlServer 不需要 RowVersion 拦截器，详 §4），因此不存在"具体类注册但按接口消费"的风险面；`AddInterceptors(sp.GetServices<ISaveChangesInterceptor>())` 沿用 HD-009 §3.11 已验证正确的接口服务类型消费方式，本 HD 仅新增 `SqlServerPersistenceOptions`（`AddOptions<T>()` 走 `Microsoft.Extensions.Options` 标准管线，不涉及接口/具体类注册歧义）与 `IDbContextInitializer` 单一实现注册（`AddSingleton<IDbContextInitializer, SqlServerDbContextInitializer>()`，接口 + 实现类型均显式声明，无歧义）。
>
> **配置绑定顺序核实（2026-07-06 errata，回应 design-review-report.md §20 B19/C104）**：[HD-009 §3.11 `AddEfCorePersistenceBase()`](HD-009-Inkwell.Persistence.EFCore-base.md#311-dependencyinjectioninkwellpersistenceefcoreservicecollectionextensionscs) 新增 `services.AddOptions<PersistenceOptions>().BindConfiguration("Inkwell:Persistence")`，在本方法内**先于**下方 `services.Configure<PersistenceOptions>(o => o.ConnectionString = connectionString)` 被调用（`builder.Services.AddEfCorePersistenceBase();` 是本方法体第一行）。[.NET Options 模式](https://learn.microsoft.com/dotnet/core/extensions/options) 按注册顺序依次对同一实例执行多个 `Configure` 委托，后一个只覆盖它显式触碰的字段——因此 `BindConfiguration` 绑定的 `CommandTimeoutSeconds`（及其余共享字段）不会被本方法紧接着的 `ConnectionString` 显式赋值覆盖或清空；二者不冲突，配置生效链条完整（核实结论详 [HD-009 §13.10](HD-009-Inkwell.Persistence.EFCore-base.md)）。

### 3.2 SqlServerPersistenceOptions.cs

- **文件路径**：`src/core/providers/Persistence/Inkwell.Persistence.EFCore.SqlServer/SqlServerPersistenceOptions.cs`
- **职责**：SqlServer 专属配置——连接重试参数；从 `appsettings.json` `"Inkwell:Persistence:SqlServer"` 段绑定（author 判断的显而易见项，非 Owner 拍板：新增专属 Options 类，比照 [HD-008 `QdrantVectorStoreOptions` / `AzureOpenAIEmbeddingOptions`](../Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md) Provider 专属 Options 先例，不回填到共享 `PersistenceOptions`——避免跨三 Provider 共享一个只有 SqlServer / Postgres 关系型 Provider 用得到的字段）
- **对外接口**：`public sealed class SqlServerPersistenceOptions { [Range(0, 20)] public int MaxRetryCount { get; init; } = 6; [Range(1, 300)] public int MaxRetryDelaySeconds { get; init; } = 30; }`
- **内部函数或类**：无（纯 DTO + DataAnnotations）
- **输入数据**：由 `IConfiguration` 绑定
- **输出数据**：`SqlServerPersistenceOptions` 实例（DI 通过 `IOptions<SqlServerPersistenceOptions>` 注入）
- **依赖模块**：`System.ComponentModel.DataAnnotations`
- **错误处理**：DataAnnotations 校验失败（`MaxRetryCount` 越界 `[0,20]` / `MaxRetryDelaySeconds` 越界 `[1,300]`）→ `.ValidateOnStart()` 触发 `OptionsValidationException`（host 兜底，[HD-002 §3.5](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 同类约定）
- **日志要求**：DI 启动期 `IValidateOptions` 失败时输出 OTel `exception.type=Microsoft.Extensions.Options.OptionsValidationException`（[HD-001 §5.3](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)）
- **测试要求**：
  - 默认值 `MaxRetryCount=6` / `MaxRetryDelaySeconds=30`（与 [`EnableRetryOnFailure()`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.sqlserverdbcontextoptionsextensions.enableretryonfailure) 无参重载的框架默认值一致，便于跨版本行为可预期）
  - `appsettings.json` 绑定：`Inkwell:Persistence:SqlServer:MaxRetryCount = 3` 生效
  - 边界：`MaxRetryCount = 0`（禁用重试，合法）/ `= 20`（合法）/ `= 21`（越界，`OptionsValidationException`）；`MaxRetryDelaySeconds = 0`（越界）/ `= 300`（合法）/ `= 301`（越界）
  - 覆盖率门槛 ≥ 90%

### 3.3 SqlServerDbContextInitializer.cs

- **文件路径**：`src/core/providers/Persistence/Inkwell.Persistence.EFCore.SqlServer/SqlServerDbContextInitializer.cs`
- **职责**：实现 [HD-009 §3.6 `IDbContextInitializer`](HD-009-Inkwell.Persistence.EFCore-base.md#36-idbcontextinitializercs)——SqlServer 场景下走 [`MigrateAsync`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.relationaldatabasefacadeextensions.migrateasync)（应用全部待处理 Migration，含建库）
- **对外接口**：`internal sealed class SqlServerDbContextInitializer : IDbContextInitializer` + `public Task InitializeAsync(InkwellDbContext db, CancellationToken ct = default)`
- **内部函数或类**：无（单一方法直接委托 `db.Database.MigrateAsync(ct)`）
- **输入数据**：`InkwellDbContext`
- **输出数据**：成功无返回值
- **依赖模块**：`Microsoft.EntityFrameworkCore.Relational` / `Inkwell.Persistence.EFCore.IDbContextInitializer`
- **错误处理**：不额外 catch——`MigrateAsync` 抛出的任何异常（含瞬时故障耗尽重试后的 `Microsoft.Data.SqlClient.SqlException`）透传给调用方 [`MigrationRunner.MigrateAsync`](HD-009-Inkwell.Persistence.EFCore-base.md#35-migrationrunnercs)（2026-07-06 errata：原 `MigrationRunner.RunAsync` 已拆分为 `MigrateAsync` / `SeedAsync`，详 [HD-009 §13.9](HD-009-Inkwell.Persistence.EFCore-base.md)），由其统一包成 `InvalidOperationException("Migration failed", inner)`（[HD-009 §4.3](HD-009-Inkwell.Persistence.EFCore-base.md#43-错误处理统一细化-hd-002-43-bcl-对照表--efcore-provider-补充)）；`OperationCanceledException` 透传
- **日志要求**：N/A——[`MigrationRunner.MigrateAsync`](HD-009-Inkwell.Persistence.EFCore-base.md#35-migrationrunnercs) 已记 `"Migration begin provider={ProviderName}"` / `"Migration ok..."`，本类不重复记录
- **测试要求**：
  - 首次调 `InitializeAsync`（对接 [Testcontainers SQL Server 镜像](https://dotnet.testcontainers.org/modules/mssql/) 的集成测试）后全部 Migration 应用成功、`__EFMigrationsHistory` 表含对应记录
  - 二次调用幂等（`MigrateAsync` 官方语义：已应用的 Migration 不重复执行）
  - `ct` 预取消时抛 `OperationCanceledException`
  - 瞬时网络故障（Testcontainers 网络注入或 mock）触发 `EnableRetryOnFailure` 重试后仍成功（回应 [HD-009 §13.7](HD-009-Inkwell.Persistence.EFCore-base.md#137-2026-07-06-errata第七轮hd-011-起草期发现executeintransactionasync-包-createexecutionstrategy-以兼容-sqlserver-enableretryonfailure) 下游联动要求）
  - 覆盖率门槛 ≥ 90%（集成测试为主，line coverage 门槛低于单元测试场景）

**完整代码**：

```csharp
// src/core/providers/Persistence/Inkwell.Persistence.EFCore.SqlServer/SqlServerDbContextInitializer.cs
namespace Inkwell.Persistence.EFCore.SqlServer;

using Microsoft.EntityFrameworkCore;
using Inkwell.Persistence.EFCore;

internal sealed class SqlServerDbContextInitializer : IDbContextInitializer
{
    public Task InitializeAsync(InkwellDbContext db, CancellationToken ct = default)
        => db.Database.MigrateAsync(ct);
}
```

## 4. RowVersion 在 SqlServer 下的真实行为

- **核心事实**：SqlServer 对 [`.IsRowVersion()`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.metadatabuilders.propertybuilder.isrowversion) 标记的 `byte[]` 属性提供**原生数据库自动生成**的并发令牌——EF Core SqlServer provider 会把该列物化为 SQL Server 原生 [`rowversion`](https://learn.microsoft.com/sql/t-sql/data-types/rowversion-transact-sql)（`timestamp` 的同义词）类型；每次 `INSERT` / `UPDATE` 时数据库引擎自动生成全库单调递增的新值，应用层**完全不需要**编写任何代码去"生成新值"（[EF Core 官方「Native database-generated concurrency tokens」](https://learn.microsoft.com/ef/core/saving/concurrency#native-database-generated-concurrency-tokens) 明确此为 SqlServer 场景的标准配置方式）。
- **无需拦截器**：值生成完全由数据库引擎负责，`.IsRowVersion()` 单独已足够；并发冲突检测本身走 EF Core 通用管线（Provider 无关），天然生效。这与需要手动模拟 RowVersion 递增的 Provider（如 Postgres，[HD-012 §4](HD-012-Inkwell.Persistence.EFCore.Postgres-adapter.md)）不同。

- **本 HD 结论**：`Inkwell.Persistence.EFCore.SqlServer` **不引入任何 `SaveChangesInterceptor`**——[HD-009 §3.1 `ApplyRowVersion(ModelBuilder mb)`](HD-009-Inkwell.Persistence.EFCore-base.md#31-inkwelldbcontextcs) 对所有 `IHasRowVersion` 实体调用的 `.IsRowVersion()` 在 SqlServer provider 下已经是完整实现，无需本 HD 补任何代码。这也是 §2 文件清单中本 HD 没有 `Interceptors/` 子目录的原因。
- **列类型说明**（避免与 §6 DbContext 子类化决策混淆）：`.IsRowVersion()` 在 SqlServer provider 下会自动推断列类型为 `rowversion`，**不需要**显式 `.HasColumnType("rowversion")` 覆写，因此不构成"需要 Provider-specific `OnModelCreating` 覆写"的理由（§6 决策的证据之一）。

## 5. 连接重试策略与连接字符串管理

### 5.1 连接字符串来源

- 按 [OQ-A006 closed §B](../../03-architecture/open-questions-arch.md) 已锁定：v1 不引入 Azure Key Vault，凭据走 [Kubernetes Secret](https://kubernetes.io/docs/concepts/configuration/secret/)（prod）/ [Docker Compose `.env`](https://docs.docker.com/compose/environment-variables/set-environment-variables/)（dev）。本 HD 不重新决策来源，仅锁定"注入到代码的路径"：
  - `Inkwell.WebApi` / `Inkwell.Worker` 的 `Program.cs` 从 `builder.Configuration.GetConnectionString("Inkwell")`（对应 `ConnectionStrings:Inkwell` 配置键，由 K8s Secret 挂载环境变量或 Compose `.env` 注入）读出字符串，作为 `UseSqlServer(connectionString)` 的显式参数传入
  - `UseSqlServer(...)` 内部同时把该字符串写入 `PersistenceOptions.ConnectionString`（§3.1 完整代码 `Configure<PersistenceOptions>`），确保 `PersistenceOptions.ConnectionString`（[HD-002 §3.5](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#35-persistenceoptionscs) `[Required]`）与 `DbContextOptionsBuilder` 实际使用的值**单一来源、不重复配置键**——避免出现 `ConnectionStrings:Inkwell` 与 `Inkwell:Persistence:ConnectionString` 两个独立配置项可能不同步的漂移风险（本 HD 在起草期发现并主动消除的潜在配置冲突，非 Owner picker 决策，纯实现层面的一致性修正）
- 安全约定沿用 [HD-002 §7](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#7-性能--安全--可观测性)：`ConnectionString` 永不写入日志（OTel `db.connection_string` 脱敏为 `"<redacted>"`）

### 5.2 连接重试策略（EnableRetryOnFailure）

- **为什么需要**：SQL Server 瞬时故障（网络抖动、Azure SQL / 自建实例主从故障转移、限流节流）是关系型 Provider 的常见场景；[`EnableRetryOnFailure`](https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency) 是 EF Core 官方推荐的标准应对方式
- **参数来源**：`SqlServerPersistenceOptions.MaxRetryCount` / `MaxRetryDelaySeconds`（§3.2），默认 6 次重试、最大延迟 30 秒——与 [`EnableRetryOnFailure()`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.sqlserverdbcontextoptionsextensions.enableretryonfailure) 无参重载的框架内置默认值一致（EF Core 内部按指数退避 + 随机抖动重试，不需要本 HD 重新实现退避算法）
- **与 `ExecuteInTransactionAsync` 的兼容性**：见本文件顶部"跨 HD 前置修正"callout + [HD-009 §13.7](HD-009-Inkwell.Persistence.EFCore-base.md#137-2026-07-06-errata第七轮hd-011-起草期发现executeintransactionasync-包-createexecutionstrategy-以兼容-sqlserver-enableretryonfailure)——`ExecuteInTransactionAsync` 已改用 `CreateExecutionStrategy().ExecuteAsync` 包装，本 HD 消费该修正后的行为，不需要在 SqlServer 侧做任何额外适配
- **幂等性要求**（继承 HD-009 §13.7）：`ExecuteInTransactionAsync(work, ct)` 的 `work` 委托在瞬时故障重试时可能被整体重新执行；本 HD 的 Repository 层调用（§2 文件清单不含业务 Repository，由 HD-009 shared base 提供）天然满足这一要求，业务 HD 起草时若在 `work` 内混入外部 I/O 需显式关注该约束

## 6. 为什么本 HD 不创建 `SqlServerInkwellDbContext` 子类

沿用 EF Core Provider 默认列类型即已满足契约的判断路径（author 判断的显而易见项，非 Owner 拍板）：

- **`IHasTimestamps` 列类型无需覆写**：EF Core SqlServer provider 对 `DateTimeOffset` CLR 类型的默认列类型即为 [`datetimeoffset`](https://learn.microsoft.com/sql/t-sql/data-types/datetimeoffset-transact-sql)——与 [HD-009 §3.1 `ApplyTimestamps`](HD-009-Inkwell.Persistence.EFCore-base.md#31-inkwelldbcontextcs) 描述的目标列类型天然一致，不需要 `HasColumnType("datetimeoffset")` 显式覆写，更不需要子类承载该覆写
- **`IHasRowVersion` 无需覆写**：见 §4——`.IsRowVersion()` 在 SqlServer provider 下已是完整实现
- **`INCLUDE` 索引优化暂不做**：[ADR-021 §Provider-specific 字段映射策略](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 提到的 SqlServer `INCLUDE` 索引例外仅限"纯优化指令"，v1 [requirements.md NFR](../../01-requirements/requirements.md) 未给出具体慢查询 / SLA 数据驱动的索引需求；本 HD 不臆造无证据支撑的索引设计。若后续性能调优（H5 执行期负载测试）确有需要，走一次小 errata 引入 `SqlServerInkwellDbContext : InkwellDbContext` 子类叠加 `OnModelCreating`（[HD-009 §3.1](HD-009-Inkwell.Persistence.EFCore-base.md#31-inkwelldbcontextcs) `OnModelCreating` 为 `virtual`，允许子类叠加），不影响本 HD 现有契约
- 少一层子类 = 少一处可能漂移的重复注册面

## 7. Migrations/ 目录约定

- **物理路径**：`src/core/providers/Persistence/Inkwell.Persistence.EFCore.SqlServer/Migrations/`
- **生成方式**：H5 编码任务执行期通过 [`dotnet ef migrations add Init -p Inkwell.Persistence.EFCore.SqlServer -s Inkwell.WebApi`](https://learn.microsoft.com/ef/core/cli/dotnet#dotnet-ef-migrations-add) 生成，**本 HD 不预先编写任何 Migration `.cs` 文件**——Migration 内容依赖最终锁定的全部 ~30 个 Entity（[HD-009 §3.7](HD-009-Inkwell.Persistence.EFCore-base.md#37-entitiesentityentitycs-模板--agententity-示例) 起，各业务 HD 逐步补齐），H3 阶段 Entity 尚未全部起草，生成 Migration 为时过早
- **命名约定**：`dotnet ef migrations add` 默认产出 `<yyyyMMddHHmmss>_<Name>.cs` + `<yyyyMMddHHmmss>_<Name>.Designer.cs` + 更新 `InkwellDbContextModelSnapshot.cs`（三者均由工具生成，不手工编辑）
- **`MigrationsAssembly`**：显式设为 `"Inkwell.Persistence.EFCore.SqlServer"`（§3.1 完整代码），确保 Migration 编译产物落在本 csproj 而非 shared base（[ADR-021 §`IPersistenceProvider` 实现唯一性](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 已给出示例代码）
- **`__EFMigrationsHistory` 表名**：使用 EF Core 默认表名，不自定义（无 NFR / ADR 要求自定义，避免无依据的配置面）

## 8. Migration 执行策略（2026-07-06 errata：由“WebApi 启动自动执行”改为 CI/CD 独立步骤，非本 HD 拍板）

> **本节记录 [ADR-021](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) / [ADR-019](../../03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md) / [HD-009](HD-009-Inkwell.Persistence.EFCore-base.md) §13.8 已锁定的上游决策，不代表本 HD 重新拍板。**

- **原设计（H2/H3 起草初期）**：[ADR-021 §Migration/DataSeed 启动行为](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) + [ADR-019](../../03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md) 原锁定 `Inkwell.WebApi` 启动时（仅 WebApi，`Inkwell.Worker` 跳过）由 `MigrationRunner` 自动调 `dbContext.Database.MigrateAsync()`
- **生产风险与真实决策过程**：本 HD 起草期发现该行为存在“未经独立人工审阅即对生产库结构做变更”的风险，已提请 Owner 确认。Owner 于 2026-07-06 拍板：**应用启动不再自动执行 Migration**，Migration 改由 CI/CD pipeline（[GitHub Actions](https://github.com/features/actions)）独立步骤执行 `dotnet ef database update`（或等价的预生成脚本 apply），在新版本 `Inkwell.WebApi` / `Inkwell.Worker` 部署之前完成；两进程启动代码均不再调用 `Database.MigrateAsync()` / `MigrationRunner.MigrateAsync()`。详见 ADR-021 2026-07-06 errata / ADR-019 2026-07-06 errata / [HD-009](HD-009-Inkwell.Persistence.EFCore-base.md) §13.8
- **本 HD §3.3 `SqlServerDbContextInitializer` 不变**：`InitializeAsync` 仍是 `db.Database.MigrateAsync(ct)` 的直接委托——变化的是“由谁在何时调用”，不是本类自身的实现；CI/CD 独立步骤 / 命令行工具通过同一 `IDbContextInitializer` 契约（或直接用标准 `dotnet ef database update` 工具）触发 Migration，两进程启动代码不再持有该调用路径
- **`InkwellSeeder.SeedAsync()` 的调用路径（2026-07-06 errata·第二轮修订，回应 design-review-report.md §20 B18/C103）**：[HD-009 §13.9](HD-009-Inkwell.Persistence.EFCore-base.md) 已把 `MigrationRunner` 拆分为 `MigrateAsync(ct)`（仅 schema 初始化）+ `SeedAsync(ct)`（仅按开关执行 Seed）两个独立公共方法——修复此前“`RunAsync()` 把二者耦合在同一方法内，SqlServer/Postgres 场景无法在跳过 Migrate 的同时仍执行 Seed”的设计空白（原 §8 遗留表述与“不再自动执行 Migration”直接矛盾）。修订后的准确描述：**`Inkwell.WebApi` / `Inkwell.Worker` 启动代码对 SqlServer 场景只调用 `MigrationRunner.SeedAsync(ct)`（`.AutoSeedOnStartup` 开关不变），不调用 `MigrationRunner.MigrateAsync(ct)`**；Seed 的前提从“随 `MigrationRunner` 完成 Migration 后触发”改为“确认 CI/CD 已将 schema 迁移到位”

## 9. Builder DSL 衔接与使用示例

承接 [HD-009 §6](HD-009-Inkwell.Persistence.EFCore-base.md#6-builder-dsl-衔接adr-021-builder-dsl-形状) + [ADR-021 §Builder DSL 形状](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 契约：

```csharp
// Inkwell.WebApi/Program.cs（prod 环境示例，appsettings.Production.json 选 SqlServer）
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddInkwell(builder.Configuration)
    .UseSqlServer(builder.Configuration.GetConnectionString("Inkwell")!)  // 本 HD 提供
    .AutoSeedOnStartup(false)              // prod 通常关闭自动 seed（HD-009 提供）
    .Build();
```

- `UseSqlServer(...)` 与 `UsePostgres(...)`（HD-012 待起草）互斥——同一个 `IServiceCollection` 上只应调用其中一个（[HD-001 §6.3](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#63-provider-扩展方法约定给-hd-002--hd-008-的契约)"后调用者覆盖前调用者"）
- `Inkwell.Worker/Program.cs` 同样调用 `.UseSqlServer(...)`（与 WebApi 共享 `AddInkwell()...` 套装，[AGENTS.md §3.1](../../../AGENTS.md) Worker DI 装配约定）；`Inkwell.WebApi` / `Inkwell.Worker` 两进程启动代码均不再调用 `MigrationRunner.MigrateAsync(ct)` 触发 Migration（2026-07-06 errata，详 §8）——Migration 由 CI/CD 独立步骤在部署前完成；两进程仍会调用 `MigrationRunner.SeedAsync(ct)`（`.AutoSeedOnStartup` 开关决定是否真正执行 Seed，详 §8 2026-07-06 errata·第二轮）

## 10. 配置项汇总

| 配置键                                               | 类型     | 默认值             | 来源                                                                                                                                         |
| ---------------------------------------------------- | -------- | ------------------ | -------------------------------------------------------------------------------------------------------------------------------------------- |
| `ConnectionStrings:Inkwell`                          | `string` | 无（`[Required]`） | ASP.NET Core 标准约定，由 `UseSqlServer(connectionString)` 参数传入并同步进 `PersistenceOptions.ConnectionString`（§5.1）                    |
| `Inkwell:Persistence:CommandTimeoutSeconds`          | `int`    | 30                 | [HD-002 §3.5](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#35-persistenceoptionscs)（共享字段，本 HD 复用不新增） |
| `Inkwell:Persistence:SqlServer:MaxRetryCount`        | `int`    | 6                  | 本 HD §3.2 新增                                                                                                                              |
| `Inkwell:Persistence:SqlServer:MaxRetryDelaySeconds` | `int`    | 30                 | 本 HD §3.2 新增                                                                                                                              |

## 11. 测试要求

### 11.1 测试 csproj 拓扑

- 单元测试：`tests/core/providers/Persistence/Inkwell.Persistence.EFCore.SqlServer.Tests/`（[MSTest.Sdk 4.x](https://github.com/microsoft/testfx) + MTP runner，与 [HD-009 §8.1](HD-009-Inkwell.Persistence.EFCore-base.md#81-测试-csproj-拓扑) 同构）——覆盖 §3.1 DI 装配 + §3.2 Options 校验（不依赖真实 SQL Server）
- 集成测试：`tests/core/providers/Persistence/Inkwell.Persistence.EFCore.SqlServer.IntegrationTests/`（[Testcontainers for .NET `mssql` 模块](https://dotnet.testcontainers.org/modules/mssql/)）——覆盖 §3.3 `MigrateAsync` + 瞬时故障重试 + §4 RowVersion 原生并发冲突场景

### 11.2 单测分组

- **`InkwellPersistenceEfCoreSqlServerServiceCollectionExtensionsTests.cs`**：见 §3.1 测试要求
- **`SqlServerPersistenceOptionsTests.cs`**：见 §3.2 测试要求
- **`SqlServerDbContextInitializerTests.cs`**（集成测试）：见 §3.3 测试要求

### 11.3 跨 Provider 契约测试联动

- HD-013（待起草）跨 Provider 契约测试包中"并发冲突（`IHasRowVersion`）"用例在 SqlServer 侧的断言依据 = 本 HD §4（原生 rowversion，无需拦截器）+ 集成测试环境（Testcontainers `mssql`）
- "Migration 启动"契约用例在 SqlServer 侧 = 本 HD §3.3 `MigrateAsync` 断言
- "瞬时故障重试"用例（新增，[HD-009 §13.7](HD-009-Inkwell.Persistence.EFCore-base.md#137-2026-07-06-errata第七轮hd-011-起草期发现executeintransactionasync-包-createexecutionstrategy-以兼容-sqlserver-enableretryonfailure) 下游联动要求）：仅 SqlServer / Postgres 关系型 Provider 适用

### 11.4 覆盖率门槛

- `SqlServerPersistenceOptions` / DI 扩展方法 line coverage ≥ 90%（对齐 [HD-001 §8.1](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#81-单元测试)）
- `SqlServerDbContextInitializer` 集成测试覆盖率不设 line coverage 数字门槛（依赖真实容器环境，以"关键路径全覆盖"为验收标准而非百分比）

## 12. 部署 / 配置

- 无独立部署单元——本 csproj 是 library，不产 Docker image
- dev `docker-compose`（[ADR-005](../../03-architecture/adr/ADR-005-deployment-docker-compose-aks.md)）默认使用 Postgres（HD-012 待起草），SqlServer 主要用于 integration test / prod；若 dev 环境需要验证 SqlServer 路径，可在 Compose 中加 `mcr.microsoft.com/mssql/server` 容器
- prod AKS Helm Chart（[ADR-005](../../03-architecture/adr/ADR-005-deployment-docker-compose-aks.md)）通过 `appsettings.Production.json` 中 `Inkwell:Providers:Persistence = "SqlServer"`（[HD-001 §3.11.1 `InkwellProvidersOptions`](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)）选中本 Provider；`ConnectionStrings:Inkwell` 由 K8s Secret 注入环境变量覆盖
- **可观测性**（2026-07-06 errata，回应 design-review-report.md §20 N32）：沿用 [HD-009 §3.2](HD-009-Inkwell.Persistence.EFCore-base.md#32-efcorepersistenceprovidercs) `EfCorePersistenceProvider` OTel span 基线；`EnableRetryOnFailure` 重试次数可通过 EF Core 内置 [`Microsoft.EntityFrameworkCore.Database.Command`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.diagnostics.dbloggercategory.database.command) 诊断事件观测，本 HD 不新增独立监控内容

## 13. 自动化检查命令

CI（GitHub Actions）在 `dotnet build` 之后执行：

```bash
ROOT=src/core/providers/Persistence/Inkwell.Persistence.EFCore.SqlServer

# C1：不引用 Inkwell.Core
grep -rn 'ProjectReference.*Inkwell\.Core\b' "$ROOT/Inkwell.Persistence.EFCore.SqlServer.csproj" \
  && echo "BAD: SqlServer adapter must not reference Inkwell.Core" && exit 1

# C2：不引入 InMemory / Npgsql 包
grep -rn 'Microsoft\.EntityFrameworkCore\.InMemory\|Npgsql\.EntityFrameworkCore\.PostgreSQL' \
  "$ROOT/Inkwell.Persistence.EFCore.SqlServer.csproj" \
  && echo "BAD: SqlServer adapter must not reference InMemory/Npgsql packages" && exit 1

# C3：必须含 Migrations/ 子目录
[ -d "$ROOT/Migrations" ] || { echo "MISSING: SqlServer adapter must have Migrations/"; exit 1; }

# C4：确认 ProjectReference base + Abstractions 均存在
grep -rn 'ProjectReference.*Inkwell\.Persistence\.EFCore\.csproj\|ProjectReference.*Inkwell\.Abstractions\.csproj' \
  "$ROOT/Inkwell.Persistence.EFCore.SqlServer.csproj" \
  || { echo "MISSING required ProjectReference(s)"; exit 1; }

# C5：确认 EnableRetryOnFailure 已接入（防止未来 errata 误删）
grep -rn 'EnableRetryOnFailure' "$ROOT/DependencyInjection/InkwellPersistenceEfCoreSqlServerServiceCollectionExtensions.cs" \
  || { echo "MISSING: EnableRetryOnFailure wiring"; exit 1; }

echo "HD-011 automation checks passed."
```

> 脚本物理位置：`scripts/ci/hd-011-checks.sh`（H5 编码任务起草，本 HD 锁脚本契约）。

## 14. 决策记录

- **`UseSqlServer` 签名**：`UseSqlServer(this IInkwellBuilder builder, string connectionString, Action<PersistenceOptions>? configure = null)`——来源 [HD-002 §6](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#6-builder-dsl-衔接hd-001-63) + [ADR-021 §Builder DSL 形状](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)；证据：两处均已预先锁定该签名，非本 HD 新拍板
- **`EnableRetryOnFailure` 与 `ExecuteInTransactionAsync` 兼容性**：Owner picker（2026-07-06）= 启用重试 + 同步修正 HD-009——已在 [HD-009 §13.7](HD-009-Inkwell.Persistence.EFCore-base.md#137-2026-07-06-errata第七轮hd-011-起草期发现executeintransactionasync-包-createexecutionstrategy-以兼容-sqlserver-enableretryonfailure) 落地（`ExecuteInTransactionAsync` 改用 `CreateExecutionStrategy().ExecuteAsync` 包装）
- **重试参数配置方式**：author 判断的显而易见项，非 Owner 拍板 = 新增 `SqlServerPersistenceOptions`（绑定 `Inkwell:Persistence:SqlServer` 配置段）——比照 [HD-008](../Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md) Provider 专属 Options 先例
- **重试参数默认值**：`MaxRetryCount=6` / `MaxRetryDelaySeconds=30`——来源 [`EnableRetryOnFailure()`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.sqlserverdbcontextoptionsextensions.enableretryonfailure) 无参重载的框架内置默认值，非本 HD 臆造数字
- **DbContext 子类化**：author 判断的显而易见项，非 Owner 拍板 = 不子类化，直接注册 base `InkwellDbContext`——理由见 §6
- **Migration 执行策略**：Owner 拍板（2026-07-06）= 由“WebApi 启动自动 `MigrateAsync()`”改为 CI/CD pipeline 独立步骤执行——详 §8 / ADR-021 errata / ADR-019 errata
- **RowVersion 值生成机制**：SqlServer 原生 `rowversion` 类型自动生成，不引入拦截器——来源 [EF Core 官方「Native database-generated concurrency tokens」](https://learn.microsoft.com/ef/core/saving/concurrency#native-database-generated-concurrency-tokens)；证据：`.IsRowVersion()` 单独已是完整实现（§4）

## 15. 待补 / 后续 HD 衔接

- **HD-012**：Postgres final adapter，需 `Migrations/` 子目录 + `UsePostgres` Builder DSL + `NpgsqlDbContextInitializer`（走 `MigrateAsync`）+ Postgres 侧 RowVersion 行为说明（`xmin` system column 映射，与 SqlServer `rowversion` 语义不同，需类似 §4 的对照说明）+ Postgres 是否也需要 `EnableRetryOnFailure` 与 `ExecuteInTransactionAsync` 兼容性核对（[Npgsql 连接重试](https://www.npgsql.org/efcore/misc/connection-resiliency.html) API 形态与 SqlServer 类似，需重复本 HD §5.2 的核查过程，不能想当然认为已被 HD-009 §13.7 完全覆盖——`CreateExecutionStrategy()` 包装是 Provider 无关的通用修正，理论上已覆盖，但仍建议 HD-012 起草时显式验证一次）
- **HD-013 跨 Provider 契约测试包**：直接复用本 HD §4 并发冲突结论（原生生成，无需拦截器）+ §3.3 Migration 断言依据；新增"瞬时故障重试"用例仅适用于 SqlServer / Postgres（§11.3）
- **`scripts/ci/hd-011-checks.sh`**：§13 自动化检查脚本物化（H5 编码任务）
- **性能调优 INCLUDE 索引**：若 H5 执行期负载测试发现具体慢查询，走小 errata 引入 `SqlServerInkwellDbContext` 子类（§6 已预留路径，不影响本 HD 现有契约）

## 16. 开放问题（需要 Owner 后续确认，非本 HD 拍板）

### 16.1 已解决

- **Migration 执行策略**（详 §8）：本 HD 起草期发现的生产风险，已提请 Owner 确认，Owner 拍板改为 CI/CD 独立步骤执行（2026-07-06，详 [ADR-021 errata](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) / [ADR-019 errata](../../03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md)）。不再是开放问题，保留在此仅作历史记录。

### 16.2 当前无其他开放问题

## 17. 同步追加跨模块文件

- [`docs/04-detailed-design/file-structure.md`](../file-structure.md) — 本 HD 同会话追加 `## providers/Persistence/Inkwell.Persistence.EFCore.SqlServer` 一级章节
- 本 HD **不**追加 `database-design.md`（SqlServer 不引入新表结构，schema 沿用 [HD-009](HD-009-Inkwell.Persistence.EFCore-base.md) 已锁定的 Entity 定义）/ **不**追加 `api-design.md`（本 HD 不含 HTTP/RPC 端点）
