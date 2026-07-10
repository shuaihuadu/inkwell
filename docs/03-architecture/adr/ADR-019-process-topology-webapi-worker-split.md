---
id: ADR-019-process-topology-webapi-worker-split
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
  - ADR-002
  - ADR-005
  - ADR-013
  - ADR-017
  - ADR-018
downstream: []
---

# ADR-019 后端进程拓扑：`Inkwell.WebApi` + `Inkwell.Worker` 双进程

## 上下文

[ADR-017](./ADR-017-backend-module-topology-ports-and-adapters.md) 锁定的后端模块拓扑下唯一应用入口是 `Inkwell.Host`（ASP.NET Core minimal-host），承担：

1. HTTP / REST endpoints（[REQ-001 ~ REQ-017](../../01-requirements/requirements.md)）
2. AG-UI Protocol SSE endpoints（[ADR-012](./ADR-012-client-server-protocol-rest-agui.md)）
3. Public API（[ADR-007](./ADR-007-public-api-token-auth.md)）
4. **背景**：[ADR-018 IQueueProvider 双 Provider](./ADR-018-queue-abstraction-channels-default.md) 决议 v1 同期出 `RedisStreamQueueProvider`——consumer group worker 需要常驻进程消费 stream（[`XREADGROUP`](https://redis.io/docs/latest/commands/xreadgroup/)）；以及 [`Microsoft.Agents.AI.DurableTask`](../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.DurableTask/) actor placement 也需要常驻 runner。

如果保留 `Inkwell.Host` 作为单一进程入口，HTTP 请求处理与队列 consumer + DurableTask runner 同进程共存，会出现：

- **故障耦合**：API 进程 OOM / panic 会拖死正在消费的 worker；反之 worker 跑长任务 OOM 也会让 API 502。
- **扩缩耦合**：[ADR-005 HPA](./ADR-005-deployment-docker-compose-aks.md) min 2 / max 10 是按 API 在线请求 CPU 70% 触发的；worker 真正需要的扩缩信号是 `queue_depth`（[ADR-018](./ADR-018-queue-abstraction-channels-default.md) v1 必发指标）。两类信号塞同一 HPA 必然牺牲一类。
- **环境对称性**：[ADR-018](./ADR-018-queue-abstraction-channels-default.md) 用「环境对称」论据解释为什么 v1 同期出 `RedisStreamQueueProvider`——但若 prod 跑「API + worker 同进程」、dev 跑「同进程」，仍会出现「上线才发现多实例 worker 之间的 [`XCLAIM`](https://redis.io/docs/latest/commands/xclaim/) 抢占语义」环境偏移 bug。
- **MAF DurableTask 文档建议**：[`Microsoft.Agents.AI.DurableTask` 中的 `DurableTaskAgentHostedService`](../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.DurableTask/) 内部建议把 worker host 与 web host 分离（避免 web 端 graceful shutdown 时 actor 状态机被中断）。

驱动因素：

- Owner 在 H3 起草前提议「`src/core/Inkwell.Host` 改名 `src/core/Inkwell.WebApi`、新增 `src/core/Inkwell.Worker`」——隐含的进程拆分决策属于 H2 进程拓扑维度，不能由 H3 author 越权拍板。本 ADR 在 H2 阶段对齐进程拓扑，为 H3 提供锁定的进程清单与部署模型。
- 与 [ADR-017 模块拓扑](./ADR-017-backend-module-topology-ports-and-adapters.md) 是**正交维度**：模块拓扑回答「源代码怎么分 csproj」；进程拓扑回答「运行时跑几个进程」。

## 决策

**采用「Web API + Worker」双进程拓扑**：

### 物理结构（在 [ADR-017](./ADR-017-backend-module-topology-ports-and-adapters.md) 基础上的增量）

```text
src/core/
├── Inkwell.Abstractions/                ← 端口层（ADR-017）
├── Inkwell.Core/                        ← 业务实现 + 默认 Provider（ADR-017）
├── providers/                           ← 7 csproj（ADR-017 + ADR-018）
└── Inkwell.WebApi/                      ← HTTP / REST / AG-UI / Public API 入口（原 Inkwell.Host）
└── Inkwell.Worker/                      ← 队列 consumer + DurableTask runner 入口（新增）
```

- 物理 csproj 数：[ADR-017](./ADR-017-backend-module-topology-ports-and-adapters.md) 锁定的 10 → 11（rename `Inkwell.Host` → `Inkwell.WebApi` + 新增 `Inkwell.Worker`）。
- 两个入口都引用 `Inkwell.Core` + 部署期选定的 `providers/*`；两份 `Program.cs` 都调用 [Builder DSL](./ADR-017-backend-module-topology-ports-and-adapters.md) `AddInkwell()`。

### 进程职责划分

| 进程             | csproj                     | SDK                                                                                                                                                                                            | 主要职责                                                                                                                                                                                                                                                                                                       | 不做什么                                                                                                                                  |
| ---------------- | -------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| `Inkwell.WebApi` | `Microsoft.NET.Sdk.Web`    | ASP.NET Core minimal-host                                                                                                                                                                      | REST CRUD（[REQ-002 ~ REQ-017](../../01-requirements/requirements.md)） / AG-UI SSE（[ADR-012](./ADR-012-client-server-protocol-rest-agui.md)） / Public API（[ADR-007](./ADR-007-public-api-token-auth.md)） / 鉴权 / Rate limit                                                                              | 不消费 `IQueueProvider` 队列；不跑 [DurableTask](../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.DurableTask/) actor |
| `Inkwell.Worker` | `Microsoft.NET.Sdk.Worker` | [.NET Generic Host](https://learn.microsoft.com/aspnet/core/fundamentals/host/generic-host) + [`BackgroundService`](https://learn.microsoft.com/aspnet/core/fundamentals/host/hosted-services) | 消费 [`IQueueProvider`](./ADR-018-queue-abstraction-channels-default.md)（dev `ChannelsQueueProvider` / prod `RedisStreamQueueProvider`）队列；DurableTask runner；后台慢任务（KB ingest [REQ-009](../../01-requirements/requirements.md) / Trigger fan-out [REQ-011](../../01-requirements/requirements.md)） | 不开 HTTP 监听端口（除了 OTel `/healthz` 探针 + Prometheus scrape `/metrics` 端口）                                                       |

### 部署形态（[ADR-005](./ADR-005-deployment-docker-compose-aks.md) 增量）

- **dev（Docker Compose）**：增加 `worker` service（与 `api` 同 image tag、不同 entrypoint），`docker compose up -d` 启动同时拉两个容器。
- **prod（AKS / Helm）**：
  - `Deployment: inkwell-webapi`（沿用原 `Deployment: api` 配置；HPA = CPU 70%，min 2 / max 10）
  - `Deployment: inkwell-worker`（新增；HPA = 自定义 metric `queue_depth` ≥ 100 触发，min 1 / max 5；fallback CPU 70%）
  - **同一 image tag**——通过 [Helm `image.tag`](https://helm.sh/docs/chart_template_guide/values_files/) 单值控制，单 release 同时滚 API + Worker，避免版本漂移（[RISK-015](../risk-analysis.md)）。
  - `entrypoint`：image 内置两份，Helm 通过 `command:` / `args:` 区分（`dotnet Inkwell.WebApi.dll` vs `dotnet Inkwell.Worker.dll`）。

### Builder DSL 装配（[ADR-017](./ADR-017-backend-module-topology-ports-and-adapters.md) 增量）

```csharp
// Inkwell.WebApi/Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInkwell()
    .UseSqlServer(builder.Configuration.GetConnectionString("Inkwell"))
    .UseAzureBlob(opts => builder.Configuration.GetSection("Inkwell:FileStorage:Azure").Bind(opts))
    .UseRedis(builder.Configuration.GetConnectionString("Redis"))
    .UseRedisQueue(builder.Configuration.GetConnectionString("RedisQueue")) // enqueue side
    .Build();
// 不注册 IQueueConsumer hosted services
var app = builder.Build();
app.MapInkwellEndpoints(); // REST + AGUI + Public
app.Run();

// Inkwell.Worker/Program.cs
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInkwell()
    .UseSqlServer(builder.Configuration.GetConnectionString("Inkwell"))
    .UseAzureBlob(opts => builder.Configuration.GetSection("Inkwell:FileStorage:Azure").Bind(opts))
    .UseRedis(builder.Configuration.GetConnectionString("Redis"))
    .UseRedisQueue(builder.Configuration.GetConnectionString("RedisQueue")) // consume side
    .Build();
builder.Services.AddInkwellWorker(); // BackgroundService 注册（H3 HD-001 决最终签名）
var host = builder.Build();
host.Run();
```

具体的 hosted service 注册扩展方法 `AddInkwellWorker()` 由 [Inkwell.Core](./ADR-017-backend-module-topology-ports-and-adapters.md) 提供（属业务编排域，不属端口层）。

> **2026-07-06 errata（关联 [ADR-021 2026-07-06 errata](./ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）**：本 ADR 仅锁定进程拓扑（`Inkwell.WebApi` 承载 HTTP 入口，`Inkwell.Worker` 承载队列 consumer + DurableTask runner），**不**对 Migration 执行时机作决策。[ADR-021 §「Migration / DataSeed 启动行为」](./ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 曾把「仅 `Inkwell.WebApi` 启动时跑 Migration」表述为本 ADR「锁定」的推论——该表述已随 ADR-021 2026-07-06 errata 一并修订：**应用启动不再自动执行 Migration**，Migration 改由 CI/CD pipeline 独立步骤执行；本 ADR 的 WebApi / Worker 双进程拓扑本身不变。触发原因：H3 HD-011 起草期发现的生产安全考量，Owner 拍板。
>
> **2026-07-09 errata（[ADR-024](./ADR-024-database-migration-seed-standalone-job.md) 新增第三入口）**：本 ADR 锁定的「WebApi + Worker 双进程」是**常驻进程**拓扑，[ADR-024](./ADR-024-database-migration-seed-standalone-job.md) 新增的 `Inkwell.Migrator` 是**一次性 Job**（Migration + Seed 跑完即退出，不常驻、不开任何监听端口），二者不是同一类实体——`Inkwell.Migrator` 不计入本 ADR「进程拓扑」的范畴，但部署时与 WebApi / Worker 共用同一镜像 tag（详见 ADR-024 §决策·部署形态）。本 ADR 的双进程决策本身不变。

### 可观测性（[ADR-013](./ADR-013-observability-otel-self-hosted-grafana.md) 增量）

- OTel `service.name` resource attribute：`inkwell-webapi` / `inkwell-worker`，dashboards 按 service 维度切分。
- Prometheus scrape 双 source（webapi `/metrics` + worker `/metrics`）。
- Grafana 默认 Dashboard 增加「队列吞吐 / Worker 健康」面板（`queue_depth` / Pod restart count / `BackgroundService` 内的自定义 metric）。

## 备选项

### 备选 A（本决议）：`Inkwell.WebApi` 与 `Inkwell.Worker` 独立 csproj + 独立 Pod

- **被选用**：
  1. **故障隔离**：API OOM / 大请求阻塞不会拖死 worker 消费；worker 跑长任务 OOM 也不会让 API 502。
  2. **独立扩缩**：API HPA 走 CPU 70%（在线请求峰值）；Worker HPA 走 `queue_depth`（异步任务堆积）——两类信号正交，HPA 不冲突。
  3. **环境对称论据延伸**（[ADR-018](./ADR-018-queue-abstraction-channels-default.md)）：dev `docker compose up -d` 同时拉两容器、prod 两 Deployment——开发态与生产态都是「双进程」拓扑，不会出现「上线才发现多实例 worker 抢占语义」偏移。
  4. **prod-ready 形态**：[`Microsoft.Agents.AI.DurableTask`](../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.DurableTask/) 文档隐含建议 worker host 分离（避免 web graceful shutdown 中断 actor 状态机）。

### 备选 B：`Inkwell.Worker` csproj 独立 + 通过 [`AddHostedService<>()`](https://learn.microsoft.com/aspnet/core/fundamentals/host/hosted-services) 寄生在 `Inkwell.WebApi` 进程内

- **放弃理由**：
  1. **故障耦合**：与现状（单 Host）等价，没解决核心问题；API 与 Worker 共享 GC heap / 线程池，互相影响。
  2. **扩缩耦合**：HPA 必须二选一，无法同时按 CPU 70%（API 信号）与 `queue_depth`（worker 信号）触发。
  3. **环境偏移风险**：dev 单进程会让开发者无意识写出「假设 enqueue 立即被同进程 consumer 处理」的代码，prod 切多副本时这类隐含假设全部破坏。
  4. **prod-ready 倒退**：与 [ADR-018](./ADR-018-queue-abstraction-channels-default.md) 「环境对称」立场矛盾。

### 备选 C：dev 走 B（单进程） / prod 走 A（双进程）

- **放弃理由**：
  1. **环境偏移自相矛盾**：[OQ-A008 closed §B](../open-questions-arch.md) Owner 立场是「dev 与 prod 拓扑对称避免偏移 bug」，备选 C 显式制造拓扑差异，与立场反向。
  2. **Helm Chart 复杂度**：dev / prod 部署模板要分叉，与 [ADR-005](./ADR-005-deployment-docker-compose-aks.md) 「dev = Compose / prod = Helm 但拓扑同构」立场冲突。

### 备选 D：进一步拆分 `Inkwell.Worker` 为多个 worker 类型（KB ingest worker / Trigger worker / DurableTask worker）

- **放弃理由**：
  1. v1 用户量级 ~100（[NFR-001](../../01-requirements/requirements.md) / [Q-A4](../../01-requirements/repo-impact-map.md)），单 worker 进程吃所有 BackgroundService 已够；多 worker 类型拆分属于 v2 优化空间。
  2. 多 worker 拆分代价（多 Deployment + 多 HPA + 多镜像 entrypoint）与收益不对称——v1 应避免。
  3. 真触发场景：未来某类 worker 出现「资源占用与其他类型不同数量级」时，再 reopen 为新 ADR 引用 `superseded-by`。

### 备选 E：新增 `Inkwell.Hosting` 共享 hosting csproj（OTel pipeline / health check / Auth middleware 注入器集中）

- **放弃理由**：
  1. **当前重复有限**：v1 阶段 hosting 配置（OTel Resource Builder / health check 路由 / Auth middleware）在 WebApi / Worker 之间确有交集，但单文件可管理；尚未到「拆抽象层」临界值。
  2. **三层升四层成本**：[ADR-017](./ADR-017-backend-module-topology-ports-and-adapters.md) 锁定 P&A 三层（Abstractions / Core / Entrypoint），新增 `Inkwell.Hosting` 是第四层（hosting infra），需要重新评估三 Provider 抽象家族与 hosting 抽象的交叉依赖；v1 不值。
  3. **延后路径清晰**：当 hosting 重复确实变成痛点时，提一份 `superseded-by` ADR 再做。

## 后果

### 正面

- 故障隔离 + 独立扩缩——API 在线请求峰值与异步任务堆积是两类正交负载信号，HPA 不再二选一。
- 环境对称延伸——`docker compose up -d` 在 dev 拉两容器，prod 两 Deployment，与 [ADR-018](./ADR-018-queue-abstraction-channels-default.md) 队列环境对称论据一致。
- prod-ready 形态——[`Microsoft.Agents.AI.DurableTask`](../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.DurableTask/) actor placement 与 web request lifecycle 解耦，graceful shutdown 行为清晰。
- OTel `service.name` 区分后，trace / log / metric 按服务维度切分，[REQ-014 trace](../../01-requirements/requirements.md) 在 [UI-007 调试页](../../01-requirements/ui-spec.md) 上能区分 API span 与 Worker span。

### 负面

- 部署节点 +1：[ADR-005](./ADR-005-deployment-docker-compose-aks.md) Compose 多 1 service / Helm 多 1 Deployment + 1 HPA + 1 ServiceAccount。
- OTel collector receiver 多 1 source；Grafana Dashboard 加 worker 维度（[ADR-013](./ADR-013-observability-otel-self-hosted-grafana.md)）。
- 两份 Program.cs：DI 装配代码部分相似——本决议**不**引入 `Inkwell.Hosting` 共享层（备选 E 放弃理由 #1）；如未来重复变成痛点，再提 superseded-by ADR。
- **版本漂移风险**：API 与 Worker 必须用同一 image tag、同一 release 同时滚——见 [RISK-015 WebApi / Worker 双进程版本漂移与 OTel 双 source](../risk-analysis.md)。

### 中性

- `Microsoft.NET.Sdk.Worker` SDK 是 .NET 6+ 标准模板，与 [ADR-002 .NET 10](./ADR-002-backend-runtime-dotnet10-aspnetcore.md) 兼容；Generic Host 已是 ASP.NET Core 标准 hosting 模型。
- Worker 不开 HTTP 监听端口（除 health probe + Prometheus scrape 端口）——是 K8s [`livenessProbe`](https://kubernetes.io/docs/concepts/workloads/pods/pod-lifecycle/#container-probes) 的常规模式，Helm Chart 模板成熟。
- 多 worker 类型拆分（备选 D）留作 v2 backlog 触发条件清晰：「某类 worker 与其他类型资源占用差数量级」。

## 迁移路径

**breaking change 标记**：是。本 ADR 落地后，下列文档需更新（按依赖顺序）：

| 步骤 | 文件                                                                                            | 改动                                                                                                                             | 是否需翻 status                                |
| ---- | ----------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------- |
| 1    | [`ADR-017` §3.1 csproj 树 + §模块映射](./ADR-017-backend-module-topology-ports-and-adapters.md) | `Inkwell.Host` → `Inkwell.WebApi`；新增 `Inkwell.Worker`；csproj 数 10 → 11                                                      | 内部增量，仍 accepted                          |
| 2    | [`ADR-018` §决策 §`Inkwell.Queue.Redis` 部署](./ADR-018-queue-abstraction-channels-default.md)  | 「consumer 跑在 `Inkwell.Worker`」一句话补充；`UseRedisQueue()` 在 WebApi 仅注册 enqueue 侧、Worker 注册 consume 侧              | 内部增量，仍 accepted                          |
| 3    | [`ADR-005` §决策 §dev / §prod](./ADR-005-deployment-docker-compose-aks.md)                      | Compose 加 `worker` service；Helm 加 `Deployment: inkwell-worker` + HPA(`queue_depth`)                                           | reviewed → 增量 reviewed（updated 2026-05-10） |
| 4    | [`ADR-013` §决策](./ADR-013-observability-otel-self-hosted-grafana.md)                          | OTel `service.name` 区分 webapi / worker；Prometheus scrape 双 source；Dashboard 加 worker 面板                                  | reviewed → 增量 reviewed                       |
| 5    | [`architecture.md` §1 总体图 / §3 后端架构 / §6 队列 consumer / §9 部署](../architecture.md)    | 拓扑图 / csproj 树 / Builder DSL / 部署清单同步                                                                                  | reviewed → 增量 reviewed                       |
| 6    | [`tech-selection.md` §0 摘要表 / §21 备选项打分表](../tech-selection.md)                        | 新增「后端进程拓扑」条目六字段；§21.11 进程拓扑对比表；§22 自检统计更新                                                          | reviewed → 增量 reviewed                       |
| 7    | [`risk-analysis.md`](../risk-analysis.md)                                                       | 新增 [RISK-015 WebApi / Worker 双进程版本漂移与 OTel 双 source](../risk-analysis.md)                                             | reviewed → 增量 reviewed                       |
| 8    | [`adr/README.md`](./README.md)                                                                  | 新增 ADR-019 行 + 依赖树                                                                                                         | draft 不变                                     |
| 9    | [`AGENTS.md` §3.1 / §3.2](../../../AGENTS.md)                                                   | `Inkwell.Host` → `Inkwell.WebApi` + `Inkwell.Worker`；§3.2 「Inkwell.Host → 全部」改为「Inkwell.WebApi / Inkwell.Worker → 全部」 | 签字位（需人工授权）                           |

**自动化检查命令**（落地后用以确认旧名已清理）：

```bash
grep -rn "Inkwell\.Host" docs/ AGENTS.md
grep -rn "src/core/Inkwell\.Host" docs/ AGENTS.md
```

## 状态

`accepted` — 2026-05-10。Owner 在本 ADR 起草会话中通过 picker 选 A：(1) 重命名 `Inkwell.Host` → `Inkwell.WebApi`；(2) Worker 独立可执行（独立 Pod，HPA 基于 `queue_depth` / CPU 独立扩缩）；(3) 共用 hosting 代码直接复用 `Inkwell.Core`（11 csproj，三层不变）。

## 置信度

`high` — 决策与四项强证据对齐：(1) [ADR-018 环境对称](./ADR-018-queue-abstraction-channels-default.md) 论据延伸；(2) [`Microsoft.Agents.AI.DurableTask`](../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.DurableTask/) 文档建议 worker host 分离；(3) [ADR-005 HPA min 2 / max 10](./ADR-005-deployment-docker-compose-aks.md) 是 API 在线请求触发器，与 `queue_depth` 异步任务触发器正交；(4) `Microsoft.NET.Sdk.Worker` 是 .NET 6+ 业界标准模板，运维成本可控。
