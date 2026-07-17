---
id: ADR-018-queue-abstraction-channels-default
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
  - ADR-003
  - ADR-006
  - ADR-017
  - OQ-A008
downstream: []
---

# ADR-018 队列抽象：`IQueueProvider` + Channels（dev）/ Redis Stream（integration / prod）双 Provider

## 上下文

[`AGENTS.md` §3.3](../../../AGENTS.md) 现状声明"v1 不引入消息队列（RabbitMQ / Service Bus）— v1 用 [`System.Threading.Channels`](https://learn.microsoft.com/dotnet/core/extensions/channels) 内异步"。[`architecture.md` §6 消息机制](../architecture.md) 现状描述：

- **后端内异步**：`System.Threading.Channels`（in-process 流式 / 跨组件传递）
- **后端持久工作流**：[`Microsoft.Agents.AI.DurableTask`](../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.DurableTask/)（跨 Pod 重启续作）
- **不引入** RabbitMQ / Azure Service Bus / Redis Stream

H3 详细设计起草启动时，Owner 在 [ADR-017](./ADR-017-backend-module-topology-ports-and-adapters.md) Ports & Adapters 重构同会话提出 `Inkwell.Abstractions` 应包含 `IQueueProvider` 接口，并允许多 csproj 切换实现（类比 [`IPersistenceProvider`](./ADR-004-data-store-provider-switchable-ef-core.md) / [`IFileStorageProvider`](./ADR-015-object-storage-provider-switchable.md) / [`ICacheProvider`](./ADR-016-cache-provider-redis.md)）。

`h2-architect-advisor` 在会话中两轮请求 v1 触发场景（即“v1 哪些场景非选 IQueueProvider 不可，DurableTask + Channels 已覆盖之外的”）。**Owner 在 [OQ-A008](../open-questions-arch.md) 评审中提供了三项前置**：

