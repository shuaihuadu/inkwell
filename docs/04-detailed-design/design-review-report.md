---
id: design-review-report
title: H3 详细设计预审报告 — HD-001 + HD-002 + HD-003 + 跨模块汇总文件
stage: H3
status: reviewed
reviewers:
  - Inkwell
updated: 2026-05-18
upstream:
  - HD-001
  - HD-002
  - HD-003
  - file-structure.md
  - database-design.md
  - REQ-001
  - REQ-002
  - REQ-003
  - REQ-009
  - REQ-013
  - REQ-014
  - REQ-015
  - REQ-016
  - REQ-017
  - NFR-004
  - NFR-005
  - NFR-006
  - ADR-002
  - ADR-004
  - ADR-005
  - ADR-008
  - ADR-009
  - ADR-013
  - ADR-015
  - ADR-017
  - ADR-019
  - ADR-021
downstream: []
---

## 0. 评审范围与基线

- **评审范围**：本轮覆盖已起草的端口层切片
  - [HD-001 Inkwell.Abstractions Foundation](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)（status: draft）
  - [HD-002 Inkwell.Abstractions Persistence Port](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)（status: draft）
  - [file-structure.md](file-structure.md)（status: draft，HD-001 + HD-002 累加章节）
  - [database-design.md](database-design.md)（status: draft，HD-002 首次创建）
- **未起草范围**（per-module 模式下合理 deferral，下文 §1 章节状态会标 partial / missing）：HD-003 ~ HD-008（其余 5 端口 + 向量存储 type-alias） / HD-009（EFCore base） / HD-010 ~ HD-012（三 final adapter） / HD-013（跨 Provider 契约用例包） / 全部业务命名空间 HD（`Inkwell.Core.Auth` / `.Agents` / 等）
- **前置闸门**（按 [h3-detailed-design-reviewer 工作流第一步](../../.he/agents/design-reviewer/AGENT.md)）：
  - [requirements.md](../01-requirements/requirements.md) `status: reviewed` ✅
  - [repo-impact-map.md](../01-requirements/repo-impact-map.md) 存在且 `status: reviewed` ✅
  - `docs/04-detailed-design/` 目录存在且至少含 `file-structure.md` + `database-design.md` ✅
  - **不触发** [io-contracts.md §5 阻塞返回](../../.he/agents/_shared/io-contracts.md)——HD-001 / HD-002 是合理的 per-module slice 切片，目录未"严重偏离" h3-detailed-design.md（其余章节按 partial / missing 标注，由后续 HD 累加贡献）

## 1. 完备性扫描

按 [h3-detailed-design.md §3 / §4 章节列表](../../.he/docs/stages/h3-detailed-design.md) 逐项打分。**章节状态约定**：`pass` = 切片范围内全覆盖；`partial` = 端口层 / 抽象层覆盖、业务 / 跨切片层未覆盖；`missing` = 文件不存在且 HD 切片未涉及。

### 1.1 文件结构 — `pass`

- 端口层 21 `*.cs` 全锁 + 后端 13 csproj 总体拓扑（[ADR-017 / 019 / 021](../03-architecture/adr/)）
- 缺口：HD-003 ~ HD-008 / HD-009 ~ HD-013 / 客户端 features 子层路径仅"建议"占位
- 证据：[file-structure.md "Inkwell.Abstractions" 章节](file-structure.md) + [HD-001 §2](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) + [HD-002 §2](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)

### 1.2 数据库 / 表 / 字段 / 索引 / 约束 — `partial`

- 端口契约层（mixin 驱动公共字段映射约定 + INK-PERSIST 错误码段）锁定
- 缺口：18 张业务表全部 `锁定 HD = TBD`（v1 业务范围由 [architecture.md §4](../03-architecture/architecture.md) 锁定，表级 schema 待业务 HD）
- 证据：[database-design.md "表清单"](database-design.md)

### 1.3 API 请求 / 响应 / 错误码 — `partial`

- 端口接口 21 `*.cs` + INK-CORE 5 + INK-PERSIST 12/13 个错误码
- 缺口：REST endpoint / Public API（[ADR-007](../03-architecture/adr/ADR-007-public-api-token-auth.md)） / AG-UI 端点（[ADR-012](../03-architecture/adr/ADR-012-client-server-protocol-rest-agui.md)）未起草；缺独立 `api-design.md` 汇总
- 证据：HD-001 §3 + HD-002 §3

### 1.4 服务 / 进程 / 后台任务 / 定时 — `partial`

- 主体由 [ADR-019 进程拓扑](../03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md) 锁定（WebApi + Worker 双进程）
- 缺口：业务进程 / DurableTask runner / Trigger 定时未起草；缺独立 `process-design.md`
- 证据：HD 切片本身不涉及；ADR-019 是 H2 锁定层级

### 1.5 每个目录 / 程序文件职责 — `pass`

- HD-001 12 `*.cs` × 10 字段 + HD-002 9 `*.cs` × 10 字段全填，无 `<TBD>`
- 缺口：—
- 证据：[HD-001 §3.1 ~ §3.13](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) + [HD-002 §3.1 ~ §3.10](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)

### 1.6 配置文件字段 / 默认值 — `partial`

- HD-001 §9 `appsettings.json` 顶层壳 + HD-002 §9 `Inkwell:Persistence` 段（含 7 字段 + 默认值）
- 缺口：缺独立 `config-design.md` 汇总；其余端口子段待 HD-003 ~ HD-008
- 证据：[HD-001 §9](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) + [HD-002 §9](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)

### 1.7 日志格式 / 字段 — `partial`

- HD-001 §4.2 OTel 公共字段（7 字段） + HD-002 §4.4 `db.*` 字段（6 字段）
- 缺口：缺独立 `log-design.md` 汇总
- 证据：[HD-001 §4.2](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) + [HD-002 §4.4](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)

### 1.8 监控指标 / 告警策略 — `partial`

- HD-002 §7 三条 Grafana 告警建议（INK-PERSIST-003 / 007 + `transaction.outcome` 比例）
- 缺口：缺独立 `monitoring-design.md`；业务 SLI / SLO 未起草
- 证据：[HD-002 §7](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)

### 1.9 部署步骤 / 回滚 / 备份恢复 — `partial`

- HD-001 §9 + HD-002 §9 K8s Secret 注入；主体走 [ADR-005](../03-architecture/adr/ADR-005-deployment-docker-compose-aks.md)
- 缺口：缺独立 `deployment-design.md`；备份恢复策略未起草（[RISK-004](../03-architecture/risk-analysis.md) v1 单 region SLA 99%）
- 证据：HD-001 §9 + HD-002 §9

### 1.10 性能边界 / 安全边界 / 已知限制 — `partial`

- HD-002 §7 "建议默认值"（`GetByIdAsync` p99 < 30ms 等） + HD-001 §7 性能 / 安全段
- 缺口：缺独立 `performance-boundary.md`；NFR-005 等业务 SLA 未拍板
- 证据：[HD-002 §7](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)

### 1.11 完备性结论

HD-001 / HD-002 在自己声明的 "范围切片" 内 100% 覆盖；`docs/04-detailed-design/` 整体进度合理（端口层基础 + 持久化端口 = 21 / ~120 个未来 `*.cs` 已锁定，~17%）。10 个章节中 2 个 `pass`、8 个 `partial` 是 per-module slice 模式下的预期分布——本报告**不**把"未起草章节"全部计 missing 作为整体 reject 理由，而是 logged 为 partial 由后续 HD 累加。

## 2. 一致性扫描

逐项执行 [h3-detailed-design-reviewer 工作流第三步](../../.he/agents/design-reviewer/AGENT.md) 的 6 类交叉检查。**状态约定**：`FAIL` = 真实不一致且会卡 H4 / H5；`PARTIAL` = 不一致但可在后续 HD 起草时自然消化；`PASS` = 已对齐。

### 2.1 概览

- **C1**（FAIL）— Provider 配置段名（H2 vs H3）
- **C2**（FAIL）— INK-PERSIST 错误码段计数（HD-002 内部三处）
- **C3**（PARTIAL）— `orchestrations` vs `orchestration_graphs` 表名
- **C4**（PARTIAL）— `AuditContext.ActorUserId` 类型 vs `IHasOwner.OwnerUserId`
- **C5**（PARTIAL）— HD-001 §3.11 InkwellOptions 占位类 vs HD-002 §3.5 路径迁移
- **C6**（PARTIAL）— InMemory Migration 行为分支（HD-002 §8.3 vs ADR-021 §5）
- **C7**（PARTIAL）— InMemory Provider RowVersion 自动管理可行性
- **C8**（PASS）— 端口层零外部包约束符合性
- **C9**（PASS）— `ErrorCodes.cs` partial 拓扑
- **C10**（PASS）— 上游 ADR 编号引用真实存在
- **C11**（PASS）— csproj 计数（H2 锁定 13）vs 文件结构总体拓扑
- **C12**（PASS）— 取消传播一致性

### 2.2 详细证据（bullet 子段）

#### C1（FAIL）— Provider 配置段名

- 不一致点：[architecture.md §4 line 207](../03-architecture/architecture.md) 用 `"Inkwell:DataStore:Provider"`；[HD-002 §3.5 / §9](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) + [database-design.md "总体设计原则"](database-design.md) 用 `"Inkwell:Persistence:Provider"`——同一字段两个字面量
- 证据：architecture.md line 207 vs HD-002 §3.5 / §9 vs database-design.md "总体设计原则" line 14

#### C2（FAIL）— INK-PERSIST 错误码段计数

- 不一致点：
  - HD-002 §3.10 `ErrorCodes.Persist.cs` 接口字段定义到 `MissingOwner = "INK-PERSIST-012"`（**12 个常量**）
  - HD-002 §7 文字段说"占用 `INK-PERSIST-013` 名额"添加 `EnableSensitiveDataLoggingForbiddenInProd`
  - database-design.md "错误码 INK-PERSIST-NNN 段"表列了 **13 个**（含第 13 行 `EnableSensitiveDataLoggingForbiddenInProd`）
- 证据：[HD-002 §3.10](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) vs [HD-002 §7](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) vs [database-design.md "错误码 INK-PERSIST-NNN 段"](database-design.md)

#### C3（PARTIAL）— 表名 `orchestrations` vs `orchestration_graphs`

- 不一致点：[architecture.md §4 line 209](../03-architecture/architecture.md) 用 `orchestration_graphs`；[database-design.md "表清单"](database-design.md) 用 `orchestrations`
- 证据：architecture.md line 209 vs database-design.md "表清单"

#### C4（PARTIAL）— `AuditContext.ActorUserId` 类型 vs `IHasOwner.OwnerUserId`

- 不一致点：[HD-001 §3.7](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) `AuditContext.ActorUserId: string`；[HD-002 §3.9](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) `IHasOwner.OwnerUserId: Guid`——若 `ActorUserId` 实际指向 `users.id` 应统一类型
- 证据：HD-001 §3.7 vs HD-002 §3.9

#### C5（PARTIAL）— InkwellOptions 占位类与 PersistenceOptions 路径迁移

- 不一致点：[HD-001 §3.11](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) 文字仍说 `PersistenceOptions` 占位类在 `Options/InkwellOptions.cs` 中；[HD-002 §11](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 显式声明"搬到 `Persistence/PersistenceOptions.cs`"——精化已声明但 HD-001 文字未同步
- 证据：HD-001 §3.11 vs HD-002 §3.5 / §11

#### C6（PARTIAL）— InMemory Provider Migration 行为分支

- 不一致点：[HD-002 §8.3](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 跨 Provider 契约用例列 "Migration 启动"；[ADR-021 §5](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 锁 InMemory **不支持** Migration（仅 [`EnsureCreated`](https://learn.microsoft.com/ef/core/managing-schemas/ensure-created)）
- 证据：HD-002 §8.3 vs ADR-021 §5

#### C7（PARTIAL）— InMemory Provider RowVersion 自动管理可行性

- 不一致点：[HD-002 §3.8](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 描述 "InMemory = 客户端递增 byte[8]"；事实上 [`Microsoft.EntityFrameworkCore.InMemory`](https://learn.microsoft.com/ef/core/providers/in-memory/limitations) **不**实现 `IsRowVersion()` 自动 token——需要 HD-009 在 `SaveChangesInterceptor` 内手动模拟
- 证据：HD-002 §3.8 vs EF Core InMemory 文档

#### C8（PASS）— 端口层零外部包约束符合性

- HD-001 §2 + HD-002 §2 csproj 白名单一致：`Microsoft.Extensions.{DependencyInjection,Configuration,Options,Logging}.Abstractions` + `Microsoft.Extensions.VectorData.Abstractions`（HD-008 起用）
- 未引入 EF / Redis / MAF / Azure / Minio 任何 Provider 包
- 证据：[ADR-017 §依赖规则 line 140](../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) + HD-001 §2 + HD-002 §2

#### C9（PASS）— `ErrorCodes.cs` partial 拓扑

- HD-001 §3.13 占用 `INK-CORE-001` ~ `005`；HD-002 §3.10 占用 `INK-PERSIST-001` ~ `012`（C2 待修后扩到 `013`）；段不重叠
- `partial class ErrorCodes` 跨文件追加合法（[C# Roslyn partial type](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/partial-type)）
- 证据：HD-001 §3.13 + HD-002 §3.10

#### C10（PASS）— 上游 ADR 编号引用真实存在

- HD-001 / HD-002 引用的 ADR-002 / 004 / 008 / 013 / 017 / 018 / 019 / 020 / 021 全部存在于 `docs/03-architecture/adr/`
- 21 条 ADR 全 `accepted`
- 证据：[docs/03-architecture/adr/](../03-architecture/adr/)

#### C11（PASS）— csproj 计数（H2 锁定 13）vs 文件结构总体拓扑

- [AGENTS.md §3.1](../../AGENTS.md) 锁 13 csproj
- [file-structure.md "总体拓扑"](file-structure.md) 列出：core 4 (`Inkwell.Abstractions` + `Core` + `WebApi` + `Worker`) + providers 9 (`Persistence.EFCore` 共享 base + 3 final + `FileStorage` × 2 + `Cache.Redis` + `Queue.Redis` + `VectorStore.Qdrant`) = 13 ✓
- 证据：file-structure.md "总体拓扑"

#### C12（PASS）— 取消传播一致性

- HD-001 §4.3 / §5.3 + HD-002 §4.3 都规定 `OperationCanceledException` 重抛（不转 `Result.Failure`）
- `CancellationToken ct = default` 必带（picker Q6 决策）一致
- 证据：HD-001 §4.3 + HD-002 §4.3

### 2.3 一致性结论

12 项检查中 2 项 `FAIL`（C1 / C2）、5 项 `PARTIAL`（C3 ~ C7）、5 项 `PASS`（C8 ~ C12）。`FAIL` 项是 H4 / H5 真实卡点；`PARTIAL` 项是后续 HD 起草时必须显式处理的悬挂项。

## 3. 反问清单

按 `blocking` / `non-blocking` 分组排序。`blocking` 由 [io-contracts.md §6.1 picker 拍板](../../.he/agents/_shared/io-contracts.md)。

### 3.1 Blocking（必须在 HD-001 / HD-002 翻 `reviewed` 之前回炉修正）

#### B1：Provider 配置段名 `DataStore` vs `Persistence` 不一致（C1）

- **问题**：[architecture.md §4 line 207](../03-architecture/architecture.md) 写 `"Inkwell:DataStore:Provider"`；HD-002 + database-design.md 写 `"Inkwell:Persistence:Provider"`。同一配置 key 给出两个字面量。
- **影响范围**：
  - HD-009 EFCore base 起草时不知 `IConfiguration.GetSection()` 用什么字符串
  - `PersistenceOptionsValidator` 启动期校验绑定路径
  - `appsettings.json` / Helm Chart values 模板
  - H4 测试用例 `IConfiguration` mock 字符串
  - H5 编码任务卡 Verify 命令
  - REQ-013 / REQ-015 配置面所有变更链路
- **建议方向**（**不替设计师下结论，仅给方向**）：
  - 选项 1：以 H3 为准——保留 `Inkwell:Persistence`，发起 architecture.md 精化（追加 errata 条目，不动 H2 主决策）
  - 选项 2：以 H2 为准——HD-002 §3.5 / §9 + database-design.md / file-structure.md 全部回炉改 `DataStore`
  - 选项 3：以 [ADR-021 命名空间 `Inkwell.Persistence.EFCore`](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 为锚——配置段同步走 `Persistence`，明文废弃 H2 文字层 `DataStore` 字面量
- **卡点等级**：**blocking**（picker 2026-05-10 已确认）
- **追溯**：C1

#### B2：HD-002 INK-PERSIST 错误码段计数三处不一致（C2）

- **问题**：
  - HD-002 §3.10 `ErrorCodes.Persist.cs` 接口字段定义：`NotFound` ~ `MissingOwner` 共 **12 个常量**（INK-PERSIST-001 ~ 012）
  - HD-002 §7 文字段：`INK-PERSIST-013 = EnableSensitiveDataLoggingForbiddenInProd` 占用第 13 名额
  - database-design.md "错误码 INK-PERSIST-NNN 段" 表：列了 **13 个**（含第 13 个）
- **影响范围**：
  - H4 `ErrorCodesTests.cs` 全局唯一性 + 正则断言（HD-001 §3.13 + HD-002 §3.10）会按 §3.10 接口字段编译——若 §7 prod 校验逻辑引用未定义的 `ErrorCodes.Persist.EnableSensitiveDataLoggingForbiddenInProd` 常量，H5 起草 HD-009 时直接编译失败
  - database-design.md 已经把 13 个写进了表格——证据不一致
- **建议方向**：
  - 选项 1：HD-002 §3.10 补 `public const string EnableSensitiveDataLoggingForbiddenInProd = "INK-PERSIST-013";` 常量定义，让 §7 + database-design.md 表格 + §3.10 三处对齐
  - 选项 2：把 §7 的 prod 校验逻辑下沉到 HD-009（端口层不锁 `INK-PERSIST-013`，由实现层各自分配 014+），同步把 database-design.md 错误码表去掉第 13 行
- **卡点等级**：**blocking**（picker 2026-05-10 已确认）
- **追溯**：C2

### 3.2 Non-blocking（建议在后续 HD 起草时处理，不阻塞当前两张 HD 翻 `reviewed`）

#### N1：`orchestrations` vs `orchestration_graphs` 表名（C3）

- **问题**：架构层用 `orchestration_graphs`；database-design.md "表清单"用 `orchestrations`；REQ-012 不指定。
- **影响范围**：未来 `Inkwell.Core.Orchestrations` 业务 HD 起草、`Inkwell.Persistence.EFCore` Migration 文件名
- **建议方向**：在起草 `Inkwell.Core.Orchestrations` 业务 HD 时显式拍板（picker），同步 errata 一处文档
- **卡点等级**：non-blocking
- **追溯**：C3

#### N2：`AuditContext.ActorUserId` 与 `IHasOwner.OwnerUserId` 类型分歧（C4）

- **问题**：HD-001 §3.7 `ActorUserId: string`；HD-002 §3.9 `OwnerUserId: Guid`。
- **影响范围**：HD-007 `IAuditLogger` 起草时；将来 `audit_logs` 表 `actor_user_id` 列类型设计；可能漏出系统 actor `"system"` 字面量
- **建议方向**：起草 HD-007 时显式拍板：
  - 选项 1：`AuditContext.ActorUserId` 改 `Guid`（强一致 users.id），系统 actor 用 `Guid.Empty` 或预设保留 GUID
  - 选项 2：保留 `string`，但在 HD-007 + database-design.md 显式声明 `audit_logs.actor_user_id` 列允许字面量 `"system"` / `"trigger"` 等非 Guid 值
- **卡点等级**：non-blocking
- **追溯**：C4

#### N3：HD-001 §3.11 与 HD-002 §3.5 PersistenceOptions 路径漂移（C5）

- **问题**：HD-001 §3.11 文字仍说 `PersistenceOptions` 占位类挂在 `Options/InkwellOptions.cs`；HD-002 §11 已显式精化为搬到 `Persistence/PersistenceOptions.cs`。
- **影响范围**：HD-001 reviewer 翻 `reviewed` 时若不知 §11 已精化，可能误判
- **建议方向**：HD-001 翻 `reviewed` 之前补一行 `> 2026-05-10 §3.11 errata` 声明 PersistenceOptions 路径已被 HD-002 §3.5 精化（或在 HD-001 §3.11 字段表 "依赖模块" 列加引用 HD-002 §3.5）
- **卡点等级**：non-blocking
- **追溯**：C5

#### N4：HD-002 §8.3 跨 Provider 契约用例 "Migration 启动" 在 InMemory 上的语义（C6）

- **问题**：HD-002 §8.3 列 "Migration 启动 + DataSeed 幂等" 作为跨 Provider 契约用例；ADR-021 §5 锁 InMemory 不支持 Migration（仅 `EnsureCreated`）。
- **影响范围**：HD-013 `tests/core/Inkwell.Providers.Contract/Persistence/` 起草时
- **建议方向**：HD-013 起草时把契约用例分两类——Provider-agnostic（CRUD / 并发 / 事务回滚）三 Provider 全跑；Migration-specific（Migration up/down + DataSeed）仅 SqlServer + Postgres 跑，InMemory 用 `EnsureCreated` 等价跑 schema-creation 替代用例
- **卡点等级**：non-blocking
- **追溯**：C6

#### N5：InMemory Provider RowVersion 自动管理可行性（C7）

- **问题**：HD-002 §3.8 说 "InMemory = 客户端递增 byte[8]"，但 EF Core InMemory provider 不实现 `IsRowVersion()` 自动 token——需要 HD-009 在 `SaveChangesInterceptor` 中手动维护 byte[8]。
- **影响范围**：HD-009 EFCore base 起草时；跨 Provider 并发冲突契约用例的 InMemory 路径
- **建议方向**：HD-009 起草时显式描述 `RowVersionInterceptor` 的实现策略（`SaveChangesInterceptor.SavingChangesAsync` 钩子，Domain 实现 `IHasRowVersion` 时手动 `++value`）；HD-002 §3.8 不需要回炉，其文字"客户端递增"已暗示这一约定
- **卡点等级**：non-blocking
- **追溯**：C7

#### N6：HD 累加进度 vs H4 起步前置（章节状态 partial 集合）

- **问题**：完备性表 9 章节中 8 章节 partial / missing。这是 per-module slice 模式预期分布，但 `TestCaseAuthor` 在 H4 起步时需要至少 HD-001 / HD-002 + 第一个业务命名空间 HD（如 `Inkwell.Core.Auth`）才能反推 TC，纯端口层不够。
- **影响范围**：H4 启动条件
- **建议方向**：HD-001 / HD-002 翻 `reviewed` 后**不要**直接进 H4——继续 HD-003 ~ HD-008（其余端口）+ HD-009（EFCore base）+ 至少一张业务 HD（建议 `Inkwell.Core.Auth`，覆盖 REQ-001 + REQ-013，且最小化外部依赖）。当端口层 + 第一张业务 HD 全 `reviewed` 后再进 H4，避免 `TestCaseAuthor` 反复回炉。
- **卡点等级**：non-blocking（**仅是阶段路线建议，不是 HD 内部缺陷**）
- **追溯**：进度建议

## 4. 评审结论

- **HD-001 翻 `reviewed` 前置条件**：N3 errata（同步 §3.11 PersistenceOptions 路径已精化）
- **HD-002 翻 `reviewed` 前置条件**：B2 修正（INK-PERSIST 错误码段三处对齐）
- **跨文档前置条件**（影响 HD-001 / HD-002 + architecture.md / database-design.md / file-structure.md）：B1 拍板（Provider 配置段名）
- **建议路径**：
  1. Owner 拍板 B1 + B2 → AI 单次 errata 落地（不重做 HD，仅追加 errata + 同步表/字段）
  2. Owner 在 HD-001 / HD-002 frontmatter 翻 `status: draft → reviewed` + 填 `reviewers:`（**人工签字位**，AI 不替签）
  3. 继续 HD-003 / HD-009（按 [N6 建议路径](#n6hd-累加进度-vs-h4-起步前置章节状态-partial-集合)）

## 5. 自检

- ✅ 每条 `pass` / `partial` / `FAIL` 都附了文件路径或具体引用
- ✅ `blocking` 反问都能映射到具体 REQ / 一致性冲突 / 编译错误
- ✅ 未使用 "看起来" / "似乎" / "感觉" 等主观词汇
- ✅ 未凭文件名臆测，每条结论都打开了对应文件读到对应字段
- ✅ 未尝试用部分数据写"半个报告"——前置闸门已确认通过
- ✅ 未越界修改 HD-001 / HD-002 / file-structure.md / database-design.md
- ✅ 未给越界建议（如"建议你顺便重构 X"）
- ✅ `blocking` 卡点等级由 picker 拍板（2026-05-10）
- ✅ 报告路径走 H3 规范默认 `docs/04-detailed-design/design-review-report.md`（picker 拍板）

## 6. 评审反馈记录

- **评审人**：Inkwell
- **评审日期**：2026-05-10
- **评审范围**：HD-001 / HD-002 / file-structure.md / database-design.md（含本报告本身）
- **润色说明**：本节由本 reviewer 把 Owner 评审会原文润色为结构化条目，原文意图不变；类型字段对每条标注是 "B1 拍板" / "新命名规范" / "提问答复" / "实现层约束" / "决策点" / "命名约束（预防）" 之一。

### 6.1 反馈逐条

#### F1 — Provider 配置段名走 `Persistence`

- **原文**：Provider 命名走 Persistence
- **类型**：B1 拍板（呼应 [§3.1 B1](#b1provider-配置段名-datastore-vs-persistence-不一致c1) 选项 1）
- **意图**：保留 `Inkwell:Persistence:Provider`；[architecture.md §4 line 207](../03-architecture/architecture.md) `Inkwell:DataStore:Provider` 字面量走 errata 精化（**不**动 H2 主决策）
- **影响范围**：architecture.md §4 errata；HD-002 §3.5 / §9 + database-design.md "总体设计原则" 维持现状（已用 `Persistence`）
- **下一动作**：architecture.md §4 line 207 上方追加 errata 块声明字面量已被 [ADR-021 命名空间](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) + HD-002 精化为 `Inkwell:Persistence`

#### F2 — 时间字段命名走 `XxxTime` + `DateTimeOffset` UTC

- **原文**：时间的命名以 XXXXTime，使用 DateTimeOffset，UTC 时间
- **类型**：新命名规范（全局适用）
- **意图**：所有时间字段统一用 `XxxTime` 后缀（不再 `XxxAtUtc` / `XxxAt`）；类型一律 [`DateTimeOffset`](https://learn.microsoft.com/dotnet/api/system.datetimeoffset)（带时区、值强制 UTC）
- **影响范围**：
  - [HD-001 §3.7](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) `AuditContext.OccurredAtUtc` → `OccurredTime`
  - [HD-002 §3.7](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) `IHasTimestamps.CreatedAtUtc` → `CreatedTime`、`UpdatedAtUtc` → `UpdatedTime`
  - [database-design.md 公共字段映射约定](database-design.md) 列名 `created_at_utc` / `updated_at_utc` 待 F5 澄清后 errata
  - HD-003 ~ HD-008 + 所有业务 HD 起草时遵守本规范
- **下一动作**：HD-001 / HD-002 / database-design.md errata（一次性落地）

#### F3 — Guid v7 结构（提问答复）

- **原文**：Guid v7 是什么样的？
- **类型**：提问答复（[HD-002 Q2 决策](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 无变更）
- **答复**：Guid v7（[RFC 9562 §5.7](https://datatracker.ietf.org/doc/html/rfc9562) + .NET [`Guid.CreateVersion7()`](https://learn.microsoft.com/dotnet/api/system.guid.createversion7)）是 128-bit UUID 的子标准。按位结构：
  - **高 48-bit**：Unix milliseconds timestamp —— 这是 v7 与 v4 的关键差异：让 Guid 主键近似按时间排序，B-tree 索引插入友好，避免 v4 随机分布带来的 [page split](https://learn.microsoft.com/sql/relational-databases/sql-server-index-design-guide#page-split) 风暴
  - **接 4-bit version**：固定值 `7`
  - **接 12-bit rand_a**：随机熵段 1
  - **接 2-bit variant**：固定值 `10`（IETF variant）
  - **接 62-bit rand_b**：随机熵段 2 —— 共 74-bit 随机性，碰撞概率仍可忽略
- **字符串形式**：`019xxxxx-xxxx-7xxx-yxxx-xxxxxxxxxxxx`，其中前 8 位 hex 是时间戳、第三段首字符固定 `7`、第四段首字符 `8/9/a/b`（variant 标记）
- **示例**：`019662ce-79f4-7321-9b7d-3a8f1e2d4c5b`
- **生成代码**：[`Guid.CreateVersion7()`](https://learn.microsoft.com/dotnet/api/system.guid.createversion7)（.NET 9+ 内置，.NET 10 沿用，无需第三方库）
- **与 v4 对比**：v4 全 122-bit 随机，无时间序；v7 牺牲一部分熵换排序，是当前 distributed system 主键的事实标准
- **影响范围**：HD-002 Q2 决策有据可循（无变更）；本条作答复存档

#### F4 — C# 枚举映射为字符串（EFCore `HasConversion`）

- **原文**：如果 C# 是枚举类型，则需要 Mapping 为字符串，然后使用 HasConversion
- **类型**：实现层约束（落 HD-009）
- **意图**：所有 `enum` 属性持久化时**不**走默认 `int` 列，统一 [`HasConversion<string>()`](https://learn.microsoft.com/ef/core/modeling/value-conversions) 映射为字符串列。理由：
  - DB schema 自描述（不需查 enum 定义反推数字含义）
  - 新增 enum 值不破坏 ordinal
  - SQL 工具 / BI 报表 / 跨 DB 工具友好
- **影响范围**：HD-009 `OnModelCreating` 通用约定；[HD-001 §3.5](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) `SortDirection` 枚举若入库按本规则
- **下一动作**：HD-009 起草时锁定全局约定 `enum` 默认 `HasConversion<string>().HasMaxLength(64)`；HD-002 §5 拓扑约束加 errata 引用本规范

#### F5 — CodeFirst + 列命名按 Entity 规范（部分待澄清）

- **原文**：使用 CodeFirst，列命名按照 Entity 规范
- **类型**：约束（CodeFirst 已锁；列命名规则待二次澄清）
- **意图**：CodeFirst 已锁（[ADR-004 §决策](../03-architecture/adr/ADR-004-data-store-provider-switchable-ef-core.md) + [ADR-021 D1](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)），无变更；"列命名按 Entity 规范" 两种解释，Owner 在 HD-009 起草前需选定一种：
  - **解释 A**：列名 = Entity property 名（PascalCase 直接落 DB，如 `CreatedTime`），不做 snake_case 转换 → 与 [database-design.md 公共字段映射约定](database-design.md) 当前写的 `created_at_utc` / `id` snake_case 列名冲突，需 errata
  - **解释 B**：列名遵守 "Entity 规范" 中的具体子规则（如 PK = `Id`、FK = `<Entity>Id`、时间带 `Time` 后缀），不约束大小写 → database-design.md 维持 snake_case 列名
- **影响范围**：database-design.md 公共字段映射约定；HD-009 `OnModelCreating` 列命名策略
- **下一动作**：Owner 在 HD-009 起草前用 picker 拍板 A / B；本 reviewer 不替选

#### F6 — Entity / DTO 分离 + AutoMapper（mapper 选型待决）

- **原文**：Entity 和 DTO 分开，使用 AutoMapper 转换？
- **类型**：决策点（建议起 ADR-022 + HD-009 picker）
- **意图**：Entity（in [`providers/Inkwell.Persistence.EFCore/`](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）与 Domain DTO（in [`Inkwell.Abstractions/Persistence/<Module>/`](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)）物理分离已锁；mapper 选型有三个候选：
  - **A**：[AutoMapper](https://automapper.org/) — 反射 + Source Gen 双模式，社区主流，约定优于配置
  - **B**：[Mapperly](https://mapperly.riok.app/) — 编译期 [Source Generator](https://learn.microsoft.com/dotnet/csharp/roslyn-sdk/source-generators-overview)，零反射、AOT 友好，编译期发现错误
  - **C**：手写 mapper — 每对 Entity / Domain 一对一 `ToDomain()` / `FromDomain()`，可读但维护成本高
- **影响范围**：HD-009 EFCore base；潜在跨业务 HD（业务 HD 是否声明 mapper 接口）
- **下一动作**：建议起 [ADR-022 Entity / Domain mapper 选型] 锁选型，按 H2 ADR 六字段（选什么 / 为什么 / 替代方案 / 放弃理由 / 维护影响 / 成本性能安全交付影响）；HD-009 picker 引用本 ADR；本条**不**翻为 errata（HD-002 范围内 Entity / DTO 分离已锁）

#### F7 — Repository 接口统一 `IXxxRepository`、禁用 `XxxStore`

- **原文**：关系数据库统一使用 IXXX Repository，别使用 XXXX Store
- **类型**：命名约束（已对齐 + 预防强化）
- **意图**：DB 层接口统一 `IXxxRepository` 前缀，禁止 `XxxStore` / `XxxDao` / `XxxGateway` 等替代命名。当前 [HD-002 §3.2 + §4.1](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 已锁 `IRepository<TDomain, TKey>` + `I<Entity>Repository`，但**未显式禁用**其他后缀
- **影响范围**：所有业务命名空间 HD 的 Repository 接口命名（[Auth / Agents / Conversations / 等](../../AGENTS.md)）
- **下一动作**：HD-002 §4.1 errata 加一句"**禁用** `XxxStore` / `XxxDao` / `XxxGateway` 等替代命名；违反由 [Roslyn analyzer / `BannedSymbols.txt`](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/) CI 强制"

#### F8 — Entity 合理使用 Attribute + Fluent API

- **原文**：Entity 合理使用 Attribute 和 Fluent API
- **类型**：实现层约束（落 HD-009）
- **意图**：EFCore 两种 Entity 配置方式合理混用——
  - [Data Annotations Attribute](https://learn.microsoft.com/ef/core/modeling/#use-data-annotations-to-configure-a-model)（如 `[Required]` / `[MaxLength(128)]` / `[Column("col")]`）：单字段简单约束；可读性高，紧贴 Entity class
  - [Fluent API in `OnModelCreating`](https://learn.microsoft.com/ef/core/modeling/#use-fluent-api-to-configure-a-model)（如 `HasIndex(...).IsUnique()` / `IsRowVersion()` / Provider-specific 映射）：跨字段、跨 Entity 关系、Provider-specific 配置；灵活度高，集中管理
- **意图**：两者**不偏废**——简单约束走 Attribute、复杂关系 / Provider-specific 走 Fluent API
- **影响范围**：HD-009 EFCore base 的 "配置策略" 章节
- **下一动作**：HD-009 起草时在 §"配置策略" 锁定 Attribute / Fluent API 切分边界

### 6.2 反馈与本报告 §3 反问清单的合并

- **F1** = §3.1 B1 → 拍板方向 1，待 architecture.md errata
- **F2 / F4 / F5 / F8** = 新增项目（time naming / enum mapping / column naming clarification / Attribute+Fluent 边界）
- **F3** = 提问答复（HD-002 Q2 决策无变更）
- **F6** = 新增决策点 → 建议起 ADR-022 mapper 选型
- **F7** = §3 之外的预防约束（HD-002 §4.1 禁用清单 errata）
- **§3.1 B2**（INK-PERSIST-001 ~ 012 vs 013 计数三处不一致）= **未在本次反馈中拍板，仍 blocking**，等 Owner 二次拍板后落 errata
- **§3.2 N1 ~ N5** = 未在本次反馈中处理，按本报告原结论 non-blocking 流转到 HD-007 / HD-009 / HD-013

### 6.3 评审结论与下一步

- **本评审报告**：`status: draft → reviewed`，`reviewers: [Inkwell]`，`updated: 2026-05-10` ✅（frontmatter 已翻）
- **HD-001 / HD-002 / file-structure.md / database-design.md**：维持 `status: draft`，下列 errata 落地后 Owner 再次签字翻 `reviewed`（详见 §6.4 一次性 errata 计划）。
- **architecture.md**：维持 H2 已 approved；F1 errata 在 §6.4 一次性 errata 范围内追加（不动 H2 主决策）。

### 6.4 Owner 二次答复与一次性 errata 授权（2026-05-10）

Owner 在 §6.1 八条反馈基础上又确认了 **B2 / F5 / F6 / F9（InkwellOptions 形态）** 与 **批量 errata 授权**。下列拍板为最终态，AI 切换到 `h3-detailed-design-author` 模式后**不再二次询问**，直接落地。

#### 6.4.1 Owner 拍板逐条

- **B2 → 选项 1**：HD-002 §3.10 `INK-PERSIST` 常量补到 013，新增 `EnableSensitiveDataLoggingForbiddenInProd = "INK-PERSIST-013"`（与 §7 现有 `INK-PERSIST-013` 引用对齐，§3 表头计数 `001 ~ 012` 同步改为 `001 ~ 013`）。
- **F5 → 解释 A（PascalCase 直入）**：实体属性 = 列名 = `CreatedTime` / `UpdatedTime`，不再做 snake_case 转换；database-design.md "总体设计原则·命名" 段落锁此约束，HD-009 EFCore base 不引入 `UseSnakeCaseNamingConvention()` 之类的扩展。
- **F6 → 选项 A（同期起 ADR-022）**：在 `docs/03-architecture/adr/ADR-022-entity-domain-mapper-selection.md` 起草新 ADR，按 H2 六字段格式（选什么 / 为什么 / 替代方案 / 放弃理由 / 维护影响 / 成本性能安全交付影响）评估 AutoMapper / Mapperly / 手写映射；ADR 评审通过后再启动 HD-009 EFCore base 起草，避免 mapper 选型在 HD-009 内部反复改写。
- **F9（InkwellOptions 形态）→ 形态 C（选择器集中 + 详细独立）**：
  - 顶层新增选择器段 `Inkwell:Providers:{Persistence, Queue, Cache, FileStorage, VectorStore, AgentRuntime}`，值取自 Provider 枚举（如 `InMemory` / `SqlServer` / `Postgres`）。
  - 端口 Options 类（`PersistenceOptions` / `QueueOptions` / 等）**移除 `Provider` 字段**，只保留各自详细配置；详细段仍走 `Inkwell:<Module>:*`（如 `Inkwell:Persistence:ConnectionString`）。
  - Builder DSL `.UsePersistenceXxx()` / `.UseQueueXxx()` 在装配期对照 `Inkwell:Providers:<Module>` 做交叉校验：值不一致或同一 Module 注册两次抛新错误码 **`INK-CORE-006 ProviderRegistrationConflict`**（HD-001 §3.13 ErrorCodes.cs 追加）。
- **批量 errata 授权**：Owner 一次性授权 AI 在 `h3-detailed-design-author` 模式下落 §6.4.2 全部 errata，落完后 HD-001 / HD-002 / file-structure.md / database-design.md / architecture.md 各自重新进入 reviewed 流程。

#### 6.4.2 一次性 errata 计划（按文件聚合，待 author 模式执行）

下列条目的"涉及位置"是 reviewer 视角的指引，author 模式下应以实际章节为准并允许做不破坏语义的微调。

- **`HD-001-Inkwell.Abstractions-foundation.md`**
  - §3.7 `AuditContext` 字段 `OccurredAtUtc` → `OccurredTime`（F2）
  - §3.11 `InkwellOptions.cs` 重塑（F9）：删除子 Options 的 `Provider` 字段；新增 `InkwellProvidersOptions.cs`（或同等 nested class）承载 `Inkwell:Providers:*` 选择器；说明子 Options 仅承载详细字段
  - §3.13 `ErrorCodes.cs` 新增 `INK-CORE-006 ProviderRegistrationConflict`（F9）
  - §9 appsettings.json 示例改写为 `Inkwell:Providers:*` + `Inkwell:<Module>:*` 的并列结构
- **`HD-002-Inkwell.Abstractions-persistence-port.md`**
  - §3.5 `PersistenceOptions` 删除 `Provider` 字段 + 注释指向 `Inkwell:Providers:Persistence`（F9）
  - §3.6 `PersistenceOptionsValidator` 删除 Provider 白名单（F9，校验上移至 Builder DSL 装配期）
  - §3.7 `IHasTimestamps` 字段 `CreatedAtUtc` / `UpdatedAtUtc` → `CreatedTime` / `UpdatedTime`（F2）
  - §3.10 `INK-PERSIST` 常量段补 013 `EnableSensitiveDataLoggingForbiddenInProd`，§3 表头计数同步（B2）
  - §4.1 errata 加 `IXxxRepository` 命名约束 + 禁用 `XxxStore` / `XxxDao` / `XxxGateway` 清单 + Roslyn `BannedSymbols.txt` 规则提示（F7）
  - §5 errata 补两条公开约定：(a) enum 全 `HasConversion<string>().HasMaxLength(64)` 由 EFCore base 实现（F4）；(b) Attribute 用于单字段约束、Fluent API 用于跨字段 / Provider-specific（F8）；具体实现锁定到 HD-009
  - §9 appsettings.json 示例改写为 `Inkwell:Providers:Persistence` 选择器 + `Inkwell:Persistence:*` 详细段
- **`file-structure.md`**
  - HD-001 文件清单新增 `Options/InkwellProvidersOptions.cs`（或等价 nested class，依 author 落地选择）（F9）
  - HD-002 时间字段重命名同步标注（F2）
- **`database-design.md`**
  - "总体设计原则·命名" 段落锁定 F5 解释 A（PascalCase 直入，不做 snake_case 转换）
  - HD-002 18 表占位的列名约定从 `created_at_utc` / `updated_at_utc` → `CreatedTime` / `UpdatedTime`（F2 + F5 联合）
  - "Provider 配置键" 引用从 `Inkwell:Persistence:Provider` → `Inkwell:Providers:Persistence`（F9）
  - INK-PERSIST 13 行表对齐 HD-002 §3.10（B2）
- **`docs/03-architecture/architecture.md`**
  - §4 line 207 上方追加 errata 块：`Inkwell:DataStore:Provider` 字面量已被精化为 `Inkwell:Providers:Persistence`；详细配置位仍在 `Inkwell:Persistence:*`（F1 + F9 合并）
- **`docs/03-architecture/adr/ADR-022-entity-domain-mapper-selection.md`（新文件）**
  - 按 H2 六字段格式起草草案；候选 = AutoMapper / Mapperly / 手写映射；status:draft，待 H2 评审 approved 后再启 HD-009（F6）

#### 6.4.3 仍待澄清的非阻塞项

- **§3.2 N1 ~ N5 / N6**：non-blocking，按本报告原结论流转到 HD-007 / HD-009 / HD-013，本轮 errata 不动。
- **F4 / F8 实现层**：仅在 HD-002 §5 留公开约定，不在本轮 errata 内 ship 实现细节，统一进 HD-009 EFCore base 起草时锁定。

## 7. HD-003 FileStorage Port 增量评审（2026-05-11）

> 本轮在已 reviewed 的报告主体之上**追加**，仅评审增量产物：[HD-003 Inkwell.Abstractions FileStorage Port](Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md)（status: draft，2026-05-11 起草）+ [file-structure.md `## Inkwell.Abstractions.FileStorage` 章节追加](file-structure.md#inkwellabstractionsfilestorage)。报告主体的 status / reviewers / 签字字段**不**因本节调整。

### 7.0 评审范围与基线

- **本轮评审对象**：HD-003 全文（§1 ~ §13）+ file-structure.md `## Inkwell.Abstractions.FileStorage` 章节
- **不在本轮范围**：HD-002 / HD-009 / database-design.md / architecture.md（已在 §1 ~ §6 评审完毕并 reviewed）
- **前置闸门**：
  - [requirements.md](../01-requirements/requirements.md) `status: reviewed` ✅
  - [repo-impact-map.md](../01-requirements/repo-impact-map.md) `status: reviewed` ✅（REQ-009 / REQ-016 / REQ-017 在 §2.9 / §2.16 / §2.17 真实存在）
  - HD-003 frontmatter 完整、upstream 13 项均可定位（REQ-003 / 009 / 016 / 017 / NFR-004 / 006 + ADR-002 / 005 / 009 / 015 / 017 + HD-001 真实存在；NFR-004 由 [requirements.md NFR-004](../01-requirements/requirements.md) 锁定）
  - **不触发** [io-contracts.md §5 阻塞返回](../../.he/agents/_shared/io-contracts.md)

### 7.1 完备性扫描（HD-003 范围内）

按 [h3-detailed-design.md §3 / §4 章节列表](../../.he/docs/stages/h3-detailed-design.md) 在 HD-003 切片范围内逐项打分。

| 章节                           | 状态                 | 覆盖度                                                                                      | 缺口                                                    | 证据                                                                                                                                                                    |
| ------------------------------ | -------------------- | ------------------------------------------------------------------------------------------- | ------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1.1 文件结构                   | `pass`               | 8 `*.cs` 全锁 + `## Inkwell.Abstractions.FileStorage` 跨模块章节同步落地                    | —                                                       | [HD-003 §2](Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md) + [file-structure.md §FileStorage](file-structure.md#inkwellabstractionsfilestorage) |
| 1.2 数据库 / 表 / 字段         | `n/a`                | 端口层不直接接 DB                                                                           | HD-003 §12 显式声明 database-design.md "不贡献"         | HD-003 §12                                                                                                                                                              |
| 1.3 API 请求 / 响应 / 错误码   | `pass`（端口接口段） | 7 方法签名 + 9 错误码 INK-FILESTORE-001 ~ 009 + 错误分类表（业务失败 vs 程序错误）          | —                                                       | HD-003 §3.1 / §3.8 / §4                                                                                                                                                 |
| 1.4 服务 / 进程 / 后台任务     | `n/a`                | 端口层无独立进程                                                                            | HD-003 §9 说明随 Inkwell.Abstractions.csproj 一同打镜像 | HD-003 §9                                                                                                                                                               |
| 1.5 每个目录 / 程序文件职责    | `pass`               | 8 `*.cs` × 10 字段全填，无 `<TBD>` / `<待定>`                                               | —                                                       | HD-003 §3.1 ~ §3.8                                                                                                                                                      |
| 1.6 配置文件字段 / 默认值      | `pass`               | `Inkwell:FileStorage:*` 完整段（7 字段 + 默认值 + 范围）+ Provider 子段路径明确             | —                                                       | HD-003 §3.6 / §9                                                                                                                                                        |
| 1.7 日志格式 / 字段            | `pass`               | `filestore.*` 7 span × 6 私有字段 + PII 提示                                                | —                                                       | HD-003 §4.3                                                                                                                                                             |
| 1.8 监控指标 / 告警策略        | `pass`               | §7.3 三档告警建议（P1 / P2 / P3）+ 维度明确                                                 | —                                                       | HD-003 §7.3                                                                                                                                                             |
| 1.9 部署步骤 / 回滚 / 备份恢复 | `partial`            | K8s Secret 注入（凭证位）+ Helm bucket 初始化标记由 Provider HD 接手                        | Helm `helm install` / 桶自动创建脚本 → 留 Provider HD   | HD-003 §7.2 / §9 / §11                                                                                                                                                  |
| 1.10 性能 / 安全 / 已知限制    | `pass`               | 性能预算 P50/P99 + 凭证位 + path traversal 守护（`InvalidKeyFormat`）+ §11 5 条 known-issue | —                                                       | HD-003 §7 / §11                                                                                                                                                         |

**完备性结论**：HD-003 在自己声明的"范围切片"内 8 / 10 章节 `pass`、2 / 10 `n/a`（端口层不接 DB / 不独立进程）、0 / 10 `missing`。1.9 部署的 `partial` 是合理 deferral 到 Provider HD，不阻塞本 HD 翻 reviewed。

### 7.2 一致性扫描（HD-003 ↔ 上游）

逐项执行 6 类交叉检查。状态约定同 [§2.1](#21-概览)。

#### 概览

- **C13**（FAIL）— 测试包路径分歧
- **C14**（FAIL）— ADR-015 接口草图参数名漂移
- **C15**（PARTIAL）— §1.4 偏离表未对齐 ADR-015 接口形态
- **C16**（PARTIAL）— §10 CI 命令 `rg` 内 `\|` escape 在 shell 直接拷贝执行时失效
- **C17**（PARTIAL）— file-structure.md "端口接口文件" 建议段陈旧
- **C18**（PASS）— `InkwellProvidersOptions.FileStorage` 字段锚点对齐
- **C19**（PASS）— ADR-015 容器命名约定 / 4 容器名 / Q5 ListAsync 形态对齐
- **C20**（PASS）— 时间字段 `XxxTime + DateTimeOffset UTC` 与 [§6.1 F2](#f2--时间字段命名走-xxxtime--datetimeoffset-utc) 全局规范一致
- **C21**（PASS）— `ErrorCodes.FileStore` partial 类与 HD-001 §3.13 / HD-002 §3.10 拓扑兼容
- **C22**（PASS）— §1.4 偏离表显式声明 + Reviewer 反查路径明确豁免 4 方法
- **C23**（PASS）— `FileDownloadResponse` 实现 `IAsyncDisposable + IDisposable` 双接口 + Stream 释放语义明确

#### 详细证据（bullet 子段）

##### C13（FAIL）— 测试包路径分歧

- **不一致点**：HD-003 §3.1 / §8.3 / §10 用 `tests/core/Inkwell.FileStorage.Tests.Contract/`；HD-002 §8 / HD-009 §6 / file-structure.md "总体拓扑" line 41 / §3.2 N4 一致使用 `tests/core/Inkwell.Providers.Contract/{Persistence,FileStorage,...}/`（跨 4 端口家族**统一**测试包，按子目录切片）
- **证据**：[HD-003 §3.1 line 109](Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md) + [HD-003 §8.3](Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md) vs [HD-002 §8 公共契约用例包](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) + [HD-009 §6 line 696](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) + [file-structure.md line 41](file-structure.md)
- **影响**：HD-013（跨 Provider 契约测试包 HD）起草时会两难——按 HD-003 起两个独立 csproj（`Inkwell.Persistence.Tests.Contract` + `Inkwell.FileStorage.Tests.Contract` + …）还是按 HD-002/HD-009 起统一 `Inkwell.Providers.Contract`？H4 [TestCaseAuthor](../../.he/agents/test-case-author/AGENT.md) 反推时也会撞这个分歧

##### C14（FAIL）— ADR-015 接口草图参数名漂移

- **不一致点**：
  - [ADR-015 line 46](../03-architecture/adr/ADR-015-object-storage-provider-switchable.md)：`UploadAsync(string container, string key, Stream data, FileMetadata? meta = null, ...)`
  - [HD-003 §3.1 接口](Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md)：`UploadAsync(..., FileMetadata? metadata = null, ...)` —— 参数名 `meta` → `metadata`
  - [ADR-015 line 52](../03-architecture/adr/ADR-015-object-storage-provider-switchable.md)：`ListAsync(string container, string? prefix = null, ...)`
  - HD-003 §3.1 接口：`ListAsync(..., string? keyPrefix = null, ...)` —— 参数名 `prefix` → `keyPrefix`
- **证据**：ADR-015 line 46 / 52 vs HD-003 §3.1 line 102
- **影响**：public API 参数名是 ABI 一部分（[C# named arguments](https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/named-and-optional-arguments)），调用方用 `Upload(meta: foo)` 与 `Upload(metadata: foo)` 编译结果不同；[`PublicApiAnalyzers`](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) 把参数名 diff 计为 breaking change；现已锁的 PublicAPI.Shipped.txt 与 ADR-015 草图存在歧义

##### C15（PARTIAL）— §1.4 偏离表未对齐 ADR-015 接口形态

- **不一致点**：HD-003 §1.4 偏离表只列出"HD-001 §5.2 默认" vs "本 HD 决议"；未显式标注对 ADR-015 接口草图的两处超越：
  - ADR-015 line 46 `Task<FileUploadResult>` （无 Result）→ HD-003 `Task<Result<FileUploadResult>>`
  - ADR-015 line 47 `Task<Stream> DownloadAsync` → HD-003 `Task<Result<FileDownloadResponse>>`
- **证据**：HD-003 §1.4 vs ADR-015 line 46 / 47
- **影响**：H4 TestCaseAuthor 反推 TC 时若先看 ADR-015 接口草图会撞接口形态分歧；reviewer 无法快速判定 HD-003 是合理精化还是违规
- **理由**：H3 通常视 ADR 接口草图为"示意"而非"ABI 锁定"，但 HD-003 §1.4 显式声明这一约定可彻底消除歧义

##### C16（PARTIAL）— §10 CI 命令 `rg` 内 `\|` escape

- **不一致点**：[HD-003 §10 F1](Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md)：`rg -n "using\s+(Azure\.Storage\.Blobs\|Minio\|Amazon\.S3)" ...`；§10 F5 同样有 `\|`。Markdown 表格内的 `\|` escape 是为绕开 [GFM table delimiter](https://github.github.com/gfm/#tables-extension-)，但当 H5 [CodingExecutor](../../.he/agents/coding-executor/AGENT.md) 把命令拷到 Verify 字段执行时，shell（zsh / bash）会把字面量 `\|` 传给 ripgrep，导致正则解析错误
- **证据**：HD-003 §10 F1 / F5
- **影响**：H5 编码任务 Verify 步骤误以为 BannedSymbols 检查通过（实则 `rg` 正则没真正匹配 alternation）
- **理由**：GitHub Actions YAML 或 shell 直接运行时应写 `\\\|` 或改用单独 `-e` flag（`rg -n -e "Azure\.Storage\.Blobs" -e "Minio" -e "Amazon\.S3" ...`）

##### C17（PARTIAL）— file-structure.md "端口接口文件" 建议段陈旧

- **不一致点**：[file-structure.md line 96-103](file-structure.md) 在 §Inkwell.Abstractions 内的"端口接口文件"建议段仍只列 2 个 FileStorage 文件（`IFileStorageProvider.cs` + `FileStorageOptions.cs`）；同文件新增的 [`## Inkwell.Abstractions.FileStorage` 章节 line 186-194](file-structure.md#inkwellabstractionsfilestorage) 列了完整 8 个文件
- **证据**：file-structure.md line 96-103 vs line 186-194
- **影响**：reviewer 在 H4 / H5 引用文件结构时可能落在陈旧建议段，错过新增 6 文件
- **理由**：建议段 line 96-103 是 H1 草图占位（"建议路径"），现已被本 HD 章节超越；可在建议段加 errata 行 ">  HD-003 已落地完整 8 文件清单，详见 [§Inkwell.Abstractions.FileStorage](file-structure.md#inkwellabstractionsfilestorage)" 即可

##### C18（PASS）— `InkwellProvidersOptions.FileStorage` 字段锚点对齐

- HD-003 §3.6 头引用 `[HD-001 §3.11.1 InkwellProvidersOptions]` 锚点；[HD-001 §3.11.1 line 276](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) 确实定义 `[Required] public string FileStorage { get; init; } = "LocalFileSystem";` 字段
- **证据**：HD-003 §3.6 + HD-001 §3.11.1 line 276

##### C19（PASS）— ADR-015 容器命名约定 / 4 容器名 / ListAsync 形态对齐

- HD-003 §1.3 Q3 / Q5 引用：
  - ADR-015 §容器命名（line 59）真实存在 ✓
  - 4 容器名 `uploads` / `kb-source` / `kb-extracted` / `audit-export` （line 61-64）真实存在 ✓
  - ADR-015 line 52 `IAsyncEnumerable<FileObjectInfo>` 与 HD-003 §3.1 / picker Q5=A 完全一致 ✓
- **证据**：ADR-015 line 59 / 61-64 / 52

##### C20（PASS）— 时间字段命名规范一致

- HD-003 §3.3 `FileUploadResult.UploadedTime` / §3.4 `FileDownloadResponse.UploadedTime` / §3.5 `FileObjectInfo.LastModifiedTime` 全用 `XxxTime` 后缀 + `DateTimeOffset.Offset == TimeSpan.Zero` UTC 强制
- **证据**：HD-003 §3.3 / §3.4 / §3.5 vs [§6.1 F2 全局规范](#f2--时间字段命名走-xxxtime--datetimeoffset-utc)

##### C21（PASS）— `ErrorCodes` partial 拓扑

- HD-003 §3.8 `ErrorCodes.FileStore.cs` `public static partial class ErrorCodes { public static class FileStore { ... } }` 与 HD-001 §3.13 `ErrorCodes.Core` / HD-002 §3.10 `ErrorCodes.Persist` partial 拓扑兼容（[C# Roslyn partial type](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/partial-type)）
- 9 个常量 `INK-FILESTORE-001` ~ `INK-FILESTORE-009` 编号连续无跳跃 + 段不重叠
- **证据**：HD-003 §3.8 vs HD-001 §3.13 vs HD-002 §3.10

##### C22（PASS）— §1.4 偏离表 + Reviewer 反查路径

- HD-003 §1.4 显式声明 `Upload` / `Download` 仍走 `Result<T>`、`Exists` / `Delete` / `Presign*` / `List` 偏离；末尾 reviewer 反查路径明确豁免 4 方法
- **证据**：HD-003 §1.4

##### C23（PASS）— `FileDownloadResponse` Stream 释放语义

- HD-003 §3.4 `FileDownloadResponse : IAsyncDisposable, IDisposable` 双接口 + `Dispose` / `DisposeAsync` 委托给底层 `Stream`；§4.4 显式声明"调用方负责释放"
- **证据**：HD-003 §3.4 / §4.4

**一致性结论**：12 项检查中 2 项 `FAIL`（C13 / C14）、3 项 `PARTIAL`（C15 / C16 / C17）、7 项 `PASS`（C18 ~ C23）。`FAIL` 项是 HD-003 翻 reviewed 前必须修复的卡点；`PARTIAL` 项是 reviewer 翻 reviewed 前建议消化的项目。

### 7.3 反问清单（HD-003 增量）

按 `blocking` / `non-blocking` 分组。`blocking` 等级建议由 Owner picker 拍板。

#### B3：测试包路径分歧（C13）

- **问题**：HD-003 用 `tests/core/Inkwell.FileStorage.Tests.Contract/`；HD-002 / HD-009 / file-structure.md / §3.2 N4 一致使用 `tests/core/Inkwell.Providers.Contract/FileStorage/`。同一测试目录给出两套路径
- **影响范围**：
  - HD-013（跨 Provider 契约测试包 HD）起草时拓扑选择
  - H4 TestCaseAuthor 反推 FileStorage 三 Provider matrix 用例时 csproj 引用
  - H5 编码任务卡 `tests/core/.../` 路径所有引用
  - CI matrix workflow yaml 中的 `dotnet test` target
- **建议方向**：
  - 选项 1：HD-003 §3.1 / §8.3 / §10 改 `Inkwell.Providers.Contract/FileStorage/`，与 HD-002 / HD-009 / file-structure.md 拓扑统一
  - 选项 2：file-structure.md "总体拓扑" + HD-002 / HD-009 反过来改成 per-family `Inkwell.<Family>.Tests.Contract/`，每端口家族独立 csproj
- **卡点等级**：**blocking**（建议）
- **追溯**：C13

#### B4：ADR-015 接口草图参数名漂移（C14）

- **问题**：HD-003 §3.1 接口签名参数名 `metadata` / `keyPrefix` 与 ADR-015 line 46 / 52 接口草图 `meta` / `prefix` 漂移
- **影响范围**：
  - PublicAPI.Shipped.txt 锁定的 ABI 参数名
  - 调用方 named-argument 调用兼容性（`Upload(meta: foo)` vs `Upload(metadata: foo)`）
  - H5 编码 Verify 步骤 [`PublicApiAnalyzers`](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) 与 ADR-015 接口草图 diff
- **建议方向**：
  - 选项 1：HD-003 §3.1 改回 `meta` / `prefix`（贴 ADR-015 草图）
  - 选项 2：HD-003 §3.1 维持 `metadata` / `keyPrefix`（更可读），同时在 ADR-015 加 errata 块声明 H3 接口名已被精化为 `metadata` / `keyPrefix`
- **卡点等级**：**blocking**（建议）
- **追溯**：C14

#### N7：§1.4 偏离表未对齐 ADR-015 接口形态（C15）

- **问题**：HD-003 §1.4 偏离表只对齐 HD-001 §5.2，没显式标注对 ADR-015 接口草图的两处形态超越（`Task<FileUploadResult>` → `Task<Result<FileUploadResult>>`、`Task<Stream>` → `Task<Result<FileDownloadResponse>>`）
- **影响范围**：H4 TestCaseAuthor / [h3-detailed-design-reviewer](../../.github/agents/h3-detailed-design-reviewer.agent.md) 后续反推时迷惑源
- **建议方向**：HD-003 §1.4 偏离表加两行：`vs ADR-015 接口草图：Task<FileUploadResult>（无 Result）→ Task<Result<FileUploadResult>>（picker Q1 重负荷走 Result）` + `vs ADR-015 接口草图：Task<Stream>（裸 Stream）→ Task<Result<FileDownloadResponse>>（picker Q2 含元数据避免二次 HEAD）`
- **卡点等级**：non-blocking
- **追溯**：C15

#### N8：§10 CI 命令 `rg \|` shell escape 失效（C16）

- **问题**：HD-003 §10 F1 / F5 命令中 `rg` 正则用 `\|` 是 markdown 表格内 escape，shell（zsh / bash）拷贝执行时 `\|` 会变成字面量
- **影响范围**：H5 CodingExecutor Verify 命令运行；CI matrix workflow yaml 写入
- **建议方向**：
  - 选项 1：改 `rg -n -e "Azure\.Storage\.Blobs" -e "Minio" -e "Amazon\.S3" ...` 多 `-e` flag
  - 选项 2：改 fenced code block（4 空格缩进或 ``` ``` `` ）而非 markdown 表格 cell，规避 `|` escape
- **卡点等级**：non-blocking
- **追溯**：C16

#### N9：file-structure.md "端口接口文件"建议段陈旧（C17）

- **问题**：file-structure.md line 96-103 仍只列 FileStorage 2 文件（H1 草图遗迹）
- **影响范围**：reviewer / TestCaseAuthor 引用建议段时漏掉 6 文件
- **建议方向**：建议段 line 96-103 加 errata 行指向 §Inkwell.Abstractions.FileStorage 完整章节，或直接精化建议段为完整 8 文件
- **卡点等级**：non-blocking
- **追溯**：C17

### 7.4 评审结论与下一步

- **HD-003 翻 `reviewed` 前置条件**：
  1. Owner 拍板 B3 / B4（picker，2 条 blocking）
  2. AI 在 [h3-detailed-design-author](../../.github/agents/h3-detailed-design-author.agent.md) 模式下落 B3 / B4 + N7 / N8 / N9 一次性 errata
  3. Owner 在 HD-003 frontmatter 翻 `status: draft → reviewed` + 填 `reviewers: [Inkwell]`（**人工签字位**，AI 不替签）
- **跨文档前置条件**：B3 选项 1 落地不影响 HD-002 / HD-009 / file-structure.md；B4 选项 2 需 ADR-015 加 errata（不改 H2 主决策）
- **本评审报告**：[§4 评审结论](#4-评审结论) / [§6 评审反馈记录](#6-评审反馈记录) / §7（本节）的 `status: reviewed` 维持不变；本节作为增量证据归档
- **后续 HD 建议路径**：HD-003 reviewed 后继续 HD-004 ICacheProvider（[ADR-016](../03-architecture/adr/ADR-016-cache-provider-redis.md)）或 HD-005 IQueueProvider + MessageEnvelope（[ADR-018](../03-architecture/adr/ADR-018-queue-abstraction-channels-default.md) + [RISK-015](../03-architecture/risk-analysis.md) 跨服务 trace 字段）

### 7.5 自检

- ✅ 每条 `pass` / `partial` / `FAIL` 都附了文件路径或具体引用
- ✅ `blocking` 反问（B3 / B4）都能映射到具体一致性冲突 + 影响范围
- ✅ 未使用 "看起来" / "似乎" / "感觉" 等主观词汇
- ✅ 未凭文件名臆测，每条结论都打开了对应文件读到对应字段
- ✅ 未尝试用部分数据写"半个报告"——前置闸门已确认通过
- ✅ 未越界修改 HD-003 / file-structure.md / 报告主体
- ✅ 未给越界建议（如"建议你顺便重构 X"）
- ✅ 报告路径仍走 H3 规范默认 [docs/04-detailed-design/design-review-report.md](design-review-report.md)（追加 §7 而非新建文件）

## 8. H3 第二轮规约翻转纪要（2026-05-11）

> 本节由 H3-DetailedDesignAuthor 在 [ADR-023](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) 起草同会话追加；评审主体（§1 ~ §7）`status: reviewed` 维持不变。本篇纪要仅作翻转决策的溢出结组位，不复审 ADR-023 本身。

### 8.1 触发

- 时间：2026-05-11
- 触发点：[§7 评审收尾 → HD-003 5 errata 落地完成](#7-hd-003-filestorage-port-增量评审2026-05-11) → Owner 审视 `Task<Result<T>>` 规约
- Owner 表态：希望端口层切回与 .NET BCL / ASP.NET Core / EF Core / StackExchange.Redis 主流 SDK 一致的“裸 `Task<T>` + 异常”风格

### 8.2 picker 决策（2026-05-11）

- **Q-scope = A**：端口层（`IXxxProvider`）裸 `Task<T>`；业务命名空间 `Result<T>` / `Error` 工具保留作可选模式
- **Q-errorcode = A**：错误码表 `ErrorCodes.<Module>` + `INK-<MODULE>-<NNN>` 格式保留；端口层统一抛 `InkwellException(code, message, inner?)`，**不**为每错误码起异常子类

### 8.3 影响清单与落地批次

| 文件                                                                                                      | 本会话                                  | 待落地（下一批）                                                                                                                        |
| --------------------------------------------------------------------------------------------------------- | --------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| [ADR-023](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)                     | ✅ 新建 `status: accepted`               | —                                                                                                                                       |
| [HD-001 frontmatter / §3.3 / §5.2 / §5.3](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) | ✅ callout + 职责微调 + §5.2 / §5.3 重写 | —                                                                                                                                       |
| 本报告 §8                                                                                                 | ✅ 本节                                  | —                                                                                                                                       |
| [HD-002 IPersistenceProvider](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)       | ⏳ 未动                                  | 全方法签名翻 `Task<Result<T>>` → `Task<T>` + frontmatter 加第二轮 errata callout                                                        |
| [HD-003 IFileStorageProvider](Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md)      | ⏳ 未动                                  | §3.1 七方法签名翻 + §1.3 picker Q1/Q2 标 superseded + §1.4 偏离表大幅缩减 + frontmatter 加第二轮 errata + §3.8 catch 说明同步改异常风格 |
| [ADR-015 FileStorage Provider](../03-architecture/adr/ADR-015-object-storage-provider-switchable.md)      | ⏳ 未动                                  | 二次 errata 块追加（声明 H3 第二轮规约翻转；本轮 5/11 第一轮 errata 块已落地）                                                          |
| HD-004 ~ HD-008（未起草）                                                                                 | —                                       | 直接用新规约起草                                                                                                                        |

### 8.4 质量閘门补丁项（入 H4 必验）

- **CI A1**：`rg -n 'Task<Result<' src/core/Inkwell.Abstractions/` 期望 0 行
- **CI A2**：`rg -n -e 'public Result<' -e ': Result<' src/core/Inkwell.Abstractions/ -g '!Common/Result.cs' -g '!Common/Error.cs'` 期望 0 行
- **CI A4**：`rg -n 'throw new InkwellException\(' src/core/ providers/` 应全部使用 `ErrorCodes.<Module>.<Name>` 常量，不允许字面量
- **测试范例迁移**：`result.IsSuccess.Should().BeTrue()` → `await act.Should().NotThrowAsync()`；`result.Error.Code.Should().Be(...)` → `(await act.Should().ThrowAsync<InkwellException>()).Which.Code.Should().Be(...)`

### 8.5 本会话未做的（下批责任区）

- 未翻 HD-002 / HD-003 主体签名——避免超 per-module 注意力预算
- 未追 ADR-015 二次 errata 块——需与 HD-003 二次 errata 同批以保记录连贯
- 未起草 HD-004——需先有本 ADR 作为起草前置

### 8.6 下一轮可选动作（Owner 拍板）

1. **继续第二批**：一次会话同时翻 HD-002 / HD-003 签名 + ADR-015 二次 errata
2. **先发当前第一批**：Owner 先给 ADR-023 + HD-001 补签（`reviewers: [Inkwell]`），再批次进入第二批
3. **跳第二批、先起 HD-004**（不推荐）：会产生 HD-002 / HD-003 与 HD-004 签名风格不一致的中间状态

### 8.7 自检

- ✅ ADR-023 6 字段齐：选什么 / 为什么 / 替代方案 / 放弃理由 / 维护影响 / 成本-性能-安全-交付影响
- ✅ HD-001 未删 picker 2026-05-10 Q1 决策（自研 Result）——仅翻转“端口层强制”语义
- ✅ HD-001 §3.1 / §3.2 主体 10 字段未无痕重写——通过 frontmatter callout + §3.3 职责微调 + §5.2 / §5.3 重写实现“声明 + 改动隔离”
- ✅ HD-002 / HD-003 / ADR-015 留待第二批 errata，避免本会话注意力预算超支
- ✅ 业务命名空间 `Result<T>` / `Error` 工具保留——picker Q-scope=A 边界正确落地
- ✅ 错误码 `INK-<MODULE>-<NNN>` 表保留——picker Q-errorcode=A 边界正确落地
- ✅ 给 H4 留下三条补丁项（CI A1 / A2 / A4 + 测试范例迁移），不越界去写 HD-002 / HD-003 主体

## 8.8 第三轮规约翻转纪录（2026-05-11，同会话叠加）

> **反模式趋近检查**：同一会话中 ADR-023 `status: accepted` 后 1 小时内发生第二轮决策翻转。[copilot-instructions §5 反模式 "反复纠错"](../../.github/copilot-instructions.md) 在本轮已主动给 Owner 列出代价清单警示；Owner 拍板 Q-existing=A（ADR-023 补 errata、不废止），按 [ADR-022 先例](../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 保决策历史证据链。本节作为第三轮纪要，不复审 ADR-023 errata·01 本身。

### 8.8.1 picker 拍板（2026-05-11）

- **Q-strategy = A**：端口层全 .NET BCL 异常类型（`FileNotFoundException` / `KeyNotFoundException` / `IOException` / `TimeoutException` / `ArgumentException` / `InvalidOperationException` / `UnauthorizedAccessException` / `NotSupportedException` 等）；零自建异常类（`InkwellConfigurationException` / `InkwellBuilderException` 除外——两类仅用于 DI / Builder 程序错误）
- **Q-existing = A**：[ADR-023](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) `status: accepted` 保持不变，仅增 errata·01 节
- **Q-scope = A**：本会话仅动根决策层（ADR-023 errata·01 + HD-001 §3.3 / §4 + 本节）

### 8.8.2 本会话落地清单

| 文件                                                                                           | 改动                                                                                                                                                                                 |
| ---------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| [ADR-023](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)          | 末尾追加 §errata · 2026-05-11 errata·01 节（含 picker / 覆盖表 / 保留表 / 下一步）。`status: accepted` 不变                                                                          |
| [HD-001 frontmatter](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)           | 加 第三轮 errata callout；与 第二轮 callout 用 `>` 单字符空引用行连接（避 MD028）                                                                                                    |
| [HD-001 §3.3 InkwellException](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) | 10 字段表重写——删除 `InkwellException` 公共基类；仅保留 `InkwellConfigurationException` / `InkwellBuilderException` 两子类直继 `System.Exception`；无 `Code` 字段；不重写 `ToString` |
| [HD-001 §4](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)                    | 标题 “错误码与日志公共约定” → “错误与日志公共约定”；§4.1 错误码命名表整段废除改为“错误表达机制”说明；§4.2 日志字段表改 OTel `exception.*` 五字段 + 废字段列表                        |
| 本报告 §8.8                                                                                    | 追加本节                                                                                                                                                                             |

### 8.8.3 H4 补丁项同步更新

- **CI A4 原文**：验证 `ErrorCodes.<Module>.<Name>` 常量引用 → **废除**
- **CI A4'**：`rg -n 'class \w+Exception' src/core/Inkwell.Abstractions/` 期望仅 2 行（仅 `InkwellConfigurationException` / `InkwellBuilderException`）
- **CI A4''**：`rg -n 'throw new \w+Exception' src/core/Inkwell.Abstractions/` 期望全为 BCL 异常类（除 Configuration/Builder 外不出现自建异常类）
- **CI A5 原文**：验证 `ErrorCodes` 静态类存在 ≥ 6 个 → **废除**
- **测试范例**：§8.4 原“`result.Error.Code.Should().Be(...)` → `(await act.Should().ThrowAsync<InkwellException>()).Which.Code.Should().Be(...)`” → 现“`await act.Should().ThrowAsync<KeyNotFoundException>()` / `<TimeoutException>` / `<UnauthorizedAccessException>` 等按 BCL 型型验证”

### 8.8.4 下一轮（第二批）增量

原 §8.3 “待落地（下一批）” 清单上叠加：

- [HD-002](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)：原计划 “翻 `Task<Result<T>>` → `Task<T>`” 之外补上“错误路径按 BCL 对照表重写”（实体不存在 → `KeyNotFoundException` / 唯一约束冲突 → `InvalidOperationException` / DbUpdate 错误 → `DbUpdateException` 原样上抩）
- [HD-003](Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md)：原计划之外补上 “§3.8 ErrorCodes.FileStore.cs 整段删 + §1.3 picker Q6 标 superseded + §3.1 × 7 方法错误处理字段按 BCL 对照表重写”：ObjectAlreadyExists → `IOException` 或 `Azure.RequestFailedException(409)` / QuotaExceeded → `IOException` 或 SDK 子类 / UploadFailed → `IOException` / FileNotFound → `FileNotFoundException` / ContainerNotFound → `DirectoryNotFoundException` / Unauthorized → `UnauthorizedAccessException` / Timeout → `TimeoutException` / PresignedUrlGenerationFailed → `InvalidOperationException` / ConnectionFailed → `IOException`
- [ADR-015](../03-architecture/adr/ADR-015-object-storage-provider-switchable.md)：二次 errata 块以反映 “端口层无错误码 / 无 InkwellException(code) / 全 BCL”

### 8.8.5 本会话未做（下批责任区）

- 未翻 HD-002 / HD-003 主体签名与错误处理——避免超 per-module 注意力预算（同 §8.5）
- 未追 ADR-015 二次 errata 块——需与 HD-003 二次 errata 同批以保记录连贯
- 未动 ADR-013 OTel 字段示例——ADR-013 `exception.*` 五字段本就锁，仅 §4.2 主动重申子集，未越界重写 ADR

### 8.8.6 自检

- ✅ ADR-023 status 保 accepted；errata·01 不重写 §决策主体，仅补 errata 节（同 ADR-022 先例）
- ✅ HD-001 §3.1 / §3.2 10 字段未动——`Result<T>` / `Error` 作业务层可选工具保留
- ✅ picker 2026-05-10 Q1 不撤回（原决策仅“端口层强制使用”语义被翻）
- ✅ HD-002 / HD-003 / ADR-015 仍留二批 errata，同一批次增量事项已在 §8.8.4 记录
- ✅ 反模式 “反复纠错” 被识别并列代价 + picker 拍板 → 选择 errata 路径而非废止 ADR，保决策历史证据链
- ✅ H4 补丁项同步更新（CI A4 / A5 废除，新增 A4' / A4''），避免接会话差错

## 8.9 第四轮规约翻转纪录（2026-05-11，同会话叠加）

§8.8 错误码废止落地后约 30 分钟，Owner 在同会话主动指令 “`Result` 和 `Error` 这两个类都不需要了，有点增加了系统的复杂度”。本次质疑的对象是 §8.8 第三轮 errata·01 保留下来的「业务命名空间可选 `Result<T>` / `Error` 工具」——errata·01 仅把端口层强制翻成业务层可选，但 Inkwell 仍持有 `Common/Result.cs` + `Common/Error.cs` 两个抽象文件作为业务层可选入口。Owner 判断：保留可选会形成两套并存范式，长期沉淀为 Inkwell-private 方言，与 [ADR-023](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) 主决策“与 .NET 主流 SDK 一致”的初衷相悖。

AI 在收到指令时按 [Harness Engineering 反模式 “反复纠错”](../../.github/copilot-instructions.md) 主动警示：本会话已是连续第 3 轮规约级翻新（端口层签名 + 错误码废止 + 抽象删除），需评估是否进入“反复纠错”。Owner picker `session-pause = A`（继续推进；理由：本会话三轮均为“单向收紧不来回”，与典型反复纠错的“A 改 B、B 改 A” 不同）。

### 8.9.1 picker 拍板（2026-05-11）

- **`result-error-scope` = A**：完全删除 `Common/Result.cs` + `Common/Error.cs` 两个文件及全部引用。业务命名空间（`Inkwell.Core.Agents` / `.Models` / `.Tools` / ...）错误处理一律走 [BCL 异常类型](https://learn.microsoft.com/dotnet/standard/exceptions/) + 现成抽象（[`ValidationResult`](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations.validationresult) / `IEnumerable<string>` / `record` 业务返回类型）
- **`session-pause` = A**：本会话三轮单向收紧 → 继续推进至本轮落地后再沉淀
- **errata 写入策略**：[ADR-023 errata·02](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 叠加在 errata·01 之上（同 ADR-022 多轮 errata 先例）；[HD-001 §13](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#13-errata-记录) 加 “2026-05-11 第四轮变更” 节

### 8.9.2 本会话落地清单

- ✅ **[ADR-023 §errata·02](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 新增**：触发说明 + picker 三问（`Q-scope=A` / `Q-existing=A` / `Q-session-pause=A`） + 备选项 A/B/C/D 取舍 + 翻转覆盖表（4 行：核心边界·第 5 条 / 备选项 A 放弃理由 / 维护影响 / 联动提示） + 翻转保留 + 维护影响 + 成本性能安全交付 + 置信度 high + 下一步清单（accepted by Inkwell 2026-05-11）
- ✅ **[HD-001 §3.1 / §3.2 整段删除](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#3-程序文件设计10-字段--10-文件)**：`Common/Result.cs` / `Common/Error.cs` 10 字段表移除；§3 标题“10 字段 × 12 文件” → “10 字段 × 10 文件”；§3.3 ~ §3.12 编号不调整以保追溯不断（§3.1 / §3.2 锁位保留不重用）
- ✅ **[HD-001 §1.2 文件清单](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#12-本-hd-起草的文件清单)**：“Common Result/Error” 行 → “Common Exception”行（仅 `InkwellException.cs`）
- ✅ **[HD-001 §2.2 文件树](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#22-文件树)**：删 `Common/Result.cs` + `Common/Error.cs` 行
- ✅ **[HD-001 §1.3 / §10 Q1](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#13-关键决策摘要)**：strikethrough + 指向 ADR-023 errata·02 锚点
- ✅ **[HD-001 §4.1 第四条](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#41-错误处理与异常约定)**：“业务命名空间可选使用 `Result<T>` / `Error`” → “与端口层遵同一机制，全走 BCL 异常 + `ValidationResult`”
- ✅ **[HD-001 §5.3 末条](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)**：删 “`Result<T>` 工具保留”条；BCL 异常对照表完整
- ✅ **[HD-001 §7](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#7-性能--安全)**：删 “`Result<T>` 是 readonly struct” 句；删 `Error.Context` PII 提示
- ✅ **[HD-001 §11 业务命名空间条](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#11-待补--后续-hd-衔接)**：改为 “零 `Result<T>` / `Error` 抽象、零错误码表” + 推荐 `ValidationResult`
- ✅ **[HD-001 §13 2026-05-11 第四轮变更](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#13-errata-记录)**：触发 + picker + 变更清单（10 项） + 上游补齐落地（ADR-023 errata·02 已 accepted）
- ✅ **[HD-001 §14.1 csproj→namespace 表](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#141-命名空间)**：`Common/Result.cs` 示例行 → `Common/Pagination.cs` 示例行（保持 “§子目录不入 namespace” 语义）
- ✅ **HD-001 §0 / §1.3 / §10 / §11 / §13 措辞同步**：6 处 “待 H2 起草” / “(待 H2 ArchitectAdvisor 起草 + accept)” 措辞去除 → 改为指向 ADR-023 errata·02 锚点

### 8.9.3 H4 补丁项同步更新

- **新增 CI A4'''**：业务命名空间禁 `Result<T>` / `Error` 使用——`rg -n -e 'Result<' -e ': Error\b' src/core/ providers/` 期望 0 行（`Microsoft.Extensions.Logging` 等 BCL `Result` 类型靠 import 区分，不在击中范围内）
- **A4''（errata·01 新增）保留不变**：`rg -n 'throw new \w*Exception' src/core/Inkwell.Abstractions/` 不命中除 `InkwellConfigurationException` / `InkwellBuilderException` 外的自定义异常
- **A5（errata·01 已废除）保持废除**
- **测试代码**：errata·01 已大量翻 `result.IsSuccess.Should().BeTrue()` → `await act.Should().NotThrowAsync()`；errata·02 进一步保证业务命名空间测试一致——零 `Result.Success(...)` / `Result.Failure(...)` / `Error.Code` 残留

### 8.9.4 本会话未做（下批责任区）

- **HD-002 / HD-003 第二批 errata**——翻 `Task<Result<T>>` → 裸 `Task<T>` + 删 `ErrorCodes.FileStore` + 按 BCL 对照表重写错误处理字段 + 删 `Result<T>` / `Error` 引用 + frontmatter 加第二+第三+第四轮 errata callout；避免超 per-module 注意力预算（同 §8.5 / §8.8.5）
- **ADR-015 二次 errata 块追加**——需与 HD-003 二次 errata 同批以保记录连贯（H2 范围，AI 不写需 Owner 或 h2-architect-advisor agent）
- **HD-004 ~ HD-008 起草**——下次会话直接用 errata·01 + errata·02 后的新规约
- **ADR-024 横切根决策**（namespace + file-scoped + GlobalUsings + UTF-8/LF 四项规约证据）——尚未起草，需 h2-architect-advisor agent 或 Owner 手动起草

### 8.9.5 自检

- ✅ ADR-023 status 保 accepted；errata·02 叠加在 errata·01 之上（同 ADR-022 先例）
- ✅ HD-001 §3.1 / §3.2 已整段删除；§3 编号 §3.3 ~ §3.12 不调整以保追溯不断
- ✅ picker 2026-05-10 Q1 标 superseded by errata·02（不撤回，仅状态翻转）
- ✅ HD-002 / HD-003 / HD-004~008 / ADR-015 / ADR-024 仍留下批，下批责任区已在 §8.9.4 列清
- ✅ 反模式 “反复纠错” 被识别 + 给 Owner picker 拍板 → `session-pause = A` 选择继续（理由：三轮均为单向收紧不来回）+ 沉淀点在本轮结束后
- ✅ 6 处 “待 H2 起草” 措辞均同步去除（HD-001 §0 / §1.3 Q1 / §10 Q1 / §11 / §13.触发 / §13.上游补齐）；errata·02 锚点已修复指向 ADR-023 实体 anchor
- ✅ 跨文件证据链闭环：HD-001 §13 → ADR-023 errata·02 → design-review-report §8.9 三向互链

## 9. HD-009 EFCore Persistence base 增量评审（2026-05-11）

> 本轮在已 reviewed 的报告主体之上**追加**，仅评审增量产物：[HD-009 Inkwell.Persistence.EFCore base](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)（status: draft，2026-05-11 起草）+ [file-structure.md `## providers/Inkwell.Persistence.EFCore` 章节追加](file-structure.md#providersinkwellpersistenceefcore) + [database-design.md `## providers/Inkwell.Persistence.EFCore（EFCore base 实现）` 章节追加](database-design.md)。报告主体 §1 ~ §8 的 `status / reviewers` 字段**不**因本节调整。

### 9.0 评审范围与基线

- **本轮评审对象**：HD-009 全文（§1 ~ §13）+ file-structure.md `## providers/Inkwell.Persistence.EFCore` 章节 + database-design.md `## providers/Inkwell.Persistence.EFCore（EFCore base 实现）` 章节
- **不在本轮范围**：HD-001 / HD-002 / HD-003 / architecture.md / 报告主体 §1 ~ §8（已在前序评审中处理）；ADR-023 三轮 errata 的跨 HD 二轮一致性扫描另立 §10
- **前置闸门**（按 [h3-detailed-design-reviewer 工作流第一步](../../.he/agents/design-reviewer/AGENT.md)）：
  - [requirements.md](../01-requirements/requirements.md) `status: reviewed` ✅
  - [repo-impact-map.md](../01-requirements/repo-impact-map.md) `status: reviewed` ✅
  - HD-009 frontmatter 完整、upstream 16 项均可定位（HD-002 / ADR-004 / 017 / 019 / 021 / 022 / 023 / REQ-001 / 002 / 009 / 010 / 011 / 012 / 014 / 015 / NFR-005 真实存在）
  - **不触发** [io-contracts.md §5 阻塞返回](../../.he/agents/_shared/io-contracts.md)——HD-009 是合理 per-module slice 切片，目录未"严重偏离" h3-detailed-design.md

### 9.1 完备性扫描（HD-009 范围内）

按 [h3-detailed-design.md §3 / §4 章节列表](../../.he/docs/stages/h3-detailed-design.md) 在 HD-009 切片范围内逐项打分。

| 章节                                 | 状态                  | 覆盖度                                                                                                                                                                                                          | 缺口                                                                                                                                                                                                                                                                                                   | 证据                                                                                                                                                                                                                                                                                      |
| ------------------------------------ | --------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1.1 文件结构                         | `pass`                | base 8 `*.cs` + 1 `BannedSymbols.txt` + 1 `.csproj` 全锁 + `## providers/Inkwell.Persistence.EFCore` 跨模块章节同步落地 + §3.13 18 业务实体 × 4 类文件批量模板表                                                | —                                                                                                                                                                                                                                                                                                      | [HD-009 §2](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) + [file-structure.md §providers/Inkwell.Persistence.EFCore](file-structure.md#providersinkwellpersistenceefcore) + [HD-009 §3.13](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) |
| 1.2 数据库 / 表 / 字段 / 索引 / 约束 | `pass`（base 范围内） | 三 mixin 自动配置规则 + SaveChangesInterceptor 行为 + AutoSeed 幂等 + 跨 Provider 字段映射策略 + 18 业务表占位（具体字段由各业务 HD 起草）                                                                      | —                                                                                                                                                                                                                                                                                                      | [HD-009 §3.0 / §3.3 / §3.4](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) + [database-design.md §providers/Inkwell.Persistence.EFCore](database-design.md)                                                                                                        |
| 1.3 API / 接口契约 / 错误码          | `partial`             | EfCorePersistenceProvider / InkwellSeeder / MigrationRunner / AuditingSaveChangesInterceptor / 18 Repository 接口签名全锁 + §4.3 错误处理统一表（13 行）；**但与 ADR-023 三轮 errata 严重不一致**（详 §10）     | 错误处理路径全文走 `Result<T>` + `INK-PERSIST-NNN`，与 [HD-002 §4.3](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 已翻新的 BCL 异常对照表完全脱节；这是 **§10 一致性扫描的 blocking 根源**，不是完备性维度的缺口                                                              | [HD-009 §3.2 / §3.3 / §3.4 / §3.5 / §3.10 / §4.3](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)                                                                                                                                                                   |
| 1.4 服务 / 进程 / 后台任务           | `n/a`                 | base 是 library，不独立进程；MigrationRunner 由 [Inkwell.WebApi / Inkwell.Worker 启动期调用](../03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md)                                            | —                                                                                                                                                                                                                                                                                                      | HD-009 §9                                                                                                                                                                                                                                                                                 |
| 1.5 每个目录 / 程序文件职责          | `pass`                | base 8 `*.cs` × 10 字段全填，无 `<TBD>` / `<待定>`                                                                                                                                                              | —                                                                                                                                                                                                                                                                                                      | [HD-009 §3.1 ~ §3.12](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)                                                                                                                                                                                               |
| 1.6 配置文件字段 / 默认值            | `pass`                | §7 复用 [HD-002 §3.5 PersistenceOptions](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) + 6 字段消费表（不引入新字段）                                                                   | —                                                                                                                                                                                                                                                                                                      | [HD-009 §7](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)                                                                                                                                                                                                         |
| 1.7 日志格式 / 字段                  | `partial`             | §4.2 Repository OTel span `db.repository.<entity>.<verb>` + `BeginScope` 字段；**未对齐 [HD-002 §4.4](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 已新增的 `exception.*` 五字段标准** | OTel `exception.type` / `.message` / `.stacktrace` / `.escaped` / `.id` 五字段（[HD-001 §4.2](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) 已锁，[HD-002 §4.4](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) errata·01 已重写）在 HD-009 §3 / §4 全文未出现 | HD-009 §3.2 / §4.2 vs HD-001 §4.2 vs HD-002 §4.4                                                                                                                                                                                                                                          |
| 1.8 监控指标 / 告警策略              | `partial`             | §8.4 line coverage ≥ 95% 门槛；缺 [HD-002 §7 三档告警](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 的 base 实现层映射                                                                 | base 实现层未声明 `INK-PERSIST-003 / -007` 告警源 metric 名（与 HD-002 §7 无 ↔ 关联）                                                                                                                                                                                                                  | HD-009 §8.4 vs HD-002 §7                                                                                                                                                                                                                                                                  |
| 1.9 部署步骤 / 回滚 / 备份恢复       | `pass`                | §9 显式声明 base 不产 image；MigrationRunner 启动期行为 + AutoSeed 幂等模式 + ADR-005 引用                                                                                                                      | —                                                                                                                                                                                                                                                                                                      | [HD-009 §9](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)                                                                                                                                                                                                         |
| 1.10 性能边界 / 安全边界 / 已知限制  | `pass`                | §3.0 Provider-agnostic 五条原则 + §3.2 EFCore Provider 行为约束 + §3.12 BannedSymbols 安全边界 + §10 8 条 CI 自动化检查 + §12 待补清单                                                                          | —                                                                                                                                                                                                                                                                                                      | [HD-009 §3.0 / §3.12 / §10 / §12](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)                                                                                                                                                                                   |

**完备性结论**：HD-009 在自己声明的"范围切片"内 7 / 10 章节 `pass`、2 / 10 `partial`（1.3 错误处理 + 1.7 OTel 字段——这两项是 ADR-023 三轮 errata 跨 HD 不一致的体现，详 §10 一致性扫描）、1 / 10 `n/a`（base 不独立进程）、1 / 10 `partial`（1.8 监控指标）、0 / 10 `missing`。**完备性维度本身不卡 HD-009 翻 reviewed**——卡点全在一致性维度（详 §10）。

### 9.2 一致性扫描（HD-009 ↔ HD-002 / ADR-021 / ADR-022）

逐项执行 6 类交叉检查（暂不含 ADR-023 跨 HD 二轮扫描，那一组归 §10 单独成节）。状态约定同 [§2.1](#21-概览)。

#### 9.2.1 概览

- **C24**（PASS）— ADR-021 base csproj 拓扑落地一致
- **C25**（PASS）— ADR-022 mapper 选型 + 物理位置 + 三方法签名 一致
- **C26**（PASS）— [HD-002 §4.1.3 Repository 动词白名单](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 6 动词 + 不带 `Async` 后缀 一致
- **C27**（PASS）— [HD-002 §4.1.5 F7 BannedSymbols `IXxxStore` / `IXxxDao` / `IXxxGateway`](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 落地为 §3.12 `BannedSymbols.txt` 第 2 段一致
- **C28**（PASS）— mixin 三件套 `IHasTimestamps` / `IHasRowVersion` / `IHasOwner` 字段名 + 类型 + EFCore 自动配置规则一致
- **C29**（PASS）— [HD-002 §3.5 PersistenceOptions](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 6 字段被 HD-009 §7 完整消费、零字段引入
- **C30**（PASS）— ADR-021 InMemory Provider 不支持 Migration、走 EnsureCreated 行为在 §3.6 IDbContextInitializer + §8.3 跨 Provider 契约用例 N4 处置一致
- **C31**（PASS）— csproj 12（[ADR-019 后修订](../03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md) + [ADR-020](../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) + [ADR-021](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）拓扑与本 csproj 物理路径一致
- **C32**（FAIL）— **HD-009 §3 / §4 全文错误处理走 `Result<T>` + `INK-PERSIST-NNN`，与 [HD-002 errata·01 / errata·02 / errata·03](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 已翻新的 BCL 异常 + 删 `Common/Result.cs` + `Common/Error.cs` 完全脱节**（详 §10 单独立节扫描）
- **C33**（FAIL）— **HD-009 §11 决策记录表未列 [ADR-023](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) 三轮**（详 §10）
- **C34**（FAIL）— **HD-009 §10 自动化检查 8 条缺 ADR-023 三轮翻新检查（无 `Task<Result<` / 无 `INK-PERSIST-` / 无 `Result.Failure` grep）**（详 §10）

#### 9.2.2 详细证据（PASS 项）

##### C24（PASS）— ADR-021 base csproj 拓扑

- HD-009 §3.0 ~ §3.12 物理路径 `src/core/providers/Inkwell.Persistence.EFCore/` 与 [ADR-021 D1](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 一致
- base 集中 Entity / Configurations / Mapping / Repositories / DbContext / Provider / Interceptor / Initializer / Seeder / MigrationRunner / DI 扩展 / BannedSymbols；final adapter csproj（HD-010 / HD-011 / HD-012）通过 ProjectReference 引本 csproj
- 证据：HD-009 §3.0 文件路径 + [ADR-021 §决策](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)

##### C25（PASS）— ADR-022 mapper 选型 + 物理位置 + 三方法签名

- HD-009 §3.0 + §4.1 + §3.13 18 业务实体 × `Mapping/<TypeName>MappingExtensions.cs` 三方法签名（`ToModel()` / `ToEntity()` / `SelectAsModel()`）100% 与 [ADR-022 §决策](../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 锁定一致
- §3.10 AgentRepository sample 代码内调 `entity.ToModel()` / `agent.ToEntity()` / `.SelectAsModel()` 三方法均出现
- §3.12 BannedSymbols.txt 第 1 段禁 AutoMapper / Mapster / Mapperly
- 证据：HD-009 §3.0 / §3.10 / §3.12 / §3.13 / §4.1 + [ADR-022 §决策 / §约束](../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md)

##### C26（PASS）— Repository 动词白名单 + 无 Async 后缀

- HD-009 §3.10 AgentRepository sample 6 个动词方法 `AddAgent` / `UpdateAgent` / `GetAgent` / `DeleteAgent` / `ListAgents` / `FindAgentsByOwner` 全用白名单动词、无 `Async` 后缀
- §4.2 Repository 共性约束第 3 项明确"6 动词方法 + 不带 Async 后缀"
- §10 自动化检查 C5 / C6 grep 强制
- 证据：HD-009 §3.10 / §4.2 / §10 + [HD-002 §4.1.3](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)

##### C27（PASS）— BannedSymbols 后缀禁用清单

- HD-009 §3.12 BannedSymbols.txt 第 2 段：`T:Inkwell.Abstractions.Persistence.Agents.IAgentStore` / `IAgentDao` / `IAgentGateway` 三条
- 与 [HD-002 §4.1.5 F7](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 锁定的禁用清单完全对齐
- 证据：HD-009 §3.12 + HD-002 §4.1.5

##### C28（PASS）— 三 mixin 字段 + EFCore 自动配置

- HD-009 §3.3 AuditingSaveChangesInterceptor 联动三 mixin 字段名 `CreatedTime` / `UpdatedTime` / `OwnerUserId` / `RowVersion` 与 [HD-002 §3.7 / §3.8 / §3.9](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 完全一致
- §3.0 + database-design.md `## providers/Inkwell.Persistence.EFCore（EFCore base 实现）` 三 mixin 自动配置规则表锁定 EFCore 行为：`IHasTimestamps` → `IsRequired()` / `IHasRowVersion` → `IsRowVersion()` / `IHasOwner` → `IsRequired() + HasIndex()`
- 证据：HD-009 §3.3 + database-design.md `## providers/Inkwell.Persistence.EFCore（EFCore base 实现）` 三 mixin 自动配置规则表

##### C29（PASS）— PersistenceOptions 6 字段消费

- HD-009 §7 表 6 字段 `ConnectionString` / `CommandTimeoutSeconds` / `MigrationTimeoutSeconds` / `AutoSeedOnStartup` / `EnableSensitiveDataLogging` / `EnableDetailedErrors` 与 [HD-002 §3.5](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 完全对齐
- §7 显式声明"本 HD 不引入新配置字段"
- 证据：HD-009 §7 + HD-002 §3.5

##### C30（PASS）— InMemory Migration → EnsureCreated 行为分支

- HD-009 §3.6 IDbContextInitializer 抽象 + §8.3 跨 Provider 契约用例分两类（Provider-agnostic 三 Provider 全跑 / Migration-specific 仅 SqlServer + Postgres，InMemory 用 EnsureCreated 等价 schema-creation 替代）
- 与 [§3.2 N4](#n4hd-002-83-跨-provider-契约用例-migration-启动-在-inmemory-上的语义c6) 建议方向一致
- 证据：HD-009 §3.6 / §8.3 + ADR-021 §5

##### C31（PASS）— csproj 12 拓扑

- [AGENTS.md §3.1](../../AGENTS.md) 锁 12 csproj（[ADR-019 修订后](../03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md)：core 4 + providers 8 = 12，含 `Inkwell.Persistence.EFCore` shared base + 3 final adapter）
- HD-009 §5 拓扑约束 + file-structure.md `## providers/Inkwell.Persistence.EFCore` 章节物理路径与 ADR-019 / 020 / 021 一致
- 证据：HD-009 §5 + file-structure.md "总体拓扑"

#### 9.2.3 一致性结论

11 项检查中 8 项 `PASS`（C24 ~ C31）、3 项 `FAIL`（C32 / C33 / C34）。3 项 `FAIL` 全部根源于 [ADR-023 三轮 errata 跨 HD 不一致](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)（HD-009 起草于 ADR-023 第三轮 errata·01 + 第四轮 errata·02 之前）；下方 §10 单独立节做跨 HD 二轮扫描 + 给出每条结构化反问。

## 10. ADR-023 三轮 errata 跨 HD 一致性二轮扫描（2026-05-11）

> 本节专门处理 [ADR-023 主决策 + errata·01 + errata·02](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)（2026-05-11 三轮 accepted）的跨 HD 落地一致性。HD-001 / HD-002 / HD-003 已分批落 errata（详 [§8.3 第二批 / §8.8.4 第三批 / §8.9.4 第四批](#83-影响清单与落地批次)），HD-009 起草于 ADR-023 第三/四轮之前 → **整文档与 ADR-023 完全脱节**。本节给出每处不一致的 grep 证据 + 一次性 errata 翻新清单。

### 10.1 ADR-023 三轮锁定的最终态规约

| 维度                  | 规约（三轮叠加后最终态）                                                                                                                                                                                                                                           | 证据                                                                                                                                                                                                                              |
| --------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 端口签名              | 裸 `Task<T>` / `Task<int>`（**无** `Result<T>` 包装）                                                                                                                                                                                                              | [ADR-023 §决策](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)                                                                                                                                       |
| 错误传递              | 直抛 BCL 异常类型（`KeyNotFoundException` / `InvalidOperationException` / `TimeoutException` / `IOException` / `ArgumentException` / `ArgumentOutOfRangeException` / `UnauthorizedAccessException` / `NotSupportedException` / `OperationCanceledException` 透传） | [ADR-023 errata·01](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改业务异常类型分流)                                                                                 |
| 自定义异常            | 仅 `InkwellConfigurationException` / `InkwellBuilderException` 两类直继 `System.Exception`（DI / Builder 装配期程序错误专用），**无** `Code` 字段                                                                                                                  | [HD-001 §3.3](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) + [ADR-023 errata·01](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改业务异常类型分流) |
| 错误码                | **完全废除**（无 `INK-XXX-NNN` 字面量、无 `ErrorCodes.<Module>` 静态类、无 `ErrorCodes.<Module>.cs` 文件）                                                                                                                                                         | [ADR-023 errata·01](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改业务异常类型分流)                                                                                 |
| `Result<T>` / `Error` | **完全废除**（删 `Common/Result.cs` + `Common/Error.cs` 两文件 + 业务命名空间零引用）                                                                                                                                                                              | [ADR-023 errata·02](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常)                                         |
| OTel 异常字段         | `exception.type` / `.message` / `.stacktrace` / `.escaped` / `.id` 五字段（[HD-001 §4.2](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) 锁，[HD-002 §4.4](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) errata·01 重写）  | HD-001 §4.2 + HD-002 §4.4                                                                                                                                                                                                         |

### 10.2 跨 HD 落地状态矩阵

| HD                      | 端口签名翻新                                          | 错误传递翻新                                                                       | 删 `ErrorCodes.<Module>`                                                                                                                          | 删 Result/Error 引用                                                               | OTel 五字段                                   | 决策记录引用 ADR-023          | 总体状态          |
| ----------------------- | ----------------------------------------------------- | ---------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------- | --------------------------------------------- | ----------------------------- | ----------------- |
| HD-001 Foundation       | n/a（端口接口在 HD-002+）                             | ✅ §3.3 / §5.3 重写                                                                 | ✅ §3.13 删 ErrorCodes.cs                                                                                                                          | ✅ §3.1 / §3.2 整段删                                                               | ✅ §4.2 重写                                   | ✅ §13 + 报告 §8 / §8.8 / §8.9 | **已三轮翻完**    |
| HD-002 Persistence Port | ✅ §3.1 翻 `Task<T>`                                   | ✅ §4.3 BCL 对照表（9 行）                                                          | ✅ §3.10 删 ErrorCodes.Persist                                                                                                                     | ✅ 全文零 `Result<T>` 引用                                                          | ✅ §4.4 含 `exception.*` 五字段                | ✅ §13.1 ~ §13.4 四轮 errata   | **已三轮翻完**    |
| HD-003 FileStorage Port | ✅ §3.1 翻 `Task<T>`                                   | ✅ §4 BCL 对照表                                                                    | ✅ §3.8 删 ErrorCodes.FileStore                                                                                                                    | ✅ 全文零 `Result<T>` 引用                                                          | ✅ §4 含 `exception.*` 五字段                  | ✅ §13.1 ~ §13.4 四轮 errata   | **已三轮翻完**    |
| **HD-009 EFCore base**  | ❌ §3.2 `Task<Result<T>>` / §3.4 / §3.5 `Task<Result>` | ❌ §3.2 / §3.3 / §3.4 / §3.5 / §3.10 / §4.3 全 `INK-PERSIST-NNN` + `Result.Failure` | ❌ 全文 13 处 `INK-PERSIST-NNN` 引用                                                                                                               | ❌ §3.10 100 行 sample 代码全 `Result.Success/Failure` + `Error("INK-PERSIST-NNN")` | ❌ 仅 §4.2 OTel span 名，无 `exception.*` 字段 | ❌ §11 决策表未列 ADR-023      | **完全未翻**      |
| file-structure.md       | n/a                                                   | n/a                                                                                | ❌ §Inkwell.Abstractions 文件树仍含 `Common/Result.cs` + `Common/Error.cs` + `ErrorCodes.cs` + `ErrorCodes.Persist.cs` + `ErrorCodes.FileStore.cs` | ❌ 同上                                                                             | n/a                                           | n/a                           | **未同步 errata** |
| database-design.md      | n/a                                                   | ❌ "错误码 INK-PERSIST-NNN 段" 13 行表完整保留                                      | n/a                                                                                                                                               | n/a                                                                                | n/a                                           | n/a                           | **未同步 errata** |

### 10.3 HD-009 不一致点 grep 证据（C32 / C33 / C34）

#### C32 详细证据 — HD-009 § 3 / § 4 错误处理脱节

##### C32-1：EfCorePersistenceProvider 接口签名（[HD-009 §3.2](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)）

- 当前签名：
  - `public Task<Result<T>> ExecuteInTransactionAsync<T>(Func<IUnitOfWork, CancellationToken, Task<Result<T>>> work, ...)`
  - `public Task<Result> SaveChangesAsync(...)`
- ADR-023 主决策应为：
  - `public Task<T> ExecuteInTransactionAsync<T>(Func<IUnitOfWork, CancellationToken, Task<T>> work, ...)`
  - `public Task<int> SaveChangesAsync(...)`（返回 EF Core 标准 `int affectedRows`）
- 证据：HD-009 §3.2 接口字段表 + [HD-002 §3.1](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 已翻 `Task<T>` 形态

##### C32-2：错误处理表内 INK-PERSIST-NNN（[HD-009 §3.2 / §3.4 / §3.5 / §4.3](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)）

- 当前：
  - §3.2 `Error("INK-PERSIST-003", "ConcurrencyConflict")` / `Error("INK-PERSIST-002", "DuplicateKey")` / `Error("INK-PERSIST-008", "CommandTimeout")` / `Error("INK-PERSIST-004", "TransactionRolledBack")`
  - §3.3 `InkwellException("INK-PERSIST-012", "MissingOwner")`
  - §3.4 `Task<Result> SeedAsync` + `INK-PERSIST-006 SeederFailed`
  - §3.5 `Task<Result> RunAsync` + `INK-PERSIST-005 / -006 / -007`
  - §4.3 错误处理统一表 13 行全 `INK-PERSIST-NNN`
- ADR-023 errata·01 应为：
  - `DbUpdateConcurrencyException` 透传或包成 `InvalidOperationException("ConcurrencyConflict ...", inner)`
  - 唯一约束冲突 → `InvalidOperationException("DuplicateKey ...", inner)`
  - 命令超时 → `TimeoutException`
  - 事务非取消异常 → 透传 `DbUpdateException`
  - `MissingOwner` → `ArgumentException("OwnerUserId cannot be Guid.Empty", nameof(...))`
  - `SeederFailed` / `MigrationFailed` → `InvalidOperationException` 包装 + inner
- 证据：HD-009 §3.2 / §3.3 / §3.4 / §3.5 / §4.3 + [HD-002 §4.3 BCL 对照表 9 行](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)

##### C32-3：§3.10 AgentRepository sample 100 行代码全旧规约

- 当前：6 个方法（AddAgent / UpdateAgent / GetAgent / DeleteAgent / ListAgents / FindAgentsByOwner）全 `Task<Result<TModel>>` 返回类型 + `Result.Success(...)` / `Result.Failure(...)` + `Error("INK-PERSIST-NNN")` 字面量
- ADR-023 errata·01 + errata·02 应为：
  - `AddAgent` → `Task<AgentDefinition>`，唯一冲突时 `throw new InvalidOperationException("DuplicateKey: ...", ex);`
  - `UpdateAgent` → `Task` 或 `Task<int>`，并发冲突时 `DbUpdateConcurrencyException` 透传
  - `GetAgent` → `Task<AgentDefinition?>`（推荐 nullable 返回，业务层不存在 → `KeyNotFoundException`）或 `Task<AgentDefinition>` 不存在时直抛 `KeyNotFoundException`
  - `DeleteAgent` → `Task` 或 `Task<bool>`（不存在时返回 `false` 或抛 `KeyNotFoundException`）
  - `ListAgents` → `Task<PagedResult<AgentDefinition>>`（裸返回）
  - `FindAgentsByOwner` → `Task<IReadOnlyList<AgentDefinition>>`（裸返回）
- 证据：HD-009 §3.10 整段 100 行代码

##### C32-4：§4.3 错误处理统一表 13 行 INK-PERSIST-NNN

- 当前：13 行表全 `INK-PERSIST-NNN` 错误码 + `Result.Failure` 处理
- ADR-023 errata·01 应为：13 行表改 BCL 异常对照表（参考 [HD-002 §4.3](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 9 行模式）
- 证据：HD-009 §4.3

#### C33 详细证据 — §11 决策记录未列 ADR-023

- 当前：HD-009 §11 决策记录表 10 行，引用 ADR-021 / ADR-022 / HD-002，**完全未列 ADR-023**
- ADR-023 三轮 accepted 后应为：表加 4 行
  - 端口签名形态：`Task<T>` 裸 → 来源 [ADR-023 主决策](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)
  - 错误传递机制：BCL 异常 + 透传 → 来源 [ADR-023 errata·01](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改业务异常类型分流)
  - 错误码废除：无 `INK-PERSIST-NNN` → 来源 [ADR-023 errata·01](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改业务异常类型分流)
  - `Result<T>` / `Error` 抽象删除：业务命名空间零 `Result<T>` 引用 → 来源 [ADR-023 errata·02](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常)
- 证据：HD-009 §11

#### C34 详细证据 — §10 自动化检查缺 ADR-023 翻新检查

- 当前：8 条 C1 ~ C8 检查（mapping 三方法 / null 守护 / 业务层不引 mapping / Repository 动词 / Async 后缀 / Model 后缀 / 包级 banlist）
- ADR-023 三轮 accepted 后应为：补 4 条 C9 ~ C12
  - **C9**：`grep -rn 'Task<Result<' "$ROOT"` 期望 0 行（端口签名翻新检查，对齐 [§8.4 CI A1](#84-质量閘门补丁项入-h4-必验)）
  - **C10**：`grep -rn 'INK-PERSIST-' "$ROOT"` 期望 0 行（错误码废除检查，对齐 [§8.8.3 CI A4''](#883-h4-补丁项同步更新)）
  - **C11**：`grep -rEn 'Result\.(Success|Failure)' "$ROOT"` 期望 0 行（`Result<T>` / `Error` 抽象删除检查，对齐 [§8.9.3 CI A4'''](#893-h4-补丁项同步更新)）
  - **C12**：`grep -rEn 'throw new \w+Exception' "$ROOT"` 应全为 BCL 异常或 `DbUpdateException` 透传（对齐 [§8.8.3 CI A4'](#883-h4-补丁项同步更新)）
- 证据：HD-009 §10 全 8 条

### 10.4 file-structure.md 跨 HD 不一致 grep 证据

#### C35（FAIL）— file-structure.md `## Inkwell.Abstractions` 文件树仍含已废文件

- 当前文件树残留：
  - `Common/Result.cs` — 已被 [HD-001 §3.1 + §13.4](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) 整段删 + 报告 [§8.9.2](#892-本会话落地清单) 锁定
  - `Common/Error.cs` — 同上
  - `ErrorCodes.cs` — 已被 [HD-001 §3.13](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) errata·01 删 + 报告 [§8.8.2](#882-本会话落地清单) 锁定
  - `ErrorCodes.Persist.cs` — 已被 [HD-002 §3.10](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) errata·01 删 + 报告 [§7.4 / §8.8.4](#74-评审结论与下一步) 锁定
  - `ErrorCodes.FileStore.cs` — 已被 [HD-003 §3.8](Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md) errata·01 删 + 报告 [§8.8.5](#885-本会话未做下批责任区) 锁定
  - 文件计数文字"13（HD-001）+ 9（HD-002 本体）+ 8（HD-003）= 30 个 `*.cs`"过期（应减 5 个废文件 = 25 个）
- ADR-023 errata·01 + errata·02 应为：删 5 行 + 文件计数减 5
- 证据：file-structure.md `## Inkwell.Abstractions` line 47-92（含 `Common/` + `ErrorCodes.cs` + `ErrorCodes.Persist.cs`）+ `## Inkwell.Abstractions.FileStorage` line 195（含 `ErrorCodes.FileStore.cs`）

### 10.5 database-design.md 跨 HD 不一致 grep 证据

#### C36（FAIL）— database-design.md "错误码 INK-PERSIST-NNN 段"表完整保留

- 当前：`### 错误码 INK-PERSIST-NNN 段（HD-002 §3.10 锁定）` 13 行表全保留
- ADR-023 errata·01 + [HD-002 §13.3 errata·01](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 应为：整段表删除，改一行链接指向 [HD-002 §4.3 BCL 异常对照表 9 行](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)
- 证据：database-design.md `### 错误码 INK-PERSIST-NNN 段` 13 行表

### 10.6 HD-002 frontmatter typo

#### C37（FAIL）— HD-002 frontmatter `status: reviweed`

- 当前：[HD-002 frontmatter](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 第 4 行 `status: "reviweed"`（4 字符 typo）
- 应为：`status: reviewed`
- 影响：phase-gate-runner / 其他自动化机械识别 HD 状态时按字面量比对 `reviewed` 失败，HD-002 不被认为是 reviewed
- 证据：HD-002 frontmatter line 4

### 10.7 一致性结论（§10）

6 项 FAIL（C32 / C33 / C34 / C35 / C36 / C37）。其中 5 项（C32 ~ C36）是 ADR-023 三轮 errata 跨 HD 落地未同步，1 项（C37）是 frontmatter typo。**全部 6 项均 blocking**：HD-009 翻 reviewed 必须先翻新 + file-structure.md / database-design.md errata + HD-002 typo 修复，否则 H4 TestCaseAuthor 看到 HD-009 旧规约会反推一套与 HD-002 / HD-003 完全不兼容的 TC，H5 CodingExecutor 直接编译失败。

## 11. 本轮反问清单（HD-009 + ADR-023 跨 HD 二轮 + typo）

按 `blocking` / `non-blocking` 分组排序。`blocking` 由 [io-contracts.md §6.1 picker 拍板](../../.he/agents/_shared/io-contracts.md)。

### 11.1 Blocking（必须在 HD-009 翻 `reviewed` 之前回炉）

#### B5：HD-009 端口签名 + 错误处理 + Repository sample 全文未跟 ADR-023 三轮翻新（C32）

- **问题**：HD-009 §3.2 EfCorePersistenceProvider / §3.3 AuditingSaveChangesInterceptor / §3.4 InkwellSeeder / §3.5 MigrationRunner / §3.10 AgentRepository（100 行 sample 代码） / §4.3 错误处理统一表（13 行）全用 `Task<Result<T>>` + `Result.Failure` + `Error("INK-PERSIST-NNN")`，与 [ADR-023 主决策](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) + [errata·01](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改业务异常类型分流) + [errata·02](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 全脱节
- **影响范围**：
  - HD-009 唯一 `IPersistenceProvider` 实现 `EfCorePersistenceProvider` 不能编译（[HD-002 §3.1](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 接口已翻 `Task<T>`）
  - 18 业务实体 × `<Type>Repository` H5 编码任务卡基线代码错（用 HD-009 §3.10 sample 起的所有 H5 任务全部失败）
  - H4 TestCaseAuthor 看 HD-009 旧规约会反推一套与 HD-002 / HD-003 不兼容的 TC（`Result.IsSuccess` vs `await act.Should().NotThrowAsync()`）
  - 跨 Provider 契约用例（HD-013）三 Provider matrix 失败
  - REQ-001 / 002 / 009 / 010 / 011 / 012 / 014 / 015 全部依赖此 base 的业务 HD 阻塞
- **建议方向**：Owner 切到 [`h3-detailed-design-author`](../../.github/agents/h3-detailed-design-author.agent.md) 模式起一次 HD-009 三轮 errata 翻新会话（同 [HD-001 §8.9 / HD-002 §13.3 / HD-003 §13.3 三批先例](#892-本会话落地清单)）：
  - §3.2 接口签名翻 `Task<T>` / `Task<int>`（参考 ADR-023 §决策示例代码）
  - §3.3 / §3.4 / §3.5 错误处理改 BCL 异常类型 + DbUpdateException 透传策略（参考 HD-002 §4.3 9 行 BCL 对照表）
  - §3.10 AgentRepository sample 100 行代码重写（参考 HD-002 §4.1.3 + ADR-023 errata·02 业务命名空间零 `Result<T>` 范式）
  - §4.3 错误处理统一表 13 行 → BCL 异常对照表（参考 HD-002 §4.3）
- **卡点等级**：**blocking**（建议 Owner picker 确认）
- **追溯**：C32

#### B6：HD-009 §11 决策记录未列 ADR-023 三轮（C33）

- **问题**：HD-009 §11 决策记录表引用 ADR-021 / ADR-022 / HD-002，未列 ADR-023 三轮 accepted
- **影响范围**：
  - 评审证据链：HD-009 reviewer / 后续 HD 起草者无法追到 ADR-023 锁定的端口签名 / 错误处理 / 错误码废除 / `Result<T>` 删除四项决策来源
  - traceability matrix（H6 release-note-writer）反向追溯失败
- **建议方向**：HD-009 §11 决策记录表追加 4 行：
  - 端口签名形态 → ADR-023 主决策
  - 错误传递机制 → ADR-023 errata·01
  - 错误码废除 → ADR-023 errata·01
  - `Result<T>` / `Error` 抽象删除 → ADR-023 errata·02
- **卡点等级**：**blocking**（建议 Owner picker 确认）
- **追溯**：C33

#### B7：HD-009 §10 自动化检查缺 ADR-023 翻新检查（C34）

- **问题**：HD-009 §10 8 条 C1 ~ C8 检查覆盖 mapping / Repository / namespace 隔离，但缺 ADR-023 三轮翻新的强制检查
- **影响范围**：
  - CI 不能机械拦截"H5 编码任务无意中引入 `Task<Result<` / `INK-PERSIST-` / `Result.Success` 字面量"
  - [§8.4 / §8.8.3 / §8.9.3 H4 补丁项](#84-质量閘门补丁项入-h4-必验) 在 HD-009 范围内未落地
- **建议方向**：HD-009 §10 自动化检查脚本追加 4 条 C9 ~ C12：
  - C9：`grep -rn 'Task<Result<' "$ROOT"` 期望 0 行
  - C10：`grep -rn 'INK-PERSIST-' "$ROOT"` 期望 0 行
  - C11：`grep -rEn 'Result\.(Success|Failure)' "$ROOT"` 期望 0 行
  - C12：`grep -rEn 'throw new \w+Exception' "$ROOT"` 应全为 BCL 异常或 `DbUpdateException` 透传
- **卡点等级**：**blocking**（建议 Owner picker 确认）
- **追溯**：C34

#### B8：file-structure.md 文件树残留 5 个已废文件（C35）

- **问题**：file-structure.md `## Inkwell.Abstractions` 文件树仍含 `Common/Result.cs` + `Common/Error.cs` + `ErrorCodes.cs` + `ErrorCodes.Persist.cs` + `ErrorCodes.FileStore.cs`，文件计数文字"13 + 9 + 8 = 30"过期
- **影响范围**：
  - reviewer / TestCaseAuthor / CodingExecutor 引用 file-structure.md 时认为 5 个已废文件仍存在 → 反推 TC 失败 / H5 编码任务无意识重建已废文件
  - 与 HD-001 §3.1 / §3.2 整段删除 + HD-002 §3.10 / HD-003 §3.8 删 ErrorCodes 的 errata·01 / errata·02 锁定**直接矛盾**
- **建议方向**：file-structure.md `## Inkwell.Abstractions` 段落起一次同步 errata：
  - 删 `Common/Result.cs` + `Common/Error.cs` 行
  - 删 `ErrorCodes.cs` + `ErrorCodes.Persist.cs` 行
  - 删 `## Inkwell.Abstractions.FileStorage` 段 `ErrorCodes.FileStore.cs` 行
  - 文件计数文字 "13 + 9 + 8 = 30" → "11 + 8 + 7 = 26" 或重新核算
  - 追加 errata 行声明三轮 ADR-023 accepted 后的删除依据
- **卡点等级**：**blocking**（建议 Owner picker 确认）
- **追溯**：C35

#### B9：database-design.md "错误码 INK-PERSIST-NNN 段" 13 行表完整保留（C36）

- **问题**：database-design.md `### 错误码 INK-PERSIST-NNN 段（HD-002 §3.10 锁定）` 13 行表完整保留，与 HD-002 §3.10 errata·01 删 ErrorCodes.Persist 锁定矛盾
- **影响范围**：
  - reviewer / TestCaseAuthor 引用 database-design.md 时认为 INK-PERSIST 错误码段仍生效 → H4 反推一套 INK-PERSIST-NNN matrix TC
  - 与 [HD-002 §13.3](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) errata·01 + ADR-023 errata·01 锁定矛盾
- **建议方向**：database-design.md 整段表删除，改一行 `> 错误处理改 BCL 异常对照表，详 [HD-002 §4.3](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)`
- **卡点等级**：**blocking**（建议 Owner picker 确认）
- **追溯**：C36

#### B10：HD-002 frontmatter typo `status: reviweed`（C37）

- **问题**：[HD-002 frontmatter](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 第 4 行 `status: "reviweed"`（typo，正确拼写 `reviewed`）
- **影响范围**：
  - phase-gate-runner / 其他自动化机械识别 HD 状态时按字面量 `reviewed` 比对失败
  - HD-002 在追溯链中被错误识别为 "未 reviewed 状态"
  - 此为人工签字位 typo（[§6.2 禁区](#62-反馈与本报告-3-反问清单的合并)）
- **建议方向**：Owner 直接修复 frontmatter 第 4 行 `reviweed` → `reviewed`（4 字符 typo 修复，不需要 errata 块；同步登记到 [docs/07-reviews/2026-05-10-h2-architecture-review.md] 评审记录）
- **卡点等级**：**blocking**（typo 直接卡机械识别，[§6.2 人工签字位禁区](#62-反馈与本报告-3-反问清单的合并) 内 AI 不替修）
- **追溯**：C37

### 11.2 Non-blocking（建议在后续 HD 起草时处理）

#### N10：HD-009 §4.2 OTel span 缺 `exception.*` 五字段标准（§9.1 章节 1.7 partial）

- **问题**：HD-009 §4.2 Repository OTel span `db.repository.<entity>.<verb>` + `BeginScope` 字段定义完整，但缺 `exception.type` / `.message` / `.stacktrace` / `.escaped` / `.id` 五字段（[HD-001 §4.2](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) 锁定 + [HD-002 §4.4](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) errata·01 重写）
- **影响范围**：HD-009 EFCore base 实现层 trace 异常字段不全 → Grafana 异常告警维度不足
- **建议方向**：HD-009 §4.2 errata 段加一行声明 OTel `exception.*` 五字段在 `EfCorePersistenceProvider.ExecuteInTransactionAsync` 内 catch 块内以 `Activity.SetStatus(ActivityStatusCode.Error, ex.Message)` + `Activity.AddException(ex)` 记入；Repository 内业务失败异常透传不记 `exception.*`（在 base 实现层 `EfCorePersistenceProvider` 集中记）
- **卡点等级**：non-blocking（实现层细节，可在 HD-009 三轮 errata 翻新会话中一次性补上）
- **追溯**：§9.1 章节 1.7 partial

#### N11：HD-009 §8.4 缺监控 metric 名映射（§9.1 章节 1.8 partial）

- **问题**：HD-009 §8.4 line coverage ≥ 95% 门槛，缺 [HD-002 §7](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 三档告警建议（INK-PERSIST-003 / -007 + transaction.outcome 比例）的 base 实现层 metric 名映射
- **影响范围**：HD-002 §7 告警定义在 HD-009 base 实现层无对应 metric source → Grafana dashboard 配置无依据
- **建议方向**：HD-009 §8.4 后追加新 §8.5 章节"监控 metric 映射"，列出 `db.repository.<entity>.<verb>.duration_ms` / `db.transaction.outcome` / `db.command.timeout` 三 metric 名（OTel metric instrument 名），与 HD-002 §7 三档告警一一对应。注意：HD-002 §7 在 ADR-023 errata·01 后已不应再出现 `INK-PERSIST-NNN` 字面量——若仍存在则为 HD-002 §7 漏翻，需另起 HD-002 一致性反问
- **卡点等级**：non-blocking
- **追溯**：§9.1 章节 1.8 partial

### 11.3 ask.user picker 待 Owner 确认

按 [io-contracts.md §6.1](../../.he/agents/_shared/io-contracts.md)，下列封闭枚举 reviewer 不替设计师下结论，需 Owner picker 拍板：

- **B5 / B6 / B7 / B8 / B9 / B10 卡点等级**：是否全部 blocking？（reviewer 建议全 blocking，但 Owner 可降级 N10 / N11 之外的某一项为 non-blocking）
- **整体评审决议**：FAIL（HD-009 + 跨模块 5 文件回炉）/ PASS（HD-009 范围内不卡，跨模块作 follow-up） / UNKNOWN（待 Owner 复读 §10 / §11 后再拍）—— reviewer 建议 **FAIL**（6 条 blocking 不可绕过，证据链完整）
- **下一动作选择**：
  - 选项 1：Owner 切到 `h3-detailed-design-author` 模式起一次 HD-009 三轮 errata 翻新会话（同 HD-001 / HD-002 / HD-003 三批先例），同会话同步落 file-structure.md / database-design.md errata + HD-002 typo 修复
  - 选项 2：分两批——先 HD-009 三轮 errata 单 PR；再 file-structure.md / database-design.md / HD-002 typo 同步 PR
  - 选项 3：暂不翻 HD-009，先继续起草 HD-004 ~ HD-008 端口家族，等端口家族全 reviewed 后回头一次性翻 HD-009（**reviewer 不建议**，HD-009 是 EFCore base 唯一 `IPersistenceProvider` 实现，业务 HD 已批量依赖）

### 11.4 评审结论与下一步

- **HD-009 翻 `reviewed` 前置条件**：
  1. Owner 拍板 B5 / B6 / B7（picker 三条 blocking）+ B10（typo 直接修复）
  2. AI 在 [`h3-detailed-design-author`](../../.github/agents/h3-detailed-design-author.agent.md) 模式下落 B5 / B6 / B7 一次性 errata（同 HD-001 / HD-002 / HD-003 三批先例）
  3. Owner 在 HD-009 frontmatter 翻 `status: draft → reviewed` + 填 `reviewers: [Inkwell]`（**人工签字位**，AI 不替签）
- **跨模块 file-structure.md / database-design.md `reviewed` 前置条件**：
  1. Owner 拍板 B8 / B9（picker 两条 blocking）
  2. AI 在 `h3-detailed-design-author` 模式下落 B8 / B9 一次性 errata（同会话同步落更佳，避免文档基线漂移）
- **本评审报告**：[§4 评审结论](#4-评审结论) / [§6 评审反馈记录](#6-评审反馈记录) / §7 / §8 的 `status: reviewed` 维持不变；§9 + §10 + §11（本三节）作为 HD-009 增量评审证据归档
- **后续 HD 建议路径**：HD-009 reviewed 后继续 HD-004 ICacheProvider（[ADR-016](../03-architecture/adr/ADR-016-cache-provider-redis.md)）或 HD-005 IQueueProvider + MessageEnvelope（[ADR-018](../03-architecture/adr/ADR-018-queue-abstraction-channels-default.md) + [RISK-015](../03-architecture/risk-analysis.md) 跨服务 trace 字段），新 HD 起草时直接走 ADR-023 三轮 errata 后的最终态规约（同 HD-001 / HD-002 / HD-003 已翻完态）

### 11.5 自检

- ✅ 每条 `pass` / `partial` / `FAIL` 都附了文件路径或具体引用
- ✅ `blocking` 反问（B5 ~ B10）都能映射到具体一致性冲突 + 影响范围 + 建议方向
- ✅ 未使用 "看起来" / "似乎" / "感觉" 等主观词汇
- ✅ 未凭文件名臆测，每条结论都打开了对应文件读到对应字段
- ✅ 未尝试用部分数据写"半个报告"——前置闸门已确认通过
- ✅ 未越界修改 HD-009 / file-structure.md / database-design.md / HD-002 frontmatter / 报告主体（§6.2 禁区严守）
- ✅ 未给越界建议（如"建议你顺便重构 X"）
- ✅ 报告路径仍走 H3 规范默认 [docs/04-detailed-design/design-review-report.md](design-review-report.md)（追加 §9 + §10 + §11 三节而非新建文件）
- ✅ §10 ADR-023 三轮 errata 跨 HD 落地状态矩阵（§10.2）覆盖全 4 HD + 2 跨模块文件 + 1 typo
- ✅ 反模式 "反复纠错" 已在 [§8.8](#88-第三轮规约翻转纪录2026-05-11同会话叠加) / [§8.9](#89-第四轮规约翻转纪录2026-05-11同会话叠加) 主动识别；HD-009 三轮 errata 翻新会话本身**不**是反复纠错（是 HD-001 / HD-002 / HD-003 已翻完态向 HD-009 的同向收紧追平）

### 11.6 Owner picker 拍板记录（2026-05-12）

按 [§11.3 ask.user picker 待 Owner 确认](#113-askuser-picker-待-owner-确认) 三组封闭枚举，Owner 在本会话评审收尾环节通过 picker 拍板（[reviewer Agent §6.2 禁区](#62-反馈与本报告-3-反问清单的合并) 内人决策位）：

| picker            | 选定值                                                                                                      | reviewer 默认建议 | 拍板结果     |
| ----------------- | ----------------------------------------------------------------------------------------------------------- | ----------------- | ------------ |
| `blocking-grade`  | 全部采纳（六条全 blocking）                                                                                 | 全部采纳          | 与建议一致 ✅ |
| `overall-verdict` | FAIL（HD-009 + 跨模块 5 文件回炉，全部 errata 落地后再 reviewed）                                           | FAIL              | 与建议一致 ✅ |
| `next-action`     | 选项 1：切 `h3-detailed-design-author` 单会话同步落 HD-009 + file-structure + database-design + HD-002 typo | 选项 1            | 与建议一致 ✅ |

**结论锁定**：

- B5 / B6 / B7 / B8 / B9 / B10 **六条全 blocking**，逐条对应整改清单详 [§11.1](#111-blocking必须在-hd-009-翻-reviewed-之前回炉)
- 本轮整体 **FAIL**——HD-009 + file-structure.md + database-design.md + HD-002 frontmatter typo 全部回炉，errata 落地后再走 reviewed 流程
- 下一动作走 **选项 1**：单会话同步落（同 [HD-001 §8.9](#89-第四轮规约翻转纪录2026-05-11同会话叠加) / HD-002 §13.4 / HD-003 §13.4 三批先例），避免文档基线漂移
- 本会话 reviewer 工作收尾；交接动作详 [§11.4 评审结论与下一步](#114-评审结论与下一步)；author 模式详细任务说明在 chat 中由 reviewer 同步交接

## 12. 第五轮验收评审（2026-05-12，HD-009 + 跨 HD errata 落地后回审）

> **本节背景**：上一会话 author 模式按 [§11.1](#111-blocking必须在-hd-009-翻-reviewed-之前回炉) 六条整改清单执行 B5/B6/B7（HD-009 三批 errata） + B8/B9（file-structure.md / database-design.md 跨模块同步） + B10（HD-002 frontmatter typo 修复 Owner 手工补签）。本节按 reviewer 工作流逐项验收。

### 12.1 完备性扫描（按 H3 §3 章节清单）

> 按 user-memory `markdown-lint.md` 已知陷阱（中英文混排长内容表必触发 MD060），本节以 bullet list 呈现。

- **文件结构**：`pass` — REQ-009 EFCore base 18 entity + 8 base 文件覆盖完整。证据：[HD-009 §2 文件清单](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) + [file-structure.md §Inkwell.Abstractions](file-structure.md) + 三 csproj 计数链 `11(HD-001) + 8(HD-002) + 7(HD-003) = 26` 全链一致
- **数据库**：`pass` — 覆盖 REQ-002 / REQ-009 / REQ-010 / REQ-014 / REQ-015。证据：[HD-009 §3.7 / §3.8 / §3.13](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) Entity / Configuration / 18 表批量模板；[database-design.md](database-design.md) INK-PERSIST 13-row 表已删 + 引用 [HD-002 §4.3](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) + [HD-009 §4.3](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) 单行引用
- **接口**：`pass` — REQ-009 EFCore Provider / Repository 六动词覆盖完整。证据：[HD-009 §3.2 EfCorePersistenceProvider](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) 双签 `ExecuteInTransactionAsync<T>(Task<T>) / (Task)`；§3.10 AgentRepository 6 方法 `AddAgent → Task<AgentDefinition>` / `UpdateAgent → Task` / `GetAgent → Task<AgentDefinition>` / `DeleteAgent → Task<bool>` / `ListAgents → Task<PagedResult<AgentDefinition>>` / `FindAgentsByOwner → Task<IReadOnlyList<AgentDefinition>>` 与 [HD-002 §4.1.3](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 模板完全一致
- **流程**：`pass` — REQ-009 SaveChanges 流程 / Audit 拦截覆盖完整。证据：[HD-009 §3.3 AuditingSaveChangesInterceptor](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) `InkwellException("INK-PERSIST-012", ...)` → `ArgumentException("OwnerUserId cannot be Guid.Empty", nameof(IHasOwner.OwnerUserId))`
- **配置**：`pass` — REQ-009 EFCore Options + 三 final adapter 覆盖完整。证据：[HD-009 §6 Builder DSL 衔接](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) + §7 配置项
- **日志**：`pass` — REQ-014 OTel exception 五字段（`.type` / `.message` / `.stacktrace` / `.escaped` / `.id`）覆盖完整。证据：[HD-009 §4.3 中段集中化描述](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) — 所有 catch 块 rethrow / wrap-and-throw 前调 `Activity.SetStatus + AddException` 一次性写入，Repository / Interceptor / Seeder / MigrationRunner 不重复写入
- **监控**：`pass` — 引用 HD-002 §4.4 OTel 五字段标准（避免重复定义）。证据：[HD-009 §4.3](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) 指向 [HD-002 §4.4](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 单点定义 + [OTel exception attribute registry](https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/)
- **部署**：`pass` — REQ-009 部署 / 三 final adapter Migration 覆盖完整。证据：[HD-009 §9 部署 / 配置](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) — 沿用上轮基线，本批未改动
- **性能边界**：`pass` — REQ-009 测试覆盖率门槛 + 跨 Provider 行为契约覆盖完整。证据：[HD-009 §8 测试要求](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) §8.1~§8.4 — 沿用上轮基线，本批未改动

**完备性结论**：9 条章节 9 条 `pass`，0 条 `partial` / `missing`。

### 12.2 一致性扫描（机械化 grep + 跨文件交叉验证）

- **C-1**：`pass` — HD-009 范围内零 `Task<Result<` 签名（ADR-023 主决策）。证据：grep `Task<Result<` 匹配 = §0 callout L33 + §10 C9 grep 脚本 L775-776 + §13.2 errata 历史描述 L831-833 + §11 决策记录"端口签名形态"行；**示范代码段 0 违禁残留**
- **C-2**：`pass` — HD-009 范围内零 `INK-PERSIST-` 错误码字面量（ADR-023 errata·01）。证据：grep `INK-PERSIST-` 17 处匹配全部在 §0 callout / §13 errata 历史 / §11 决策记录 / §10 C10 grep 脚本 / §13.3 errata 描述
- **C-3**：`pass` — HD-009 范围内零 `Result.Success` / `Result.Failure` / `new Error(` 调用（ADR-023 errata·02）。证据：grep 匹配仅 §10 C11 grep 脚本 + §13.4 errata 历史描述；§3.4 / §3.10 示范代码段 0 违禁残留
- **C-4**：`pass` — HD-009 §3.10 AgentRepository 6 方法签名 ↔ HD-002 §4.1.3 模板一致。证据：6 方法签名逐字段对齐：返回类型 / 参数 / `CancellationToken ct = default`；无 `Async` 后缀（具名 Repo 例外）
- **C-5**：`pass` — HD-009 §4.3 BCL 对照表 ↔ HD-002 §4.3 BCL 对照表对齐。证据：HD-009 §4.3 标注"细化 HD-002 §4.3"，BCL 异常类型 + message 前缀 + OTel 五字段三段对齐
- **C-6**：`pass` — HD-009 §11 决策记录补 ADR-023 三轮 errata 决策行。证据：表末新增 4 行：端口签名形态 / 错误传递机制 / 错误码废除 / Result/Error 抽象删除，每行附 ADR-023 链接 + accepted date
- **C-7**：`pass` — HD-009 §13.2 / §13.3 / §13.4 三轮 errata sections 成段且日期统一为 2026-05-12。证据：grep `^### 13\.` 匹配 §13.2 / §13.3 / §13.4 三段，标题字面统一
- **C-8**：`pass` — file-structure.md `Inkwell.Abstractions` csproj 计数链 `11 + 8 + 7 = 26`。证据：L85 HD-001 + HD-002 = 19；L195 加 HD-003 = 26；L300 总计 26（从 30 减 4）；三处计数完全一致
- **C-9**：`pass` — file-structure.md 5 文件树条目删除验证。证据：grep `Result.cs` / `Error.cs` / `ErrorCodes.cs` / `ErrorCodes.Persist` / `ErrorCodes.FileStore` 文件树段（L65-78 / L184-194）零匹配；其余匹配全在 errata 描述段
- **C-10**：`pass` — database-design.md INK-PERSIST 13-row 表删除 + 节标题改为"错误处理"。证据：grep `INK-PERSIST-001` / `INK-PERSIST-013` 仅 3 处匹配，全在 errata 描述段（L91 / L140 / L155）；13-row 表体已删
- **C-11**：`pass` — 设计文档引用的源码路径与 [repo-impact-map.md](../01-requirements/repo-impact-map.md) 一致。证据：`providers/Inkwell.Persistence.EFCore/` shared base 路径与 [AGENTS.md §3.1](../../AGENTS.md) + [ADR-021](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 一致；本批 errata 未改动源码路径
- **C-12**：`pass` — HD-002 frontmatter typo 修复（B10）。证据：`status: reviewed` + `reviewers: [Inkwell]` 已 Owner 手工签字（§6.2 signature-position 禁区，AI 不代签）
- **C-13**：`partial` — HD-009 §13 编号一致性（§13 节头 → §13.2 是否跳层）。证据：grep `^### 13\.` 匹配 §13.2 / §13.3 / §13.4，**缺 §13.1**；§13 节头下方"同步追加跨模块文件"列表无子节标题。结构合规但**编号视觉跳层**：读者从 §13 直跳 §13.2 会困惑是否丢失 §13.1。详 §12.3 R1
- **C-14**：`partial` — file-structure.md L95 2026-05-11 旧 errata 未标 superseded。证据：L95 旧 errata 文字仍描述"HD-003 锁定 8 文件清单（含 `ErrorCodes.FileStore.cs`）"；L195 计数 = 7 + L197 新 errata 声明 8 → 7。从上到下读会先看到旧 8 文件、再看到新 7 文件，**局部时序矛盾**。详 §12.3 R2

**一致性结论**：14 条 12 条 `pass`，2 条 `partial`（均 non-blocking）。

### 12.3 反问清单（blocking = 0 / non-blocking = 2）

#### R1（non-blocking）：HD-009 §13 编号跳层

- **问题**：HD-009 §13 主节头"同步追加跨模块文件"下方直接出现 §13.2 / §13.3 / §13.4 三轮 errata，**缺 §13.1**。读者从 §13 顺序读会困惑是否丢失了 §13.1 内容，或误以为 §13 主节文字 = §13.1（隐式）。
- **影响范围**：H4 / H5 引用本 HD §13 errata 链时如用 anchor `#131-...` 会断链；future 维护者补充第五轮 errata 时编号会混淆（应该叫 §13.5 还是 §13.1.5？）。
- **建议方向**（不替设计师做决定，只列两条路径）：
  - 选项 A：补 §13.1 标题，把当前 §13 节头下方"同步追加跨模块文件"列表（L823-826 行附近）转为 `### 13.1 跨模块同步基线（H3 首版）` 子节。
  - 选项 B：把 §13.2 / §13.3 / §13.4 重编号为 §13.1 / §13.2 / §13.3。**注意**：此选项需同步翻 design-review-report.md / file-structure.md / database-design.md 等其他文档中所有 `§13.2 / §13.3 / §13.4` 引用链接的 anchor，工作量较大。
- **卡点等级**：`non-blocking`（不阻 reviewed 翻转；可在 HD-010 / HD-011 起草后随手补）。

#### R2（non-blocking）：file-structure.md L95 2026-05-11 旧 errata 未标 superseded

- **问题**：file-structure.md L95 的 2026-05-11 errata 文字仍声称"HD-003 锁定 `Inkwell.Abstractions/FileStorage/` 子目录**完整 8 文件清单**（`IFileStorageProvider.cs` / 4 DTO / `FileStorageOptions.cs` + Validator / **`ErrorCodes.FileStore.cs`**）"。但 L195 文件计数 = 7 + L197 新 errata 声明"HD-003 删 `ErrorCodes.FileStore.cs`，计数 8 → 7"。读者顺序读 L95 → L195 → L197 会先看到旧 8 文件、再看到新 7 文件，**局部时序矛盾**。
- **影响范围**：读者困惑当前 HD-003 锁定的文件清单到底是 7 还是 8；同类前后矛盾在 file-structure.md L297 也存在（**L297 误写"HD-001 计数 13 → 11"，按 HD-001 实际 base 数 + ADR-023 删 3 文件应为 13 → 11，与 L85 主表中 `11(HD-001)` 一致** — 这条 OK，**无矛盾**，仅 L95 一处需翻新）。
- **建议方向**：在 L95 旧 errata 末尾追加一句 `（已被 2026-05-12 errata 修订：HD-003 删 ErrorCodes.FileStore.cs，最终计数 7；详 §Inkwell.Abstractions.FileStorage L197）`。
- **卡点等级**：`non-blocking`（不阻 reviewed 翻转；errata 链式记录的精神是"旧 errata 保留 + 新 errata 标日期翻新"，本条只是缺少跨 errata 路标）。

### 12.4 评审结论与下一步

**结论锁定**：

- **完备性 9 / 9 全 pass；一致性 12 / 14 pass + 2 partial，二者均 non-blocking**
- 本轮整体 **PASS**——HD-009 + file-structure.md + database-design.md + HD-002 frontmatter typo 整改齐备
- HD-009 当前 `status: draft / reviewers: []` — 推荐 Owner 翻 `status: draft → reviewed` + `reviewers: [Inkwell]`（§6.2 signature-position 禁区，AI 不代签）
- file-structure.md / database-design.md 当前 `status: draft / reviewers: []` — 本批是 errata 增量，未改动主体设计，建议沿用现状；若 Owner 想随手翻 reviewed 亦可（这两文件本身一直随增量演进）

**下一动作建议（按优先级排序）**：

1. **立即可做**：Owner 手翻 HD-009 frontmatter `status: draft → reviewed` + `reviewers: [Inkwell]`（B10 模式，手工签字位）
2. **可推迟到 HD-010 起草前**：处理 [§12.3 R1](#r1non-blockinghd-009-13-编号跳层)（HD-009 §13 编号跳层）+ [R2](#r2non-blockingfile-structuremd-l95-2026-05-11-旧-errata-未标-superseded)（file-structure.md L95 superseded 路标）
3. **本轮收尾**：reviewer 工作结束；author 模式回退；HD-009 + HD-001 + HD-002 + HD-003 四件 H3 Abstractions / EFCore 首批设计可视为"主体设计 + 四轮 errata"完整闭环

**评审签字位**（由 Owner 在评审完成后人工补签，AI 不替签）：

- 评审日期：2026-05-12
- 评审范围：HD-009 三轮 errata 落地 + file-structure.md + database-design.md 跨 HD 同步 + HD-002 frontmatter typo 修复
- 评审决议：PASS（blocking = 0，non-blocking 改进项 = 2）
- 评审人：[ ]（待 Owner 签）

## 13. 第六轮设计层 picker 回审（2026-05-18，Owner 提出 IPersistenceProvider 价值挑战）

> **本节背景**：[§12](#12-第五轮验收评审2026-05-12hd-009--跨-hd-errata-落地后回审) 第五轮验收 PASS 6 天后，Owner 凭使用直觉提出"`IPersistenceProvider` 在 HD-009 §3.10 AgentRepository 自治 SaveChanges 落地后没有价值——调用者要实现的不是一个接口，而是 facade + 13 个具名 IXxxRepository"。reviewer 复盘 [HD-002](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 全文后发现这是已 reviewed HD 内部存在 §1.1 ↔ §3.1 文字/接口不一致（第五轮 §12.2 一致性扫描未 catch），**不是 picker 回翻而是修复内部不一致**。Owner 在 chat 中 picker A2 决议（泛型工厂入口）= 修复 §3.1 接口形态向 §1.1 早已声明的能力对齐。

### 13.1 reviewer 复盘 — §1.1 ↔ §3.1 内部不一致（第五轮失职信号）

> 本节按 [h3-detailed-design-reviewer §6 行为约束](../../.he/agents/design-reviewer/AGENT.md)（reviewer 不评"设计是否优雅"，但**内部不一致**是 reviewer 必 catch 的项目）反向追责。

**关键事实**：

- [HD-002 §1.1 职责](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#11-职责) 第 1 条 bullet 字面：「顶层 facade `IPersistenceProvider`：提供事务包装 + SaveChanges + **Repository 工厂查询能力**」
- [HD-002 §3.1 `IPersistenceProvider.cs`](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#31-persistenceipersistenceprovidercs) "对外接口"字段实际只锁了 3 个方法：`ExecuteInTransactionAsync<T>` / `ExecuteInTransactionAsync` / `SaveChangesAsync` — **缺 `GetRepository<TRepository>()` 方法**
- [HD-002 §3.3 `IUnitOfWork.cs`](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#33-persistenceiunitofworkcs) 已经有 `TRepository GetRepository<TRepository>() where TRepository : class` 方法（事务作用域内可访问）

**reviewer 复盘**：[第五轮 §12.2 一致性扫描](#122-一致性扫描机械化-grep--跨文件交叉验证) 的 14 条 C-N 全部基于 grep 字面/数字比对（csproj 计数 / `Result.cs` 残留 / `INK-PERSIST-` 错误码残留 / file-structure.md 计数链），**没有覆盖"HD §1.x 职责声明 ↔ §3.x 接口字面"语义对齐**。这条不一致直到 Owner 6 天后凭使用直觉提出"facade 没价值"才浮现——本质是 §1.1 写了"Repository 工厂查询能力"但 §3.1 接口字面漏写。

**reviewer 改进项（下一轮一致性扫描必增条目）**：在 [§12.2](#122-一致性扫描机械化-grep--跨文件交叉验证) 类似的一致性扫描里追加 **C-15 类型**：HD §1.x 职责文字 ↔ §3.x 接口字面 / 文件清单**必须逐条对齐**（grep 不够，需要逐字段读 + 比对）。本条 reviewer 自我备忘已记。

### 13.2 Owner picker 决议：A2（泛型工厂入口 `GetRepository<TRepository>()`）

**Owner 在 chat 中陈述初衷**：

> 我设计 `IPersistenceProvider` 的初衷是为了通过这一个暴露所有的需要外部实现的持久化的操作，调用者只要实现这一个接口就可以了。

reviewer 在 chat 中列三路径 picker：

- **A1 has-a 属性聚合**：`provider.Agents.AddAsync(...)`（每加新 Entity 改 facade，违反 [开闭原则](https://en.wikipedia.org/wiki/Open%E2%80%93closed_principle)）
- **A2 has-a 泛型工厂**：`provider.GetRepository<IAgentRepository>().AddAsync(...)`（对齐 [MEAI VectorStore.GetCollection 风格](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-vector-data) + [ADR-020](../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md)）
- **A3 is-a 多接口继承**：`provider.AddAgentAsync(...)`（违反 [ISP](https://en.wikipedia.org/wiki/Interface_segregation_principle) + 撞名 + 翻动词白名单）

**Owner 决议（2026-05-18）**：**A2**。理由（reviewer 转录）：

1. Provider 实现侧真正"只实现一个接口"——`IPersistenceProvider`；所有具名 `IXxxRepository` 实现都被 `GetRepository<TRepository>()` 内部托管（委托 [`IServiceProvider.GetRequiredService<T>`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.serviceproviderserviceextensions.getrequiredservice)）
2. 业务调用侧真正"只 inject 一个对象"——`IPersistenceProvider`
3. 不撞 ISP——具名 Repo 仍保持窄接口；调用语法 `provider.GetRepository<IAgentRepository>()` 显式表达"我要 Agent 这一窄面"
4. 改幅最小——HD-002 §3.1 加 1 方法，HD-009 加 1 工厂方法实现，业务 HD 改"注入哪个"描述即可
5. 与 [ADR-020 Microsoft.Extensions.VectorData 风格](../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) 同款：`VectorStore.GetCollection<TKey, TRecord>()` 是工厂入口

### 13.3 下游 errata 落地清单（不立即落代码，待 Owner 切 author 模式）

> **落地原则**：A2 实际是修复 [HD-002 §1.1 ↔ §3.1 内部不一致](#131-reviewer-复盘--11--31-内部不一致第五轮失职信号)，**不需要回 H2 走 ADR**（[HD-002 §1.1](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#11-职责) 早已声明工厂能力）；Owner 切 [h3-detailed-design-author](../../.github/agents/h3-detailed-design-author.agent.md) 模式按 E1 ~ E8 落 errata，落完回本节 §13.4 翻签字位。

**E1 — HD-002 §1.3 Q1 picker 决议描述翻新**

- 文件：[HD-002 §1.3 关键决策摘要](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#13-关键决策摘要)
- 改动：Q1 行决议描述从「`IPersistenceProvider` = facade only；具名 Repository 推迟到业务 HD」翻新为「`IPersistenceProvider` = facade with `GetRepository<TRepository>()` 泛型工厂入口；具名 `IXxxRepository` 仍由各业务 HD 起草，业务通过 `provider.GetRepository<IXxxRepository>()` / `uow.GetRepository<IXxxRepository>()` 双入口取得」
- 来源行加 `+ 2026-05-18 errata·第五轮`
- 类别：文字翻新

**E2 — HD-002 §3.1 `IPersistenceProvider.cs` 接口形态翻新**

- 文件：[HD-002 §3.1](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#31-persistenceipersistenceprovidercs)
- 改动：
  - "对外接口"字段在现有 3 方法后追加 1 方法：`TRepository GetRepository<TRepository>() where TRepository : class;`（签名与 [§3.3 `IUnitOfWork.GetRepository<T>`](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#33-persistenceiunitofworkcs) 完全对齐）
  - "职责"字段在末尾追加"Repository 工厂查询入口（事务作用域外的读路径走 facade 工厂，事务作用域内的读/写路径走 `IUnitOfWork.GetRepository<T>`，二者签名同款）"
  - "错误处理"字段追加"`GetRepository<T>` 类型未注册 → [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)（message 前缀 `\"Required repository type not registered:\"`，含请求的类型名；与 §3.3 `IUnitOfWork.GetRepository<T>` 错误语义一致）"
- 类别：接口形态翻新

**E3 — HD-002 §13 追加第五轮 errata 段**

- 文件：[HD-002 §13](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#13-errata-记录2026-05-10)
- 改动：追加 `### 13.5 2026-05-18 errata·第五轮（Q1 = A2 picker 落地）` 子节，含 (1) Owner 决策上下文（chat picker A2，链接本报告 [§13.2](#132-owner-picker-决议a2泛型工厂入口-getrepositorytrepository)） (2) §1.3 / §3.1 修改清单（E1 + E2） (3) 影响范围（HD-009 §3.2 同步 = E4）
- 类别：errata 段追加

**E4 — HD-009 §3.2 `EfCorePersistenceProvider.cs` 实现形态翻新**

- 文件：[HD-009 §3.2](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)
- 改动：
  - "对外接口"字段（公开 API 集合）在现有方法后追加 1 方法：`public TRepository GetRepository<TRepository>() where TRepository : class => _serviceProvider.GetRequiredService<TRepository>();`
  - "内部函数或类"字段追加"持有 [`IServiceProvider`](https://learn.microsoft.com/dotnet/api/system.iserviceprovider) 字段（构造期由 DI 注入），`GetRepository<TRepository>()` 委托 [`GetRequiredService<TRepository>()`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.serviceproviderserviceextensions.getrequiredservice)；未注册类型由 `GetRequiredService` 抛 `InvalidOperationException`，message 前缀对齐 HD-002 §3.1"
  - "依赖模块"字段追加 `Microsoft.Extensions.DependencyInjection.Abstractions`（[ADR-017 零外部包约束](../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) 已含此包，不引入新依赖）
- 类别：实现形态翻新

**E5 — HD-009 §13 追加第五轮 errata 段**

- 文件：[HD-009 §13](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)
- 改动：追加 `### 13.5 2026-05-18 errata·第五轮（IPersistenceProvider Q1=A2 picker 落地）` 子节，含 §3.2 修改清单（E4）+ ProjectReference 影响（[ADR-017 csproj 白名单](../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) 范围内，无新增依赖）
- 类别：errata 段追加

**E6 — HD-009 §11 决策记录追加**

- 文件：[HD-009 §11](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)
- 改动：因 §11 为显示宽度对齐的中英混排 4 列表、追加表格行会触发 markdownlint MD060（整表 16 行需重排），改为在 §11 表下方追加 note bullet 记录同一决策（追溯等价，绕开 MD060）：「`IPersistenceProvider` 增 `GetRepository<TRepository>()` 泛型工厂入口 · `EfCorePersistenceProvider` 委托 `IServiceProvider.GetRequiredService<TRepository>()` · 来源 HD-002 Q1=A2 picker(2026-05-18)」
- 类别：决策记录追加（note bullet）

**E7 — AGENTS.md §3.1 后端拓扑描述同步**

- 文件：[AGENTS.md §3.1](../../AGENTS.md)
- 改动：「`src/core/Inkwell.Abstractions/`」段内关于 `IPersistenceProvider` 的描述补一句「`IPersistenceProvider` 是**事务 + SaveChanges + Repository 工厂**三能力 facade；业务命名空间通过 `provider.GetRepository<IXxxRepository>()` 取具名 Repo（事务作用域外） / `uow.GetRepository<IXxxRepository>()`（事务作用域内）」
- 备注：AGENTS.md 顶部已声明"H2 评审 + Owner 一次性授权下的同步应用"模式；本条由 Owner 在本次 chat picker 决议中隐式授权
- 类别：AGENTS.md 翻新

**E8 — AGENTS.md §3.2 依赖规则同步**

- 文件：[AGENTS.md §3.2](../../AGENTS.md)
- 改动：在"业务命名空间 → 端口层"段追加一句「**注入风格统一**：业务命名空间统一通过 DI inject `IPersistenceProvider`，再 `provider.GetRepository<IXxxRepository>()` 拿具名 Repo；**不**直接 inject 具名 `IXxxRepository`（防止 13 个具名 Repo 重复出现在每个业务 csproj 的 ctor 参数列）。CI 强制由 [Roslyn analyzer / BannedSymbols.txt](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/) 在业务 csproj 范围内拒 ctor 参数类型 = `IXxxRepository`」
- 类别：AGENTS.md 翻新

**E1 ~ E8 类别小结**：

- **文字翻新（E1 / E3 / E5 / E7 / E8）**：picker / errata / AGENTS.md 描述对齐 — author 模式直接改即可
- **接口形态翻新（E2 / E4 / E6）**：实质改 1 个方法签名 + 1 个实现 + 1 行决策记录 — author 模式按 picker 决议落地，**不需要回 H2 走 ADR**（因为 [HD-002 §1.1](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#11-职责) 早已声明工厂能力，A2 落地实际是修复内部不一致）

**E1 ~ E8 不含**：

- 业务命名空间 HD（HD-014 / HD-015 / ...）的修订 — 这些 HD 尚未起草，新起草时按 A2 形态写即可，不需要回炉
- 三 final adapter HD（HD-010 / HD-011 / HD-012）的修订 — 这些 HD 尚未起草，新起草时引用 HD-009 §3.2 新形态即可
- 源码层修订 — 尚无 `Inkwell.Abstractions` / `Inkwell.Persistence.EFCore` 代码落地，本批纯文档级 errata

### 13.4 评审签字位

> **评审决议**：**PASS-AS-ERRATA**——不阻 [HD-002](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) / [HD-009](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) 已 reviewed 状态；E1 ~ E8 由 Owner 切 [h3-detailed-design-author](../../.github/agents/h3-detailed-design-author.agent.md) 模式落地，落地后回到本节人工签字翻 errata 闭环。

- 评审日期：2026-05-18
- 评审触发：Owner 凭使用直觉提出"`IPersistenceProvider` 在 HD-009 §3.10 AgentRepository 自治 SaveChanges 落地后没有价值"
- 评审根因：[HD-002 §1.1 ↔ §3.1 内部不一致](#131-reviewer-复盘--11--31-内部不一致第五轮失职信号)，第五轮验收 §12.2 未 catch
- 评审决议：PASS-AS-ERRATA（E1 ~ E8 errata 路线图详 [§13.3](#133-下游-errata-落地清单不立即落代码待-owner-切-author-模式)）
- 评审人：[ ]（待 Owner 签）
- errata 落地完成日期：**E1 ~ E6 = 2026-05-18**（author 模式落地：[HD-002 §1.3 Q1 / §3.1 / §13.5](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) + [HD-009 §3.2 / §11 note / §13.5](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)，均 0 lint error）；**E7 ~ E8 = 2026-05-18**（默认 Agent 落地：[AGENTS.md §3.1 / §3.2](../../AGENTS.md) + 顶部"2026-05-18 增量更新·第六轮"callout，0 lint error）

## 14. HD-004 ICacheProvider 增量评审（2026-07-05）

> 本轮在已 reviewed 的报告主体之上**追加**，仅评审增量产物：[HD-004 Inkwell.Abstractions Cache Port](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md)（status: draft，2026-07-05 起草）+ [file-structure.md `## Inkwell.Abstractions.Cache` 章节追加](file-structure.md#inkwellabstractionscache)。报告主体 §1 ~ §13 的 `status / reviewers` 字段**不**因本节调整。按 user-memory `markdown-lint.md` 已知陷阱（中英文混排长内容表必触发 MD060），本节全程以 bullet list 呈现，不使用表格。

### 14.0 评审范围与基线

- **本轮评审对象**：HD-004 全文（§1 ~ §13）+ file-structure.md `## Inkwell.Abstractions.Cache` 章节
- **不在本轮范围**：HD-001 / HD-002 / HD-003 / HD-009 / database-design.md 主体（已在前序评审中处理，本轮仅在发现跨引用缺陷时反查）
- **前置闸门**：
  - [requirements.md](../01-requirements/requirements.md) `status: reviewed` ✅
  - [repo-impact-map.md](../01-requirements/repo-impact-map.md) `status: reviewed` ✅
  - HD-004 frontmatter 完整，upstream 8 项均可定位：REQ-010（[requirements.md line 130 / 263](../01-requirements/requirements.md)）/ REQ-013（[line 133 / 266](../01-requirements/requirements.md)）/ ADR-002 / ADR-005 / ADR-016 / ADR-017 / HD-001 / ADR-023 全部真实存在
  - **不触发** [io-contracts.md §5 阻塞返回](../../.he/agents/_shared/io-contracts.md)——HD-004 是合理 per-module slice 切片，目录未"严重偏离" h3-detailed-design.md

### 14.1 完备性扫描（HD-004 范围内）

按 [h3-detailed-design.md 章节清单](../../.he/docs/stages/h3-detailed-design.md) 逐项打分：

- **文件结构**：`pass` — Cache/ 4 个 `*.cs` 全锁（`ICacheProvider.cs` / `CacheEntryOptions.cs` / `CacheOptions.cs` / `CacheOptionsValidator.cs`）+ file-structure.md `## Inkwell.Abstractions.Cache` 章节同步落地。证据：[HD-004 §2](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#2-文件结构) + [file-structure.md §Inkwell.Abstractions.Cache](file-structure.md#inkwellabstractionscache)
- **数据库**：`n/a` — 端口层不直接接 DB，HD-004 §12 显式声明 database-design.md "不贡献"。证据：[HD-004 §12](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#12-跨模块章节贡献)
- **接口 / 错误码**：`pass` — 7 方法签名齐全 + §4.1 显式声明"不分配 `INK-CACHE-NNN` 错误码"（与 ADR-023 errata 后最终态一致）。证据：[HD-004 §3.1](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#31-cacheicacheprovidercs) + [§4.1](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#41-错误码)
- **流程 / 后台任务**：`n/a` — 端口层无独立进程，§9 声明"与端口层一同打镜像（无独立部署）"。证据：[HD-004 §9](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#9-部署--配置)
- **每个目录 / 程序文件职责**：`pass` — 4 `*.cs` × 10 字段全填，无 `<TBD>` / `<待定>`。证据：[HD-004 §3.1 ~ §3.4](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#3-程序文件设计10-字段--4-文件)
- **配置文件字段 / 默认值**：`pass` — `CacheOptions` 4 字段 + 默认值 + `[Range]` 边界 + §9 appsettings.json 示例。证据：[HD-004 §3.3](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#33-cachecacheoptionscs) + [§9](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#9-部署--配置)
- **日志格式 / 字段**：`pass` — 7 `cache.<verb>` span × 4 私有字段 + 5 个 OTel `exception.*` 标准字段 + PII 提示。证据：[HD-004 §4.3](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#43-otel-span--字段)
- **监控指标 / 告警策略**：`pass` — §7.3 三档告警建议（P1 连接/超时失血 / P2 锁竞争异常 / P3 命中率异常）。证据：[HD-004 §7.3](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#73-可观测性)
- **部署步骤 / 回滚 / 备份恢复**：`partial` — 凭证位 + K8s Secret 引用明确，但 Redis 部署 / 回滚步骤留给 `providers/Inkwell.Cache.Redis` 独立 HD 起草（合理 deferral，与 [HD-003 §7.4 §1.9 partial 先例](Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md) 同模式）。证据：[HD-004 §7.2](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#72-安全) + [§9](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#9-部署--配置)
- **性能边界 / 安全边界 / 已知限制**：`pass` — §7.1 7 方法 P50/P99 预算表 + §7.2 安全（凭证位 / token 不可预测 / 缓存值不进 OTel）+ §11 4 条已知待补事项。证据：[HD-004 §7](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#7-性能--安全--可观测性) + [§11](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#11-待补--待评审)

**完备性结论**：10 项中 7 项 `pass`、2 项 `n/a`（端口层不接 DB / 不独立进程）、1 项 `partial`（部署回滚合理 deferral 到 Provider HD）、0 项 `missing`。完备性维度不卡 HD-004 翻 reviewed。

### 14.2 一致性扫描（HD-004 ↔ HD-001 / ADR-016 / ADR-023）

- **C38（FAIL）**— HD-001 §5.2 "调用方语义约定" 遗留 `InkwellException(EntityNotFound)` / `InkwellException(ConnectionFailed)` 字面量，与同文件 §5.3 BCL 对照表 + [ADR-023 errata·01 / errata·02](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) 已锁定的"仅保留 `InkwellConfigurationException` / `InkwellBuilderException` 两子类，业务失败全走 BCL 异常"完全脱节。[HD-004 §1.4](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#14-与-hd-001-51--52-命名约定的一致性声明) 引用 "`[HD-001 §5.2]` 锁定 `Get*Async` 隐含实体不存在则抛 `KeyNotFoundException`"，但 [HD-001 §5.2 原文](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#52-签名) 实际写的是 `InkwellException(EntityNotFound)`，并非 `KeyNotFoundException`——HD-004 的转述已经是"应然"（ADR-023 errata 后的正确形态），但被引用的原文本身还没跟上。证据：[HD-001 §5.2](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#52-签名)（"`Get*Async` → `Task<T>`：实体不存在抛 `InkwellException(EntityNotFound)`"、"`Exists*Async` → `Task<bool>`：仅查询，网络故障抛 `InkwellException(ConnectionFailed)`"两行）vs [HD-001 §5.3 BCL 对照表](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表) vs [HD-004 §1.4](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#14-与-hd-001-51--52-命名约定的一致性声明)。**根因在 HD-001（已 reviewed），不在 HD-004**——HD-001 §5.1 / §5.2 / §5.3 在 [§8.9.2 第四轮 errata](#892-本会话落地清单) 批量翻新时列出的受影响清单未覆盖 §5.2 这两行遗留字面量。
- **C39（PASS）**— HD-004 全文（§3 / §4 / §10 CI 自检）零 `Task<Result<` / 零 `ErrorCodes.` / 零 `Result.Success` 残留，从起草第一天直接采用 ADR-023 最终态，无历史包袱。证据：[HD-004 §10 C3 / C4](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#10-ci-自检命令grep-列表) + 全文 grep 心算
- **C40（PASS）**— OTel `exception.*` 五字段（`.type` / `.message` / `.stacktrace` / `.escaped` / `.id`）与 [HD-001 §4.2](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#42-日志结构化字段) 锁定字段完全一致。证据：[HD-004 §4.3](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#43-otel-span--字段) vs HD-001 §4.2
- **C41（PASS）**— 全 7 方法 `CancellationToken ct = default` 必填，与 [HD-001 §4.3 取消传播](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#43-取消传播) 一致；`OperationCanceledException` 不包装。证据：HD-004 §3.1 接口签名 + §4.2 参数/取消错误分类
- **C42（PASS）**— HD-004 §10 CI grep 命令全部使用多 `-e` flag（`rg -n -e 'x' -e 'y'`），未重复 [HD-003 N8（`\|` markdown 表格 escape 在 shell 执行失效）](#n810-ci-命令-rg--shell-escape-失效c16) 的坑——本轮吸取前例教训。证据：[HD-004 §10](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#10-ci-自检命令grep-列表) 全 6 条命令
- **C43（PASS）**— 跨 Provider 契约测试包路径统一 `tests/core/Inkwell.Providers.Contract/Cache/`，未重复 [HD-003 B3（测试包路径分歧）](#b3测试包路径分歧c13) 的坑。证据：[HD-004 §3.1 测试要求](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#31-cacheicacheprovidercs) + [§8.3](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#83-集成测试)
- **C44（PASS）**— `GetAsync<T>` 缓存未命中返回 `null` 不抛异常的"Cache 领域 Get 前缀例外"有显式声明 + 行业先例引用（`IDistributedCache.GetAsync` / `IMemoryCache.TryGetValue`），不是静默违反 HD-001 命名约定，符合 [HD-003 C22 式"显式偏离声明 + reviewer 反查路径"](#c22pass--14-偏离表--reviewer-反查路径) 质量门槛。证据：[HD-004 §1.4](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#14-与-hd-001-51--52-命名约定的一致性声明)
- **C45（PARTIAL）**— [ADR-016 §决策](../03-architecture/adr/ADR-016-cache-provider-redis.md) 配置 key 字面量为 `Inkwell:Cache:Provider`；HD-001 §3.11.1 F9 + HD-004 §9 最终锁定的是 `Inkwell:Providers:Cache`（选择器与详细段分离）。同类字面量漂移在 [ADR-015](../03-architecture/adr/ADR-015-object-storage-provider-switchable.md#39) 中同样存在（`Inkwell:FileStorage:Provider`），且 [HD-003 增量评审](#7-hd-003-filestorage-port-增量评审2026-05-11) 也未捕获此项——本条延续同一未清理的历史遗留，非 HD-004 独有。证据：[ADR-016 §决策"配置 key"](../03-architecture/adr/ADR-016-cache-provider-redis.md) vs [HD-001 §3.11.1](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#3111-optionsinkwellprovidersoptionscsf9-新增) vs [HD-004 §9](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#9-部署--配置)
- **C46（PARTIAL）**— file-structure.md `## Inkwell.Abstractions` 内"端口接口文件"建议段（line 93-104）仅列 Cache 2 个文件（`ICacheProvider.cs` / `CacheOptions.cs`），未含 `CacheEntryOptions.cs` / `CacheOptionsValidator.cs`；与新增的 `## Inkwell.Abstractions.Cache` 章节（4 文件完整清单）不一致，同类问题在 [HD-003 N9](#n9file-structuremd-端口接口文件建议段陈旧c17) 已有先例但未修。证据：file-structure.md line 93-104 vs [§Inkwell.Abstractions.Cache](file-structure.md#inkwellabstractionscache)
- **C47（PARTIAL）**— HD-004 §9 appsettings.json 示例中 `"Cache:Redis": { "ConnectionString": "..." }` 作为与 `"Cache"` 同级的 JSON key 字面量出现（键名本身含冒号），不符合标准 [ASP.NET Core 配置分层](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/) 的 JSON 对象嵌套写法（应为 `"Cache": { ..., "Redis": { "ConnectionString": "..." } }`）；若 H5 CodingExecutor 直接照抄该 JSON 片段会产生一个字面量键名为 `"Cache:Redis"` 的诡异配置项而非真正嵌套的 `Inkwell:Cache:Redis:ConnectionString` 路径。证据：[HD-004 §9](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#9-部署--配置)
- **C48（PARTIAL）**— HD-004 §13.1 决策记录表 `Q-scope` 行文字"A：6 方法（Get/Set/Remove/Exists/Increment/TryAcquireLock+ReleaseLock）"实际列出 7 个方法名，与 §1.1 / §1.3 一致使用的"6 类能力共 7 方法"表述不一致（表格误写"6 方法"漏计 `ReleaseLock`）。证据：[HD-004 §13.1](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#131-起草期-picker-决策2026-07-05) vs [§1.1](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#11-职责) / [§1.3](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#13-关键决策摘要)

**一致性结论**：11 项检查中 1 项 `FAIL`（C38）、4 项 `PARTIAL`（C45 ~ C48）、6 项 `PASS`（C39 ~ C44）。`FAIL` 根因在已 reviewed 的 HD-001（非 HD-004 本体缺陷），需一条小幅 errata 修复；`PARTIAL` 项均为文档精度问题，不阻塞编译或测试反推。

### 14.3 反问清单

#### Blocking

##### B11：HD-001 §5.2 遗留 `InkwellException(EntityNotFound)` / `InkwellException(ConnectionFailed)` 字面量，未随 ADR-023 errata·01/02 同步翻新（C38）

- **问题**：HD-001 §5.2"调用方语义约定"中 `Get*Async` / `Exists*Async` 两行仍写 `InkwellException(EntityNotFound)` / `InkwellException(ConnectionFailed)`；但 ADR-023 errata·01（废错误码机制）+ errata·02（删 `Common/Result.cs` / `Common/Error.cs`）已锁定"`InkwellException` 仅保留 `InkwellConfigurationException` / `InkwellBuilderException` 两个程序错误子类，业务失败一律走 BCL 异常类型"，与 HD-001 §5.3 BCL 对照表（`KeyNotFoundException` / `IOException` 等）一致。HD-004 §1.4 引用 HD-001 §5.2 时已经按"应然"（BCL 形态）转述，但读者直接翻开 HD-001 §5.2 原文会看到矛盾的旧字面量。
- **影响范围**：
  - 后续起草 HD-005（`IQueueProvider`）/ HD-006（`IAgentRuntime`）/ HD-007（`IAuditLogger`）时若直接照抄 HD-001 §5.2 字面量，会重新引入已废止的 `InkwellException(EntityNotFound)` 业务异常用法
  - H4 TestCaseAuthor 反推 TC 时若信 HD-001 §5.2 字面，会写出与 HD-002/HD-003/HD-004 实际实现（BCL 异常）不匹配的断言
  - HD-001 自身的"最终态"完整性——§5.3 已四轮 errata 但 §5.2 遗漏，导致同一 HD 内部两节自相矛盾
- **建议方向**（不替设计师下结论，仅给方向）：
  - 选项 1：HD-001 §5.2 两行替换为 BCL 形态（`Get*Async` 实体不存在 → `KeyNotFoundException`；`Exists*Async` 网络故障 → `IOException`），补一行 errata 说明"本行由 ADR-023 errata·01 后同步翻新，与 §5.3 保持一致"
  - 选项 2：§5.2 两行改为直接指向 §5.3（"失败语义详见 §5.3 BCL 对照表"），不重复维护两处容易漂移的文字
- **卡点等级**：**blocking**（建议 Owner picker 确认；修复目标是 HD-001，不改动 HD-004 本体）
- **追溯**：C38
- **✅ 已处理（2026-07-05）**：Owner picker 拍板选项 1；[HD-001 §5.2](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#52-签名) 两行字面量已改为 `KeyNotFoundException` / `IOException`，并在 [HD-001 §13 2026-07-05 errata](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#2026-07-05-errata52-调用方语义约定遗留字面量翻新b11) 追加记录；`status: reviewed` 未打回 `draft`

#### Non-blocking

##### N12：ADR-016 配置 key 字面量 `Inkwell:Cache:Provider` 未随 F9 选择器整合翻新（C45）

- **问题**：ADR-016（H2，2026-05-09）"决策"段配置 key 写 `Inkwell:Cache:Provider`；HD-001 §3.11.1（F9 errata）+ HD-004 §9 最终锁定 `Inkwell:Providers:Cache`（选择器）+ `Inkwell:Cache:*`（详细段）分离形态。同类漂移在 ADR-015（`Inkwell:FileStorage:Provider`）中同样存在且未修。
- **影响范围**：读者直接查 ADR-016 / ADR-015 会拿到过期配置 key，需要跳到 HD-001 §3.11.1 才能拿到最终态
- **建议方向**：建议一次性批量处理——起草 ADR-004 / ADR-015 / ADR-016 / ADR-018 四条 Provider 类 ADR 的"配置 key"字段统一 errata（声明均被 F9 `Inkwell:Providers:<Module>` 选择器形态取代），而非逐个 HD 增量评审时零散发现
- **卡点等级**：non-blocking
- **追溯**：C45

##### N13：file-structure.md "端口接口文件"建议段 Cache 子段仅列 2 文件（C46）

- **问题**：file-structure.md line 93-104 建议段仍只列 `ICacheProvider.cs` + `CacheOptions.cs`，未含本轮新增的 `CacheEntryOptions.cs` / `CacheOptionsValidator.cs`
- **影响范围**：reviewer / TestCaseAuthor 引用该建议段可能漏掉 2 个文件；与 [HD-003 N9](#n9file-structuremd-端口接口文件建议段陈旧c17) 同类问题，建议一次性处理而非逐轮增量发现
- **建议方向**：建议段加一行 errata 指向 `## Inkwell.Abstractions.Cache` 完整章节，或直接精化为完整清单（含 FileStorage 6 文件 + Cache 4 文件）
- **卡点等级**：non-blocking
- **追溯**：C46

##### N14：HD-004 §9 appsettings.json 示例 `"Cache:Redis"` 键名不符合 JSON 嵌套写法（C47）

- **问题**：`"Cache:Redis": { "ConnectionString": "..." }` 作为与 `"Cache"` 同级键出现，键名本身含冒号，不是标准 JSON 对象嵌套；若 H5 直接照抄会产生字面量键 `"Cache:Redis"` 而非真正的 `Inkwell:Cache:Redis:ConnectionString` 配置路径
- **影响范围**：H5 CodingExecutor 起 `appsettings.json` 骨架时可能照抄错误结构
- **建议方向**：改为 `"Cache": { "MinTtlSeconds": 1, ..., "Redis": { "ConnectionString": "..." } }` 嵌套写法
- **卡点等级**：non-blocking
- **追溯**：C47
- **✅ 已处理（2026-07-05）**：[HD-004 §9](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#9-部署--配置) appsettings.json 示例已改为标准嵌套写法，`Redis` 段落入 `Cache` 内部

##### N15：HD-004 §13.1 决策表 `Q-scope` 行"6 方法"计数与 §1.1 / §1.3"7 方法"表述不一致（C48）

- **问题**：§13.1 决策记录表字面"A：6 方法（Get/Set/Remove/Exists/Increment/TryAcquireLock+ReleaseLock）"漏计 `ReleaseLock`，实际列出 7 个方法名；§1.1 / §1.3 一致使用"6 类能力共 7 方法"表述
- **影响范围**：纯文档精度问题，不影响接口实现或测试反推（§3.1 接口签名本身就是 7 方法，无歧义）
- **建议方向**：§13.1 表格文字"A：6 方法"改为"A：6 类能力 / 7 方法"，与 §1.1 / §1.3 措辞对齐
- **卡点等级**：non-blocking
- **追溯**：C48
- **✅ 已处理（2026-07-05）**：[HD-004 §13.1](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#131-起草期-picker-决策2026-07-05) `Q-scope` 行文字已改为"A：6 类能力 / 7 方法"，与 §1.1 / §1.3 对齐

### 14.4 评审结论与下一步

- **整体评审决议**：**PASS-AS-ERRATA**——HD-004 本体设计（接口 / DTO / Options / OTel / CI 自检）完整且自洽，唯一 blocking 项（B11）的修复目标是已 reviewed 的 HD-001（一行字面量级 errata），不要求改动 HD-004 文件本身
- **HD-004 翻 `reviewed` 前置条件**：
  1. ✅ Owner 拍板 B11（picker 确认卡点等级 + 修复路径选项）——已选定选项 1（2026-07-05）
  2. ✅ AI 在 [`h3-detailed-design-author`](../../.github/agents/h3-detailed-design-author.agent.md) 模式下落 HD-001 §5.2 errata（2026-07-05 已完成，同会话顺带处理 N14 / N15；N12 / N13 按建议留待 HD-005 ~ HD-008 起草完毕后批量处理，未处理）
  3. ⬜ Owner 在 HD-004 frontmatter 翻 `status: draft → reviewed` + 填 `reviewers: [Inkwell]`（**人工签字位**，AI 不替签，尚待 Owner 执行）
- **不阻塞的后续建议**：N12（ADR 配置 key 批量 errata）与 N13（file-structure.md 建议段陈旧）仍建议积攒到 HD-005 ~ HD-008 起草完毕后一次性处理，避免逐 HD 零散 errata；N14 / N15 已随本轮处理完毕
- **后续 HD 建议路径**：HD-004 reviewed 后继续 HD-005 `IQueueProvider` + `MessageEnvelope`（[ADR-018](../03-architecture/adr/ADR-018-queue-abstraction-channels-default.md) + [RISK-015](../03-architecture/risk-analysis.md) 跨服务 trace 字段）

### 14.5 自检

- ✅ 每条 `pass` / `partial` / `n/a` / `FAIL` 都附了文件路径或具体引用
- ✅ `blocking` 反问（B11）能映射到具体一致性冲突（HD-001 §5.2 vs §5.3 vs ADR-023 errata）+ 影响范围
- ✅ 未使用"看起来" / "似乎" / "感觉"等主观词汇
- ✅ 未凭文件名臆测，每条结论都打开了对应文件读到对应字段（HD-001 §5.2 / §5.3 逐行核对）
- ✅ 未尝试用部分数据写"半个报告"——前置闸门已确认通过
- ✅ 未越界修改 HD-004 / HD-001 / file-structure.md / 报告主体
- ✅ 未给越界建议（如"建议你顺便重构 X"）
- ✅ 报告路径仍走 H3 规范默认 [docs/04-detailed-design/design-review-report.md](design-review-report.md)（追加 §14 而非新建文件）
- ✅ 全程使用 bullet list 呈现（避免中英文混排表格触发 MD060，按 user-memory 已知陷阱处理）

## 15. HD-005 IQueueProvider 增量评审（2026-07-05）

> 本轮在已 reviewed 的报告主体之上**追加**，仅评审增量产物：[HD-005 Inkwell.Abstractions Queue Port](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md)（status: draft，2026-07-05 起草）+ [file-structure.md `## Inkwell.Abstractions.Queue` 章节追加](file-structure.md#inkwellabstractionsqueue)。报告主体 §1 ~ §14 的 `status / reviewers` 字段**不**因本节调整。按 user-memory `markdown-lint.md` 已知陷阱（中英文混排长内容表必触发 MD060），本节全程以 bullet list 呈现，不使用表格。

### 15.0 评审范围与基线

- **本轮评审对象**：HD-005 全文（§1 ~ §13）+ file-structure.md `## Inkwell.Abstractions.Queue` 章节
- **不在本轮范围**：HD-001 / HD-002 / HD-003 / HD-004 / HD-009 / database-design.md 主体（已在前序评审中处理，本轮仅在发现跨引用缺陷时反查）
- **前置闸门**：
  - [requirements.md](../01-requirements/requirements.md) `status: reviewed` ✅
  - [repo-impact-map.md](../01-requirements/repo-impact-map.md) `status: reviewed` ✅
  - HD-005 frontmatter 完整，upstream 9 项均可定位：REQ-009（[requirements.md line 129 / 262](../01-requirements/requirements.md)）/ REQ-011（[line 131 / 264](../01-requirements/requirements.md)）/ REQ-014（[line 134 / 267](../01-requirements/requirements.md)）/ ADR-002 / ADR-018 / ADR-019 / ADR-023 / HD-001 / HD-004 全部真实存在
  - **不触发** [io-contracts.md §5 阻塞返回](../../.he/agents/_shared/io-contracts.md)——HD-005 是合理 per-module slice 切片，目录未"严重偏离" h3-detailed-design.md

### 15.1 完备性扫描（HD-005 范围内）

按 [h3-detailed-design.md 章节清单](../../.he/docs/stages/h3-detailed-design.md) 逐项打分：

- **文件结构**：`pass` — Queue/ 4 个 `*.cs` 全锁（`IQueueProvider.cs` / `MessageEnvelope.cs` / `QueueOptions.cs` / `QueueOptionsValidator.cs`）+ file-structure.md `## Inkwell.Abstractions.Queue` 章节同步落地，文件计数 34 与逐 HD 累加（11+8+7+4+4）吻合。证据：[HD-005 §2](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#2-文件结构) + [file-structure.md §Inkwell.Abstractions.Queue](file-structure.md#inkwellabstractionsqueue)
- **数据库**：`n/a` — 端口层不直接接 DB，HD-005 §12 显式声明 database-design.md "不贡献"。证据：[HD-005 §12](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#12-跨模块章节贡献)
- **接口 / 错误码**：`pass` — 4 方法签名齐全 + §4.1 显式声明"不分配 `INK-QUEUE-NNN` 错误码"（与 ADR-023 errata 后最终态一致）。证据：[HD-005 §3.1](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#31-queueiqueueprovidercs) + [§4.1](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#41-错误码)
- **流程 / 后台任务**：`n/a` — 端口层无独立进程，§9 声明"与端口层一同打镜像（无独立部署）"；具体 consumer 循环（`BackgroundService` / `AddInkwellWorker()`）显式移交 `Inkwell.Core` 独立 HD。证据：[HD-005 §9](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#9-部署--配置) + [§1.2](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#12-范围)
- **每个目录 / 程序文件职责**：`pass` — 4 `*.cs` × 10 字段全填，无 `<TBD>` / `<待定>`。证据：[HD-005 §3.1 ~ §3.4](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#3-程序文件设计10-字段--4-文件)
- **配置文件字段 / 默认值**：`pass` — `QueueOptions` 4 字段 + 默认值 + `[Range]` 边界 + §9 appsettings.json 嵌套示例（未重复 HD-004 N14 曾出现的扁平键名坑）。证据：[HD-005 §3.3](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#33-queuequeueoptionscs) + [§9](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#9-部署--配置)
- **日志格式 / 字段**：`pass` — 4 `queue.<verb>` span × 6 私有字段 + 5 个 OTel `exception.*` 标准字段 + PII 提示 + 跨进程 trace 恢复说明。证据：[HD-005 §4.3](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#43-otel-span--字段)
- **监控指标 / 告警策略**：`pass` — §7.3 三档告警建议（P1 连接/超时失血 / P2 Nack 速率异常 / `queue_depth` 必发 + 五项残余指标移交 RISK-014）+ 跨服务 trace correlation 验证要求。证据：[HD-005 §7.3](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#73-可观测性)
- **部署步骤 / 回滚 / 备份恢复**：`partial` — 凭证位 + K8s Secret 引用明确，但 Redis 部署 / 回滚步骤、Redis 实例复用策略、重试退避参数均合理移交 `Inkwell.Queue.Redis` 独立 HD（与 [HD-004 §7.2/§9 partial 先例](#141-完备性扫描hd-004-范围内) 同模式）。证据：[HD-005 §7.2](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#72-安全) + [§11](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#11-待补--待评审)
- **性能边界 / 安全边界 / 已知限制**：`pass` — §7.1 4 方法 P50/P99 预算表（Dequeue 改用消息可见延迟预算，理由充分）+ §7.2 安全（凭证位 / 载荷不进 OTel / TraceParent 公开字段说明）+ §11 3 条已知待补事项。证据：[HD-005 §7](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#7-性能--安全--可观测性) + [§11](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#11-待补--待评审)

**完备性结论**：10 项中 7 项 `pass`、2 项 `n/a`（端口层不接 DB / 不独立进程）、1 项 `partial`（部署回滚合理 deferral 到 Provider HD）、0 项 `missing`。完备性维度不卡 HD-005 翻 reviewed。

### 15.2 一致性扫描（HD-005 ↔ HD-001 / HD-004 / ADR-018 / ADR-019 / ADR-023）

- **C49（PASS）**— HD-005 全文（§3 / §4 / §10 CI 自检）零 `Task<Result<` / 零 `ErrorCodes.` / 零 `Result.Success` 残留，从起草第一天直接采用 ADR-023 最终态，无历史包袱。证据：[HD-005 §10 Q3 / Q4](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#10-ci-自检命令grep-列表) + 全文 grep 心算
- **C50（FAIL）**— `JsonException` 在 [HD-004 §4.2](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#42-bcl-异常分类业务失败-vs-程序错误) 被归入"**程序错误 / 失血告警（P1 / P2 告警）**"档（"`GetAsync<T>` 反序列化失败……`SetAsync<T>` 序列化失败……"），但同一异常类型在 [HD-005 §4.2](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#42-bcl-异常分类业务失败-vs-程序错误) 被归入"**业务失败 / 预期错误（调用方应 try/catch 并按业务策略处理，不触发 P1 告警）**"档（"`DequeueAsync` 枚举中遇到无法反序列化为 `T` 的消息（毒消息）……`EnqueueAsync` 序列化 `T` 失败……"）——两 HD 明确复用同一序列化决策（[HD-005 §1.3 Q-serialization](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#13-关键决策摘要)"复用 HD-004 Q-serialization 决策"），却对完全相同的异常类型给出互相矛盾的告警分级，属真实一致性冲突而非表述差异。证据：[HD-004 §4.2](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#42-bcl-异常分类业务失败-vs-程序错误) vs [HD-005 §4.2](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#42-bcl-异常分类业务失败-vs-程序错误)
- **C51（PASS）**— OTel `exception.*` 五字段（`.type` / `.message` / `.stacktrace` / `.escaped` / `.id`）与 [HD-001 §4.2](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#42-日志结构化字段) / [HD-004 §4.3](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#43-otel-span--字段) 锁定字段完全一致。证据：[HD-005 §4.3](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#43-otel-span--字段)
- **C52（PASS）**— 全 4 方法 `CancellationToken ct = default` 必填，`DequeueAsync` 使用 `[EnumeratorCancellation]` 标注，与 [HD-001 §4.3 取消传播](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#43-取消传播) + [§5.2 流式签名约定](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#52-签名) 一致；`OperationCanceledException` 不包装、枚举取消不抛到 `foreach` 外，遵循 [Microsoft 官方 `IAsyncEnumerable` 取消惯例](https://learn.microsoft.com/dotnet/csharp/asynchronous-programming/generate-consume-asynchronous-stream#stop-the-enumeration)。证据：HD-005 §3.1 接口签名 + §4.2 参数/取消错误分类
- **C53（PASS）**— 序列化决策正确复用 [HD-004 §13.1 Q-serialization](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#131-起草期-picker-决策2026-07-05)（`System.Text.Json` + `JsonSerializerOptions.Web`），未另起一套可配置项，§9 部署配置显式声明"同 HD-004 §9 决策一致，Owner『能不新增配置项就不新增』原则"。证据：[HD-005 §1.3 Q-serialization](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#13-关键决策摘要) + [§9](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#9-部署--配置)
- **C54（PASS）**— `Q-scope` 方法计数在 §1.1 / §1.3 / §13.1 三处一致均为"4 方法"，未重复 [HD-004 C48 计数偏差先例](#142-一致性扫描hd-004--hd-001--adr-016--adr-023)（HD-004 §13.1 曾漏计一个方法导致"6 方法"表述与实际 7 方法不符）。证据：[HD-005 §1.1](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#11-职责) / [§1.3](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#13-关键决策摘要) / [§13.1](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#131-起草期-picker-决策2026-07-05)
- **C55（PARTIAL）**— [HD-005 §2 csproj 依赖白名单声明](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#2-文件结构)仅列"`Microsoft.Extensions.{DependencyInjection,Configuration,Options,Logging}.Abstractions` + `Microsoft.Extensions.VectorData.Abstractions` + `System.Text.Json`"，未提及 `System.Diagnostics.Activity` / `ActivitySource`（`TraceParent` 自动捕获与 §4.3 跨进程 trace 恢复机制均依赖该命名空间）；而 [file-structure.md §Inkwell.Abstractions.Queue](file-structure.md#inkwellabstractionsqueue) 转述同一白名单时补上了"+ BCL 内置 `System.Text.Json` + `System.Diagnostics.Activity`"。两处对同一"csproj 依赖白名单"的字面表述不一致，虽不影响实际编译（`System.Diagnostics.Activity` 属运行时内置命名空间，无需额外 NuGet 引用），但作为设计文档的"权威依赖清单"存在漂移。证据：[HD-005 §2](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#2-文件结构) vs [file-structure.md §Inkwell.Abstractions.Queue](file-structure.md#inkwellabstractionsqueue)
- **C56（PARTIAL）**— [RISK-015 缓解方案第 5 项"schema 兼容性 SOP"](../03-architecture/risk-analysis.md#risk-015-webapi--worker-双进程版本漂移与-otel-双-source)（"新字段必须可选（向后兼容），废弃字段保留至少两个 release"）未在 HD-005 §1.2 / §11 待补事项中出现——§11 仅列 Redis 实例复用策略、重试退避算法参数、跨服务集成测试用例三项待移交事项，遗漏 `MessageEnvelope<T>` 自身的字段演进规则移交声明。证据：[risk-analysis.md RISK-015 缓解方案 #5](../03-architecture/risk-analysis.md#risk-015-webapi--worker-双进程版本漂移与-otel-双-source) vs [HD-005 §11](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#11-待补--待评审)
- **C57（PARTIAL）**— [ADR-019 §进程职责划分](../03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md) 声明"`Inkwell.WebApi` 仅注册 enqueue 侧 producer"/"不消费 `IQueueProvider` 队列"，但 HD-005 §3.1 的 `IQueueProvider` facade 把 Enqueue / Dequeue / Acknowledge / NegativeAcknowledge 四个方法放在同一接口上，未提供接口隔离（如拆 `IQueueProducer` / `IQueueConsumer`）或显式的"WebApi 侧禁止调用 Dequeue* / Acknowledge* / NegativeAcknowledge*"设计约束；HD-005 §8.3 仅以"`DequeueAsync` 更贴近 `Inkwell.Worker` 的 `BackgroundService.ExecuteAsync` 长驻循环模型"这一使用场景描述来"暗示"分工，未把 ADR-019 的进程职责边界固化为可机械检查的接口级约束。证据：[ADR-019 §进程职责划分表](../03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md) vs [HD-005 §3.1](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#31-queueiqueueprovidercs) / [§13.2 Q-dequeue-shape 放弃理由](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#132-候选与放弃理由)
- **C58（PARTIAL）**— [HD-005 §3.2](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#32-queuemessageenvelopecs) 与 [§4.3](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#43-otel-span--字段) 均声明 `TraceParent` 取自 `Activity.Current?.Id`（"W3C `ActivityIdFormat`"），格式引用本身准确（[`Activity.Id`](https://learn.microsoft.com/dotnet/api/system.diagnostics.activity.id) 在 `ActivityIdFormat.W3C` 下确实返回等价于 W3C `traceparent` header 的字符串），但 HD-005 全文未显式声明该机制隐含依赖 [`Activity.DefaultIdFormat`](https://learn.microsoft.com/dotnet/api/system.diagnostics.activity.defaultidformat) 已被设为（或默认即为）`ActivityIdFormat.W3C`——若某端因自定义 `ActivitySource` / 第三方 instrumentation 将 `DefaultIdFormat` 改为 `Hierarchical`，`Activity.Id` 将不再是合法 `traceparent` 格式，[RISK-015](../03-architecture/risk-analysis.md) 的跨进程 trace 串联会静默失效而非报错。证据：[HD-005 §3.2](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#32-queuemessageenvelopecs) + [§4.3](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#43-otel-span--字段)
- **C59（PASS）**— file-structure.md 文件计数（34 = 11+8+7+4+4）与 HD-005 §2 / file-structure.md `## Inkwell.Abstractions.Queue` 章节的 `Queue/` 4 文件清单双向一致；`## Inkwell.Abstractions` 主章节"端口接口文件"建议段（line 105-108）也已同步补上 HD-005 三个文件（[N13 file-structure.md 建议段陈旧问题](#143-反问清单) 未在本轮重演）。证据：[file-structure.md §Inkwell.Abstractions.Queue](file-structure.md#inkwellabstractionsqueue) + line 105-108

**一致性结论**：11 项检查中 1 项 `FAIL`（C50）、4 项 `PARTIAL`（C55 ~ C58）、6 项 `PASS`（C49 / C51 ~ C54 / C59）。`FAIL` 是 HD-004 ↔ HD-005 之间的真实告警分级矛盾，需 Owner picker 拍板统一方向；`PARTIAL` 项均为文档精度 / 移交声明缺口，不阻塞编译或测试反推。

### 15.3 反问清单

#### Blocking

##### B12：`JsonException` 告警分级在 HD-004（程序错误 / P1-P2）与 HD-005（业务失败 / 不触发 P1）之间矛盾（C50）

> **已处理（2026-07-05）**：Owner picker 拍板方向为"HD-005 对齐 HD-004"——`JsonException` 统一划入"程序错误"档（P1-P2 告警）。修复落地于 [HD-005 §4.2](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#42-bcl-异常分类业务失败-vs-程序错误)（errata 说明 + 分类调整）与 [§5.3](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#53-错误处理)（错误处理摘要同步）；HD-005 仍为 `status: draft`，未走 HD-001 §13 式的独立 errata 链式记录。

- **问题**：两份共享同一序列化决策（`System.Text.Json` + `JsonSerializerOptions.Web`）的兄弟 HD，对完全相同的异常类型给出相反的运维告警语义——HD-004 把 `JsonException` 划入"运维介入修复"档，HD-005 把 `JsonException` 划入"调用方业务策略处理、不触发 P1"档。二者不可能同时成立：若"反序列化失败 = 通常因业务侧 payload/存储值 schema 变更未兼容处理"这一根因判断正确，则该判断在 Cache 与 Queue 两个端口应同样适用（不存在"Cache 场景的 schema 漂移是运维问题、Queue 场景的 schema 漂移是业务问题"的领域差异证据）。
- **影响范围**：
  - H4 TestCaseAuthor 反推告警相关测试用例时，若信 HD-004 会为 `JsonException` 写"应触发 P1 告警"断言；若信 HD-005 会写"不应触发 P1"断言——同一异常类型在跨端口集成场景（如 KB ingest 同时经过 Cache 命中判断与 Queue 消费）下断言会自相矛盾
  - H5 CodingExecutor 实现 OTel span 异常路径与 Provider 告警规则时，需要一个唯一权威分级，否则 Cache 与 Queue 两个 Provider 家族的运维 runbook 会给出不一致的处理指引
  - `Inkwell.Queue.Redis` / `Inkwell.Cache.Redis` 两个尚未起草的 Provider HD 若各自照抄自己的端口 HD，会把矛盾固化进实现代码
- **建议方向**（不替设计师下结论，仅给方向）：
  - 选项 1：统一改为"程序错误 / P1-P2"档（对齐 HD-004 现状，因 HD-004 已 `status: reviewed`，改 HD-005 一处比改已 reviewed 的 HD-004 成本更低）
  - 选项 2：统一改为"业务失败 / 不触发 P1"档（对齐 HD-005 现状，理由：反序列化失败本质是"数据契约漂移"而非"基础设施故障"，更贴近业务侧可自愈的语义；但需要一并修订已 reviewed 的 HD-004 §4.2，产生一条新 errata）
  - 选项 3：拆分判断依据——如果反序列化失败源于**存储侧数据损坏 / 传输错误**（基础设施问题）应归程序错误档，源于**业务侧变更了 schema 但未做兼容处理**应归业务失败档；但当前两份 HD 均未提供"如何在运行时区分这两种根因"的实现指引，若选此选项需要新增判别逻辑设计
- **卡点等级**：**blocking**（需要 Owner picker 确认统一方向；由于 HD-005 目前仍是 `status: draft`，最小改动路径是在 HD-005 本体内对齐已 reviewed 的 HD-004，但最终判断权在 Owner）
- **追溯**：C50

#### Non-blocking

##### N16：HD-005 §2 csproj 依赖白名单声明遗漏 `System.Diagnostics.Activity`，与 file-structure.md 转述不一致（C55）

> **已处理（2026-07-05）**：[HD-005 §2](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#2-文件结构) csproj 依赖白名单已补 `System.Diagnostics.Activity`，与 file-structure.md 转述对齐。

- **问题**：HD-005 §2 自身文本未把 `System.Diagnostics.Activity` / `ActivitySource` 列入依赖白名单，但该命名空间是 `TraceParent` 自动捕获（[RISK-015](../03-architecture/risk-analysis.md) 硬约束）的直接依赖；file-structure.md 转述时补上了这一项，形成两处表述不同步
- **影响范围**：不影响编译（BCL 内置命名空间无需 NuGet 引用），但作为"依赖白名单"权威声明存在文档间漂移，未来若有人只读 HD-005 §2 可能误以为需要额外确认该依赖是否被允许
- **建议方向**：HD-005 §2 依赖白名单文本补一句"+ `System.Diagnostics.Activity`（BCL 内置，`TraceParent` 自动捕获所需）"，与 file-structure.md 转述对齐
- **卡点等级**：non-blocking
- **追溯**：C55

##### N17：RISK-015 缓解方案第 5 项"schema 兼容性 SOP"未在 HD-005 §11 待补事项中登记移交（C56）

> **已处理（2026-07-05）**：[HD-005 §11](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#11-待补--待评审) 已补一条"`MessageEnvelope` schema 演进规则"移交声明，指向 `Inkwell.Queue.Redis` Provider HD。

- **问题**：`MessageEnvelope<T>` 字段未来演进（新增字段是否强制可选、废弃字段最少保留几个 release）是 RISK-015 缓解方案明确列出的第 5 项，但 HD-005 §1.2 / §11 的"待移交事项"清单只覆盖了 Redis 实例复用策略、重试退避算法参数、跨服务集成测试用例三项，未提及 envelope schema 演进规则应移交给谁（`Inkwell.Queue.Redis` Provider HD，还是 HD-005 自身未来的 errata）
- **影响范围**：不影响 v1 首次交付（HD-005 §3.2 已锁定 v1 的 5 字段最小集），但若后续（如 H5 编码期间）需要给 `MessageEnvelope<T>` 加字段，缺少明确的演进规则移交对象，容易出现"该改端口 HD 还是该改 Provider HD"的归属争议
- **建议方向**：HD-005 §11 补一条移交声明，明确 schema 演进规则的责任方（建议对齐 RISK-015 缓解方案原文的隐含指向——由 `Inkwell.Queue.Redis` Provider HD 起草时锁定，因为该规则本质是 Redis Streams 消费者端向后兼容性的实现细节）
- **卡点等级**：non-blocking
- **追溯**：C56

##### N18：ADR-019"WebApi 仅注册 enqueue 侧"未在 HD-005 接口层固化为可机械检查的约束（C57）

> **待后续 HD 处理**：不在本轮修复范围内，按评审建议留到 `Inkwell.WebApi` / `Inkwell.Worker` 各自 HD 起草时处理。

- **问题**：ADR-019 明确 `Inkwell.WebApi` 不应消费队列，但 HD-005 的单一 `IQueueProvider` facade 让 Enqueue 与 Dequeue/Ack/Nack 共享同一接口类型，WebApi 侧的代码在类型系统层面完全可以调用 `DequeueAsync` / `AcknowledgeAsync` / `NegativeAcknowledgeAsync` 而不会被编译期或 CI 拦截
- **影响范围**：若未来某位业务开发者在 `Inkwell.WebApi` 中意外调用了 Dequeue 系方法（如为了"调试方便"临时加一段消费逻辑），会破坏 ADR-019 的故障隔离 / 独立扩缩设计意图，且不会有任何自动化机制发现
- **建议方向**：可选方向包括——(a) 接口拆分为 `IQueueProducer`（Enqueue）+ `IQueueConsumer`（Dequeue/Ack/Nack），`Inkwell.WebApi` 只注入前者；(b) 保持单一接口，但在 `Inkwell.WebApi` 起草独立 HD 时用 Roslyn analyzer / `BannedSymbols.txt` 风格的 CI 规则禁止该 csproj 内出现 `DequeueAsync` / `AcknowledgeAsync` / `NegativeAcknowledgeAsync` 调用；(c) 接受现状，仅在文档层面强调，不做机械强制。三个方向的成本 / 收益取舍留 Owner 判断，本 HD 不下结论
- **卡点等级**：non-blocking（不阻塞 H4 / H5 起步，`Inkwell.WebApi` / `Inkwell.Worker` 各自的 HD 尚未起草，届时补上更合适）
- **追溯**：C57

##### N19：`TraceParent` 依赖 `Activity.DefaultIdFormat = W3C` 的隐含假设未显式声明（C58）

> **已处理（2026-07-05）**：[HD-005 §4.3](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#43-otel-span--字段) 已补显式声明该隐含依赖；[§8.3](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#83-集成测试) 已补 H4 应断言 `TraceParent` 匹配 W3C 格式正则而非仅断言非空的测试要求。

- **问题**：HD-005 假定 `Activity.Current?.Id` 返回的字符串就是合法 W3C `traceparent`，这一假设仅在 `Activity.DefaultIdFormat`（或调用方显式设置）为 `ActivityIdFormat.W3C` 时成立；HD-005 全文未显式声明这一前置条件，也未声明"若上游 `ActivitySource` 配置为 `Hierarchical` 格式会发生什么"
- **影响范围**：若该假设在某个环境下不成立，RISK-015 要求的跨进程 trace 串联会**静默失效**（`TraceParent` 字段仍非 null，但内容不是合法 W3C 格式，Worker 侧 `ActivitySource.StartActivity(..., parentId: envelope.TraceParent)` 可能构造出无效的父子关系而不抛异常），比"直接报错"更难在 H4 集成测试中被发现
- **建议方向**：HD-005 §4.3 或 §7.3 补一句显式声明——"本机制假设进程内 `Activity.DefaultIdFormat = ActivityIdFormat.W3C`（.NET 5+ 默认值，Inkwell 未修改）；H4 跨服务集成测试应包含一条断言校验 `envelope.TraceParent` 匹配 W3C `traceparent` 正则（`^[0-9a-f]{2}-[0-9a-f]{32}-[0-9a-f]{16}-[0-9a-f]{2}$`），而非仅断言字段非空"
- **卡点等级**：non-blocking
- **追溯**：C58

### 15.4 评审结论与下一步

- **整体评审决议**：**PASS-AS-ERRATA**——HD-005 本体设计（接口 / DTO / Options / OTel / CI 自检）完整且自洽，与 ADR-018 / ADR-019 / ADR-023 的核心决策无冲突；唯一 blocking 项（B12）是 HD-004 ↔ HD-005 之间的告警分级矛盾，修复动作是一处小范围的分类对齐（选一档并同步另一份 HD 的对应文字），不需要重新设计接口形态或推翻任何 picker 决策
- **与 HD-004 §14.4 B11 的差异提醒**：B11 的修复目标是已 reviewed 的 HD-001（HD-004 本体不用动）；B12 的修复目标可以是仍处于 `status: draft` 的 HD-005 本身（成本更低的路径），也可以是已 reviewed 的 HD-004（需要新增 errata）——具体选哪个由 Owner picker 决定，reviewer 不代为拍板
- **HD-005 翻 `reviewed` 前置条件**：
  1. ✅ Owner picker 拍板 B12（2026-07-05，选定"HD-005 对齐 HD-004"方向）
  2. ✅ AI 在 [`h3-detailed-design-author`](../../.github/agents/h3-detailed-design-author.agent.md) 模式下按 picker 结果落地 HD-005 §4.2 / §5.3 errata（同步处理 N16 / N17 / N19 非阻塞项，N18 按评审建议留待后续 HD）
  3. ⬜ Owner 在 HD-005 frontmatter 翻 `status: draft → reviewed` + 填 `reviewers: [Inkwell]`（**人工签字位**，AI 不替签）
- **不阻塞的后续建议**：N16（依赖白名单表述对齐，已处理）/ N17（schema 演进规则移交声明，已处理）/ N18（WebApi/Worker 接口隔离，建议留到 `Inkwell.WebApi` / `Inkwell.Worker` 各自 HD 起草时处理，本轮不处理）/ N19（W3C DefaultIdFormat 假设显式声明 + H4 断言建议，已处理）均已与 B12 一并由 author 模式在 2026-07-05 同一会话落地
- **后续 HD 建议路径**：HD-005 reviewed 后继续 HD-006 `IAgentRuntime`（[REQ-003](../01-requirements/requirements.md) / [ADR-003](../03-architecture/adr/ADR-003-agent-engine-microsoft-agent-framework.md) MAF 唯一合法引用点）

### 15.5 自检

- ✅ 每条 `pass` / `partial` / `n/a` / `FAIL` 都附了文件路径或具体引用
- ✅ `blocking` 反问（B12）能映射到具体一致性冲突（HD-004 §4.2 vs HD-005 §4.2，同一异常类型跨端口矛盾分级）+ 影响范围
- ✅ 未使用"看起来" / "似乎" / "感觉"等主观词汇
- ✅ 未凭文件名臆测，每条结论都打开了对应文件读到对应字段（HD-004 §4.2 / HD-005 §4.2 逐行核对）
- ✅ 未尝试用部分数据写"半个报告"——前置闸门已确认通过
- ✅ 未越界修改 HD-005 / HD-004 / file-structure.md / 报告主体
- ✅ 未给越界建议（如"建议你顺便重构 X"）
- ✅ 报告路径仍走 H3 规范默认 [docs/04-detailed-design/design-review-report.md](design-review-report.md)（追加 §15 而非新建文件）
- ✅ 全程使用 bullet list 呈现（避免中英文混排表格触发 MD060，按 user-memory 已知陷阱处理）

## 16. HD-006 Agent Runtime Port 首轮评审（2026-07-05）

> 本轮在已 reviewed 的报告主体之上**追加**，仅评审增量产物：[HD-006 Inkwell.Abstractions Agent Runtime Port](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md)（status: draft，2026-07-05 起草）+ [file-structure.md `## Inkwell.Abstractions.AgentRuntime` 章节追加](file-structure.md#inkwellabstractionsagentruntime)。报告主体 §1 ~ §15 的 `status / reviewers` 字段**不**因本节调整。按 user-memory `markdown-lint.md` 已知陷阱（中英文混排长内容表必触发 MD060），本节全程以 bullet list 呈现，不使用表格。

### 16.0 评审范围与基线

- **本轮评审对象**：HD-006 全文（§1 ~ §13）+ file-structure.md `## Inkwell.Abstractions.AgentRuntime` 章节
- **不在本轮范围**：HD-001 / HD-002 / HD-003 / HD-004 / HD-005 / HD-009 / database-design.md 主体（已在前序评审中处理，本轮仅在发现跨引用缺陷时反查）
- **前置闸门**：
  - [requirements.md](../01-requirements/requirements.md) `status: reviewed` ✅
  - [repo-impact-map.md](../01-requirements/repo-impact-map.md) `status: reviewed` ✅
  - HD-006 frontmatter 完整，upstream 15 项均可定位：REQ-003 / REQ-004 / REQ-005 / REQ-006 / REQ-007 / REQ-008 / REQ-010 / REQ-014 / REQ-016（[requirements.md line 123-136 / 256-269](../01-requirements/requirements.md)）+ ADR-003 / ADR-011 / ADR-012 / ADR-017 / ADR-023 + HD-001 / HD-004 / HD-005 全部真实存在
  - **不触发** [io-contracts.md §5 阻塞返回](../../.he/agents/_shared/io-contracts.md)——HD-006 是合理 per-module slice 切片，目录未"严重偏离" h3-detailed-design.md

### 16.1 完备性扫描（HD-006 范围内）

按 [h3-detailed-design.md 章节清单](../../.he/docs/stages/h3-detailed-design.md) 逐项打分：

- **文件结构**：`pass` — `AgentRuntime/` 10 个 `*.cs` 全锁（`IAgentRuntime.cs` / `AgentRunRequest.cs` / `AgentTurnResult.cs` / `AgentChatMessage.cs` / `AgentMessageContentPart.cs` / `AgentModelParameters.cs` / `AgentToolDefinition.cs`（含 `AgentToolCallRecord`） / `AgentRunEvent.cs` / `AgentRuntimeOptions.cs` / `AgentRuntimeOptionsValidator.cs`）+ file-structure.md `## Inkwell.Abstractions.AgentRuntime` 章节同步落地（但清单存在遗漏，详 §16.2 C60）。证据：[HD-006 §2](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#2-文件结构) + [file-structure.md §Inkwell.Abstractions.AgentRuntime](file-structure.md#inkwellabstractionsagentruntime)
- **数据库**：`n/a` — 端口层不直接接 DB，HD-006 §12 显式声明 database-design.md "不贡献"（`AgentDefinition` 持久化已在 HD-002 覆盖）。证据：[HD-006 §12](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#12-跨模块章节贡献)
- **接口 / 错误码**：`pass` — 3 方法签名齐全 + §4.1 显式声明"不分配 `INK-AGENTRUNTIME-NNN` 错误码"（与 ADR-023 errata 后最终态一致，全文零 `Task<Result<` / 零 `Result.Success` / 零 `INK-` 字面量残留）。证据：[HD-006 §3.1](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#31-agentruntimeiagentruntimecs) + [§4.1](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#41-错误码)
- **流程 / 后台任务**：`n/a` — 端口层无独立进程，具体 Provider 实现（`AzureOpenAIAgentRuntime` / `AgentSession` 生命周期管理）显式移交 `Inkwell.Core.AgentRuntime` 独立 HD。证据：[HD-006 §1.2](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#12-范围) + [§9](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#9-部署--配置)
- **每个目录 / 程序文件职责**：`pass` — 10 `*.cs` × 10 字段全填，无 `<TBD>` / `<待定>`。证据：[HD-006 §3.1 ~ §3.10](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#3-程序文件设计10-字段--10-文件)
- **配置文件字段 / 默认值**：`pass` — `AgentRuntimeOptions` 5 字段 + 默认值 + `[Range]` 边界 + §9 appsettings.json 正确嵌套示例（`AgentRuntime` 对象内嵌套 `AzureOpenAI` 子段，未重演 [HD-004 C47 扁平键坑](#142-一致性扫描hd-004--hd-001--adr-016--adr-023)）。证据：[HD-006 §3.9](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#39-agentruntimeagentruntimeoptionscs) + [§9](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#9-部署--配置)
- **日志格式 / 字段**：`pass` — 3 个 `agentruntime.<verb>` span × 6 私有字段 + 5 个 OTel `exception.*` 标准字段 + PII 提示（对话内容 / 工具参数不进 OTel）。证据：[HD-006 §4.3](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#43-otel-span--字段)
- **监控指标 / 告警策略**：`pass` — §7.3 两档告警建议（P1 连接/超时失血 / P2 调用持续失败）+ 模型推理延迟指标移交实现层，处理方式与 [HD-005 §7.3 残余指标移交先例](#151-完备性扫描hd-005-范围内) 一致。证据：[HD-006 §7.3](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#73-可观测性)
- **部署步骤 / 回滚 / 备份恢复**：`partial` — 凭证位 + K8s Secret 引用明确，但 Azure OpenAI 凭证子 Options / 具体部署步骤合理移交 `Inkwell.Core.AgentRuntime` 独立 HD（与 [HD-004 / HD-005 §7.2 partial 先例](#141-完备性扫描hd-004-范围内) 同模式）。证据：[HD-006 §7.2](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#72-安全) + [§9](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#9-部署--配置)
- **性能边界 / 安全边界 / 已知限制**：`pass` — §7.1 3 方法 P50/P99 预算表（facade overhead，不含模型推理延迟，理由与 HD-004/HD-005 一致）+ §7.2 安全（凭证位 / 对话内容不进 OTel）+ §11 5 条已知待补事项。证据：[HD-006 §7](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#7-性能--安全--可观测性) + [§11](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#11-待补--待评审)

**完备性结论**：10 项中 7 项 `pass`、2 项 `n/a`（端口层不接 DB / 不独立进程）、1 项 `partial`（部署凭证合理 deferral 到 Provider HD）、0 项 `missing`。完备性维度不卡 HD-006 翻 reviewed。

### 16.2 一致性扫描（HD-006 ↔ HD-001 / HD-004 / HD-005 / ADR-003 / ADR-011 / ADR-012 / ADR-017 / ADR-023 + file-structure.md）

- **C60（FAIL）**— [file-structure.md `## Inkwell.Abstractions.AgentRuntime` 章节](file-structure.md#inkwellabstractionsagentruntime)的 `AgentRuntime/` 文件树遗漏 `AgentModelParameters.cs`：当前树仅列 9 个文件（`IAgentRuntime.cs` / `AgentRunRequest.cs` / `AgentTurnResult.cs` / `AgentChatMessage.cs` / `AgentMessageContentPart.cs` / `AgentToolDefinition.cs` / `AgentRunEvent.cs` / `AgentRuntimeOptions.cs` / `AgentRuntimeOptionsValidator.cs`），但 [HD-006 §2 文件结构](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#2-文件结构) + [§3.6](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#36-agentruntimeagentmodelparameterscs) 明确锁定 `AgentModelParameters.cs` 是独立第 10 个文件（`temperature`/`top_p`/`max_tokens` 字段 + `[Range]` 校验，完整 10 字段设计）。file-structure.md 文件计数文字"HD-006 新增 9 个 `*.cs`（AgentRuntime/ 9）；Abstractions csproj 累计 ... + 9（HD-006）= 43 个 `*.cs`"随之算错——应为新增 10 个、累计 44 个。证据：[file-structure.md §Inkwell.Abstractions.AgentRuntime 文件树 + 文件计数](file-structure.md#inkwellabstractionsagentruntime) vs [HD-006 §2](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#2-文件结构) / [§3.6](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#36-agentruntimeagentmodelparameterscs)
- **C61（PASS）**— HD-006 §4.4 防泄漏示例 + §10 CI 自检引用的 MAF 类型标识符（`AIAgent` / `AgentSession` / `ChatMessage` / `AgentResponse` / `AgentResponseUpdate` / `AgentRunOptions`）经核对 [microsoft/agent-framework 仓库](../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.Abstractions/) 源码真实存在（`AIAgent.cs` / `AgentSession.cs` / `AgentResponse.cs` / `AgentResponseUpdate.cs` / `AgentRunOptions.cs`），非臆造类型名。证据：`agent-framework/dotnet/src/Microsoft.Agents.AI.Abstractions/AIAgent.cs` line 251/273/296（`RunAsync` 返回 `Task<AgentResponse>`）+ `AgentSession.cs` line 59 + `AgentResponse.cs` line 28
- **C62（PASS）**— HD-006 全文（§3 / §4 / §10 CI 自检）零 `Task<Result<` / 零 `ErrorCodes.` / 零 `Result.Success`/`Result.Failure` 残留，从起草第一天直接采用 ADR-023 最终态，无历史包袱，与 [HD-004](#142-一致性扫描hd-004--hd-001--adr-016--adr-023) / [HD-005](#152-一致性扫描hd-005--hd-001--hd-004--adr-018--adr-019--adr-023) 同批次做法一致。证据：[HD-006 §10 Q4](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#10-ci-自检命令grep-列表) + 全文 grep 心算
- **C63（PASS）**— OTel `exception.*` 五字段（`.type` / `.message` / `.stacktrace` / `.escaped` / `.id`）与 [HD-001 §4.2](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#42-日志结构化字段) / [HD-004 §4.3](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#43-otel-span--字段) / [HD-005 §4.3](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#43-otel-span--字段) 锁定字段完全一致。证据：[HD-006 §4.3](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#43-otel-span--字段)
- **C64（PASS）**— 全 3 方法 `CancellationToken ct = default` 必填，`RunTurnStreamingAsync` 使用 `[EnumeratorCancellation]` 标注，与 [HD-001 §4.3 取消传播](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#43-取消传播) + [§5.2 流式签名约定](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#52-签名) 一致；被动 `ct` 取消与用户主动 `CancelRunAsync` 触发的取消统一走 `OperationCanceledException` 路径（§4.2 显式声明），与 [HD-005 `DequeueAsync` 取消惯例](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#42-bcl-异常分类业务失败-vs-程序错误) 一致。证据：HD-006 §3.1 接口签名 + §4.2
- **C65（PASS）**— `IAgentRuntime` 未套用 `I<Capability>Provider` 命名模式，HD-006 §5.1 显式记录为既定命名例外，与 [HD-004 §1.4](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#14-与-hd-001-51--52-命名约定的一致性声明) / [HD-005 §1.4](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#14-与-hd-001-51--52-命名约定的一致性声明) 同款"显式偏离声明 + reviewer 反查路径"质量门槛一致，非静默违反。证据：[HD-006 §5.1](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#51-命名)
- **C66（PASS）**— HD-006 引用的 `InkwellProvidersOptions.AgentRuntime` 字段（默认值 `"AzureOpenAI"`）与 [HD-001 §3.11.1](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#3111-optionsinkwellprovidersoptionscsf9-新增) 实际定义的 `[Required] public string AgentRuntime { get; init; } = "AzureOpenAI";` 字段完全一致；[HD-001 §3.11 `InkwellOptions`](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) 也已含 `AgentRuntimeOptions AgentRuntime` 属性锚点。证据：HD-001 §3.11 line 234 + §3.11.1 line 249 vs [HD-006 顶部 callout](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md)
- **C67（PASS）**— HD-006 多处引用"[ADR-017 §依赖规则第 3 条]"，经核对 [ADR-017 line 132](../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) 具体规则第 3 条原文确为"`Inkwell.Core.AgentRuntime` 命名空间 → 唯一允许 `using Microsoft.Agents.AI.*` 的位置"，引用准确无误。证据：[ADR-017 §依赖规则 line 132](../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md)
- **C68（PASS）**— HD-006 §9 appsettings.json 示例采用标准 JSON 嵌套写法（`"AgentRuntime": { ..., "AzureOpenAI": { "Endpoint": ..., "ApiKey": ..., "DeploymentName": ... } }`），未重演 [HD-004 C47 扁平键名坑](#142-一致性扫描hd-004--hd-001--adr-016--adr-023)。证据：[HD-006 §9](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#9-部署--配置)
- **C69（PASS）**— HD-006 §13.1 决策表 `Q-facade-scope` 行"3 方法（RunTurnAsync / RunTurnStreamingAsync / CancelRunAsync）"与 §1.1 / §3.1 实际接口方法数一致，未重演 [HD-004 C48 方法计数偏差](#142-一致性扫描hd-004--hd-001--adr-016--adr-023)。证据：[HD-006 §1.1](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#11-职责) / [§3.1](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#31-agentruntimeiagentruntimecs) / [§13.1](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#131-起草期-picker-决策2026-07-05)
- **C70（PASS）**— HD-006 §10 CI 自检命令全部使用多 `-e` flag 语法（`rg -n -e 'x' -e 'y' ...`），未重演 [HD-003 N8（`\|` markdown 表格 escape 在 shell 执行失效）](#n810-ci-命令-rg--shell-escape-失效c16) 的坑。证据：[HD-006 §10](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#10-ci-自检命令grep-列表) 全 6 条命令
- **C71（PASS）**— 跨 Provider 契约测试包路径统一 `tests/core/Inkwell.Providers.Contract/AgentRuntime/`，与 [HD-002 §8](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) / [HD-003 §8.3](Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md) / [HD-004 §8.3](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md) / [HD-005 §8.3](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md) 拓扑一致，未重演 [HD-003 B3 测试包路径分歧](#b3测试包路径分歧c13) 的坑。证据：[HD-006 §3.1 测试要求](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#31-agentruntimeiagentruntimecs) + [§8.3](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#83-集成测试)
- **C72（PASS）**— HD-006 §1.4 对 [ADR-011 自动锁屏保活](../03-architecture/adr/ADR-011-auto-lock-with-inflight-task-survival.md) 与 `CancelRunAsync`（用户主动中断）的边界声明逻辑自洽：ADR-011 锁屏保活作用于 Electron 主进程 SSE 订阅层，不触发 `CancelRunAsync`；二者路径独立、互不冲突。经对照 ADR-011 原文（"主进程不退出，也不主动断 SSE"）确认无矛盾。证据：[HD-006 §1.4](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#14-adr-011-自动锁屏保活-vs-本-hd-用户主动中断的边界声明) vs [ADR-011 §决策](../03-architecture/adr/ADR-011-auto-lock-with-inflight-task-survival.md)
- **C73（PASS）**— HD-006 §3.8 `AgentRunEvent` 6 个子类型（`TextDelta` / `ToolCallRequested` / `ToolCallResult` / `StateDelta` / `RunCompleted` / `RunError`）1:1 对应 [ADR-012](../03-architecture/adr/ADR-012-client-server-protocol-rest-agui.md) AG-UI 四大类事件（message / tool_call / state_delta / lifecycle）的映射关系经核对 ADR-012 原文（"定义了 message / tool_call / state_delta / lifecycle 等事件类型"）成立，且 HD-006 §1.2 显式声明"不锁定 AG-UI 事件到 `AgentRunEvent` 的具体映射代码（留 `Inkwell.WebApi` HD）"，边界清晰。证据：[HD-006 §3.8](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#38-agentruntimeagentruneventcs) vs [ADR-012 §上下文](../03-architecture/adr/ADR-012-client-server-protocol-rest-agui.md)
- **C74（PASS）**— HD-006 upstream 引用的 REQ-003 / REQ-004 / REQ-005 / REQ-006 / REQ-007 / REQ-008 / REQ-010 / REQ-014 / REQ-016 全部在 [requirements.md](../01-requirements/requirements.md) 真实存在，且 HD-006 正文对各 REQ 验收标准的转述（如 REQ-006"这些参数最终在调试 trace 中可见"、REQ-007"调用与返回在调试 trace 中可见；工具失败按 EX-003 处理"）与 requirements.md 原文逐字对应。证据：[requirements.md line 123-136 / 256-269](../01-requirements/requirements.md) vs HD-006 §1.1 / §3.3 / §3.7
- **C75（PARTIAL）**— HD-006 §4.4"CI 自检"引用的 grep 命令与 §10 Q1 实际列出的 grep 命令内容不一致：§4.4 写 `rg -n -e 'Microsoft\.Agents\.AI' -e 'AIAgent' -e 'AgentSession' -e 'ChatMessage' -e 'AgentResponse' src/core/Inkwell.Abstractions/AgentRuntime/`（无 `\b` 单词边界、缺 `AgentResponseUpdate` / `AgentRunOptions` 两个模式）；§10 Q1 写 `rg -n -e 'Microsoft\.Agents\.AI' -e '\bAIAgent\b' -e '\bAgentSession\b' -e '\bChatMessage\b' -e '\bAgentResponse\b' -e '\bAgentResponseUpdate\b' -e '\bAgentRunOptions\b' src/core/Inkwell.Abstractions/AgentRuntime/`（含 `\b` 边界 + 完整 7 模式）。§4.4 文字声称"详 §10 Q1"暗示两处应为同一条命令，但字面不同。证据：[HD-006 §4.4](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#44-maf-类型防泄漏机制本-hd-最核心约束的落地示例) vs [HD-006 §10 Q1](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#10-ci-自检命令grep-列表)

**一致性结论**：16 项检查中 1 项 `FAIL`（C60）、1 项 `PARTIAL`（C75）、14 项 `PASS`（C61 ~ C74）。`FAIL` 是 file-structure.md 跨模块同步遗漏，非 HD-006 本体设计缺陷；`PARTIAL` 是同一 HD 内部两处 CI 命令字面不一致的文档精度问题。

### 16.3 反问清单

#### Blocking

##### B13：file-structure.md `## Inkwell.Abstractions.AgentRuntime` 文件树遗漏 `AgentModelParameters.cs`，文件计数算错（C60）

- **问题**：file-structure.md 新增的 `## Inkwell.Abstractions.AgentRuntime` 章节文件树只列 9 个文件，遗漏 [HD-006 §3.6](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#36-agentruntimeagentmodelparameterscs) 锁定的独立文件 `AgentModelParameters.cs`（`Temperature` / `TopP` / `MaxTokens` 三字段 + `[Range]` 校验，完整 10 字段设计）；文件计数文字"HD-006 新增 9 个 `*.cs`"及累计"43 个 `*.cs`"随之算错，应为"新增 10 个"、"累计 44 个"
- **影响范围**：
  - H5 [CodingExecutor](../../.he/agents/coding-executor/AGENT.md) 若以 file-structure.md 文件树作为"要创建哪些文件"的权威清单，会漏建 `AgentModelParameters.cs`，导致 `AgentRunRequest.ModelParameters` / `AgentTurnResult.ModelParametersUsed` 字段引用的类型不存在，直接编译失败
  - H4 [TestCaseAuthor](../../.he/agents/test-case-author/AGENT.md) 反推 `AgentModelParametersTests.cs`（[HD-006 §3.6 测试要求](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#36-agentruntimeagentmodelparameterscs)已锁定）时若只看 file-structure.md 清单会遗漏该测试文件
  - 后续 HD-007（`IAuditLogger`）起草时引用"当前 Abstractions csproj 累计文件数"会拿到错误基线（43 而非 44）
- **建议方向**（不替设计师下结论，仅给方向）：
  - 选项 1：file-structure.md `## Inkwell.Abstractions.AgentRuntime` 文件树在 `AgentModelParameters.cs`（原第 6 行 `AgentToolDefinition.cs` 之前，对齐 HD-006 §2 顺序）补一行，注释沿用 HD-006 §2 原文"record，temperature/top_p/max_tokens（REQ-006）"；文件计数"9 个"→"10 个"、累计"43"→"44"
  - 选项 2：若 Owner 判断 `AgentModelParameters` 应与 `AgentToolDefinition.cs` 同文件合并（如同 `AgentToolCallRecord` 的合并模式），则需回到 HD-006 §2 / §3.6 做相应精化并同步减少一个文件计数——但 HD-006 §3.6 已有独立完整 10 字段设计，合并会破坏该章节的独立性，reviewer 更倾向选项 1
- **卡点等级**：**blocking**（建议 Owner picker 确认；修复目标是 file-structure.md 一处，不改动 HD-006 本体）
- **追溯**：C60
- **✅ 已处理（2026-07-05）**：file-structure.md `## Inkwell.Abstractions.AgentRuntime` 文件树补上 `AgentModelParameters.cs`（对齐 HD-006 §2 顺序，注释沿用 HD-006 §2 原文），文件计数“9 个”→“10 个”、累计“43”→“44”（全文 grep 确认仅此一处累计数字，无遗漏）。按建议方向选项 1 落地。

#### Non-blocking

##### N20：HD-006 §4.4 与 §10 Q1 的 MAF 类型防泄漏 grep 命令字面不一致（C75）

- **问题**：§4.4"CI 自检"标注"详 §10 Q1"暗示引用同一条命令，但 §4.4 命令缺 `\b` 单词边界、且比 §10 Q1 少两个匹配模式（`AgentResponseUpdate` / `AgentRunOptions`）
- **影响范围**：不影响实际 CI 强制效果（§10 Q1 是 CI 实际引用的权威命令表，§4.4 仅为示例性重申），但若开发者直接复制 §4.4 命令当作最终 CI 脚本使用，会得到一条覆盖面较窄、且因缺 `\b` 边界可能误报（如变量名 `myAIAgentWrapper` 含子串 `AIAgent` 会被截获）的检查
- **建议方向**：§4.4 命令直接替换为与 §10 Q1 完全一致的字面量，或改为"（命令见 §10 Q1，此处不重复）"避免维护两份易漂移的副本
- **卡点等级**：non-blocking
- **追溯**：C75
- **✅ 已处理（2026-07-05）**：HD-006 §4.4 命令替换为与 §10 Q1 完全一致的字面量（补 `\b` 单词边界 + `AgentResponseUpdate` / `AgentRunOptions` 两个模式），两处不再各自保留半套。

### 16.4 评审结论与下一步

- **整体评审决议**：**PASS-AS-ERRATA**——HD-006 本体设计（接口 / DTO / Options / MAF 零泄漏边界 / OTel / CI 自检 / ADR-003+011+012 一致性）完整且自洽，唯一 blocking 项（B13）的修复目标是 file-structure.md 跨模块同步文件（HD-006 本体不用动），且是一行文件树 + 两处计数字面量的低成本修复
- **HD-006 翻 `reviewed` 前置条件**：
  1. ✅ Owner 确认 B13 修复方向——本次为机械性事实修正（file-structure.md 遗漏字面同步），不涉及设计决策，无需 picker，直接按 reviewer 建议选项 1 落地
  2. ✅ AI 在 `h3-detailed-design-author` 模式下已落 file-structure.md 一处 errata（B13）+ HD-006 §4.4 errata（N20），详见 §16.3 各条"已处理（2026-07-05）"标记
  3. ⬜ Owner 在 HD-006 frontmatter 翻 `status: draft → reviewed` + 填 `reviewers: [Inkwell]`（**人工签字位**，AI 不替签）——前两项已完成，仅剩本项待 Owner 手工签字
- **MAF 类型零泄漏边界专项结论**：HD-006 §3 全部接口方法签名 / DTO 字段逐一核对，**未发现任何 `Microsoft.Agents.AI.*` / `Microsoft.Agents.AI.AGUI.*` / `Microsoft.Agents.AI.Workflows.*` 类型泄漏**到 public 签名；`Inkwell.Core.AgentRuntime` 唯一 MAF 接触面边界在 §4/§10 均有对应机械化 grep 检查（§10 Q1 检查端口目录内 MAF 标识符 0 命中、Q2 检查业务命名空间禁 `using Microsoft.Agents.AI`），仅 §4.4 与 §10 两处命令字面不同步（N20，non-blocking）
- **后续 HD 建议路径**：HD-006 reviewed 后继续 HD-007 `IAuditLogger`（[ADR-008](../03-architecture/adr/ADR-008-audit-log-store-and-query.md)）或视 Owner 优先级安排 `Inkwell.Core.AgentRuntime` / `Inkwell.WebApi` 独立 HD 起草

### 16.5 自检

- ✅ 每条 `pass` / `partial` / `n/a` / `FAIL` 都附了文件路径或具体引用
- ✅ `blocking` 反问（B13）能映射到具体一致性冲突（file-structure.md 文件树遗漏 + 计数算错）+ 影响范围
- ✅ 未使用"看起来" / "似乎" / "感觉"等主观词汇
- ✅ 未凭文件名臆测，每条结论都打开了对应文件读到对应字段（含跨仓库核对 `microsoft/agent-framework` 源码验证 MAF 类型真实存在）
- ✅ 未尝试用部分数据写"半个报告"——前置闸门已确认通过
- ✅ 未越界修改 HD-006 / file-structure.md / 报告主体
- ✅ 未给越界建议（如"建议你顺便重构 X"）
- ✅ 报告路径仍走 H3 规范默认 [docs/04-detailed-design/design-review-report.md](design-review-report.md)（追加 §16 而非新建文件）
- ✅ 全程使用 bullet list 呈现（避免中英文混排表格触发 MD060，按 user-memory 已知陷阱处理）

## 17. HD-007 Audit Logger Port 首轮评审（2026-07-06）

> 本轮在已 reviewed 的报告主体之上**追加**，仅评审增量产物：[HD-007 Inkwell.Abstractions Audit Logger Port](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md)（status: draft，2026-07-05 起草）+ 联动的 [HD-001 §3.7 `AuditContext` 2026-07-05 errata·第五轮](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#37-commonauditcontextcs) + [ADR-008 2026-07-05 保留期 errata](../03-architecture/adr/ADR-008-audit-log-store-and-query.md) + [file-structure.md `## Inkwell.Abstractions.Audit` 章节追加](file-structure.md#inkwellabstractionsaudit)。报告主体 §1 ~ §16 的 `status / reviewers` 字段**不**因本节调整。按 user-memory `markdown-lint.md` 已知陷阱（中英文混排长内容表必触发 MD060），本节全程以 bullet list 呈现，不使用表格。

### 17.0 评审范围与基线

- **本轮评审对象**：HD-007 全文（§1 ~ §13）+ file-structure.md `## Inkwell.Abstractions.Audit` 章节 + HD-001 §3.7 errata 联动 + ADR-008 保留期 errata 联动
- **不在本轮范围**：HD-001 / HD-002 / HD-003 / HD-004 / HD-005 / HD-006 / HD-009 主体设计（已在前序评审中处理，本轮仅在发现跨引用缺陷时反查）
- **前置闸门**：
  - [requirements.md](../01-requirements/requirements.md) `status: reviewed` ✅
  - [repo-impact-map.md](../01-requirements/repo-impact-map.md) `status: reviewed` ✅
  - HD-007 frontmatter 完整，upstream 15 项均可定位：REQ-001 / REQ-002 / REQ-007 / REQ-008 / REQ-013 / REQ-014 / REQ-015 / REQ-017 / NFR-004（[requirements.md line 121-137 / 163 / 254-268](../01-requirements/requirements.md)）+ ADR-002 / ADR-008 / ADR-017 / ADR-023 + HD-001 / HD-002 全部真实存在
  - **不触发** [io-contracts.md §5 阻塞返回](../../.he/agents/_shared/io-contracts.md)——HD-007 是合理 per-module slice 切片，目录未"严重偏离" h3-detailed-design.md

### 17.1 完备性扫描（HD-007 范围内）

按 [h3-detailed-design.md 章节清单](../../.he/docs/stages/h3-detailed-design.md) 逐项打分：

- **文件结构**：`pass` — `Audit/` 7 个 `*.cs` 全锁（`IAuditLogger.cs` / `AuditLogRequest.cs` / `AuditLogEntry.cs` / `AuditLogQuery.cs` / `AuditEnums.cs` / `AuditLoggerOptions.cs` / `AuditLoggerOptionsValidator.cs`）+ file-structure.md `## Inkwell.Abstractions.Audit` 章节文件树逐一核对与 HD-007 §2 完全一致（7 文件、顺序一致）；文件计数"HD-007 新增 7 个、累计 44（HD-001~006）+ 7 = 51 个"经手工核算无误（11+8+7+4+4+10+7=51）。证据：[HD-007 §2](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#2-文件结构) + [file-structure.md §Inkwell.Abstractions.Audit](file-structure.md#inkwellabstractionsaudit)
- **数据库**：`n/a` — 端口层不直接接 DB，HD-007 §12 显式声明 database-design.md "不贡献"，`audit_logs` 表行"锁定 HD"列保持 `TBD`，留 `Inkwell.Core.AuditLogs` 业务 HD 填写；经核对 [database-design.md line 86](database-design.md) 确认该行确为 `TBD`，与声明一致。证据：[HD-007 §12](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#12-跨模块章节贡献) vs [database-design.md line 86](database-design.md)
- **接口 / 错误码**：`pass` — 2 方法签名齐全（`LogAsync` / `QueryAsync`）+ §4.1 显式声明"不分配 `INK-AUDIT-NNN` 错误码"，与 ADR-023 最终态一致，全文零 `Task<Result<` / 零 `Result.Success` / 零 `INK-` 字面量残留（§10 C2/C3 CI 自检覆盖）。证据：[HD-007 §3.1](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#31-auditiauditloggercs) + [§4.1](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#41-错误码)
- **流程 / 后台任务**：`n/a` — 端口层无独立进程，内部重试队列 / 磁盘 fallback 文件格式 / 后台清理任务显式移交 `Inkwell.Core.AuditLogs` 独立 HD；§1.4 对"异步解耦"设计意图有专门说明段落。证据：[HD-007 §1.2](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#12-范围) + [§1.4](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#14-logasync-异步解耦声明呼应-adr-008业务事务不阻塞设计意图) + [§11](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#11-待补--待评审)
- **每个目录 / 程序文件职责**：`pass` — 7 `*.cs` × 10 字段全填，无 `<TBD>` / `<待定>`。证据：[HD-007 §3.1 ~ §3.7](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#3-程序文件设计10-字段--7-文件)
- **配置文件字段 / 默认值**：`pass` — `AuditLoggerOptions` 5 字段（`RetentionDays=180` / `MaxQueryTimeRangeDays=7` / `DefaultPageSize=50` / `MaxPageSize=200` / `EnableSensitiveDataLogging=false`）+ `[Range]` 边界 + §9 appsettings.json 正确嵌套示例（`"Inkwell:Audit"` 对齐 HD-001 `InkwellOptions.Audit` 属性名，未重演 HD-004 C47 扁平键坑）。证据：[HD-007 §3.6](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#36-auditauditloggeroptionscs) + [§9](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#9-部署--配置)
- **日志格式 / 字段**：`pass` — 2 个 `audit.<verb>` span（`audit.log` / `audit.query`，`audit.log` 另有内部子 span `audit.log.retry` / `audit.log.fallback_write`）× 6 私有字段 + 5 个 OTel `exception.*` 标准字段（`exception.id` 用 `Guid.CreateVersion7()`，与 [HD-004](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#43-otel-span--字段) / [HD-005](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#43-otel-span--字段) / [HD-006](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#43-otel-span--字段) 一致）+ 明确的 PII 边界（`AuditContext.Metadata` / `PayloadJson` 原始内容永不进 OTel）。证据：[HD-007 §4.3](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#43-otel-span--字段)
- **监控指标 / 告警策略**：`pass` — §7.3 三档告警建议（`fallback_file_written` 速率 > 0 → P1；磁盘 fallback 本身写入失败 → P1 最高优先级；`QueryAsync` 异常速率 > 5/min → P2），且显式与 [AGENTS.md §3.2](../../AGENTS.md) "不吞错"运维响应义务挂钩。证据：[HD-007 §7.3](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#73-可观测性)
- **部署步骤 / 回滚 / 备份恢复**：`partial` — `appsettings.json` 配置段完整，但磁盘 fallback 文件路径 / 权限 / 后台清理任务调度的具体部署步骤合理移交 `Inkwell.Core.AuditLogs` 独立 HD（与 [HD-004 / HD-005 / HD-006 §7.2 partial 先例](#161-完备性扫描hd-006-范围内) 同模式）。证据：[HD-007 §7.2](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#72-安全) + [§9](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#9-部署--配置)
- **性能边界 / 安全边界 / 已知限制**：`pass` — §7.1 2 方法 P50/P99 预算表（`LogAsync` 仅覆盖入队耗时；`QueryAsync` 呼应 ADR-008"≤100 万条 < 200ms"目标）+ §7.2 安全（`EnableSensitiveDataLogging` 默认 false，payload 内容永不进 OTel）+ §11 4 条已知待补事项（含显式记录的 RetentionDays/ADR-008 数字冲突）。证据：[HD-007 §7](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#7-性能--安全--可观测性) + [§11](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#11-待补--待评审)

**完备性结论**：10 项中 7 项 `pass`、2 项 `n/a`（端口层不接 DB / 不独立进程）、1 项 `partial`（部署细节合理 deferral 到 Provider HD）、0 项 `missing`。完备性维度不卡 HD-007 翻 reviewed。

### 17.2 一致性扫描（HD-007 ↔ HD-001 / HD-002 / ADR-008 / ADR-023 / AGENTS.md §3.2 / ui-spec.md UI-009 + file-structure.md）

- **C76（PASS）**— HD-007 §3.6 / §9 `AuditLoggerOptions.RetentionDays` 默认值 `180` 与 [ADR-008 §决策](../03-architecture/adr/ADR-008-audit-log-store-and-query.md#决策) 中 2026-07-05 errata 修订后的"180 天（约 6 个月）"完全一致，且与 [requirements.md line 207](../01-requirements/requirements.md) "审计日志：至少保留 6 个月（v1 默认值，可配置）"一致。证据：[HD-007 §3.6](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#36-auditauditloggeroptionscs) / [§9](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#9-部署--配置) vs [ADR-008 §决策](../03-architecture/adr/ADR-008-audit-log-store-and-query.md#决策) vs [requirements.md line 207](../01-requirements/requirements.md)
- **C77（PASS）**— `AuditContext.ActorUserId` 的 `Guid` 类型迁移已在 HD-001 §3.7（含顶部 callout + 正文 + 测试要求三处同步更新）与 HD-007 全文（`AuditLogEntry.ActorUserId: Guid` / `AuditLogQuery.ActorUserId: Guid?`）完全对齐，全仓 grep `ActorUserId` 未发现任何 `string` 类型残留引用；与 [HD-002 §3.9](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) `IHasOwner.OwnerUserId: Guid` 强一致的目标已达成。证据：[HD-001 §3.7](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#37-commonauditcontextcs) + 全文 grep `ActorUserId`（4 文件 21 处命中，无 `string` 残留）
- **C78（PASS）**— `QueryAsync` 返回类型 `Task<PagedResult<AuditLogEntry>>` 与 [HD-002 §3.4 `PagedResult<T>`](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#34-persistencepagedresultcs) 定义（`Items` / `TotalCount` / `Pagination` / `TotalPages` / `HasNextPage` / `HasPreviousPage` / `Empty(...)`）签名完全匹配，未重复定义分页返回形态。证据：[HD-007 §3.1](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#31-auditiauditloggercs) vs [HD-002 §3.4](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#34-persistencepagedresultcs)
- **C79（PASS）**— `AuditActorType`（`enum { User, Token, System }`）/ `AuditResultCode`（`enum { Success, Failure }`）与 [ADR-008 §决策](../03-architecture/adr/ADR-008-audit-log-store-and-query.md) 表结构字段"`actor_type` (`user`/`token`/`system`)"+"`result_code`"字面语义闭集一致；`AuditLogEntry` 12 字段与 ADR-008 表结构关键字段（`id`/`event_type`/`actor_type`/`actor_id`/`agent_id`/`target_kind`/`target_id`/`payload`/`result_code`/`error_code`/`request_id`/`created_at`）逐一对应。证据：[HD-007 §3.3](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#33-auditauditlogentrycs) + [§3.5](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#35-auditauditenumscs) vs [ADR-008 §决策](../03-architecture/adr/ADR-008-audit-log-store-and-query.md)
- **C80（PASS）**— OTel `exception.*` 五字段（`.type`/`.message`/`.stacktrace`/`.escaped`/`.id`）与 [HD-001 §5.3](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表) / [HD-004](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#43-otel-span--字段) / [HD-005](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md#43-otel-span--字段) / [HD-006](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#43-otel-span--字段) 锁定字段完全一致；`CancellationToken ct = default` 2 方法全填，与 [HD-001 §4.3 取消传播](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#43-取消传播) 一致。证据：[HD-007 §4.3](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#43-otel-span--字段) + [§3.1](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#31-auditiauditloggercs)
- **C81（PASS）**— `IAuditLogger` 未套用 `I<Capability>Provider` 命名模式，HD-007 §5.1 显式记录为既定命名例外（与 `IAgentRuntime` 同款），非静默违反；`InkwellProvidersOptions`（[HD-001 §3.11.1](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#3111-optionsinkwellprovidersoptionscsf9-新增) 实际 6 字段：Persistence/FileStorage/Cache/Queue/VectorStore/AgentRuntime）确未包含 `Audit` 字段，与 HD-007 §1.3 Q-implementation-topology + §10 C7 CI 自检声明一致。证据：[HD-007 §5.1](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#51-命名) + [HD-001 §3.11.1 line 253](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)
- **C82（PASS）**— file-structure.md `## Inkwell.Abstractions.Audit` 章节的文件树、依赖白名单声明、文件计数（"HD-007 新增 7 个... 累计 51 个"）与 HD-007 §2 / §12 完全对齐，未重演 [HD-006 C60 file-structure.md 遗漏 + 计数算错](#162-一致性扫描hd-006--hd-001--hd-004--hd-005--adr-003--adr-011--adr-012--adr-017--adr-023--file-structuremd) 的坑。证据：[file-structure.md §Inkwell.Abstractions.Audit](file-structure.md#inkwellabstractionsaudit) vs [HD-007 §2](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#2-文件结构)
- **C83（PARTIAL）**— HD-007 §1.2"范围"段引用"UI-009 §9.4 检索表单"作为 `AuditLogQuery` / `AuditLogEntry` 字段映射的目标章节，但经核对 [ui-spec.md §9](../01-requirements/ui-spec.md) 实际结构，"用户 / Agent / 时间范围 / 事件类型"筛选条件定义在 **§9.3 表单字段**（`9.C 审计日志 tab` 行），**§9.4 操作按钮**实际列的是"解封 / 撤销共享 / 查看条目详情"三个按钮行为（`AuditLogEntry.PayloadJson` 弹层展示对应的是 §9.4"查看条目详情"行，而非筛选表单）。引用章节号指向有误——应为"§9.3（筛选字段）+ §9.4（查看条目详情展示 payload）"而非仅"§9.4"。字段设计本身（`AuditLogQuery` 的 `ActorUserId`/`EventType`/`AgentId`/`TimeRange` 四个可选过滤项 1:1 对应 §9.3 四个筛选条件）是正确、自洽的，此项仅是文档引用章节号的机械性错误。证据：[HD-007 §1.2](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#12-范围) vs [ui-spec.md §9.3 表单字段](../01-requirements/ui-spec.md) / [§9.4 操作按钮](../01-requirements/ui-spec.md)
- **C84（PASS，附观察项）**— HD-007 §4.2 "Q-write-failure-strategy=A"（`LogAsync` 存储持续失败不向调用方抛异常）经核对 [AGENTS.md §3.2](../../AGENTS.md) "写入失败不得吞错，必须走 ADR-008 失败处理路径"原文，二者不矛盾——AGENTS.md 要求的是"走 ADR-008 失败处理路径"（重试 3 次 + 磁盘 fallback + 告警），而非要求异常必须透传给调用方；HD-007 §4.2/§4.3/§7.3 对该路径（重试 → fallback → OTel `exception.*` 记录 → P1 告警）三件套均有完整落地，满足"不吞错"（可观测 + 有持久化兜底 + 有告警升级），未构成静默丢弃。**观察项（non-blocking，供 Owner 判断）**：HD-007 §1.4 对 [ADR-008 后果](../03-architecture/adr/ADR-008-audit-log-store-and-query.md) 条款"写入路径与业务事务可同步（确保业务成功必有审计）"做了字面之外的重新解释（"同步"= 时序紧邻业务操作，非同一 DB 事务）；本 HD 采用的进程内内存队列 + 后台异步持久化设计，在"业务已提交 + 进程随即崩溃 + 审计尚未持久化"的极端窗口期理论上仍可能丢失单条审计记录，与 ADR-008 后果条款字面"确保业务成功必有审计"存在语义张力。HD-007 §4.2/§11 已就此风险做了透明记录（非隐瞒），且该问题属"设计权衡是否合理"范畴（需人工判断业务连续性优先级 vs 审计完整性優先级），超出本评审"机械一致性核查"的既定边界，故不计入 blocking，仅供 Owner 在 H4 测试用例覆盖该场景时留意。证据：[HD-007 §1.4](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#14-logasync-异步解耦声明呼应-adr-008业务事务不阻塞设计意图) + [§4.2](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#42-bcl-异常分类业务失败-vs-程序错误) vs [ADR-008 §后果](../03-architecture/adr/ADR-008-audit-log-store-and-query.md)
- **C85（PASS）**— HD-007 §11 显式记录 `RetentionDays` 默认值 180 与 ADR-008 未 errata 前的正文字面"90 天"（正文本身按 errata 惯例保留不改）仍存在数字差异，并正确声明"本 HD 不越权直接修改 `docs/03-architecture/` 文档"；经核对，该差异已由本会话另一 commit（`docs(arch): ADR-008 审计日志保留期 errata`）在 ADR-008 补充 errata 段落解决，HD-007 §11 的"待补"描述与当前 ADR-008 实际状态（errata 已落地）之间存在**时间差**——HD-007 §11 文字仍停留在"需要 Owner 补一条 errata"的措辞，未反映 errata 已完成的事实。此为文档时序滞后的良性问题（errata 先落地、HD-007 §11 措辞尚未回填更新），不影响设计正确性，建议列入 non-blocking 反问。证据：[HD-007 §11](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#11-待补--待评审) vs [ADR-008 2026-07-05 errata](../03-architecture/adr/ADR-008-audit-log-store-and-query.md)（本会话已提交）

**一致性结论**：10 项检查中 1 项 `PARTIAL`（C83）、9 项 `PASS`（C76 ~ C82、C84 附观察项、C85）。`PARTIAL` 是 HD-007 内部一处 UI 章节号引用错误，不影响字段设计本身的正确性。

### 17.3 反问清单

#### Blocking

无。本轮完备性 / 一致性扫描未发现会阻塞 `TestCaseAuthor` 或 `CodingExecutor` 起步的缺口——数据库表字段 / 接口 / DTO / OTel / Options / Guid 类型迁移均完整可执行；C83（UI 章节号引用错误）与 C85（§11 措辞未回填 errata 已落地事实）均为文档精度问题，不影响任何下游产物能否起步。

#### Non-blocking

##### N21：HD-007 §1.2 引用"UI-009 §9.4 检索表单"章节号有误，应为 §9.3（C83）

- **问题**：HD-007 §1.2"范围"段"本 HD 仅保证 `AuditLogQuery` / `AuditLogEntry` 字段可 1:1 映射到 UI-009 §9.4 检索表单"一句，实际筛选表单字段在 [ui-spec.md §9.3 表单字段](../01-requirements/ui-spec.md)（`9.C 审计日志 tab` 行："用户 / Agent / 时间范围 / 事件类型"），§9.4 是"操作按钮"（解封 / 撤销共享 / 查看条目详情）
- **影响范围**：`Inkwell.WebApi` HD 起草时若直接按"§9.4"去核对检索表单字段会找错章节；不影响 `AuditLogQuery` / `AuditLogEntry` 字段设计本身（字段与 §9.3 筛选条件、§9.4"查看条目详情"展示需求均已正确覆盖）
- **建议方向**：将引用改为"UI-009 §9.3 表单字段（筛选条件）+ §9.4 查看条目详情（`PayloadJson` 展示）"，或拆成两处引用分别对应筛选与展示两个不同的 UI 行为
- **卡点等级**：non-blocking
- **追溯**：C83
- **✅ 已处理（2026-07-06）**：HD-007 §1.2 引用已更正为"UI-009 §9.3 表单字段（筛选条件）+ §9.4 查看条目详情（`PayloadJson` 展示）"，详见 [HD-007 §1.2](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#12-范围)

##### N22：HD-007 §11 "待补"措辞未回填 ADR-008 errata 已落地的事实（C85）

- **问题**：HD-007 §11 仍写"需要 Owner 在 H2 `h2-architect-advisor` 模式下对 ADR-008 补一条 errata（90 → 180）"，但 ADR-008 的该条 errata 已在本会话通过独立 commit（`docs(arch): ADR-008 审计日志保留期 errata`）落地，`tech-selection.md` 同步 errata 是否已完成需一并确认
- **影响范围**：不影响任何下游产物起步（`RetentionDays=180` 默认值已经与落地后的 ADR-008 一致）；仅是 HD-007 §11 的措辞与当前文档状态出现短暂不同步，可能让后续读者误以为该 errata 仍待办
- **建议方向**：HD-007 §11 该条目措辞由"待办"改为"已处理"或直接删除该条待办（因阻塞项已消除），同时确认 [tech-selection.md §8](../03-architecture/tech-selection.md) 的"保留 90 天"字面是否已同步改为 180 天
- **卡点等级**：non-blocking
- **追溯**：C85
- **✅ 已处理（2026-07-06）**：HD-007 §11 措辞已由"待办"改为"已通过 errata 解决"，并补充指向 [ADR-008 2026-07-05 errata](../03-architecture/adr/ADR-008-audit-log-store-and-query.md#决策) 的引用链接；经核对 [tech-selection.md §8](../03-architecture/tech-selection.md) 已同步 errata，无遗留字面不一致

### 17.4 评审结论与下一步

- **整体评审决议**：**PASS-AS-ERRATA**——HD-007 本体设计（`IAuditLogger` facade / DTO / Options / `ActorUserId` Guid 迁移 / OTel / CI 自检 / ADR-008+023 一致性）完整且自洽，"写审计失败不得吞错"约束（[AGENTS.md §3.2](../../AGENTS.md)）经核实确由"重试 3 次 + 磁盘 fallback + OTel + P1 告警"四件套满足、非变相吞错；仅 2 项 non-blocking 文档精度问题（N21 UI 章节号引用错误、N22 §11 措辞未回填 errata 已落地事实），均不阻塞下游 `TestCaseAuthor` / `CodingExecutor` 起步
- **HD-007 翻 `reviewed` 前置条件**：
  1. ✅ Owner 确认 N21 / N22 需要在本轮一并修正（均为低成本文字修订，不涉及设计决策，无需 picker）
  2. ✅ AI 在 `h3-detailed-design-author` 模式下已落 N21（§1.2 UI 章节号引用更正）+ N22（§11 待办措辞回填）两处 errata，详见 §17.3 各条"已处理（2026-07-06）"标记
  3. ⬜ Owner 在 HD-007 frontmatter 翻 `status: draft → reviewed` + 填 `reviewers: [Inkwell]`（**人工签字位**，AI 不替签）——前两项已完成，仅剩本项待 Owner 手工签字
- **"不吞错"约束专项结论**：`LogAsync` 对调用方不抛存储异常这一设计决策，经核对 AGENTS.md §3.2 原文，满足的是"走 ADR-008 失败处理路径"（而非"异常必须透传"），HD-007 §4.2/§4.3/§7.3 的"重试→fallback→OTel exception.*→P1 告警"链路完整闭环，判定为**合规**；C84 观察项（极端崩溃窗口期的残余数据丢失风险）已被 HD-007 自身透明记录，属已知权衡而非隐瞒缺陷，不计入 blocking
- **后续 HD 建议路径**：HD-007 reviewed 后可推进 `Inkwell.Core.AuditLogs`（`DefaultAuditLogger` 具体实现 + 磁盘 fallback 文件格式 + 后台清理任务）或 `Inkwell.VectorStore`（HD-008，[ADR-020](../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md)）独立 HD 起草，视 Owner 优先级安排

### 17.5 自检

- ✅ 每条 `pass` / `partial` / `n/a` 都附了文件路径或具体引用
- ✅ 无 `blocking` 反问；2 条 `non-blocking` 反问（N21/N22）均能映射到具体一致性发现（C83/C85）+ 影响范围
- ✅ 未使用"看起来" / "似乎" / "感觉"等主观词汇
- ✅ 未凭文件名臆测，每条结论都打开了对应文件读到对应字段（含 ui-spec.md §9.3/§9.4 逐段核对、requirements.md line 207 逐字核对）
- ✅ 未尝试用部分数据写"半个报告"——前置闸门已确认通过
- ✅ 未越界修改 HD-007 / HD-001 / ADR-008 / file-structure.md / 报告主体
- ✅ 未给越界建议（如"建议你顺便重构 X"）
- ✅ 报告路径仍走 H3 规范默认 [docs/04-detailed-design/design-review-report.md](design-review-report.md)（追加 §17 而非新建文件）
- ✅ 全程使用 bullet list 呈现（避免中英文混排表格触发 MD060，按 user-memory 已知陷阱处理）

## 18. HD-008 Vector Store Type-Alias + Builder DSL 钩子首轮评审（2026-07-06）

> 本轮在已 reviewed 的报告主体之上**追加**，仅评审增量产物：[HD-008 Inkwell.Abstractions Vector Store Type-Alias](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md)（status: draft，2026-07-06 起草）+ 联动的 [HD-001 §2 / §3.11 / §3.11.1 / §14.3 追加行](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) + [file-structure.md `## Inkwell.Abstractions.VectorStore` 章节追加](file-structure.md#inkwellabstractionsvectorstore)。报告主体 §1 ~ §17 的 `status / reviewers` 字段**不**因本节调整。按 user-memory `markdown-lint.md` 已知陷阱（中英文混排长内容表必触发 MD060），本节全程以 bullet list 呈现，不使用表格。HD-008 性质特殊——**不设计新接口**，仅 type-alias 复用 + Options + Builder DSL 签名声明，故完备性判定对"接口/错误码""流程/后台任务""数据库"三维度按 HD-008 自身声明的范围收窄核查，不强套其他端口 HD 的运行期方法模板。

### 18.0 评审范围与基线

- **本轮评审对象**：HD-008 全文（§1 ~ §13）+ file-structure.md `## Inkwell.Abstractions.VectorStore` 章节 + HD-001 §2 / §3.11 / §3.11.1 / §14.3 联动追加
- **不在本轮范围**：HD-001 ~ HD-007 / HD-009 主体设计（已在前序评审中处理，本轮仅在发现跨引用缺陷时反查）；`providers/Inkwell.VectorStore.Qdrant` / `Inkwell.Core/VectorStore` 独立 HD（尚未起草，HD-008 §1.2 已声明移交）
- **前置闸门**：
  - [requirements.md](../01-requirements/requirements.md) `status: reviewed` ✅
  - [repo-impact-map.md](../01-requirements/repo-impact-map.md) `status: reviewed` ✅
  - HD-008 frontmatter 完整，upstream 9 项均可定位：REQ-009 / REQ-010（[requirements.md](../01-requirements/requirements.md)）+ ADR-003 / ADR-017 / ADR-020 / ADR-023（[adr/](../03-architecture/adr/)）+ HD-001 / HD-002 全部真实存在
  - **不触发** [io-contracts.md §5 阻塞返回](../../.he/agents/_shared/io-contracts.md)——HD-008 是合理 per-module slice 切片，目录未"严重偏离" h3-detailed-design.md

### 18.1 完备性扫描（HD-008 范围内，按 HD-008 自身声明的特殊范围收窄核查）

按 [h3-detailed-design.md 章节清单](../../.he/docs/stages/h3-detailed-design.md) 逐项打分：

- **文件结构**：`pass` — `VectorStore/` 2 个新增 `*.cs`（`VectorStoreOptions.cs` / `VectorStoreOptionsValidator.cs`）+ `GlobalUsings.cs` / `Options/InkwellOptions.cs` 两处既有文件追加行全部锁定，且明确区分"本 HD 锁定签名"与"其余独立 HD 负责实现"两类文件（`Inkwell.Core/VectorStore/` 3 文件 + `providers/Inkwell.VectorStore.Qdrant/` 2 文件，均标注非本 HD 范围）。证据：[HD-008 §2](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#2-文件结构)
- **数据库**：`n/a` — 向量数据落 Qdrant/InMemory 而非关系表，[ADR-020 §决策](../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) 已明确"不引入 appsettings.json 中的 schema 字段"、schema 走 attribute model 而非 EF Migration 风格。但 HD-008 **未像 HD-006 §12 / HD-007 §12 那样显式声明"database-design.md 不贡献"**——此为轻微完备性缺口（详 §18.3 N25）。证据：[HD-008 全文](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md)（无 §12 跨模块章节贡献 小节，§12 直接是"追溯"）vs [HD-006 §12](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#12-跨模块章节贡献) / [HD-007 §12](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#12-跨模块章节贡献) 对比模式
- **接口 / 错误码**：`pass` — HD-008 §1.1 / 顶部 callout 显式声明"本 HD 不定义任何运行期方法"，装配期失败统一走 `IValidateOptions` / `InkwellBuilderException`，与 [ADR-023](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) errata·01/02 后最终态一致；这一"无接口"性质本身即是 ADR-020 §决策锁定的既定设计（复用 M.E.VectorData，不重新发明 `IVectorStore`），非缺漏。证据：[HD-008 顶部 callout](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md) + [§1.1](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#11-职责)
- **流程 / 后台任务**：`n/a` — 端口层无独立进程，且本 HD 本身不产生运行期代码（仅 Options + Builder DSL 签名），具体 Provider 装配逻辑显式移交 3 个独立 HD。证据：[HD-008 §1.2](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#12-范围)
- **每个目录 / 程序文件职责**：`pass` — 2 个新文件 + 2 处既有文件追加均按 10 字段完整表格呈现（含既有文件追加场景下的完整字段套用，未因"只是追加"而简化字段覆盖），无 `<TBD>` / `<待定>`。证据：[HD-008 §3.1 ~ §3.4](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#3-程序文件设计10-字段--2-文件--2-处既有文件追加)
- **配置文件字段 / 默认值**：`pass` — `VectorStoreOptions` 4 字段（`EmbeddingModelName` / `EmbeddingDimensions` / `DistanceMetric` / `EnableSensitiveDataLogging`）+ 默认值 + `[Range]` 边界 + §8 appsettings.json 正确嵌套示例，字面值与 [risk-analysis.md RISK-016 缓解 #4](../03-architecture/risk-analysis.md#risk-016-inmemoryvectorstore-与-qdrant-语义偏移--microsoftextensionsvectordata-上游变化) 逐字一致。证据：[HD-008 §3.1](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#31-vectorstorevectorstoreoptionscs) + [§8](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#8-appsettingsjson-示例)
- **日志格式 / 字段**：`n/a` — §3.1 / §3.2 均显式声明"无运行期方法，不产生运行期日志"，与本 HD"仅 Options + Builder 签名"的声明范围一致，非缺漏。证据：[HD-008 §3.1](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#31-vectorstorevectorstoreoptionscs) 日志要求行
- **监控指标 / 告警策略**：`partial` — 全文未设独立"可观测性"小节（不同于 HD-004 ~ HD-007 均有的 §7.3 监控/告警子段），也未显式声明"监控指标随 Qdrant / InMemory / AzureOpenAIEmbeddings 独立 HD 落地"这一移交关系（虽可从 §1.2 范围切片段落推断，但未明文写出）。[ADR-020 §迁移路径 step 9](../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) 已提到"instrumentation 由 M.E.VectorData 内置 ActivitySource 提供"，HD-008 未引用此依据。证据：HD-008 全文无"监控" / "告警" / "alert" 关键字命中（§9 仅覆盖凭据存储位置，非监控）
- **部署步骤 / 回滚 / 备份恢复**：`partial` — §9 覆盖凭据存储位置（K8s Secret / `.env`）+ 敏感日志开关，但未显式声明"具体部署步骤（Qdrant collection 创建 / InMemory 无持久化 等）移交 Provider 独立 HD"，与 HD-004 ~ HD-007 §7.2 均有的显式 deferral 措辞不同（措辞缺失，非设计缺陷）。证据：[HD-008 §9](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#9-部署--安全说明)
- **性能边界 / 安全边界 / 已知限制**：`partial` — 安全（§9 凭据位置 + 敏感日志开关）与已知限制（§10 embedding 维度/模型变更需重建 collection + InMemory/Qdrant 语义子集差异）均完整覆盖；但**无性能边界小节**（HD-002 ~ HD-007 均有 facade 方法的 P50/P99 预算表），HD-008 §7 测试策略段落提到"无运行期方法...故无契约测试/无性能基准"作为唯一解释，逻辑自洽但未单独另起"性能边界"标题呈现，与其余 HD 的章节命名习惯有形式差异。证据：[HD-008 §7](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#7-测试策略) + [§9](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#9-部署--安全说明) + [§10](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#10-已知限制)

**完备性结论**：10 项中 5 项 `pass`、2 项 `n/a`（端口层不接 DB / 本 HD 无运行期方法故无独立日志）、3 项 `partial`（监控告警缺显式小节、部署步骤缺显式 deferral 措辞、性能边界缺独立标题）。3 项 `partial` 均为**文档呈现形式缺口**（内容实质已在别处覆盖或可推断），非实质设计缺陷，不卡 HD-008 翻 `reviewed`（详 §18.3 N26 / N27 / N28）。

### 18.2 一致性扫描（HD-008 ↔ HD-001 / HD-006 / ADR-020 / AGENTS.md §3.1 §3.2 + file-structure.md）

- **C86（PASS）**— HD-008 §1.1 "命名空间级 `global using`（非逐类型别名）"技术论证（C# `using` 别名不支持开放泛型）准确；[HD-001 §14.3](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#143-global-usings) 的 `GlobalUsings.cs` 清单已实际同步追加 `global using Microsoft.Extensions.VectorData;` 一行（注释"HD-008 起用"），且 [HD-001 §2 csproj 依赖白名单注释](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#2-文件结构) 也已同步标注 `Microsoft.Extensions.VectorData.Abstractions (HD-008 起用)`——HD-008 与 HD-001 的联动追加**已真实落地**，非仅停留在 HD-008 单方声明。证据：[HD-001 §14.3 line 557](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#143-global-usings) + [HD-001 §2](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#2-文件结构) vs [HD-008 §3.3](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#33-globalusingscs-追加行hd-001-既有文件)
- **C87（PASS）**— [HD-001 §3.11](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#311-optionsinkwelloptionscs) `InkwellOptions` 对外接口实际已含 `public VectorStoreOptions VectorStore { get; init; } = new();`（插入在 `AuditLoggerOptions Audit` 之后），[HD-001 §3.11.1](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#3111-optionsinkwellprovidersoptionscsf9-新增) `InkwellProvidersOptions.VectorStore` 字段（默认值 `"InMemory"`）也已存在——均与 HD-008 §3.4 / §5 的引用逐字匹配，联动追加真实落地。证据：[HD-001 §3.11](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#311-optionsinkwelloptionscs) + [§3.11.1](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#3111-optionsinkwellprovidersoptionscsf9-新增) vs [HD-008 §3.4](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#34-optionsinkwelloptionscs-追加字段hd-001-既有文件) / [§5](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#5-inkwellprovidersoptionsvectorstore-取值白名单)
- **C88（PASS）**— HD-008 §1.3 Q1 / Q2 声明 `AzureOpenAIEmbeddingOptions`（Endpoint/ApiKey/DeploymentName）与 `QdrantVectorStoreOptions`（Host/Port/ApiKey/UseHttps）分别独立于 `Inkwell.Core/VectorStore/` 与 `providers/Inkwell.VectorStore.Qdrant/`，且与 [HD-006 `AzureOpenAIAgentRuntimeOptions`](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) 单实现拓扑一致——经核对 HD-006 实际字段（Endpoint/ApiKey/DeploymentName），两者字段命名模式确实同构；§13 Q6 "两组凭据各自独立配置段，即使指向同一 Azure OpenAI 资源"的决策与 HD-006 现状（`AzureOpenAIAgentRuntimeOptions` 是 Chat 模型专用、不与任何其他端口共用）互不冲突，无重复定义或字段撞名风险。证据：[HD-006 §9 appsettings.json 示例](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#9-部署--配置) vs [HD-008 §13 Q6](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#13-决策记录)
- **C89（PASS）**— file-structure.md `## Inkwell.Abstractions.VectorStore` 章节的文件树、依赖白名单声明、文件计数（"HD-008 新增 2 个... 累计 53 个"）与 HD-008 §2 逐一核对一致（11+8+7+4+4+10+7+2=53 手工核算无误），未重演 [HD-006 C60 file-structure.md 遗漏 + 计数算错](#162-一致性扫描hd-006--hd-001--hd-004--hd-005--adr-003--adr-011--adr-012--adr-017--adr-023--file-structuremd) 的坑。证据：[file-structure.md §Inkwell.Abstractions.VectorStore](file-structure.md#inkwellabstractionsvectorstore) vs [HD-008 §2](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#2-文件结构)
- **C90（FAIL）**— [file-structure.md §Inkwell.Abstractions.VectorStore 结尾 callout](file-structure.md#inkwellabstractionsvectorstore) 仍写"`IEmbeddingGenerator<string, Embedding<float>>` 在 KB / Memory 业务命名空间的直接消费方式是否需要为 `Microsoft.Extensions.AI.Abstractions` 开依赖白名单例外，仍是**待 Owner 确认的开放问题**（HD-008 §11）"，但 [HD-008 §11](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#11-待补--待评审) 本身已明确声明"原留的 2 条需要 Owner 确认开放问题已由 Owner 在 chat picker（2026-07-06）确认拍板...不再是待评审项"——file-structure.md 的该处 callout 未随 HD-008 §11 / §13 Q5 的拍板结果同步更新，两处文档对同一问题的"是否仍开放"状态直接矛盾。证据：[file-structure.md line 385](file-structure.md#inkwellabstractionsvectorstore) vs [HD-008 §11](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#11-待补--待评审) / [§13 Q5](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#13-决策记录)
- **C91（FAIL）**— HD-008 §13 Q5 / §6 将"`IEmbeddingGenerator<string, Embedding<float>>` 允许业务命名空间直接注入"的决策依据表述为"比照本 HD 自身对 `Microsoft.Extensions.VectorData.Abstractions` 的处理先例"，但两者的**物理实现机制并不对称**：VectorData 的先例是——`Microsoft.Extensions.VectorData.Abstractions` 已被 HD-008 本身**实际添加**到 `Inkwell.Abstractions.csproj` 的 `PackageReference` 白名单（[HD-001 §2](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#2-文件结构) 注释"HD-008 起用"）+ `GlobalUsings.cs` 追加 `global using`（[HD-001 §14.3](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#143-global-usings)），使得业务命名空间通过**依赖 `Inkwell.Abstractions` 项目引用**（符合 [AGENTS.md §3.2](../../AGENTS.md) "业务命名空间只能依赖 `Inkwell.Abstractions` + BCL"规则）间接获得该类型的编译期可见性——业务代码并未新增任何直接 `PackageReference`。而 `Microsoft.Extensions.AI.Abstractions`（`IEmbeddingGenerator<,>` 所在包）**未**被 HD-008 添加到 `Inkwell.Abstractions.csproj` 白名单，也**未**在 `GlobalUsings.cs` 追加对应 `global using`——若 `Inkwell.Core.KnowledgeBase` / `.Memory` 要直接注入 `IEmbeddingGenerator<string, Embedding<float>>`，唯一可行路径是在 `Inkwell.Core.csproj` 本身新增该包的 `PackageReference`，这正是 [AGENTS.md §3.2](../../AGENTS.md) 依赖纯度原则明确禁止的模式（业务命名空间不得引入 `Inkwell.Abstractions` + BCL 之外的第三方包），且会被 [CI Roslyn analyzer / `BannedSymbols.txt`](../../AGENTS.md) 拦下。换言之，Q5 决策所"比照"的先例其实要求"把 `Microsoft.Extensions.AI.Abstractions` 也纳入 `Inkwell.Abstractions.csproj` 白名单 + `GlobalUsings.cs`"这一步，但 HD-008 并未做这一步，也未把这一步移交给任何后续 HD / ADR——KB / Memory 业务 HD 起草时会在此处卡壳（不知道 `IEmbeddingGenerator<,>` 的包依赖到底该走 `Inkwell.Abstractions` 白名单扩展、还是走 `Inkwell.Core.csproj` 直接引用的例外授权）。证据：[HD-008 §6](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#6-与-iembeddinggeneratortinput-tembedding-的衔接) + [§13 Q5](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#13-决策记录) vs [AGENTS.md §3.2](../../AGENTS.md) vs [HD-001 §2](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#2-文件结构) / [§14.3](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#143-global-usings)（VectorData 实际落地对照）
- **C92（PASS）**— HD-008 §4 Builder DSL 签名（`UseInMemoryVectorStore` / `UseAzureOpenAIEmbeddings` / `UseQdrantVectorStore`，均 `this IInkwellBuilder builder` 扩展方法、返回 `IInkwellBuilder`）与 [HD-001 §6.3 Provider 扩展方法约定](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#63-provider-扩展方法约定给-hd-002--hd-008-的契约)"唯一入口扩展 `UseXxx(this IInkwellBuilder builder, ...)`"一致；`Action<TOptions>` 参数风格与 [HD-006 `UseAzureOpenAIAgentRuntime(this IInkwellBuilder, Action<AzureOpenAIAgentRuntimeOptions>)`](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) 同构，非本 HD 独创风格。**观察项（non-blocking）**：[HD-001 §6.1 示例代码](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#61-典型用法与-architecturemd-3-示例对齐) 里 `.UseQdrantVectorStore(builder.Configuration.GetConnectionString("Qdrant"))` 用的是单字符串参数风格，与 HD-008 §4 锁定的 `Action<QdrantVectorStoreOptions>` 签名不同；但该差异是 HD-001 §6.1 示例本身的历史遗留问题（HD-002/HD-004/HD-005/HD-006 的实际签名也都与 HD-001 §6.1 各自的插图代码不完全一致，属 HD-001 起草时的占位插图，非本 HD 新引入的不一致），不计入本轮 blocking。证据：[HD-008 §4](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#4-builder-dsl-签名声明不含实现) vs [HD-001 §6.1](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#61-典型用法与-architecturemd-3-示例对齐) / [§6.3](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#63-provider-扩展方法约定给-hd-002--hd-008-的契约)
- **C93（PASS）**— HD-008 §5 `Providers.VectorStore` 不一致时的错误信息模板 `InkwellBuilderException($"Provider registration conflict for VectorStore: configured={{Providers.VectorStore}}, registered=...")` 与 [HD-002 §Provider 一致性校验](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 实际采用的模板"`Provider registration conflict for Persistence: configured=<x>, called=<y>`"语义一致（均为"配置值 vs 实际调用值"两段式），仅第二段变量名"registered"/"called"字面不同，不影响语义，非 blocking。证据：[HD-008 §5](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#5-inkwellprovidersoptionsvectorstore-取值白名单) vs [HD-002 line 493](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)
- **C94（PASS）**— HD-008 §9 "v1 不引入 Azure Key Vault"表述与 [AGENTS.md §2.5](../../AGENTS.md) / [OQ-A006 closed §B](../03-architecture/open-questions-arch.md) 一致；Qdrant / InMemory 双 Provider 落点（`providers/Inkwell.VectorStore.Qdrant/` + `Inkwell.Core/VectorStore/`）与 [AGENTS.md §3.1](../../AGENTS.md) 现状条目、[ADR-020 §决策物理布局](../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) 完全一致。证据：[HD-008 §9](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#9-部署--安全说明) vs [AGENTS.md §2.5 / §3.1](../../AGENTS.md)

**一致性结论**：9 项检查中 2 项 `FAIL`（C90 / C91）、7 项 `PASS`（C86 ~ C89、C92 ~ C94）。两项 `FAIL` 均围绕同一根问题——`IEmbeddingGenerator<,>` 依赖白名单例外的物理落地机制未完成，且 file-structure.md 未同步 HD-008 §11 已拍板的事实。

### 18.3 反问清单

#### Blocking

##### B14：file-structure.md `## Inkwell.Abstractions.VectorStore` 结尾 callout 与 HD-008 §11 / §13 Q5 状态矛盾（C90）

- **问题**：file-structure.md 该章节结尾仍写"`IEmbeddingGenerator<,>` 依赖白名单例外仍是待 Owner 确认的开放问题（HD-008 §11）"，但 HD-008 §11 本身已明确该问题"已由 Owner 在 chat picker（2026-07-06）确认拍板...不再是待评审项"
- **影响范围**：后续读者（H4 TestCaseAuthor / KB·Memory 业务 HD 起草者）若先读 file-structure.md 会误判该决策仍待定，重复向 Owner 发起已拍板过的问题；两份文档对同一问题的状态描述直接矛盾，破坏"单一事实源"原则
- **建议方向**：将 file-structure.md 该处 callout 更新为反映 HD-008 §13 Q5 的实际拍板结果（"允许直接注入，不新增门面接口"），但**同时**需先解决 B15（因为 Q5 的拍板结果本身尚缺物理落地机制，callout 更新时应如实反映"决策已拍板，但依赖白名单物理机制待补"这一现状，而非简单删除开放问题标记）
- **卡点等级**：**blocking**
- **追溯**：C90
- **状态**：已处理（2026-07-06）——file-structure.md `## Inkwell.Abstractions.VectorStore` 结尾 callout 已更新为"已确认：`Microsoft.Extensions.AI.Abstractions` 对称纳入白名单，允许直接注入、不新增门面接口，不再是开放问题"，随 B15 一并解决。

##### B15：Q5"比照 VectorData 先例"缺物理落地机制，`IEmbeddingGenerator<,>` 依赖白名单例外未实际生效（C91）

- **问题**：HD-008 §6 / §13 Q5 决策"`IEmbeddingGenerator<string, Embedding<float>>` 允许 `Inkwell.Core.KnowledgeBase` / `.Memory` 直接注入，比照本 HD 对 `Microsoft.Extensions.VectorData.Abstractions` 的处理先例"，但 VectorData 先例的实际机制是"把包加进 `Inkwell.Abstractions.csproj` 白名单 + `GlobalUsings.cs` 追加 `global using`，让业务命名空间通过依赖 `Inkwell.Abstractions` 间接可见该类型"；`Microsoft.Extensions.AI.Abstractions` 未经历同样的落地步骤。若 KB / Memory 业务命名空间要直接注入 `IEmbeddingGenerator<,>`，唯一可行路径是在 `Inkwell.Core.csproj` 直接加包引用——这恰好违反 [AGENTS.md §3.2](../../AGENTS.md) "业务命名空间只能依赖 `Inkwell.Abstractions` + BCL"的依赖纯度原则，会被 CI `BannedSymbols.txt` 拦下
- **影响范围**：
  - `Inkwell.Core.KnowledgeBase` / `.Memory` 独立业务 HD 起草时会在"`IEmbeddingGenerator<,>` 的包依赖到底走哪条路径"上卡壳——没有任何现有 HD / ADR 声明"把 `Microsoft.Extensions.AI.Abstractions` 加进 `Inkwell.Abstractions.csproj` 白名单"这一步
  - H5 CodingExecutor 若照单全收"业务命名空间直接注入 `IEmbeddingGenerator<,>`"这条决策去写代码，会在业务 csproj 引入违规 `PackageReference`，CI `BannedSymbols.txt` / Roslyn analyzer 检查失败
  - 若后续需另发 ADR / AGENTS.md 修订才能落地该白名单例外（HD-008 §6 已自认"不在本 HD 权限范围"），则 KB / Memory 业务 HD 在该 ADR 落地前无法开工
- **建议方向**（不替设计师下结论，仅给方向）：
  - 选项 1：比照 VectorData 先例真正"对称化"——在 HD-008（或紧接的一次 errata）中把 `Microsoft.Extensions.AI.Abstractions` 也加进 `Inkwell.Abstractions.csproj` 依赖白名单 + `GlobalUsings.cs` 追加 `global using Microsoft.Extensions.AI;`，使 `IEmbeddingGenerator<,>` 通过依赖 `Inkwell.Abstractions` 间接可见，与 VectorData 完全同构，无需修改 AGENTS.md
  - 选项 2：维持"不扩展 Inkwell.Abstractions 白名单"的现状，改为发起独立 ADR 在 AGENTS.md §3.2 显式登记该例外（允许 `Inkwell.Core.KnowledgeBase` / `.Memory` 直接 `PackageReference Microsoft.Extensions.AI.Abstractions`），走 AGENTS.md 签字位流程
  - 选项 3：退回 Q5 候选 B——新增 `IEmbeddingProducer` 门面接口置于 `Inkwell.Abstractions`（彻底避免依赖纯度冲突，但增加维护成本，Owner 此前已明确否决）
  - reviewer 倾向选项 1（与 Owner 已拍板的"比照先例"意图最一致，且成本最低，只需让先例真正对称）
- **卡点等级**：**blocking**（建议 Owner picker 确认走哪个选项；HD-008 本体其余设计不受影响，可在 Owner 拍板后一次性追加 errata）
- **追溯**：C91
- **状态**：已处理（2026-07-06）——Owner picker 拍板选项 1，[HD-001 §13 2026-07-06 errata·第六轮](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#2026-07-06-errata第六轮b15-对称纳入-microsoftextensionsaiabstractions-白名单) 已把 `Microsoft.Extensions.AI.Abstractions` 对称纳入 `Inkwell.Abstractions.csproj` 依赖白名单 + `GlobalUsings.cs`（§2 / §14.3），[HD-008 §2 / §6 / §12 / §13 Q5](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md) 措辞同步精确化，未触碰 AGENTS.md。

#### Non-blocking

##### N25：HD-008 缺少显式"database-design.md 不贡献"声明（对比 HD-006 §12 / HD-007 §12）

- **问题**：HD-006 / HD-007 均有独立 §12"跨模块章节贡献"小节显式声明"database-design.md 不贡献"，HD-008 §12 直接是"追溯"，跳过了这一惯例小节
- **影响范围**：不影响任何下游产物起步（向量数据本就不落 EF Core 表，database-design.md 无需 HD-008 贡献字段是显然的），仅是与其余端口 HD 的章节命名习惯不一致，降低文档可预测性
- **建议方向**：补一句"本 HD 不贡献 database-design.md（向量数据落 Qdrant / InMemory，非关系表，[ADR-020](../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) 已锁定）"，可并入现有 §1.2 或单独起一个简短 §12 小节
- **卡点等级**：non-blocking
- **状态**：已处理（2026-07-06）——已并入 [HD-008 §1.2](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#12-范围) 末尾。

##### N26：HD-008 缺少独立"监控指标 / 告警策略"小节

- **问题**：HD-004 ~ HD-007 均有 §7.3"可观测性"子段列监控指标与告警建议，HD-008 全文无对应内容，也未显式声明"监控随 Qdrant / InMemory / AzureOpenAIEmbeddings 独立 HD 落地"
- **影响范围**：不影响 HD-008 本体翻 `reviewed`（本 HD 无运行期代码，监控指标本就该在具体 Provider 实现层定义），但下游三个独立 Provider HD 起草时若无明确指引，可能各自发明不一致的监控命名 / 告警阈值
- **建议方向**：补一句"具体监控指标（如 embedding 生成延迟、Qdrant 查询延迟、collection 大小）随 `Inkwell.Core/VectorStore/` 与 `providers/Inkwell.VectorStore.Qdrant/` 独立 HD 落地；OTel instrumentation 基线由 [`Microsoft.Extensions.VectorData` 内置 `ActivitySource`](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-vector-data) 提供（[ADR-020 §迁移路径 step 9](../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md)）"
- **卡点等级**：non-blocking
- **状态**：已处理（2026-07-06）——已补入 [HD-008 §9](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#9-部署--安全说明) 末尾。

##### N27：HD-008 §9 未显式声明"具体部署步骤移交 Provider 独立 HD"

- **问题**：HD-004 ~ HD-007 在"部署 / 安全"段落均有"具体部署步骤合理移交 Provider 独立 HD"式的显式 deferral 措辞，HD-008 §9 只覆盖凭据存储位置，未写这句话
- **影响范围**：不影响 HD-008 翻 `reviewed`（部署步骤本就该在 Qdrant / InMemory 具体实现 HD 中定义），仅是措辞不完整可能让读者误以为部署步骤遗漏而非有意移交
- **建议方向**：§9 结尾补一句"Qdrant collection 创建 / InMemory 无持久化等具体部署步骤，移交 `providers/Inkwell.VectorStore.Qdrant/` 与 `Inkwell.Core/VectorStore/` 独立 HD"
- **卡点等级**：non-blocking
- **状态**：已处理（2026-07-06）——已补入 [HD-008 §9](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#9-部署--安全说明) 末尾（与 N26 同批追加）。

##### N28：HD-008 无独立"性能边界"标题呈现

- **问题**：HD-002 ~ HD-007 均有独立的性能预算表（P50/P99），HD-008 仅在 §7 测试策略段落顺带一句"无运行期方法故无性能基准"作为解释，未单独起"性能边界"标题
- **影响范围**：不影响 HD-008 翻 `reviewed`（本 HD 确实无运行期方法，性能边界不适用有充分理由），仅是章节呈现形式与其余 HD 不一致，略微降低跨 HD 比对的一致体验
- **建议方向**：可选——若 Owner 认为形式一致性重要，可在 §10"已知限制"前插入一句显式"性能边界：不适用（本 HD 无运行期方法，参见 §1.2 范围声明）"
- **卡点等级**：non-blocking
- **状态**：已处理（2026-07-06）——已在 [HD-008 §10](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#10-已知限制) 前插入该句。

### 18.4 评审结论与下一步

- **整体评审决议**：**PASS-AS-ERRATA（有条件）**——HD-008 本体的 type-alias 复用 + Options + Builder DSL 签名声明设计完整自洽，与 ADR-020 / HD-001 / HD-006 的既有决策高度一致（C86 ~ C89、C92 ~ C94 共 7 项 PASS）；但发现 **2 项 blocking**（B14 / B15），均指向同一根问题：Q5"`IEmbeddingGenerator<,>` 直接注入"决策所"比照"的 VectorData 先例，实际缺少对称的物理落地步骤（未把 `Microsoft.Extensions.AI.Abstractions` 纳入 `Inkwell.Abstractions.csproj` 白名单 + `GlobalUsings.cs`），且 file-structure.md 的相关 callout 未同步 HD-008 §11 已拍板的事实
- **HD-008 翻 `reviewed` 前置条件**：
  1. ✅ Owner 通过 picker 对 B15 三个选项拍板（已选选项 1：把 `Microsoft.Extensions.AI.Abstractions` 对称纳入 `Inkwell.Abstractions.csproj` 白名单 + `GlobalUsings.cs`，与 VectorData 处理完全同构，不触碰 AGENTS.md）——已处理（2026-07-06）
  2. ✅ 按拍板结果在 `h3-detailed-design-author` 模式下落地 HD-008 errata（§2 文件结构 / §14.3 GlobalUsings / §6 / §13 Q5 措辞同步）+ file-structure.md callout 同步更新（B14 随 B15 一并解决）——已处理（2026-07-06）：[HD-001 §13 2026-07-06 errata·第六轮](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#2026-07-06-errata第六轮b15-对称纳入-microsoftextensionsaiabstractions-白名单) + [HD-008 §2 / §6 / §12 / §13 Q5](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md) + [file-structure.md `## Inkwell.Abstractions.VectorStore`](file-structure.md#inkwellabstractionsvectorstore)
  3. ⬜ Owner 在 HD-008 frontmatter 翻 `status: draft → reviewed` + 填 `reviewers: [Inkwell]`（**人工签字位**，AI 不替签）——前两项已完成，本项待人工签字
  4. ✅（可选，non-blocking）N25 / N26 / N27 / N28 四项文档呈现形式缺口已随本轮一并补齐——已处理（2026-07-06）
- **HD-008 自身核心设计结论**："不重新发明 `IVectorStore`"这一 ADR-020 核心决策的落地（type-alias 复用 + Builder DSL 签名）本身是**扎实且自洽的**（C86 ~ C89、C92 ~ C94 全部 PASS）；本轮发现的 2 个 blocking 项不否定这一核心设计，而是指向"决策落地是否配齐了必要的物理机制"这一更细粒度的问题，修复成本可控（1 次 Owner picker + 1 轮 errata）
- **后续路径建议**：Owner 就 B15 拍板后，建议顺序：(1) HD-008 errata 落地 → (2) HD-008 翻 `reviewed` → (3) 端口层 8 个 HD（HD-001 ~ HD-008）全部 reviewed，H3 端口层设计正式收口 → (4) 进入 `Inkwell.Core.KnowledgeBase` / `.Memory` / `Inkwell.Core.AgentRuntime` 等业务命名空间 HD 或 `providers/Inkwell.VectorStore.Qdrant` / `Inkwell.Persistence.EFCore.*` 等 Provider 独立 HD 起草

### 18.5 端口层（HD-001 ~ HD-008）整体完成度总结

- **HD-001 Foundation**：`reviewed`（[design-review-report.md §4](#4-评审结论) 首轮 + 多轮 errata 后确认，见 §6 ~ §13）
- **HD-002 Persistence Port**：`reviewed`（同上，含 §7 增量评审）
- **HD-003 File Storage Port**：`reviewed`（§7 增量评审 PASS-AS-ERRATA 后确认）
- **HD-004 Cache Port**：`reviewed`（§14 增量评审 PASS-AS-ERRATA 后确认）
- **HD-005 Queue Port**：`reviewed`（§15 增量评审 PASS-AS-ERRATA 后确认）
- **HD-006 Agent Runtime Port**：`reviewed`（§16 增量评审 PASS-AS-ERRATA，B13 file-structure.md 同步 + N20 CI 命令 errata 均已处理）
- **HD-007 Audit Logger Port**：`reviewed`（§17 增量评审 PASS-AS-ERRATA，N21/N22 均已处理，0 blocking）
- **HD-008 Vector Store Type-Alias**：`draft`（本轮 §18，**PASS-AS-ERRATA（有条件）**，2 项 blocking 待 Owner picker + 1 轮 errata 后方可翻 `reviewed`）
- **端口层整体**：8/8 HD 已起草完成，7/8 已 `reviewed`，HD-008 是唯一尚未 `reviewed` 的一张（且已进入"待 Owner 拍板 1 个开放机制问题"的收尾阶段，非需要重新起草）。待 HD-008 走完 B15 拍板 + errata 闭环，端口层（`Inkwell.Abstractions`）8 张 HD 将全部 `reviewed`，H3 可正式推进到业务命名空间（`Inkwell.Core.*`）与 Provider 独立 HD 起草阶段

### 18.6 自检

- ✅ 每条 `pass` / `partial` / `n/a` / `FAIL` 都附了具体章节锚点引用
- ✅ 2 个 `blocking` 反问（B14/B15）均能映射到具体一致性冲突（C90/C91）+ 影响范围 + 可执行的选项化建议方向
- ✅ 4 个 `non-blocking` 反问（N25~N28）均为文档呈现形式缺口，不影响 HD-008 核心设计正确性
- ✅ 未使用"看起来" / "似乎" / "感觉"等主观词汇
- ✅ 未凭文件名臆测，每条结论都打开了对应文件读到对应字段（含 HD-001 §14.3 / §2 / §3.11 / §3.11.1 实际内容核对、HD-006 凭据字段核对、AGENTS.md §3.1/§3.2 原文核对）
- ✅ 未尝试用部分数据写"半个报告"——前置闸门已确认通过
- ✅ 未越界修改 HD-008 / HD-001 / file-structure.md / 报告主体
- ✅ 未给越界建议（如"建议你顺便重构 X"）
- ✅ 报告路径仍走 H3 规范默认 [docs/04-detailed-design/design-review-report.md](design-review-report.md)（追加 §18 而非新建文件）
- ✅ 全程使用 bullet list 呈现（避免中英文混排表格触发 MD060，按 user-memory 已知陷阱处理）

## 19. HD-010 Inkwell.Persistence.EFCore.InMemory Final Adapter 首轮评审（2026-07-06）

> 本轮在已 reviewed 的报告主体之上**追加**，仅评审增量产物：[HD-010 Inkwell.Persistence.EFCore.InMemory](Inkwell.Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md)（status: draft，2026-07-06 起草）+ [file-structure.md `## providers/Inkwell.Persistence.EFCore.InMemory` 章节追加](file-structure.md#providersinkwellpersistenceefcoreinmemory)。报告主体 §1 ~ §18 的 `status / reviewers` 字段**不**因本节调整。全程以 bullet list 呈现（按 user-memory `markdown-lint.md` 已知陷阱，避免中英文混排表格触发 MD060）。

### 19.0 评审范围与基线

- **本轮评审对象**：HD-010 全文（§1 ~ §13）+ file-structure.md `## providers/Inkwell.Persistence.EFCore.InMemory` 章节追加
- **不在本轮范围**：HD-001 ~ HD-009 主体设计（已在前序评审中处理，本轮仅在发现跨引用缺陷时反查）；HD-011 / HD-012（SqlServer / Postgres final adapter，尚未起草）；HD-013（跨 Provider 契约测试包，尚未起草）
- **前置闸门**：
  - [requirements.md](../01-requirements/requirements.md) `status: reviewed` ✅
  - [repo-impact-map.md](../01-requirements/repo-impact-map.md) `status: reviewed` ✅
  - HD-010 frontmatter 完整，upstream 12 项均可定位：REQ-002 / REQ-006 / REQ-009 / REQ-013 / REQ-014（[requirements.md](../01-requirements/requirements.md)）+ ADR-004 / ADR-013 / ADR-017 / ADR-021 / ADR-023（[adr/](../03-architecture/adr/)）+ HD-001 / HD-002 / HD-009 全部真实存在且 `status: reviewed`
  - **不触发** [io-contracts.md §5 阻塞返回](../../.he/agents/_shared/io-contracts.md)——HD-010 是合理 per-module slice 切片，目录未"严重偏离" h3-detailed-design.md

### 19.1 完备性扫描（HD-010 范围内）

按 [h3-detailed-design.md 章节清单](../../.he/docs/stages/h3-detailed-design.md) 逐项打分：

- **文件结构**：`pass` — §2 文件清单（1 csproj + 3 `*.cs`）与 §3.0 ~ §3.3 十字段表一一对应，且与 [file-structure.md `## providers/Inkwell.Persistence.EFCore.InMemory`](file-structure.md#providersinkwellpersistenceefcoreinmemory) 文件树逐行核对一致（计数"3 个 `*.cs` + 1 个 `.csproj`"手工核算无误）。证据：[HD-010 §2](Inkwell.Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md#2-文件清单) + [file-structure.md 对应章节](file-structure.md#providersinkwellpersistenceefcoreinmemory)
- **数据库 / 表 / 字段 / 索引 / 约束**：`n/a`（显式声明）— §13 明确"本 HD **不**追加 database-design.md（InMemory 不引入新表结构，schema 沿用 HD-009 已锁定的 Entity 定义）"；核对属实，HD-010 全文无 `Entities/` / `Configurations/` 新文件。证据：[HD-010 §13](Inkwell.Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md#13-同步追加跨模块文件)
- **接口 / 错误码**：`partial` — 端口签名、BCL 异常透传（§3.2 `不额外 catch，透传`）均与 [ADR-023](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) 最终态一致，无 `Result<T>` / 错误码残留；**但** §3.1 `UseInMemoryDatabase()` 完整代码中 `InMemoryRowVersionInterceptor` 的 DI 注册方式与其消费方式（`AddInterceptors(sp.GetServices<ISaveChangesInterceptor>())`）不匹配（详 §19.2 C96，属实质性接口契约缺陷，非文档呈现问题）。证据：[HD-010 §3.1 完整代码](Inkwell.Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md#31-dependencyinjectioninkwellpersistenceefcoreinmemoryservicecollectionextensionscs)
- **服务 / 进程 / 后台任务**：`n/a` — HD-010 是 library，不独立进程；§9 显式声明"无独立部署单元"。证据：[HD-010 §9](Inkwell.Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md#9-部署--配置)
- **每个目录 / 程序文件职责**：`pass` — 3 个 `*.cs` × 10 字段全填，无 `<TBD>` / `<待定>`；csproj 依赖白名单 + 禁用清单均列明。证据：[HD-010 §3.0 ~ §3.3](Inkwell.Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md#3-各文件-10-字段)
- **配置文件字段 / 默认值**：`pass` — §7 显式声明"不引入新 `PersistenceOptions` 字段，仅新增一个方法参数 `databaseName`（默认 `"inkwell"`）"，隔离策略（`InMemoryDatabaseRoot` 逐次新建）有官方文档引用支撑。证据：[HD-010 §7](Inkwell.Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md#7-配置项--inmemory-数据库命名与隔离策略)
- **日志格式 / 字段**：`pass` — §3.2 / §3.3 均显式声明"N/A"并给出理由（高频路径噪音 / 与 HD-009 已有日志语义重叠），符合既有 HD 惯例。证据：[HD-010 §3.2 / §3.3 日志要求行](Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md)
- **监控指标 / 告警策略**：`n/a`（合理）— InMemory 场景无生产监控意义（进程内数据库，随进程存亡），HD-009 已覆盖 OTel span 基线；HD-010 未新增独立可观测性内容属合理移交，但**未像 HD-004 ~ HD-007 那样显式写一句 deferral 措辞**（轻微文档呈现缺口，详 §19.3 N29）
- **部署步骤 / 回滚 / 备份恢复**：`pass` — §9 显式声明"不需要为 InMemory Provider 单独起容器"+ `appsettings.Development.json` Provider 选择字段。证据：[HD-010 §9](Inkwell.Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md#9-部署--配置)
- **性能边界 / 安全边界 / 已知限制**：`partial` — §4 详细讨论了 RowVersion 值生成机制的边界，§10 决策记录给出证据链；**但未讨论 `.IsRowVersion()`（= `IsConcurrencyToken()` + `ValueGeneratedOnAddOrUpdate()`）这一"值生成"语义标记与拦截器手动赋值 `CurrentValue` 之间的交互边界**——这正是本轮 §19.2 C99 的核心发现，§4 只回答了"谁来生成新值"，未回答"被标记为 `ValueGeneratedOnAddOrUpdate` 的属性，手动设置是否会被 EF Core 管线接受/覆盖/忽略"这一更底层的边界问题。证据：[HD-010 §4](Inkwell.Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md#4-rowversion-模拟策略详解回应-n5c7)

**完备性结论**：9 项中 4 项 `pass`、3 项 `n/a`（2 项合理、1 项合理但缺显式措辞）、2 项 `partial`（接口契约缺陷 + 性能/安全边界讨论不完整）。**完备性维度的 2 个 `partial` 均指向同一类问题——设计文档描述了"应该发生什么"，但未验证/讨论"实际会不会发生"**，详见下方 §19.2 一致性扫描与 §19.3 反问清单。

### 19.2 一致性扫描（HD-010 ↔ HD-009 / HD-002 / ADR-021 / ADR-023 + file-structure.md）

- **C95（PASS）**— HD-010 §3.0 csproj 依赖白名单（`Microsoft.EntityFrameworkCore.InMemory` + ProjectReference `Inkwell.Persistence.EFCore` + `Inkwell.Abstractions`）与禁用清单（SqlServer / Postgres / `.Design` / `Inkwell.Core`）与 [ADR-021 §依赖规则补充](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) EFCore family 例外 + [ADR-017 §3.2](../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) 完全一致；§10 C1 ~ C4 自动化检查脚本与该白名单逐条对应。证据：[HD-010 §3.0 / §10](Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md) vs ADR-021 / ADR-017 §3.2
- **C96（FAIL，blocking）**— HD-010 §3.1 完整代码中，`InMemoryRowVersionInterceptor` 的注册行是 `builder.Services.AddSingleton<InMemoryRowVersionInterceptor>();`（服务类型 = 具体类 `InMemoryRowVersionInterceptor` 本身），但紧随其后的 `AddDbContext` 配置调用的是 `.AddInterceptors(sp.GetServices<Microsoft.EntityFrameworkCore.Diagnostics.ISaveChangesInterceptor>())`（按服务类型 `ISaveChangesInterceptor` 查询）。[`Microsoft.Extensions.DependencyInjection` 的 `AddSingleton<TService>()` 单类型参数重载](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.servicecollectionservicetextensions.addsingleton) 会把 `ServiceType` 和 `ImplementationType` 都注册为 `TService` 本身——`sp.GetServices<ISaveChangesInterceptor>()` 只返回"注册时显式声明服务类型为 `ISaveChangesInterceptor`"的实例，**不会**因为某个具体类恰好实现了该接口就自动纳入。这意味着 `InMemoryRowVersionInterceptor` 实际上**永远不会被 `AddInterceptors` 拾取、永远不会执行**——本 HD 的核心交付物（回应 [design-review-report N5/C7](#n5inmemory-provider-rowversion-自动管理可行性c7) 的 RowVersion 手动模拟机制）会静默失效：`RowVersion` 永远不会递增，§3.3 承诺的"并发冲突场景（核心用例）"单测会直接失败（因为两次独立 `SaveChanges` 都不会更新 `RowVersion`，EF Core 的乐观并发检测比较的是同一个从未变化的 `OriginalValue` vs 当前存储值，不会触发 `DbUpdateConcurrencyException`）。正确写法应为 `builder.Services.AddSingleton<Microsoft.EntityFrameworkCore.Diagnostics.ISaveChangesInterceptor, InMemoryRowVersionInterceptor>();`。证据：[HD-010 §3.1 完整代码](Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md#31-dependencyinjectioninkwellpersistenceefcoreinmemoryservicecollectionextensionscs) 第 2 行 vs 第 4 ~ 6 行 `AddInterceptors` 调用
- **C97（FAIL，blocking，跨 HD）**— HD-010 §3.1 注解中明确写"`AddInterceptors(sp.GetServices<ISaveChangesInterceptor>())` 要求 HD-009 §3.11 `AddEfCorePersistenceBase()` 把 `AuditingSaveChangesInterceptor` 注册为 `ISaveChangesInterceptor` 服务类型"。**已实际逐字核对 [HD-009 §3.11](Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md#311-dependencyinjectioninkwellpersistenceefcoreservicecollectionextensionscs) 全文**：该节只有"职责：注册 base 服务（`EfCorePersistenceProvider` / `AuditingSaveChangesInterceptor` / `InkwellSeeder` / `MigrationRunner` / 全部 `Repositories/<TypeName>Repository`）"一句职责描述 + 对外接口签名 `internal static IServiceCollection AddEfCorePersistenceBase(this IServiceCollection services)`，**全文档没有任何一段"完整代码"块展示该方法体内部的具体注册语句**（对照 §3.9 / §3.10 均有"完整代码"标题段，§3.11 没有）。**结论：无法从 HD-009 现有文本确认"能"或"不能"——这是一处真实的文档空白，不是本 reviewer 的主观推测**。鉴于 C96 已证实 HD-010 自己对 `InMemoryRowVersionInterceptor` 的注册方式选择了错误的服务类型，存在**同样的错误被 HD-009 对 `AuditingSaveChangesInterceptor` 重复一遍**的现实风险——若如此，则不仅 InMemory Provider，**SqlServer / Postgres 三个 Provider 上的时间戳自动填充（`CreatedTime`/`UpdatedTime`）与 `IHasOwner` 校验也会一并静默失效**，这是比 C96 影响面更大的问题（C96 只影响 InMemory 一个 Provider 的 RowVersion，C97 若坐实则影响全部三个 Provider 的审计字段）。证据：[HD-010 §3.1 注](Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md#31-dependencyinjectioninkwellpersistenceefcoreinmemoryservicecollectionextensionscs) vs [HD-009 §3.11 全文](Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md#311-dependencyinjectioninkwellpersistenceefcoreservicecollectionextensionscs)（无完整代码块，逐字确认）
- **C98（PASS）**— Migration/EnsureCreated 策略与 [ADR-021 D3](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 完全一致：HD-010 §3.2 `InMemoryDbContextInitializer.InitializeAsync` 直接委托 `db.Database.EnsureCreatedAsync(ct)`，是**真实建库**而非 no-op；§10 C3 自动化检查断言"无 `Migrations/` 子目录"；与 ADR-021 §5"InMemory 不支持 Migration"锁定条款完全对齐，无偏离。证据：[HD-010 §3.2 / §10 C3](Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md) vs [ADR-021 D3](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)
- **C99（PASS）**— HD-010 §5 "为什么不创建 `InMemoryInkwellDbContext` 子类"的决策引用 [HD-009 §6 step 3](Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md#6-builder-dsl-衔接adr-021-builder-dsl-形状) 具体契约（"final adapter 扩展方法内部调 `services.AddDbContext<InkwellDbContext>(...)`"）——经核对 HD-009 §6 原文，该句确实逐字存在；HD-010 同时坦诚说明这与 [ADR-021 §决策 早期示意文件树](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)（曾列出 `InMemoryInkwellDbContext.cs`）不同，并给出"HD-009 §6 更具体、更晚锁定，故遵循 §6"的优先级判断依据，逻辑自洽、无凭空拍板。证据：[HD-010 §5](Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md#5-为什么本-hd-不创建-inmemoryinkwelldbcontext-子类) vs [HD-009 §6](Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md#6-builder-dsl-衔接adr-021-builder-dsl-形状) vs ADR-021 §决策
- **C100（PARTIAL，non-blocking）**— HD-010 §4 引用 [EF Core 官方「Application-managed concurrency tokens」指南](https://learn.microsoft.com/ef/core/saving/concurrency#application-managed-concurrency-tokens) 作为 `SaveChangesInterceptor` 内手动递增 `RowVersion` 的依据，这一引用本身准确；**但该官方指南的典型示例场景是属性仅标记 `.IsConcurrencyToken()`（应用管理并发令牌），而 HD-009 §3.1 `ApplyRowVersion` 对 `IHasRowVersion` 属性统一调用 `.IsRowVersion()`——后者等价于 `.IsConcurrencyToken().ValueGeneratedOnAddOrUpdate()`，`ValueGeneratedOnAddOrUpdate()` 这层"数据库/存储侧生成新值"的语义标记通常意味着 EF Core 期望值由 Provider 自身在保存时生成（如 SqlServer `rowversion` 列的写后回读），而不是由应用代码在 `SavingChangesAsync` 阶段直接覆盖 `CurrentValue`**。HD-010 §4 未讨论"被标记 `ValueGeneratedOnAddOrUpdate` 的属性，在 InMemory Provider 上，拦截器手动设置的 `CurrentValue` 是否会被 EF Core 的值生成管线接受、忽略、或产生诊断警告"这一更底层的边界问题——本轮无法通过阅读设计文档确认这一点在实际 EF Core 10 InMemory Provider 上的行为，因为这是需要运行代码验证的库行为细节，超出"文档一致性核查"范畴。**不判定为 blocking 的理由**：HD-010 §3.3 已经承诺了恰好覆盖这一场景的单测（"并发冲突场景（核心用例）"——两个独立 `DbContext` 实例 + 显式断言 `DbUpdateConcurrencyException`），若该单测在 H5 实现阶段失败，将直接、明确地暴露这一边界问题，不会被静默放过；因此本条降级为 non-blocking，但建议在 H5 编码任务简报中显式标注"若 §3.3 并发冲突单测失败，需回炉重新评估 `.IsRowVersion()` 是否适合 InMemory Provider（备选：InMemory 侧改用 `.IsConcurrencyToken()`，不叠加 `ValueGeneratedOnAddOrUpdate()`，但这会造成三 Provider 间 `OnModelCreating` 配置的分叉，需要 HD-009 `InkwellDbContext` 引入 Provider-specific 判断，与 HD-010 §5"不创建子类"的决策产生新张力）"。证据：[HD-010 §4](Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md#4-rowversion-模拟策略详解回应-n5c7) vs [HD-009 §3.1 `ApplyRowVersion`](Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md#31-inkwelldbcontextcs) vs [EF Core 官方并发文档](https://learn.microsoft.com/ef/core/saving/concurrency)
- **C101（PASS）**— [file-structure.md `## providers/Inkwell.Persistence.EFCore.InMemory`](file-structure.md#providersinkwellpersistenceefcoreinmemory) 文件树、依赖注释、计数估算（"3 个 `*.cs` + 1 个 `.csproj`"）与 HD-010 §2 / §3.0 逐一核对一致；"不创建 `InMemoryInkwellDbContext` 子类 / 不创建独立 `BannedSymbols.txt`"的理由引用锚点（HD-010 §5 / §10）真实可跳转。证据：[file-structure.md 对应章节](file-structure.md#providersinkwellpersistenceefcoreinmemory) vs [HD-010 §2 / §5 / §10](Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md)

**一致性结论**：7 项检查中 2 项 `FAIL`（C96 / C97，均 blocking）、1 项 `PARTIAL`（C100，non-blocking）、4 项 `PASS`（C95、C98、C99、C101）。**2 个 FAIL 项共享同一根问题**——`ISaveChangesInterceptor` 服务类型注册在 DI 层面被误用，且这一错误在 HD-010 自身代码中已确证存在（C96），在 HD-009 中因缺少完整代码而无法排除同样存在（C97）。

### 19.3 反问清单

#### Blocking

##### B16：`InMemoryRowVersionInterceptor` 以错误的 DI 服务类型注册，导致 RowVersion 拦截器永不执行（C96）

- **问题**：HD-010 §3.1 完整代码中 `builder.Services.AddSingleton<InMemoryRowVersionInterceptor>();` 把服务类型注册为具体类 `InMemoryRowVersionInterceptor` 本身，而消费端 `.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>())` 是按接口服务类型 `ISaveChangesInterceptor` 查询——.NET DI 容器不会因为某具体类实现了某接口就自动把它纳入该接口的 `GetServices<T>()` 结果集，必须在注册时显式声明服务类型为该接口。当前写法下 `InMemoryRowVersionInterceptor` 永远不会被 `AddInterceptors` 拾取
- **影响范围**：本 HD 唯一的核心交付物——回应 [design-review-report N5/C7](#n5inmemory-provider-rowversion-自动管理可行性c7) 的 RowVersion 手动模拟机制——会静默失效；§3.3 承诺的"并发冲突场景（核心用例）"单测在 H5 编码阶段会直接失败（`RowVersion` 永不递增，`DbUpdateConcurrencyException` 永不被抛出）；REQ-002 / REQ-006（依赖乐观并发的场景）在 InMemory dev/test 环境下得不到真实覆盖
- **建议方向**：将该行改为 `builder.Services.AddSingleton<Microsoft.EntityFrameworkCore.Diagnostics.ISaveChangesInterceptor, InMemoryRowVersionInterceptor>();`——这是纯机械修正，不涉及任何新的技术决策，不需要 Owner picker
- **卡点等级**：**blocking**
- **追溯**：C96
- **已处理（2026-07-06）**：[HD-010 §3.1 完整代码](Inkwell.Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md#31-dependencyinjectioninkwellpersistenceefcoreinmemoryservicecollectionextensionscs) 注册行已按建议方向修正为 `AddSingleton<ISaveChangesInterceptor, InMemoryRowVersionInterceptor>()`；§3.1 注解与 §12 待办同步更新，去除"假设待验证"措辞

##### B17：HD-009 `AddEfCorePersistenceBase()` 是否把 `AuditingSaveChangesInterceptor` 注册为 `ISaveChangesInterceptor` 服务类型——现有文本无法确认，且 HD-010 已证实同类错误确实会发生（C97）

- **问题**：已逐字核对 [HD-009 §3.11](Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md#311-dependencyinjectioninkwellpersistenceefcoreservicecollectionextensionscs) 全文——该节仅有职责描述与方法签名，**没有任何"完整代码"块展示 `AddEfCorePersistenceBase()` 方法体内部具体如何注册 `AuditingSaveChangesInterceptor`**。明确结论：**当前 HD-009 文本既不能确认"能"也不能确认"不能"，需要 HD-009 补充说明（不是本 reviewer 代为拍板）**。鉴于 B16 已证实 HD-010 对结构完全相同的 `InMemoryRowVersionInterceptor` 确实犯了"注册为具体类而非接口服务类型"的错误，不能排除 HD-009 对 `AuditingSaveChangesInterceptor` 重复同样的错误
- **影响范围**：若 HD-009 确实只把 `AuditingSaveChangesInterceptor` 注册为具体类，则**三个 Provider（InMemory / SqlServer / Postgres）上的 `CreatedTime` / `UpdatedTime` 自动填充与 `IHasOwner.OwnerUserId` 校验会全部静默失效**——这是比 B16 更大范围的问题（B16 仅影响 InMemory 一个 Provider 的 RowVersion 机制）；下游全部 ~30 个业务 Repository 的审计字段行为、[HD-009 §8.2 `AuditingSaveChangesInterceptorTests.cs`](Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) 承诺的单测会连带失败
- **建议方向**（不替设计师下结论，仅给方向）：
  - HD-009 §3.11 补一段"完整代码"展示 `AddEfCorePersistenceBase()` 方法体，明确写出 `services.AddSingleton<Microsoft.EntityFrameworkCore.Diagnostics.ISaveChangesInterceptor, AuditingSaveChangesInterceptor>();` 这一行（若实际决策确实如此）
  - 若实际实现另有机制（例如不通过 DI 的 `GetServices<ISaveChangesInterceptor>()` 汇总模式，而是 `InkwellDbContext` 自身在 `OnConfiguring` 或构造函数中直接持有并附加拦截器实例），HD-009 需要显式声明该替代机制，HD-010 §3.1 的 `AddInterceptors(sp.GetServices<ISaveChangesInterceptor>())` 消费方式需要同步调整以匹配
- **卡点等级**：**blocking**（HD-009 虽已 `reviewed`，但本条是可执行验证发现的具体代码级缺陷，非重新评估已定决策；建议走小 errata 而非重新评审全文）
- **追溯**：C97
- **已处理（2026-07-06）**：核实结论——**HD-009 §3.11 此前无完整代码块，不构成已确认的书面 bug，而是文档空白**；已通过 [HD-009 §13.6 errata·第六轮](Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md#136-2026-07-06-errata第六轮hd-010-首轮评审-design-review-reportmd-19-b17c97-补齐-addefcorepersistencebase-完整代码) 补齐该方法完整代码，确认 `AuditingSaveChangesInterceptor` 按 `AddSingleton<ISaveChangesInterceptor, AuditingSaveChangesInterceptor>()`（接口服务类型）注册，与消费端一致，不受 B16 同类问题影响；`EfCorePersistenceProvider`（`AddScoped<IPersistenceProvider, ...>`）/ `InkwellSeeder` / `MigrationRunner`（具体类型注册，消费端亦按具体类型注入，无接口不匹配风险）/ 具名 Repository 一并补全。HD-009 `status: reviewed` 未回退，本次是补齐既有承诺的实现细节，非重新评估已定决策

#### Non-blocking

##### N29：HD-010 缺少显式"监控随 HD-009 已覆盖，不新增独立可观测性内容"措辞

- **问题**：HD-004 ~ HD-008 均有一句显式 deferral 措辞说明监控指标的归属，HD-010 全文未出现"监控" / "告警"关键字，也未显式声明"HD-009 §3.2 / §4.2 已覆盖的 OTel span 基线对 InMemory Provider 同样适用，本 HD 不新增独立可观测性内容"
- **影响范围**：不影响 HD-010 翻 `reviewed`（InMemory 场景本就无生产监控意义），仅是措辞缺失可能让读者误判为遗漏
- **建议方向**：可在 §9 末尾补一句"可观测性沿用 HD-009 §3.2 EfCorePersistenceProvider OTel span 基线，本 HD 不新增独立监控内容"
- **卡点等级**：non-blocking

##### N30：`.IsRowVersion()` 的 `ValueGeneratedOnAddOrUpdate()` 语义与拦截器手动赋值的交互边界未讨论（C100）

- **问题**：HD-010 §4 论证了"为什么需要手动生成新值"，但未讨论"被 `.IsRowVersion()` 标记为 `ValueGeneratedOnAddOrUpdate` 的属性，在 InMemory Provider 上是否真的允许拦截器手动覆盖 `CurrentValue` 并被持久化管线接受"这一更底层的 EF Core 行为边界
- **影响范围**：不影响 HD-010 翻 `reviewed`（这是需要运行代码验证的库行为细节，超出文档一致性核查范畴）；但若 H5 实现阶段 §3.3 承诺的并发冲突单测失败，需要重新评估 InMemory 是否应改用 `.IsConcurrencyToken()`（不叠加 `ValueGeneratedOnAddOrUpdate()`），而这会与 HD-010 §5"不为 InMemory 创建 DbContext 子类"的决策产生新张力（因为 `OnModelCreating` 目前是三 Provider 共享的，若 InMemory 需要不同的属性标记方式，需要 Provider-specific 判断）
- **建议方向**：在 H5 `HD-010` 对应编码任务简报中显式标注"§3.3 并发冲突单测是本机制的验收门禁，若失败需回炉评估 `.IsRowVersion()` vs `.IsConcurrencyToken()` 分歧"，把这一风险从"设计假设"转为"编码阶段的显式验收标准"，不需要现在就拍板
- **卡点等级**：non-blocking

### 19.4 评审结论与下一步

- **整体评审决议**：**REJECT（需修复后重新提交，非结构性问题）**——HD-010 的整体设计思路（不建子类、复用 base DbContext、InMemory 专属 `EnsureCreatedAsync` + `RowVersion` 拦截器）扎实自洽，csproj 依赖规则、Migration 策略、file-structure.md 同步均 100% 通过（C95、C98、C99、C101 全 PASS）；但发现 **2 项 blocking**（B16 / B17），且 B16 是**可复现、可确认的代码级 bug**（不是"设计不够清楚"，而是"设计给出的示例代码本身不会按预期工作"）——本 HD 存在的目的就是解决 RowVersion 手动模拟这一具体问题，而当前代码样本恰好让这个机制失效，这是评审报告口径下的"核心交付物未达成"，故判定为 REJECT 而非 PASS-AS-ERRATA
- **判定 REJECT 而非 PASS-AS-ERRATA 的理由**：B16 / B17 均可通过纯机械修正解决（改一行 DI 注册代码 + 补一段 HD-009 完整代码），不需要 Owner 拍板任何新的技术方向；但由于两处缺陷都直接命中"本 HD 存在的核心目的能否达成"这一验收标准（而非外围文档措辞问题），按 HD-008 §18.4 先例的"文档呈现缺口 PASS-AS-ERRATA" vs 本轮"机制性代码缺陷 REJECT"两类问题应区别对待——避免把"HD-010 已通过评审"的信号过早释放给 H5 CodingExecutor
- **HD-010 修复路径**：
  1. ✅ **已处理（2026-07-06）** 修复 B16：[HD-010 §3.1 完整代码](Inkwell.Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md#31-dependencyinjectioninkwellpersistenceefcoreinmemoryservicecollectionextensionscs) `InMemoryRowVersionInterceptor` 注册行已改为 `AddSingleton<ISaveChangesInterceptor, InMemoryRowVersionInterceptor>()`（纯机械修正，未涉及 Owner picker）
  2. ✅ **已处理（2026-07-06）** 修复 B17：核实结论——**HD-009 §3.11 此前无"完整代码"块，属文档空白，不构成已确认的书面 bug**；已通过 [HD-009 §13.6 errata·第六轮](Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md#136-2026-07-06-errata第六轮hd-010-首轮评审-design-review-reportmd-19-b17c97-补齐-addefcorepersistencebase-完整代码) 补齐 `AddEfCorePersistenceBase()` 完整代码，确认 `AuditingSaveChangesInterceptor` 按 `AddSingleton<ISaveChangesInterceptor, AuditingSaveChangesInterceptor>()`（接口服务类型）注册，与消费端一致，不受 B16 同类风险影响；HD-009 `status: reviewed` 未回退（小 errata 流程，非重新评审全文）
  3. ⬜ 修复后重新核对 §3.3 单测断言仍然成立（RowVersion 递增、并发冲突抛 `DbUpdateConcurrencyException`）——留待 H5 编码阶段验证，非本轮文档修复范围
  4. ⬜ 可选处理 N29 / N30（non-blocking，不阻塞下一轮评审通过）
  5. ⬜ **B16 / B17 已修复，满足重新提交条件**——建议再走一轮增量评审（确认 C96 / C97 转 PASS）后，Owner 再在 HD-010 frontmatter 翻 `status: draft → reviewed` + 填 `reviewers: [Inkwell]`（**人工签字位，AI 不代签**）
- **对 file-structure.md 的结论**：本轮追加章节（`## providers/Inkwell.Persistence.EFCore.InMemory`）本身准确（C101 PASS），**不需要**因 B16/B17 而改动——问题在 HD-010/HD-009 的代码样本，不在文件树描述

### 19.5 自检

- ✅ 每条 `pass` / `partial` / `n/a` / `FAIL` 都附了具体章节锚点引用
- ✅ 2 个 `blocking` 反问（B16/B17）均能映射到具体一致性冲突（C96/C97）+ 影响范围 + 可执行的建议方向（且明确标注"纯机械修正，不需要 picker"）
- ✅ 2 个 `non-blocking` 反问（N29/N30）不影响 HD-010 核心设计方向，且 N30 给出了"若单测失败如何处置"的前瞻性验收标准
- ✅ 未使用"看起来" / "似乎" / "感觉"等主观词汇——C96/C97 的结论均基于 .NET DI 容器服务类型解析的确定性规则，而非猜测
- ✅ 未凭文件名臆测：已实际打开 HD-009 §3.11 全文核对"无完整代码块"这一事实，而非假设其存在
- ✅ 用户请求的"重点核查"两项（RowVersion 模拟策略、HD-009 assumption 核实）均给出了明确结论（分别是 C96/B16"确证是 bug" 和 C97/B17"确证 HD-009 现有文本无法确认，需补充说明"），未回避或只是重复记录问题
- ✅ 未尝试用部分数据写"半个报告"——前置闸门已确认通过
- ✅ 未越界修改 HD-010 / HD-009 / file-structure.md / 报告主体，仅追加评审报告
- ✅ 未给越界建议（如"建议你顺便重构 X"）
- ✅ 报告路径仍走 H3 规范默认 [docs/04-detailed-design/design-review-report.md](design-review-report.md)（追加 §19 而非新建文件）
- ✅ 全程使用 bullet list 呈现（避免中英文混排表格触发 MD060）

### 19.6 复审（2026-07-06，聚焦 B16/B17 修复核实）

> 本次不重跑 §19.1 ~ §19.3 全部检查项，仅针对 author 已提交的 B16/B17 修复做聚焦复审，核实范围与用户请求四项一一对应。

#### 19.6.1 HD-010 §3.1 DI 注册代码复核（对应请求项 1）

- **检查内容**：逐行核对 [HD-010 §3.1 完整代码](Inkwell.Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md#31-dependencyinjectioninkwellpersistenceefcoreinmemoryservicecollectionextensionscs)
- **发现**：注册行现为 `builder.Services.AddSingleton<Microsoft.EntityFrameworkCore.Diagnostics.ISaveChangesInterceptor, InMemoryRowVersionInterceptor>();`；消费行为 `options.AddInterceptors(sp.GetServices<Microsoft.EntityFrameworkCore.Diagnostics.ISaveChangesInterceptor>())`——服务类型（`ISaveChangesInterceptor`）与查询类型完全一致，`GetServices<T>()` 会返回该拦截器实例
- **结论**：`PASS` — 注册与消费类型匹配，B16 根因已消除，`InMemoryRowVersionInterceptor` 会被正常执行

#### 19.6.2 HD-010 全文同类 DI 注册扫描（对应请求项 2）

- **检查方法**：逐节核对 §2 文件清单列出的全部 4 个文件（csproj / DI 扩展 / `InMemoryDbContextInitializer` / `InMemoryRowVersionInterceptor`），确认是否存在其他"注册一个服务 + 用 `GetServices<TInterface>()` 消费"的组合
- **发现**：
  - §3.1 内另有一处注册 `builder.Services.AddSingleton<IDbContextInitializer, InMemoryDbContextInitializer>()`——消费方是 [HD-009 §3.5 `MigrationRunner`](Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) 构造函数参数 `IDbContextInitializer initializer`（单例构造函数注入，非 `GetServices<T>()` 多实例聚合），服务类型与构造函数参数类型均为 `IDbContextInitializer`，两者一致；此模式与 B16 不同类（构造函数注入下服务类型与消费类型不一致会直接导致解析失败 / 编译期可发现，不存在"静默失效"风险）
  - §3.2 / §3.3 本身不含 DI 注册代码（仅定义类型，由 §3.1 统一注册）
  - 未发现 HD-010 全文还有第二处 `AddXxx<TImpl>()`（服务类型=具体类）与 `GetServices<TInterface>()`（按接口查询）不匹配的组合
- **结论**：`PASS` — HD-010 全文范围内无遗漏的同类 DI 注册类型不匹配问题

#### 19.6.3 HD-009 §3.11 补全代码复核（对应请求项 3）

- **检查内容**：核对 [HD-009 §3.11 完整代码](Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md#311-dependencyinjectioninkwellpersistenceefcoreservicecollectionextensionscs) + [§13.6 errata 说明](Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md#136-2026-07-06-errata第六轮hd-010-首轮评审-design-review-reportmd-19-b17c97-补齐-addefcorepersistencebase-完整代码)
- **`AuditingSaveChangesInterceptor` 注册**：`services.AddSingleton<ISaveChangesInterceptor, AuditingSaveChangesInterceptor>();`——与 HD-010 §3.1 消费行 `sp.GetServices<ISaveChangesInterceptor>()` 类型一致，`PASS`
- **自洽性核查（Singleton 捕获依赖风险）**：`AuditingSaveChangesInterceptor(TimeProvider clock)`（[HD-009 §3.3](Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md#33-interceptorsauditingsavechangesinterceptorcs)）唯一依赖 `TimeProvider`——BCL 类型，`TimeProvider.System` 语义上是进程级单例，以 `AddSingleton` 注册不构成"Singleton 捕获 Scoped 依赖"的经典 DI 生命周期缺陷；未发现自洽性问题
- **其余注册逐一核对**：`EfCorePersistenceProvider` → `AddScoped<IPersistenceProvider, EfCorePersistenceProvider>()`（依赖 Scoped 的 `InkwellDbContext`，生命周期匹配）；`InkwellSeeder` / `MigrationRunner` → 均 `AddScoped<TConcrete>()` 按具体类型注册，消费端（[HD-009 §3.5](Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) `MigrationRunner` 构造函数）按同一具体类型 / 接口注入，无 `GetServices<T>()` 聚合场景，不受 B16 同类风险影响；具名 `IAgentRepository` → `AddScoped<IAgentRepository, AgentRepository>()`，与 [HD-009 §3.2](Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) `GetRepository<TRepository>()` 工厂的 `GetRequiredService<TRepository>()` 消费方式一致
- **与 HD-010 调用方式兼容性**：HD-010 §3.1 先调 `builder.Services.AddEfCorePersistenceBase()` 再追加 `AddSingleton<ISaveChangesInterceptor, InMemoryRowVersionInterceptor>()`——两次 `AddSingleton<ISaveChangesInterceptor, TImpl>()` 分别注册 `AuditingSaveChangesInterceptor` 与 `InMemoryRowVersionInterceptor`，.NET DI 容器对同一服务类型的多次 `Add*` 调用会全部保留（而非后者覆盖前者），`GetServices<ISaveChangesInterceptor>()` 会同时返回两个实例——与 HD-010 §3.1 消费方"汇总全部拦截器"的设计意图一致
- **结论**：`PASS` — HD-009 §3.11 补全代码正确、自洽，且与 HD-010 调用方式兼容

#### 19.6.4 是否引入新的不一致（对应请求项 4）

- **检查方法**：对比 HD-010 本次修改前后的 §3.1 / §12、HD-009 本次新增的 §3.11 代码块 / §13.6，核对是否与报告 §19.1/§19.2 已 `PASS` 的其余检查项（C95、C98、C99、C101）、及 §1~§18 已 reviewed 内容存在新冲突
- **发现**：
  - HD-010 §12"跨 HD 假设已验证"段落措辞与 HD-009 §13.6 结论完全对应，无矛盾表述
  - HD-009 frontmatter `status: reviewed` 未被回退，§13.6 明确"未变项"边界（不改签名/返回类型/错误处理策略），与既有 §1~§12 内容无冲突
  - HD-010 §3.1 新增的 errata 注解未修改任何测试要求 / 覆盖率门槛 / 错误处理条款，与 §19.1 完备性扫描已认定 `pass` 的其余章节无冲突
  - 未发现 HD-011 / HD-012（尚未起草）或 HD-013（尚未起草）与本次修改产生的新引用断链
- **结论**：`PASS` — 未引入新的不一致

#### 19.6.5 复审结论

- **HD-010 §3.1 DI 注册**：`PASS`（19.6.1）
- **HD-010 全文同类问题扫描**：`PASS`，无遗漏（19.6.2）
- **HD-009 §3.11 补全代码正确性与兼容性**：`PASS`（19.6.3）
- **新增不一致排查**：`PASS`，未发现（19.6.4）
- **B16 / B17 状态**：均已修复并核实，`design-review-report.md §19.2` 的 C96 / C97 判定由 `FAIL` 转 `PASS`
- **整体复审结论**：**PASS** — HD-010 首轮评审的 2 项 blocking（B16/B17）均已消除，未发现新增缺陷；non-blocking 项 N29/N30 仍待处理但不阻塞（详 §19.3，处置方向不变：N29 可选补一句 deferral 措辞，N30 已转化为 H5 编码阶段的显式验收标准）
- **是否可推荐 Owner 翻 `reviewed`**：**是**——建议 Owner 在 [HD-010 frontmatter](Inkwell.Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md) 手动将 `status: draft` 翻为 `status: reviewed` 并填写 `reviewers: [Inkwell]`（人工签字位，AI 不代签）

#### 19.6.6 复审自检

- ✅ 仅复核 B16/B17 修复相关章节（HD-010 §3.1/§12、HD-009 §3.11/§13.6），未重跑 §19.1~§19.3 全部检查项
- ✅ 每条 `PASS` 结论均附具体代码片段 / 章节锚点证据，未使用"看起来"/"似乎"等主观词
- ✅ 已扩大检查面到 HD-010 全文其余 DI 注册点（§3.2 `IDbContextInitializer`），而非只看 §3.1 单点
- ✅ 已验证 `AuditingSaveChangesInterceptor` 的 Singleton 生命周期自洽性（依赖 `TimeProvider`，无 Scoped 捕获风险），非仅比对服务类型字符串
- ✅ 未越界修改 HD-010 / HD-009 正文，仅追加评审报告子节
- ✅ 报告路径仍为 [docs/04-detailed-design/design-review-report.md](design-review-report.md)（追加 §19.6，未另开顶层章节）
- ✅ 全程使用 bullet list 呈现（避免中英文混排表格触发 MD060）
