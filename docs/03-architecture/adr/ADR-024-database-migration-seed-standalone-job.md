---
id: ADR-024-database-migration-seed-standalone-job
stage: H2
status: accepted
authors:
  - name: GitHub Copilot
    role: agent
reviewers: [ Inkwell Owner ]
created: 2026-07-09
updated: 2026-07-09
upstream:
  - ADR-004
  - ADR-005
  - ADR-017
  - ADR-019
  - ADR-021
downstream: []
---

# ADR-024 数据库 Migration + Seed 执行方式：独立一次性 Migrator 项目/镜像

## 上下文

[ADR-021 §「Migration / DataSeed 启动行为」](./ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 当前锁定的状态：

- **Migration**：不在应用启动时执行，改由 CI/CD pipeline 独立步骤跑裸 [`dotnet ef database update`](https://learn.microsoft.com/ef/core/cli/dotnet#dotnet-ef-database-update)（2026-07-06 errata，因生产安全考量从「`Inkwell.WebApi` 启动时跑」改出来）。
- **Seed**：`InkwellSeeder.SeedAsync()` 仍在 `Inkwell.WebApi` 启动时由 `MigrationRunner` 调用，通过 `.AutoSeedOnStartup(bool)` 开关控制（default true），未受 2026-07-06 errata 影响。

这个状态本身有三处站不住脚的地方：

1. **CI/CD 跑裸 CLI 需要额外环境**：`dotnet ef database update` 要求执行环境装有 .NET SDK + EF 工具 + 项目源码，且执行者需要直接持有生产数据库的 schema 修改权限。这与「构建一次镜像、到处部署」的容器化流水线是两套机制，"哪次 CI 运行对应哪个镜像 tag" 难以追溯。
2. **Seed 多副本竞态是已记录的真实风险**：[risk-analysis.md](../risk-analysis.md) 已经写明——`InkwellSeeder.SeedAsync()` 默认随 `Inkwell.WebApi` 启动执行，[ADR-005 HPA min 2](./ADR-005-deployment-docker-compose-aks.md) 意味着多副本同时启动/pod 重启圈都可能并发跑 Seed，目前完全依赖幂等判定（按业务唯一键 `if not exists`）兜底正确性，架构上没有消除并发本身。
3. **与 Microsoft 官方指导不一致**：[EF Core「Applying Migrations」文档](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying) 明确不建议在应用运行时调用 `Database.MigrateAsync()`（即便 EF 9+ 加了迁移锁），理由包括应用进程需要拿到 schema 修改的高权限、SQL 未经独立审阅就直接执行、不便回滚；官方推荐的容器化场景做法是生成 [Migration Bundle](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying#bundles)（自包含单文件可执行程序）或等价的「独立制品跑一次」形态。

Owner 在本 ADR 起草会话中通过 picker 拍板：**Migration 与 Seed 一起挪出，合并进一个新增的独立一次性 Migrator 项目/镜像**，不再分别处理。

## 决策

**新增 `src/core/Inkwell.Migrator/` 控制台项目**，作为独立的一次性可执行程序，专职「把数据库 schema 迁移到最新 + 跑幂等 Seed」，运行完成即退出，不常驻。

### 物理结构

```text
src/core/
├── Inkwell.Abstractions/
├── Inkwell.Core/
├── providers/                  ← 8 csproj（不变）
├── Inkwell.WebApi/
├── Inkwell.Worker/
└── Inkwell.Migrator/            ← 新增：一次性 Migration + Seed 入口
```

- 核心 csproj 数：12 → 13（不含测试项目）。
- `Inkwell.Migrator` 依赖：`Inkwell.Persistence.EFCore`（base，取 `MigrationRunner`/`InkwellSeeder`）+ `Inkwell.Persistence.EFCore.SqlServer` + `Inkwell.Persistence.EFCore.Postgres`（两个 final adapter 都引用，运行时按配置二选一）+ `Microsoft.Extensions.Hosting`/`Configuration`（构建最小 DI 容器，读取连接串与 Provider 选择）。
- **不引用** `Inkwell.Core`：Migrator 只处理 schema + Seed，不涉及业务逻辑，不需要 AI/MAF 相关任何包——是三个入口项目里依赖面最窄的一个。

### 运行逻辑

```csharp
// Inkwell.Migrator/Program.cs（示意）
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

string provider = builder.Configuration["Inkwell:Persistence:Provider"]
    ?? throw new InvalidOperationException("Missing Inkwell:Persistence:Provider.");
string connectionString = builder.Configuration.GetConnectionString("Inkwell")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:Inkwell.");

IInkwellBuilder inkwell = builder.Services.AddInkwell(builder.Configuration);

_ = provider switch
{
    "SqlServer" => inkwell.UseSqlServer(connectionString),
    "Postgres" => inkwell.UsePostgres(connectionString),
    _ => throw new InvalidOperationException($"Unknown Inkwell:Persistence:Provider '{provider}'."),
};

inkwell.Build();

using IHost host = builder.Build();
using IServiceScope scope = host.Services.CreateScope();
MigrationRunner runner = scope.ServiceProvider.GetRequiredService<MigrationRunner>();

await runner.MigrateAsync();
await runner.SeedAsync();
```

- 退出码：成功 `0`；任何异常向上抛，进程以非零退出码结束——Kubernetes Job / Docker Compose 均按退出码判定成败。
- `MigrateAsync()` 与 `SeedAsync()` 顺序执行，两者都是幂等操作（EF 9+ 迁移锁 + `InkwellSeeder` 按业务唯一键判定），Job 意外重跑（重试）也安全。

### 部署形态

- **dev（Docker Compose）**：新增一次性 `migrator` service；`api`/`worker` 通过 [`depends_on.condition: service_completed_successfully`](https://docs.docker.com/reference/compose-file/services/#depends_on) 等待 `migrator` 退出码 0 后再启动，不需要额外脚本胶水。
- **prod（AKS / Helm）**：新增 `pre-install,pre-upgrade` [Helm hook Job](https://helm.sh/docs/topics/charts_hooks/)，`helm.sh/hook-delete-policy: before-hook-creation,hook-succeeded`（失败的 Job 保留现场便于排障，成功的在下次执行前清理）；与 `Inkwell.WebApi`/`Inkwell.Worker` **共用同一镜像 tag**（多阶段 Dockerfile 把三个应用的 publish 产物打进同一镜像，Job 只是用不同的 `command`/`args` 跑 `dotnet Inkwell.Migrator.dll`），Helm 原生保证「Job 跑完才继续滚动升级」的顺序语义。

### 权限收窄（可选，非本 ADR 强制项）

Migrator 独立后，为「schema 迁移用户」和「应用读写用户」分配不同数据库账号成为可能（前者需要 DDL 权限，后者只需 DML）——这是最小权限原则的自然延伸，但 v1 不强制要求立即实施，留作后续加固项记录在「后果」里，不阻塞本 ADR 落地。

### 幂等性保证

Migrator 作为一次性 Job，必须在「Job 因超时/探针/人工原因被重跑」「极端情况下短暂出现两个 Job 实例」两种场景下都保持结果正确：

- **Migration**：`Database.MigrateAsync()` 依赖 EF Core 自身维护的 [迁移历史表](https://learn.microsoft.com/ef/core/managing-schemas/migrations/#applying-migrations-at-runtime)（`__EFMigrationsHistory`），只应用尚未记录的迁移，重复调用天然幂等；EF 9+ 的[迁移锁](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying#migration-locking)在数据库层面防止两个实例并发执行迁移互相破坏。
- **Seed**：`InkwellSeeder.SeedAsync()` 现有实现（`SeedDefaultAdminAsync`）是「先 `AnyAsync` 判存在、不存在再 `Add`+`SaveChangesAsync`」的 check-then-act 模式——单实例顺序执行时完全幂等（第二次运行直接跳过），但**不是**并发安全的：如果出现两个 Migrator 实例同时跑到这一步，两边都可能通过「不存在」检查、都尝试插入，此时数据库侧 `Username` 唯一索引（[`UserEntityConfiguration.cs`](../../../src/core/providers/Persistence/Inkwell.Persistence.EFCore/Configurations/UserEntityConfiguration.cs) 已声明 `HasIndex(x => x.Username).IsUnique()`）会挡住重复数据本身，但落败的一方当前会把这次唯一约束冲突包装成 `InvalidOperationException` 直接向上抛，导致该 Job 实例报错退出——**数据不会错，但 Job 会误报失败**。
  - **本 ADR 要求**：`Inkwell.Migrator` 落地实现时，`InkwellSeeder` 的每个 seed 段都需要捕获「本段对应唯一键的约束冲突」并当作「已被别的实例种过」的正常幂等结果处理（返回 0 条新增，而不是向上抛异常），而不是仅依赖调用前的 `AnyAsync` 预检查。这一点在 H5 编码阶段落地 `Inkwell.Migrator` 时一并修，不需要现在改 `InkwellSeeder.cs`。

## 备选项

### 备选 A（本决议）：独立 `Inkwell.Migrator` 项目 + 与 WebApi/Worker 同镜像的一次性 Job/service

- **被选用**：镜像化制品可追溯到具体版本 tag；Helm hook / Compose `depends_on` 原生支持「等待完成」语义，不需要额外脚本；架构上彻底消除 Seed 多副本竞态（只有一个 Job 实例运行，不是 N 个应用副本）；与 Microsoft 官方「独立制品跑一次」的推荐形态一致。

### 备选 B（维持现状）：Migration 走 CI/CD 裸 `dotnet ef database update`，Seed 继续留 `Inkwell.WebApi` 启动时跑

- **放弃理由**：
  1. CI runner 需要装 .NET SDK + EF 工具 + 项目源码，与「构建一次镜像到处部署」的容器化流水线是两套机制。
  2. Seed 多副本竞态风险（[risk-analysis.md](../risk-analysis.md) 已记录）完全没有解决，仍然依赖幂等判定本身的正确性兜底。
  3. "哪次 CI 运行对应哪个部署" 缺乏镜像 tag 级别的可追溯性。

### 备选 C：只把 Migration 挪进独立 Job，Seed 继续留 `Inkwell.WebApi` 启动时跑

- **放弃理由**：Owner picker 已明确选择「一起挪走」，此处仅存档被放弃的选项——单独挪 Migration 不能解决 Seed 多副本竞态这条已记录的真实风险。

### 备选 D：用 [EF Core Migration Bundle](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying#bundles)（`dotnet ef migrations bundle` 自包含单文件可执行）代替独立 csproj + 镜像

- **放弃理由**：Bundle 是针对单一目标 Provider 生成的产物，本仓库需要同时支持 SqlServer / Postgres 两个 Provider、由部署期配置二选一，用 Bundle 意味着要维护两份 bundle 对应两个 Provider，与既有「单镜像 + 运行时按配置选 Provider」的部署形态不贴合；且 Bundle 产物难以纳入「WebApi/Worker/Migrator 三个入口打进同一镜像」的既有打包习惯（[ADR-019](./ADR-019-process-topology-webapi-worker-split.md) 已确立的模式）。

## 后果

### 正面

- Seed 多副本竞态从架构上被消除——迁移前，Seed 幂等判定的正确性是唯一防线；迁移后，同一时刻只有一个 Migrator Job 实例在跑，不依赖判定逻辑兜底。
- Migration 执行有版本化制品（镜像 tag）可追溯，不再是裸 CI 脚本调用。
- Helm hook / Docker Compose `depends_on` 原生支持「等待完成再启动依赖方」，不需要额外脚本或人工介入的部署步骤。
- 为「迁移用户」和「应用用户」数据库权限分离铺路（可选加固项，见「决策」）。
- `Inkwell.Migrator` 是三个入口项目里依赖面最窄的一个（不碰 `Inkwell.Core`/MAF），构建速度快、镜像攻击面小。

### 负面

- 新增一个 csproj + 镜像 entrypoint：[RISK-015 WebApi/Worker 双进程版本漂移](../risk-analysis.md) 需要扩展为「三产物同镜像 tag 同步」，原先的两方协调升级为三方协调。
- Helm hook 失败时需要新的运维 runbook（Job 失败不会自动重试，需要人工介入排查后重新触发 `helm upgrade`）。
- dev 的 Compose `depends_on.condition: service_completed_successfully` 语法要求 Compose v2.20+，需要确认团队本地 Docker Desktop / CI 里的 Compose 版本达标。

### 中性

- Migrator 的运行时 Provider 选择（`Inkwell:Persistence:Provider` 配置项）是本 ADR 新引入的机制；`Inkwell.WebApi`/`Inkwell.Worker` 目前在 `Program.cs` 里硬编码调用 `.UsePostgres(...)`，尚未做成同样的配置驱动开关——这是两个入口项目已存在的、独立于本 ADR 的小缺口，不在本 ADR 范围内一并修正，如需统一可另开 ADR 或作为后续小改动处理。

## 迁移路径

**breaking change 标记**：是（改变 Migration/Seed 的部署时机与执行载体）。本 ADR 落地（`accepted` 后）需要更新的下游文档：

| 步骤 | 文件 | 改动 | 是否需翻 status |
| --- | --- | --- | --- |
| 1 | [ADR-021 §「Migration / DataSeed 启动行为」+ 2026-07-06 errata](./ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) | 追加 errata：Migration 由裸 CI/CD CLI、Seed 留 WebApi 启动，两条均被本 ADR 取代，指向 ADR-024 | reviewed → 增量 reviewed |
| 2 | [ADR-019 §决策（进程/入口清单）](./ADR-019-process-topology-webapi-worker-split.md) | 补充第三个入口 `Inkwell.Migrator`，说明其是一次性 Job 而非常驻进程 | accepted → 增量 accepted |
| 3 | [ADR-005 §决策 dev / prod](./ADR-005-deployment-docker-compose-aks.md) | dev Compose 加 `migrator` 一次性 service + `depends_on.condition`；prod Helm 加 `pre-install,pre-upgrade` hook Job | accepted → 增量 accepted |
| 4 | [risk-analysis.md RISK-015](../risk-analysis.md) | 从「WebApi/Worker 双进程版本漂移」扩展为「WebApi/Worker/Migrator 三产物同步」 | reviewed → 增量 reviewed |
| 5 | [architecture.md §部署清单](../architecture.md) | 补充 Migrator 入口 | reviewed → 增量 reviewed |
| 6 | [tech-selection.md](../tech-selection.md) | 补一条选型条目（如有六字段表约定） | reviewed → 增量 reviewed |
| 7 | [AGENTS.md §3.1](../../../AGENTS.md) | csproj 数 12 → 13；模块拓扑列表加入 `Inkwell.Migrator` | 签字位（需人工授权） |
| 8 | [adr/README.md](./README.md) | 新增 ADR-024 索引行 | draft 不变 |

## 状态

`accepted` —— 核心方向（Migration + Seed 一起挪出、进独立一次性 Job/镜像）已通过 picker 确认；Owner 已审阅本文档整体内容并确认落地。

## 置信度

`high` —— 双重支撑：(1) [Microsoft 官方 EF Core 文档](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying) 明确不建议应用运行时执行 Migration，推荐独立制品化的部署方式；(2) 本仓库 [risk-analysis.md](../risk-analysis.md) 已记录的 Seed 多副本竞态是真实存在、已写入文档的风险，本决议从架构上直接消除该风险类别。
