---
id: ADR-017-backend-module-topology-ports-and-adapters
stage: H2
status: accepted
authors:
  - name: H2-ArchitectAdvisor
    role: agent
reviewers: [ Inkwell Owner ]
created: 2026-05-10
updated: 2026-05-10
upstream:
  - REQ-inkwell-agent-platform
  - repo-impact-map-inkwell-agent-platform
  - ADR-002
  - ADR-003
  - ADR-004
  - ADR-015
  - ADR-016
downstream: []
---

<!-- markdownlint-disable MD060 -->

# ADR-017 后端模块拓扑：Ports & Adapters（替换 capability-folder 拓扑）

## 上下文

[`AGENTS.md` §3.1](../../../AGENTS.md) 在 H2 评审通过时锁定的拓扑是 capability-folder 风格——按业务能力域切 16 个 csproj：`Inkwell.Auth` / `Inkwell.Agents` / `Inkwell.Skills` / `Inkwell.KnowledgeBase` / `Inkwell.Memory` / `Inkwell.Triggers` / `Inkwell.Orchestrations` / `Inkwell.PublicApi` / `Inkwell.Traces` / `Inkwell.Versioning` / `Inkwell.Multimodal` / `Inkwell.AuditLogs` / `Inkwell.Conversations` / `Inkwell.Health` / `Inkwell.AgentRuntime` / `Inkwell.DataAccess` / `Inkwell.Cache` / `Inkwell.FileStorage` / `Inkwell.Common.*`。

H3 详细设计起草启动时，Owner 提出按 [Microsoft Agent Framework](../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI/) 的 Builder DSL 风格重构为 Ports & Adapters：接口位集中（`Inkwell.Abstractions`）、默认实现集中（`Inkwell.Core`）、各 Provider 独立 csproj（`providers/*`）。同步要求路径重命名 `src/server/` → `src/core/`、`apps/desktop/` → `src/app/`。

`AGENTS.md` §3 在脚注中明确："具体路径名仍可在 H3 第一张 task 卡里再校准"——但**模块拓扑模型**（capability-folder vs Ports & Adapters）属于 H2 决策维度，不能由 H3 author 越权拍板。本 ADR 在 H2 阶段重新对齐拓扑模型，为 H3 提供锁定的模块清单。

驱动因素：

- **Builder DSL fluent API 收拢**：`AddInkwell().UseSqlServer()...UseAzureBlob()...UseRedis()...Build()`（参考 [`Microsoft.Agents.AI` 的 `AgentApplicationBuilder`](../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI/)）要求接口位集中可见，capability-folder 下接口分散在 16 个 csproj 中，extension method 类型推导难以收拢。
- **三 Provider 抽象家族 DRY**：[`IPersistenceProvider`](./ADR-004-data-store-provider-switchable-ef-core.md) / [`IFileStorageProvider`](./ADR-015-object-storage-provider-switchable.md) / [`ICacheProvider`](./ADR-016-cache-provider-redis.md) 在 capability-folder 下三个独立模块各自维护"InMemory + 真实 Provider"切换逻辑，重复且难一致。
- **客户裁剪**：Provider 独立 csproj 后，纯 Postgres 客户的运行时无需依赖 `Microsoft.EntityFrameworkCore.SqlServer` / `Npgsql`（仅装一个）；`AzureBlob` 客户无需装 `MinIO` SDK。
- **Contract test 物理落点**：[RISK-002](../risk-analysis.md) / [RISK-011](../risk-analysis.md) / [RISK-012](../risk-analysis.md) 都需要"三 Provider 公共契约用例包"——Ports & Adapters 下集中放 `tests/core/providers/Inkwell.Providers.Contract/`，capability-folder 下散在三处难一致。

## 决策

**采用 Ports & Adapters 拓扑**，物理结构如下：

