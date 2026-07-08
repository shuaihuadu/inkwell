---
id: tech-selection-inkwell-agent-platform
stage: H2
status: reviewed
authors:
  - name: H2-ArchitectAdvisor
    role: agent
reviewers: [Inkwell]
created: 2026-05-08
updated: 2026-05-10
upstream:
  - REQ-inkwell-agent-platform
  - repo-impact-map-inkwell-agent-platform
downstream:
  - architecture-inkwell-agent-platform
---

<!-- 2026-05-10 追加 §19 后端模块拓扑（ADR-017）+ §20 队列抽象（ADR-018）六字段；ADR-019 进程拓扑拆分 §21 + §21.11 备选项打分表。ADR-020 向量存储抽象作为 ADR-004 的精化 refinement，本文件 §0 摘要表 + §4 + §22 已同步。ADR-021 EFCore Persistence 共享层作为 ADR-004 的精化 refinement，本文件 §0 摘要表 + §4 + §22 已同步。
     2026-05-11 追加 ADR-022 Entity ↔ Model mapper 选型作为 ADR-021 的下游 refinement，本文件 §0 摘要表 + §22 自检已同步；不独立占 §节位（手写映射 = 实现级决策，六字段在 ADR-022 内完成）。
     人工评审动作：确认 status 仍保持 reviewed（incremental update）或翻为 draft 重新过评审。 -->

<!-- markdownlint-disable MD060 -->
<!-- 中文 + 英文混排长表格在 markdownlint 列宽计算下字面对齐 ≠ 视觉对齐（详 /memories/markdown-lint.md），表格仍按 docs-style §3 视觉对齐维护，机械 MD060 不予执行。 -->

# Inkwell Agent 平台 · 技术选型

> 本文档对应 [stages.md §5.5](../../../.he/docs/stages.md) 与 [agents/architect-advisor/AGENT.md §4.2 / §6](../../../.he/agents/architect-advisor/AGENT.md) 的"六字段"要求：每条选型必须给出 选择 / 为什么 / 替代方案 / 放弃理由 / 团队维护影响 / 成本性能安全交付影响。详细论证见 [adr/](./adr/) 各 ADR。本文是给评审者的"一页纸"摘要。

## 0. 选型摘要表（与 ADR-NNN 一一对应）

