---
id: HD-005
title: Inkwell.Abstractions 详细设计 — Queue Port（IQueueProvider facade + MessageEnvelope + Options）
stage: H3
status: reviewed
reviewers: [Inkwell]
upstream:
  - REQ-009
  - REQ-014
  - ADR-002
  - ADR-018
  - ADR-019
  - ADR-023
  - HD-001
  - HD-004
---

<!-- markdownlint-disable MD060 -->
<!-- 中文 + 英文混排长表格在 markdownlint 列宽计算下字面对齐 ≠ 视觉对齐（详 /memories/markdown-lint.md，与 HD-004 同处理方式），表格仍按 docs-style §3 视觉对齐维护，机械 MD060 不予执行。 -->

> **错误处理约定**（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) accepted by Inkwell 2026-05-11，含 [errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) 废错误码、[errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 删 `Result<T>` / `Error` 抽象）：端口层与业务层统一采用裸 `Task<T>` + .NET BCL 异常。Inkwell **不自建 `Result<T>` / `Error` 抽象** / 不自建错误码机制 / 不自建端口层异常基类；仅保留 `InkwellConfigurationException` / `InkwellBuilderException` 两个程序错误子类用于 DI 装配期校验。本 HD 与 [HD-004](HD-004-Inkwell.Abstractions-cache-port.md) 同批次起草，ADR-023 已是最终态规约，全部签名从第一版直接采用裸 `Task<T>` + BCL 异常，不存在"先 Result 后 errata"的历史包袱。
>
> **范围切片**：本 HD 覆盖 `Inkwell.Abstractions/Queue/` 子层——`IQueueProvider` facade（4 方法：Enqueue / Dequeue / Acknowledge / NegativeAcknowledge，[ADR-018 §决策](../../03-architecture/adr/ADR-018-queue-abstraction-channels-default.md) 三动作最小集 + 本 HD picker 追加显式 Nack）、`MessageEnvelope<T>` DTO（含 [RISK-015](../../03-architecture/risk-analysis.md) 要求的 `TraceParent` 字段）、`QueueOptions` + Validator。**不**实现 Provider 行为（`ChannelsQueueProvider` 在 `Inkwell.Core` 独立 HD 起草；`RedisStreamQueueProvider` 在 `providers/Queue/Inkwell.Queue.Redis/` 独立 HD 起草）、**不**锁队列名命名规约（[picker Q-queuename-convention=A](#13-决策记录)：端口层只接受 `string queueName`，业务侧各自拼接，如 `kb-ingest` / `trigger-fanout`）、**不**锁重试退避算法细节（[ADR-018](../../03-architecture/adr/ADR-018-queue-abstraction-channels-default.md) 指数退避 1s~60s 是 `RedisStreamQueueProvider` 实现细节，留 Provider HD 决）。
>
> **跨 HD 关联**：本 HD 与 [HD-001 foundation](HD-001-Inkwell.Abstractions-foundation.md)（Builder DSL / OTel 字段 / `InkwellProvidersOptions.Queue` 选择器槽位，默认值 `"Channels"`）+ [HD-004 Cache port](HD-004-Inkwell.Abstractions-cache-port.md)（同级端口模板，OTel span 命名风格对齐）形成 Abstractions 端口族的第四张 HD。**关键风险联动**：[AGENTS.md §3.4 RISK-015](../../../AGENTS.md) 要求"H4 必须补 enqueue (WebApi) → consume (Worker) → ack 跨服务集成用例"+"`MessageEnvelope` 必含 `traceparent` 字段以保 [REQ-014](../../01-requirements/requirements.md) trace 全链路跨服务不断链"——本 HD §3.2 / §5.3 / §7.3 已显式设计该字段与自动注入机制，避免成为 H4 / H5 返工点。

## 1. 模块概述

### 1.1 职责

- `IQueueProvider` facade（§3.1）：定义业务命名空间访问队列层的统一入口；Enqueue / Dequeue / Acknowledge / NegativeAcknowledge 共 4 个方法，覆盖 [ADR-018 §决策](../../03-architecture/adr/ADR-018-queue-abstraction-channels-default.md) 列出的 fire-and-forget + worker pool 场景（KB ingest [REQ-009](../../01-requirements/requirements.md)、Trigger fan-out [REQ-011](../../01-requirements/requirements.md)）
- `MessageEnvelope<T>` DTO（§3.2）：`DequeueAsync` 的消息载体，携带 `MessageId` / `Payload` / `EnqueuedTime` / `DeliveryCount` / `TraceParent`（[RISK-015](../../03-architecture/risk-analysis.md) 跨进程 trace 不断链的关键字段）
- `QueueOptions` + Validator（§3.3 ~ §3.4）：DLQ / 可见性超时可靠性参数、`EnableSensitiveDataLogging` 开关

### 1.2 范围

- **在内**：facade 接口 + DTO + Options
- **不在内**：
  - 两 Provider 实现（`ChannelsQueueProvider` 在 `Inkwell.Core` 独立 HD；`RedisStreamQueueProvider` 在 `providers/Queue/Inkwell.Queue.Redis/` 独立 HD，[ADR-018](../../03-architecture/adr/ADR-018-queue-abstraction-channels-default.md)）
  - 队列名命名规约的强制实现（`queueName` 留业务命名空间自行拼接，[picker Q-queuename-convention=A](#13-决策记录)）
  - 重试退避算法细节（指数退避 1s ~ 60s + jitter 是 `RedisStreamQueueProvider` 内部包装 [`XPENDING`](https://redis.io/docs/latest/commands/xpending/) + [`XADD`](https://redis.io/docs/latest/commands/xadd/) 的实现细节，[ADR-018](../../03-architecture/adr/ADR-018-queue-abstraction-channels-default.md)，留 Provider HD 决）
  - DLQ 消息的人工检视 / 重发管理 UI（v1 不交付，[RISK-014 残余风险](../../03-architecture/risk-analysis.md)）
  - `Inkwell.Queue.Redis` 与 `Inkwell.Cache.Redis` 是否复用同一 Redis 实例（不同 db number）vs 独立部署——留 `Inkwell.Queue.Redis` Provider HD 决（[risk-analysis.md RISK-014](../../03-architecture/risk-analysis.md)）
  - `consumerGroup` 的业务级命名约定（如 `kb-ingest-workers` / `trigger-fanout-workers`）——业务侧 `Inkwell.Core.KnowledgeBase` / `.Triggers` HD 起草时决定
  - WebApi → Worker 跨服务集成测试的具体用例设计（[AGENTS.md §3.4 RISK-015](../../../AGENTS.md) 要求 H4 补齐，本 HD 仅锁定 `MessageEnvelope.TraceParent` 字段位置）

### 1.3 关键决策摘要

> 全部由 2026-07-05 picker 拍板，决策证据见本节"出处"列；详细候选与放弃理由见 [§13 决策记录](#13-决策记录)。

- **Q-scope**：`IQueueProvider` 含 4 方法（Enqueue / Dequeue / Acknowledge / NegativeAcknowledge），在 [ADR-018](../../03-architecture/adr/ADR-018-queue-abstraction-channels-default.md) 三动作最小集基础上追加显式 `NegativeAcknowledgeAsync`，支持业务判定永久失败时立即拒绝重投，不必等待 visibility timeout
- **Q-dequeue-shape**：`DequeueAsync<T>` 返回 `IAsyncEnumerable<MessageEnvelope<T>>` 流式拉取；显式 `consumerGroup`（必填，逻辑 worker 池名）+ 可选 `consumerName`（`null` 时 Provider 实现回退到 `{Environment.MachineName}-{Guid.CreateVersion7()}` 自动生成）
- **Q-envelope-shape**：`MessageEnvelope<T>` 5 字段最小集——`MessageId` / `Payload` / `EnqueuedTime` / `DeliveryCount` / `TraceParent`（可空 `string`，W3C `traceparent` 格式），不额外携带 `TraceState`
- **Q-traceparent-injection**：`EnqueueAsync` 内部自动从 [`Activity.Current?.Id`](https://learn.microsoft.com/dotnet/api/system.diagnostics.activity.id)（W3C `ActivityIdFormat`）捕获并写入 `MessageEnvelope.TraceParent`，业务调用方无需手工传参
- **Q-serialization**：复用 [HD-004 Q-serialization](HD-004-Inkwell.Abstractions-cache-port.md#13-决策记录) 决策——统一 [`System.Text.Json`](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/overview) + BCL 内置静态实例 [`JsonSerializerOptions.Web`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions.web)，不新增可配置项
- **Q-dlq-policy**：`MaxDeliveryAttempts` / `VisibilityTimeoutSeconds` / `DlqRetentionHours` 暴露为 `QueueOptions` 字段，默认值锁 [ADR-018](../../03-architecture/adr/ADR-018-queue-abstraction-channels-default.md) 数字（3 / 300s / 24h），允许环境覆写，与 [HD-004 CacheOptions](HD-004-Inkwell.Abstractions-cache-port.md) TTL 上下限模式一致
- **Q-ack-return-shape**：`AcknowledgeAsync` / `NegativeAcknowledgeAsync` 均返回 `Task<bool>`；`false` 表示 `messageId` 未知或已过期（幂等语义，不抛异常），与 [HD-004 `ReleaseLockAsync`](HD-004-Inkwell.Abstractions-cache-port.md#14-与-hd-001-51--52-命名约定的一致性声明) 风格一致
- **Q-perf-budget**：宽松档，`EnqueueAsync` facade overhead P50 < 20ms / P99 < 100ms；`DequeueAsync` 是流式长轮询，不适用相同语义，改用"消息可见延迟"预算 P50 < 1s / P99 < 5s，与 [HD-003](HD-003-Inkwell.Abstractions-file-storage-port.md) / [HD-004](HD-004-Inkwell.Abstractions-cache-port.md) 风格对齐
- **Q-queuename-convention**：不在端口层锁——`IQueueProvider` 只接受 `string queueName`；命名约定（如 `kb-ingest` / `trigger-fanout`）留业务命名空间自行拼接，与 [HD-003 picker Q3](HD-003-Inkwell.Abstractions-file-storage-port.md) / [HD-004 picker Q-key-convention](HD-004-Inkwell.Abstractions-cache-port.md#13-决策记录) 风格一致
- **Q-otel**：OTel span 命名 `queue.<verb>`（enqueue / dequeue / acknowledge / negative_acknowledge）+ 6 个私有字段 `queue.provider` / `queue.name` / `queue.message_id` / `queue.consumer_group` / `queue.delivery_count` / `queue.operation_outcome`

### 1.4 与 HD-001 §5.1 / §5.2 命名约定的一致性声明

[HD-001 §5.1 / §5.2](HD-001-Inkwell.Abstractions-foundation.md#51-命名) 锁定的命名前缀语义（`Find*` / `Get*` / `Exists*` / `Delete*` / `List*` / `Create*`）不覆盖队列领域的"生产 - 消费 - 确认"语义。本 HD 是 Abstractions 端口族中首次出现该类语义的 HD，作为**补充规则**记录（同 [HD-004 §1.4](HD-004-Inkwell.Abstractions-cache-port.md#14-与-hd-001-51--52-命名约定的一致性声明) Cache 领域例外的记录方式）：

- `Enqueue*Async` → `Task`（无返回值）：成功即入队，失败抛 BCL 异常（§4.2）；不视为查询也不视为幂等操作
- `Dequeue*Async` → `IAsyncEnumerable<MessageEnvelope<T>>`：持续流式拉取，同 [HD-001 §5.2 流式签名约定](HD-001-Inkwell.Abstractions-foundation.md#52-签名)（`IAsyncEnumerable<T> XxxAsync(..., [EnumeratorCancellation] CancellationToken ct = default)`）；空队列时枚举挂起等待新消息（长轮询语义），不返回空集合后立即结束
- `Acknowledge*Async` / `NegativeAcknowledge*Async` → `Task<bool>`：**预期返回值**（不是异常，调用方按值判断）——`false` 表示 `messageId` 未知 / 已过期 / 已被确认过（幂等），与 [HD-001 §5.2 `Delete*Async`](HD-001-Inkwell.Abstractions-foundation.md#52-签名) 幂等语义一致但命名不套用该前缀（队列领域惯例用 Ack / Nack 而非 Delete，同 [`Azure Service Bus` `CompleteMessageAsync` / `AbandonMessageAsync`](https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusreceiver) 惯例一致）

## 2. 文件结构

```text
src/core/Inkwell.Abstractions/
  Queue/                                  # （新增子目录）
    IQueueProvider.cs                     # 顶层 facade（4 方法）
    MessageEnvelope.cs                    # record<T>，含 TraceParent（RISK-015）
    QueueOptions.cs                       # 详细配置
    QueueOptionsValidator.cs              # IValidateOptions<QueueOptions>
```

> **csproj 依赖白名单**：HD-005 不引入新依赖，仍仅 [HD-001 §2 锁定的](HD-001-Inkwell.Abstractions-foundation.md) `Microsoft.Extensions.{DependencyInjection,Configuration,Options,Logging}.Abstractions` + `Microsoft.Extensions.VectorData.Abstractions`（HD-008 起用）+ [`System.Text.Json`](https://learn.microsoft.com/dotnet/api/system.text.json)（BCL 内置，无需额外包引用）+ [`System.Diagnostics.Activity`](https://learn.microsoft.com/dotnet/api/system.diagnostics.activity)（BCL 内置，`TraceParent` 自动捕获所需，2026-07-05 [design-review-report.md §15.3 N16](../design-review-report.md#15-hd-005-iqueueprovider-增量评审2026-07-05) 评审发现后补齐，与 file-structure.md 转述对齐）。**严禁**因本 HD 引入 `StackExchange.Redis` 等任何具体 SDK（违反 [ADR-017 零外部包约束](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) + [RISK-011 三 Provider contract 漏出](../../03-architecture/risk-analysis.md) 同构风险）。

## 3. 程序文件设计（10 字段 × 4 文件）

### 3.1 `Queue/IQueueProvider.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Queue/IQueueProvider.cs`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| 职责         | 顶层队列 facade；4 个方法覆盖生产 / 消费 / 确认 / 拒绝重投；两 Provider 实现完全相同 ABI（[ADR-018](../../03-architecture/adr/ADR-018-queue-abstraction-channels-default.md)）；全 4 方法签名走裸 `Task` / `Task<bool>` / `IAsyncEnumerable<T>` + BCL 异常（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)），不走 `Result<T>`                                                                                                                                                                                                                                                                                                                                                                                                                    |
| 对外接口     | `public interface IQueueProvider { Task EnqueueAsync<T>(string queueName, T payload, CancellationToken ct = default); IAsyncEnumerable<MessageEnvelope<T>> DequeueAsync<T>(string queueName, string consumerGroup, string? consumerName = null, [EnumeratorCancellation] CancellationToken ct = default); Task<bool> AcknowledgeAsync(string queueName, string consumerGroup, string messageId, CancellationToken ct = default); Task<bool> NegativeAcknowledgeAsync(string queueName, string consumerGroup, string messageId, CancellationToken ct = default); }`                                                                                                                                                                                                                          |
| 内部函数或类 | 接口本身；实现由两 Provider HD 各自提供（`ChannelsQueueProvider` / `RedisStreamQueueProvider`）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| 输入数据     | `string queueName`（全部方法）/ `T payload`（Enqueue） / `string consumerGroup`（Dequeue / Acknowledge / NegativeAcknowledge） / `string? consumerName`（Dequeue，`null` 时 Provider 实现自动生成） / `string messageId`（Acknowledge / NegativeAcknowledge） / `CancellationToken ct`（全部方法）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| 输出数据     | `Task`（Enqueue 无返回值） / `IAsyncEnumerable<MessageEnvelope<T>>`（Dequeue） / `bool`（Acknowledge / NegativeAcknowledge，全部裸返回，不包 `Result<>`）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| 依赖模块     | `Queue/MessageEnvelope.cs` / `System.Text.Json`（序列化，统一复用 BCL 内置静态实例 [`JsonSerializerOptions.Web`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions.web)，[picker Q-serialization=A](#13-决策记录)，禁止 Provider 各自覆盖） / `System.Diagnostics.Activity`（`TraceParent` 自动捕获，[picker Q-traceparent-injection=A](#13-决策记录)） / System（`Guid` / `Environment.MachineName`）                                                                                                                                                                                                                                                                                                                                                          |
| 错误处理     | 全部上抛 BCL 异常（见 [§4.2 BCL 异常分类](#42-bcl-异常分类业务失败-vs-程序错误)）：`EnqueueAsync` 序列化失败 → [`JsonException`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonexception)；`DequeueAsync` 枚举中反序列化失败（毒消息） → `JsonException`（在 `MoveNextAsync` 处抛）；`consumerGroup` / `queueName` / `messageId` 为 null / empty → `ArgumentException`；`Acknowledge` / `NegativeAcknowledge` 竞争失败（`messageId` 未知 / 已过期） → 幂等返回值不抛异常；全部方法网络故障 → `IOException`；超时 → `TimeoutException`；取消 → `OperationCanceledException`                                                                                                                                                                                                    |
| 日志要求     | 实现层（两 Provider HD）在每个方法入口 / 出口写 OTel span，命名 `queue.<verb>`（`enqueue` / `dequeue` / `acknowledge` / `negative_acknowledge`）；6 个 Inkwell 私有字段（`queue.provider` / `queue.name` / `queue.message_id` / `queue.consumer_group` / `queue.delivery_count` / `queue.operation_outcome`）+ 5 个 OTel 标准 `exception.*` 字段（详 §4.3）；`queue.name` / `queue.message_id` 可能含业务上下文——实现层直接打，调用方在写额外业务日志前自行过滤（[HD-001 §7 安全](HD-001-Inkwell.Abstractions-foundation.md)）；`queue.operation_outcome` 值域见 §4.3                                                                                                                                                                                                                       |
| 测试要求     | `tests/core/Inkwell.Abstractions.Tests/Queue/IQueueProviderContractTests.cs`：契约测试（接口形态 ABI 锁定 via [`PublicApiAnalyzers`](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md)）；4 个方法签名 / 参数顺序 / 默认值 / 返回类型逐一验证；行为测试在 `tests/core/Inkwell.Providers.Contract/Queue/`（统一跨 Provider 家族契约包，与 [HD-002 §8](HD-002-Inkwell.Abstractions-persistence-port.md) / [HD-003 §8.3](HD-003-Inkwell.Abstractions-file-storage-port.md) / [HD-004 §8.3](HD-004-Inkwell.Abstractions-cache-port.md) / [file-structure.md 总体拓扑](../file-structure.md) 拓扑一致；[RISK-014](../../03-architecture/risk-analysis.md) / [RISK-015](../../03-architecture/risk-analysis.md)），两 Provider 跑同一套用例 |

### 3.2 `Queue/MessageEnvelope.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                        |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Queue/MessageEnvelope.cs`                                                                                                                                                                                                                                                                                    |
| 职责         | `DequeueAsync` 返回的消息载体；携带 [RISK-015](../../03-architecture/risk-analysis.md) 要求的跨进程 trace 字段 `TraceParent`，保 [REQ-014 trace 全链路](../../01-requirements/requirements.md) 跨服务不断链                                                                                                                                 |
| 对外接口     | `public sealed record MessageEnvelope<T> { public required string MessageId { get; init; } public required T Payload { get; init; } public required DateTimeOffset EnqueuedTime { get; init; } public required int DeliveryCount { get; init; } public string? TraceParent { get; init; } }`                                                |
| 内部函数或类 | record 自身；`TraceParent` 由 `EnqueueAsync` 实现层在写入时从 [`Activity.Current?.Id`](https://learn.microsoft.com/dotnet/api/system.diagnostics.activity.id)（W3C `ActivityIdFormat`）自动捕获（[picker Q-traceparent-injection=A](#13-决策记录)），本 DTO 不做捕获逻辑，仅承载字段                                                        |
| 输入数据     | 由 Provider 实现构造（`DequeueAsync` 内部从队列存储反序列化后填充）                                                                                                                                                                                                                                                                         |
| 输出数据     | `MessageEnvelope<T>` 实例                                                                                                                                                                                                                                                                                                                   |
| 依赖模块     | System（`DateTimeOffset`）                                                                                                                                                                                                                                                                                                                  |
| 错误处理     | DTO 自身不产生异常；反序列化失败由 `DequeueAsync` 在 `MoveNextAsync` 处抛 `JsonException`（详 §4.2）                                                                                                                                                                                                                                        |
| 日志要求     | DTO 自身不做日志；`DequeueAsync` 在 `queue.dequeue` span 输出 `queue.message_id` / `queue.delivery_count`；`TraceParent` 由消费方用于重建父 [`Activity`](https://learn.microsoft.com/dotnet/api/system.diagnostics.activity)（`Activity.Start(name, ActivityKind.Consumer, parentId: envelope.TraceParent)`），不作为独立 OTel 字段重复输出 |
| 测试要求     | `MessageEnvelopeTests.cs`：(1) 全部字段可正常构造；(2) `TraceParent` 为 `null` 时合法（Provider 未启用 tracing 场景）；(3) value equality（record 默认，`Payload` 类型 `T` 参与比较）                                                                                                                                                       |

### 3.3 `Queue/QueueOptions.cs`

> [HD-001 §3.11.1 `InkwellProvidersOptions`](HD-001-Inkwell.Abstractions-foundation.md#3111-optionsinkwellprovidersoptionscsf9-新增) 已承载 Provider 选择器 `Inkwell:Providers:Queue ∈ {"Channels","Redis"}`（默认值 `"Channels"`）；本 Options **不**重复承载 Provider 字段。[HD-001 §3.11](HD-001-Inkwell.Abstractions-foundation.md#311-optionsinkwelloptionscs) 根 `InkwellOptions.Queue` 槽位当前为占位 `QueueOptions` 类，本 HD 补全其字段。

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Queue/QueueOptions.cs`                                                                                                                                                                                                                                                                                                                                                               |
| 职责         | 队列端口详细配置；从 `appsettings.json` `"Inkwell:Queue"` 段绑定                                                                                                                                                                                                                                                                                                                                                    |
| 对外接口     | `public sealed class QueueOptions { [Range(1, 100)] public int MaxDeliveryAttempts { get; init; } = 3; [Range(1, 3600)] public int VisibilityTimeoutSeconds { get; init; } = 300; [Range(1, 168)] public int DlqRetentionHours { get; init; } = 24; public bool EnableSensitiveDataLogging { get; init; } = false; }`                                                                                               |
| 内部函数或类 | DataAnnotations 校验；`MaxDeliveryAttempts` 默认 3（[ADR-018](../../03-architecture/adr/ADR-018-queue-abstraction-channels-default.md) DLQ 阈值）；`VisibilityTimeoutSeconds` 默认 300（5 min，Redis `XCLAIM` 语义）；`DlqRetentionHours` 默认 24；[picker Q-dlq-policy=A](#13-决策记录) 三字段均允许环境覆写；重试退避（指数 1s ~ 60s + jitter）**不**在本 Options，属 `Inkwell.Queue.Redis` Provider 实现内部细节 |
| 输入数据     | 由 `IConfiguration` 绑定                                                                                                                                                                                                                                                                                                                                                                                            |
| 输出数据     | `QueueOptions` 实例（DI 通过 `IOptions<QueueOptions>` 注入）                                                                                                                                                                                                                                                                                                                                                        |
| 依赖模块     | `System.ComponentModel.DataAnnotations`                                                                                                                                                                                                                                                                                                                                                                             |
| 错误处理     | DataAnnotations 校验失败 → `OptionsValidationException`，host 兜底；Provider 不一致由 Builder DSL 抓 `InkwellBuilderException`                                                                                                                                                                                                                                                                                      |
| 日志要求     | DI 启动期 `IValidateOptions` 失败时输出 OTel `exception.type=Microsoft.Extensions.Options.OptionsValidationException`（[HD-001 §5.3](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)）                                                                                                                                                                                                             |
| 测试要求     | `QueueOptionsTests.cs`：默认值（3 / 300 / 24 / false）、`appsettings.json` 绑定、`[Range]` 边界（1 / 100，1 / 3600，1 / 168，越界）                                                                                                                                                                                                                                                                                 |

### 3.4 `Queue/QueueOptionsValidator.cs`

| 字段         | 内容                                                                                                                                                          |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Queue/QueueOptionsValidator.cs`                                                                                                |
| 职责         | `IValidateOptions<QueueOptions>` 实现；DataAnnotations 校验（无跨字段约束——三个可靠性参数互相独立，不同 CacheOptions 的 `MinTtl <= MaxTtl` 存在依赖关系）     |
| 对外接口     | `internal sealed class QueueOptionsValidator : IValidateOptions<QueueOptions> { public ValidateOptionsResult Validate(string? name, QueueOptions options); }` |
| 内部函数或类 | `Validator.TryValidateObject` DataAnnotations；Provider 特定连接 / 凭证不在本 Validator                                                                       |
| 输入数据     | `QueueOptions` 实例                                                                                                                                           |
| 输出数据     | `ValidateOptionsResult.Success` / `Fail(IEnumerable<string>)`                                                                                                 |
| 依赖模块     | `Microsoft.Extensions.Options` / `System.ComponentModel.DataAnnotations`                                                                                      |
| 错误处理     | 同 [HD-001 §3.12](HD-001-Inkwell.Abstractions-foundation.md#312-optionsinkwelloptionsvalidatorcs)，校验失败 → `Fail` 含全部消息                               |
| 日志要求     | 失败由 `OptionsValidationException` 抛出，host 打 fatal                                                                                                       |
| 测试要求     | `QueueOptionsValidatorTests.cs`：(1) DataAnnotations 边界合格；(2) 默认值（3 / 300 / 24 / false）通过；(3) 各字段单独越界（0、101、0、3601、0、169）均被拒    |

## 4. BCL 异常与日志（端口段补充 HD-001 §4）

> **错误处理路径**：本端口与业务命名空间统一采用裸 `Task<T>` + .NET BCL 异常。Inkwell 不自建错误码常量集 / 不自建 `Result<T>` / `Error` 抽象 / 不自建端口异常基类，仅保留 `InkwellConfigurationException` / `InkwellBuilderException` 用于 DI 装配期校验。具体 BCL 异常映射 + OTel `exception.*` 五字段详见下表与 [HD-001 §5.3](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)。

### 4.1 错误码

本端口**不分配** `INK-QUEUE-NNN` 错误码。与 [HD-002](HD-002-Inkwell.Abstractions-persistence-port.md) / [HD-003](HD-003-Inkwell.Abstractions-file-storage-port.md) / [HD-004](HD-004-Inkwell.Abstractions-cache-port.md) 最终态一致，错误语义全部走 BCL 异常类型表达 + OTel `exception.*` 五字段。

### 4.2 BCL 异常分类（业务失败 vs 程序错误）

> **2026-07-05 errata（B12）**：`JsonException` 原划入下方"业务失败 / 预期错误"档（不触发 P1），经 [design-review-report.md §15.3 B12](../design-review-report.md#15-hd-005-iqueueprovider-增量评审2026-07-05) 评审发现与 [HD-004 §4.2](HD-004-Inkwell.Abstractions-cache-port.md#42-bcl-异常分类业务失败-vs-程序错误) 矛盾——两 HD 共享同一序列化决策却对同一异常类型给出相反告警语义。本行由该评审发现后同步翻新：`JsonException` 改判为"程序错误"档（P1-P2 告警），与 HD-004 §4.2 保持一致，理由：序列化 / 反序列化失败通常意味着代码缺陷或数据损坏，需要人工介入。

按 [HD-001 §5.3 BCL 对照表](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表) 的分类语义：

- **预期返回值（不是异常，调用方按值判断）**：
  - `AcknowledgeAsync` `messageId` 未知 / 已过期 / 已确认过 → 返回 `false`（幂等）
  - `NegativeAcknowledgeAsync` `messageId` 未知 / 已过期 → 返回 `false`（幂等）
  - `DequeueAsync` 空队列 → 枚举挂起等待新消息（长轮询语义，不是错误）
- **业务失败 / 预期错误**（调用方应 try/catch 并按业务策略处理，**不**触发 P1 告警）：
  - 本端口 v1 暂无归入此档的异常类型（`JsonException` 已按上方 2026-07-05 errata 改判为"程序错误"档，见下）
- **程序错误 / 失血告警**（运维介入修复，P1 / P2 告警）：
  - `JsonException`：`DequeueAsync` 枚举中遇到无法反序列化为 `T` 的消息（毒消息）；`EnqueueAsync` 序列化 `T` 失败（含不支持的循环引用 / 非 JSON 友好类型）——序列化 / 反序列化失败通常意味着代码缺陷（业务侧变更 payload schema 未做兼容处理）或数据损坏，需要人工介入排查，与 [HD-004 §4.2](HD-004-Inkwell.Abstractions-cache-port.md#42-bcl-异常分类业务失败-vs-程序错误) 分级一致
  - `IOException`：DNS / TLS / network 失败 / Redis 连接断开 / 中途传输中断；触发方法：全部 4 个；message 应含具体根因（如 `"Connection to Redis endpoint failed"`）
  - `TimeoutException`：单次远端调用超过 Provider 子 Options 的超时配置；触发方法：全部 4 个
- **参数 / 取消错误**（调用方 bug，应在测试期暴露）：
  - `ArgumentException` / `ArgumentNullException`：`queueName` / `consumerGroup` / `messageId` 为 null / empty
  - `OperationCanceledException`：所有方法响应 `ct`（[HD-001 §4.3](HD-001-Inkwell.Abstractions-foundation.md#43-取消传播)）；`DequeueAsync` 的长轮询枚举在 `ct` 触发时立即终止枚举（不抛到调用方 `foreach` 外，遵循 [`IAsyncEnumerable` 取消惯例](https://learn.microsoft.com/dotnet/csharp/asynchronous-programming/generate-consume-asynchronous-stream#stop-the-enumeration)）

### 4.3 OTel span / 字段

每个方法在实现层（两 Provider HD）按 [picker Q-otel](#13-决策记录) 输出 span：

- `queue.enqueue` ← `EnqueueAsync`
- `queue.dequeue` ← `DequeueAsync`（每次 `MessageEnvelope<T>` 产出各起一个子 span，而非整个枚举生命周期一个 span）
- `queue.acknowledge` ← `AcknowledgeAsync`
- `queue.negative_acknowledge` ← `NegativeAcknowledgeAsync`

**Inkwell 私有字段**（6 个）：

- `queue.provider`（`Channels` / `Redis`）
- `queue.name`（`queueName`）
- `queue.message_id`（`EnqueueAsync` 场景为 Provider 生成的新 ID；其余三方法为传入 / 拉取到的 `MessageId`）
- `queue.consumer_group`（仅 `DequeueAsync` / `AcknowledgeAsync` / `NegativeAcknowledgeAsync` 填充）
- `queue.delivery_count`（仅 `DequeueAsync` 产出的每条消息填充，取自 `MessageEnvelope.DeliveryCount`）
- `queue.operation_outcome`：值域按方法区分——
  - `EnqueueAsync`：`success` / `failed` / `cancelled`
  - `DequeueAsync`：每条产出消息为 `delivered`；枚举因 `ct` 结束为 `cancelled`
  - `AcknowledgeAsync`：`acknowledged` / `not_found` / `cancelled`
  - `NegativeAcknowledgeAsync`：`negative_acknowledged` / `not_found` / `cancelled`

**OTel 标准字段**（5 个，按 [`exception.*` 语义约定](https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/)，仅异常路径填充）：

- `exception.type`（如 `System.IO.IOException` / `System.TimeoutException` / `System.Text.Json.JsonException`）
- `exception.message`
- `exception.stacktrace`
- `exception.escaped`
- `exception.id`（[`Guid.CreateVersion7()`](https://learn.microsoft.com/dotnet/api/system.guid.createversion7) 生成，便于 Grafana / Tempo 跨 span 关联，与 [HD-004 §4.3](HD-004-Inkwell.Abstractions-cache-port.md#43-otel-span--字段) 一致）

> **跨进程 trace 恢复**（[RISK-015](../../03-architecture/risk-analysis.md)）：`DequeueAsync` 的实现层在产出每条 `MessageEnvelope<T>` 前，应以 `envelope.TraceParent` 为 `parentId` 启动一个 `ActivityKind.Consumer` 的新 `Activity`（[`ActivitySource.StartActivity(name, ActivityKind.Consumer, parentId: envelope.TraceParent)`](https://learn.microsoft.com/dotnet/api/system.diagnostics.activitysource.startactivity)），使 `Inkwell.Worker` 侧的 `queue.dequeue` span 与 `Inkwell.WebApi` 侧的 `queue.enqueue` span 在同一条 trace 内呈父子关系，满足 [REQ-014 trace 全链路](../../01-requirements/requirements.md) 跨服务不断链要求。若 `TraceParent` 为 `null`（生产方未启用 tracing），消费方新起一条独立 trace，不视为错误。
>
> **W3C DefaultIdFormat 隐含依赖**（2026-07-05 [design-review-report.md §15.3 N19](../design-review-report.md#15-hd-005-iqueueprovider-增量评审2026-07-05) 评审发现后补齐）：上述机制假设进程内 [`Activity.DefaultIdFormat`](https://learn.microsoft.com/dotnet/api/system.diagnostics.activity.defaultidformat) = `ActivityIdFormat.W3C`（.NET 5+ 默认值，Inkwell 未修改）。若某端因自定义 `ActivitySource` / 第三方 instrumentation 将其改为 `Hierarchical`，`Activity.Current?.Id` 将不再是合法 W3C `traceparent` 格式——`MessageEnvelope.TraceParent` 字段仍非 `null` 但内容非法，[RISK-015](../../03-architecture/risk-analysis.md) 的跨进程 trace 串联会**静默失效**而非报错。H4 集成测试应断言 `envelope.TraceParent` 匹配 W3C `traceparent` 正则（`^[0-9a-f]{2}-[0-9a-f]{32}-[0-9a-f]{16}-[0-9a-f]{2}$`），而非仅断言字段非空（详 §8.3）。
>
> **PII 提示**：`queue.name` / `queue.message_id` 可能含业务上下文；这些字段允许进 OTel（Inkwell 自托管 Grafana 栈在边界内），调用方在写**额外**业务日志时应自行过滤（同 [HD-004 §4.3](HD-004-Inkwell.Abstractions-cache-port.md#43-otel-span--字段) PII 处理方式一致）。消息**载荷本身**（`EnqueueAsync` / `MessageEnvelope.Payload` 的 `T`）**不得**进入任何 OTel 字段——需要观测载荷规模时仅追加 `queue.payload_size_bytes`（长度而非内容）。

## 5. 公共约定继承（HD-001）

### 5.1 命名

- `IQueueProvider` ↔ [HD-001 §5.1](HD-001-Inkwell.Abstractions-foundation.md#51-命名) `I<Capability>Provider`
- `EnqueueAsync` / `DequeueAsync` / `AcknowledgeAsync` / `NegativeAcknowledgeAsync` ↔ §5.1 异步方法以 `Async` 结尾；四词是队列领域惯例命名，命名偏差详见 [§1.4](#14-与-hd-001-51--52-命名约定的一致性声明)
- `MessageEnvelope<T>` ↔ §5.1 DTO 命名（非 `<Action><Entity>Request/Response` 模式，因本 DTO 是跨方法复用的消息载体而非单方法请求 / 响应，与 [HD-004 `CacheEntryOptions`](HD-004-Inkwell.Abstractions-cache-port.md#51-命名) 同类偏差记录方式）
- `QueueOptions` ↔ §5.1 `<Provider>Options`

### 5.2 签名

- 4 个方法走裸 `Task` / `Task<bool>` / `IAsyncEnumerable<MessageEnvelope<T>>` + BCL 异常，↔ [HD-001 §5.3 BCL 对照表](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)
- `DequeueAsync<T>` 是流式长轮询语义 ↔ [HD-001 §5.2 流式签名约定](HD-001-Inkwell.Abstractions-foundation.md#52-签名)（`IAsyncEnumerable<T>` + `[EnumeratorCancellation]`）
- `AcknowledgeAsync` / `NegativeAcknowledgeAsync` 的 `bool` 返回值保留幂等语义（同 [HD-004 §5.2](HD-004-Inkwell.Abstractions-cache-port.md#52-签名) `RemoveAsync` / `ReleaseLockAsync` 风格）
- `CancellationToken ct = default` 全 4 方法必填 ↔ [HD-001 §4.3](HD-001-Inkwell.Abstractions-foundation.md#43-取消传播)

### 5.3 错误处理

- 业务失败 / 预期错误 → 本端口 v1 暂无归入此档的异常类型（`JsonException` 已按 [§4.2 2026-07-05 errata（B12）](#42-bcl-异常分类业务失败-vs-程序错误) 改判为程序错误）
- 程序错误 / 失血告警 → BCL 程序异常（`IOException` / `TimeoutException` / `JsonException`）；触发运维告警
- 参数错误 → `ArgumentException` / `ArgumentNullException`
- 幂等确认型返回 → `AcknowledgeAsync` / `NegativeAcknowledgeAsync` 返回值本身表达"已确认 / 未知消息"语义，不抛异常
- 取消 → `OperationCanceledException`
- 实现层用 [`ActivitySource.StartActivity`](https://learn.microsoft.com/dotnet/api/system.diagnostics.activitysource.startactivity) 创建 span 后，**异常路径**自动用 `Activity.RecordException` 或 `Activity.SetStatus(ActivityStatusCode.Error, message)` 写入 `exception.*` 五字段（详 §4.3）
- **跨进程 trace 传递**（[RISK-015](../../03-architecture/risk-analysis.md)）：`EnqueueAsync` 自动捕获 `Activity.Current?.Id` 写入 `MessageEnvelope.TraceParent`；`DequeueAsync` 消费侧以该值为 `parentId` 重建 `Activity`（详 §4.3），异常信息不跨进程重建 `Exception` 实例，仅通过各自进程内的 OTel `exception.*` 字段独立记录（遵循 [HD-001 §4.1](HD-001-Inkwell.Abstractions-foundation.md#41-错误表达机制)"跨进程序列化不重建 Exception 实例"约定）

## 6. Builder DSL 钩子（给 Provider HD 的契约）

每个 Provider csproj 提供唯一入口扩展方法：

```csharp
// providers/Queue/Inkwell.Queue.Redis/RedisQueueBuilderExtensions.cs
public static class RedisQueueBuilderExtensions
{
    public static IInkwellBuilder UseRedisQueue(
        this IInkwellBuilder builder,
        string connectionString);
}

// src/core/Inkwell.Core/Queue/ChannelsQueueBuilderExtensions.cs
public static class ChannelsQueueBuilderExtensions
{
    public static IInkwellBuilder UseChannelsQueue(
        this IInkwellBuilder builder,
        Action<ChannelsQueueOptions>? configure = null);
}
```

每个扩展方法**必须**：(1) 校验入参非 null；(2) 调用 `builder.Services.AddSingleton<IQueueProvider, XxxQueueProvider>()`；(3) 注册 `IValidateOptions<QueueOptions>` + 各自 Provider 子 Options 的 Validator；(4) 与 `InkwellProvidersOptions.Queue` 取值交叉校验（不一致抛 `InkwellBuilderException(message)`，[HD-001 §3.13](HD-001-Inkwell.Abstractions-foundation.md) 锁定的 BCL 程序错误子类）；(5) 返回 `builder`。

> **与 Cache / FileStorage 端口的一处差异**：[ADR-018 §决策](../../03-architecture/adr/ADR-018-queue-abstraction-channels-default.md) declares "`AddInkwell().Build()` 默认注入 `ChannelsQueueProvider`"——即 `IQueueProvider` 的 dev 默认实现由 `InkwellBuilder.Build()`（[HD-001 §3.9](HD-001-Inkwell.Abstractions-foundation.md#39-builderinkwellbuildercs)）在未显式调用 `UseChannelsQueue()` / `UseRedisQueue()` 时自动注册，不同于 Cache / FileStorage 端口"必须显式调用 `Use*()`"的模式。`UseChannelsQueue()` 仅用于需要自定义 `ChannelsQueueOptions`（如自定义 channel 容量上限）的场景；该差异的具体落地由 `Inkwell.Core` 独立 HD 起草时实现，本 HD 仅记录契约期望。

## 7. 性能 / 安全 / 可观测性

### 7.1 性能预算（[picker Q-perf-budget=A 宽松档](#13-决策记录)）

| 方法                       | 预算类型        | P50    | P99     | 备注                                     |
| -------------------------- | --------------- | ------ | ------- | ---------------------------------------- |
| `EnqueueAsync`             | facade overhead | < 20ms | < 100ms | Redis 内网 RTT 量级                      |
| `DequeueAsync`（单条产出） | 消息可见延迟    | < 1s   | < 5s    | 从 `EnqueueAsync` 完成到消费方收到该消息 |
| `AcknowledgeAsync`         | facade overhead | < 20ms | < 100ms | `XACK` 单次往返                          |
| `NegativeAcknowledgeAsync` | facade overhead | < 20ms | < 100ms | 释放 pending entry + 触发提前重投        |

> 上述为 facade overhead / 消息可见延迟（端口实现自身代码 + 一次远端调用 RTT 或 Redis Streams 内部调度延迟），`ChannelsQueueProvider` 场景应远低于该预算（进程内操作，消息可见延迟趋近于 0）。

### 7.2 安全

- `QueueOptions.EnableSensitiveDataLogging` 默认 `false`；启用后仅追加 `queue.payload_size_bytes`（大小而非内容）——**消息载荷本身永不进入 OTel**（详 [§4.3 PII 提示](#43-otel-span--字段)）
- 凭证（Redis `ConnectionString` / 密码）由 `providers/Queue/Inkwell.Queue.Redis` 自己的子 Options 承载，**不**在本 `QueueOptions`；走 [K8s Secret](https://kubernetes.io/docs/concepts/configuration/secret/) / Compose `.env`（[OQ-A006 closed §B](../../03-architecture/open-questions-arch.md)，v1 不引 Azure Key Vault）
- `MessageEnvelope.TraceParent` 是 W3C 标准公开字段（不含敏感信息，仅 trace-id / parent-id / flags），可安全跨进程传递，无需额外加密
- Redis 端口暴露面由 [ADR-005](../../03-architecture/adr/ADR-005-deployment-docker-compose-aks.md) 部署拓扑约束（dev Compose 内网 / prod AKS ClusterIP 或 Azure Cache for Redis Private Endpoint），本 HD 不重复约束

### 7.3 可观测性

- 6 私有 + 5 OTel 标准 `exception.*` 字段进 OTel；本 HD 不锁告警规则（H4 [TestCaseAuthor](../../../.github/agents/h4-test-case-author.agent.md) 反推时锁），但建议告警维度：
  - `exception.type` ∈ {`System.IO.IOException`, `System.TimeoutException`} 速率 > 5/min → P1（连接 / 超时类失血，直接关联 [RISK-014](../../03-architecture/risk-analysis.md) Redis 单点风险）
  - `queue.operation_outcome = negative_acknowledged` 速率异常升高 → P2（业务侧下游处理持续失败，可能触发 DLQ 堆积）
  - v1 **必发** `queue_depth`（[ADR-018](../../03-architecture/adr/ADR-018-queue-abstraction-channels-default.md) 锁定，来自 [`XLEN`](https://redis.io/docs/latest/commands/xlen/)）；`queue_consume_latency_p95` / `queue_dlq_count` / `queue_consumer_lag` / `queue_redelivery_count` / `queue_consumer_active` 五项进 [RISK-014 残余风险](../../03-architecture/risk-analysis.md)，prod 上线前补齐，本 HD 不重复锁定
  - **跨服务 trace correlation**（[RISK-015](../../03-architecture/risk-analysis.md)）：Grafana Tempo 应能通过 `MessageEnvelope.TraceParent` 串联 `service.name = inkwell-webapi`（enqueue）与 `service.name = inkwell-worker`（dequeue）两侧 span；H4 集成测试须验证该串联关系存在

## 8. 测试要求

### 8.1 单元测试（本 HD 范围内）

- 测试项目：`tests/core/Inkwell.Abstractions.Tests/Queue/`（与 HD-001 同 csproj，[MSTest.Sdk 4.x](https://github.com/microsoft/testfx) + MTP runner）
- 每个文件至少一个 `*Tests.cs` 配对（见 §3 各小节"测试要求"）
- 覆盖率门槛：`MessageEnvelope` ≥ 95%；`QueueOptions` + Validator ≥ 90%；`IQueueProviderContractTests` 仅锁 ABI ≥ 100%

### 8.2 契约测试

- 接口 ABI 用 [`PublicApiAnalyzers`](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) 锁定
- `IQueueProvider` 形态变更 → 需新建 ADR + 影响 2 个 Provider HD

### 8.3 集成测试

- 本 HD **不**起集成测试（端口层无外部依赖）
- 两 Provider 行为测试在 `tests/core/Inkwell.Providers.Contract/Queue/`（统一跨 Provider 家族契约包，与 [HD-002 §8](HD-002-Inkwell.Abstractions-persistence-port.md) / [HD-003 §8.3](HD-003-Inkwell.Abstractions-file-storage-port.md) / [HD-004 §8.3](HD-004-Inkwell.Abstractions-cache-port.md) / [file-structure.md 总体拓扑](../file-structure.md) 拓扑一致；[RISK-014](../../03-architecture/risk-analysis.md) / [RISK-015](../../03-architecture/risk-analysis.md)），由 Provider HD 联合起草；CI matrix 跑 Channels / [Testcontainers.Redis](https://testcontainers.com/modules/redis/) 两 Provider 同一套用例，含：
  - crash recovery（worker SIGKILL 后未 ack 消息在 visibility timeout 后被重插）
  - fairness（多副本 worker 并发消费同一 queue，跨副本 ack 顺序不出现永久偏斜）
  - DLQ（连续 `MaxDeliveryAttempts` 次失败后消息进入死信）
  - `MessageEnvelope.TraceParent` 跨进程 trace 串联（[RISK-015](../../03-architecture/risk-analysis.md)，[AGENTS.md §3.4](../../../AGENTS.md) 要求的 enqueue (WebApi) → consume (Worker) → ack 跨服务集成用例雏形，具体用例设计留 H4）；**H4 应断言** `envelope.TraceParent` 匹配 W3C `traceparent` 格式正则（`^[0-9a-f]{2}-[0-9a-f]{32}-[0-9a-f]{16}-[0-9a-f]{2}$`），而非仅断言字段非空（2026-07-05 [design-review-report.md §15.3 N19](../design-review-report.md#15-hd-005-iqueueprovider-增量评审2026-07-05) 评审发现后补齐，详 §4.3）

### 8.4 BannedSymbols（CI 强制）

- `Inkwell.Abstractions.Queue.*` 禁用引入 `StackExchange.Redis.*` 等具体 SDK 命名空间（写在 `BannedSymbols.txt`，违反 → 编译阻塞）

## 9. 部署 / 配置

`Inkwell.Abstractions.csproj` 与端口层一同打镜像（无独立部署）。`appsettings.json` 顶层段：

```json
{
  "Inkwell": {
    "Providers": {
      "Queue": "Redis"
    },
    "Queue": {
      "MaxDeliveryAttempts": 3,
      "VisibilityTimeoutSeconds": 300,
      "DlqRetentionHours": 24,
      "EnableSensitiveDataLogging": false,
      "Redis": {
        "ConnectionString": "..."
      }
    }
  }
}
```

> Provider 特定子段（`Queue:Redis` / `Queue:Channels`，即 `Inkwell:Queue:Redis` / `Inkwell:Queue:Channels` 嵌套段）由各 Provider HD 起草时锁定。序列化选项统一锁定为 BCL 内置静态实例 `System.Text.Json.JsonSerializerOptions.Web`，不通过 `appsettings.json` 覆盖（同 [HD-004 §9](HD-004-Inkwell.Abstractions-cache-port.md#9-部署--配置) 决策一致，Owner"能不新增配置项就不新增"原则）。

## 10. CI 自检命令（grep 列表）

| 编号 | 检查项                                                                                                                   | 命令（CI [GitHub Actions](https://docs.github.com/actions) 工作流引用）                                                                                                                                                            |
| ---- | ------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Q1   | 业务命名空间禁直接 `using StackExchange.Redis`                                                                           | `rg -n -e 'using\s+StackExchange\.Redis' src/core/Inkwell.Core/` 期望 0 行                                                                                                                                                         |
| Q2   | `IQueueProvider` 接口签名稳定                                                                                            | [PublicApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) `PublicAPI.Shipped.txt` diff                                                                          |
| Q3   | 端口层无 `Task<Result<` 残留（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)） | `rg -n -e 'Task<Result<' -e 'Task<Result>' src/core/Inkwell.Abstractions/Queue/` 期望 0 行                                                                                                                                         |
| Q4   | 业务命名空间禁 `Result<T>` / `ErrorCodes` 引用                                                                           | `rg -n -e 'Common\.Result' -e 'Common\.Error' -e 'ErrorCodes\.' src/core/Inkwell.Core/ src/core/Inkwell.WebApi/ src/core/Inkwell.Worker/ providers/Queue/Inkwell.Queue.Redis/` 期望 0 行                                                 |
| Q5   | 消息载荷不进 OTel（仅大小字段）                                                                                          | `rg -n -e '"queue\.payload"' -e 'queue\.payload\s*=' src/core/ providers/Queue/Inkwell.Queue.Redis/` 期望 0 行（仅 `queue.payload_size_bytes` 允许）                                                                                     |
| Q6   | OTel span 字段名一致                                                                                                     | `rg -n -e '"queue\.provider"' -e '"queue\.name"' -e '"queue\.message_id"' -e '"queue\.consumer_group"' -e '"queue\.delivery_count"' -e '"queue\.operation_outcome"' src/core/ providers/Queue/Inkwell.Queue.Redis/` 期望全部在实现层覆盖 |
| Q7   | `MessageEnvelope.TraceParent` 字段存在（[RISK-015](../../03-architecture/risk-analysis.md) 硬约束）                      | `rg -n 'TraceParent' src/core/Inkwell.Abstractions/Queue/MessageEnvelope.cs` 期望 ≥ 1 行                                                                                                                                           |

## 11. 待补 / 待评审

以下 4 条为本轮起草后仍待后续 HD / H4 明确的开放事项，均已在 §1.2 范围声明为"不在本 HD 内"，此处仅作追踪索引：

- **Redis 实例复用策略**：`Inkwell.Queue.Redis` 与 `Inkwell.Cache.Redis` 是否复用同一 Redis 实例（不同 db number）vs 独立部署——留 `Inkwell.Queue.Redis` Provider HD 决定（[risk-analysis.md RISK-014](../../03-architecture/risk-analysis.md) 建议独立部署，非本 HD 强制）
- **重试退避算法参数**：指数退避 1s ~ 60s + jitter 的具体实现（是否可配置、jitter 算法选型）——留 `Inkwell.Queue.Redis` Provider HD 决定，本 HD `QueueOptions` 不预留该字段（[picker Q-dlq-policy=A](#13-决策记录) 范围仅覆盖 `MaxDeliveryAttempts` / `VisibilityTimeoutSeconds` / `DlqRetentionHours` 三项）
- **跨服务集成测试具体用例设计**：[AGENTS.md §3.4 RISK-015](../../../AGENTS.md) 要求的 enqueue (WebApi) → consume (Worker) → ack 全链路用例、`Inkwell.Triggers` [REQ-011](../../01-requirements/requirements.md) / KB ingest [REQ-009](../../01-requirements/requirements.md) 两类典型异步场景的具体测试步骤——留 H4 [TestCaseAuthor](../../../.github/agents/h4-test-case-author.agent.md) 反推详细用例，本 HD 仅锁定 `MessageEnvelope.TraceParent` 承载字段确保串联可行
- **`MessageEnvelope` schema 演进规则**（[RISK-015 缓解方案 #5](../../03-architecture/risk-analysis.md#risk-015-webapi--worker-双进程版本漂移与-otel-双-source)"schema 兼容性 SOP"）：`MessageEnvelope<T>` 字段未来新增 / 废弃的向后兼容规则（新字段必须可选、废弃字段至少保留两个 release）——留 `Inkwell.Queue.Redis` Provider HD 起草时锁定（该规则本质是 Redis Streams 消费者端向后兼容性的实现细节），本 HD §3.2 仅锁定 v1 首次交付的 5 字段最小集，不预判未来演进路径（2026-07-05 [design-review-report.md §15.3 N17](../design-review-report.md#15-hd-005-iqueueprovider-增量评审2026-07-05) 评审发现后补齐）

## 12. 跨模块章节贡献

本 HD 在以下跨模块文件中追加一级章节 `## Inkwell.Abstractions.Queue`：

- `docs/04-detailed-design/file-structure.md` — 新增 `Inkwell.Abstractions/Queue/` 子目录树 + 累计文件计数更新
- `docs/04-detailed-design/database-design.md` — **不贡献**（端口层不直接接 DB）

> 跨模块章节追加由本 HD 起草后**立即同步**到对应文件（**只追加**不改其他模块章节）。

## 13. 决策记录

### 13.1 起草期 picker 决策（2026-07-05）

| 字段                    | 选定值                                                                                                     | picker 时间 |
| ----------------------- | ---------------------------------------------------------------------------------------------------------- | ----------- |
| Q-scope                 | A：4 方法（Enqueue / Dequeue / Acknowledge / NegativeAcknowledge），在 ADR-018 三动作最小集上追加显式 Nack | 2026-07-05  |
| Q-dequeue-shape         | A：`IAsyncEnumerable<MessageEnvelope<T>>` 流式拉取，`consumerGroup` 必填 + `consumerName` 可选自动生成     | 2026-07-05  |
| Q-envelope-shape        | A：5 字段最小集（MessageId / Payload / EnqueuedTime / DeliveryCount / TraceParent），不含 TraceState       | 2026-07-05  |
| Q-traceparent-injection | A：`EnqueueAsync` 内部自动从 `Activity.Current?.Id` 捕获，调用方无需手工传参                               | 2026-07-05  |
| Q-serialization         | A：复用 HD-004 决策，`System.Text.Json` + `JsonSerializerOptions.Web`                                      | 2026-07-05  |
| Q-dlq-policy            | A：`MaxDeliveryAttempts` / `VisibilityTimeoutSeconds` / `DlqRetentionHours` 暴露为 `QueueOptions` 字段     | 2026-07-05  |
| Q-ack-return-shape      | A：`Task<bool>`，`false` = `messageId` 未知 / 已过期                                                       | 2026-07-05  |
| Q-perf-budget           | A：宽松档，`Dequeue` 用消息可见延迟预算（P50 < 1s / P99 < 5s）                                             | 2026-07-05  |
| Q-queuename-convention  | A：不锁，端口层只接受 `string queueName`，调用方自行拼接                                                   | 2026-07-05  |
| Q-otel                  | A：`queue.<verb>` + 6 个私有字段                                                                           | 2026-07-05  |

### 13.2 候选与放弃理由

- **Q-scope**：备选 B（仅 3 方法，无显式 Nack）被否决——业务侧判定永久失败（如消息 payload 结构性损坏、下游资源已被删除）时若只能等待 visibility timeout（5 min）才被重投进而再次失败进入 DLQ 计数，白白消耗一次 5 分钟等待与一次无意义重试；显式 `NegativeAcknowledgeAsync` 让业务侧能立即释放 pending entry 加速失败反馈，成本仅为多一个方法
- **Q-dequeue-shape**：备选 B（批量拉取 `Task<IReadOnlyList<MessageEnvelope<T>>>`）被否决——与 `Inkwell.Worker` 的 `BackgroundService.ExecuteAsync` 长驻循环模型不匹配，`IAsyncEnumerable` 的 `await foreach` 更贴近该场景；批量拉取需要业务侧自行管理轮询间隔与空批次处理，增加样板代码
- **Q-envelope-shape**：备选 B（额外携带 `TraceState`）被否决——v1 场景无 vendor-specific tracing 附加信息的实际需求，[W3C tracestate](https://www.w3.org/TR/trace-context/#tracestate-header) 主要用于多 vendor 混合场景，Inkwell 自托管单一 OTel 栈不需要；提前引入属过度设计
- **Q-traceparent-injection**：备选 B（显式 `traceParent` 可选参数）被否决——业务命名空间调用 `EnqueueAsync` 时不应关心 trace 传播细节，自动捕获与 [OTel 自动埋点惯例](https://opentelemetry.io/docs/languages/net/automatic/)（如 `HttpClient` / `SqlClient` instrumentation 自动传播 trace context）一致，显式参数会让每个调用点多一份样板代码且容易被遗漏
- **Q-serialization**：不设自定义方案候选——与 HD-004 保持同一序列化策略是明确的一致性要求，跨端口序列化格式分裂会增加运维与调试认知负担，无收益
- **Q-dlq-policy**：备选 B（不暴露，Provider 内部硬编码）被否决——[ADR-018](../../03-architecture/adr/ADR-018-queue-abstraction-channels-default.md) 虽锁定了 v1 默认数字，但不同部署环境（如 staging 需要更短的 DLQ 保留期以加速排障迭代）可能需要覆写；暴露为 Options 字段且默认值与 ADR 数字一致，零额外运维负担但保留调优空间，与 `CacheOptions` TTL 模式一致
- **Q-ack-return-shape**：不设对立候选——`Task`（void 语义）会让调用方无法区分"确认成功"与"消息本已不存在"两种正常场景，不利于业务侧判断是否需要补偿逻辑；`Task<bool>` 零额外成本换取更明确的语义
- **Q-perf-budget**：备选 B（紧凑档 `Enqueue` P50 < 10ms）未选——理由同 [HD-004 Q-perf-budget](HD-004-Inkwell.Abstractions-cache-port.md#132-候选与放弃理由)：v1 未锁定 Redis 部署是否同 region，宽松档更稳妥
- **Q-queuename-convention**：备选 B（锁 `InkwellQueues` 常量集）被否决——与 [HD-003 picker Q3](HD-003-Inkwell.Abstractions-file-storage-port.md#13-关键决策摘要) / [HD-004 picker Q-key-convention](HD-004-Inkwell.Abstractions-cache-port.md#13-决策记录) 风格保持一致，端口层保持薄，队列名语义留业务侧决定（如 `Inkwell.Core.KnowledgeBase` 决定 `kb-ingest`，`Inkwell.Core.Triggers` 决定 `trigger-fanout`）
- **Q-otel**：不设对立候选——与 [HD-004 §4.3](HD-004-Inkwell.Abstractions-cache-port.md#43-otel-span--字段) `cache.<verb>` 风格保持族内一致，降低跨端口可观测性认知成本

### 13.3 评审后修订记录（2026-07-05，[design-review-report.md §15](../design-review-report.md#15-hd-005-iqueueprovider-增量评审2026-07-05)）

- **B12（blocking，已修复）**：Owner picker 拍板方向为"HD-005 对齐 HD-004"——`JsonException` 由"业务失败 / 预期错误"档改判为"程序错误 / 失血告警（P1-P2）"档，与 [HD-004 §4.2](HD-004-Inkwell.Abstractions-cache-port.md#42-bcl-异常分类业务失败-vs-程序错误) 保持一致。落地位置：§4.2（分类 + errata 说明）、§5.3（错误处理摘要同步）。因 HD-005 仍为 `status: draft`，直接修改正文，不走 HD-001 §13 式的 errata 链式记录
- **N16（non-blocking，已修复）**：§2 csproj 依赖白名单补 `System.Diagnostics.Activity`，与 file-structure.md 转述对齐
- **N17（non-blocking，已修复）**：§11 补一条 `MessageEnvelope` schema 演进规则的移交声明，指向 `Inkwell.Queue.Redis` Provider HD
- **N19（non-blocking，已修复）**：§4.3 补 `Activity.DefaultIdFormat = W3C` 隐含依赖的显式声明；§8.3 补 H4 应断言 `TraceParent` 格式而非仅非空的测试要求
- **N18（non-blocking，暂不处理）**：ADR-019"WebApi 仅注册 enqueue 侧"接口层固化，按评审建议留到 `Inkwell.WebApi` / `Inkwell.Worker` 各自 HD 起草时处理，本次不改动 HD-005 §3.1 接口形态