```text
inkwell/
├── Inkwell.slnx
├── src/
│   ├── core/
│   │   ├── Inkwell.Abstractions/                    ← 接口 + Model + Options + DI Builder
│   │   ├── Inkwell.Core/                            ← 默认实现 + AgentRuntime + 业务编排域
│   │   │   └── （内部 namespace 隔离：Inkwell.Core.Auth / .Agents /
│   │   │     .Skills / .KnowledgeBase / .Memory / .Triggers /
│   │   │     .Orchestrations / .Traces / .Versioning / .Multimodal /
│   │   │     .Conversations / .Health / .AgentRuntime）
│   │   ├── providers/
│   │   │   ├── Inkwell.Persistence.EFCore/                ← 共享 base（[ADR-021](./ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）
│   │   │   ├── Inkwell.Persistence.EFCore.SqlServer/
│   │   │   ├── Inkwell.Persistence.EFCore.Postgres/
│   │   │   ├── Inkwell.FileStorage.MinIO/
│   │   │   ├── Inkwell.FileStorage.AzureBlob/
│   │   │   ├── Inkwell.Cache.Redis/
│   │   │   ├── Inkwell.Queue.Redis/                  ← OQ-A008 closed §B：v1 同期实现（环境对称论据）
│   │   │   └── Inkwell.VectorStore.Qdrant/             ← [ADR-020](./ADR-020-vector-store-microsoft-extensions-vectordata.md)：Microsoft.Extensions.VectorData.Qdrant connector
│   │   ├── Inkwell.WebApi/                          ← ASP.NET Core HTTP / REST / AGUI 入口（ADR-019 重命名自 Inkwell.Host）
│   │   └── Inkwell.Worker/                          ← 队列 consumer + DurableTask runner（ADR-019 新增）
│   └── app/
│       ├── electron/                                ← 原 apps/desktop/electron/
│       └── web/                                     ← 原 apps/desktop/src/
├── tests/
│   ├── core/
│   │   ├── Inkwell.Abstractions.Tests/
│   │   ├── Inkwell.Core.Tests/
│   │   ├── providers/
│   │   │   ├── Inkwell.Persistence.EFCore.Tests/             ← [ADR-021](./ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) base 单元测试
│   │   │   ├── Inkwell.Persistence.EFCore.SqlServer.Tests/
│   │   │   ├── Inkwell.Persistence.EFCore.Postgres.Tests/
│   │   │   ├── Inkwell.FileStorage.MinIO.Tests/
│   │   │   ├── Inkwell.FileStorage.AzureBlob.Tests/
│   │   │   ├── Inkwell.Cache.Redis.Tests/
│   │   │   └── Inkwell.Queue.Redis.Tests/
│   │   └── Inkwell.Providers.Contract/              ← contract test 公共用例包
│   └── app/
│       └── ...
├── docker/
│   └── docker-compose.yml
├── .editorconfig
├── .gitignore
├── .dockerignore
├── Directory.Build.props
├── Directory.Packages.props                          ← 中央包管理（启用）
├── README.md
└── LICENSE
```

### 模块映射（旧 §3.1 → 新拓扑）