| 维度            | 选择                                                                   | 关联决策                                                                       | 置信度 |
| --------------- | ---------------------------------------------------------------------- | ------------------------------------------------------------------------------ | ------ |
| 客户端运行时    | Electron + React + Vite + TypeScript                                   | [ADR-001](./adr/ADR-001-client-runtime-electron-react.md)                      | high   |
| 后端运行时      | .NET 10 + ASP.NET Core                                                 | [ADR-002](./adr/ADR-002-backend-runtime-dotnet10-aspnetcore.md)                | high   |
| Agent 引擎      | Microsoft Agent Framework                                              | [ADR-003](./adr/ADR-003-agent-engine-microsoft-agent-framework.md)             | high   |
| 关系 + 向量数据 | IPersistenceProvider 抽象（SqlServer / PostgreSQL）+ Qdrant + InMemoryVectorStore | [ADR-004](./adr/ADR-004-data-store-provider-switchable-ef-core.md) + [ADR-020](./adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) + [ADR-021](./adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) | high   |
| 部署形态        | Compose (dev) / AKS (prod)                                             | [ADR-005](./adr/ADR-005-deployment-docker-compose-aks.md)                      | high   |
| 编排画布        | React Flow + MAF Workflows                                             | [ADR-006](./adr/ADR-006-orchestration-canvas-react-flow.md)                    | high   |
| 公开 API 鉴权   | 单 Token + Bearer                                                      | [ADR-007](./adr/ADR-007-public-api-token-auth.md)                              | high   |
| 多模态          | Azure Speech + 模型 vision                                             | [ADR-009](./adr/ADR-009-multimodal-azure-speech.md)                            | high   |
| Skill 加载      | v1 仅静态                                                              | [ADR-010](./adr/ADR-010-skill-loading-static-only-v1.md)                       | high   |
| 锁屏 + 在途任务 | 主进程长 SSE + 5 min idle                                              | [ADR-011](./adr/ADR-011-auto-lock-with-inflight-task-survival.md)              | medium |
| 客户端↔后端协议 | REST + AG-UI（无 cursor）                                              | [ADR-012](./adr/ADR-012-client-server-protocol-rest-agui.md)                   | high   |
| 可观测性        | OTel + Grafana 自托管                                                  | [ADR-013](./adr/ADR-013-observability-otel-self-hosted-grafana.md)             | high   |
| 国际化          | v1 仅 zh-CN                                                            | [ADR-014](./adr/ADR-014-i18n-out-of-scope-v1.md)                               | high   |
| 文件存储        | IFileStorageProvider 抽象（LocalFileSystem / AzureBlob / MinIO）       | [ADR-015](./adr/ADR-015-object-storage-provider-switchable.md)                 | medium |
| 缓存层          | ICacheProvider 抽象（Redis）                                           | [ADR-016](./adr/ADR-016-cache-provider-redis.md)                               | medium |
| 凭据存储        | K8s Secret + .env（v1，不走 Key Vault）                                | [OQ-A006 closed §B](./open-questions-arch.md) + [RISK-013](./risk-analysis.md) | medium |
| 测试与 CI       | MSTest.Sdk 4.x + Vitest + Playwright + GitHub Actions                       | [OQ-A007 closed §A](./open-questions-arch.md)                                  | high   |
| 后端模块拓扑    | Ports & Adapters（Abstractions / Core / providers/* / WebApi / Worker）           | [ADR-017](./adr/ADR-017-backend-module-topology-ports-and-adapters.md)         | high   |
| 后端队列抽象    | IQueueProvider + ChannelsQueueProvider（dev） + RedisStreamQueueProvider（integration / prod）双 Provider | [ADR-018](./adr/ADR-018-queue-abstraction-channels-default.md) / [OQ-A008 closed §B](./open-questions-arch.md) | high   |
| 后端进程拓扑    | Inkwell.WebApi + Inkwell.Worker 双进程（独立 Pod / 独立 HPA）           | [ADR-019](./adr/ADR-019-process-topology-webapi-worker-split.md) | high   |
| Entity ↔ Model mapper | 手写扩展方法（`Entity.ToModel()` / `Model.ToEntity()` / `SelectAsModel`），禁 AutoMapper / Mapperly / Mapster | [ADR-022](./adr/ADR-022-entity-domain-mapper-selection.md) | high   |

> 置信度统计：high 17 / medium 5 / low 0；low 占比 0%（[architect-advisor/prompt.md §第六步](../../../.he/agents/architect-advisor/prompt.md) 要求 ≤ 30%，达标）。

## 1. 客户端运行时（ADR-001）

- **选择**：Electron 38+ + React 19 + Vite 6 + TypeScript 5.x（最新 minor）；视觉风格参考 Ant Design Pro。
- **为什么**：跨 Win11 / macOS 12+；主进程能持有跨锁屏的 AG-UI 状态；麦克风 / 文件系统访问能力齐全；与 [ADR-012 AG-UI 客户端 SDK](./adr/ADR-012-client-server-protocol-rest-agui.md) 兼容。
- **替代方案**：Tauri 2.0 / 纯 PWA / 双端原生（Swift + WPF）。
- **放弃理由**：详见 [ADR-001 §备选项](./adr/ADR-001-client-runtime-electron-react.md)。
- **团队维护影响**：React + TS 是团队已有经验栈；Electron 主进程的 IPC 与生命周期管理是新增学习项，但社区资料丰富。
- **成本/性能/安全/交付**：成本中（包体 ≈ 90 MB）；性能高（Vite HMR < 1 s）；安全中（[contextIsolation](https://www.electronjs.org/docs/latest/tutorial/context-isolation) 必开）；交付高（v1 工期可控）。

## 2. 后端运行时（ADR-002）

- **选择**：.NET 10 + ASP.NET Core 10，C# 14（与 .NET 10 同步发布）。
- **为什么**：与 [ADR-003 Microsoft Agent Framework](./adr/ADR-003-agent-engine-microsoft-agent-framework.md) 同生态；ASP.NET Core 内置 SSE / Rate Limiter / Auth middleware；self-contained 镜像 ≈ 80 MB。
- **替代方案**：Python (FastAPI) + LangGraph / Node.js (NestJS) / Java (Spring Boot)。
- **放弃理由**：详见 [ADR-002 §备选项](./adr/ADR-002-backend-runtime-dotnet10-aspnetcore.md)。
- **团队维护影响**：团队 .NET 经验大于其他栈；nullable / pattern matching 学习曲线对新人有要求。
- **成本/性能/安全/交付**：成本低（容器镜像小，AKS 启动快）；性能高（async / `IAsyncEnumerable`）；安全高（内置 Auth / KeyVault 集成）；交付高。

## 3. Agent 引擎（ADR-003）

- **选择**：Microsoft Agent Framework（MAF）。
- **为什么**：与 [Q-A3](./open-questions-arch.md) 决策一致；内置 AG-UI 协议、Workflows、DurableTask、OTel instrumentation；与 [REQ-005 / REQ-007 / REQ-008 / REQ-010 / REQ-012 / REQ-014](../01-requirements/requirements.md) 多条直接对齐。
- **替代方案**：Semantic Kernel / LangChain / 自研。
- **放弃理由**：详见 [ADR-003 §备选项](./adr/ADR-003-agent-engine-microsoft-agent-framework.md)。
- **团队维护影响**：MAF 仍在演进，需要锁定具体版本 + 升级前跑全量 H4 用例。
- **成本/性能/安全/交付**：成本低（开源）；性能由模型主导；安全高（凭据通过 [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration) + KeyVault）；交付高。

## 4. 数据存储（ADR-004 + ADR-020 + ADR-021）

- **选择**：`IPersistenceProvider` 抽象 + EF Core 10 + 两 Provider 实现（SQL Server 2025 / PostgreSQL 17）采用 Code First + EF Migration；EFCore family 物理布局为 3 csproj（1 共享 base + 2 final adapter）— [ADR-021](./adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)；向量数据：复用 [Microsoft.Extensions.VectorData](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-vector-data) 抽象，prod = [Qdrant 1.x](https://qdrant.tech/) 独立服务 (`providers/Inkwell.VectorStore.Qdrant/`)，dev / unit test = `Inkwell.Core/VectorStore/InMemoryVectorStore`。
- **为什么**：与 [Q-A4](./open-questions-arch.md) 决策一致；关系层与向量层解耦避免 Provider 切换时的“最小公倍数”约束放大；Qdrant 在 Compose / AKS 部署成熟；v1 两 Provider 抽象与 [`IFileStorageProvider`](./adr/ADR-015-object-storage-provider-switchable.md) / [`ICacheProvider`](./adr/ADR-016-cache-provider-redis.md) 一同形成 Provider 家族模式；ADR-020 复用 M.E.VectorData 与 [ADR-003 MAF](./adr/ADR-003-agent-engine-microsoft-agent-framework.md) 生态对齐，避免重复发明轮子；ADR-021 在不重费 Migration tooling 的前提下，Entity / `OnModelCreating` / `EfCorePersistenceProvider` / DataSeed 集中在共享 base csproj，避免两 final adapter 各自依样实体带来的语义漂移。
- **替代方案**：两引擎都做向量 / 仅 PostgreSQL + pgvector / Azure AI Search；Inkwell 自定义 IVectorStore / 仅 Qdrant 不交付 InMemoryVectorStore；EFCore family csproj 布局备选 A-E 共 5 种（详见 [ADR-021 §备选项](./adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）。
- **放弃理由**：详见 [ADR-004 §备选项](./adr/ADR-004-data-store-provider-switchable-ef-core.md) + [ADR-020 §备选项](./adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) + [ADR-021 §备选项](./adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)。
- **团队维护影响**：SqlServer / Postgres Migration 迁移测试加 CI；Qdrant 是新组件，需要建立运维 runbook；Code First 与 EF Migration 是团队已有经验；M.E.VectorData attribute model 类似 EF Code First，零额外学习曲线；DataSeed 幂等实践（按业务唯一键判定而非 Id）需在 H3 InkwellSeeder HD 中给出范例。
- **成本/性能/安全/交付**：成本中（Qdrant 自托管 + EFCore family 3 csproj）；性能高（Qdrant gRPC 单查 < 50 ms；`EfCorePersistenceProvider` 唯一实现减少多份实现不一致风险）；安全中（向量库没有 row-level 权限，靠应用层）；交付中（SqlServer / Postgres 两 Provider Migration 协调 + InMemoryVectorStore 与 Qdrant 语义偏移走 [RISK-016](./risk-analysis.md) + EFCore family 幂等 / schema 漂移走 [RISK-017](./risk-analysis.md)）。

## 5. 部署形态（ADR-005）

- **选择**：dev = Docker Compose / prod = AKS（Helm）。
- **为什么**：与 [Q-A5](./open-questions-arch.md) 决策一致；dev 启动快 + 全栈本地复现；prod 与 Azure 生态（Speech / KeyVault / Blob）同 region。
- **替代方案**：dev 也用 AKS / prod 用 ACA / prod 用自建 K8s / Docker Swarm。
- **放弃理由**：详见 [ADR-005 §备选项](./adr/ADR-005-deployment-docker-compose-aks.md)。
- **团队维护影响**：Helm / AKS 是 Azure 上的主流栈，运维资料丰富。
- **成本/性能/安全/交付**：成本中；性能高；安全中（单 region 不具备跨区高可用）；交付高。

## 6. 编排画布（ADR-006）

- **选择**：React Flow（`@xyflow/react` 12+）+ Microsoft Agent Framework Workflows + DurableTask。
- **为什么**：与 [OQ-013 closed §A](../01-requirements/open-questions.md) 一致；前端只做画布交互，后端只做 DAG 执行，IR 是清晰契约边界；DurableTask 提供跨 Pod 重启续作。
- **替代方案**：自研 SVG / DSL-only / BPMN.js / LangGraph。
- **放弃理由**：详见 [ADR-006 §备选项](./adr/ADR-006-orchestration-canvas-react-flow.md)。
- **团队维护影响**：React Flow + dagre 自动布局是社区标准；MAF Workflows 是新学习项。
- **成本/性能/安全/交付**：成本低；性能中（300+ 节点流畅）；安全中；交付高。

## 7. 公开 API 鉴权（ADR-007）

- **选择**：单 Token + Bearer + ASP.NET Core 内置 RateLimiter middleware。
- **为什么**：与 [OQ-004 closed §A](../01-requirements/open-questions.md) 一致；实现路径短（≈ 100 行代码 + 内置组件）；Token 哈希存储。
- **替代方案**：多 Token + RBAC / Token + TTL / OAuth2 / mTLS。
- **放弃理由**：详见 [ADR-007 §备选项](./adr/ADR-007-public-api-token-auth.md)。
- **团队维护影响**：标准 ASP.NET Core middleware，无新依赖。
- **成本/性能/安全/交付**：成本低；性能高；安全中（无 RBAC / TTL，靠运维约束）；交付高。

## 8. ~~审计日志（ADR-008）~~（已移除）

> **2026-07-09 决策更新**：Owner 决定 v1 不做审计日志功能（详见 [requirements.md §13 第 14/23 条 2026-07-09 决策更新](../01-requirements/requirements.md)）。`ADR-008` 已删除；本节原内容（主 DB 表 + UI 检索、保留期 errata 等）不再适用，编号保留占位、不重排后续章节。

## 9. 多模态（ADR-009）

- **选择**：Azure Speech 后端 ASR + 模型 vision 处理图像 / PDF / Office。
- **为什么**：与 [OQ-003 closed §A](../01-requirements/open-questions.md) 一致；Azure Speech 在 zh-CN 上业界领先；客户端不持有凭据。
- **替代方案**：模型直接处理 audio / 双路并存 / 客户端 Whisper.cpp。
- **放弃理由**：详见 [ADR-009 §备选项](./adr/ADR-009-multimodal-azure-speech.md)。
- **团队维护影响**：Azure Speech SDK 标准，封装在 `Inkwell.Multimodal.Speech` 模块。
- **成本/性能/安全/交付**：成本中（按调用计费，需告警）；性能高（流式 ASR < 500 ms 首字符）；安全高；交付高。

## 10. Skill 加载（ADR-010）

- **选择**：v1 仅 Discovery + Activation；不实现 Execution；不预留 `ISkillExecutor` 接口。
- **为什么**：与 [Q-A7](./open-questions-arch.md) §A 一致；安全边界最强；不留接口债。
- **替代方案**：v1 静态 + 预留 Executor 接口 / v1 静态 + 命令白名单 / v1 直接做 Docker sandbox。
- **放弃理由**：详见 [ADR-010 §备选项](./adr/ADR-010-skill-loading-static-only-v1.md)。
- **团队维护影响**：仅 markdown 解析 + 字符串拼接，最简实现。
- **成本/性能/安全/交付**：成本低；性能高；安全高（不执行任何脚本）；交付高。

## 11. 锁屏 + 在途任务（ADR-011）

- **选择**：5 min idle 锁屏；Electron 主进程背后保持 SSE 订阅；UI 进程被 lock screen 覆盖；解锁后 UI 从主进程环缓重渲染。
- **为什么**：与 [NFR-003 + OQ-017 closed §A](../01-requirements/open-questions.md) + [OQ-A002 closed §A](./open-questions-arch.md) 一致；v1 范围控制下实现路径最短；不引入 cursor / RunEventStore / Run resume；DurableTask 仅负责任务本身跨 Pod 存活。
- **替代方案**：AG-UI Run resume + cursor + RunEventStore（§C） / AG-UI + SignalR 双协议（§B） / 锁屏即终止（§D）。
- **放弃理由**：详见 [ADR-011 §备选项](./adr/ADR-011-auto-lock-with-inflight-task-survival.md)。
- **团队维护影响**：主进程环缓区 + 重连逻辑 + 休眠检测是新增代码路径；不需事件持久化调优。
- **成本/性能/安全/交付**：成本低；性能高（SSE 原生流式）；安全中（主进程锁屏期间仍持连接，边界比 §C 模糊）；交付中（§A 取决于主进程休眠后重连可靠性，[RISK-007](./risk-analysis.md) 待 H4 验证）。

## 12. 客户端↔后端协议（ADR-012）

- **选择**：REST（CRUD）+ AG-UI Protocol over SSE（Run）；不引入 cursor / RunEventStore / Run resume；重连走 `GET /api/runs/{id}/state` 兑底 endpoint。
- **为什么**：与 [Q-A6 + OQ-A002 closed §A](./open-questions-arch.md) 一致；与 [microsoft/agent-framework hosting 包](../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.Hosting.AGUI.AspNetCore/) 一行注册。
- **替代方案**：REST + GraphQL / 纯 gRPC / REST + 自建 WebSocket / 纯 AG-UI。
- **放弃理由**：详见 [ADR-012 §备选项](./adr/ADR-012-client-server-protocol-rest-agui.md)。
- **团队维护影响**：客户端两套代码路径（REST + AG-UI），但都是社区标准 SDK；不重复造 cursor 轮子。
- **成本/性能/安全/交付**：成本低；性能高（SSE + 流式）；安全高（[ADR-007 单 Token](./adr/ADR-007-public-api-token-auth.md)）；交付高。

## 13. 可观测性（ADR-013）

- **选择**：OpenTelemetry .NET SDK → OTel Collector → Tempo / Loki / Prometheus → Grafana。
- **为什么**：与 [Q-A8 §A](./open-questions-arch.md) 一致；与 MAF 内置 OTel instrumentation 对齐；客户跨云可迁移。
- **替代方案**：Azure App Insights / 自建 ELK / Datadog / 仅文件日志。
- **放弃理由**：详见 [ADR-013 §备选项](./adr/ADR-013-observability-otel-self-hosted-grafana.md)。
- **团队维护影响**：自托管栈带来运维负担；通过 grafana/helm-charts + 默认告警缓解。
- **成本/性能/安全/交付**：成本中（自托管）；性能高（Loki / Tempo / Prometheus 都是水平扩展）；安全中；交付中（运维 runbook 待 H3 写）。

## 14. 国际化（ADR-014）

- **选择**：v1 仅 zh-CN；不引入 i18n 框架；UI 文案直接中文字面量。
- **为什么**：与 [OQ-015 closed §A](../01-requirements/open-questions.md) 一致；客户群在中文环境；范围控制理念一致。
- **替代方案**：双语 / 仅 zh-CN 但抽 i18n key / 不声明语言。
- **放弃理由**：详见 [ADR-014 §备选项](./adr/ADR-014-i18n-out-of-scope-v1.md)。
- **团队维护影响**：H5 工期不背负 i18n 抽 key 与翻译成本；v2 引入时遍历抽 key（[RISK-010](./risk-analysis.md)）。
- **成本/性能/安全/交付**：成本低；性能 N/A；安全 N/A；交付高。

## 15. 文件存储（ADR-015）

- **选择**：`IFileStorageProvider` 抽象 + 三 Provider 切换 `LocalFileSystem` / `AzureBlob` / `MinIO`，启动时按 `Inkwell:FileStorage:Provider` 选择。
- **为什么**：与 [OQ-A005 closed §D](./open-questions-arch.md) 一致；与 [`IPersistenceProvider`](./adr/ADR-004-data-store-provider-switchable-ef-core.md) + [`ICacheProvider`](./adr/ADR-016-cache-provider-redis.md) 同构；允许客户在 Azure / 自建 K8s / 本地环境任选 prod Provider，客户端代码零差异。
- **替代方案**：单一 Azure Blob / 单一 MinIO / 数据库 BLOB 字段 / 仅 LocalFileSystem + AzureBlob。
- **放弃理由**：详见 [ADR-015 §备选项](./adr/ADR-015-object-storage-provider-switchable.md)。
- **团队维护影响**：三 Provider contract test matrix；Helm Chart 三套 values；MinIO 运维 runbook 是新增技能项。
- **成本/性能/安全/交付**：成本低（AzureBlob managed）至中（自托管 MinIO）；性能高（预签名 URL 直传）；安全中（凭据走 K8s Secret，[OQ-A006 closed §B](./open-questions-arch.md)）；交付中（[RISK-011](./risk-analysis.md) contract 漏出待 H3 验证）。

## 16. 缓存层（ADR-016）

- **选择**：`ICacheProvider` 抽象 + Redis 8（dev 走本机容器 / prod 走 [Azure Cache for Redis](https://learn.microsoft.com/azure/azure-cache-for-redis/) 或自建 StatefulSet）；启动时按 `Inkwell:Cache:Provider` 选择 `InMemory` / `Redis`。
- **为什么**：与 [OQ-A004 closed §B](./open-questions-arch.md) 一致；与 [`IPersistenceProvider`](./adr/ADR-004-data-store-provider-switchable-ef-core.md) + [`IFileStorageProvider`](./adr/ADR-015-object-storage-provider-switchable.md) 同构；HPA min 2 副本场景下 Public API rate limit / Agent 会话状态 / 元数据缓存 都需分布式语义一致。
- **替代方案**：IMemoryCache + 内存 token bucket（§A） / IDistributedCache 不抽象为 Provider / Memcached / NCache / Hazelcast。
- **放弃理由**：详见 [ADR-016 §备选项](./adr/ADR-016-cache-provider-redis.md)。
- **团队维护影响**：Redis 是新运维项（dev Compose 一个容器 / prod 一个外部资源或 StatefulSet）；缓存键 invalidation 是新 contract，H3 需明确。
- **成本/性能/安全/交付**：成本低（Standard tier 起价较小）；性能高（同 VPC < 5 ms）；安全中（连接串 K8s Secret + TLS）；交付中（[RISK-012](./risk-analysis.md) 多副本一致性待 H3 验证）。

## 17. 凭据存储（OQ-A006 closed §B，无独立 ADR）

- **选择**：v1 仅 Kubernetes Secret（prod）+ Docker Compose `.env`（dev）；未引入 Azure Key Vault。
- **为什么**：与 [OQ-A006 closed §B](./open-questions-arch.md) 一致；与 [OQ-006 v1 范围控制](../01-requirements/open-questions.md) 思路一致；减少一个外部依赖。
- **替代方案**：Azure Key Vault + [Key Vault CSI driver](https://learn.microsoft.com/azure/aks/csi-secrets-store-driver)（§A） / HashiCorp Vault。
- **放弃理由**：§A 引入 Azure-specific 依赖与运维复杂度的 trade-off 被 Owner 评估为“v1 不值”；不过残余风险能够被 K8s RBAC + at-rest 加密上手接住。
- **团队维护影响**：.env 需严格 `.gitignore`；Secret 轮换需 Pod 重启（无 hot reload）；轮换 SOP 作为 H3 runbook。
- **成本/性能/安全/交付**：成本低；性能高（启动时加载，运行期零开销）；安全中（强依赖 RBAC + at-rest，轮换代价高，[RISK-013](./risk-analysis.md)）；交付高。

## 18. 测试与 CI（OQ-A007 closed §A）

- **选择**：后端 [MSTest.Sdk 4.x](https://github.com/microsoft/testfx)（最新稳定 4.2.2，默认使用 [`Microsoft.Testing.Platform`](https://learn.microsoft.com/dotnet/core/testing/unit-testing-platform-intro) / MTP runner；底层 `MSTest.TestFramework` 仍为 3.x、两者版本号错位是上游项目表现、与选型决策无关） + 前端 [Vitest](https://vitest.dev/) + E2E [Playwright](https://playwright.dev/) + CI [GitHub Actions](https://docs.github.com/actions)。
- **为什么**：与 [OQ-A007 closed §A](./open-questions-arch.md) 一致；MSTest.Sdk 4.x 是微软官方当前推荐的 .NET 测试 MSBuild SDK（[MSTest.Sdk 文档](https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest-sdk)），提供零配置项目模板 + 默认 MTP；Microsoft.Testing.Platform 冷启动明显优于 VSTest（[`dotnet test` 集成](https://learn.microsoft.com/dotnet/core/testing/unit-testing-with-dotnet-test)）；前端与 Vite 原生配套；Playwright 跨平台。
- **替代方案**：xUnit / NUnit（后端）；Jest（前端）；Cypress（E2E）；Azure DevOps（CI）。
- **放弃理由**：详见 [OQ-A007 候选说明](./open-questions-arch.md)；后端取 MSTest 微软系是 Owner 明确修正。
- **团队维护影响**：`MSTest.Sdk` 是 NuGet-provided MSBuild SDK（[已知限制](https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest-sdk#known-limitations)：Visual Studio NuGet UI / Dependabot 升级站不住，需手动同步修改 [`global.json`](https://learn.microsoft.com/dotnet/core/tools/global-json) `msbuild-sdks` 与 csproj `Sdk="MSTest.Sdk/<version>"`）；锁定版本随 .NET 10 同步发布；CI matrix 跑 SqlServer / Postgres 两 EFCore Provider + 三 FileStorage Provider（InMemory 不跑 contract test）。
- **成本/性能/安全/交付**：成本低；性能高（MTP 冷启动提速）；安全 N/A；交付高。

## 19. 后端模块拓扑（ADR-017 + ADR-019）

- **选择**：Ports & Adapters 拓扑。物理为 `Inkwell.Abstractions`（接口 + Model + Options + Builder DSL） + `Inkwell.Core`（业务实现 + 默认 Provider + AgentRuntime） + `providers/*` 七个独立 csproj + `Inkwell.WebApi`（ASP.NET Core HTTP 入口；enqueue 侧） + `Inkwell.Worker`（.NET Generic Host + BackgroundService；队列 consumer + DurableTask runner）。共 11 csproj。文件路径 `src/server/` → `src/core/`、`apps/desktop/` → `src/app/`。
- **为什么**（1）三 Provider 抽象家族（[`IPersistenceProvider`](./adr/ADR-004-data-store-provider-switchable-ef-core.md) / [`IFileStorageProvider`](./adr/ADR-015-object-storage-provider-switchable.md) / [`ICacheProvider`](./adr/ADR-016-cache-provider-redis.md)）在 `Inkwell.Abstractions` 集中，contract 公共用例包 `tests/core/providers/Inkwell.Providers.Contract/` 是 [RISK-002](./risk-analysis.md) / [RISK-011](./risk-analysis.md) / [RISK-012](./risk-analysis.md) 缓解动作的统一落地点；（2）Builder DSL `AddInkwell().UseSqlServer()...Build()` 与 [Microsoft Agent Framework](../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI/) `AgentApplicationBuilder` 风格一致；（3）客户运行时只装需要的 Provider 包；（4）[ADR-019](./adr/ADR-019-process-topology-webapi-worker-split.md) 拆 WebApi / Worker 双进程是为了让请求处理与队列 consumer + DurableTask runner 独立扩缩，HPA 用不同 metric（CPU vs queue_depth）。
- **替代方案**：维持 capability-folder（AGENTS.md §3.1 原状 16 个 csproj） / 极简 monolith（单 csproj） / 微服务 / `Inkwell.Host` 单进程（ADR-019 备选 B：节省 1 csproj 但失去独立扩缩）。
- **放弃理由**：details 在 [ADR-017 §备选项](./adr/ADR-017-backend-module-topology-ports-and-adapters.md) + [ADR-019 §备选项](./adr/ADR-019-process-topology-webapi-worker-split.md)。汇总：capability-folder 破坏三 Provider DRY + Builder DSL 难收拢；极简 monolith 强依赖 Provider NuGet 不可裁剪；微服务与 v1 用户量级（[NFR-001](../01-requirements/requirements.md)）不匹配；单进程导致 HTTP 与 consumer 抢 CPU + 扩缩 metric 冲突。
- **团队维护影响**：init 阶段需一次性应用 [§ADR-017 迁移路径](./adr/ADR-017-backend-module-topology-ports-and-adapters.md) + [§ADR-019 迁移路径](./adr/ADR-019-process-topology-webapi-worker-split.md) 中合计 16 步；之后 new joiner 看 `Inkwell.Abstractions` 一个 csproj 即可掌握 contract，capability-folder 下需读 16 个 README。WebApi / Worker 必须**同 image tag 单 Helm release 同步滚**（[RISK-015](./risk-analysis.md)）。
- **成本/性能/安全/交付**：成本中（csproj 数 16 → 11，编译图更简，但增加 1 个 Pod）；性能优（独立扩缩 + HPA 不同 metric）；安全中（[RISK-001](./risk-analysis.md) MAF 隔离从 csproj 硬边界降级为 lint + 接口收敛软边界，需重写缓解方案；[RISK-015](./risk-analysis.md) 双进程版本漂移 + OTel 双 source 风险）；交付中（一次性重构代价 + WebApi/Worker 双 Program.cs DI 装配重复）。

## 20. 后端队列抽象（ADR-018）

- **选择**：`IQueueProvider` 接口位于 `Inkwell.Abstractions`；**环境对称双 Provider**——`ChannelsQueueProvider`（基于 [`System.Threading.Channels`](https://learn.microsoft.com/dotnet/core/extensions/channels)，in-process）位于 `Inkwell.Core`作为 dev / unit test 默认；`RedisStreamQueueProvider`（基于 [Redis 8 Streams](https://redis.io/docs/latest/develop/data-types/streams/) consumer group）位于 `providers/Inkwell.Queue.Redis/` 独立 csproj作为 integration test / prod 默认。锁定默认：DLQ N=3 + 24h 保留；Redis Streams 内置语义（[`XREADGROUP`](https://redis.io/docs/latest/commands/xreadgroup/) / [`XCLAIM`](https://redis.io/docs/latest/commands/xclaim/) visibility timeout = 5 min / 指数退避 1s/max 60s）；observability v1 必发 `queue_depth`。
- **为什么**：（1）**环境对称原则**与其他三 Provider 家族（[`IPersistenceProvider`](./adr/ADR-004-data-store-provider-switchable-ef-core.md) / [`ICacheProvider`](./adr/ADR-016-cache-provider-redis.md) / [`IFileStorageProvider`](./adr/ADR-015-object-storage-provider-switchable.md)）一致，避免「开发期靠 in-process 跳过可靠性设计、上线才发现多副本抢同一任务」环境偏移 bug；（2）[Redis Streams](https://redis.io/docs/latest/develop/data-types/streams/) consumer group + XCLAIM 是业界成熟实践（[Sidekiq](https://github.com/sidekiq/sidekiq) / [Bull](https://github.com/OptimalBits/bull) 等众多实现都遵此拓扑），v1 不需自创可靠性抽象；（3）Owner 立场与语义一致。
- **替代方案**：（A）不引入 `IQueueProvider`，维持 [`System.Threading.Channels`](https://learn.microsoft.com/dotnet/core/extensions/channels) + [`Microsoft.Agents.AI.DurableTask`](../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.DurableTask/) 现状；（B）`IQueueProvider` 接口 + 仅 `ChannelsQueueProvider`，Redis 实现挂 OQ-A008 推迟；（D）`IQueueProvider` 包装 DurableTask 作为 thin wrapper。
- **放弃理由**：详见 [ADR-018 §备选项](./adr/ADR-018-queue-abstraction-channels-default.md)。汇总：A 与 Owner 环境对称论据冲突；B 推迟 Redis 到 OQ 会造成 H3 期间业务代码隐含「单进程」偏见；D 是 Adapter 装饰无业务价值 + 丢失 DurableTask 的 sub-orchestration / replay 能力。
- **团队维护影响**：v1 交付 `RedisStreamQueueProvider` thin wrapper，[Inkwell.Queue.Redis](./adr/ADR-017-backend-module-topology-ports-and-adapters.md) 的 H3 HD 必须决「Redis 实例是否与 [Inkwell.Cache.Redis](./adr/ADR-016-cache-provider-redis.md) 复用」（建议独立）；observability 五项补齐项走 prod ProdReady checklist。
- **成本/性能/安全/交付**：成本中（多 1 csproj + 可能多 1 Redis 实例）；性能中（开发态与现状等价，prod 是 Redis 跨进程 RTT）；安全依赖 [ADR-007](./adr/ADR-007-public-api-token-auth.md) Token 不变；交付中（H3 同期交付 [Inkwell.Queue.Redis](./adr/ADR-017-backend-module-topology-ports-and-adapters.md) HD）。

## 21. 备选项打分表（关键维度）

> 评分：✅ 优 / ◎ 良 / ⚠ 一般 / ❌ 差。打分仅在"该项有真实候选"的维度评分。

### 21.1 客户端运行时

| 维度       | Electron+React | Tauri+Rust | 纯 PWA |
| ---------- | -------------- | ---------- | ------ |
| 满足 REQ   | ✅              | ◎          | ⚠      |
| 团队维护   | ✅              | ❌          | ✅      |
| 跨锁屏能力 | ✅              | ◎          | ❌      |
| 包体大小   | ⚠              | ✅          | ✅      |

### 21.2 后端运行时

| 维度         | .NET 10 | Python FastAPI | Node NestJS |
| ------------ | ------- | -------------- | ----------- |
| 与 MAF 适配  | ✅       | ⚠              | ❌           |
| 团队维护     | ✅       | ◎              | ◎           |
| 流式 / async | ✅       | ✅              | ◎           |
| 镜像大小     | ✅       | ◎              | ◎           |

### 21.3 数据存储

| 维度         | IPersistenceProvider + Qdrant | 仅 PostgreSQL + pgvector | Azure AI Search |
| ------------ | ----------------------------- | ------------------------ | --------------- |
| 满足 Q-A4    | ✅                             | ❌                        | ❌               |
| dev 体验     | ✅                             | ✅                        | ❌               |
| 向量性能     | ✅                             | ◎                        | ✅               |
| 切换抽象漏出 | ⚠                             | ✅                        | N/A             |

### 21.4 协议

| 维度         | REST + AG-UI | REST + GraphQL | 纯 gRPC | REST + 自建 WebSocket |
| ------------ | ------------ | -------------- | ------- | --------------------- |
| 与 MAF 对齐  | ✅            | ❌              | ⚠       | ❌                     |
| 跨锁屏续传   | ✅            | ⚠              | ⚠       | ◎                     |
| 浏览器友好   | ✅            | ✅              | ⚠       | ✅                     |
| OpenAPI 文档 | ✅            | N/A            | ⚠       | ✅                     |

### 21.5 可观测性

| 维度         | OTel + Grafana | Azure App Insights | ELK | Datadog |
| ------------ | -------------- | ------------------ | --- | ------- |
| 跨云迁移     | ✅              | ❌                  | ✅   | ⚠       |
| dev 本地全栈 | ✅              | ❌                  | ◎   | ❌       |
| 运维成本     | ⚠              | ✅                  | ❌   | ✅       |
| Trace 能力   | ✅              | ✅                  | ⚠   | ✅       |

### 21.6 文件存储

| 维度         | 三 Provider 切换 | 单一 AzureBlob | 单一 MinIO | DB BLOB |
| ------------ | ---------------- | -------------- | ---------- | ------- |
| 满足 OQ-A005 | ✅                | ⚠              | ⚠          | ❌       |
| dev 零依赖   | ✅                | ⚠              | ⚠          | ✅       |
| 跨云可移植   | ✅                | ❌              | ✅          | N/A     |
| 运维复杂度   | ⚠                | ✅              | ⚠          | ✅       |

### 21.7 缓存层

| 维度          | ICacheProvider + Redis | IMemoryCache | IDistributedCache | Memcached |
| ------------- | ---------------------- | ------------ | ----------------- | --------- |
| 多副本一致性  | ✅                      | ❌            | ✅                 | ◎         |
| Provider 抽象 | ✅                      | N/A          | ⚠                 | ⚠         |
| 运维资料      | ✅                      | N/A          | ✅                 | ◎         |
| .NET 生态     | ✅                      | ✅            | ✅                 | ⚠         |

### 21.8 凭据存储

| 维度          | K8s Secret + .env | Azure Key Vault CSI | HashiCorp Vault |
| ------------- | ----------------- | ------------------- | --------------- |
| v1 安装复杂度 | ✅                 | ⚠                   | ❌               |
| 加密边界      | ⚠                 | ✅                   | ✅               |
| 轮换体验      | ⚠                 | ✅                   | ◎               |
| Azure 生态    | ✅                 | ✅                   | ⚠               |

### 21.9 后端模块拓扑

| 维度             | Ports & Adapters（本决） | capability-folder（原§3.1） | 极简 monolith | 微服务 |
| ---------------- | ------------------------ | --------------------------- | ------------- | ------ |
| 三 Provider DRY  | ✅                        | ⚠                           | ✅             | ⚠      |
| Builder DSL 收敛 | ✅                        | ❌                           | ✅             | ❌      |
| MAF 隔离边界     | ◎（lint 软边界）         | ✅（csproj 硬边界）          | ❌             | ✅      |
| csproj 数量      | 9                        | 16+                         | 1             | N/A    |
| 客户裁剪         | ✅                        | ◎                           | ❌             | ✅      |

### 21.10 后端队列抽象

| 维度                | Channels（dev） + Redis Stream（prod）双 Provider（本决） | 不引入抽象（原§3.3） | 仅 Channels，Redis 推迟 OQ | 包装 DurableTask thin wrapper |
| ------------------- | ----------------------------------------------- | ------------------- | ----------------------------- | ------------------------------ |
| Owner 环境对称论据 | ✅                                              | ❌                  | ⚠（推迟）                       | ❌                             |
| dev 零依赖           | ✅（Channels）                                  | ✅                  | ✅                            | ✅                             |
| prod 多副本语义      | ✅（Redis Streams）                              | ⚠（DurableTask 仅覆盖工作流） | ⚠（升级后才有）                | ⚠                              |
| ADR 六字段可填齐  | ✅                                              | ✅                  | ✅                            | ⚠                              |
| H4 补鱼类用例      | crash recovery / fairness / DLQ                 | 无                  | 仅 in-process                  | 无                             |

### 21.11 后端进程拓扑（ADR-019）

| 维度              | WebApi + Worker 双进程（本决） | 同进程 IHostedService | dev B + prod A | 多 worker 类型拆分 |
| ----------------- | ------------------------------- | --------------------- | -------------- | ------------------ |
| 故障隔离           | ✅                              | ❌                    | ⚠              | ✅                  |
| 独立扩缩（queue_depth vs CPU）| ✅                              | ❌                    | ⚠              | ✅                  |
| 环境对称           | ✅                              | ✅                    | ❌              | ✅                  |
| v1 交付代价        | ⚠（+1 csproj +1 Deployment）    | ✅                    | ⚠（双拓扑）     | ❌（+多 Deployment）  |
| Helm 同 image tag | ✅                              | N/A                   | ❌              | ⚠                  |

## 22. 自检

- 22 项选型× 6 字段 = 132 字段，全部填写（凭据存储 / 测试与 CI 为无独立 ADR 选型，仅依附 OQ）。ADR-020 / ADR-021 / ADR-022 作为 ADR-004 谱系的精化 refinement，不重复打分但计在 ADR 总数中。
- 置信度统计：high 18 / medium 4 / low 0；low 占比 0% ≤ 30%（[architect-advisor/prompt.md §第六步](../../../.he/agents/architect-advisor/prompt.md) 阈值通过）。
- 与 [open-questions-arch.md](./open-questions-arch.md) 关联完整：OQ-A001 ~ OQ-A008 均 closed。
- 与 22 ADR 一一对应（ADR-001 ~ ADR-022）。