- **(a) v1 触发场景**：**环境对称论据**——`ChannelsQueueProvider` = 开发态默认（InMemory / 单进程 / 零依赖）；`RedisStreamQueueProvider` = 集成测试 + prod 默认，与 [`IPersistenceProvider`](./ADR-004-data-store-provider-switchable-ef-core.md) / [`ICacheProvider`](./ADR-016-cache-provider-redis.md) / [`IFileStorageProvider`](./ADR-015-object-storage-provider-switchable.md) 「InMemory dev · 真 Provider prod」拓扑对齐。以此避免「开发期靠 Channels 跳过可靠性设计、上线才发现多副本抢同一任务」这类环境偏移 bug。
- **(b) 可靠性策略**：DLQ N=3 + 24h 保留期；fairness / crash recovery / retry 依赖 [Redis Streams 内置语义](https://redis.io/docs/latest/develop/data-types/streams/)（[`XREADGROUP`](https://redis.io/docs/latest/commands/xreadgroup/) consumer group + [`XCLAIM`](https://redis.io/docs/latest/commands/xclaim/) visibility timeout = 5 min + 指数退避 1s/max 60s）。
- **(c) observability 最小发送集**：`queue_depth`（活动 stream 长度）。其余指标进 [RISK-014](../risk-analysis.md) “prod 上线前补齐”残余风险。

驱动因素：

- Owner 立场：希望 `Inkwell.Abstractions` 拥有 `IQueueProvider` 抽象，与三 Provider 家族（Persistence / FileStorage / Cache）保持 `*Provider` 后缀风格一致。
- **环境对称原则**：v1 同期出两个 Provider——`ChannelsQueueProvider`（dev / unit test）与 `RedisStreamQueueProvider`（integration test / prod），与其他三 Provider 家族一致。
- ADR 六字段约束：「为什么选择」指向「所有依赖复杂中间件的抽象都遵环境对称拓扑」；「放弃」指向「DurableTask 已覆盖工作流语义，但不覆盖「fire-and-forget + worker pool」语义」。
- DurableTask / Channels / RedisStream 在 v1 并存：DurableTask 负责跨节点工作流（replay / fan-out）；Channels 负责进程内流式（token output）；Redis Stream 负责「幂等 worker pool + 任务级 fairness / crash recovery」。

## 决策

### 接口位

`Inkwell.Abstractions` 新增 `IQueueProvider` 接口，最小必要面如下（具体方法签名留 H3 HD-001 协作 picker 拍板）：

```csharp
namespace Inkwell.Abstractions.Queues;

/// <summary>
/// 后端队列抽象。用于业务代码不耦合具体队列实现。
/// </summary>
public interface IQueueProvider
{
    Task EnqueueAsync<T>(string queueName, T message, CancellationToken ct = default);
    IAsyncEnumerable<QueueMessage<T>> DequeueAsync<T>(string queueName, CancellationToken ct = default);
    Task AcknowledgeAsync(string queueName, string messageId, CancellationToken ct = default);
}

public sealed record QueueMessage<T>(string Id, T Payload, int DeliveryCount);
```

具体接口面（重试 / dead-letter / FIFO 语义 / 消息可见性窗口）由 H3 HD-001 在 picker 模式下逐字段拍板，本 ADR 仅锁"必有 Enqueue/Dequeue/Ack 三动作"。

### 默认实现：`ChannelsQueueProvider`

`Inkwell.Core` 内置 `ChannelsQueueProvider`，基于 [`System.Threading.Channels`](https://learn.microsoft.com/dotnet/core/extensions/channels) 实现：

- **语义**：in-process、单 Pod 内、不持久（Pod 重启消息丢失）。
- **DI 注册**：`AddInkwell().Build()` 默认注入 `ChannelsQueueProvider` 为 `IQueueProvider`。
- **使用边界**：与现状 `architecture.md` §6 Channels 用途等价——业务代码改用 `IQueueProvider` 注入即可保持当前行为。

### 分布式实现：`RedisStreamQueueProvider`（v1 同期出 csproj）

`providers/Queue/Inkwell.Queue.Redis/` **v1 同期出 csproj**（[OQ-A008 closed §B](../open-questions-arch.md)）。锁定设计点（实现详细留 H3 HD）：

- **底层**：[Redis 8 Streams](https://redis.io/docs/latest/develop/data-types/streams/) + consumer group；与 [ADR-016 ICacheProvider](./ADR-016-cache-provider-redis.md) **可复用同一 Redis 实例**（不同 db number）也可独立部署。具体由 H3 [Inkwell.Queue.Redis](./ADR-017-backend-module-topology-ports-and-adapters.md) HD 决。
- **默认 DLQ**：N=3（消费失败三次后转入 `<queueName>:dlq` stream）；保留 24h；DLQ 消息走 [`MAXLEN ~ N`](https://redis.io/docs/latest/commands/xadd/) trim。
- **Fairness**：多副本 worker 用 [`XREADGROUP`](https://redis.io/docs/latest/commands/xreadgroup/) consumer group 默认公平分发（Redis 内置顺序分发 + ack 机制）。
- **Crash recovery**：[`XCLAIM`](https://redis.io/docs/latest/commands/xclaim/) visibility timeout = 5 min；worker SIGKILL 后未 ack 的 message 被同赋 consumer group 重插赋。
- **Retry**：指数退避 + jitter（1s 起，max 60s），在 [`Inkwell.Queue.Redis`](./ADR-017-backend-module-topology-ports-and-adapters.md) 包装 [`XPENDING`](https://redis.io/docs/latest/commands/xpending/) + [`XADD`](https://redis.io/docs/latest/commands/xadd/) 重发。
- **Observability v1 必发**：`queue_depth`（[`XLEN`](https://redis.io/docs/latest/commands/xlen/)）。其余 `queue_consume_latency_p95` / `queue_dlq_count` / `queue_consumer_lag` / `queue_redelivery_count` / `queue_consumer_active` 进 [RISK-014 残余风险](../risk-analysis.md)，prod 上线前补齐。
- **DI 切换**：`AddInkwell().UseRedisQueue(connectionString)` 插入后覆盖 `ChannelsQueueProvider`；dev 不调用则保持 Channels 默认。
- **Consumer 跑在哪里**（[ADR-019](./ADR-019-process-topology-webapi-worker-split.md)）：`RedisStreamQueueProvider` 的 [`XREADGROUP`](https://redis.io/docs/latest/commands/xreadgroup/) consumer 默认跑在 `Inkwell.Worker` 进程（独立 Pod / HPA 基于 `queue_depth`）；`Inkwell.WebApi` 仅注册 enqueue 侧 producer。根据 [ADR-019](./ADR-019-process-topology-webapi-worker-split.md) 决议拓扑，业务代码不能依赖「enqueue 后同进程 consumer 立即处理」隐含假设。

### `AGENTS.md` §3.3 措辞调整

由“不引入消息队列（RabbitMQ / Service Bus）”调整为“**不引入消息队列外部中间件**（RabbitMQ / Azure Service Bus）；`IQueueProvider` 接口 + `ChannelsQueueProvider`（dev） + `RedisStreamQueueProvider`（integration / prod）是与 [`IPersistenceProvider`](./ADR-004-data-store-provider-switchable-ef-core.md) / [`ICacheProvider`](./ADR-016-cache-provider-redis.md) / [`IFileStorageProvider`](./ADR-015-object-storage-provider-switchable.md) 同构的环境对称 Provider，不被视为“外部中间件””。

## 备选项

### 备选 A：不引入 `IQueueProvider`，维持现状（架构 §6 + AGENTS.md §3.3）

- **放弃理由**：
  1. Owner 在 H2 评审周期内多次明确表态"v1 引入 IQueueProvider 抽象"立场，备选 A 与立场冲突。
  2. 接口位预留对未来分布式队列扩展无运维代价（v1 仅 Channels 套壳，运行时与现状等价）。

### 备选 B：`IQueueProvider` 接口 + 仅 `ChannelsQueueProvider`，Redis 实现挂 OQ-A008 推迟

- **放弃理由**：
  1. Owner 在 [OQ-A008](../open-questions-arch.md) 评审中提供了**环境对称论据**作为明确 v1 触发场景（dev = Channels / integration + prod = Redis Stream）——与三 Provider 家族（[`IPersistenceProvider`](./ADR-004-data-store-provider-switchable-ef-core.md) / [`ICacheProvider`](./ADR-016-cache-provider-redis.md) / [`IFileStorageProvider`](./ADR-015-object-storage-provider-switchable.md)）拓扑一致。备选 B 仅适合「触发场景缺失」场景，现在不适用。
  2. 「开发期靠 Channels 跳过可靠性设计」是实际差异不是理论差异——推迟 Redis 实现到 OQ 会造成 H3 详细设计期间业务代码隐含「单进程 in-process」偏见，prod 上线才发现。

### 备选 C（本决议）：`IQueueProvider` 接口 + `ChannelsQueueProvider`（dev） + `RedisStreamQueueProvider`（integration / prod）v1 同期出 csproj

- **被选用**：
  1. **环境对称原则**：与三 Provider 家族（Persistence / FileStorage / Cache）都是「InMemory dev / 真 Provider prod」拓扑；队列 Provider 不足不额外出拓扑。避免「开发期靠 Channels 跳过可靠性设计、上线才发现多副本抢同一任务」这类环境偏移 bug。
  2. ADR 六字段可填齐——「为什么」指向环境对称；「放弃 DurableTask」指向「DurableTask = workflow、本抽象 = fire-and-forget queue，语义不同」。
  3. v1 代价可控：H3 同期交付 [`Inkwell.Queue.Redis`](./ADR-017-backend-module-topology-ports-and-adapters.md) HD；DLQ N=3 + Redis Streams 内置语义（[`XREADGROUP`](https://redis.io/docs/latest/commands/xreadgroup/) consumer group + [`XCLAIM`](https://redis.io/docs/latest/commands/xclaim/) visibility timeout = 5 min + 指数退避），不需自建 retry / fairness / crash recovery 逻辑。
  4. observability 代价接受：v1 必发仅 `queue_depth`（[XLEN](https://redis.io/docs/latest/commands/xlen/)），其余进残余风险，不阻塞 v1。

### 备选 D：`IQueueProvider` 包装 DurableTask 作为 thin wrapper

- **放弃理由**：
  1. DurableTask 是工作流编排（actor placement + 跨步骤状态机），语义与队列（fire-and-forget + worker pool）不同；强制包装会丢失 DurableTask 的 sub-orchestration / fan-out / fan-in / replay 能力。
  2. Adapter pattern 装饰本身无业务价值——业务代码直接用 [`Microsoft.Agents.AI.DurableTask`](../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.DurableTask/) 接口已是清晰契约。

## 后果

### 正面

- 接口位预留 + 环境对称双 Provider——与三 Provider 家族拓扑对齐，团队认知一致（`I*Provider` 后缀 + Builder DSL `Use*()` 扩展方法）。
- v1 同期交付 `RedisStreamQueueProvider`，避免「开发期靠 Channels 跳过可靠性设计、上线才发现多副本语义问题」环境偏移 bug。
- DLQ 默认 N=3 24h + Redis Streams 内置语义（consumer group + visibility timeout + XCLAIM）= H3 HD 可以 thin wrapper 交付，不需自建可靠性逻辑。

### 负面

- v1 同期交付 `RedisStreamQueueProvider` 带来附加工作量：
  1. 新增 `providers/Queue/Inkwell.Queue.Redis/` csproj + `tests/core/providers/Queue/Inkwell.Queue.Redis.Tests/`。
  2. [RISK-014](../risk-analysis.md) 从「占位」激活为「已激活」；observability 指标仅 v1 发 `queue_depth`，其余（`queue_consume_latency_p95` / `queue_dlq_count` / `queue_consumer_lag` / `queue_redelivery_count` / `queue_consumer_active`）进残余风险，prod 上线前补齐。
  3. [`ADR-005`](./ADR-005-deployment-docker-compose-aks.md) Compose / AKS 需明确 Redis 实例是否复用（与 [ADR-016 cache](./ADR-016-cache-provider-redis.md) 同一 Redis 不同 db number） vs 各自独立部署——H3 [Inkwell.Queue.Redis](./ADR-017-backend-module-topology-ports-and-adapters.md) HD 决。
  4. [`ADR-013`](./ADR-013-observability-otel-self-hosted-grafana.md) metrics 需补 `inkwell_queue_depth`（v1 必发），其余同上。
- [`AGENTS.md` §3.3](../../../AGENTS.md) 「不引入消息队列」措辞需修订（见 [§决策](#决策) “AGENTS.md §3.3 措辞调整”段）——属人工签字位修订。

### 中性

- `IQueueProvider` 接口面（重试 / dead-letter / FIFO / 可见性窗口语义）由 H3 HD-001 picker 拍板，本 ADR 仅锁三动作最小集 + DLQ N=3 24h 全局默认。
- Channels 与 Redis Stream 在接口语义上存在差异（如 Channels 不原生支持 message ID / DeliveryCount）：`ChannelsQueueProvider` 需伪造 ID + 进程内 dictionary 跟踪 DeliveryCount，以适配接口。详细设计留 H3 HD。

## 状态

`accepted` — 2026-05-10。[OQ-A008](../open-questions-arch.md) 同时 closed §B（v1 同期出 `RedisStreamQueueProvider` csproj + DLQ N=3 24h + queue_depth metric）。[RISK-014](../risk-analysis.md) 同步从「占位」激活为「已激活」。

## 置信度

`high` — 环境对称论据与三 Provider 家族一致；DLQ + Redis Streams 内置语义（consumer group + XCLAIM + visibility timeout）是 Redis 官方文档 + 业界成熟实践（[Sidekiq](https://github.com/sidekiq/sidekiq) / [Bull](https://github.com/OptimalBits/bull) 等众多实现都遵此拓扑），v1 不需自创抽象。