| 旧（AGENTS.md §3.1）                                                                                                                                            | 新                                                                                                                                                                       |
| --------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `Inkwell.Common.*`                                                                                                                                              | `Inkwell.Abstractions`（Result / Error / Options 模型） + `Inkwell.Core`（Telemetry / Auth middleware 实现）                                                             |
| `Inkwell.DataAccess`                                                                                                                                            | 接口 → `Inkwell.Abstractions`；实现 → `providers/Persistence/Inkwell.Persistence.EFCore.{SqlServer,Postgres}` 两 csproj                                                              |
| `Inkwell.Cache`                                                                                                                                                 | 接口 → `Inkwell.Abstractions`；`InMemoryCacheProvider` 默认 → `Inkwell.Core`；`RedisCacheProvider` → `providers/Cache/Inkwell.Cache.Redis`                                     |
| §队列（[ADR-018](./ADR-018-queue-abstraction-channels-default.md)）                                                                                            | 接口 `IQueueProvider` → `Inkwell.Abstractions`；`ChannelsQueueProvider` 默认 → `Inkwell.Core`；`RedisStreamQueueProvider` → `providers/Queue/Inkwell.Queue.Redis`（[OQ-A008 closed §B](../open-questions-arch.md)） |
| `Inkwell.FileStorage`                                                                                                                                           | 接口 → `Inkwell.Abstractions`；`LocalFileSystemProvider` 默认 → `Inkwell.Core`；`MinIO` / `AzureBlob` → `providers/*`                                                    |
| `Inkwell.AgentRuntime`                                                                                                                                          | **不再设置自建 Runtime facade**；`Inkwell.Abstractions` 通过 `IAgentFactory` 直接产出 MAF `AIAgent`，协议 Hosting 与 Workflow 复用同一 Agent 实例 |
| 业务模块（Auth / Agents / Skills / KnowledgeBase / Memory / Triggers / Orchestrations / Traces / Versioning / Multimodal / Conversations / Health）             | **合进** `Inkwell.Core.<Module>` 命名空间，namespace + folder 软隔离；不再每业务一 csproj                                                                                |
| `Inkwell.PublicApi` / `Inkwell.Api` / `Inkwell.AGUI`                                                                                                            | 应用入口层；[ADR-019](./ADR-019-process-topology-webapi-worker-split.md) 锁定 `Inkwell.WebApi`（HTTP/REST/AGUI/Public） + `Inkwell.Worker`（队列 consumer + DurableTask runner）双进程                                                                                               |

### 依赖规则（替换 §3.2）

```text
                ┌──────────────────────────┐
                │ Inkwell.WebApi           │  Inkwell.Worker
                │ (ASP.NET Core HTTP)      │  (BackgroundService)
                │ ↑ ADR-019 双进程拓扑 ↑    │
                └────────┬─────────────────┘
                         │ 依赖
              ┌──────────▼───────────┐
              │ Inkwell.Core         │
              │ (业务实现 + 默认     │
              │  Provider + Agent-   │
              │  Runtime 命名空间)   │
              └──────────┬───────────┘
                         │ 仅依赖接口
              ┌──────────▼───────────┐                ┌────────────────────────┐
              │ Inkwell.Abstractions │ ←仅依赖接口────│ providers/*            │
              │ (接口 + Model +      │                │ (各 Provider 实现)     │
              │  Options + Builder)  │                │                        │
              └──────────────────────┘                └────────────────────────┘
                         │
                         │ 不依赖任何 Provider 包
                         │ 不依赖任何业务实现
```

具体规则：

