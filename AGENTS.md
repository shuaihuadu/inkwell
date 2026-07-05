# Inkwell

> AI 协作单一事实源——遵循 [AGENTS.md 跨工具开放约定](https://agents.md/)。
> 本仓库采用 [Harness Engineering](.he/HANDBOOK.md) 作为工程骨架。
>
> **本文件状态**：本草稿由 AI 基于 H1 [requirements.md](docs/01-requirements/requirements.md) + H2 [architecture.md](docs/03-architecture/architecture.md) / [tech-selection.md](docs/03-architecture/tech-selection.md) / 16 ADR + [repo-impact-map.md §3.1](docs/01-requirements/repo-impact-map.md) 起草。**§1 / §3 是项目负责人签字位**——AI 不替签：Owner 评审后直接修改本文件并在评审记录中登记。
>
> **2026-05-10 增量更新**：H2 评审接受 [ADR-017 Ports & Adapters 后端拓扑](docs/03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) + [ADR-018 IQueueProvider 双 Provider](docs/03-architecture/adr/ADR-018-queue-abstraction-channels-default.md) + [OQ-A008 closed §B](docs/03-architecture/open-questions-arch.md)；§3.1 / §3.2 / §3.3 / §3.4 / §4 由 AI 在 Owner 一次性授权下同步应用，请在 [docs/07-reviews/2026-05-10-h2-architecture-retrofit.md] 评审记录中复核。
>
> **2026-05-10 增量更新·第二轮**：H2 评审接受 [ADR-019 后端进程拓扑](docs/03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md)：`Inkwell.Host` 拆为 `Inkwell.WebApi` + `Inkwell.Worker` 双进程，csproj 10 → 11。§3.1 / §3.2 / §3.4 / §4 由 AI 在 Owner 一次性授权下同步应用，请在评审记录中复核。
>
> **2026-05-10 增量更新·第三轮**：H2 评审接受 [ADR-020 向量存储抽象](docs/03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md)：复用 [Microsoft.Extensions.VectorData](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-vector-data) 抽象；v1 = `providers/Inkwell.VectorStore.Qdrant/` + `Inkwell.Core/InMemoryVectorStore` 双 Provider，csproj 11 → 12。§3.1 / §4 由 AI 在 Owner 一次性授权下同步应用，请在评审记录中复核。
>
> **2026-05-10 增量更新·第四轮**：H2 评审接受 [ADR-021 EFCore Persistence 共享层 + 三 Provider 多层 csproj 布局](docs/03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)：EFCore family = 4 csproj（1 共享 base + 3 final adapter）；Entity / `OnModelCreating` / 唯一实现 `EfCorePersistenceProvider` / `InkwellSeeder` 集中在 `providers/Inkwell.Persistence.EFCore/` shared base；SqlServer / Postgres final adapter 各自 `Migrations/`；InMemory 不支持 Migration，仅走 [`EnsureCreated`](https://learn.microsoft.com/ef/core/managing-schemas/ensure-created)。csproj 12 → 13（providers/ 8 → 9）。§3.1 / §3.2 / §4 由 AI 在 Owner 一次性授权下同步应用，请在评审记录中复核。
>
> **2026-05-11 增量更新·第五轮**：H2 评审接受 [ADR-022 Entity ↔ Model Mapper 选型](docs/03-architecture/adr/ADR-022-entity-domain-mapper-selection.md)：锁手写扩展方法（`Entity.ToModel()` / `Model.ToEntity()` / `IQueryable<Entity>.SelectAsModel()`），禁 AutoMapper / Mapperly / Mapster；`providers/Inkwell.Persistence.EFCore/` shared base 新增 `Mapping/` + `Repositories/` 子目录；业务命名空间只见 Model、不见 Entity，Model 默认无后缀，撞名降级 `XxxDefinition`。csproj 数不变 13。§3.1 由 AI 在 Owner 一次性授权下同步应用，请在 [docs/07-reviews/2026-05-10-h2-architecture-review.md §11](docs/07-reviews/2026-05-10-h2-architecture-review.md) 评审记录中复核。

## 1. 项目身份

> Inkwell 是一个 dogfooding 项目——完整采用 [Harness Engineering](https://github.com/shuaihuadu/harness-engineering) 工程规范，基于 Microsoft Agent Framework 打造一个可工作的“智能体工厂”。
> 项目目标是用真实落地的代码与文档，端到端验证这套规范 + 配套工具链的有效性。

**当前阶段**：H2（已 Approved，详见 [docs/07-reviews/2026-05-10-h2-architecture-review.md](docs/07-reviews/2026-05-10-h2-architecture-review.md)）；准备进入 H3 详细设计。

**双重定位（来自 [requirements.md §1](docs/01-requirements/requirements.md)）**：

- **业务定位**：满足"团队成员能自助创建 / 配置 / 使用 / 共享 LLM Agent"的内部生产力工具
- **作者定位**：Owner 想从 0 到 1 实现一个 Agent 平台的学习项目

> 草稿由 AI 起草，Owner 请确认或改写为最终一句话。改写后请在 commit message 中登记。

## 2. 技术栈

> 自动可探：根目录 `Directory.Build.props` / `package.json` / `*.sln` 在 H5 起步任务建立后才会出现，本节先以 H2 决策为权威源。

### 2.1 客户端（[ADR-001](docs/03-architecture/adr/ADR-001-client-runtime-electron-react.md)）

- **运行时**：Electron 38+ + React 19 + Vite 6 + TypeScript 5.x（最新 minor）
- **状态管理**：Zustand + React Query（数据获取与缓存）
- **画布**：[`@xyflow/react`](https://reactflow.dev/) 12+（[ADR-006](docs/03-architecture/adr/ADR-006-orchestration-canvas-react-flow.md)）
- **流式 SDK**：[`@ag-ui/client`](https://github.com/ag-ui-protocol)（[ADR-012](docs/03-architecture/adr/ADR-012-client-server-protocol-rest-agui.md)）
- **视觉风格**：参考 Ant Design Pro（[OQ-011 closed](docs/01-requirements/open-questions.md)）

### 2.2 后端（[ADR-002](docs/03-architecture/adr/ADR-002-backend-runtime-dotnet10-aspnetcore.md) + [ADR-003](docs/03-architecture/adr/ADR-003-agent-engine-microsoft-agent-framework.md)）

- **运行时**：.NET 10 + ASP.NET Core 10 + C# 14
- **Agent 引擎**：Microsoft Agent Framework（MAF）— `Microsoft.Agents.AI` / `.AGUI` / `.Workflows` / `.DurableTask`
- **数据访问**：EF Core 10 Code-First + Migrations，三 Provider（InMemory / SQL Server 2025 / PostgreSQL 17）通过 `IPersistenceProvider` 抽象（[ADR-004](docs/03-architecture/adr/ADR-004-data-store-provider-switchable-ef-core.md)）
- **向量库**：Qdrant 1.x，gRPC SDK
- **缓存**：Redis 8（dev 容器 / prod Azure Cache for Redis 或自建 StatefulSet），通过 `ICacheProvider` 抽象（[ADR-016](docs/03-architecture/adr/ADR-016-cache-provider-redis.md)）
- **文件存储**：`IFileStorageProvider` 抽象 + 三 Provider（LocalFileSystem / AzureBlob / MinIO）（[ADR-015](docs/03-architecture/adr/ADR-015-object-storage-provider-switchable.md)）
- **多模态**：Azure Speech ASR + 模型 vision（[ADR-009](docs/03-architecture/adr/ADR-009-multimodal-azure-speech.md)）
- **可观测性**：OpenTelemetry .NET SDK → OTel Collector → Tempo / Loki / Prometheus → Grafana 自托管（[ADR-013](docs/03-architecture/adr/ADR-013-observability-otel-self-hosted-grafana.md)）

### 2.3 部署（[ADR-005](docs/03-architecture/adr/ADR-005-deployment-docker-compose-aks.md)）

- **dev**：Docker Compose（api / postgres / qdrant / redis / minio / otel-collector / tempo / loki / prometheus / grafana）
- **prod**：AKS + Helm Chart；HPA min 2 / max 10
- **CI**：GitHub Actions（[OQ-A007 closed §A](docs/03-architecture/open-questions-arch.md)）

### 2.4 测试

- **后端**：[MSTest.Sdk 4.x](https://github.com/microsoft/testfx)（最新稳定 4.2.2，默认使用 [`Microsoft.Testing.Platform`](https://learn.microsoft.com/dotnet/core/testing/unit-testing-platform-intro) / MTP runner；底层测试框架仍为 `MSTest.TestFramework 3.x`）
- **前端**：[Vitest](https://vitest.dev/)
- **E2E**：[Playwright](https://playwright.dev/)
- **目标矩阵**：Win11 ≥ 22H2 + macOS 12+ Apple Silicon（[OQ-009 closed](docs/01-requirements/open-questions.md)）

### 2.5 私有依赖来源

- 暂无私有 NuGet / npm registry。Azure OpenAI / Azure Speech 凭据走 [Kubernetes Secret](https://kubernetes.io/docs/concepts/configuration/secret/)（prod）/ Docker Compose `.env`（dev）（[OQ-A006 closed §B](docs/03-architecture/open-questions-arch.md)）；**v1 不引入 Azure Key Vault**（残余风险走 [RISK-013](docs/03-architecture/risk-analysis.md)）。

## 3. 模块边界 / 禁区

> 本节由 H2 ADR 群锁定（详见 [docs/03-architecture/adr/](docs/03-architecture/adr/) **22 条 ADR（ADR-001 ~ ADR-022，均 accepted）** + [open-questions-arch.md](docs/03-architecture/open-questions-arch.md) **8 条 closed OQ（OQ-A001 ~ OQ-A008）**）。后端拓扑由 [ADR-017 Ports & Adapters](docs/03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) + [ADR-019 进程拓扑](docs/03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md) 锁定（不再遵循 [repo-impact-map.md](docs/01-requirements/repo-impact-map.md) 的“建议拓扑”，后者是 H1 偏调查提示、不再是加锁实体）。

### 3.1 模块拓扑（[ADR-017](docs/03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) + [ADR-019](docs/03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md)）

后端按 Ports & Adapters 三层组织，物理上 12 个 csproj（[`src/core/`](docs/03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md)）：

**`src/core/Inkwell.Abstractions/`**（端口层 / 1 csproj）

- 全部接口：`IPersistenceProvider` / `IFileStorageProvider` / `ICacheProvider` / `IQueueProvider` / `IAgentRuntime` / `IAuditLogger` / 业务模块对外接口
- **向量存储抽象复用** [`Microsoft.Extensions.VectorData.VectorStore`](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-vector-data) + `VectorStoreCollection<TKey, TRecord>`（[ADR-020](docs/03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md)，Inkwell 不重发明 `IVectorStore`，仅提供 Builder DSL `UseQdrantVectorStore` / `UseInMemoryVectorStore` / `UseAzureOpenAIEmbeddings`）
- DTO / Model / Options
- Builder DSL（`IInkwellBuilder` / `AddInkwell()`、参考 MAF `AgentApplicationBuilder` 模式）

**`src/core/Inkwell.Core/`**（适配器默认实现 + 业务层 / 1 csproj）

- 抽象的进程内默认实现：`InMemoryCacheProvider` / `LocalFileSystemFileStorageProvider` / `ChannelsQueueProvider`（[ADR-018](docs/03-architecture/adr/ADR-018-queue-abstraction-channels-default.md) dev / unit test 默认） / `InMemoryVectorStore`（[ADR-020](docs/03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) dev / unit test 默认，基于 [`Microsoft.Extensions.VectorData.InMemory`](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-vector-data)）
- 业务命名空间：`Inkwell.Core.Auth` / `.Agents` / `.Models` / `.Tools` / `.Skills` / `.KnowledgeBase` / `.Memory` / `.Triggers` / `.Orchestrations` / `.PublicApi` / `.Traces` / `.Versioning` / `.Multimodal` / `.AuditLogs` / `.Conversations` / `.Health`
- `Inkwell.Core.AgentRuntime` 命名空间——**唯一**允许 `using Microsoft.Agents.AI.*` 的位置（[ADR-017 §依赖规则](docs/03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) + [RISK-001](docs/03-architecture/risk-analysis.md)）

**`src/core/providers/`**（多 Provider 适配器 / 9 csproj）

- `Inkwell.Persistence.EFCore/`——EFCore family **共享 base**（[ADR-021](docs/03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) + [ADR-022](docs/03-architecture/adr/ADR-022-entity-domain-mapper-selection.md)）：Entity / `OnModelCreating` / 唯一 [`IPersistenceProvider`](docs/03-architecture/adr/ADR-004-data-store-provider-switchable-ef-core.md) 实现 `EfCorePersistenceProvider` / `InkwellSeeder`（幂等） / `MigrationRunner`；新增 `Mapping/` 子目录存放 per-entity `<TypeName>MappingExtensions.cs`（手写扩展方法 `Entity.ToModel()` / `Model.ToEntity()` / `IQueryable<Entity>.SelectAsModel()`）+ `Repositories/` 子目录存放 `<XxxRepository>` 实现（动词白名单限定 `Get*` / `Find*` / `Add*` / `Update*` / `Remove*` / `Count*` / `List*` / `Exists*` / `Iterate*`）；业务接口只见 Model 不见 Entity
- `Inkwell.Persistence.EFCore.InMemory/`——`UseInMemoryDatabase` + [`EnsureCreated`](https://learn.microsoft.com/ef/core/managing-schemas/ensure-created)（[ADR-021](docs/03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)：[`Microsoft.EntityFrameworkCore.InMemory`](https://learn.microsoft.com/ef/core/providers/in-memory) 不支持 Migration）
- `Inkwell.Persistence.EFCore.SqlServer/`——`UseSqlServer` + 自有 `Migrations/`
- `Inkwell.Persistence.EFCore.Postgres/`——`UseNpgsql` + 自有 `Migrations/`
- `Inkwell.FileStorage.MinIO/`
- `Inkwell.FileStorage.AzureBlob/`
- `Inkwell.Cache.Redis/`
- `Inkwell.Queue.Redis/`——`RedisStreamQueueProvider`（[ADR-018](docs/03-architecture/adr/ADR-018-queue-abstraction-channels-default.md) integration test / prod 默认）
- `Inkwell.VectorStore.Qdrant/`——基于 [`Microsoft.Extensions.VectorData.Qdrant`](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-vector-data) connector（[ADR-020](docs/03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) integration test / prod 默认）

**`src/core/Inkwell.WebApi/`**（HTTP 入口 / 1 csproj，[ADR-019](docs/03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md)）

- ASP.NET Core minimal-host（`Microsoft.NET.Sdk.Web`）
- DI 装配（按 `appsettings.json` 选 Provider）
- REST CRUD endpoints / AG-UI Protocol SSE endpoints / Public API
- **仅注册 IQueueProvider enqueue 侧**，不跑 consumer
- HPA：CPU 70%, min 2 / max 10

**`src/core/Inkwell.Worker/`**（后台进程 / 1 csproj，[ADR-019](docs/03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md)）

- [.NET Generic Host](https://learn.microsoft.com/aspnet/core/fundamentals/host/generic-host) + [`BackgroundService`](https://learn.microsoft.com/aspnet/core/fundamentals/host/hosted-services)（`Microsoft.NET.Sdk.Worker`）
- DI 装配（与 WebApi 共享 `AddInkwell()...UseRedisQueue()` 套装）
- `RedisStreamQueueProvider` consumer group worker（消费 [`XREADGROUP`](https://redis.io/docs/latest/commands/xreadgroup/)）
- [`Microsoft.Agents.AI.DurableTask`](../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.DurableTask/) actor runner
- KB ingest（[REQ-009](docs/01-requirements/requirements.md)） / Trigger fan-out（[REQ-011](docs/01-requirements/requirements.md)） / 后台慢任务
- **不开 HTTP 业务端口**（仅 `/healthz` probe + Prometheus scrape `/metrics`）
- HPA：自定义 metric `queue_depth` ≥ 100, min 1 / max 5；fallback CPU 70%
- WebApi 与 Worker **必须同 image tag 单 Helm release 同步滚**（[RISK-015](docs/03-architecture/risk-analysis.md)）

**客户端 `src/app/desktop/`**（路径由原 `apps/desktop/` 收敛到 `src/app/`）

- `electron/` — 主进程 + 预加载 + 自动更新；持有跨锁屏长 SSE（[ADR-011](docs/03-architecture/adr/ADR-011-auto-lock-with-inflight-task-survival.md)）
- `src/features/auth/` — REQ-001 / NFR-003
- `src/features/lock/` — NFR-003 + OQ-017 在途任务保活
- `src/features/agent-library/` / `agent-detail/` / `chat/` / `orchestration/` / `debug/` / `eval/` / `version/` / `admin/` / `skill-upload/` — 对应 REQ-002 ~ REQ-017
- `src/shared/network/` — NFR-001 连通性（**禁止**任何本地缓存对话）
- `src/shared/design-system/` — Ant Design Pro 风格基线

### 3.2 模块依赖规则

- **客户端 → 后端**：所有 `src/app/desktop/src/features/*` 通过 `src/app/desktop/src/shared/network/` 调用后端 API；不允许跨过 shared 层直连后端。
- **业务命名空间 → 端口层**：`Inkwell.Core.*`（除 `Inkwell.Core.AgentRuntime`）业务命名空间**只能依赖 `Inkwell.Abstractions`** + 进程内 BCL；不得直接 `using Microsoft.Agents.AI.*`、`using StackExchange.Redis`、`using Azure.Storage.Blobs`、`using Microsoft.EntityFrameworkCore.SqlServer`、`using Npgsql.*`、`using Minio.*`。CI 强制由 [Roslyn analyzer / `BannedSymbols.txt`](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/) 验证。
- **`Inkwell.Core.AgentRuntime` 命名空间例外**：是**唯一**允许 `using Microsoft.Agents.AI.*` 的位置；对外暴露 `IAgentRuntime` 接口（在 `Inkwell.Abstractions`），禁止把 MAF type 泄漏到接口签名。
- **providers/* → 端口层**：`src/core/providers/Inkwell.*` 只能引用 `Inkwell.Abstractions` + 该 Provider 自身的 SDK（如 `Microsoft.EntityFrameworkCore.SqlServer` / `Azure.Storage.Blobs` / `StackExchange.Redis`）；**禁止**引用 `Inkwell.Core`。**EFCore family 例外**（[ADR-021](docs/03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）：`providers/Inkwell.Persistence.EFCore.{InMemory,SqlServer,Postgres}` **允许**引用同 providers/ 下的 `Inkwell.Persistence.EFCore` shared base csproj（shared adapter base + final adapter 分层）；base 仍**禁止**引用 `Inkwell.Core` / 其他兄弟 csproj。其他 family（FileStorage / Cache / Queue / VectorStore）**不享受此例外**——如需同样拓扑必须以独立 ADR 为入口（[RISK-017](docs/03-architecture/risk-analysis.md)）。
- **`Inkwell.WebApi` / `Inkwell.Worker` → 全部**（[ADR-019](docs/03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md)）：DI 装配是唯一允许同时 `using` 多个 providers + Inkwell.Core 的位置。`Inkwell.WebApi` 仅注册 enqueue 侧，`Inkwell.Worker` 跑 consumer + DurableTask runner。
- **审计日志注入**：业务命名空间通过 `IAuditLogger`（在 `Inkwell.Abstractions`）写审计；写入失败**不得吞错**，必须走 [ADR-008](docs/03-architecture/adr/ADR-008-audit-log-store-and-query.md) 失败处理路径。
- **prototypes/**：原型仅用于 H1 评审，**不进 main 分支的产品代码**——若产品代码引用了 `prototypes/*` 视为违规。

### 3.3 禁区（v1 显式不做 / 不引入）

下列条目在 v1 严格禁止；如有需求请走 v2 backlog，不得在 H5 编码任务里"顺手做了"。

- **客户端**
  - 不做本地缓存对话历史（[NFR-001](docs/01-requirements/requirements.md)）
  - 不本地化（不引入 i18n 框架，文案直接 zh-CN 字面量；轻量 `tFn(label)` wrapper 是允许的预备，[ADR-014](docs/03-architecture/adr/ADR-014-i18n-out-of-scope-v1.md) + [RISK-010](docs/03-architecture/risk-analysis.md)）
  - 不做客户端 ASR / TTS（[ADR-009](docs/03-architecture/adr/ADR-009-multimodal-azure-speech.md)）
  - 不做客户端 PII / 敏感字段拦截（[OQ-001 closed §A](docs/01-requirements/open-questions.md)）
- **协议**
  - 不实现 AG-UI Run resume / cursor / RunEventStore（[ADR-011](docs/03-architecture/adr/ADR-011-auto-lock-with-inflight-task-survival.md) + [ADR-012](docs/03-architecture/adr/ADR-012-client-server-protocol-rest-agui.md)）
  - 不引入 GraphQL / 纯 gRPC / 自建 WebSocket / SignalR
- **Skills**
  - 不实现 Skill Execution，**不预留 `ISkillExecutor` 接口**（[ADR-010](docs/03-architecture/adr/ADR-010-skill-loading-static-only-v1.md)）
  - 拒收 `SKILL.md` 同级 `scripts/` 目录
- **鉴权 / 权限**
  - 不引入 RBAC / 多租户 / OAuth2 / SSO / OIDC（[OQ-003 closed §A](docs/01-requirements/open-questions.md)）
  - 不开放自助注册 / 自助密码重置（[OQ-005 closed §A](docs/01-requirements/open-questions.md)）— 账号由后端运维通过 SQL 创建
  - 公开 API 不引入多 Token / Token TTL / mTLS（[ADR-007](docs/03-architecture/adr/ADR-007-public-api-token-auth.md)）
- **凭据**
  - **不引入 Azure Key Vault**（[OQ-A006 closed §B](docs/03-architecture/open-questions-arch.md)）— 仅 K8s Secret + Compose `.env`
- **审计**
  - 不做审计导出（[OQ-020 closed §B](docs/01-requirements/open-questions.md)）— 合规导出走后端运维 SQL
  - 不引入 ELK / Loki 双 sink（[ADR-008](docs/03-architecture/adr/ADR-008-audit-log-store-and-query.md)）
- **基础设施**
  - 不引入消息队列**外部中间件**（RabbitMQ / Azure Service Bus）——`IQueueProvider` 接口 + `ChannelsQueueProvider`（dev） + `RedisStreamQueueProvider`（integration / prod）是与 [`IPersistenceProvider`](docs/03-architecture/adr/ADR-004-data-store-provider-switchable-ef-core.md) / [`ICacheProvider`](docs/03-architecture/adr/ADR-016-cache-provider-redis.md) / [`IFileStorageProvider`](docs/03-architecture/adr/ADR-015-object-storage-provider-switchable.md) 同构的环境对称 Provider，**不被视为外部中间件**（[ADR-018](docs/03-architecture/adr/ADR-018-queue-abstraction-channels-default.md) + [OQ-A008 closed §B](docs/03-architecture/open-questions-arch.md)）。
  - 不引入跨 region active-passive（[RISK-004](docs/03-architecture/risk-analysis.md)）— v1 单 region SLA = 99%
  - 不引入 Redis Cluster / Sentinel HA（[ADR-016](docs/03-architecture/adr/ADR-016-cache-provider-redis.md)）— v1 单节点 + PVC 或 Azure Standard tier
  - 不在三种 EF Provider 上做向量检索（[OQ-A001 closed §A](docs/03-architecture/open-questions-arch.md)）— 向量统一走 Qdrant
- **签字位**
  - `docs/01-requirements/` / `docs/03-architecture/` / `docs/04-detailed-design/` / `docs/05-test-design/` 下文档的 `status:` / `reviewers:` 字段一律由人工翻转，AI / Custom Agent / 安装脚本**不得替写**

### 3.4 H1 / H2 待解残余（H3 / H4 必须显式处理）

- **W-003**：[NFR-003 字面](docs/01-requirements/requirements.md) 与 OQ-017 在途任务特例的文字漂移；H4 / H5 引用 NFR-003 时必须同时引用 [ADR-011](docs/03-architecture/adr/ADR-011-auto-lock-with-inflight-task-survival.md) + [AC-076 ~ AC-079](docs/01-requirements/acceptance-criteria.md)（[RISK-003](docs/03-architecture/risk-analysis.md)）
- **RISK-007 主进程长 SSE 跨锁屏可靠性**：H4 必须有恢复性用例（macOS / Windows 锁屏 30 min × 多次 sleep/resume）
- **RISK-011 文件存储三 Provider contract 漏出**：H3 必须建立 `tests/core/Inkwell.Providers.Contract` 公共用例包，CI 跑 LocalFileSystem / Azurite / MinIO 三套 matrix（[ADR-017 §联动提示](docs/03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md)路径名调整）
- **RISK-014 RedisStreamQueueProvider 运维代价**（[OQ-A008 closed §B](docs/03-architecture/open-questions-arch.md) v1 同期交付）：observability 五项补齐项（`queue_consume_latency_p95` / `queue_dlq_count` / `queue_consumer_lag` / `queue_redelivery_count` / `queue_consumer_active`）进 prod ProdReady checklist；H4 必须补三类用例（crash recovery / fairness / DLQ）。
- **RISK-015 WebApi / Worker 双进程版本漂移与 OTel 双 source**（[ADR-019](docs/03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md) 新增）：(1) Helm Chart 单 `image.tag` + `helm upgrade --atomic` 单 release 同时滚；(2) OTel `service.name = inkwell-webapi` / `inkwell-worker` 双 source，Grafana Dashboard 加 Worker 面板；(3) **H4 必须补 enqueue (WebApi) → consume (Worker) → ack 跨服务集成用例**（覆盖 [Inkwell.Triggers REQ-011](docs/01-requirements/requirements.md) / KB ingest [REQ-009](docs/01-requirements/requirements.md) / DurableTask）；(4) `MessageEnvelope` 必含 `traceparent` 字段以保 [REQ-014 trace 全链路](docs/01-requirements/requirements.md) 跨服务不断链。

## 4. 文档入口

- 操作手册：[`.he/HANDBOOK.md`](.he/HANDBOOK.md)
- 阶段细则（H1–H6）：[`.he/docs/stages/`](.he/docs/stages/)
- 需求 / 原型 / 架构 / 详细设计 / 测试 / 任务 / 评审 / 发布：`docs/01-requirements/` … `docs/08-releases/`
- 仓库影响图（H1 ↔ H3 衔接）：[`docs/01-requirements/repo-impact-map.md`](docs/01-requirements/repo-impact-map.md)
- ADR 目录：[`docs/03-architecture/adr/`](docs/03-architecture/adr/)（22 条均 accepted）
- 模板与 Skill：`.github/templates/` 与 `.github/skills/`
- 多语言代码风格：[`.he/docs/instructions-layout.md`](.he/docs/instructions-layout.md)
- Copilot 实施细节：[`.github/copilot-instructions.md`](.github/copilot-instructions.md)
- 提交规范：[`.github/instructions/commit-format.instructions.md`](.github/instructions/commit-format.instructions.md)

## 5. 给 AI 工具的通用指令

- **修改前先读**：上方 §3 模块边界 / 禁区、对应任务的 `docs/06-tasks/T-NNN-*.md`、相关详细设计章节、相关 ADR
- **代码 / 提交 / 文档约束**：见 [`.github/instructions/`](.github/instructions/)（按 `applyTo` 自动加载）
- **签字位**：`status: draft → reviewed`、`reviewers: []`、本文件 §1 / §3 一律由人工填，AI / Custom Agent / 安装脚本不替签
- **风格与禁区违反**：阻塞返回，不要尝试绕路（参见 [`.github/copilot-instructions.md` §5 "反模式"](.github/copilot-instructions.md) + [`.he/agents/_shared/io-contracts.md` §5 "阻塞返回"](.he/agents/_shared/io-contracts.md)）
- **追溯链**：所有变更必须能映射到 `REQ-NNN`（需求）+ `HD-NNN` / `API-NNN` / `DB-NNN`（设计）+ `TC-NNN`（测试）+ `TASK-NNN`（任务），缺一律先补 docs 再写代码
