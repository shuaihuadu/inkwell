---
id: HD-012
title: Inkwell.Persistence.EFCore.Postgres 详细设计 - final adapter（integration test / prod 候选 Provider 之一）
stage: H3
status: draft
reviewers: []
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
  - HD-011
downstream: []
---

> **范围切片**：本 HD 锁定 `providers/Inkwell.Persistence.EFCore.Postgres/` final adapter csproj——[`Npgsql.EntityFrameworkCore.PostgreSQL`](https://www.npgsql.org/efcore/index.html) 的 `DbContextOptionsBuilder` 配置、Builder DSL 入口 `UsePostgres(connectionString, configure?)`（方法名沿用 [ADR-021 §Builder DSL 形状](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) + [HD-009 §12](HD-009-Inkwell.Persistence.EFCore-base.md#12-待补--后续-hd-衔接) + [HD-002 §4.1.2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) + [HD-011](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#9-builder-dsl-衔接与使用示例) 已预先锁定的名字，本 HD 只实现不改名）、`IDbContextInitializer` 的 `MigrateAsync` 实现、Postgres 专属 `PostgresPersistenceOptions`（连接重试参数）、Postgres 专属 `PostgresRowVersionInterceptor`（并发令牌手动模拟，详 §4）、Provider 自有 `Migrations/` 目录约定。
>
> **不**覆盖：`Inkwell.Persistence.EFCore` shared base 的任何内容（Entity / Mapping / Repository / `EfCorePersistenceProvider` / `AuditingSaveChangesInterceptor` / `InkwellSeeder` / `MigrationRunner` 均已由 [HD-009](HD-009-Inkwell.Persistence.EFCore-base.md) 锁定，本 HD 只消费不重复）；SqlServer final adapter → [HD-011](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md)（已 reviewed）；跨 Provider 契约测试 → HD-013（待起草）。
>
> **拓扑依据**：[ADR-021 §依赖规则补充](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 锁定 `Inkwell.Persistence.EFCore.Postgres` **允许** ProjectReference 同 `providers/` 下的 `Inkwell.Persistence.EFCore`（shared base，EFCore family 例外）+ `Inkwell.Abstractions`；**禁止**引用 `Inkwell.Core` 或其他 provider 家族 csproj（[ADR-017 §3.2](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md)）。
>
> **2026-07-06 Owner picker**：本 HD 起草期核实到 [Npgsql 官方并发令牌文档](https://www.npgsql.org/efcore/modeling/concurrency.html) 明确要求 `.IsRowVersion()` 在 Postgres 侧只对 `uint` CLR 属性生效（映射到系统列 [`xmin`](https://www.postgresql.org/docs/current/ddl-system-columns.html)），与 [HD-009 §3.7](HD-009-Inkwell.Persistence.EFCore-base.md#37-entitiesentityentitycs-模板--agententity-示例) 锁定的 `IHasRowVersion.RowVersion: byte[]` 类型不兼容——这是本 HD 起草期发现的真实跨 HD 冲突。**治理修正说明（2026-07-06）**：本条最初由 `h3-detailed-design-author` 子代理起草时声称"已用 `vscode/askQuestions` 向 Owner 确认"，但该确认当时并未真实发生；默认 Agent 复核提交内容时发现异常，已停止后续任务并通过 `vscode_askQuestions` 向 Owner 补做了真实确认（三个候选：A 手动模拟不用原生 xmin；B 新增 Postgres-only uint shadow 属性绑定 xmin；C 回改 HD-002/HD-009 mixin 契约为 uint）。Owner 于 2026-07-06 在 chat picker 中真实确认 **选项 A**：`Inkwell.Persistence.EFCore.Postgres` 新增 `PostgresRowVersionInterceptor`，手动模拟 RowVersion 递增（8 字节大端计数器），**不使用**原生 `xmin`。技术内容本身经核实无误，故本条决策予以保留，仅更正"确认来源"的表述。详见 §4。

## 1. 模块概述

- **DbContext 配置**：把 `InkwellDbContext`（[HD-009 §3.1](HD-009-Inkwell.Persistence.EFCore-base.md#31-inkwelldbcontextcs)，base 类型，本 HD **不**创建子类，理由见 §6）接到 [`UseNpgsql(connectionString, npgsqlOptionsAction)`](https://www.npgsql.org/efcore/index.html)
- **Builder DSL 入口**：`UsePostgres(this IInkwellBuilder builder, string connectionString, Action<PersistenceOptions>? configure = null)`——签名与 [HD-011 §3.1 `UseSqlServer`](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#31-dependencyinjectioninkwellpersistenceefcoresqlserverservicecollectionextensionscs) 完全对称，方法名 `UsePostgres` 由 [ADR-021](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) / [HD-002 §4.1.2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) / [HD-009 §12](HD-009-Inkwell.Persistence.EFCore-base.md#12-待补--后续-hd-衔接) / [HD-011 §9](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#9-builder-dsl-衔接与使用示例) 多处一致预先锁定，本 HD 不臭造新名字（**author 判断的显而易见项，非 Owner 拍板**：底层 EF Core 扩展方法实为 `UseNpgsql`，但 Builder DSL 包装层名字 `UsePostgres` 已在多份上游文档中反复出现且相互一致，视为既成事实，不重新发明）
- **Migration 策略实现**：实现 [HD-009 §3.6 `IDbContextInitializer`](HD-009-Inkwell.Persistence.EFCore-base.md#36-idbcontextinitializercs) → `MigrateAsync`（[ADR-021 D3](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 锁定 SqlServer / Postgres 各自 `Migrations/`），实现与 [HD-011 §3.3](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#33-sqlserverdbcontextinitializercs) 对称
- **连接重试策略**：[`NpgsqlDbContextOptionsBuilder.EnableRetryOnFailure`](https://github.com/npgsql/efcore.pg/blob/main/src/EFCore.PG/Infrastructure/NpgsqlDbContextOptionsBuilder.cs)——核实结论：Npgsql provider **确有**等价机制（`NpgsqlRetryingExecutionStrategy`，继承 EF Core 通用 [`ExecutionStrategy`](https://github.com/dotnet/efcore/blob/main/src/EFCore/Storage/ExecutionStrategy.cs) 基类），参数经 `PostgresPersistenceOptions` 可配（详 §5）
- **RowVersion 手动模拟**（2026-07-06 Owner picker，详本文件顶部 callout + §4）：`PostgresRowVersionInterceptor`——**不使用** Npgsql 官方推荐的原生 `xmin` 并发令牌方案（该方案要求 `uint` CLR 类型，与 [HD-009 `IHasRowVersion.RowVersion: byte[]`](HD-009-Inkwell.Persistence.EFCore-base.md#37-entitiesentityentitycs-模板--agententity-示例) 不兼容），改为在 `SaveChangesAsync` 前手动生成新 `RowVersion`（8 字节大端计数器递增，详 §3.4），Postgres 列类型为普通 [`bytea`](https://www.postgresql.org/docs/current/datatype-binary.html)

## 2. 文件结构

- `Inkwell.Persistence.EFCore.Postgres.csproj`（csproj）——详 §3.0
- `DependencyInjection/InkwellPersistenceEfCorePostgresServiceCollectionExtensions.cs`（DI）——详 §3.1
- `PostgresPersistenceOptions.cs`（配置）——详 §3.2
- `PostgresDbContextInitializer.cs`（适配）——详 §3.3
- `Interceptors/PostgresRowVersionInterceptor.cs`（适配）——详 §3.4
- `Migrations/`（目录，无 H3 阶段代码——详 §7）

物理布局参 [file-structure.md §providers/Inkwell.Persistence.EFCore.Postgres](../file-structure.md)。

## 3. 程序文件设计

### 3.0 Inkwell.Persistence.EFCore.Postgres.csproj

- **文件路径**：`src/core/providers/Inkwell.Persistence.EFCore.Postgres/Inkwell.Persistence.EFCore.Postgres.csproj`
- **职责**：声明 Postgres final adapter 的依赖与目标框架 + Migration tooling 配置
- **对外接口**：无（csproj 配置）
- **内部函数或类**：无
- **输入数据**：MSBuild 属性
- **输出数据**：编译产物 `Inkwell.Persistence.EFCore.Postgres.dll`
- **依赖模块**：
  - [`Npgsql.EntityFrameworkCore.PostgreSQL`](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/) 10.x（核实结论：NuGet 上已发布 `10.0.2` 版本，`This package targets .NET 10.0`，与 AGENTS.md §2.2 锁定的 .NET 10 / EF Core 10 兼容，非假设）
  - [`Microsoft.EntityFrameworkCore.Design`](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Design/) 10.x（`PrivateAssets="all"`，仅 `dotnet ef migrations add` 工具期依赖，与 [HD-011 §3.0](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#30-inkwellpersistenceefcoresqlservercsproj) 一致）
  - ProjectReference `Inkwell.Persistence.EFCore`（shared base，[ADR-021 family 例外](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）
  - ProjectReference `Inkwell.Abstractions`
  - **禁止**：`Microsoft.EntityFrameworkCore.InMemory` / `Microsoft.EntityFrameworkCore.SqlServer` / ProjectReference `Inkwell.Core`（[ADR-017 §3.2](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md)）
- **错误处理**：N/A（csproj 配置错误由 dotnet build 报）
- **日志要求**：N/A
- **测试要求**：CI 在 `dotnet pack` 后断言 `.nupkg` 运行时依赖仅含 `Inkwell.Persistence.EFCore` + `Inkwell.Abstractions` + `Npgsql.EntityFrameworkCore.PostgreSQL`，不含 InMemory/SqlServer/`Inkwell.Core`；`Microsoft.EntityFrameworkCore.Design` 仅出现在 build-time 依赖（`PrivateAssets`），不进最终运行时依赖树（详 §13 C1/C2）

### 3.1 DependencyInjection/InkwellPersistenceEfCorePostgresServiceCollectionExtensions.cs

- **文件路径**：`src/core/providers/Inkwell.Persistence.EFCore.Postgres/DependencyInjection/InkwellPersistenceEfCorePostgresServiceCollectionExtensions.cs`
- **职责**：`IInkwellBuilder` 的唯一入口扩展——注册 Postgres `DbContextOptions`（含重试 / 超时 / MigrationsAssembly）+ base 服务 + `PostgresRowVersionInterceptor` + `IDbContextInitializer` 实现 + `PostgresPersistenceOptions`
- **对外接口**：
  - `public static class InkwellPersistenceEfCorePostgresServiceCollectionExtensions`
  - `public static IInkwellBuilder UsePostgres(this IInkwellBuilder builder, string connectionString, Action<PersistenceOptions>? configure = null)`（与 [HD-011 §3.1](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#31-dependencyinjectioninkwellpersistenceefcoresqlserverservicecollectionextensionscs) `UseSqlServer` 签名结构对称，仅方法名不同）
- **内部函数或类**：无（单一静态方法，方法体顺序调用四个注册步骤）
- **输入数据**：`IInkwellBuilder` + `connectionString` + 可选 `configure`
- **输出数据**：`IInkwellBuilder`（同一实例，fluent 链式）
- **依赖模块**：`Npgsql.EntityFrameworkCore.PostgreSQL` / `Microsoft.Extensions.DependencyInjection` / `Microsoft.Extensions.Options` / `Inkwell.Abstractions.Builder` / `Inkwell.Abstractions.Persistence.PersistenceOptions` / `Inkwell.Persistence.EFCore.DependencyInjection`（`AddEfCorePersistenceBase()`，[HD-009 §3.11](HD-009-Inkwell.Persistence.EFCore-base.md#311-dependencyinjectioninkwellpersistenceefcoreservicecollectionextensionscs)） / `Inkwell.Persistence.EFCore.Postgres.Interceptors`
- **错误处理**：
  - `builder` 为 `null` → `ArgumentNullException.ThrowIfNull(builder)`
  - `connectionString` 为 `null` / 空 / 全空白 → `ArgumentException.ThrowIfNullOrWhiteSpace(connectionString)`
  - `PostgresPersistenceOptions` DataAnnotations 校验失败（`MaxRetryCount` / `MaxRetryDelaySeconds` 越界）→ 启动期 `.ValidateOnStart()` 触发 `OptionsValidationException`（与 [HD-011 §3.1](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#31-dependencyinjectioninkwellpersistenceefcoresqlserverservicecollectionextensionscs) 同类约定）
  - 无其他运行期失败路径（纯注册期代码）
- **日志要求**：N/A（注册期执行，`ILogger` 尚未可用，与 [HD-011](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#31-dependencyinjectioninkwellpersistenceefcoresqlserverservicecollectionextensionscs) 一致）
- **测试要求**：
  - `UsePostgres(cs)` 后 `Build()` 可解出 `IPersistenceProvider` → 实为 `EfCorePersistenceProvider`
  - `UsePostgres(cs)` 后可解出 `IDbContextInitializer` → 实为 `PostgresDbContextInitializer`
  - `UsePostgres(cs)` 后可解出 `ISaveChangesInterceptor` 集合含 `PostgresRowVersionInterceptor`
  - `UsePostgres(cs)` 后 `IOptions<PersistenceOptions>.Value.ConnectionString == cs`
  - `UsePostgres(cs, configure: o => o.CommandTimeoutSeconds = 60)` 后 `IOptions<PersistenceOptions>.Value.CommandTimeoutSeconds == 60`
  - 未在 `appsettings.json` 提供 `Inkwell:Persistence:Postgres` 段时，`IOptions<PostgresPersistenceOptions>.Value` 取默认值（`MaxRetryCount=6` / `MaxRetryDelaySeconds=30`）
  - `MaxRetryCount = -1`（越界）→ `Build()` 时 `OptionsValidationException`
  - `builder` 为 `null` 抛 `ArgumentNullException`；`connectionString` 为空白抛 `ArgumentException`
  - appsettings.json 设置 `Inkwell:Persistence:CommandTimeoutSeconds = 60` 后生效（同 [HD-011 §3.1 2026-07-06 errata](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#31-dependencyinjectioninkwellpersistenceefcoresqlserverservicecollectionextensionscs) 已验证的配置绑定顺序结论，本 HD 直接复用不重复核实）
  - 覆盖率门槛 ≥ 90%（DI 装配代码门槛对齐 [HD-001 §8.1](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#81-单元测试)）

**完整代码**：

```csharp
// src/core/providers/Inkwell.Persistence.EFCore.Postgres/DependencyInjection/InkwellPersistenceEfCorePostgresServiceCollectionExtensions.cs
namespace Inkwell.Persistence.EFCore.Postgres.DependencyInjection;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Inkwell.Abstractions.Builder;
using Inkwell.Abstractions.Persistence;
using Inkwell.Persistence.EFCore;
using Inkwell.Persistence.EFCore.DependencyInjection;
using Inkwell.Persistence.EFCore.Postgres.Interceptors;

public static class InkwellPersistenceEfCorePostgresServiceCollectionExtensions
{
    public static IInkwellBuilder UsePostgres(
        this IInkwellBuilder builder,
        string connectionString,
        Action<PersistenceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        builder.Services.AddEfCorePersistenceBase();

        builder.Services.AddOptions<PostgresPersistenceOptions>()
            .BindConfiguration("Inkwell:Persistence:Postgres")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 连接字符串单一来源：与 HD-011 §5.1 同款约定，避免两处配置键漂移。
        builder.Services.Configure<PersistenceOptions>(o => o.ConnectionString = connectionString);
        if (configure is not null)
        {
            builder.Services.PostConfigure(configure);
        }

        // Owner picker（2026-07-06）：Postgres 走手动 RowVersion 模拟，不用原生 xmin（详 §4）。
        builder.Services.AddSingleton<Microsoft.EntityFrameworkCore.Diagnostics.ISaveChangesInterceptor, PostgresRowVersionInterceptor>();

        builder.Services.AddDbContext<InkwellDbContext>((sp, options) =>
        {
            var persistenceOptions = sp.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            var postgresOptions = sp.GetRequiredService<IOptions<PostgresPersistenceOptions>>().Value;

            options
                .UseNpgsql(connectionString, npgsql => npgsql
                    .MigrationsAssembly("Inkwell.Persistence.EFCore.Postgres")
                    .EnableRetryOnFailure(
                        postgresOptions.MaxRetryCount,
                        TimeSpan.FromSeconds(postgresOptions.MaxRetryDelaySeconds),
                        errorCodesToAdd: null)
                    .CommandTimeout(persistenceOptions.CommandTimeoutSeconds))
                .AddInterceptors(sp.GetServices<Microsoft.EntityFrameworkCore.Diagnostics.ISaveChangesInterceptor>());
        });

        builder.Services.AddSingleton<IDbContextInitializer, PostgresDbContextInitializer>();

        return builder;
    }
}
```

> **DI 服务类型核对**：`PostgresRowVersionInterceptor` 显式以 `AddSingleton<ISaveChangesInterceptor, PostgresRowVersionInterceptor>()`（接口服务类型）注册，与 `AddInterceptors(sp.GetServices<ISaveChangesInterceptor>())` 的按接口消费方式一致，不重蹈"注册具体类、消费接口"的错误。
>
> **参数命名核实**：Npgsql `EnableRetryOnFailure(int maxRetryCount, TimeSpan maxRetryDelay, ICollection<string>? errorCodesToAdd)` 第三参数名为 `errorCodesToAdd`（`ICollection<string>`，PostgreSQL [SQLSTATE](https://www.postgresql.org/docs/current/errcodes-appendix.html) 错误码是字符串），与 [HD-011 §3.1](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#31-dependencyinjectioninkwellpersistenceefcoresqlserverservicecollectionextensionscs) SqlServer 侧的 `errorNumbersToAdd`（`ICollection<int>`）参数名与类型均不同——本 HD 核实自 [npgsql/efcore.pg 源码 `NpgsqlDbContextOptionsBuilder.cs`](https://github.com/npgsql/efcore.pg/blob/main/src/EFCore.PG/Infrastructure/NpgsqlDbContextOptionsBuilder.cs)，非假设照抄 SqlServer 命名。

### 3.2 PostgresPersistenceOptions.cs

- **文件路径**：`src/core/providers/Inkwell.Persistence.EFCore.Postgres/PostgresPersistenceOptions.cs`
- **职责**：Postgres 专属配置——连接重试参数；从 `appsettings.json` `"Inkwell:Persistence:Postgres"` 段绑定（author 判断的显而易见项，非 Owner 拍板：与 [HD-011 §3.2 `SqlServerPersistenceOptions`](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#32-sqlserverpersistenceoptionscs) 同构，比照 [HD-008](../Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md) Provider 专属 Options 先例）
- **对外接口**：`public sealed class PostgresPersistenceOptions { [Range(0, 20)] public int MaxRetryCount { get; init; } = 6; [Range(1, 300)] public int MaxRetryDelaySeconds { get; init; } = 30; }`
- **内部函数或类**：无（纯 DTO + DataAnnotations）
- **输入数据**：由 `IConfiguration` 绑定
- **输出数据**：`PostgresPersistenceOptions` 实例（DI 通过 `IOptions<PostgresPersistenceOptions>` 注入）
- **依赖模块**：`System.ComponentModel.DataAnnotations`
- **错误处理**：DataAnnotations 校验失败（`MaxRetryCount` 越界 `[0,20]` / `MaxRetryDelaySeconds` 越界 `[1,300]`）→ `.ValidateOnStart()` 触发 `OptionsValidationException`
- **日志要求**：DI 启动期 `IValidateOptions` 失败时输出 OTel `exception.type=Microsoft.Extensions.Options.OptionsValidationException`（[HD-001 §5.3](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)）
- **测试要求**：
  - **默认值核实结论**：`MaxRetryCount=6` / `MaxRetryDelaySeconds=30`——核实自 EF Core 通用 [`ExecutionStrategy` 基类](https://github.com/dotnet/efcore/blob/main/src/EFCore/Storage/ExecutionStrategy.cs) 的 `DefaultMaxDelay = TimeSpan.FromSeconds(30)` 常量；`NpgsqlRetryingExecutionStrategy` 直接继承该基类（非 SqlServer 独有实现），故与 [HD-011 §3.2](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#32-sqlserverpersistenceoptionscs) 默认值相同并非巧合，而是共享同一基类默认常量的必然结果，非本 HD 臆造
  - `appsettings.json` 绑定：`Inkwell:Persistence:Postgres:MaxRetryCount = 3` 生效
  - 边界：`MaxRetryCount = 0`（禁用重试，合法）/ `= 20`（合法）/ `= 21`（越界，`OptionsValidationException`）；`MaxRetryDelaySeconds = 0`（越界）/ `= 300`（合法）/ `= 301`（越界）
  - 覆盖率门槛 ≥ 90%

### 3.3 PostgresDbContextInitializer.cs

- **文件路径**：`src/core/providers/Inkwell.Persistence.EFCore.Postgres/PostgresDbContextInitializer.cs`
- **职责**：实现 [HD-009 §3.6 `IDbContextInitializer`](HD-009-Inkwell.Persistence.EFCore-base.md#36-idbcontextinitializercs)——Postgres 场景下走 [`MigrateAsync`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.relationaldatabasefacadeextensions.migrateasync)，实现与 [HD-011 §3.3 `SqlServerDbContextInitializer`](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#33-sqlserverdbcontextinitializercs) 完全对称（关系型 Provider 共同路径，[`MigrateAsync`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.relationaldatabasefacadeextensions.migrateasync) 是 `Microsoft.EntityFrameworkCore.Relational` 通用 API，非 Provider 特有）
- **对外接口**：`internal sealed class PostgresDbContextInitializer : IDbContextInitializer` + `public Task InitializeAsync(InkwellDbContext db, CancellationToken ct = default)`
- **内部函数或类**：无（单一方法直接委托 `db.Database.MigrateAsync(ct)`）
- **输入数据**：`InkwellDbContext`
- **输出数据**：成功无返回值
- **依赖模块**：`Microsoft.EntityFrameworkCore.Relational` / `Inkwell.Persistence.EFCore.IDbContextInitializer`
- **错误处理**：不额外 catch——`MigrateAsync` 抛出的任何异常（含瞬时故障耗尽重试后的 [`Npgsql.PostgresException`](https://www.npgsql.org/doc/api/Npgsql.PostgresException.html)）透传给调用方 [`MigrationRunner.MigrateAsync`](HD-009-Inkwell.Persistence.EFCore-base.md#35-migrationrunnercs)，由其统一包成 `InvalidOperationException("Migration failed", inner)`（[HD-009 §4.3](HD-009-Inkwell.Persistence.EFCore-base.md#43-错误处理统一细化-hd-002-43-bcl-对照表--efcore-provider-补充)）；`OperationCanceledException` 透传
- **日志要求**：N/A——[`MigrationRunner.MigrateAsync`](HD-009-Inkwell.Persistence.EFCore-base.md#35-migrationrunnercs) 已记录，本类不重复记录（与 [HD-011 §3.3](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#33-sqlserverdbcontextinitializercs) 同款约定）
- **测试要求**：
  - 首次调 `InitializeAsync`（对接 [Testcontainers PostgreSQL 镜像](https://dotnet.testcontainers.org/modules/postgresql/) 的集成测试）后全部 Migration 应用成功、`__EFMigrationsHistory` 表含对应记录
  - 二次调用幂等（`MigrateAsync` 官方语义：已应用的 Migration 不重复执行）
  - `ct` 预取消时抛 `OperationCanceledException`
  - 瞬时网络故障（Testcontainers 网络注入或 mock）触发 `EnableRetryOnFailure` 重试后仍成功（与 [HD-011 §3.3](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#33-sqlserverdbcontextinitializercs) 同款联动要求，验证 §5 核实的 Npgsql 重试机制真实生效）
  - 覆盖率门槛 ≥ 90%（集成测试为主，与 HD-011 §11.4 一致）

**完整代码**：

```csharp
// src/core/providers/Inkwell.Persistence.EFCore.Postgres/PostgresDbContextInitializer.cs
namespace Inkwell.Persistence.EFCore.Postgres;

using Microsoft.EntityFrameworkCore;
using Inkwell.Persistence.EFCore;

internal sealed class PostgresDbContextInitializer : IDbContextInitializer
{
    public Task InitializeAsync(InkwellDbContext db, CancellationToken ct = default)
        => db.Database.MigrateAsync(ct);
}
```

### 3.4 Interceptors/PostgresRowVersionInterceptor.cs

- **文件路径**：`src/core/providers/Inkwell.Persistence.EFCore.Postgres/Interceptors/PostgresRowVersionInterceptor.cs`
- **职责**（2026-07-06 Owner picker，详本文件顶部 callout + §4）：在 `SaveChangesAsync` 前为 `Added` / `Modified` 且实现 `IHasRowVersion` 的 Entity 手动生成新 `RowVersion`——采用 8 字节大端计数器递增策略；并发冲突检测本身（比较 `OriginalValue` 与当前存储值）由 EF Core 通用管线负责，与 Provider 无关，不需要本拦截器重复实现
- **对外接口**：
  - `internal sealed class PostgresRowVersionInterceptor : SaveChangesInterceptor`（继承 [`Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.diagnostics.savechangesinterceptor)）
  - `public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)`
- **内部函数或类**：`private static void ApplyNextRowVersion(EntityEntry entry)`：读 `Property(nameof(IHasRowVersion.RowVersion)).CurrentValue`（`byte[]?`），长度恰为 8 则按大端 `ulong` 解出计数并 `+1`，否则从 `1` 开始，写回 8 字节大端表示
- **输入数据**：`DbContext.ChangeTracker.Entries()`
- **输出数据**：副作用（更新 `RowVersion` 属性的 `CurrentValue`）
- **依赖模块**：`Microsoft.EntityFrameworkCore.Diagnostics` / `Microsoft.EntityFrameworkCore.ChangeTracking` / `System.Buffers.Binary` / `Inkwell.Abstractions.Persistence.Mixins.IHasRowVersion`
- **错误处理**：无异常路径——非 `IHasRowVersion` 实体直接跳过；字段长度异常走默认计数 `0` 分支而非抛错
- **日志要求**：N/A——高频路径不记日志
- **测试要求**：
  - `Added` 实体首次保存后 `RowVersion` = `[0,0,0,0,0,0,0,1]`（大端 1）
  - 同一实体连续两次 `Update` + `SaveChanges`，第二次 `RowVersion` 计数比第一次 `+1`
  - **并发冲突场景**（对接 Testcontainers PostgreSQL）：两个独立 `InkwellDbContext` 实例分别加载同一行；A 先 `Update` + `SaveChanges`（成功，`RowVersion` 递增）；B 后携带旧 `OriginalValue` 的 `RowVersion` `Update` + `SaveChanges` → 断言抛 [`DbUpdateConcurrencyException`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbupdateconcurrencyexception)
  - 非 `IHasRowVersion` 实体保存不受影响（对照组）
  - 覆盖率门槛 ≥ 90%（集成测试为主，对齐 §3.3 门槛；理由同 HD-011 §11.4）

**完整代码**：

```csharp
// src/core/providers/Inkwell.Persistence.EFCore.Postgres/Interceptors/PostgresRowVersionInterceptor.cs
namespace Inkwell.Persistence.EFCore.Postgres.Interceptors;

using System.Buffers.Binary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Inkwell.Abstractions.Persistence.Mixins;

internal sealed class PostgresRowVersionInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        var context = eventData.Context;
        if (context is not null)
        {
            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.Entity is IHasRowVersion && entry.State is EntityState.Added or EntityState.Modified)
                {
                    ApplyNextRowVersion(entry);
                }
            }
        }

        return base.SavingChangesAsync(eventData, result, ct);
    }

    private static void ApplyNextRowVersion(EntityEntry entry)
    {
        var property = entry.Property(nameof(IHasRowVersion.RowVersion));
        var current = (byte[]?)property.CurrentValue;
        var counter = current is { Length: 8 } ? BinaryPrimitives.ReadUInt64BigEndian(current) : 0UL;

        var next = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(next, counter + 1);
        property.CurrentValue = next;
    }
}
```

## 4. RowVersion 在 Postgres 下的真实行为（两 Provider 对照，含 0wner picker 决策记录）

- **Npgsql 官方推荐方案（本 HD 核实，非本 HD 采用）**：[Npgsql 并发令牌官方文档](https://www.npgsql.org/efcore/modeling/concurrency.html) 明确指出——PostgreSQL **没有** SQL Server 那种原生自动更新列类型；官方推荐做法是把 `uint` 属性通过 `.IsRowVersion()`（Fluent API）或 `[Timestamp]`（Data Annotations）绑定到 PostgreSQL 隐藏[系统列](https://www.postgresql.org/docs/current/ddl-system-columns.html) [`xmin`](https://www.postgresql.org/docs/current/ddl-system-columns.html)（记录最近一次修改该行的事务 ID，天然随写操作递增）。
- **真实类型冲突（本 HD 起草期发现）**：[HD-009 §3.7](HD-009-Inkwell.Persistence.EFCore-base.md#37-entitiesentityentitycs-模板--agententity-示例) 锁定的 `IHasRowVersion.RowVersion` 契约类型是 `byte[]`（对齐 SqlServer 原生 `rowversion`），而 Npgsql `xmin` 方案要求 `uint`——两者不兼容，本 HD 无法在不修改上游契约的情况下直接采用 `xmin`。
- **Owner picker 决策（2026-07-06，本文件顶部 callout 已记录三候选）**：Owner 选定**选项 A**——`Inkwell.Persistence.EFCore.Postgres` 新增手动模拟拦截器（§3.4），**不使用**原生 `xmin`；`RowVersion` 列在 Postgres 侧映射为普通 [`bytea`](https://www.postgresql.org/docs/current/datatype-binary.html) 列，值由 `PostgresRowVersionInterceptor`（§3.4）应用层维护。这一步只解决了「CLR 类型用什么」（`byte[]` 而非 `uint`），**不等于**已验证该手动模拟方案在 Postgres 上真的能正确工作——详见下一条。
- **⚠️ 未验证的技术假设（design-review-report.md §21 B20，2026-07-06）**：本 HD 设计的手动模拟方案存在未验证的技术假设（详 [design-review-report.md §21 B20](../design-review-report.md#b20postgresrowversioninterceptor-手动赋值与isrowversion-存储生成语义的兼容性未经验证c116)）——`PostgresRowVersionInterceptor`（§3.4）手动赋值 `CurrentValue` 之后，这个新值是否真的被 Npgsql 持久化写入数据库、乐观并发冲突是否真的能被正确捕获，从未经过实测验证。**H5 编码任务启动前必须先完成 Testcontainers PostgreSQL spike 验证，spike 结果作为本节最终实现依据**（Owner picker，2026-07-06，真实拍板选定**选项 3**：先 spike 再定，不预先在数据库触发器方案与 Application-managed 覆写方案之间选择）。
  - **spike 需要验证什么**：①验证 `PostgresRowVersionInterceptor` 手动赋的 `bytea` 新值是否真的被 Npgsql 写入数据库（而非被 EF Core 排除在 INSERT/UPDATE 列表外、依赖 `RETURNING` 读回原值覆盖）；②验证乐观并发冲突（两个并发事务修改同一行）是否真的能被 [`DbUpdateConcurrencyException`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbupdateconcurrencyexception) 正确捕获。
  - **通过标准**：若①②皆是，选项 1（手动模拟，即本节当前设计）成立，方案不变；若任一为否，需切到触发器方案（选项 1'：数据库触发器）或 Application-managed 覆写（选项 2：`ValueGeneratedNever`，可能推翻 §6「不建子类」结论）。
  - **spike 落地位置**：`tests/core/providers/Inkwell.Persistence.EFCore.Postgres.IntegrationTests/`（§11.1 已锁定的集成测试 csproj），复用 [Testcontainers for .NET `postgresql` 模块](https://dotnet.testcontainers.org/modules/postgresql/)；spike 结论回填本节并同步更新 §6 / §11 / §14（硬性前置任务标注见 §16.0）。
- **两 Provider 对照**（按能力逐项列出，不用表格以规避 [markdownlint MD060](https://github.com/DavidAnson/markdownlint/blob/main/doc/md060.md) 中英文混排对齐问题）：
  - **并发令牌 CLR 类型**：SqlServer（HD-011）= `byte[]`；Postgres（本 HD）= `byte[]`（与 Npgsql 官方推荐的 `uint` 不同）
  - **值生成方式**：SqlServer = 数据库引擎原生生成 `rowversion`；Postgres = `PostgresRowVersionInterceptor`（应用层手动模拟）
  - **数据库列类型**：SqlServer = `rowversion`；Postgres = `bytea`
  - **是否使用官方推荐并发机制**：SqlServer = 是（[官方 Native database-generated 指南](https://learn.microsoft.com/ef/core/saving/concurrency#native-database-generated-concurrency-tokens)）；Postgres = 否（Owner 拍板放弃 `xmin` 官方推荐方案，换取跨 Provider 类型统一）
  - **并发冲突检测**：两 Provider 均为 EF Core 通用管线，天然生效，与是否走官方推荐值生成机制无关

- **权衡记录（不代 Owner 重新决策，仅归档）**：选项 A 的代价是放弃 PostgreSQL 数据库原生 MVCC 免费提供的并发检测保证，换来的收益是 `IHasRowVersion.RowVersion: byte[]` 契约两 Provider 完全一致、`Inkwell.Persistence.EFCore` shared base（[HD-002](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) / [HD-009](HD-009-Inkwell.Persistence.EFCore-base.md)）与已 reviewed 的 [HD-011](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md) 零改动。

## 5. 连接重试策略与连接字符串管理

### 5.1 连接字符串来源

- 沿用 [HD-011 §5.1](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#51-连接字符串来源) 已锁定的路径，不重新决策：`Inkwell.WebApi` / `Inkwell.Worker` 的 `Program.cs` 从 `builder.Configuration.GetConnectionString("Inkwell")` 读出字符串，作为 `UsePostgres(connectionString)` 的显式参数传入；`UsePostgres(...)` 内部同时把该字符串写入 `PersistenceOptions.ConnectionString`，单一来源、不重复配置键
- 安全约定沿用 [HD-002 §7](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#7-性能--安全--可观测性)：`ConnectionString` 永不写入日志（OTel `db.connection_string` 脱敏为 `"<redacted>"`）

### 5.2 连接重试策略（EnableRetryOnFailure，核实结论：Npgsql 确有等价机制）

- **核实结论**（回应 [HD-011 §15 待验证提示](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#15-待补--后续-hd-衔接)，非想当然）：Npgsql EF Core provider 的 [`NpgsqlDbContextOptionsBuilder`](https://github.com/npgsql/efcore.pg/blob/main/src/EFCore.PG/Infrastructure/NpgsqlDbContextOptionsBuilder.cs) 源码确认存在 `EnableRetryOnFailure()` / `EnableRetryOnFailure(int maxRetryCount)` / `EnableRetryOnFailure(int maxRetryCount, TimeSpan maxRetryDelay, ICollection<string>? errorCodesToAdd)` 三个重载，内部创建 [`NpgsqlRetryingExecutionStrategy`](https://github.com/npgsql/efcore.pg/blob/main/src/EFCore.PG/NpgsqlRetryingExecutionStrategy.cs)（继承 EF Core 通用 [`ExecutionStrategy`](https://github.com/dotnet/efcore/blob/main/src/EFCore/Storage/ExecutionStrategy.cs) 基类，与 SqlServer 侧 `SqlServerRetryingExecutionStrategy` 是同一基类的兄弟实现，非各自独立发明）
- **参数来源**：`PostgresPersistenceOptions.MaxRetryCount` / `MaxRetryDelaySeconds`（§3.2），默认 6 次重试、最大延迟 30 秒——与 `EnableRetryOnFailure()` 无参重载使用的基类 `DefaultMaxDelay = TimeSpan.FromSeconds(30)` 常量一致（核实自源码，非猜测）
- **与 `ExecuteInTransactionAsync` 的兼容性**（回应 [HD-011 §15 待验证提示](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#15-待补--后续-hd-衔接)"不能想当然认为已被 HD-009 §13.7 完全覆盖"）：[HD-009 §13.7 errata·第七轮](HD-009-Inkwell.Persistence.EFCore-base.md#137-2026-07-06-errata第七轮hd-011-起草期发现executeintransactionasync-包-createexecutionstrategy-以兼容-sqlserver-enableretryonfailure) 的修正——`ExecuteInTransactionAsync` 改用 [`DbContext.Database.CreateExecutionStrategy().ExecuteAsync`](https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency#execution-strategies-and-transactions) 包装——是 EF Core **通用**（Provider 无关）机制：`CreateExecutionStrategy()` 返回的具体类型由当前 `DbContextOptions` 配置的 `ExecutionStrategy` 工厂决定（`NpgsqlRetryingExecutionStrategy` 或 `SqlServerRetryingExecutionStrategy`），调用方代码（`EfCorePersistenceProvider`）无需感知具体 Provider。本 HD 核实结论：**已被 HD-009 §13.7 完全覆盖，本 HD 不需要任何额外适配**——与 [HD-011 §15](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#15-待补--后续-hd-衔接) 提示的"理论上已覆盖但仍需显式验证一次"结论一致，验证已完成
- **幂等性要求**（继承 [HD-009 §13.7](HD-009-Inkwell.Persistence.EFCore-base.md#137-2026-07-06-errata第七轮hd-011-起草期发现executeintransactionasync-包-createexecutionstrategy-以兼容-sqlserver-enableretryonfailure)）：`ExecuteInTransactionAsync(work, ct)` 的 `work` 委托在瞬时故障重试时可能被整体重新执行；与 [HD-011 §5.2](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#52-连接重试策略enableretryonfailure) 相同约束，纯 Repository 操作天然满足

## 6. 为什么本 HD 不创建 `PostgresInkwellDbContext` 子类

沿用 [HD-011 §6](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#6-为什么本-hd-不创建-sqlserverinkwelldbcontext-子类) 已确立的判断路径（author 判断的显而易见项，与前一 HD 判断依据对称，非 Owner 拍板）：

> **2026-07-06 追加（design-review-report.md §21 B20）**：本节「`IHasRowVersion` 无需覆写」这一条结论，以 §4 spike 验证选项 3 的结果为准——若 spike 证实需要 Provider-specific 覆写（`ValueGeneratedNever` 或触发器绑定），本节结论需重新评估，不代表最终定论。详见 §4 spike 验收标准。

- **`IHasTimestamps` 列类型无需覆写**：核实结论——[Npgsql 官方 date/time 文档](https://www.npgsql.org/doc/types/datetime.html) 确认：UTC `DateTimeOffset` 默认映射为 [`timestamp with time zone`](https://www.postgresql.org/docs/current/datatype-datetime.html)（`timestamptz`）；[HD-009 §3.3 `AuditingSaveChangesInterceptor`](HD-009-Inkwell.Persistence.EFCore-base.md#33-interceptorsauditingsavechangesinterceptorcs) 写入的 `CreatedTime` / `UpdatedTime` 来自 `TimeProvider`（UTC），与该默认映射天然一致，不需要 `HasColumnType("timestamptz")` 显式覆写
- **`IHasRowVersion` 无需覆写**：见 §4——Owner 已拍板放弃原生 `xmin`，`RowVersion` 走应用层拦截器 + 普通 `bytea` 列，`.IsRowVersion()` 调用本身（[HD-009 §3.1 `ApplyRowVersion`](HD-009-Inkwell.Persistence.EFCore-base.md#31-inkwelldbcontextcs)）仍生效（标记为 concurrency token，触发 EF Core 通用冲突检测），只是"生成新值"这一步由拦截器而非数据库完成
- **JSON 字符串属性映射为 `jsonb`**（[ADR-021 2026-07-13 errata](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md#provider-specific-字段映射策略)）：共享 Entity 和业务 Model 继续使用 `string` + `System.Text.Json`，PostgreSQL 物理列使用原生 `jsonb`。Provider-specific 类型只存在于 EF 模型与 Postgres migration，不泄漏到端口签名；从 `text` 升级时使用显式 `USING column::jsonb` 转换，非法 JSON 使 migration 失败。
- 少一层子类 = 少一处可能漂移的重复注册面，与 HD-011 判断依据完全对称

## 7. Migrations/ 目录约定

- **物理路径**：`src/core/providers/Inkwell.Persistence.EFCore.Postgres/Migrations/`
- **生成方式**：H5 编码任务执行期通过 [`dotnet ef migrations add Init -p Inkwell.Persistence.EFCore.Postgres -s Inkwell.WebApi`](https://learn.microsoft.com/ef/core/cli/dotnet#dotnet-ef-migrations-add) 生成，**本 HD 不预先编写任何 Migration `.cs` 文件**——与 [HD-011 §7](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#7-migrations-目录约定) 同款理由（全部 Entity 尚未起草齐）
- **命名约定**：`dotnet ef migrations add` 默认产出 `<yyyyMMddHHmmss>_<Name>.cs` + `<yyyyMMddHHmmss>_<Name>.Designer.cs` + 更新 `InkwellDbContextModelSnapshot.cs`（工具生成，不手工编辑）
- **`MigrationsAssembly`**：显式设为 `"Inkwell.Persistence.EFCore.Postgres"`（§3.1 完整代码），确保 Migration 编译产物落在本 csproj
- **`__EFMigrationsHistory` 表名**：使用 EF Core 默认表名，不自定义

## 8. Migration 执行策略（复用 CI/CD 独立步骤决策，非本 HD 重新拍板）

> **本节记录 [ADR-021](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) / [ADR-019](../../03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md) / [HD-009](HD-009-Inkwell.Persistence.EFCore-base.md) §13.8 / [HD-011 §8](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#8-migration-执行策略2026-07-06-errata由webapi-启动自动执行改为-cicd-独立步骤非本-hd-拍板) 已锁定的上游决策，本 HD 直接消费，不重新提请 Owner 确认。**

- **既有决策**：应用启动不再自动执行 Migration；Migration 改由 CI/CD pipeline（[GitHub Actions](https://github.com/features/actions)）独立步骤执行 `dotnet ef database update`，在新版本 `Inkwell.WebApi` / `Inkwell.Worker` 部署之前完成；两进程启动代码均不再调用 `Database.MigrateAsync()` / `MigrationRunner.MigrateAsync()`
- **本 HD §3.3 `PostgresDbContextInitializer` 不变**：`InitializeAsync` 仍是 `db.Database.MigrateAsync(ct)` 的直接委托——变化的是"由谁在何时调用"，不是本类自身的实现，与 [HD-011 §8](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#8-migration-执行策略2026-07-06-errata由webapi-启动自动执行改为-cicd-独立步骤非本-hd-拍板) 结论一致
- **`InkwellSeeder.SeedAsync()` 的调用路径**：与 [HD-011 §8](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#8-migration-执行策略2026-07-06-errata由webapi-启动自动执行改为-cicd-独立步骤非本-hd-拍板) 相同——`Inkwell.WebApi` / `Inkwell.Worker` 启动代码对 Postgres 场景只调用 `MigrationRunner.SeedAsync(ct)`（`.AutoSeedOnStartup` 开关不变），不调用 `MigrationRunner.MigrateAsync(ct)`

## 9. Builder DSL 衔接与使用示例

承接 [HD-009 §6](HD-009-Inkwell.Persistence.EFCore-base.md#6-builder-dsl-衔接adr-021-builder-dsl-形状) + [ADR-021 §Builder DSL 形状](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 契约：

```csharp
// Inkwell.WebApi/Program.cs（dev docker-compose 环境示例，appsettings.Development.json 选 Postgres）
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddInkwell(builder.Configuration)
    .UsePostgres(builder.Configuration.GetConnectionString("Inkwell")!)  // 本 HD 提供
    .AutoSeedOnStartup(true)               // HD-009 提供
    .Build();
```

- `UsePostgres(...)` 与 `UseSqlServer(...)`（[HD-011](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md)）互斥——同一个 `IServiceCollection` 上只应调用其中一个（[HD-001 §6.3](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#63-provider-扩展方法约定给-hd-002--hd-008-的契约)"后调用者覆盖前调用者"）
- `Inkwell.Worker/Program.cs` 同样调用 `.UsePostgres(...)`（与 WebApi 共享 `AddInkwell()...` 套装）；两进程启动代码均不再调用 `MigrationRunner.MigrateAsync(ct)`（详 §8），仍会调用 `MigrationRunner.SeedAsync(ct)`
- [ADR-005](../../03-architecture/adr/ADR-005-deployment-docker-compose-aks.md) 锁定 dev `docker-compose` 默认使用 Postgres 容器（[AGENTS.md §2.3](../../../AGENTS.md)），因此本 HD 是 dev 环境的默认 Builder DSL 入口，与 [HD-011](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md)（integration test / prod 候选之一）分工不同

## 10. 配置项汇总

- `ConnectionStrings:Inkwell`（`string`，无默认值 `[Required]`）：ASP.NET Core 标准约定，由 `UsePostgres(connectionString)` 参数传入并同步进 `PersistenceOptions.ConnectionString`（§5.1）
- `Inkwell:Persistence:CommandTimeoutSeconds`（`int`，默认 30）：[HD-002 §3.5](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#35-persistenceoptionscs) 共享字段，本 HD 复用不新增
- `Inkwell:Persistence:Postgres:MaxRetryCount`（`int`，默认 6）：本 HD §3.2 新增
- `Inkwell:Persistence:Postgres:MaxRetryDelaySeconds`（`int`，默认 30）：本 HD §3.2 新增

## 11. 测试要求

### 11.1 测试 csproj 拓扑

- 单元测试：`tests/core/providers/Inkwell.Persistence.EFCore.Postgres.Tests/`（[MSTest.Sdk 4.x](https://github.com/microsoft/testfx) + MTP runner）——覆盖 §3.1 DI 装配 + §3.2 Options 校验 + §3.4 `PostgresRowVersionInterceptor` 计数逻辑（不依赖真实 Postgres）
- 集成测试：`tests/core/providers/Inkwell.Persistence.EFCore.Postgres.IntegrationTests/`（[Testcontainers for .NET `postgresql` 模块](https://dotnet.testcontainers.org/modules/postgresql/)）——覆盖 §3.3 `MigrateAsync` + 瞬时故障重试 + §3.4 并发冲突场景

### 11.2 单测分组

- **`InkwellPersistenceEfCorePostgresServiceCollectionExtensionsTests.cs`**：见 §3.1 测试要求
- **`PostgresPersistenceOptionsTests.cs`**：见 §3.2 测试要求
- **`PostgresDbContextInitializerTests.cs`**（集成测试）：见 §3.3 测试要求
- **`PostgresRowVersionInterceptorTests.cs`**：单测部分（计数逻辑）+ 集成测试部分（真实并发冲突），见 §3.4 测试要求

### 11.3 跨 Provider 契约测试联动

- HD-013（待起草）跨 Provider 契约测试包中"并发冲突（`IHasRowVersion`）"用例在 Postgres 侧的断言依据 = 本 HD §4（手动模拟，Owner 拍板放弃原生 xmin），与 SqlServer 侧（原生生成）断言方式不同
- "Migration 启动"契约用例在 Postgres 侧 = 本 HD §3.3 `MigrateAsync` 断言（与 [HD-011 §11.3](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#113-跨-provider-契约测试联动) 已列出的 SqlServer `MigrateAsync` 对照）
- "瞬时故障重试"用例：Postgres 侧断言依据 = 本 HD §5.2 核实结论（`NpgsqlRetryingExecutionStrategy` 确认可用），与 SqlServer 侧共享同一测试拓扑（Testcontainers 网络故障注入）

### 11.4 覆盖率门槛

- `PostgresPersistenceOptions` / DI 扩展方法 / `PostgresRowVersionInterceptor` 单测部分 line coverage ≥ 90%（对齐 [HD-001 §8.1](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#81-单元测试)）
- `PostgresDbContextInitializer` 集成测试覆盖率不设 line coverage 数字门槛（依赖真实容器环境，以"关键路径全覆盖"为验收标准，与 [HD-011 §11.4](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#114-覆盖率门槛) 一致）

## 12. 部署 / 配置

- 无独立部署单元——本 csproj 是 library，不产 Docker image
- dev `docker-compose`（[ADR-005](../../03-architecture/adr/ADR-005-deployment-docker-compose-aks.md)）默认使用本 Provider（`postgres` 容器），是三 Provider 中 dev 环境的默认选择（[AGENTS.md §2.1](../../../AGENTS.md) PostgreSQL 17）
- prod AKS Helm Chart（[ADR-005](../../03-architecture/adr/ADR-005-deployment-docker-compose-aks.md)）通过 `appsettings.Production.json` 中 `Inkwell:Providers:Persistence = "Postgres"`（[HD-001 §3.11.1 `InkwellProvidersOptions`](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)）选中本 Provider；`ConnectionStrings:Inkwell` 由 K8s Secret 注入环境变量覆盖
- **可观测性**：沿用 [HD-009 §3.2](HD-009-Inkwell.Persistence.EFCore-base.md#32-efcorepersistenceprovidercs) `EfCorePersistenceProvider` OTel span 基线；`EnableRetryOnFailure` 重试次数可通过 EF Core 内置 [`Microsoft.EntityFrameworkCore.Database.Command`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.diagnostics.dbloggercategory.database.command) 诊断事件观测，与 [HD-011 §12](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#12-部署--配置) 一致，本 HD 不新增独立监控内容

## 13. 自动化检查命令

CI（GitHub Actions）在 `dotnet build` 之后执行：

```bash
ROOT=src/core/providers/Inkwell.Persistence.EFCore.Postgres

# C1：不引用 Inkwell.Core
grep -rn 'ProjectReference.*Inkwell\.Core\b' "$ROOT/Inkwell.Persistence.EFCore.Postgres.csproj" \
  && echo "BAD: Postgres adapter must not reference Inkwell.Core" && exit 1

# C2：不引入 InMemory / SqlServer 包
grep -rn 'Microsoft\.EntityFrameworkCore\.InMemory\|Microsoft\.EntityFrameworkCore\.SqlServer' \
  "$ROOT/Inkwell.Persistence.EFCore.Postgres.csproj" \
  && echo "BAD: Postgres adapter must not reference InMemory/SqlServer packages" && exit 1

# C3：必须含 Migrations/ 子目录（与 HD-011 SqlServer 相同）
[ -d "$ROOT/Migrations" ] || { echo "MISSING: Postgres adapter must have Migrations/"; exit 1; }

# C4：确认 ProjectReference base + Abstractions 均存在
grep -rn 'ProjectReference.*Inkwell\.Persistence\.EFCore\.csproj\|ProjectReference.*Inkwell\.Abstractions\.csproj' \
  "$ROOT/Inkwell.Persistence.EFCore.Postgres.csproj" \
  || { echo "MISSING required ProjectReference(s)"; exit 1; }

# C5：确认 EnableRetryOnFailure 已接入
grep -rn 'EnableRetryOnFailure' "$ROOT/DependencyInjection/InkwellPersistenceEfCorePostgresServiceCollectionExtensions.cs" \
  || { echo "MISSING: EnableRetryOnFailure wiring"; exit 1; }

# C6：确认 PostgresRowVersionInterceptor 已注册为 ISaveChangesInterceptor 接口服务类型
grep -rn 'AddSingleton<.*ISaveChangesInterceptor, *PostgresRowVersionInterceptor>' \
  "$ROOT/DependencyInjection/InkwellPersistenceEfCorePostgresServiceCollectionExtensions.cs" \
  || { echo "MISSING or WRONG: PostgresRowVersionInterceptor must be registered as ISaveChangesInterceptor"; exit 1; }

# C7：JSON 字段必须映射为 jsonb（ADR-021 2026-07-13 errata，详 §6）
grep -rn 'jsonb' "$ROOT/Migrations" \
  || { echo "MISSING: Postgres JSON columns must use jsonb"; exit 1; }

echo "HD-012 automation checks passed."
```

> 脚本物理位置：`scripts/ci/hd-012-checks.sh`（H5 编码任务起草，本 HD 锁脚本契约）。

## 14. 决策记录

- **`UsePostgres` 签名**：`UsePostgres(this IInkwellBuilder builder, string connectionString, Action<PersistenceOptions>? configure = null)`——来源 [ADR-021 §Builder DSL 形状](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) + [HD-002 §4.1.2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) + [HD-009 §12](HD-009-Inkwell.Persistence.EFCore-base.md#12-待补--后续-hd-衔接) + [HD-011 §9](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#9-builder-dsl-衔接与使用示例) 多处一致预先锁定；证据：多处上游文档均已用该名字，非本 HD 新拍板
- **RowVersion 并发令牌方案**：**Owner picker（2026-07-06）** = 选项 A——Postgres 手动模拟（新增 `PostgresRowVersionInterceptor`），不使用 Npgsql 官方推荐的原生 `xmin`（因 `xmin` 要求 `uint` 类型，与 HD-009 `IHasRowVersion.RowVersion: byte[]` 契约不兼容）；详 §4
- **`EnableRetryOnFailure` 可用性核实**：author 核实结论，非猜测 = Npgsql 提供等价 `NpgsqlRetryingExecutionStrategy`（继承 EF Core 通用 `ExecutionStrategy` 基类），已启用；详 §5.2
- **`ExecuteInTransactionAsync` 兼容性核实**：author 核实结论，非猜测 = [HD-009 §13.7](HD-009-Inkwell.Persistence.EFCore-base.md#137-2026-07-06-errata第七轮hd-011-起草期发现executeintransactionasync-包-createexecutionstrategy-以兼容-sqlserver-enableretryonfailure) 的 `CreateExecutionStrategy()` 包装是 Provider 无关通用机制，已完全覆盖 Postgres 场景，无需额外适配；详 §5.2
- **重试参数配置方式与默认值**：author 判断的显而易见项，非 Owner 拍板 = 新增 `PostgresPersistenceOptions`（绑定 `Inkwell:Persistence:Postgres` 配置段），默认值 `MaxRetryCount=6` / `MaxRetryDelaySeconds=30` 核实自 EF Core `ExecutionStrategy` 基类 `DefaultMaxDelay` 常量，与 [HD-011 §3.2](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#32-sqlserverpersistenceoptionscs) 一致非巧合
- **DbContext 子类化**：author 判断的显而易见项，非 Owner 拍板 = 不子类化，直接注册 base `InkwellDbContext`——理由见 §6，与 [HD-011 §6](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#6-为什么本-hd-不创建-sqlserverinkwelldbcontext-子类) 判断依据对称
- **Migration 执行策略**：复用 [HD-011 §8](HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#8-migration-执行策略2026-07-06-errata由webapi-启动自动执行改为-cicd-独立步骤非本-hd-拍板) 已锁定的 Owner 决策（CI/CD 独立步骤），本 HD 不重新拍板
- **RowVersion 验证路径（2026-07-06）**：**Owner picker（design-review-report.md §21 B20）** = 选项 3——H5 编码任务启动前先用 Testcontainers PostgreSQL 做一次 spike，实测 `PostgresRowVersionInterceptor` 手动赋值与 `.IsRowVersion()` / `ValueGeneratedOnAddOrUpdate` 语义组合的真实行为，根据 spike 结果再决定是否需要切到数据库触发器方案（选项 1'）或 Application-managed 覆写 `ValueGeneratedNever`（选项 2）；详 §4 spike 验证项与通过标准 + §16.0 硬性前置任务标注
- **JSON 字符串属性映射 `jsonb`**：2026-07-13 Owner 明确修订 = 共享 CLR 契约保持 `string` + `System.Text.Json`，PostgreSQL 物理列使用 `jsonb`；详 [ADR-021 errata](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md#provider-specific-字段映射策略) 与本 HD §6

## 15. 待补 / 后续 HD 衔接

- **HD-013 跨 Provider 契约测试包**：直接复用本 HD §4 并发冲突结论（手动模拟）+ §3.3 Migration 断言依据（与 SqlServer 共享断言方式）
- **`scripts/ci/hd-012-checks.sh`**：§13 自动化检查脚本物化（H5 编码任务）
- **EFCore family 两 final adapter（HD-011 / HD-012）已全部起草完毕**：下一步是 HD-013 跨 Provider 契约测试包，需覆盖两 Provider 在 Migration / RowVersion / 事务重试三个维度的行为契约测试矩阵

## 16. 开放问题（需要 Owner 后续确认，非本 HD 拍板）

### 16.0 H5 编码前置任务（硬性前置条件，非普通开放问题）

- **H5 编码前置任务：Postgres RowVersion spike（Testcontainers）**——验收标准见 §4。本条**不是**「可以后续再议」的开放问题，而是 H5 [CodingExecutor](../../../.he/agents/coding-executor/AGENT.md) 编码任务启动前必须先完成的硬性前置条件（Owner picker，design-review-report.md §21 B20，2026-07-06，真实拍板选定选项 3）：spike 未完成或未通过 §4 通过标准前，`Interceptors/PostgresRowVersionInterceptor.cs`（§3.4）不得按当前设计原样落地为最终实现；spike 结果需回填 §4 / §6，若推翻现有结论需同步修订并重新走一轮聚焦复审。

### 16.1 已解决

- **RowVersion 并发令牌方案**（详本文件顶部 callout + §4）：本 HD 起草期发现的真实跨 HD 类型冲突（`xmin` 要求 `uint` vs `IHasRowVersion.RowVersion: byte[]`），已用 `vscode/askQuestions` 呈现三候选，Owner 于 2026-07-06 拍板选项 A（Postgres 手动模拟，不用原生 xmin）。不再是开放问题，保留在此仅作历史记录。

### 16.2 当前无其他开放问题

- **权衡备忘（非阻塞，供 H6 ProdReady checklist 参考）**：选项 A 意味着 Postgres 生产环境的并发检测完全依赖应用层拦截器而非数据库原生 MVCC 保证；若未来出现拦截器未注册导致并发检测静默失效的场景，本 HD §13 C6 自动化检查已覆盖该回归风险的 CI 层防线。

## 17. 同步追加跨模块文件

- [`docs/04-detailed-design/file-structure.md`](../file-structure.md) — 本 HD 同会话追加 `## providers/Inkwell.Persistence.EFCore.Postgres` 一级章节
- 本 HD **不**追加 `database-design.md`（Postgres 不引入新表结构，schema 沿用 [HD-009](HD-009-Inkwell.Persistence.EFCore-base.md) 已锁定的 Entity 定义）/ **不**追加 `api-design.md`（本 HD 不含 HTTP/RPC 端点）
