---
id: HD-010
title: Inkwell.Persistence.EFCore.InMemory 详细设计 - final adapter（dev / unit test 默认 Provider）
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
  - ADR-021
  - ADR-023
  - HD-001
  - HD-002
  - HD-009
downstream: []
---

> **范围切片**：本 HD 锁定 `providers/Inkwell.Persistence.EFCore.InMemory/` final adapter csproj——[`Microsoft.EntityFrameworkCore.InMemory`](https://learn.microsoft.com/ef/core/providers/in-memory/) 的 `DbContextOptionsBuilder` 配置、Builder DSL 入口 `UseInMemoryDatabase()`、`IDbContextInitializer` 的 `EnsureCreated` 实现、InMemory 专属的 `RowVersion` 手动模拟拦截器（承接 [design-review-report.md N5/C7](../design-review-report.md#n5inmemory-provider-rowversion-自动管理可行性c7) 悬挂项）。
>
> **不**覆盖：`Inkwell.Persistence.EFCore` shared base 的任何内容（Entity / Mapping / Repository / `EfCorePersistenceProvider` / `AuditingSaveChangesInterceptor` / `InkwellSeeder` / `MigrationRunner` 均已由 [HD-009](HD-009-Inkwell.Persistence.EFCore-base.md) 锁定，本 HD 只消费不重复）；SqlServer / Postgres final adapter → [HD-011](.) / [HD-012](.) 各自起草；跨 Provider 契约测试 → [HD-013](.) 起草。
>
> **拓扑依据**：[ADR-021 §依赖规则补充](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 锁定 `Inkwell.Persistence.EFCore.InMemory` **允许** ProjectReference 同 `providers/` 下的 `Inkwell.Persistence.EFCore`（shared base，EFCore family 例外）+ `Inkwell.Abstractions`；**禁止**引用 `Inkwell.Core` 或其他 provider 家族 csproj（[ADR-017 §3.2](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md)）。

## 1. 模块职责

- **DbContext 配置**：把 `InkwellDbContext`（[HD-009 §3.1](HD-009-Inkwell.Persistence.EFCore-base.md#31-inkwelldbcontextcs)，base 类型，本 HD **不**创建子类，理由见 §5）接到 [`UseInMemoryDatabase(databaseName, root)`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.inmemorydbcontextoptionsextensions.useinmemorydatabase)
- **Builder DSL 入口**：`UseInMemoryDatabase(this IInkwellBuilder builder, string databaseName = "inkwell")`——[HD-001 §6.3](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#63-provider-扩展方法约定给-hd-002--hd-008-的契约) Provider 扩展方法约定的具体落地
- **Migration 策略实现**：实现 [HD-009 §3.6 `IDbContextInitializer`](HD-009-Inkwell.Persistence.EFCore-base.md#36-idbcontextinitializercs) → `EnsureCreatedAsync`（[ADR-021 D3](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 锁定 InMemory 不支持 Migration）
- **RowVersion 手动模拟**：`InMemoryRowVersionInterceptor`——EF Core InMemory 不为 `.IsRowVersion()` 提供自动生成的并发令牌（[EF Core 官方文档「Application-managed concurrency tokens」](https://learn.microsoft.com/ef/core/saving/concurrency#application-managed-concurrency-tokens) 明确此类场景需 App 自行维护令牌值），本 HD 用 `SaveChangesInterceptor` 补齐这一环节；EF Core 自身的并发冲突检测（比较 `OriginalValue` 与当前存储值）与 Provider 无关，天然生效，不需要额外模拟（详 §4）
- **数据库隔离策略**：每次 `UseInMemoryDatabase()` 调用创建独立 [`InMemoryDatabaseRoot`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.inmemorydatabaseroot)，避免同进程内多个 DI 容器（如多个测试 host）意外共享同名数据库（详 §7）

## 2. 文件清单

- `Inkwell.Persistence.EFCore.InMemory.csproj`（csproj）——详 §3.0
- `DependencyInjection/InkwellPersistenceEfCoreInMemoryServiceCollectionExtensions.cs`（DI）——详 §3.1
- `InMemoryDbContextInitializer.cs`（适配）——详 §3.2
- `Interceptors/InMemoryRowVersionInterceptor.cs`（适配）——详 §3.3

物理布局参 [file-structure.md §providers/Inkwell.Persistence.EFCore.InMemory](../file-structure.md)。

## 3. 各文件 10 字段

### 3.0 Inkwell.Persistence.EFCore.InMemory.csproj

- **文件路径**：`src/core/providers/Inkwell.Persistence.EFCore.InMemory/Inkwell.Persistence.EFCore.InMemory.csproj`
- **职责**：声明 InMemory final adapter 的依赖与目标框架
- **对外接口**：无（csproj 配置）
- **内部函数或类**：无
- **输入数据**：MSBuild 属性
- **输出数据**：编译产物 `Inkwell.Persistence.EFCore.InMemory.dll`
- **依赖模块**：
  - [`Microsoft.EntityFrameworkCore.InMemory`](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.InMemory/) 10.x
  - ProjectReference `Inkwell.Persistence.EFCore`（shared base，[ADR-021 family 例外](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）
  - ProjectReference `Inkwell.Abstractions`
  - **禁止**：`Microsoft.EntityFrameworkCore.SqlServer` / `Npgsql.EntityFrameworkCore.PostgreSQL` / [`Microsoft.EntityFrameworkCore.Design`](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Design/)（Migration tooling，InMemory 无 Migration，不需要）/ ProjectReference `Inkwell.Core`（[ADR-017 §3.2](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md)）
- **错误处理**：N/A（csproj 配置错误由 dotnet build 报）
- **日志要求**：N/A
- **测试要求**：CI 在 `dotnet pack` 后断言 `.nupkg` 仅含 `Inkwell.Persistence.EFCore` + `Inkwell.Abstractions` 依赖 + `Microsoft.EntityFrameworkCore.InMemory`，不含 SqlServer/Postgres/Design 包，不含 `Inkwell.Core` ProjectReference（详 §10 C1/C2）

### 3.1 DependencyInjection/InkwellPersistenceEfCoreInMemoryServiceCollectionExtensions.cs

- **文件路径**：`src/core/providers/Inkwell.Persistence.EFCore.InMemory/DependencyInjection/InkwellPersistenceEfCoreInMemoryServiceCollectionExtensions.cs`
- **职责**：`IInkwellBuilder` 的唯一入口扩展——注册 InMemory `DbContextOptions` + base 服务 + `IDbContextInitializer` 实现
- **对外接口**：
  - `public static class InkwellPersistenceEfCoreInMemoryServiceCollectionExtensions`
  - `public static IInkwellBuilder UseInMemoryDatabase(this IInkwellBuilder builder, string databaseName = "inkwell")`（[HD-009 §6 step 2](HD-009-Inkwell.Persistence.EFCore-base.md#6-builder-dsl-衔接adr-021-builder-dsl-形状) 契约「InMemory 无参」——可选参数使零参调用 `UseInMemoryDatabase()` 合法，同时保留调用方按需自定义命名空间的能力，详 §7）
- **内部函数或类**：无（单一静态方法，方法体直接顺序调用三个注册步骤）
- **输入数据**：`IInkwellBuilder` + 可选 `databaseName`
- **输出数据**：`IInkwellBuilder`（同一实例，fluent 链式）
- **依赖模块**：`Microsoft.EntityFrameworkCore.InMemory` / `Microsoft.Extensions.DependencyInjection` / `Inkwell.Abstractions.Builder` / `Inkwell.Persistence.EFCore.DependencyInjection`（`AddEfCorePersistenceBase()`，internal + `InternalsVisibleTo`，[HD-009 §3.11](HD-009-Inkwell.Persistence.EFCore-base.md#311-dependencyinjectioninkwellpersistenceefcoreservicecollectionextensionscs)） / `Inkwell.Persistence.EFCore.InMemory.Interceptors`
- **错误处理**：
  - `builder` 为 `null` → `ArgumentNullException.ThrowIfNull(builder)`
  - `databaseName` 为 `null` / 空 / 全空白 → `ArgumentException.ThrowIfNullOrWhiteSpace(databaseName)`
  - 无其他运行期失败路径（纯注册期代码）
- **日志要求**：N/A（注册期执行，`ILogger` 尚未可用，与 [HD-009 §3.11](HD-009-Inkwell.Persistence.EFCore-base.md#311-dependencyinjectioninkwellpersistenceefcoreservicecollectionextensionscs) 一致）
- **测试要求**：
  - `UseInMemoryDatabase()`（零参）后 `Build()` 可解出 `IPersistenceProvider` → 实为 `EfCorePersistenceProvider`
  - `UseInMemoryDatabase()` 后可解出 `IDbContextInitializer` → 实为 `InMemoryDbContextInitializer`
  - 两次独立 `AddInkwell().UseInMemoryDatabase()`（各自独立 `IServiceCollection` + `BuildServiceProvider()`）即便 `databaseName` 相同，读写互不可见（验证 `InMemoryDatabaseRoot` 隔离，详 §7）
  - `builder` 为 `null` 抛 `ArgumentNullException`；`databaseName` 为空白抛 `ArgumentException`
  - 覆盖率门槛 ≥ 90%（DI 装配代码门槛对齐 [HD-001 §8.1](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#81-单元测试)）

**完整代码**：

```csharp
// src/core/providers/Inkwell.Persistence.EFCore.InMemory/DependencyInjection/InkwellPersistenceEfCoreInMemoryServiceCollectionExtensions.cs
namespace Inkwell.Persistence.EFCore.InMemory.DependencyInjection;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Inkwell.Abstractions.Builder;
using Inkwell.Persistence.EFCore;
using Inkwell.Persistence.EFCore.DependencyInjection;
using Inkwell.Persistence.EFCore.InMemory.Interceptors;

public static class InkwellPersistenceEfCoreInMemoryServiceCollectionExtensions
{
    public static IInkwellBuilder UseInMemoryDatabase(this IInkwellBuilder builder, string databaseName = "inkwell")
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        // 每次调用独立 root：即使 databaseName 相同，不同 DI 容器（不同 AddInkwell() 调用）也不会共享数据。
        // 依据 EF Core 官方指南：https://learn.microsoft.com/ef/core/testing/testing-without-the-database#inmemory-provider
        var root = new InMemoryDatabaseRoot();

        builder.Services.AddEfCorePersistenceBase();
        builder.Services.AddSingleton<Microsoft.EntityFrameworkCore.Diagnostics.ISaveChangesInterceptor, InMemoryRowVersionInterceptor>();
        builder.Services.AddDbContext<InkwellDbContext>((sp, options) => options
            .UseInMemoryDatabase(databaseName, root)
            .AddInterceptors(sp.GetServices<Microsoft.EntityFrameworkCore.Diagnostics.ISaveChangesInterceptor>()));
        builder.Services.AddSingleton<IDbContextInitializer, InMemoryDbContextInitializer>();

        return builder;
    }
}
```

> **注·2026-07-06 修正**：`builder.Services.AddSingleton<InMemoryRowVersionInterceptor>()`（服务类型=具体类）此前与消费端 `AddInterceptors(sp.GetServices<ISaveChangesInterceptor>())`（按接口服务类型查询）不匹配，导致该拦截器永不执行（design-review-report.md §19 B16/C96）；已修正为 `AddSingleton<ISaveChangesInterceptor, InMemoryRowVersionInterceptor>()`（上方代码已同步）。`AddInterceptors(sp.GetServices<ISaveChangesInterceptor>())` 同时要求 [HD-009 §3.11 `AddEfCorePersistenceBase()`](HD-009-Inkwell.Persistence.EFCore-base.md#311-dependencyinjectioninkwellpersistenceefcoreservicecollectionextensionscs) 把 `AuditingSaveChangesInterceptor` 同样注册为 `ISaveChangesInterceptor` 服务类型——[HD-009 §13.6 errata·第六轮](HD-009-Inkwell.Persistence.EFCore-base.md#136-2026-07-06-errata第六轮hd-010-首轮评审-design-review-reportmd-19-b17c97-补齐-addefcorepersistencebase-完整代码) 已补齐该方法的完整代码并确认采用同一注册方式，该假设现已成立。

### 3.2 InMemoryDbContextInitializer.cs

- **文件路径**：`src/core/providers/Inkwell.Persistence.EFCore.InMemory/InMemoryDbContextInitializer.cs`
- **职责**：实现 [HD-009 §3.6 `IDbContextInitializer`](HD-009-Inkwell.Persistence.EFCore-base.md#36-idbcontextinitializercs)——InMemory 场景下走 [`EnsureCreatedAsync`](https://learn.microsoft.com/ef/core/managing-schemas/ensure-created)，**不是** no-op（[ADR-021 §Migration/DataSeed 启动行为](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 明确要求 InMemory 走 `EnsureCreatedAsync` 建库，而非跳过初始化）
- **对外接口**：`internal sealed class InMemoryDbContextInitializer : IDbContextInitializer` + `public async Task InitializeAsync(InkwellDbContext db, CancellationToken ct = default)`
- **内部函数或类**：无（单一方法直接委托 `db.Database.EnsureCreatedAsync(ct)`）
- **输入数据**：`InkwellDbContext`
- **输出数据**：成功无返回值；`EnsureCreatedAsync` 内部返回的 `bool`（是否新建）不对外暴露——[HD-009 §3.5 `MigrationRunner`](HD-009-Inkwell.Persistence.EFCore-base.md#35-migrationrunnercs) 不关心该值
- **依赖模块**：`Microsoft.EntityFrameworkCore` / `Inkwell.Persistence.EFCore.IDbContextInitializer`
- **错误处理**：不额外 catch——`EnsureCreatedAsync` 抛出的任何异常透传给调用方 [`MigrationRunner.MigrateAsync`](HD-009-Inkwell.Persistence.EFCore-base.md#35-migrationrunnercs)（2026-07-06 errata：原 `MigrationRunner.RunAsync` 已拆分为 `MigrateAsync` / `SeedAsync`，详 [HD-009 §13.9](HD-009-Inkwell.Persistence.EFCore-base.md)），由其统一包成 `InvalidOperationException("Migration failed", inner)`（[HD-009 §4.3](HD-009-Inkwell.Persistence.EFCore-base.md#43-错误处理统一细化-hd-002-43-bcl-对照表--efcore-provider-补充)）；`OperationCanceledException` 透传
- **日志要求**：N/A——[`MigrationRunner.MigrateAsync`](HD-009-Inkwell.Persistence.EFCore-base.md#35-migrationrunnercs) 已记 `"Migration begin provider={ProviderName}"` / `"Migration ok..."`，本类不重复记录，避免同一次启动出现两条语义重叠日志
- **测试要求**：
  - 首次调 `InitializeAsync` 后 `db.Database.CanConnectAsync()` 返回 `true`（[`DatabaseFacade.CanConnectAsync`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.infrastructure.databasefacade.canconnectasync)）
  - 二次调用幂等（`EnsureCreatedAsync` 官方语义：已存在则返回 `false`，不抛异常）
  - `ct` 预取消时抛 `OperationCanceledException`
  - 覆盖率门槛 ≥ 95%

**完整代码**：

```csharp
// src/core/providers/Inkwell.Persistence.EFCore.InMemory/InMemoryDbContextInitializer.cs
namespace Inkwell.Persistence.EFCore.InMemory;

using Microsoft.EntityFrameworkCore;
using Inkwell.Persistence.EFCore;

internal sealed class InMemoryDbContextInitializer : IDbContextInitializer
{
    public Task InitializeAsync(InkwellDbContext db, CancellationToken ct = default)
        => db.Database.EnsureCreatedAsync(ct);
}
```

### 3.3 Interceptors/InMemoryRowVersionInterceptor.cs

- **文件路径**：`src/core/providers/Inkwell.Persistence.EFCore.InMemory/Interceptors/InMemoryRowVersionInterceptor.cs`
- **职责**：在 `SaveChangesAsync` 前为 `Added` / `Modified` 且实现 `IHasRowVersion` 的 Entity 手动生成新 `RowVersion`（[HD-002 §3.8](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 早已写明 InMemory 侧"客户端递增"约定，[design-review-report N5](../design-review-report.md#n5inmemory-provider-rowversion-自动管理可行性c7) 要求本 HD 显式落实）；并发冲突检测本身（比较 `OriginalValue` 与当前存储值）由 EF Core 通用管线负责，不需要本拦截器重复实现
- **对外接口**：
  - `internal sealed class InMemoryRowVersionInterceptor : SaveChangesInterceptor`（继承 [`Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.diagnostics.savechangesinterceptor)）
  - `public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)`
- **内部函数或类**：
  - `private static void ApplyNextRowVersion(EntityEntry entry)`——读 `Property(nameof(IHasRowVersion.RowVersion)).CurrentValue`（`byte[]?`），按大端 [`ulong`](https://learn.microsoft.com/dotnet/api/system.buffers.binary.binaryprimitives.writeuint64bigendian) 解释：长度恰为 8 则解出当前计数并 `+1`；否则（`null` / 长度不为 8，典型为 Added 场景的默认 `RowVersion = []`）视为计数 `0` 从 `1` 开始；写回 8 字节大端表示
- **输入数据**：`DbContext.ChangeTracker.Entries()`（由基类 `SavingChangesAsync` hook 提供上下文）
- **输出数据**：副作用（更新 `RowVersion` 属性的 `CurrentValue`）
- **依赖模块**：`Microsoft.EntityFrameworkCore.Diagnostics` / `Microsoft.EntityFrameworkCore.ChangeTracking` / `System.Buffers.Binary` / `Inkwell.Abstractions.Persistence.Mixins.IHasRowVersion`
- **错误处理**：无异常路径——非 `IHasRowVersion` 实体直接跳过；字段长度异常走默认计数 `0` 分支而非抛错（容错优先，避免因历史脏数据导致启动期或运行期崩溃）
- **日志要求**：N/A——与 [HD-009 §3.3 `AuditingSaveChangesInterceptor`](HD-009-Inkwell.Persistence.EFCore-base.md#33-interceptorsauditingsavechangesinterceptorcs) 一致的"高频路径不记日志"原则，每次 `SaveChanges` 都会触发，记录会造成噪音
- **测试要求**：
  - `Added` 实体首次保存后 `RowVersion` = `[0,0,0,0,0,0,0,1]`（大端 1）
  - 同一实体连续两次 `Update` + `SaveChanges`，第二次 `RowVersion` 计数比第一次 `+1`
  - **并发冲突场景**（核心用例）：同一 `databaseName` + 同一 `InMemoryDatabaseRoot` 下开两个独立 `InkwellDbContext` 实例分别加载同一行；A 先 `Update` + `SaveChanges`（成功，`RowVersion` 递增）；B 后携带旧 `OriginalValue` 的 `RowVersion` `Update` + `SaveChanges` → 断言抛 [`DbUpdateConcurrencyException`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbupdateconcurrencyexception)（验证 EF Core 通用并发检测在 InMemory + 本拦截器组合下确实生效，回应 [N5](../design-review-report.md#n5inmemory-provider-rowversion-自动管理可行性c7)）
  - 非 `IHasRowVersion` 实体保存不受影响（对照组）
  - 覆盖率门槛 ≥ 95%

**完整代码**：

```csharp
// src/core/providers/Inkwell.Persistence.EFCore.InMemory/Interceptors/InMemoryRowVersionInterceptor.cs
namespace Inkwell.Persistence.EFCore.InMemory.Interceptors;

using System.Buffers.Binary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Inkwell.Abstractions.Persistence.Mixins;

internal sealed class InMemoryRowVersionInterceptor : SaveChangesInterceptor
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

## 4. RowVersion 模拟策略详解（回应 N5/C7）

- **问题边界**：`.IsRowVersion()`（[HD-009 §3.1 `ApplyRowVersion`](HD-009-Inkwell.Persistence.EFCore-base.md#31-inkwelldbcontextcs)）在 EF Core 内部等价于 `.IsConcurrencyToken().ValueGeneratedOnAddOrUpdate()`。SqlServer `rowversion` / Postgres 触发器可以让数据库自动生成 `ValueGeneratedOnAddOrUpdate()` 要求的新值；InMemory Provider **没有**为任意 `byte[]` 属性提供这种"写后自动生成新值"的机制，这正是 [design-review-report N5/C7](../design-review-report.md#n5inmemory-provider-rowversion-自动管理可行性c7) 指出的缺口。
- **不缺的部分**：并发冲突检测（保存前比较 `EntityEntry.OriginalValues` 中的并发令牌与数据库/存储当前值，不一致则抛 `DbUpdateConcurrencyException`）是 EF Core [`SaveChanges` 管线的通用行为](https://learn.microsoft.com/ef/core/saving/concurrency#optimistic-concurrency)，只要属性被标记为 concurrency token（`.IsRowVersion()` 已经标记），**任何** Provider（含 InMemory）都会执行这个比较——这部分不需要本 HD 额外实现。
- **本 HD 补的部分**：仅仅是"生成新值"这一步——`InMemoryRowVersionInterceptor` 在保存前把 `RowVersion` 递增，充当 InMemory 缺失的 value generator。这与 [EF Core 官方「Application-managed concurrency tokens」指南](https://learn.microsoft.com/ef/core/saving/concurrency#application-managed-concurrency-tokens) 描述的"通过 `SaveChanges` interceptor 生成新令牌值"完全一致，属于文档明确背书的标准做法，不是本 HD 自创机制。
- **8 字节大端计数器 vs 其他方案**：选用"大端 `ulong` 递增"而非 `Guid.NewGuid()` 随机值，是因为 [HD-002 §3.8](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 与 [N5](../design-review-report.md#n5inmemory-provider-rowversion-自动管理可行性c7) 已用"客户端递增"措辞锁定方向，递增计数器比随机值更贴合这一既有措辞，且与 SqlServer `rowversion`（单调递增的 8 字节值）语义更接近，便于跨 Provider 契约测试（[HD-013](.) 待起草）用统一的"值必须变化"断言覆盖三个 Provider。

## 5. 为什么本 HD 不创建 `InMemoryInkwellDbContext` 子类

[ADR-021 §决策](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 早期文件树曾列出 `InMemoryInkwellDbContext.cs` 作为示意；但 [HD-009 §6 step 3](HD-009-Inkwell.Persistence.EFCore-base.md#6-builder-dsl-衔接adr-021-builder-dsl-形状)（更具体、更晚锁定的契约）明确写"final adapter 扩展方法内部调 `services.AddDbContext<InkwellDbContext>(...)`"——直接注册 base 类型，不是派生类型。本 HD 遵循 §6 的具体契约：

- v1 范围内三 Provider 的 schema 强制取最小公倍数（[ADR-021 §Provider-specific 字段映射策略](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)），InMemory 没有需要追加的 Provider-specific `OnModelCreating` 逻辑
- 少一层子类 = 少一处可能漂移的重复注册面（[HD-009 §3.1](HD-009-Inkwell.Persistence.EFCore-base.md#31-inkwelldbcontextcs) 的 `OnModelCreating` 是 `virtual`，允许子类叠加，但"允许"不等于"必须"）
- 若后续确有 InMemory 专属模型配置需求，可用一次小 errata 引入子类，不影响本 HD 现有契约（`IDbContextInitializer` / `InMemoryRowVersionInterceptor` 均不依赖是否存在子类）

## 6. Builder DSL 衔接与使用示例

承接 [HD-009 §6](HD-009-Inkwell.Persistence.EFCore-base.md#6-builder-dsl-衔接adr-021-builder-dsl-形状) 契约，本 HD 落地第 2 / 3 步：

```csharp
// Inkwell.WebApi/Program.cs（dev 环境示例，appsettings.Development.json 选 InMemory）
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddInkwell(builder.Configuration)
    .UseInMemoryDatabase()                 // 本 HD 提供；databaseName 默认 "inkwell"
    .AutoSeedOnStartup(true)               // HD-009 提供
    .Build();
```

- `UseInMemoryDatabase()` 与 `UseSqlServer(...)` / `UsePostgres(...)`（[HD-011](.) / [HD-012](.) 待起草）互斥——同一个 `IServiceCollection` 上只应调用其中一个（[HD-001 §6.3](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#63-provider-扩展方法约定给-hd-002--hd-008-的契约)"后调用者覆盖前调用者"）
- 方法名 `UseInMemoryDatabase` 对齐 [ADR-021 §Builder DSL 形状](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) + [HD-009 §12](HD-009-Inkwell.Persistence.EFCore-base.md#12-待补--后续-hd-衔接) 已经写明的预期命名——不是本 HD 新起的名字

## 7. 配置项 / InMemory 数据库命名与隔离策略

本 HD 不引入新的 `PersistenceOptions` 字段（沿用 [HD-002 §3.5](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)），仅新增一个方法参数：

- `databaseName`——默认值 `"inkwell"`；传给 [`UseInMemoryDatabase(databaseName, root)`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.inmemorydbcontextoptionsextensions.useinmemorydatabase)

**隔离策略**（回应"每次进程启动新库还是固定名，避免测试间数据串扰"）：

- 固定字面量默认值 `"inkwell"` 足够——真正决定数据隔离边界的不是名字，而是 [`InMemoryDatabaseRoot`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.inmemorydatabaseroot) 实例。EF Core 官方指南（[Testing without the database § InMemory provider](https://learn.microsoft.com/ef/core/testing/testing-without-the-database#inmemory-provider)）明确：只有共享同一个 `InMemoryDatabaseRoot` 实例 **且** 同名，两个 `DbContext` 才会看到同一份数据；不同 root 即使同名也天然隔离。
- `UseInMemoryDatabase()` 每次调用 `new InMemoryDatabaseRoot()`——即每个独立的 `AddInkwell()...Build()` 调用（无论是一次生产进程启动，还是一个测试 host 实例）都获得专属 root，因此：
  - **dev / prod 场景**：单进程内只调用一次 `AddInkwell()`，`"inkwell"` 这个名字在整个进程生命周期内保持稳定可读——符合"重启前数据持续可见"的直觉预期
  - **测试场景**：每个测试 host（如每个 `WebApplicationFactory` 实例）各自调用一次 `AddInkwell().UseInMemoryDatabase()`，天然获得独立 root，不会跨 host 串扰，不需要测试作者手动传不同 `databaseName`
- 若某个测试场景需要在**同一个** DI 容器内、多个测试方法之间也强制隔离（而非仅按 host 隔离），可选传不同 `databaseName`（如 `UseInMemoryDatabase(Guid.NewGuid().ToString())`）——本方法保留该参数正是为这一更细粒度场景开口子，不需要新增配置字段
- 这一策略是 EF Core 官方文档针对"避免 InMemory 测试间数据串扰"给出的标准解法，参数默认值已覆盖最常见场景，不构成需要 Owner 拍板的产品决策

## 8. 测试要求

### 8.1 测试 csproj 拓扑

- 测试项目：`tests/core/providers/Inkwell.Persistence.EFCore.InMemory.Tests/`（[MSTest.Sdk 4.x](https://github.com/microsoft/testfx) + MTP runner，与 [HD-009 §8.1](HD-009-Inkwell.Persistence.EFCore-base.md#81-测试-csproj-拓扑) 同构）

### 8.2 单测分组

- **`InkwellPersistenceEfCoreInMemoryServiceCollectionExtensionsTests.cs`**：见 §3.1 测试要求
- **`InMemoryDbContextInitializerTests.cs`**：见 §3.2 测试要求
- **`InMemoryRowVersionInterceptorTests.cs`**：见 §3.3 测试要求（含并发冲突核心用例）

### 8.3 跨 Provider 契约测试联动

- 本 HD 的并发冲突用例（§3.3）是 [HD-013](.) 跨 Provider 契约测试包中"并发冲突（`IHasRowVersion`）"用例在 InMemory 侧的具体实现依据；[HD-009 §8.3](HD-009-Inkwell.Persistence.EFCore-base.md#83-跨-provider-行为契约测试前置-hd-010--hd-011--hd-012-起草后启动) 已列出该用例，本 HD 起草完成后 HD-013 可以引用本 HD §3.3 作为 InMemory 侧断言依据
- "Migration 启动"契约用例在 InMemory 侧等价替换为"§3.2 `EnsureCreatedAsync` 建库"（[HD-009 §8.3 / N4](HD-009-Inkwell.Persistence.EFCore-base.md#83-跨-provider-行为契约测试前置-hd-010--hd-011--hd-012-起草后启动) 已锁定此替换关系）

### 8.4 覆盖率门槛

- 全 csproj line coverage ≥ 95%（`InMemoryDbContextInitializer` / `InMemoryRowVersionInterceptor`）；DI 扩展方法 ≥ 90%（对齐 [HD-001 §8.1](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#81-单元测试) 的 Builder 类门槛）

## 9. 部署 / 配置

- 无独立部署单元——本 csproj 是 library，不产 Docker image
- dev `docker-compose`（[ADR-005](../../03-architecture/adr/ADR-005-deployment-docker-compose-aks.md)）不需要为 InMemory Provider 单独起容器——它是进程内数据库，随 `Inkwell.WebApi` / `Inkwell.Worker` 进程存在与消亡
- `appsettings.Development.json` 中 `Inkwell:Providers:Persistence = "InMemory"`（[HD-001 §3.11.1 `InkwellProvidersOptions`](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)）选中本 Provider

## 10. 自动化检查命令

CI（GitHub Actions）在 `dotnet build` 之后执行：

```bash
ROOT=src/core/providers/Inkwell.Persistence.EFCore.InMemory

# C1：不引用 Inkwell.Core
grep -rn 'ProjectReference.*Inkwell\.Core\b' "$ROOT/Inkwell.Persistence.EFCore.InMemory.csproj" \
  && echo "BAD: InMemory adapter must not reference Inkwell.Core" && exit 1

# C2：不引入 SqlServer / Postgres / Migration Design 包（ADR-021 §自动化检查命令 同款）
grep -rn 'Microsoft\.EntityFrameworkCore\.SqlServer\|Npgsql\.EntityFrameworkCore\.PostgreSQL\|Microsoft\.EntityFrameworkCore\.Design' \
  "$ROOT/Inkwell.Persistence.EFCore.InMemory.csproj" \
  && echo "BAD: InMemory adapter must not reference relational/Design packages" && exit 1

# C3：无 Migrations/ 子目录（InMemory 不支持 Migration，ADR-021 D3）
[ -d "$ROOT/Migrations" ] && echo "BAD: InMemory adapter must not have Migrations/" && exit 1

# C4：确认 ProjectReference base + Abstractions 均存在
grep -rn 'ProjectReference.*Inkwell\.Persistence\.EFCore\.csproj\|ProjectReference.*Inkwell\.Abstractions\.csproj' \
  "$ROOT/Inkwell.Persistence.EFCore.InMemory.csproj" \
  || { echo "MISSING required ProjectReference(s)"; exit 1; }

echo "HD-010 automation checks passed."
```

> 脚本物理位置：`scripts/ci/hd-010-checks.sh`（H5 编码任务起草，本 HD 锁脚本契约）。

## 11. 决策记录（继承上游 ADR / HD，本 HD 无新 Owner picker）

- **Migration 策略**：`EnsureCreatedAsync`（非 no-op）——来源 [ADR-021 D3](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)；证据：InMemory Provider 官方文档不支持 Migration tooling
- **Builder DSL 方法名**：`UseInMemoryDatabase(this IInkwellBuilder builder, string databaseName = "inkwell")`——来源 [ADR-021 §Builder DSL 形状](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) + [HD-009 §12](HD-009-Inkwell.Persistence.EFCore-base.md#12-待补--后续-hd-衔接)；证据：两处均已预先点名该方法名，非本 HD 新拍板
- **DbContext 子类化**：不创建 `InMemoryInkwellDbContext`，直接注册 base `InkwellDbContext`——来源 [HD-009 §6 step 3](HD-009-Inkwell.Persistence.EFCore-base.md#6-builder-dsl-衔接adr-021-builder-dsl-形状)；证据：§6 契约具体写明 `AddDbContext<InkwellDbContext>`，v1 无 Provider-specific 模型配置需求
- **RowVersion 值生成机制**：`SaveChangesInterceptor` 内 8 字节大端计数器递增——来源 [EF Core 官方「Application-managed concurrency tokens」](https://learn.microsoft.com/ef/core/saving/concurrency#application-managed-concurrency-tokens) + [design-review-report N5](../design-review-report.md#n5inmemory-provider-rowversion-自动管理可行性c7)；证据：InMemory 无原生 rowversion 生成器，并发检测本身与 Provider 无关、天然生效
- **InMemory 数据库隔离策略**：固定名 `"inkwell"` + 每次调用独立 `InMemoryDatabaseRoot`——来源 [EF Core 官方「Testing without the database」](https://learn.microsoft.com/ef/core/testing/testing-without-the-database#inmemory-provider)；证据：隔离粒度落在 DI 容器（每个进程 / 每个测试 host），无需额外配置

## 12. 待补 / 后续 HD 衔接

- **HD-011 / HD-012**：SqlServer / Postgres final adapter，需各自 `Migrations/` 子目录 + `UseSqlServer` / `UsePostgres` Builder DSL + 各自 `IDbContextInitializer` 实现（走 `MigrateAsync`，非 `EnsureCreatedAsync`）
- **HD-013 跨 Provider 契约测试包**：直接复用本 HD §3.3 并发冲突用例作为 InMemory 侧断言依据；InMemory 侧"Migration 启动"用例替换为本 HD §3.2 `EnsureCreatedAsync` 断言（[HD-009 §8.3 / N4](HD-009-Inkwell.Persistence.EFCore-base.md#83-跨-provider-行为契约测试前置-hd-010--hd-011--hd-012-起草后启动)）
- **`scripts/ci/hd-010-checks.sh`**：§10 自动化检查脚本物化（H5 编码任务）
- **跨 HD 假设已验证（2026-07-06）**：§3.1 注解中提到的"`AddEfCorePersistenceBase()` 是否把 `AuditingSaveChangesInterceptor` 注册为 `ISaveChangesInterceptor` 服务类型"——[HD-009 §13.6 errata·第六轮](HD-009-Inkwell.Persistence.EFCore-base.md#136-2026-07-06-errata第六轮hd-010-首轮评审-design-review-reportmd-19-b17c97-补齐-addefcorepersistencebase-完整代码) 已补齐 `AddEfCorePersistenceBase()` 完整代码，确认按 `ISaveChangesInterceptor` 服务类型注册，假设成立，无需下游再核实

## 13. 同步追加跨模块文件

- [`docs/04-detailed-design/file-structure.md`](../file-structure.md) — 本 HD 同会话追加 `## providers/Inkwell.Persistence.EFCore.InMemory` 一级章节
- 本 HD **不**追加 `database-design.md`（InMemory 不引入新表结构，schema 沿用 [HD-009](HD-009-Inkwell.Persistence.EFCore-base.md) 已锁定的 Entity 定义）/ **不**追加 `api-design.md`（本 HD 不含 HTTP/RPC 端点）
