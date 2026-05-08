---
id: architecture-inkwell-agent-platform
stage: H2
status: reviewed
authors:
  - name: H2-ArchitectAdvisor
    role: agent
reviewers: [ Inkwell ]
created: 2026-05-08
updated: 2026-05-08
upstream:
  - REQ-inkwell-agent-platform
  - repo-impact-map-inkwell-agent-platform
  - tech-selection-inkwell-agent-platform
  - risk-analysis-inkwell-agent-platform
downstream: []
---

# Inkwell Agent 平台 · 架构说明

> 本文档对应 [stages.md §5.4](../../../.he/docs/stages.md) 必填章节：总体架构图 / 前端 / 后端 / 数据库 / 缓存 / 消息 / 鉴权 / 文件存储 / 部署 / 可观测性 / 性能目标 / 扩展性 / 安全 / 主要技术风险 / 替代方案比较。详细论证见 [adr/](./adr/) 与 [tech-selection.md](./tech-selection.md) / [risk-analysis.md](./risk-analysis.md)；本文是把它们串成一张架构地图。

## 0. 输入与边界

- **输入**：[REQ-001 ~ REQ-017](../01-requirements/requirements.md) + [NFR-001 ~ NFR-006](../01-requirements/requirements.md) + [UI-001 ~ UI-010](../01-requirements/ui-spec.md) + [UF-001 ~ UF-010](../01-requirements/user-flow.md) + [EX-001 ~ EX-008](../01-requirements/requirements.md) + [repo-impact-map](../01-requirements/repo-impact-map.md) §3 / §6。
- **范围裁剪签字**：[OQ-006 closed §A](../01-requirements/open-questions.md) — v1 范围已固化。
- **不在 v1 范围**：多 region active-passive、客户端离线模式、客户端本地 ASR、TTS、RBAC、多租户隔离、Skill Execution、双语 UI。

## 1. 总体架构图

```text
┌──────────────────────────────────────────────────────────────────────┐
│ Electron 客户端  (ADR-001)                                            │
│  ┌──────────────────────────────────┐  ┌─────────────────────────┐   │
│  │ Renderer (React + Vite + TS)     │  │ Main (Node.js)          │   │
│  │  · UI-001~010 (Pro Layout)       │  │  · idle / lock 调度     │   │
│  │  · React Flow 画布 (ADR-006)     │  │  · powerMonitor / blur  │   │
│  │  · AG-UI 客户端 SDK (ADR-012)    │  │  · cursor / run_id 持有 │   │
│  │  · MediaRecorder 麦克风录音      │  │  · 自动更新             │   │
│  └──────────────┬───────────────────┘  └─────────┬───────────────┘   │
└──────────────────┼──────────── IPC contextBridge ┼────────────────────┘
                   │ HTTPS REST + SSE              │
                   ▼                               │
┌──────────────────────────────────────────────────┴────────────────────┐
│ ASP.NET Core 后端  (ADR-002, .NET 10)                                  │
│  ┌────────────────────┐  ┌────────────────────┐  ┌────────────────┐   │
│  │ Inkwell.Api        │  │ Inkwell.AGUI       │  │ Inkwell.Public │   │
│  │ (REST CRUD)        │  │ (Hosting + Resume) │  │ (Token API)    │   │
│  └─────────┬──────────┘  └─────────┬──────────┘  └────┬───────────┘   │
│            │                       │                  │               │
│  ┌─────────▼───────────────────────▼──────────────────▼───────────┐   │
│  │ Inkwell.AgentRuntime (ADR-003 MAF 门面层)                       │   │
│  │  · Microsoft.Agents.AI / AGUI / Workflows / DurableTask         │   │
│  └────┬───────────┬───────────┬──────────┬─────────┬───────────────┘   │
│       │           │           │          │         │                   │
│  ┌────▼────┐ ┌────▼────┐ ┌────▼─────┐ ┌──▼────┐ ┌──▼─────────────┐    │
│  │Conver-  │ │Skills   │ │Knowledge │ │Multi- │ │Orchestrations  │    │
│  │sations  │ │(static) │ │Base+RAG  │ │modal  │ │(WorkflowComp.) │    │
│  └────┬────┘ └────┬────┘ └────┬─────┘ └──┬────┘ └──┬─────────────┘    │
│       │           │           │          │         │                   │
│  ┌────▼───────────▼───────────▼──────────▼─────────▼───────────────┐  │
│  │ Inkwell.DataAccess (ADR-004)  · IAuditLogger · IVectorStore      │  │
│  └────────────┬─────────────────────────┬──────────────────────────┘  │
└───────────────┼─────────────────────────┼─────────────────────────────┘
                │                         │
                ▼                         ▼
        ┌──────────────┐          ┌────────────────┐
        │ EF Provider  │          │  Qdrant        │
        │  · InMemory  │          │  (向量库)       │
        │  · SQL Server│          └────────────────┘
        │  · PostgreSQL│
        └──────────────┘

        ┌──────────────────────────────────────────────────┐
        │ 横切：OTel + Grafana 栈 (ADR-013)                │
        │  Tempo (trace) / Loki (log) / Prometheus (metric)│
        └──────────────────────────────────────────────────┘

        ┌──────────────────────────────────────────────────┐
        │ Azure 依赖：Speech (ADR-009) · Blob · Key Vault   │
        └──────────────────────────────────────────────────┘
```

