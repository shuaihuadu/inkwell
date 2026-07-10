---
id: ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout
stage: H2
status: accepted
authors:
  - name: H2-ArchitectAdvisor
    role: agent
reviewers: [Inkwell Owner]
created: 2026-05-10
updated: 2026-05-10
upstream:
  - REQ-008
  - REQ-009
  - REQ-010
  - REQ-013
  - REQ-014
  - NFR-005
  - ADR-004
  - ADR-017
downstream: []
---

# ADR-021 EFCore Persistence 共享层 + 两 Provider 多层 csproj 布局

> **2026-07-08 增量更新**：本 ADR 原文锁定的三 final adapter（InMemory / SqlServer / Postgres）已被 [ADR-004 同日更新](./ADR-004-data-store-provider-switchable-ef-core.md) 精化：Owner 拍板删除 InMemory Provider（[`Microsoft.EntityFrameworkCore.InMemory`](https://learn.microsoft.com/ef/core/providers/in-memory/) 不支持外键约束，对本地开发 / 单测价值有限）。EFCore family 从 4 csproj（base + 3 final adapter）收敛为 **3 csproj**（base + SqlServer + Postgres 两 final adapter）；本地开发与单元 / 集成测试改用 [Testcontainers](https://testcontainers.com/) 起真实 SqlServer / Postgres 实例。后端总 csproj 数 13 → **12**（providers/ 9 → **8**）。本 ADR 正文已直接删除 InMemory 相关描述，不再保留历史遗迹文字。

## 上下文

[ADR-004 §决策](./ADR-004-data-store-provider-switchable-ef-core.md) 锁定「`IPersistenceProvider` 抽象 + 两 Provider 实现（SQL Server 2025 / PostgreSQL 17）+ Code First + EF Migration」，[ADR-017 §3.1 csproj 树](./ADR-017-backend-module-topology-ports-and-adapters.md) 把两 Provider 直接列在 `src/core/providers/` 下与 Cache / FileStorage / Queue / VectorStore Provider 平级——但**未锁定**：

1. **Entity 类的归属**：业务 Entity（`Agent` / `Conversation` / `Run` / `Trace` / `KnowledgeBase` / `MemoryItem` / `Skill` / `Tool` / `Trigger` / `Orchestration` / `Version` / `User` 等共 ~30 个 Entity）属于关系数据建模，**不应在两 Provider csproj 中重复定义**——重复会导致 schema 漂移、Migration 不一致、新加字段时两处改两遍的 DRY 灾难。

2. **`IPersistenceProvider` 实现位置**：`EfCorePersistenceProvider`（注入 `InkwellDbContext` 的具体实现）应该出现一份还是两份？两份会出现「跨 Provider 切换 = 切实现」错误模型——实际上业务代码逻辑是同一份，仅 `DbContextOptionsBuilder` 选项不同。

3. **共享行为**：DbContext 的 `OnModelCreating` 配置（索引 / 关系 / 转换器 / 软删过滤器）+ DataSeed 逻辑 + Migration runner 工具方法都是 Provider-agnostic，应该集中。

4. **Migration 物理位置**：Migration SQL 文本本身是 Provider-specific（[`varchar(max)`](https://learn.microsoft.com/sql/t-sql/data-types/char-and-varchar-transact-sql) vs [`text`](https://www.postgresql.org/docs/current/datatype-character.html) / `IDENTITY` vs `bigserial` / `JSON` vs `JSONB`），由 [`dotnet ef migrations add`](https://learn.microsoft.com/ef/core/cli/dotnet#dotnet-ef-migrations-add) 在指定 Provider 下生成，**必须**放在该 Provider 自己的 csproj 中（[`MigrationsAssembly`](https://learn.microsoft.com/ef/core/managing-schemas/migrations/projects) 配置）。

Owner 在本 ADR 起草会话中通过 picker 拍板：D1 = A（共享层独立 csproj 与 Provider 平级）；D2 = B（DataSeed 通过 Builder DSL `.AutoSeedOnStartup(bool)` 开关，default true）；D3 = A（SqlServer / Postgres 各自 `Migrations/`）。

## 决策

**EFCore Persistence 拓扑变为 3 层 csproj**——共享 base + 两 final adapter Provider，物理位于 `src/core/providers/` 下平级：

```text
src/core/providers/
├── Inkwell.Persistence.EFCore/                     ← 共享 base（NEW，不接任何 DBMS）
│   ├── Entities/                                   ← 全部业务 Entity 类（~30 个）
│   ├── Configuration/                              ← IEntityTypeConfiguration<T> 实现
│   ├── DbContexts/InkwellDbContext.cs              ← base DbContext（virtual OnModelCreating / OnConfiguring）
│   ├── EfCorePersistenceProvider.cs                ← IPersistenceProvider 唯一实现
│   ├── DataSeeds/InkwellSeeder.cs                  ← 幂等 seed 入口
│   ├── Migrations/MigrationRunner.cs               ← 包装 dbContext.Database.MigrateAsync()
│   └── DependencyInjection/                        ← 共享注入逻辑（不暴露 UseXxx）
├── Inkwell.Persistence.EFCore.SqlServer/           ← final adapter
│   ├── DbContexts/SqlServerInkwellDbContext.cs     ← 继承 base 添加 SqlServer-specific 配置
│   ├── DependencyInjection/UseSqlServer.cs         ← Builder DSL `.UseSqlServer(connectionString)`
│   └── Migrations/                                 ← `dotnet ef migrations add Init -p Inkwell.Persistence.EFCore.SqlServer`
└── Inkwell.Persistence.EFCore.Postgres/            ← final adapter
    ├── DbContexts/PostgresInkwellDbContext.cs      ← 继承 base 添加 Postgres-specific 配置（如 jsonb 字段映射）
    ├── DependencyInjection/UsePostgres.cs          ← Builder DSL `.UsePostgres(connectionString)`
    └── Migrations/                                 ← `dotnet ef migrations add Init -p Inkwell.Persistence.EFCore.Postgres`
```

后端总 csproj 数 [ADR-020](./ADR-020-vector-store-microsoft-extensions-vectordata.md) 锁定的 12 → **13** → 2026-07-08 移除 InMemory adapter 后回落为 **12**（providers/ 8 → 9 → **8**）。

### 依赖规则补充（细化 [ADR-017 §3.2](./ADR-017-backend-module-topology-ports-and-adapters.md)）

ADR-017 §3.2 规则：「`providers/* → 端口层`：只能引用 `Inkwell.Abstractions` + 该 Provider 自身的 SDK；**禁止**引用 `Inkwell.Core`」。本 ADR 补**一条例外**：

- **`Inkwell.Persistence.EFCore.{SqlServer,Postgres}` 允许引用 `Inkwell.Persistence.EFCore`（同 providers/ 下的 base csproj）**——这是 P&A 中"shared adapter base + final adapter"的标准分层，不是越权依赖。
- 例外**仅限** EFCore 家族；其他 Provider 家族（FileStorage / Cache / Queue / VectorStore）若未来出现类似 base 需求，须各自 ADR 申请例外。
- `Inkwell.Persistence.EFCore` base 仍**禁止**引用 `Inkwell.Core` / 其他 providers/ 下兄弟 csproj——保 P&A 单向依赖。

### `IPersistenceProvider` 实现唯一性（D1 = A 的关键含义）

- `EfCorePersistenceProvider` 实现 `IPersistenceProvider` 接口（在 `Inkwell.Abstractions`），位于 `Inkwell.Persistence.EFCore/`——**唯一一份**。
- 两 final adapter csproj **不实现** `IPersistenceProvider`，仅注册 `DbContextOptions<InkwellDbContext>`：
  - SqlServer: `services.AddDbContext<InkwellDbContext>(o => o.UseSqlServer(cs, b => b.MigrationsAssembly("Inkwell.Persistence.EFCore.SqlServer")))`
  - Postgres: `services.AddDbContext<InkwellDbContext>(o => o.UseNpgsql(cs, b => b.MigrationsAssembly("Inkwell.Persistence.EFCore.Postgres")))`
- DI 装配（[ADR-019](./ADR-019-process-topology-webapi-worker-split.md) `Inkwell.WebApi` / `Inkwell.Worker`）通过 Builder DSL 选 final adapter，自动级联注册 `EfCorePersistenceProvider`。

### Builder DSL 形状（H3 锁定具体签名）

```csharp
builder.Services.AddInkwell()
    .UseSqlServer(builder.Configuration.GetConnectionString("Inkwell"))   // 来自 Inkwell.Persistence.EFCore.SqlServer
    .AutoSeedOnStartup(true)                                               // 来自 Inkwell.Persistence.EFCore（默认 true）
    .UseAzureBlob(...)
    .UseRedis(...)
    .UseRedisQueue(...)
    .UseQdrantVectorStore(...)
    .UseAzureOpenAIEmbeddings(...)
    .Build();
```

H3 [HD-001 Inkwell.Abstractions] 必须锁定：(1) `PersistenceOptions` 字段（含 `AutoSeedOnStartup` / `MigrationTimeoutSeconds` / `CommandTimeoutSeconds`）；(2) `UseSqlServer` / `UsePostgres` 两个扩展方法签名（位于各 final adapter csproj，但 fluent return type `IInkwellBuilder` 在 Abstractions 中定义）；(3) `AutoSeedOnStartup(bool)` 扩展方法签名。

### Migration / DataSeed 启动行为

- **Migration**：[ADR-019](./ADR-019-process-topology-webapi-worker-split.md) 锁定的 `Inkwell.WebApi` 启动时（**仅 WebApi**，Worker 跳过）由 `MigrationRunner` 包装调 `dbContext.Database.MigrateAsync()`，SqlServer / Postgres 均走 Provider-specific Migration。
- **DataSeed**：`InkwellSeeder.SeedAsync()` 在 Migration 完成后由 `MigrationRunner` 调；Owner 通过 `.AutoSeedOnStartup(false)` 可关闭（如灾难恢复后只想跑 schema 不想覆盖数据）。Seed 内容必须**幂等**（按业务唯一键 `if not exists then insert`），不能用 `Id` 主键判定（Id 由 DB 生成）。

> **2026-07-06 errata（H3 HD-011 起草期发现，Owner 拍板修订）**：上方「Migration」一条——`Inkwell.WebApi` 启动时由 `MigrationRunner` 包装调 `dbContext.Database.MigrateAsync()`——因生产安全考量（未经独立人工审阅即对生产数据库执行 schema 变更）被修订。**应用启动不再自动执行 Migration**：Migration 改由 CI/CD pipeline（[GitHub Actions](https://github.com/features/actions)，[OQ-A007 closed §A](../open-questions-arch.md) 已锁定）独立步骤执行 `dotnet ef database update`（或等价的预生成脚本 apply），在新版本 `Inkwell.WebApi` / `Inkwell.Worker` 部署**之前**完成；两进程启动代码均不再调用 `Database.MigrateAsync()` / `MigrationRunner` 的 Migration 分支。
>
> 上方「DataSeed」一条不受影响——`InkwellSeeder.SeedAsync()` 仍在 `Inkwell.WebApi` 启动时运行（`.AutoSeedOnStartup` 开关不变），但前提从「随 `MigrationRunner` 完成 Migration 后触发」改为「确认 CI/CD 已将 schema 迁移到位」。
>
> 触发原因：H3 [HD-011 SqlServer final adapter](../../04-detailed-design/Inkwell.Persistence.EFCore/HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md) 起草期发现的生产安全考量；2026-07-06 经 0wner chat picker 拍板确认。同步 errata：[ADR-019](./ADR-019-process-topology-webapi-worker-split.md)、[risk-analysis.md RISK-017](../risk-analysis.md)。
>
> **2026-07-09 errata（[ADR-024](./ADR-024-database-migration-seed-standalone-job.md) 取代本节）**：上方「2026-07-06 errata」描述的两条状态——Migration 走 CI/CD pipeline 裸 `dotnet ef database update`、Seed 仍在 `Inkwell.WebApi` 启动时运行——均已被 [ADR-024](./ADR-024-database-migration-seed-standalone-job.md) 取代。新状态：Migration **与** Seed 合并进独立的一次性 `Inkwell.Migrator` 项目/镜像（dev 走 Docker Compose 一次性 service + `depends_on.condition: service_completed_successfully`；prod 走 Helm `pre-install,pre-upgrade` hook Job），`Inkwell.WebApi` 启动代码不再调用 `MigrationRunner.SeedAsync()`。触发原因：risk-analysis.md 已记录的 Seed 多副本竞态风险 + [Microsoft EF Core 官方文档](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying) 不建议应用运行时执行 Migration；2026-07-09 经 Owner picker 拍板确认。

### Provider-specific 字段映射策略

- v1 两 Provider 的 schema **强制取最小公倍数**——遵循 [ADR-004 §后果·负面](./ADR-004-data-store-provider-switchable-ef-core.md) 锁定的「Provider-specific 特性不能用」规则。即 `OnModelCreating` 中**不**用 `HasColumnType("jsonb")` / `HasColumnType("nvarchar(max)")` 等 Provider-specific 写法；JSON 字段统一用 `string` + `JsonSerializer` value converter；UTC datetime 统一用 `DateTimeOffset`。
- 例外仅限**索引策略**：SqlServer 用 `INCLUDE`，Postgres 用 `WHERE` 过滤索引——这类纯优化指令在 final adapter csproj 的 `OnModelCreating` 重写里加，base 不写。

### Versioning（v1 单向 Migration）

v1 仅支持向上 Migration（`dotnet ef database update`），不引入向下回滚自动化。回滚通过 IT 手动 SQL（备份恢复或 SQL 脚本）。

## 备选项

### 备选 A（D1 = B）：`src/core/Inkwell.Persistence.EFCore/` 与 `Inkwell.Core` 平级

- **理由**：分层更清晰——`providers/` 仅放 final adapter，shared base 升到 core 同级
- **放弃理由**：(1) 与 Owner 直觉描述不一致——Owner 期望 3 个 csproj 平级；(2) Inkwell.Persistence.EFCore 仍是**适配器实现**而非业务核心，与 `Inkwell.Core`（业务逻辑层）平级会暗示它是核心层；(3) 引入 P&A "二层 vs 三层" 的方法论辩论，v1 不值；(4) 与未来若 FileStorage / Cache / Queue / VectorStore 也出现 base 时，那一刻新 csproj 该放哪儿就更难统一

### 备选 B（D1 = C）：不抽公共 csproj，把 Entity 放 `Inkwell.Abstractions`

- **放弃理由**：(1) 违反 [ADR-017 §模块依赖规则](./ADR-017-backend-module-topology-ports-and-adapters.md)「`Inkwell.Abstractions` 零外部包依赖」——Entity 类需 `[Key]` / `[Required]` / 导航属性 → 拉进 `System.ComponentModel.DataAnnotations` 与 EF Core attribute（如 `[Owned]`）；(2) Abstractions 应该是端口（接口）层，不该含具体 Entity 数据建模——concept smell；(3) 两 Provider 各自仍需要 `OnModelCreating` 共享配置 + DataSeed 共享逻辑，问题没解决只是把重复代码从 Entity 转移到 DbContext 配置

### 备选 C（D2 = A）：DataSeed 启动时无条件自动幂等执行

- **放弃理由**：(1) 灾难恢复 / 数据迁移场景需要"只跑 schema 不动数据"的能力；(2) Owner 在不同环境（本地试不同分支）会希望快速重置 seed，自动执行让回归 reproducible 时无法选择；(3) 与 [Identity Seed pattern](https://learn.microsoft.com/aspnet/core/security/authentication/identity) 等业界做法一致：seed 提供 opt-out

### 备选 D（D2 = C）：仅 dev / test 自动 seed，prod 走命令

- **放弃理由**：(1) 与 v1 [NFR-001 用户量级](../../01-requirements/requirements.md) 不匹配——团队内部生产力工具，prod 部署模型简单（[ADR-005 AKS Helm](./ADR-005-deployment-docker-compose-aks.md)），不需要专门的 seed 命令；(2) 引入"环境差异"反模式——dev 跑过的 seed 在 prod 必须重跑，多一次手动步骤就多一次出错机会；(3) Builder DSL `.AutoSeedOnStartup(false)` 已经能覆盖"prod 不想自动 seed"场景，不需 separate command

### 备选 E（D3 = B）：公共层托管所有 Provider Migrations + `MigrationsAssembly` 切换

- **放弃理由**：(1) [`dotnet ef` tooling](https://learn.microsoft.com/ef/core/cli/dotnet) 工作流复杂——`--project` 与 `--startup-project` 双参数 + `MigrationsAssembly` 三方协调，开发者认知负担重；(2) 公共层耦合 SqlServer + Postgres 两套 Migration 文件 → 公共层引用 [`Microsoft.EntityFrameworkCore.SqlServer`](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer/) 与 [`Npgsql.EntityFrameworkCore.PostgreSQL`](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/) 两个重 SDK，违反 base csproj 不接 DBMS 的设计；(3) EF Core 官方 multi-provider 示例都用 A 模式

## 后果

### 正面

- Entity / `OnModelCreating` 共享配置 / DataSeed / IPersistenceProvider 实现仅一份，DRY 严格
- 三 final adapter csproj 仅持「DbContextOptionsBuilder.UseXxx + Migrations + 该 Provider 特定 NuGet」——polished thin wrapper，~50-100 LoC each
- `dotnet ef migrations add` 命令直接指向 final adapter csproj，与 [`dotnet ef` 官方文档](https://learn.microsoft.com/ef/core/cli/dotnet) 工作流完全对齐
- Builder DSL `.AutoSeedOnStartup(bool)` 给 Owner / 运维灾难恢复场景留 escape hatch
- ADR-017 §3.2 依赖规则的"providers/* 互相不依赖"细化为"非 EFCore 家族互相不依赖；EFCore family 内 final adapter → base allowed"——足够明确

### 负面

- providers/* 出现"non-final adapter"是新模式，与 FileStorage / Cache / Queue / VectorStore 家族不对称——Roslyn analyzer / `BannedSymbols.txt` 必须按家族允许 white-list（例外条目，[RISK-017](../risk-analysis.md)）
- v1 Provider-specific schema 优化（如 Postgres `jsonb` / SqlServer 列存索引）被锁定为「最小公倍数」，性能调优受限
- DataSeed 幂等性是开发约束——每次写新 seed 必须用业务唯一键 if-not-exists 模式，CI 必须有 seed-twice 测试

### 中性

- csproj 数 12 → 13 →（2026-07-08）回落为 12
- 两 final adapter csproj 几乎对称（仅 NuGet 包不同）——未来加新 Provider（如 [MySQL Pomelo](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql)）路径明确

## 迁移路径

**breaking change 标记**：是（细化 ADR-004 §决策的 csproj 物理布局；与 ADR-017 §3.1 的 csproj 树有冲突——ADR-017 列三 Provider 平级，本 ADR 加 base 升级为 4 csproj 子家族）。

| 步骤 | 文件                                                                                                          | 改动                                                                                                                                   | 是否需翻 status                |
| ---- | ------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------ |
| 1    | [`ADR-004` §决策 + downstream](./ADR-004-data-store-provider-switchable-ef-core.md)                           | downstream +ADR-021；§决策 line 39-41 加 refinement note                                                                               | 内部增量                       |
| 2    | [`ADR-017` §3.1 csproj 树 + §3.2 依赖规则 + §状态段](./ADR-017-backend-module-topology-ports-and-adapters.md) | csproj 树 providers/ 加 `Inkwell.Persistence.EFCore/`；§3.2 加 EFCore family 例外条款；状态段补 csproj 12 → 13                         | 内部增量                       |
| 3    | [`adr/README.md`](./README.md)                                                                                | 索引表 +ADR-021；依赖树 ADR-017 子节点 +ADR-021                                                                                        | draft（编辑者签字位）          |
| 4    | [`architecture.md` §1 总体图 + §3.1 csproj 树 + §3.2 模块说明 + §3.3 数据访问](../architecture.md)            | csproj 树 providers/ 列表加 `Inkwell.Persistence.EFCore/`；§3.2 providers 数 7 → 8；§3.3 加共享 base 说明                              | reviewed（incremental update） |
| 5    | [`tech-selection.md` §0 + §4 + §22](../tech-selection.md)                                                     | §0 摘要表「关系 + 向量数据」行的关联决策 +ADR-021；§4 段加 refinement note；§22 自检 ADR 数 20 → 21                                    | reviewed（incremental update） |
| 6    | [`risk-analysis.md`](../risk-analysis.md)                                                                     | §0 摘要表 +RISK-017 行；新增 §RISK-017 全文（DataSeed 幂等性 / 最小公倍数 schema 漂移 / EFCore family 例外管理）；§1 自检 16 → 17 风险 | reviewed（incremental update） |
| 7    | [`AGENTS.md` §3.1 + §3.2 + §4](../../../AGENTS.md)                                                            | §3.1 providers/ 加 `Inkwell.Persistence.EFCore/`；§3.2 加 EFCore family 例外；§4 ADR 数 20 → 21                                        | 签字位（需人工授权）           |
| 8    | [HD-001 Inkwell.Abstractions](../../04-detailed-design/)                                                      | `PersistenceOptions` 字段 + `AutoSeedOnStartup` Builder DSL 扩展方法                                                                   | H3 起草阶段                    |
| 9    | [HD-002 Inkwell.Persistence.EFCore](../../04-detailed-design/)                                                | 全套 Entity 类 + `OnModelCreating` 配置 + `EfCorePersistenceProvider` + `InkwellSeeder` 设计                                           | H3 起草阶段（首批 HD 之一）    |

**自动化检查命令**：

```bash
# 检查共享 base csproj 是否被两 final adapter 引用
grep -rn 'ProjectReference.*Inkwell\.Persistence\.EFCore\b' src/core/providers/

# 检查两 final adapter 是否各自有 Migrations/
ls src/core/providers/Inkwell.Persistence.EFCore.SqlServer/Migrations/
ls src/core/providers/Inkwell.Persistence.EFCore.Postgres/Migrations/

# 检查仓库中是否残留 InMemory adapter csproj（应不存在）
test ! -d src/core/providers/Inkwell.Persistence.EFCore.InMemory && echo OK || echo "BAD: InMemory adapter 仍存在"
```

## 状态

`accepted` — 2026-05-10（2026-07-08 由 [ADR-004 同日更新](./ADR-004-data-store-provider-switchable-ef-core.md) 精化：InMemory final adapter 移除）。Owner 在本 ADR 起草会话中通过 3 个 picker 拍板：D1 = A（共享层 csproj 与 Provider 平级在 `providers/`）；D2 = B（DataSeed 通过 Builder DSL 开关 default true）；D3 = A（Migration 在 SqlServer / Postgres 各自 csproj）。本 ADR 是 [ADR-004 §决策](./ADR-004-data-store-provider-switchable-ef-core.md) + [ADR-017 §3.1 csproj 树](./ADR-017-backend-module-topology-ports-and-adapters.md) 的精化（refinement），**不 supersede** 任一前置 ADR。

## 置信度

`high`。

依据：(1) 4 层 csproj 布局是 EF Core 多 Provider 业界标准模式（参考 [ABP Framework Volo.Abp.EntityFrameworkCore.{Mysql,Postgres,SqlServer}](https://abp.io/docs/latest/Entity-Framework-Core) / [IdentityServer4.EntityFramework.Storage](https://github.com/IdentityServer/IdentityServer4) / [Microsoft.AspNetCore.Identity.EntityFrameworkCore](https://learn.microsoft.com/aspnet/core/security/authentication/identity)）——零方法论风险；(2) [`MigrationsAssembly`](https://learn.microsoft.com/ef/core/managing-schemas/migrations/projects) 与 [`dotnet ef`](https://learn.microsoft.com/ef/core/cli/dotnet) tooling 行为可机械验证；(3) Owner 直觉的 D1 / D3 选择与 EF Core 官方多 Provider 示例完全一致。
