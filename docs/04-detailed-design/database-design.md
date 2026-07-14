---
id: database-design
title: Inkwell 数据库设计汇总
stage: H3
status: draft
reviewers: []
upstream:
  - ADR-004
  - ADR-021
  - HD-002
downstream: []
---

> 本文件由 H3 起草过的所有持久化相关模块**累加贡献**——每个模块的 HD 在自己 §12 同步追加 `## <module>` 一级章节。**只追加**，禁止删除或改动其他模块章节。
>
> 全文最终在所有持久化 HD（HD-002 / HD-009 / HD-011 / HD-012 + 业务 HD）起草完毕后由人工评审统一翻 `status: reviewed`。

## 总体设计原则（[ADR-004](../03-architecture/adr/ADR-004-data-store-provider-switchable-ef-core.md) + [ADR-021](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）

- **两 Provider 切换**：SQL Server 2025 / PostgreSQL 18，通过 `appsettings.json` 的 `Inkwell:Providers:Persistence` 字段选择（F9：选择器集中在 `Inkwell:Providers` 段，详细连接 / 超时配置在 `Inkwell:Persistence` 段；参 [HD-001 §3.11.1 `InkwellProvidersOptions`](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)）
- **Provider 原生 JSON**：JSON 属性的 CLR 契约统一为 `string` + `System.Text.Json`，物理列由 Provider 映射为 PostgreSQL `jsonb` / SQL Server 2025 `json`；UTC 时间统一 `DateTimeOffset`（[ADR-021 2026-07-13 errata](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md#provider-specific-字段映射策略)）
- **主键策略**：[`Guid` v7](https://learn.microsoft.com/dotnet/api/system.guid.createversion7)（[HD-002 Q2](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)），16 字节 binary 映射
- **命名（F5 解释 A）**：实体属性名 **= 列名 × PascalCase 直接下发**，不做 snake_case 转换。如 `CreatedTime` 属性 → DB 列名也是 `CreatedTime`。HD-009 EFCore base 不引入 `UseSnakeCaseNamingConvention()` 之类的扩展；表名由 `IEntityTypeConfiguration<TEntity>.Configure` 在跨字段路径上显式设置（如 `agents` / `conversations` 是业务表名约定区分于列名约定）。
- **时间戳**：`CreatedTime` / `UpdatedTime` 字段（Model 实现 `IHasTimestamps` mixin，F2），由 EFCore `SaveChangesInterceptor` 自动填充（HD-009）
- **乐观并发**：`RowVersion` 字段（Model 实现 `IHasRowVersion` mixin），EFCore `IsRowVersion()` 自动管理（HD-009）
- **软删除**：v1 **不提供**软删（[HD-002 Q5](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)）
- **enum 存储**（F4）：所有 enum 列一律 `HasConversion<string>().HasMaxLength(64)` 以 `string` 存储（不走 int），保证两 Provider 表现一致；实现在 HD-009 EFCore base
- **Migration**：SqlServer / Postgres 各自 `Migrations/` 子目录（[ADR-021 D3](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）
- **Seed**：`InkwellSeeder.SeedAsync()` 启动时由 Builder DSL `.AutoSeedOnStartup(true)` 默认执行（[ADR-021 D2](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）；幂等

## Inkwell.Abstractions（Persistence 端口）

> 由 [HD-002 §3 / §4](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 锁定。**本节是端口契约**，**不**包含具体表结构（具体表结构由 HD-009 + 各业务 HD 起草）。

### 抽象类型（不入数据库）

`Inkwell.Abstractions/Persistence/` 子目录定义的是**端口契约**，不直接对应数据库对象：

- **`IPersistenceProvider`** — facade interface；暴露 `ExecuteInTransactionAsync` / `SaveChangesAsync`；具名 Repository 由业务 HD 各自定义
- **`IRepository<TModel, TKey>`** — 零成员 marker interface（[ADR-022](../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) + [HD-002 §3.2](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 2026-05-11 errata）；具名 `IXxxRepository` 继承该 marker 以启动 BannedSymbols（F7）锁定 `IXxxStore` / `IXxxDao` / `IXxxGateway` 后缀为禁用；具名动词方法（Add / Update / Get / Delete / List / Find）由具名接口独立声明。
- **`IUnitOfWork`** — 事务作用域；仅在 `ExecuteInTransactionAsync` lambda 内可见
- **`IHasTimestamps`** — mixin；Model 实现 → 表自动加 `CreatedTime` / `UpdatedTime` 列（F2 + F5 解释 A，PascalCase 直入）
- **`IHasRowVersion`** — mixin；Model 实现 → 表自动加 `RowVersion` 列 + `IsRowVersion()`
- **`IHasOwner`** — mixin；Model 实现 → 表自动加 `OwnerUserId` 列 + 索引

### 公共字段映射约定（HD-009 实现锁定，F5 解释 A：PascalCase 直入）

- **`Guid Id`** → DB 列名 `Id`
  - SqlServer：`binary(16)` / Postgres：`bytea`
  - 主键，Guid v7
- **`DateTimeOffset CreatedTime`** → DB 列名 `CreatedTime`（F2）
  - SqlServer：`datetimeoffset` / Postgres：`timestamptz`
  - `IHasTimestamps` 实现时强制 NOT NULL
- **`DateTimeOffset UpdatedTime`** → DB 列名 `UpdatedTime`（F2）
  - SqlServer：`datetimeoffset` / Postgres：`timestamptz`
  - `IHasTimestamps` 实现时强制 NOT NULL
- **`byte[] RowVersion`** → DB 列名 `RowVersion`
  - SqlServer：`rowversion` / Postgres：`xmin` system column
  - `IHasRowVersion` 实现时由 EFCore 自动管理
- **`Guid OwnerUserId`** → DB 列名 `OwnerUserId`
  - SqlServer：`binary(16)` / Postgres：`bytea`
  - `IHasOwner` 实现时强制 NOT NULL + 索引

### 表清单（占位，由 HD-009 + 业务 HD 锁定）

> 以下表清单来自 [architecture.md §4 关键表](../03-architecture/architecture.md)，是 H2 锁定的**业务范围**。每张表的具体字段 / 索引 / 约束由对应业务命名空间 HD 起草，本节仅占位。`triggers` / `orchestrations` / `orchestration_runs` 三张表随触发器（REQ-011）/ 多 Agent 协作编排（REQ-012）于 2026-07-09 推迟至下一期 v2，已从本表移除，详见 [requirements.md §13 第 28 条](../01-requirements/requirements.md)。

<!-- markdownlint-disable MD060 -->

| 表名                 | 业务模块              | 锁定 HD | 说明                                                                                                                                                               |
| -------------------- | --------------------- | ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `users`              | Inkwell.Core.Auth     | HD-014  | [REQ-001](../01-requirements/requirements.md) + [REQ-017](../01-requirements/requirements.md)                                                                      |
| `agents`             | Inkwell.Core.Agents   | HD-015  | [REQ-002](../01-requirements/requirements.md) ~ [REQ-008](../01-requirements/requirements.md)                                                                      |
| `agent_versions`     | Inkwell.Versioning    | HD-015  | [REQ-002 + REQ-015](../01-requirements/requirements.md)                                                                                                            |
| `agent_skills`       | Inkwell.Skills        | HD-020  | [REQ-008](../01-requirements/requirements.md) + [ADR-010](../03-architecture/adr/ADR-010-skill-loading-static-only-v1.md)                                          |
| `agent_tools`        | Inkwell.Tools         | HD-016  | [REQ-007](../01-requirements/requirements.md)                                                                                                                      |
| `knowledge_bases`    | Inkwell.KnowledgeBase | TBD     | [REQ-009](../01-requirements/requirements.md)                                                                                                                      |
| `kb_documents`       | Inkwell.KnowledgeBase | TBD     | [REQ-009](../01-requirements/requirements.md)                                                                                                                      |
| `kb_chunks`          | Inkwell.KnowledgeBase | TBD     | [REQ-009](../01-requirements/requirements.md)                                                                                                                      |
| `memory_items`       | Inkwell.Memory        | TBD     | [REQ-010](../01-requirements/requirements.md)                                                                                                                      |
| `agent_session`      | Inkwell.Conversations | HD-017  | [REQ-010 + NFR-005](../01-requirements/requirements.md)                                                                                                            |
| `agent_chat_message` | Inkwell.Conversations | HD-017  | [REQ-010 + NFR-005](../01-requirements/requirements.md)                                                                                                            |
| `traces`             | Inkwell.Traces        | TBD     | [REQ-014](../01-requirements/requirements.md)                                                                                                                      |
| `agui_run_events`    | Inkwell.Conversations | TBD     | [ADR-011](../03-architecture/adr/ADR-011-auto-lock-with-inflight-task-survival.md) + [ADR-012](../03-architecture/adr/ADR-012-client-server-protocol-rest-agui.md) |
| `public_api_tokens`  | Inkwell.PublicApi     | TBD     | [ADR-007](../03-architecture/adr/ADR-007-public-api-token-auth.md)                                                                                                 |

<!-- markdownlint-enable MD060 -->

### 错误处理（2026-05-12 errata · [ADR-023 errata·01](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) 废 INK-PERSIST-NNN 错误码机制）

[ADR-023 errata·01](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) accepted by Inkwell 2026-05-11 后，`INK-PERSIST-001 ~ 013` 13 条错误码全部作废；Persist 端口错误处理统一走 .NET BCL 异常类型 + OTel `exception.*` 五字段。

> **详 [HD-002 §4.3 BCL 对照表](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#43-错误处理细化-hd-001-53-bcl-对照表) + [HD-009 §4.3 EFCore Provider 实现补充](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)**——表体不在本文件重复锁定，避免多点漂移。

## providers/Inkwell.Persistence.EFCore（EFCore base 实现）

> 由 [HD-009 §1 ~ §10](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) 锁定。**本节是实现锚点**——具体表结构（每张表的字段 / 索引 / 约束 / 关系）仍由 18 张业务表对应的业务命名空间 HD 各自起草后增量贡献。

### 三 mixin 自动配置规则

| Mixin            | EFCore 行为（由 `InkwellDbContext.OnModelCreating` 反射扫描自动应用）                                | 字段配置                                                                                                            |
| ---------------- | ---------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| `IHasTimestamps` | 扫描实现该 mixin 的 entity type，对 `CreatedTime` / `UpdatedTime` 调 `IsRequired()`                  | `DateTimeOffset` UTC；Provider 列类型由 final adapter override：SqlServer `datetimeoffset` / Postgres `timestamptz` |
| `IHasRowVersion` | 扫描实现该 mixin 的 entity type，对 `RowVersion` 调 `IsRowVersion()`                                 | SqlServer `rowversion` / Postgres `xmin` system column                                                              |
| `IHasOwner`      | 扫描实现该 mixin 的 entity type，对 `OwnerUserId` 调 `IsRequired()` + `HasIndex(e => e.OwnerUserId)` | `Guid` v7；`AuditingSaveChangesInterceptor` 校验 `!= Guid.Empty`                                                    |

### SaveChangesInterceptor 行为（HD-009 §3.3）

`AuditingSaveChangesInterceptor`（继承 `SaveChangesInterceptor`）在每次 `SaveChangesAsync` 前对 `ChangeTracker.Entries()` 中实现三 mixin 的 entity 执行：

- `EntityState.Added`：`IHasTimestamps` → 写 `CreatedTime = clock.GetUtcNow()` 且 `UpdatedTime = clock.GetUtcNow()`（覆盖业务默认值）
- `EntityState.Modified`：`IHasTimestamps` → 仅写 `UpdatedTime = clock.GetUtcNow()`；`CreatedTime` 在 `ChangeTracker` 中标记为 `IsModified = false`（防业务越权改写）
- `EntityState.Added` / `Modified`：`IHasOwner` 校验 `OwnerUserId != Guid.Empty`，否则抛 `InkwellException("INK-PERSIST-012", "MissingOwner")`

时钟来源走 [`TimeProvider`](https://learn.microsoft.com/dotnet/standard/datetime/timeprovider-overview) DI 注入，便于单测 mock 时间。

### AutoSeed 幂等模式（HD-009 §3.4）

`InkwellSeeder.SeedAsync()` 在 `MigrationRunner` 完成 Migration 后调（条件 `PersistenceOptions.AutoSeedOnStartup = true`，默认 true，[ADR-021 D2](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）。每个 seed 段必须满足：

- 先查业务唯一键（如 `users.Email`），不存在则 `Add`；**禁**用 `Id` 主键判定（Id 由 DB 生成 / Guid v7 时戳不可作幂等键）
- 段失败 → 包成 `INK-PERSIST-006`（不重试，K8s probe 触发 pod 重启）
- 双跑（同 schema 二次跑 Seeder）→ 零插入

### 跨 Provider 字段映射策略（HD-009 §3.0 + ADR-021 2026-07-13 errata）

- JSON 属性在共享 Entity 中保持 `string`，序列化统一使用 `System.Text.Json`；物理列按当前 Provider 映射为 PostgreSQL `jsonb` / SQL Server 2025 `json`
- 两套独立 migration 负责物理类型差异；从文本列升级时校验并转换已有 JSON，非法数据阻止 migration
- JSON 与既有索引策略之外仍采用最小公倍数 schema；UTC 时间统一 `DateTimeOffset`
- enum 列统一 `HasConversion<string>().HasMaxLength(64)`（[HD-002 §5.1.1 F4](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)）
- Provider-specific 列类型覆写（如 SqlServer `INCLUDE` 索引 / Postgres `WHERE` 过滤索引）在 final adapter csproj 各自的 `OnModelCreating` override 中追加，base **不**写

## Errata 记录（2026-05-10）

本文件 `status: draft` 期间，根据 [`design-review-report.md` §6.4](design-review-report.md) Owner 二次答复一次性落以下变更（已嵌入"总体设计原则"+"Inkwell.Abstractions（Persistence 端口）"两节，本节是变更摘要）：

- **F2 时间字段命名**：公共字段映射约定 `created_at_utc` / `updated_at_utc` → `CreatedTime` / `UpdatedTime`；类型保持 `DateTimeOffset` UTC
- **F5 列命名解释 A**：实体属性 = 列名 × PascalCase 直接下发，不做 snake_case 转换；HD-009 EFCore base 不引入 `UseSnakeCaseNamingConvention()`；表名仍由 `IEntityTypeConfiguration<TEntity>.Configure` 在跨字段路径上显式设置
- **F4 enum 存储约定**：所有 enum 列一律 `HasConversion<string>().HasMaxLength(64)` 以 `string` 存储，保证三 Provider 表现一致；实现在 HD-009 EFCore base
- **F9 Provider 配置键**：`Inkwell:Persistence:Provider` → `Inkwell:Providers:Persistence`（选择器集中段）；详细连接 / 超时 / Seed 配置仍在 `Inkwell:Persistence:*`
- **B2 INK-PERSIST-013**：保持 13 行表（与 [HD-002 §3.10](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 常量段对齐）

## Errata 记录（2026-05-11）

本文件 `status: draft` 期间，根据 [ADR-022 entity-domain-mapper-selection](../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 锁定的术语 + Repository 形态 + 命名规则一次性落以下变更（已嵌入总体设计原则 + Inkwell.Abstractions 端口两节）：

- **术语 Domain → Model 全文翻面**：总体设计原则与 Inkwell.Abstractions 端口两节中的 `Domain` 一律改为 `Model`（如 “Domain 实现 mixin” → “Model 实现 mixin”）。业务 Model 默认无后缀（详 [HD-002 §4.1.2](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)）；撞名场景降级 `XxxDefinition`（当前已知 `AgentDefinition` / `ToolDefinition` / `SkillDefinition` / `TriggerDefinition`）。
- **§Inkwell.Abstractions · IRepository 形态更新**：`IRepository<TDomain, TKey>` generic CRUD base → `IRepository<TModel, TKey>` 纯 marker interface（零成员）；具名动词方法（`AddAgent` / `GetAgent` / 等，动词白名单 = Add / Update / Get / Delete / List / Find）由具名接口独立声明。
- **INK-PERSIST 错误码表语义补注**：表第三列中 `GetByIdAsync` / `UpdateAsync` / `AddAsync` 为示例泛指（历史描述）；实际具名 Repository 方法不带 `Async` 后缀 + 动词取白名单 Add / Update / Get / Delete / List / Find（如 `AddAgent` / `GetAgent`），详见 [HD-002 §4.1.3](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)。表体未修改以保证 lint 列宽与历史评审引用一致。
- **Mapper 选型锁定**（[ADR-022 §决策](../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md)）：所有 Entity ↔ Model 转换走手写 `XxxMappingExtensions` 静态类的扩展方法三件套（`ToModel()` / `ToEntity()` / `SelectAsModel()`）；禁止引入 AutoMapper / Mapster / Riok.Mapperly 等运行时或 SourceGen 框架。具名 Mapping 扩展物理位置：`providers/Inkwell.Persistence.EFCore/Mapping/`（HD-009 起草）。

## Errata 记录（2026-05-12：ADR-023 三轮 errata 跨 HD 同步）

本文件 `status: draft` 期间，根据 [ADR-023 主决策](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) + [errata·01](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) + [errata·02](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 三轮 accepted by Inkwell 2026-05-11，同步落以下变更（已嵌入 Inkwell.Abstractions §错误处理节，本节是变更摘要）：

- **`### 错误码 INK-PERSIST-NNN 段` 13-row 表废除**（[ADR-023 errata·01](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流)）：`INK-PERSIST-001 ~ 013` 13 条错误码全部作废；节标题改为 `### 错误处理`，表体改为指向 [HD-002 §4.3 BCL 对照表](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#43-错误处理细化-hd-001-53-bcl-对照表) + [HD-009 §4.3 EFCore Provider 实现补充](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) 的单行引用（避免多点漂移）
- **错误传递机制**：Persist 端口与全部 EFCore Provider 错误处理统一走 .NET BCL 异常类型（`KeyNotFoundException` / `InvalidOperationException` / `TimeoutException` / `IOException` / `ArgumentException` / `OperationCanceledException` + `InkwellConfigurationException` / `InkwellBuilderException` 两保留子类）；OTel `exception.type` / `.message` / `.stacktrace` / `.escaped` / `.id` 五字段取代 `error.code`。
- **业务命名空间零 `Result<T>` / `Error` 引用**（[ADR-023 errata·02](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常)）：`Common/Result.cs` + `Common/Error.cs` 抽象删除；具名 Repository 方法全裸 `Task<T>` / `Task<bool>` / `Task<PagedResult<T>>` / `Task<IReadOnlyList<T>>` / `Task`（[ADR-023 主决策](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)）。
- **上游证据链**：[HD-001 §13 第三 / 第四轮 errata](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#13-errata-记录) + [HD-002 §13.3 / §13.4 errata](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#13-errata-记录-2026-05-10) + [HD-009 §13.3 / §13.4 errata](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) + [file-structure.md 末尾 errata 记录（2026-05-12）](file-structure.md)。

## Inkwell.Core.Auth

> 由 [HD-014 §5](Inkwell.Core/HD-014-Inkwell.Core.Auth.md#5-数据库设计增量追加至-database-designmd) 锁定。本节是 H3 第一张业务命名空间贡献的表结构（此前"表清单"占位表中 `users` 行为 `TBD`，现更新为 `HD-014`）。

### 表 `users`（[REQ-001](../01-requirements/requirements.md) + [REQ-017](../01-requirements/requirements.md) 解封子能力）

- `Id`：`Guid` v7，主键
- `Username`：`string`，唯一索引，长度上限 100（作者判断，非 Owner 拍板，需求未指定具体上限）
- `PasswordHash`：`string`，无业务长度上限（容纳未来任意算法输出；算法本体 = PBKDF2，2026-07-06 Owner 确认，见 [HD-014 §6.1](Inkwell.Core/HD-014-Inkwell.Core.Auth.md#61-已解决问题原需要-owner-确认2026-07-06-已由默认-agent-通过-vscode_askquestions-真实确认)）
- `IsSuper`：`bool`，默认 `false`（[OQ-007 closed §C](../01-requirements/open-questions.md#oq-007-团队角色暂不区分的下游含义)，仅由后端运维 SQL 设置，无 API 可写）
- `IsLocked`：`bool`，默认 `false`
- `FailedUnlockAttempts`：`int`，默认 `0`（[UF-002](../01-requirements/user-flow.md#uf-002-自动锁定与解锁) 解锁失败计数，阈值 5 次，[HD-014 §1.3 Q3](Inkwell.Core/HD-014-Inkwell.Core.Auth.md#13-关键决策摘要)）
- `LastLoginTime`：`DateTimeOffset?`，可空
- `CreatedTime` / `UpdatedTime`：`IHasTimestamps`
- `RowVersion`：`IHasRowVersion`（乐观并发，防"失败计数递增"与"Admin 解封"并发写互相覆盖）

**索引**：`Username` 唯一索引；`IsLocked` 非唯一索引（[UF-012](../01-requirements/user-flow.md#uf-012-admin-解封账号) 账号 tab 默认过滤已锁账号）。

**Entity / Mapping / Repository 实现物理位置**：`providers/Inkwell.Persistence.EFCore/{Entities,Mapping,Repositories}/`（[ADR-021](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) + [ADR-022](../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 锁定物理位置）——**本节仅记录契约缺口**，具体实现需通过 errata 追加到已 reviewed 的 [HD-009](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)，本次提交不改写 HD-009。

## Inkwell.Core.Agents

> 由 [HD-015 §6](Inkwell.Core/HD-015-Inkwell.Core.Agents.md#6-数据库设计增量追加至-database-designmd) 锁定。本节是 H3 第二张业务命名空间贡献的表结构（此前"表清单"占位表中 `agents` 行为 `TBD`，现更新为 `HD-015`）。

### 表 `agents`（[REQ-002](../01-requirements/requirements.md) ~ [REQ-008](../01-requirements/requirements.md)）

- `Id`：`Guid` v7，主键
- `OwnerUserId`：`Guid`（`IHasOwner`），非唯一索引
- `IsShared`：`bool`，默认 `false`
- `SharedRevokedByAdminTime`：`DateTimeOffset?`，可空（[AC-068](../01-requirements/acceptance-criteria.md) Admin 撤销共享状态条触点）
- `CurrentPublishedVersionId`：`Guid?`，当前默认运行版本指针；首次发布前为空
- `DraftVersionId`：`Guid?`，Owner 唯一可编辑草稿指针；无草稿时为空
- `LatestPublishedVersionNumber`：`int`，首次发布前为 `0`，每次发布或回滚递增
- `CreatedTime` / `UpdatedTime`：`IHasTimestamps`
- `RowVersion`：`IHasRowVersion`（乐观并发）

**索引**：`OwnerUserId` 非唯一索引（"我的" tab 查询路径）；`IsShared` 非唯一索引（"团队共享" tab 过滤路径）；`CurrentPublishedVersionId` 与 `DraftVersionId` 非唯一索引（活动版本批量投影路径）。

Agent 名称、头像、描述、Instructions、模型参数、工具与 Skill 绑定均属于可版本化运行配置，只存于 `agent_versions.Snapshot`，不得在 `agents` 重复存储形成双事实源。

### 表 `agent_versions`（[REQ-002](../01-requirements/requirements.md) + [REQ-015](../01-requirements/requirements.md)）

- `Id`：`Guid` v7，主键
- `AgentId`：`Guid`，外键指向 `agents.Id`；Agent 硬删除时级联删除
- `VersionNumber`：`int`；同一 Agent 内单调递增
- `Status`：`string`，仅 `Draft` / `Published`
- `Snapshot`：`string`，完整序列化 `AgentSnapshot`，非空
- `CreatedByUserId`：`Guid`，创建该版本的用户标识
- `ChangeSummary`：`string?`，长度上限 500
- `CreatedTime` / `UpdatedTime`：`IHasTimestamps`
- `PublishedTime`：`DateTimeOffset?`；Draft 为空，Published 非空
- `RowVersion`：`IHasRowVersion`（乐观并发）

**约束与索引**：`(AgentId, VersionNumber)` 唯一索引；`AgentId` 非唯一索引。服务层保证每个 Agent 至多一个 Draft，并在同一事务内更新版本与 `agents` 指针。Published 行一经发布不可修改；回滚通过复制历史 Snapshot 创建新的 Published 行完成。

**2026-07-06 已解决**：本表**不**包含软删除字段，遵循已 reviewed 的 [HD-002 §1.3 Q5](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#13-关键决策摘要)"v1 不提供软删"。原与 [requirements.md §8.3](../01-requirements/requirements.md) / [ui-spec.md §3.5](../01-requirements/ui-spec.md)"30 天回收期可恢复"字面承诺冲突，Owner 已拍板维持硬删除，需求文档已同步 errata 修正，详见 [HD-015 §8](Inkwell.Core/HD-015-Inkwell.Core.Agents.md#8-需要-owner-确认的问题)。

**Entity / Mapping / Repository 实现物理位置**：`providers/Inkwell.Persistence.EFCore/{Entities,Mapping,Repositories}/`（[ADR-021](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) + [ADR-022](../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 锁定物理位置）——**本节仅记录契约缺口**，具体实现需通过 errata 追加到已 reviewed 的 [HD-009](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)，本次提交不改写 HD-009。

## Inkwell.Core.Tools

> 由 [HD-016 §6](Inkwell.Core/HD-016-Inkwell.Core.Tools.md#6-数据库设计增量追加至-database-designmd) 锁定。本节是 H3 第三张业务命名空间贡献的表结构。

### 表 `agent_tools`（[REQ-007](../01-requirements/requirements.md)）

- `Id`：`Guid` v7，主键
- `Name`：`string`，唯一索引，长度上限 100（作者判断，非 Owner 拍板，需求未指定具体上限）
- `Description`：`string`，无长度上限
- `ParametersJsonSchema`：`string`，无长度上限（[JSON Schema](https://json-schema.org/) 文本）
- `CreatedTime` / `UpdatedTime`：`IHasTimestamps`

**索引**：`Name` 唯一索引。**不**包含 `RowVersion`（[HD-016 §1.3 Q3](Inkwell.Core/HD-016-Inkwell.Core.Tools.md#13-关键决策摘要)，v1 无运行期 Update 场景）；**不**包含 `OwnerUserId`（系统级目录，非用户私有资源）。

**2026-07-07 已解决**：本表 v1 是否需要运行期管理 API（Admin CRUD）以及具体内置工具清单是否需要在 v1 落地，此前均未拍板；Owner 已在对话中直接明确确认——维持只读目录设计（不补 CRUD API），且 v1 需要至少一个真实可用的内置工具（已落地 `get_current_datetime`，详见 [HD-016 §3.12](Inkwell.Core/HD-016-Inkwell.Core.Tools.md#312-inkwellcoretoolscurrentdatetimetoolexecutorcs) + [§6.1 Seed 数据](Inkwell.Core/HD-016-Inkwell.Core.Tools.md#61-tools-表-seed-数据2026-07-07-新增)），详见 [HD-016 §8 Q&A-A / Q&A-C](Inkwell.Core/HD-016-Inkwell.Core.Tools.md#8-需要-owner-确认的问题)。

**Entity / Mapping / Repository 实现物理位置**：`providers/Inkwell.Persistence.EFCore/{Entities,Mapping,Repositories}/`（[ADR-021](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) + [ADR-022](../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 锁定物理位置）——**本节仅记录契约缺口**，具体实现需通过 errata 追加到已 reviewed 的 [HD-009](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)，本次提交不改写 HD-009。

## Inkwell.Core.Skills

> 由 [HD-020 §6](Inkwell.Core/HD-020-Inkwell.Core.Skills.md#6-数据库设计增量追加至-database-designmd) 锁定。本节是 H3 第七张业务命名空间贡献的表结构。

### 表 `agent_skills`（[REQ-008](../01-requirements/requirements.md) + [ADR-010](../03-architecture/adr/ADR-010-skill-loading-static-only-v1.md)）

- `Id`：`Guid` v7，主键
- `Name`：`string`，长度上限 100（作者判断，非 Owner 拍板，需求未指定具体上限），**不加唯一约束**（[HD-020 §1.3 Q4](Inkwell.Core/HD-020-Inkwell.Core.Skills.md#13-关键决策摘要)，Skill 库允许多个成员各自上传同名 Skill）
- `Description`：`string`，无长度上限
- `ContentMarkdown`：`string`，无长度上限（SKILL.md 正文，frontmatter 之外的部分）
- `ReferenceFileUris`：`string`，默认 `"[]"`（`references/` 附件 `Uri` 集合序列化存储）
- `AssetFileUris`：`string`，默认 `"[]"`（`assets/` 附件 `Uri` 集合序列化存储）
- `CreatedTime` / `UpdatedTime`：`IHasTimestamps`

**索引**：无。**不**包含 `RowVersion`（[HD-020 §1.3 Q2](Inkwell.Core/HD-020-Inkwell.Core.Skills.md#13-关键决策摘要)，v1 无运行期 Update 场景）；**不**包含 `OwnerUserId`（Skill 库是全体成员共享的目录，非用户私有资源）。

**Entity / Mapping / Repository 实现物理位置**：`providers/Inkwell.Persistence.EFCore/{Entities,Mapping,Repositories}/`（[ADR-021](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) + [ADR-022](../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 锁定物理位置）——**本节仅记录契约缺口**，具体实现需通过 errata 追加到已 reviewed 的 [HD-009](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)，本次提交不改写 HD-009。

## Inkwell.Core.Conversations

> 由 [HD-017 §6](Inkwell.Core/HD-017-Inkwell.Core.Conversations.md#6-数据库设计增量追加至-database-designmd) 锁定。本节是 H3 第四张业务命名空间贡献的表结构；运行时术语已统一为 Agent Session / Agent Chat Message。

### 表 `agent_session`（[REQ-010 + NFR-005](../01-requirements/requirements.md)）

- `Id`：`Guid` v7，主键
- `AgentId`：`Guid`，外键 → `agents.Id`，非唯一索引
- `AgentVersionId`：`Guid`，外键 → `agent_versions.Id`，创建 Session 时锁定；版本删除受限
- `OwnerUserId`：`Guid`（`IHasOwner`，[HD-017 §1.3 Q1](Inkwell.Core/HD-017-Inkwell.Core.Conversations.md#13-关键决策摘要)，语义 = 会话参与用户，非 `agents.OwnerUserId`），非唯一索引
- `Title`：`string?`，长度上限 30（[ui-spec.md UI-005 §5.1](../01-requirements/ui-spec.md)"首条用户消息前 30 字"）
- `SessionState`：`string?`，由对应版本 `AIAgent.SerializeSessionAsync` 生成；首次运行前可空
- `CreatedTime` / `UpdatedTime`：`IHasTimestamps`（`UpdatedTime` 兼作"最近使用时间"依据）
- `RowVersion`：`IHasRowVersion`（乐观并发）

**索引**：`(AgentId, OwnerUserId)` 复合索引（历史会话侧栏查询路径）；`OwnerUserId` 非唯一索引（"我使用过"聚合查询路径）；`AgentVersionId` 非唯一索引。

### 表 `agent_chat_message`（[REQ-010 + NFR-005](../01-requirements/requirements.md)）

- `Id`：`Guid` v7，主键
- `SessionId`：`Guid`，外键 → `agent_session.Id`，非唯一索引
- `Message`：`string`，完整序列化 `Microsoft.Extensions.AI.ChatMessage`，非空
- `SequenceNumber`：`int`，会话内严格递增
- `CreatedTime` / `UpdatedTime`：`IHasTimestamps`

**索引**：`(SessionId, SequenceNumber)` 唯一复合索引。批量追加在 Serializable 事务内读取最大序号并连续分配。**不**包含 `RowVersion` / `OwnerUserId`（消息一旦写入不可变，非用户直接拥有的独立资源，[HD-017 §1.3 Q7](Inkwell.Core/HD-017-Inkwell.Core.Conversations.md#13-关键决策摘要)）。

**2026-07-08 已解决**（Owner 在本次会话中通过 `vscode_askQuestions` 真实确认，详见 [HD-017 §8](Inkwell.Core/HD-017-Inkwell.Core.Conversations.md#8-需要-owner-确认的问题)）：`ConversationOptions.MaxMessagesPerConversation` 超限行为 v1 **暂不实现**（字段存在但不消费）；`agui_run_events` 表归属判定为**占位过时，实际应归 `Inkwell.Core.Traces`**（未起草，待该 HD 起草时更新本表归属）。原决议另含"删除消息 / 清空对话需要写审计日志"一项，因 2026-07-09 v1 不做审计日志功能的决定已作废。

**Entity / Mapping / Repository 实现物理位置**：`providers/Inkwell.Persistence.EFCore/{Entities,Mapping,Repositories}/`（[ADR-021](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) + [ADR-022](../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 锁定物理位置）——**本节仅记录契约缺口**，具体实现需通过 errata 追加到已 reviewed 的 [HD-009](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)，本次提交不改写 HD-009。

**Entity / Mapping / Repository 实现物理位置**：`providers/Inkwell.Persistence.EFCore/{Entities,Mapping,Repositories}/`（[ADR-021](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) + [ADR-022](../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 锁定物理位置）——**本节仅记录契约缺口**，具体实现需通过 errata 追加到已 reviewed 的 [HD-009](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)，本次提交不改写 HD-009。