## 2. 前端架构

详见 [ADR-001](./adr/ADR-001-client-runtime-electron-react.md)。

- **Renderer 层**：React 19 + Vite 6 + TypeScript 5.x（最新 minor）；Pro Layout 风格；状态管理使用 [Zustand](https://github.com/pmndrs/zustand)（轻量，避开 Redux 模板代码）+ React Query（数据获取与缓存）。
- **画布层**：[React Flow](https://reactflow.dev/)（`@xyflow/react` 12+）+ 自定义 NodeTypes / EdgeTypes（[ADR-006](./adr/ADR-006-orchestration-canvas-react-flow.md)）。
- **流式层**：[`@ag-ui/client`](https://github.com/ag-ui-protocol) 客户端 SDK 接收 SSE 事件 → Zustand store → 渲染 [UI-002 聊天](../01-requirements/ui-spec.md) / [UI-007 调试](../01-requirements/ui-spec.md)。
- **Main 层**：负责 idle 监听 / lock 调度（[ADR-011](./adr/ADR-011-auto-lock-with-inflight-task-survival.md)）/ 麦克风权限 / 自动更新（[electron-updater](https://www.electron.build/auto-update)）；通过 `preload.ts` + `contextBridge` 向 Renderer 暴露受控 API。
- **构建**：Vite 单工具链；自动打包 Win / macOS arm64 + macOS x86_64。

## 3. 后端架构

详见 [ADR-002](./adr/ADR-002-backend-runtime-dotnet10-aspnetcore.md) + [ADR-003](./adr/ADR-003-agent-engine-microsoft-agent-framework.md)。

- **应用层**：[Inkwell.Api](../01-requirements/repo-impact-map.md) (REST CRUD) + [Inkwell.AGUI.Hosting](../01-requirements/repo-impact-map.md) (流式) + [Inkwell.Public](../01-requirements/repo-impact-map.md) (Token API)。
- **领域层**：`Inkwell.Agents` / `Inkwell.Skills` / `Inkwell.KnowledgeBase` / `Inkwell.Multimodal` / `Inkwell.Orchestrations` / `Inkwell.Conversations`（详见 [§3.1 repo-impact-map](../01-requirements/repo-impact-map.md)）。
- 使用CodeFirst，EF Migration
- 关于PersistenceProvider，先使用EFCore实现一个抽象层，然后再实现InMemory、SQL Server、PostgresSQL，说白了这三个就是不同类型的DbContext去兼容
- **运行时门面**：[Inkwell.AgentRuntime](../01-requirements/repo-impact-map.md) 封装 MAF 公共 API（[Microsoft.Agents.AI](../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI/) / [.AGUI](../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.AGUI/) / [.Workflows](../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.Workflows/) / [.DurableTask](../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.DurableTask/)），其他模块只引用门面接口。
- **数据访问层**：[Inkwell.DataAccess](../01-requirements/repo-impact-map.md) 提供 `InkwellDbContext` + `IVectorStore` + `IAuditLogger` 三个接口。
- **横切**：`Inkwell.Common`（Result / Error 模型）/ `Inkwell.Common.Telemetry`（OTel 注册）/ `Inkwell.Common.Auth`（鉴权 middleware）。

## 4. 数据库选型

详见 [ADR-004](./adr/ADR-004-data-store-provider-switchable-ef-core.md)。

- **关系数据**：EF Core 10（与 .NET 10 同步发布）+ Provider 切换（InMemory / SQL Server 2025 / PostgreSQL 17），通过 `appsettings.json` 的 `Inkwell:DataStore:Provider` 字段选择。
- **向量数据**：[Qdrant 1.x](https://qdrant.tech/)，gRPC SDK，封装在 `Inkwell.DataAccess.VectorStore`。
- **关键表**：
  - `agents` / `agent_versions`（[REQ-002](../01-requirements/requirements.md) + [REQ-012 §3 版本锁定](../01-requirements/requirements.md)）
  - `skills`（[ADR-010](./adr/ADR-010-skill-loading-static-only-v1.md)）
  - `knowledge_bases` / `kb_documents` / `kb_chunks`（[REQ-009](../01-requirements/requirements.md)）
  - `conversations` / `messages`（[REQ-006](../01-requirements/requirements.md) + [NFR-005](../01-requirements/requirements.md)）
  - `orchestration_graphs` / `orchestration_runs`（[REQ-012](../01-requirements/requirements.md)）
  - `agui_run_events`（[ADR-011](./adr/ADR-011-auto-lock-with-inflight-task-survival.md) + [ADR-012](./adr/ADR-012-client-server-protocol-rest-agui.md)）
  - `audit_logs`（[ADR-008](./adr/ADR-008-audit-log-store-and-query.md)）
  - `public_api_tokens`（[ADR-007](./adr/ADR-007-public-api-token-auth.md)）
- **Migration**：每 Provider 一份 `Migrations/<Provider>/`；CI 跑三套（[RISK-002](./risk-analysis.md)）。

## 5. 缓存策略

详见 [ADR-016 ICacheProvider + Redis](./adr/ADR-016-cache-provider-redis.md)。

- **抽象**：后端业务代码仅依赖 [`Inkwell.Cache.ICacheProvider`](../01-requirements/repo-impact-map.md)（Get / Set / Remove / Exists / Increment / TryAcquireLock）；不直接 `using StackExchange.Redis`。
- **实现**：v1 仅 `RedisCacheProvider`（[StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/)）；`InMemoryCacheProvider` 仅用于单元测试。
- **部署**：dev = 本机 [redis:8](https://hub.docker.com/_/redis) 容器 / prod = [Azure Cache for Redis](https://learn.microsoft.com/azure/azure-cache-for-redis/) Standard 或 Premium tier / 自建场景 = `redis` StatefulSet + PVC。
- **使用场景**：
  - **Public API rate limit**（[ADR-007](./adr/ADR-007-public-api-token-auth.md)）：[`Microsoft.AspNetCore.RateLimiting`](https://learn.microsoft.com/aspnet/core/performance/rate-limit) middleware 后端接 Redis token bucket，在 HPA min 2 副本场景下仍保证“租户 60 req/min”语义一致。
  - **Agent 会话短期状态**：`AgentThread` 当前对话窗口的最近 N 条消息缓存，避免每次 Run 调用 [`IPersistenceProvider`](./adr/ADR-004-data-store-provider-switchable-ef-core.md)。
  - **元数据缓存**：Skill registry / Agent 配置 / 模型 Provider 列表（TTL 5 min）。
  - **模型 response cache**：默认关闭，H3 详细设计明确“安全可缓存的模型调用集合”后再启用。
- **限流**：[`PartitionedRateLimiter`](https://learn.microsoft.com/dotnet/api/system.threading.ratelimiting.partitionedratelimiter) 后端从内存 token bucket 切换为 Redis 分布式 token bucket。
- **Key 命名约定**：`{tenant}:{module}:{purpose}:{id}`。
- **不在 v1 范围**：Redis Cluster / Sentinel HA / Lua 脚本式分布式锁（v1 用 `SET NX EX` 简单锁）。

## 6. 消息机制

- **客户端↔后端**：REST + AG-UI Protocol over SSE（[ADR-012](./adr/ADR-012-client-server-protocol-rest-agui.md)）；不持久化 AG-UI 事件流，跨锁屏体验由客户端主进程长 SSE 保证（[ADR-011](./adr/ADR-011-auto-lock-with-inflight-task-survival.md)）。
- **后端内异步**：[`System.Threading.Channels`](https://learn.microsoft.com/dotnet/core/extensions/channels) 用于流式 token 输出与后端内跨组件传递；不再用于 RunEventStore 批量写入（已在 [OQ-A002 closed §A](./open-questions-arch.md) 中取消）。
- **后端持久工作流**：[Microsoft.Agents.AI.DurableTask](../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.DurableTask/)（[ADR-006](./adr/ADR-006-orchestration-canvas-react-flow.md)）；跨 Pod 重启续作。
- **v1 不引入消息队列**（RabbitMQ / Service Bus）：避免新增组件，等 v2 性能瓶颈出现再评估。

## 7. 鉴权与权限模型

- **客户端会话**：v1 单用户 + 启动时密码登录 + cookie session（[NFR-003](../01-requirements/requirements.md) 锁屏共用同一会话）。
- **公开 API**：单 Token + Bearer scheme（[ADR-007](./adr/ADR-007-public-api-token-auth.md)）；Rate limit token bucket 在 [`ICacheProvider`](./adr/ADR-016-cache-provider-redis.md) 上分布式均平。
- **凭据保护**：Azure Speech / Azure OpenAI / SMTP / Redis / DB 等凭据都走 [Kubernetes Secret](https://kubernetes.io/docs/concepts/configuration/secret/)（prod）/ [Docker Compose `.env`](https://docs.docker.com/compose/environment-variables/set-environment-variables/)（dev）（[OQ-A006 closed §B](./open-questions-arch.md)）；**v1 不引入 Azure Key Vault**，该决策的残余风险走 [RISK-013](./risk-analysis.md)。
- **不在 v1 范围**：RBAC / 多租户 / OAuth2 / SSO。

## 8. 文件存储方案

详见 [ADR-015](./adr/ADR-015-object-storage-provider-switchable.md) + [OQ-A005 closed §D](./open-questions-arch.md)。

- **抽象**：后端接口 [`IFileStorageProvider`](../01-requirements/repo-impact-map.md)（Upload / Download / Delete / Exists / CreatePresignedUploadUrl / CreatePresignedDownloadUrl / List），业务代码只依赖接口。
- **三 Provider 切换**（与 [ADR-004 IPersistenceProvider](./adr/ADR-004-data-store-provider-switchable-ef-core.md) + [ADR-016 ICacheProvider](./adr/ADR-016-cache-provider-redis.md) 同构）：
  - **`LocalFileSystem`**：写本地磁盘（`./data/objects/`），预签名由后端中转模拟。适用：单元测试 / 单体部署 / 离线场景。
  - **`AzureBlob`**：[Azure Blob Storage](https://learn.microsoft.com/azure/storage/blobs/) + [SAS URL](https://learn.microsoft.com/azure/storage/common/storage-sas-overview)。适用：Azure 客户的 prod 环境；dev 可选接 [Azurite emulator](https://learn.microsoft.com/azure/storage/common/storage-use-azurite)。
  - **`MinIO`**：[MinIO](https://min.io/)（S3 兼容） + S3 预签名 URL。适用：dev 默认 / 自建 K8s prod / 不入 Azure 的客户。
- **启动选择**：`appsettings.json` 的 `Inkwell:FileStorage:Provider` 字段（`LocalFileSystem` / `AzureBlob` / `MinIO`）；Helm values 在 prod 切换。
- **容器命名**：`uploads/` / `kb-source/` / `kb-extracted/` / `audit-export/`（v2 启用）。在 `MinIO` 上映射为 bucket；在 `LocalFileSystem` 上为子目录。
- **客户端上传**：三 Provider 都是"后端发预签名 URL → 客户端直传"模式，客户端代码零差异。
- **多模态文件**（[ADR-009](./adr/ADR-009-multimodal-azure-speech.md)）：上传后后端生成预签名下载 URL 作为多模态消息的 image_url / file 元素传给 Azure OpenAI（仅当模型支持 vision）。

## 9. 部署方式

详见 [ADR-005](./adr/ADR-005-deployment-docker-compose-aks.md)。

### 9.1 dev 部署（Docker Compose）

```text
docker compose up -d
  ├─ api            (Inkwell.Api + AGUI + Public)
  ├─ postgres       (PostgreSQL 17, named volume)
  ├─ qdrant         (Qdrant 1.x, named volume)
  ├─ redis          (Redis 8, ICacheProvider 后端, ADR-016)
  ├─ minio          (S3 兼容文件存储 dev 默认 Provider, ADR-015)
  ├─ otel-collector (OTLP receiver)
  ├─ tempo
  ├─ loki
  ├─ prometheus
  └─ grafana        (默认 dashboard 已加载)
```

### 9.2 prod 部署（AKS + Helm）

```text
helm install inkwell ./charts/inkwell --namespace inkwell-prod
  ├─ Deployment: api (HPA: CPU 70%, min 2, max 10)
  ├─ StatefulSet: postgres (Azure Disk Premium, 100GiB)
  ├─ StatefulSet: qdrant (Azure Disk Premium, 50GiB)
  ├─ Cache: Azure Cache for Redis Standard (默认) 或 StatefulSet: redis (自建场景, ADR-016)
  ├─ FileStorage: AzureBlob (默认) 或 StatefulSet: minio (自建场景, ADR-015)
  ├─ Deployment: otel-collector
  ├─ Stack (Helm subchart): grafana / tempo / loki / prometheus
  ├─ Ingress: NGINX + cert-manager (Let's Encrypt)
  └─ Secrets: Kubernetes Secret + RBAC + at-rest 加密（OQ-A006 §B，未引入 Key Vault，残余风险 RISK-013）
```

- **CI/CD**：GitHub Actions（[OQ-A007 closed §A](./open-questions-arch.md)）→ build → ACR push → Helm deploy。后端仓库走 [MSTest v3](https://github.com/microsoft/testfx)（`MSTest.Sdk` + Microsoft.Testing.Platform）；前端 Vitest；E2E Playwright。
- **region**：v1 单 region；多 region 列入 v2（[RISK-004](./risk-analysis.md)）。

## 10. 可观测性方案

详见 [ADR-013](./adr/ADR-013-observability-otel-self-hosted-grafana.md)。

- **三件套**：Trace (Tempo) / Log (Loki) / Metric (Prometheus) → Grafana 统一 UI。
- **应用 instrumentation**：[OpenTelemetry .NET SDK](https://opentelemetry.io/docs/languages/dotnet/) + MAF 内置 instrumentation（覆盖 LLM / 工具调用 / Skill / Workflow 节点）。
- **关键指标**：
  - `inkwell_runs_total` / `_errors_total`（Run 数与错误率）
  - `inkwell_run_duration_seconds`（P50 / P95 / P99）
  - `inkwell_tool_calls_total`（按 tool_id label）
  - `inkwell_skill_activations_total`（按 skill_id label）
  - `inkwell_audit_log_writes_total` / `_failures_total`
- **告警**：Grafana Alerting → SMTP（v1 唯一通道）；月度调用量阈值告警（[RISK-005](./risk-analysis.md)）。
- **业务 trace UI**：[UI-007 调试页](../01-requirements/ui-spec.md) 直接查 Tempo（自建 UI），与 Grafana ops Dashboard 分离。

## 11. 性能目标

> [OQ-002 closed §B](../01-requirements/open-questions.md) 已锁"v1 仅给软目标，不强承诺 SLA"。下表是工程指引，不是合同。

| 维度                       | 软目标（P95） | 备注                      |
| -------------------------- | ------------- | ------------------------- |
| REST CRUD 响应             | < 300 ms      | 单租户场景                |
| AG-UI 流式首字符           | < 1.5 s       | 受模型主导                |
| 编排画布渲染（300 节点）   | < 800 ms      | React Flow 性能基线       |
| 语音 ASR 首字符            | < 500 ms      | Azure Speech 流式         |
| 知识库 RAG 检索            | < 200 ms      | Qdrant gRPC               |
| 审计日志查询（≤ 100 万条） | < 200 ms      | EF + 索引                 |
| 缓存读 / 写（Redis）       | < 5 ms        | 同 VPC / Private Endpoint |

## 12. 扩展性设计

- **横向扩展**：API Deployment 通过 HPA 自动伸缩；DurableTask actor placement 保证 Run 在 Pod 替换时不丢（[ADR-011](./adr/ADR-011-auto-lock-with-inflight-task-survival.md)）。
- **数据扩展**：PostgreSQL StatefulSet 单实例；v2 评估读副本 / Citus 分片。
- **向量扩展**：Qdrant StatefulSet 单实例；v2 评估 [Qdrant cluster](https://qdrant.tech/documentation/guides/distributed_deployment/)。
- **可观测性扩展**：OTel Collector 可水平扩展；Loki / Tempo / Prometheus 启用 [Object Storage backend](https://grafana.com/docs/loki/latest/operations/storage/)（[RISK-006](./risk-analysis.md) 缓解 #2）。
- **文件存储扩展**（[ADR-015](./adr/ADR-015-object-storage-provider-switchable.md)）：`AzureBlob` 天然水平扩展；`MinIO` v2 可平滑升级为 [分布式部署](https://min.io/docs/minio/linux/operations/install-deploy-manage/deploy-minio-multi-node-multi-drive.html)；`LocalFileSystem` 仅推荐单实例 / 测试场景。
- **缓存扩展**（[ADR-016](./adr/ADR-016-cache-provider-redis.md)）：prod 走 Azure Cache for Redis Standard 两节点复制 / Premium cluster；自建场景 v1 单节点 + PVC，v2 评估 Redis Sentinel / Cluster。
- **模型扩展**（[REQ-005](../01-requirements/requirements.md)）：通过 MAF `IChatClient` 接口接入 Azure OpenAI / OpenAI / Anthropic / Qwen / 智谱（v1 真接 Azure OpenAI，其他 v2）。

## 13. 安全设计

详见 [NFR-006](../01-requirements/requirements.md) 与 [ADR-007](./adr/ADR-007-public-api-token-auth.md) / [ADR-009](./adr/ADR-009-multimodal-azure-speech.md) / [ADR-010](./adr/ADR-010-skill-loading-static-only-v1.md) / [ADR-011](./adr/ADR-011-auto-lock-with-inflight-task-survival.md) / [ADR-016](./adr/ADR-016-cache-provider-redis.md)。

- **传输加密**：HTTPS / TLS 1.3（[NGINX Ingress + cert-manager](https://learn.microsoft.com/azure/aks/app-routing)）。
- **凭据管理**（[OQ-A006 closed §B](./open-questions-arch.md)）：prod 走 [Kubernetes Secret](https://kubernetes.io/docs/concepts/configuration/secret/) + [静态加密](https://kubernetes.io/docs/tasks/administer-cluster/encrypt-data/) + RBAC；dev 走 [Docker Compose `.env`](https://docs.docker.com/compose/environment-variables/set-environment-variables/)（`.gitignore` 收敛）；Token / Skill 文件等敏感字段哈希存储。**v1 不引入 Azure Key Vault**，详见 [RISK-013](./risk-analysis.md)。
- **客户端锁屏**：5 min idle 自动锁定；主进程背后保持 SSE 订阅（[ADR-011](./adr/ADR-011-auto-lock-with-inflight-task-survival.md)）。
- **Skill 执行边界**：v1 不执行任何脚本（[ADR-010](./adr/ADR-010-skill-loading-static-only-v1.md)）。
- **公开 API 限流**：60 req/min 默认（[ADR-007](./adr/ADR-007-public-api-token-auth.md)）；走 [`ICacheProvider`](./adr/ADR-016-cache-provider-redis.md) 后端保证多副本下额度一致。
- **审计日志**：所有写操作 + 鉴权失败 + 限流触发都进 [audit_logs](./adr/ADR-008-audit-log-store-and-query.md)。
- **客户端 IPC 隔离**：Electron `contextIsolation: true` + `nodeIntegration: false`（[ADR-001](./adr/ADR-001-client-runtime-electron-react.md)）。

## 14. 主要技术风险

详见 [risk-analysis.md](./risk-analysis.md)。13 条 RISK-NNN 摘要：

- [RISK-001](./risk-analysis.md#risk-001-microsoft-agent-framework-成熟度) Microsoft Agent Framework 成熟度
- [RISK-002](./risk-analysis.md#risk-002-ipersistenceprovider-切换抽象漏出) IPersistenceProvider 切换抽象漏出
- [RISK-003](./risk-analysis.md#risk-003-nfr-003-字面与-oq-017-文字差异-w-003) NFR-003 字面与 OQ-017 文字差异（W-003）
- [RISK-004](./risk-analysis.md#risk-004-aks-单-region-可用性) AKS 单 region 可用性
- [RISK-005](./risk-analysis.md#risk-005-azure-speech-依赖--成本) Azure Speech 依赖 / 成本
- [RISK-006](./risk-analysis.md#risk-006-自托管-grafana-栈数据保留--运维) 自托管 Grafana 栈数据保留 / 运维
- [RISK-007](./risk-analysis.md#risk-007-主进程长-sse-跨锁屏可靠性) 主进程长 SSE 跨锁屏可靠性
- [RISK-008](./risk-analysis.md#risk-008-v1-范围裁剪压力) v1 范围裁剪压力
- [RISK-009](./risk-analysis.md#risk-009-skill-加载错误传播到对话) Skill 加载错误传播
- [RISK-010](./risk-analysis.md#risk-010-v1-不引入-i18n-的-v2-重做成本) v2 引入 i18n 的重构成本
- [RISK-011](./risk-analysis.md#risk-011-文件存储三-provider-contract-漏出) 文件存储三 Provider contract 漏出
- [RISK-012](./risk-analysis.md#risk-012-redis-单点与缓存-invalidation-一致性) Redis 单点与缓存 invalidation 一致性
- [RISK-013](./risk-analysis.md#risk-013-v1-未引入-key-vault-的凭据轮换与隔离弱化) v1 未引入 Key Vault 的凭据轮换与隔离弱化

## 15. 替代方案比较

详见 [tech-selection.md §15](./tech-selection.md) 备选项打分表。摘要：

- **客户端**：Electron 完胜 Tauri / PWA（跨锁屏能力）
- **后端**：.NET 完胜 Python / Node（与 MAF 同生态）
- **数据**：IPersistenceProvider + Qdrant 完胜单 Provider 方案（与 Q-A4 一致）
- **文件存储**：LocalFileSystem / AzureBlob / MinIO 三 Provider 切换完胜单一 Azure Blob 锁定（[ADR-015](./adr/ADR-015-object-storage-provider-switchable.md)）
- **缓存**：ICacheProvider + Redis 完胜 IMemoryCache + 内存 token bucket（[ADR-016](./adr/ADR-016-cache-provider-redis.md)）
- **协议**：REST + AG-UI 完胜 GraphQL / gRPC / 自建 WS（与 MAF hosting 一行注册）
- **锁屏跨会话**：主进程长 SSE（§A）完胜 Run resume cursor（§C）与双协议（§B）—v1 范围控制与实现成本最佳平衡（[ADR-011](./adr/ADR-011-auto-lock-with-inflight-task-survival.md)）
- **凭据**：v1 K8s Secret + .env 完胜 Key Vault CSI（[OQ-A006 §B](./open-questions-arch.md)）—v2 可升级为 Key Vault CSI driver
- **可观测性**：OTel + Grafana 完胜 Azure App Insights / ELK / Datadog（跨云迁移）

## 16. 自检

- 必填章节（[stages.md §5.4](../../../.he/docs/stages.md)）：总体架构图 ✅ / 前端 ✅ / 后端 ✅ / 数据库 ✅ / 缓存 ✅ / 消息 ✅ / 鉴权 ✅ / 文件存储 ✅ / 部署 ✅ / 可观测性 ✅ / 性能目标 ✅ / 扩展性 ✅ / 安全 ✅ / 主要技术风险 ✅ / 替代方案比较 ✅ — 15/15 完整。
- 与 [tech-selection.md](./tech-selection.md) 16 选型 + [risk-analysis.md](./risk-analysis.md) 13 风险全部交叉引用。
- 引用 16 ADR + 7 OQ + REQ / NFR / UI / UF / EX 多条，可向上游追溯。
