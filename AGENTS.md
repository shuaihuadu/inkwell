# Inkwell

> AI 协作单一事实源——遵循 [AGENTS.md 跨工具开放约定](https://agents.md/)。
> 本仓库采用 [Harness Engineering](.he/HANDBOOK.md) 作为工程骨架。
>
> **本文件状态**：本草稿由 AI 基于 H1 [requirements.md](docs/01-requirements/requirements.md) + H2 [architecture.md](docs/03-architecture/architecture.md) / [tech-selection.md](docs/03-architecture/tech-selection.md) / 16 ADR + [repo-impact-map.md §3.1](docs/01-requirements/repo-impact-map.md) 起草。**§1 / §3 是项目负责人签字位**——AI 不替签：Owner 评审后直接修改本文件并在评审记录中登记。

## 1. 项目身份

> Inkwell 是面向团队 / 企业内部使用（约 100 人量级）的 LLM Agent 平台：
> 团队成员能在统一 Electron 客户端中创建 / 配置 / 使用 / 共享自己或团队的 AI Agent。

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

- **后端**：[MSTest v3](https://github.com/microsoft/testfx)（`MSTest.Sdk` + `Microsoft.Testing.Platform`）
- **前端**：[Vitest](https://vitest.dev/)
- **E2E**：[Playwright](https://playwright.dev/)
- **目标矩阵**：Win11 ≥ 22H2 + macOS 12+ Apple Silicon（[OQ-009 closed](docs/01-requirements/open-questions.md)）

### 2.5 私有依赖来源

- 暂无私有 NuGet / npm registry。Azure OpenAI / Azure Speech 凭据走 [Kubernetes Secret](https://kubernetes.io/docs/concepts/configuration/secret/)（prod）/ Docker Compose `.env`（dev）（[OQ-A006 closed §B](docs/03-architecture/open-questions-arch.md)）；**v1 不引入 Azure Key Vault**（残余风险走 [RISK-013](docs/03-architecture/risk-analysis.md)）。

## 3. 模块边界 / 禁区

> 本节由 H2 ADR 群锁定（详见 [docs/03-architecture/adr/](docs/03-architecture/adr/) 16 条 ADR + [open-questions-arch.md](docs/03-architecture/open-questions-arch.md) 7 条 closed OQ）。模块拓扑沿用 [repo-impact-map.md §3.1](docs/01-requirements/repo-impact-map.md) 的"建议拓扑"——具体路径名仍可在 H3 第一张 task 卡里再校准。

### 3.1 模块拓扑

**客户端 `apps/desktop/`**：

- `electron/` — 主进程 + 预加载 + 自动更新；持有跨锁屏长 SSE（[ADR-011](docs/03-architecture/adr/ADR-011-auto-lock-with-inflight-task-survival.md)）
- `src/features/auth/` — REQ-001 / NFR-003
- `src/features/lock/` — NFR-003 + OQ-017 在途任务保活
- `src/features/agent-library/` / `agent-detail/` / `chat/` / `orchestration/` / `debug/` / `eval/` / `version/` / `admin/` / `skill-upload/` — 对应 REQ-002 ~ REQ-017
- `src/shared/network/` — NFR-001 连通性（**禁止**任何本地缓存对话）
- `src/shared/design-system/` — Ant Design Pro 风格基线

**后端 `src/server/`**：

- `Inkwell.Auth/` — REQ-001 / REQ-017 / NFR-003
- `Inkwell.Agents/` — REQ-002 ~ REQ-006
- `Inkwell.Models/Providers/` — REQ-005 / REQ-006（v1 真接 Azure OpenAI，其他厂商仅占接入位）
- `Inkwell.Tools/` — REQ-007
- `Inkwell.Skills/` — REQ-008（**v1 仅 Discovery + Activation**，不含 Execution；[ADR-010](docs/03-architecture/adr/ADR-010-skill-loading-static-only-v1.md)）
- `Inkwell.KnowledgeBase/` — REQ-009
- `Inkwell.Memory/` — REQ-010
- `Inkwell.Triggers/` — REQ-011
- `Inkwell.Orchestrations/` — REQ-012（基于 MAF Workflows + DurableTask）
- `Inkwell.PublicApi/` — REQ-013（[ADR-007](docs/03-architecture/adr/ADR-007-public-api-token-auth.md)）
- `Inkwell.Traces/` — REQ-014
- `Inkwell.Versioning/` — REQ-015
- `Inkwell.Multimodal/` — REQ-016（含 Azure Speech ASR）
- `Inkwell.AuditLogs/` — NFR-004（[ADR-008](docs/03-architecture/adr/ADR-008-audit-log-store-and-query.md)）
- `Inkwell.Conversations/` — NFR-005
- `Inkwell.Health/` — NFR-001 探针
- `Inkwell.AgentRuntime/` — MAF 门面层（隔离 MAF 升级影响，详见 §3.2）
- `Inkwell.DataAccess/` — `IPersistenceProvider` / `IVectorStore` 实现层
- `Inkwell.Cache/` — `ICacheProvider` 实现层
- `Inkwell.FileStorage/` — `IFileStorageProvider` 实现层
- `Inkwell.Common.*/` — Result / Error / Telemetry / Auth middleware

### 3.2 模块依赖规则

- **客户端 → 后端**：所有 `apps/desktop/src/features/*` 通过 `apps/desktop/src/shared/network/` 调用后端 API；不允许跨过 shared 层直连后端。
- **后端跨模块**：业务模块（`Inkwell.Agents` / `Inkwell.Skills` / `Inkwell.KnowledgeBase` / ...）**只能依赖 `Inkwell.AgentRuntime` 门面层**，不得直接 `using Microsoft.Agents.AI.*`（隔离 MAF 升级影响，[RISK-001](docs/03-architecture/risk-analysis.md)）。
- **Repository 层**：业务代码**只能依赖** `IPersistenceProvider` / `IFileStorageProvider` / `ICacheProvider` 三组接口；不得直接 `using StackExchange.Redis` / `Azure.Storage.Blobs` / `Microsoft.EntityFrameworkCore.SqlServer` 等具体 Provider 包（[RISK-002](docs/03-architecture/risk-analysis.md) / [RISK-011](docs/03-architecture/risk-analysis.md) / [RISK-012](docs/03-architecture/risk-analysis.md)）。
- **Inkwell.AuditLogs**：被几乎所有写操作模块依赖；写入失败**不得吞错**，必须走 [ADR-008](docs/03-architecture/adr/ADR-008-audit-log-store-and-query.md) 失败处理路径。
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
  - 不引入消息队列（RabbitMQ / Service Bus）— v1 用 `System.Threading.Channels` 内异步
  - 不引入跨 region active-passive（[RISK-004](docs/03-architecture/risk-analysis.md)）— v1 单 region SLA = 99%
  - 不引入 Redis Cluster / Sentinel HA（[ADR-016](docs/03-architecture/adr/ADR-016-cache-provider-redis.md)）— v1 单节点 + PVC 或 Azure Standard tier
  - 不在三种 EF Provider 上做向量检索（[OQ-A001 closed §A](docs/03-architecture/open-questions-arch.md)）— 向量统一走 Qdrant
- **签字位**
  - `docs/01-requirements/` / `docs/03-architecture/` / `docs/04-detailed-design/` / `docs/05-test-design/` 下文档的 `status:` / `reviewers:` 字段一律由人工翻转，AI / Custom Agent / 安装脚本**不得替写**

### 3.4 H1 / H2 待解残余（H3 / H4 必须显式处理）

- **W-003**：[NFR-003 字面](docs/01-requirements/requirements.md) 与 OQ-017 在途任务特例的文字漂移；H4 / H5 引用 NFR-003 时必须同时引用 [ADR-011](docs/03-architecture/adr/ADR-011-auto-lock-with-inflight-task-survival.md) + [AC-076 ~ AC-079](docs/01-requirements/acceptance-criteria.md)（[RISK-003](docs/03-architecture/risk-analysis.md)）
- **RISK-007 主进程长 SSE 跨锁屏可靠性**：H4 必须有恢复性用例（macOS / Windows 锁屏 30 min × 多次 sleep/resume）
- **RISK-011 文件存储三 Provider contract 漏出**：H3 必须建立 `Inkwell.FileStorage.Tests.Contract` 公共用例包，CI 跑 LocalFileSystem / Azurite / MinIO 三套 matrix

## 4. 文档入口

- 操作手册：[`.he/HANDBOOK.md`](.he/HANDBOOK.md)
- 阶段细则（H1–H6）：[`.he/docs/stages/`](.he/docs/stages/)
- 需求 / 原型 / 架构 / 详细设计 / 测试 / 任务 / 评审 / 发布：`docs/01-requirements/` … `docs/08-releases/`
- 仓库影响图（H1 ↔ H3 衔接）：[`docs/01-requirements/repo-impact-map.md`](docs/01-requirements/repo-impact-map.md)
- ADR 目录：[`docs/03-architecture/adr/`](docs/03-architecture/adr/)（16 条 reviewed）
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