1. **业务代码**（`Inkwell.Core` 内业务命名空间 + `Inkwell.WebApi` + `Inkwell.Worker`）→ 只能依赖 `Inkwell.Abstractions` 中的接口；不允许直接 `using StackExchange.Redis` / `Microsoft.EntityFrameworkCore.SqlServer` / `Azure.Storage.Blobs` / `Minio`。
2. **`providers/*`** → 只依赖 `Inkwell.Abstractions`，**不依赖** `Inkwell.Core`（避免循环 + 让 provider 可独立分发）。**EFCore family 例外**（[ADR-021](./ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）：`Inkwell.Persistence.EFCore.{SqlServer,Postgres}` 允许引用同位于 `providers/` 下的 `Inkwell.Persistence.EFCore` base csproj（shared adapter base + final adapter 分层）；base 仍**禁止**引用 `Inkwell.Core` / 其他兄弟 csproj。
3. **MAF 是 Inkwell 的核心运行时契约** → `Inkwell.Abstractions` 允许公开引用 `Microsoft.Agents.AI`，`IAgentFactory.BuildAsync` 直接返回 `AIAgent`。Inkwell 不再复制 `AgentRunRequest` / `AgentRunEvent` / `AgentTurnResult`，也不再维护 MAF 与自建 Runtime DTO 的双向 Mapper。具体模型客户端创建仍集中在 `Inkwell.Core.AgentRuntime`，业务模块不得直接构造 Azure/OpenAI SDK 客户端。

    > **2026-07-12 errata（取代 2026-07-10 RoutingAgent 方案）**：`IAgentRuntime`、`IAgentInvocationService`、`RoutingAgent`、`AgentResponseMapper` 与 `DatabaseChatHistoryProvider` 的旧实现已移除。新主干以不可变 `AgentVersion.Snapshot` 作为构建输入，由 `IAgentFactory` 生成标准 MAF `AIAgent`；AG-UI、OpenAI Chat Completions、OpenAI Responses 与 OpenAI Conversations Hosting 直接消费该实例。会话连续性使用 MAF `AgentSession` / `ChatHistoryProvider`，协议层不再通过自定义 `RoutingAgent` 模拟动态 Agent。
4. **`Inkwell.Abstractions`** → 允许依赖 `Microsoft.Extensions.*.Abstractions`、`Microsoft.Extensions.VectorData.Abstractions`、`Microsoft.Extensions.AI.Abstractions` 与核心契约 `Microsoft.Agents.AI`；仍禁止依赖 EF Core / Redis / Azure / Minio 等 Provider 实现包。
5. **`Inkwell.WebApi` / `Inkwell.Worker` → 全部**：DI 装配是唯一允许同时 `using` 多个 providers + `Inkwell.Core` 的位置（[ADR-019](./ADR-019-process-topology-webapi-worker-split.md)）。
6. **客户端 → 后端**：保持 [`AGENTS.md` §3.2](../../../AGENTS.md) 原有规则，路径名按本 ADR 调整（`apps/desktop/` → `src/app/`）。
7. **`prototypes/`**：保持 §3.2 原规则，不进 main 分支产品代码。

### Builder DSL（参考 MAF）

```csharp
// Inkwell.Abstractions
public static class InkwellHostBuilderExtensions
{
    public static IInkwellBuilder AddInkwell(this IServiceCollection services);
}

public interface IInkwellBuilder
{
    IInkwellBuilder UseSqlServer(string connectionString);
    IInkwellBuilder UseAzureBlob(Action<AzureBlobOptions> configure);
    IInkwellBuilder UseRedis(string connectionString);
    IInkwellBuilder UseDefaults();   // 等价于 UseLocalFileSystem + UseInMemoryCache（Persistence 无默认值，必须显式调用 UseSqlServer / UsePostgres，2026-07-08 移除 InMemory 关系型 Provider）
    IServiceCollection Build();
}
```

具体扩展方法（如 `UseSqlServer` / `UseAzureBlob` / `UseRedis`）实现位于对应 `providers/*` csproj 中，作为 `IInkwellBuilder` 的扩展方法。Provider 不引入 = `using` 不引用 = 扩展方法不可见——天然裁剪。

## 备选项

### 备选 A：维持 capability-folder 拓扑（AGENTS.md §3.1 现状）

- **放弃理由**：
  1. 三 Provider 切换逻辑（`IPersistenceProvider` / `IFileStorageProvider` / `ICacheProvider`）在 `Inkwell.DataAccess` / `Inkwell.FileStorage` / `Inkwell.Cache` 三个独立模块各自维护"InMemory + 真实 Provider"切换逻辑——重复且难一致，违反 DRY。
  2. Builder DSL `AddInkwell().UseSqlServer()...Build()` 要求接口位集中可见，capability-folder 下 fluent API 必须 cross-cutting 引用 16 个 csproj 的接口才能形成 extension method chain。
  3. 业务模块边界（Auth / Agents / Skills / KB）在 v1 单 backend 进程下仅 namespace 级隔离即够——csproj 物理隔离的维护代价（16 个 .csproj、16 套 PackageReference、16 个测试 csproj）无对应运行时收益。

### 备选 B：Ports & Adapters（本决议）

- **被选用**：
  1. 三 Provider 抽象在 `Inkwell.Abstractions` 集中，contract 公共用例包 `tests/core/providers/Inkwell.Providers.Contract/` 物理隔离，[RISK-002](../risk-analysis.md) / [RISK-011](../risk-analysis.md) / [RISK-012](../risk-analysis.md) 缓解动作有清晰落点。
  2. 与 [Microsoft Agent Framework](../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI/) `AgentApplicationBuilder` Builder DSL 风格一致，团队认知对齐。
  3. 客户运行时只装需要的 Provider 包，可裁剪。
  4. csproj 数从 16+ 收敛到 11（Abstractions / Core / 7 providers / WebApi / Worker），编译图更简单。

### 备选 C：极简 monolith（单 csproj `Inkwell`）

- **放弃理由**：
  1. 单 csproj 让 `Microsoft.EntityFrameworkCore.SqlServer` / `Npgsql` / `StackExchange.Redis` / `Azure.Storage.Blobs` / `Minio` 全部成为强制依赖；客户即使只用 PostgreSQL 也得装 SqlServer 包，违反裁剪原则。
  2. 测试 contract 包无法物理隔离，[RISK-002](../risk-analysis.md) / [RISK-011](../risk-analysis.md) / [RISK-012](../risk-analysis.md) 缓解手段失效。
  3. `Inkwell.Abstractions`（公开 contract surface）与 `Inkwell.Core`（内部实现）合并后，第三方 Provider 扩展开发者无法仅引用 contract 包。

### 备选 D：微服务（按业务能力拆独立服务）

- **放弃理由**：
  1. v1 用户量级 ~100（[NFR-001](../../01-requirements/requirements.md) / [Q-A4](../../01-requirements/repo-impact-map.md)），单 backend 进程已够。
  2. 微服务拆分需 service mesh / discovery / cross-service tracing，[ADR-013](./ADR-013-observability-otel-self-hosted-grafana.md) 横切复杂度暴涨。
  3. [OQ-006 closed §A](../../01-requirements/open-questions.md) v1 范围裁剪压力下，微服务拆分代价不对称。

## 后果

### 正面

- `IPersistenceProvider` / `IFileStorageProvider` / `ICacheProvider` 三抽象的实现物理隔离，contract test 公共包（`tests/core/providers/Inkwell.Providers.Contract/`）是 [RISK-002](../risk-analysis.md) / [RISK-011](../risk-analysis.md) / [RISK-012](../risk-analysis.md) 缓解动作的统一落地点。
- Builder DSL 与 [Microsoft Agent Framework](../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI/) 风格一致，新 joiner 学习曲线短。
- csproj 数从 16+ → 11（[ADR-018](./ADR-018-queue-abstraction-channels-default.md) +1：`Inkwell.Queue.Redis`；[ADR-019](./ADR-019-process-topology-webapi-worker-split.md) +1：`Inkwell.Worker`），`Inkwell.slnx` 编译图清晰；客户运行时仅装需要的 Provider。
- 第三方扩展开发：仅引用 `Inkwell.Abstractions` 即可写 custom Provider。

### 负面

- **[RISK-001](../risk-analysis.md) MAF 隔离的硬边界（`Inkwell.AgentRuntime` 独立 csproj）丢失**——替换为软边界：lint 规则禁 `using Microsoft.Agents.AI.*` 在 `Inkwell.Core.AgentRuntime` 之外的 namespace，外加接口收敛 `IAgentRuntime`。如未来 `Inkwell.Core` 内部规模膨胀，lint 规则可能漂洗。需要在 [risk-analysis.md RISK-001](../risk-analysis.md) 重写缓解方案；详见 [§迁移路径](#迁移路径) 第 3 步。
- **breaking change**：[`AGENTS.md` §3.1 / §3.2 / §3.3](../../../AGENTS.md) + [repo-impact-map.md L61 起](../../01-requirements/repo-impact-map.md) + [`ADR-004` / `ADR-015` / `ADR-016`](.) 中"实现路径"表述都需更新；这些是 reviewed 文档，需要人工翻 status。
- 业务模块边界从 csproj 降级为 namespace，重构期间需要 lint 强约束 + 代码评审纪律。

### 中性

- `src/server/` → `src/core/`、`apps/desktop/` → `src/app/`：[`AGENTS.md` §3 footnote](../../../AGENTS.md) 允许 H3 校准，本 ADR 一并钉死。
- 应用入口拆 `Inkwell.WebApi` + `Inkwell.Worker` 双进程：交 [ADR-019](./ADR-019-process-topology-webapi-worker-split.md) 锁定，本 ADR 仅同步 csproj 清单。
- `Directory.Packages.props` 中央包管理：建议启用，具体启用与否留 H3 HD-001。

## 迁移路径

**breaking change 标记**：是。本 ADR 落地后，下列文档需人工更新（按依赖顺序）：

| 步骤 | 文件                                                                                                                                                                             | 改动                                                                                                 | 是否需翻 status  |
| ---- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------- | ---------------- |
| 1    | [`AGENTS.md` §3.1](../../../AGENTS.md)                                                                                                                                           | 模块清单整体重写为新拓扑（`Inkwell.Abstractions` / `Inkwell.Core` / `providers/*` / `Inkwell.Host`） | 签字位 ⚠️         |
| 2    | [`AGENTS.md` §3.2](../../../AGENTS.md)                                                                                                                                           | 依赖规则改为：业务代码只能依赖 `Inkwell.Abstractions`；`Inkwell.Core.AgentRuntime` 是唯一 MAF 接触面 | 签字位 ⚠️         |
| 3    | [`risk-analysis.md` RISK-001](../risk-analysis.md)                                                                                                                               | 缓解方案从"独立 csproj 硬边界"改为"lint + 接口收敛软边界" + 残余风险（lint 漂洗）                    | reviewed → draft |
| 4    | [`repo-impact-map.md` L61 起](../../01-requirements/repo-impact-map.md)                                                                                                          | "src/server/Inkwell.*" 路径声明全替为新拓扑路径                                                      | reviewed → draft |
| 5    | [`ADR-004`](./ADR-004-data-store-provider-switchable-ef-core.md) / [`ADR-015`](./ADR-015-object-storage-provider-switchable.md) / [`ADR-016`](./ADR-016-cache-provider-redis.md) | "实现 csproj 路径"表述更新为 `providers/*` 形态                                                      | reviewed → draft |
| 6    | [`architecture.md` §1 总体图 + §3 后端架构](../architecture.md)                                                                                                                  | 总体图重绘 + §3 后端架构重写                                                                         | reviewed → draft |
| 7    | [`tech-selection.md`](../tech-selection.md)                                                                                                                                      | 新增"§后端模块拓扑"条目                                                                              | reviewed → draft |

**自动化检查命令**（落地后用以确认旧路径已清理）：

```bash
# 检查残留旧路径声明
grep -rn "src/server" docs/ AGENTS.md
grep -rn "apps/desktop" docs/ AGENTS.md
grep -rn "Inkwell\.\(DataAccess\|Cache\|FileStorage\|AgentRuntime\)/" docs/ AGENTS.md

# 检查业务模块仍以独立 csproj 引用（应无命中）
grep -rn "Inkwell\.\(Auth\|Agents\|Skills\|KnowledgeBase\|Memory\|Triggers\|Orchestrations\|Traces\|Versioning\|Multimodal\|Conversations\|Health\)/" docs/ AGENTS.md
```

## 状态

`accepted` — 2026-05-10。同期 [ADR-018 IQueueProvider 双 Provider](./ADR-018-queue-abstraction-channels-default.md) + [OQ-A008 closed §B](../open-questions-arch.md) 加补 `Inkwell.Queue.Redis` csproj（providers/ 6 → 7）；同期 [ADR-019 进程拓扑](./ADR-019-process-topology-webapi-worker-split.md) 重命名 `Inkwell.Host` → `Inkwell.WebApi` + 新增 `Inkwell.Worker`；同期 [ADR-020 向量存储抽象](./ADR-020-vector-store-microsoft-extensions-vectordata.md) 加补 `Inkwell.VectorStore.Qdrant` csproj（providers/ 7 → 8）；同期 [ADR-021 EFCore Persistence 共享层](./ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 加补 `Inkwell.Persistence.EFCore` base csproj（providers/ 8 → 9）。后端总 csproj 从 9 上调为 13。

> **2026-07-09 errata（Owner 决定 dev 默认实现也拆进 providers/，取代本节及 §模块映射表「`InMemoryCacheProvider`/`ChannelsQueueProvider`/`LocalFileSystemProvider` 默认 → `Inkwell.Core`」的表述）**：`Inkwell.Core` 业务代码量持续增长后，Owner 判断"零外部依赖 ≠ 必须放 Core"，之前放 Core 是为了让 `UseDefaults()` 免装 provider csproj 就能跑，但这个便利性收益不足以抵消 Core 与 providers/* 拓扑不对称带来的持续解释成本（[ADR-020 备选 C](./ADR-020-vector-store-microsoft-extensions-vectordata.md) 当年就已经因为这个不对称被放弃，只是 VectorStore 一个用了对称拓扑、Cache/Queue/FileStorage 三个仍留 Core）。新状态：四个 dev 默认实现全部拆成独立 `providers/*` csproj，与 Redis/MinIO/AzureBlob/Qdrant 完全对称：
>
> | 端口 | 默认实现新位置 |
> | --- | --- |
> | `ICacheProvider` | `providers/Cache/Inkwell.Cache.InMemory/` |
> | `IQueueProvider` | `providers/Queue/Inkwell.Queue.Channels/` |
> | `IFileStorageProvider` | `providers/FileStorage/Inkwell.FileStorage.Local/` |
> | Vector Store | `providers/VectorStore/Inkwell.VectorStore.InMemory/`（同步取代 [ADR-020](./ADR-020-vector-store-microsoft-extensions-vectordata.md) D2=C 的决定） |
>
> `Inkwell.Core` 只留业务命名空间（Auth/Agents/Models/Tools/Skills/KnowledgeBase/Memory/PublicApi/Traces/Versioning/Multimodal/Conversations/Health）+ `AgentRuntime/`；不再含任何 `ICacheProvider`/`IQueueProvider`/`IFileStorageProvider`/Vector Store 的默认实现。代价：`Inkwell.WebApi`/`Inkwell.Worker`/`Inkwell.Migrator` 即便只想跑最简单的 dev 默认组合，也需要显式引用这四个新 csproj（不再有"零额外引用"的 `UseDefaults()` 捷径）。后端总 csproj 13 → 17（providers/ 8 → 12）。

> **2026-07-18 errata（Provider 按能力增加一级物理目录）**：`src/core/providers/` 下的项目按 `Cache/`、`FileStorage/`、`LLM/`、`Persistence/`、`Queue/`、`VectorStore/` 六类组织。项目目录名、程序集名、命名空间和依赖方向均不改变；分类目录不构成新的模块或依赖授权。`Inkwell.LLM.LiteLLM` 随 [ADR-026](./ADR-026-model-gateway-litellm.md) 加入后，当前 Provider 为 13 个 csproj，后端总计 19 个 csproj。

## 置信度

`high` — 重构幅度大，但 MAF Builder DSL 风格借鉴有据，四 Provider 抽象家族（[`ADR-004`](./ADR-004-data-store-provider-switchable-ef-core.md) / [`ADR-015`](./ADR-015-object-storage-provider-switchable.md) / [`ADR-016`](./ADR-016-cache-provider-redis.md) / [`ADR-018`](./ADR-018-queue-abstraction-channels-default.md)）均遵 ports & adapters 态势，本 ADR 是补齐物理拓扑层。`Inkwell.Core.AgentRuntime` 合并代价由软边界（lint）补偿——见后果·负面。进程拓扑拆分交 [ADR-019](./ADR-019-process-topology-webapi-worker-split.md) 锁定。
