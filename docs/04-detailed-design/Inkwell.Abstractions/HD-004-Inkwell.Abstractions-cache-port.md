---
id: HD-004
title: Inkwell.Abstractions 详细设计 — Cache Port（ICacheProvider facade + Options）
stage: H3
status: draft
reviewers: []
upstream:
  - REQ-010
  - REQ-013
  - ADR-002
  - ADR-005
  - ADR-016
  - ADR-017
  - HD-001
  - ADR-023
---

<!-- markdownlint-disable MD060 -->
<!-- 中文 + 英文混排长表格在 markdownlint 列宽计算下字面对齐 ≠ 视觉对齐（详 /memories/markdown-lint.md，与 tech-selection.md / risk-analysis.md 同处理方式），表格仍按 docs-style §3 视觉对齐维护，机械 MD060 不予执行。 -->

> **错误处理约定**（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) accepted by Inkwell 2026-05-11，含 [errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) 废错误码、[errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 删 `Result<T>` / `Error` 抽象）：端口层与业务层统一采用裸 `Task<T>` + .NET BCL 异常。Inkwell **不自建 `Result<T>` / `Error` 抽象** / 不自建错误码机制 / 不自建端口层异常基类；仅保留 `InkwellConfigurationException` / `InkwellBuilderException` 两个程序错误子类用于 DI 装配期校验。本 HD 起草时 ADR-023 已是最终态规约，全部签名从第一版直接采用裸 `Task<T>` + BCL 异常，不存在"先 Result 后 errata"的历史包袱。具体错误语义走 BCL 异常类型表达 + OTel [`exception.*` 五字段](https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/)，详 [HD-001 §5.3 BCL 对照表](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)。
>
> **范围切片**：本 HD 覆盖 `Inkwell.Abstractions/Cache/` 子层——`ICacheProvider` facade（7 方法：Get / Set / Remove / Exists / Increment / TryAcquireLock / ReleaseLock，[ADR-016 §决策](../../03-architecture/adr/ADR-016-cache-provider-redis.md) 列出的能力面）、`CacheEntryOptions` DTO、`CacheOptions` + Validator。**不**实现 Provider 行为（`RedisCacheProvider` 在 `providers/Inkwell.Cache.Redis/` 独立 HD 起草；`InMemoryCacheProvider` 在 `Inkwell.Core` 独立 HD 起草）、**不**锁 Key 命名规范强制机制（[picker Q-key-convention=A](#13-决策记录)：端口层只接受 `string key`，业界 `{tenant}:{module}:{purpose}:{id}` 约定留文档层建议，业务命名空间各自拼接）。
>
> **跨 HD 关联**：本 HD 与 [HD-001 foundation](HD-001-Inkwell.Abstractions-foundation.md)（Builder DSL / OTel 字段 / `InkwellProvidersOptions.Cache` 选择器槽位）+ [HD-003 FileStorage port](HD-003-Inkwell.Abstractions-file-storage-port.md)（同级端口模板，OTel span 命名风格对齐）形成 Abstractions 端口族的第三张 HD。

## 1. 模块概述

### 1.1 职责

- `ICacheProvider` facade（§3.1）：定义业务命名空间访问缓存层的统一入口；Get / Set / Remove / Exists / Increment / TryAcquireLock / ReleaseLock 共 7 个方法，覆盖 [ADR-016 §决策](../../03-architecture/adr/ADR-016-cache-provider-redis.md) 列出的全部 v1 使用场景（Public API rate limit 计数、Agent 会话短期状态、Skill registry / Agent 配置元数据缓存、简单互斥锁）
- `CacheEntryOptions` DTO（§3.2）：`SetAsync` 的强制 TTL 载体（[picker Q-ttl-policy=A](#13-决策记录)：不提供无限期缓存选项）
- `CacheOptions` + Validator（§3.3 ~ §3.4）：TTL 上下限、锁 TTL 默认值、`EnableSensitiveDataLogging` 开关

### 1.2 范围

- **在内**：facade 接口 + DTO + Options
- **不在内**：
  - 两 Provider 实现（`RedisCacheProvider` 在 `providers/Inkwell.Cache.Redis/` 独立 HD；`InMemoryCacheProvider` 在 `Inkwell.Core` 独立 HD，[ADR-016](../../03-architecture/adr/ADR-016-cache-provider-redis.md)）
  - Key 命名规范的强制实现（`{tenant}:{module}:{purpose}:{id}` 留业务命名空间自行拼接，[picker Q-key-convention=A](#13-决策记录)）
  - 分布式锁续约 / 心跳机制——v1 仅 [ADR-016 §决策](../../03-architecture/adr/ADR-016-cache-provider-redis.md) 声明的 "`SET NX EX` 简单锁"，不支持锁持有期间自动延长 TTL；持锁方需在业务侧保证临界区耗时 < 锁 TTL，v2 backlog
  - Public API rate limit 的 [`PartitionedRateLimiter`](https://learn.microsoft.com/dotnet/api/system.threading.ratelimiting.partitionedratelimiter) 适配层（业务侧 `Inkwell.Core.PublicApi` HD 起草时消费本端口的 `IncrementAsync`）
  - Agent 会话短期状态的具体缓存键设计（业务侧 `Inkwell.Core.Conversations` / `.Agents` HD 起草时决定）

### 1.3 关键决策摘要

> 全部由 2026-07-05 picker 拍板，决策证据见本节"出处"列；详细候选与放弃理由见 [§13 决策记录](#13-决策记录)。

- **Q-scope**：`ICacheProvider` 含 6 类能力共 7 方法（Get / Set / Remove / Exists / Increment / TryAcquireLock + ReleaseLock），覆盖 [ADR-016](../../03-architecture/adr/ADR-016-cache-provider-redis.md) 列出的全部 v1 场景，不缩窄到"仅 4 方法留业务侧自行拼原子性"
- **Q-lock-shape**：`TryAcquireLockAsync` 返回 `string? lockToken`（`null` = 未获取）；`ReleaseLockAsync(key, lockToken)` 显式释放；Provider 内部用 `SET NX EX` + Lua CAS 防误删他人持有的锁
- **Q-ttl-policy**：`SetAsync` **强制**通过 `CacheEntryOptions` 指定 `AbsoluteExpirationRelativeToNow`；不提供"无限期缓存"选项，直接呼应 [RISK-012](../../03-architecture/risk-analysis.md) 缓存一致性 / 内存无限增长风险
- **Q-serialization**：泛型值统一 [`System.Text.Json`](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/overview)（`JsonSerializer`），零额外三方依赖，与 [ADR-002](../../03-architecture/adr/ADR-002-backend-runtime-dotnet10-aspnetcore.md) .NET 生态一致
- **Q-key-convention**：不在端口层强制 Key 命名——`ICacheProvider` 只接受 `string key`；[ADR-016 §Key 命名约定](../../03-architecture/adr/ADR-016-cache-provider-redis.md) `{tenant}:{module}:{purpose}:{id}` 仅作文档层建议，与 [HD-003 picker Q3](HD-003-Inkwell.Abstractions-file-storage-port.md) 不锁容器名常量集风格一致
- **Q-increment-behavior**：`IncrementAsync` 对不存在的 key 自动创建并从 0 开始累加（Redis `INCRBY` 原生语义），可选 `ttl` 参数**仅在创建时**生效
- **Q-perf-budget**：宽松档，全部 7 方法 facade overhead P50 < 20ms / P99 < 100ms（Redis 内网 RTT 量级），与 [HD-003 §7.1](HD-003-Inkwell.Abstractions-file-storage-port.md) 风格对齐
- **Q-otel**：OTel span 命名 `cache.<verb>`（get / set / remove / exists / increment / acquire_lock / release_lock）+ 私有字段 `cache.provider` / `cache.key` / `cache.ttl_seconds` / `cache.operation_outcome`
- **Q-cache-entry-options**：`CacheEntryOptions` 仅支持绝对过期 `AbsoluteExpirationRelativeToNow`；v1 不支持滑动过期（`SlidingExpiration`），降低实现复杂度（滑动过期需每次 Get 后 `EXPIRE` 刷新，增加往返）
- **Q-ttl-bounds**：`CacheOptions.MinTtlSeconds = 1` / `MaxTtlSeconds = 86400`（24h）/ `DefaultLockTtlSeconds = 30`

### 1.4 与 HD-001 §5.1 / §5.2 命名约定的一致性声明

[HD-001 §5.1](HD-001-Inkwell.Abstractions-foundation.md#51-命名) 锁定的命名前缀语义中，`Get*Async` 隐含"实体不存在则抛 `KeyNotFoundException`"（[HD-001 §5.2](HD-001-Inkwell.Abstractions-foundation.md#52-签名)）。本 HD 的 `GetAsync<T>` **显式偏离**该前缀语义，改遵循缓存领域惯例——与 [`IDistributedCache.GetAsync`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.caching.distributed.idistributedcache.getasync) / [`IMemoryCache.TryGetValue`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.caching.memory.imemorycache.trygetvalue) 一致：**缓存未命中是预期返回值，不是错误**，返回 `T?`（null 表示未命中），不抛异常。这不是对 HD-001 的违反，而是"Cache 领域 Get 前缀例外"——本 HD 是该例外的首次落地记录，供后续端口（如需要缓存类语义）参照。

`RemoveAsync` 语义等价于 HD-001 §5.2 的 `Delete*Async`（幂等：`true` = 实际删除 / `false` = 本不存在），命名沿用 [ADR-016 §决策](../../03-architecture/adr/ADR-016-cache-provider-redis.md) 原文用词"Remove"而非"Delete"，两者语义等价、仅用词差异，不视为偏离。

`IncrementAsync` / `TryAcquireLockAsync` / `ReleaseLockAsync` 三个方法引入的"原子计数器"与"显式锁 pair"语义不在 [HD-001 §5.1 / §5.2](HD-001-Inkwell.Abstractions-foundation.md) 原始命名枚举内——本 HD 是 Abstractions 端口族中首次出现该类语义的 HD，作为**补充规则**记录：

- `Increment*Async` → `Task<long>`，返回操作后的新值；不存在的 key 视为初值 0（[picker Q-increment-behavior=A](#13-决策记录)），不视为失败语义
- `TryAcquireLock*Async` → `Task<string?>`；`null` 表示锁竞争失败（预期返回值，不抛异常），非 null 为释放锁所需的 token
- `Release*Async`（锁场景）→ `Task<bool>`；`false` 表示 token 不匹配或锁已过期（幂等语义，不抛异常）

## 2. 文件结构

```text
src/core/Inkwell.Abstractions/
  Cache/                                  # （新增子目录）
    ICacheProvider.cs                     # 顶层 facade（7 方法）
    CacheEntryOptions.cs                  # record，SetAsync 强制 TTL 载体
    CacheOptions.cs                       # 详细配置
    CacheOptionsValidator.cs              # IValidateOptions<CacheOptions>
```

> **csproj 依赖白名单**：HD-004 不引入新依赖，仍仅 [HD-001 §2 锁定的](HD-001-Inkwell.Abstractions-foundation.md) `Microsoft.Extensions.{DependencyInjection,Configuration,Options,Logging}.Abstractions` + `Microsoft.Extensions.VectorData.Abstractions`（HD-008 起用）+ [`System.Text.Json`](https://learn.microsoft.com/dotnet/api/system.text.json)（BCL 内置，无需额外包引用）。**严禁**因本 HD 引入 `StackExchange.Redis` 等任何具体 SDK（违反 [ADR-017 零外部包约束](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) + [RISK-011 三 Provider contract 漏出](../../03-architecture/risk-analysis.md) 同构风险）。

## 3. 程序文件设计（10 字段 × 4 文件）

### 3.1 `Cache/ICacheProvider.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Cache/ICacheProvider.cs`                                                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| 职责         | 顶层缓存 facade；7 个方法覆盖读 / 写 / 删除 / 探测 / 原子递增 / 分布式简单锁；两 Provider 实现完全相同 ABI（[ADR-016](../../03-architecture/adr/ADR-016-cache-provider-redis.md)）；全 7 方法签名走裸 `Task<T>` / `Task` / `Task<bool>` / `Task<long>` / `Task<string?>` + BCL 异常（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)），不走 `Result<T>`                                                                                                                        |
| 对外接口     | `public interface ICacheProvider { Task<T?> GetAsync<T>(string key, CancellationToken ct = default); Task SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken ct = default); Task<bool> RemoveAsync(string key, CancellationToken ct = default); Task<bool> ExistsAsync(string key, CancellationToken ct = default); Task<long> IncrementAsync(string key, long delta = 1, TimeSpan? ttl = null, CancellationToken ct = default); Task<string?> TryAcquireLockAsync(string key, TimeSpan ttl, CancellationToken ct = default); Task<bool> ReleaseLockAsync(string key, string lockToken, CancellationToken ct = default); }` |
| 内部函数或类 | 接口本身；实现由两 Provider HD 各自提供（`InMemoryCacheProvider` / `RedisCacheProvider`）                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| 输入数据     | `string key`（全部方法）/ `T value`（Set） / `CacheEntryOptions options`（Set） / `long delta` + `TimeSpan? ttl`（Increment） / `TimeSpan ttl`（AcquireLock） / `string lockToken`（ReleaseLock） / `CancellationToken ct`（全部方法）                                                                                                                                                                                                                                                                                    |
| 输出数据     | `T?` / `bool` / `long` / `string?`（全部裸返回，不包 `Result<>`）                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| 依赖模块     | `Cache/CacheEntryOptions.cs` / `System.Text.Json`（序列化，[picker Q-serialization=A](#13-决策记录)） / System（`TimeSpan`）                                                                                                                                                                                                                                                                                                                                                                                              |
| 错误处理     | 全部上抛 BCL 异常（见 [§4.2 BCL 异常分类](#42-bcl-异常分类业务失败-vs-程序错误)）：`GetAsync` 反序列化失败 → [`JsonException`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonexception)；`SetAsync` TTL 越界 → `ArgumentOutOfRangeException`；`IncrementAsync` key 持有非数字值 → `InvalidOperationException`；`TryAcquireLockAsync` / `ReleaseLockAsync` 竞争失败 / token 不匹配 → 幂等返回值不抛异常；全部方法网络故障 → `IOException`；超时 → `TimeoutException`；取消 → `OperationCanceledException` |
| 日志要求     | 实现层（两 Provider HD）在每个方法入口 / 出口写 OTel span，命名 `cache.<verb>`（`get` / `set` / `remove` / `exists` / `increment` / `acquire_lock` / `release_lock`）；4 个 Inkwell 私有字段（`cache.provider` / `cache.key` / `cache.ttl_seconds` / `cache.operation_outcome`）+ 5 个 OTel 标准 `exception.*` 字段（详 §4.3）；`cache.key` 可能含 PII——实现层直接打，调用方在写额外业务日志前自行过滤（[HD-001 §7 安全](HD-001-Inkwell.Abstractions-foundation.md)）；`cache.operation_outcome` 值域见 §4.3                     |
| 测试要求     | `tests/core/Inkwell.Abstractions.Tests/Cache/ICacheProviderContractTests.cs`：契约测试（接口形态 ABI 锁定 via [`PublicApiAnalyzers`](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md)）；7 个方法签名 / 参数顺序 / 默认值 / 返回类型逐一验证；行为测试在 `tests/core/Inkwell.Providers.Contract/Cache/`（统一跨 Provider 家族契约包，与 [HD-002 §8](HD-002-Inkwell.Abstractions-persistence-port.md) / [HD-003 §8.3](HD-003-Inkwell.Abstractions-file-storage-port.md) / [file-structure.md 总体拓扑](../file-structure.md) 拓扑一致；[RISK-011](../../03-architecture/risk-analysis.md)），两 Provider 跑同一套用例                                     |

### 3.2 `Cache/CacheEntryOptions.cs`

| 字段         | 内容                                                                                                                                                                                                                                          |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Cache/CacheEntryOptions.cs`                                                                                                                                                                                    |
| 职责         | `SetAsync` 的强制 TTL 载体；不提供无限期缓存选项（[picker Q-ttl-policy=A](#13-决策记录)）                                                                                                                                                     |
| 对外接口     | `public sealed record CacheEntryOptions { public required TimeSpan AbsoluteExpirationRelativeToNow { get; init; } }`                                                                                                                          |
| 内部函数或类 | record 自身；构造期校验 `AbsoluteExpirationRelativeToNow > TimeSpan.Zero`（违反 → `ArgumentOutOfRangeException`）；与 `CacheOptions.MinTtlSeconds` / `MaxTtlSeconds` 的范围校验**不**在 DTO 构造期做（DTO 无法访问注入的 `CacheOptions`），由 Provider 实现层在 `SetAsync` 调用时校验 |
| 输入数据     | 调用方构造时填写                                                                                                                                                                                                                              |
| 输出数据     | `CacheEntryOptions` 实例                                                                                                                                                                                                                      |
| 依赖模块     | System（`TimeSpan`）                                                                                                                                                                                                                          |
| 错误处理     | 构造期 `AbsoluteExpirationRelativeToNow <= TimeSpan.Zero` → `ArgumentOutOfRangeException`（程序错误，调用方传错）；越出 `CacheOptions` 配置的 `[MinTtlSeconds, MaxTtlSeconds]` 范围 → Provider 实现层在 `SetAsync` 抛 `ArgumentOutOfRangeException`（paramName=`"options"`） |
| 日志要求     | DTO 自身不做日志；`SetAsync` 成功时实现层在 `cache.set` span 输出 `cache.ttl_seconds`                                                                                                                                                        |
| 测试要求     | `CacheEntryOptionsTests.cs`：(1) `AbsoluteExpirationRelativeToNow <= 0` 抛异常；(2) 正常构造合法；(3) value equality（record 默认）                                                                                                          |

### 3.3 `Cache/CacheOptions.cs`

> [HD-001 §3.11.1 `InkwellProvidersOptions`](HD-001-Inkwell.Abstractions-foundation.md#3111-optionsinkwellprovidersoptionscsf9-新增) 已承载 Provider 选择器 `Inkwell:Providers:Cache ∈ {"InMemory","Redis"}`；本 Options **不**重复承载 Provider 字段。[HD-001 §3.11](HD-001-Inkwell.Abstractions-foundation.md#311-optionsinkwelloptionscs) 根 `InkwellOptions.Cache` 槽位当前为占位 `CacheOptions` 类，本 HD 补全其字段。

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                       |
| ------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Cache/CacheOptions.cs`                                                                                                                                                                                                                                                                                                                                                     |
| 职责         | 缓存端口详细配置；从 `appsettings.json` `"Inkwell:Cache"` 段绑定                                                                                                                                                                                                                                                                                                                                          |
| 对外接口     | `public sealed class CacheOptions { [Range(1, 86400)] public int MinTtlSeconds { get; init; } = 1; [Range(1, 86400)] public int MaxTtlSeconds { get; init; } = 86400; [Range(1, 86400)] public int DefaultLockTtlSeconds { get; init; } = 30; public bool EnableSensitiveDataLogging { get; init; } = false; }`                                                                                          |
| 内部函数或类 | DataAnnotations 校验；TTL 单位统一为秒（与 [HD-003 §3.6](HD-003-Inkwell.Abstractions-file-storage-port.md) 分钟单位不同——缓存场景 TTL 粒度通常更细，秒级更贴近 Redis `EXPIRE` 原生单位）；`MaxTtlSeconds` 默认 86400 秒（24h，[picker Q-ttl-bounds=A](#13-决策记录)）；Provider 特定的连接字符串 / 端点由各 Provider HD 自己的子 Options 承载（如 `RedisCacheOptions`）                                     |
| 输入数据     | 由 `IConfiguration` 绑定                                                                                                                                                                                                                                                                                                                                                                                  |
| 输出数据     | `CacheOptions` 实例（DI 通过 `IOptions<CacheOptions>` 注入）                                                                                                                                                                                                                                                                                                                                              |
| 依赖模块     | `System.ComponentModel.DataAnnotations`                                                                                                                                                                                                                                                                                                                                                                    |
| 错误处理     | DataAnnotations 校验失败 → `OptionsValidationException`，host 兜底；Provider 不一致由 Builder DSL 抓 `InkwellBuilderException`                                                                                                                                                                                                                                                                            |
| 日志要求     | DI 启动期 `IValidateOptions` 失败时输出 OTel `exception.type=Microsoft.Extensions.Options.OptionsValidationException`（[HD-001 §5.3](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)）                                                                                                                                                                                                  |
| 测试要求     | `CacheOptionsTests.cs`：默认值（1 / 86400 / 30 / false）、`appsettings.json` 绑定、`[Range]` 边界（1 / 86400 / 越界）                                                                                                                                                                                                                                                                                      |

### 3.4 `Cache/CacheOptionsValidator.cs`

| 字段         | 内容                                                                                                                                                                                                                                        |
| ------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Cache/CacheOptionsValidator.cs`                                                                                                                                                                            |
| 职责         | `IValidateOptions<CacheOptions>` 实现；DataAnnotations + 跨字段校验                                                                                                                                                                       |
| 对外接口     | `internal sealed class CacheOptionsValidator : IValidateOptions<CacheOptions> { public ValidateOptionsResult Validate(string? name, CacheOptions options); }`                                                                             |
| 内部函数或类 | (1) `Validator.TryValidateObject` DataAnnotations；(2) 跨字段：`MinTtlSeconds <= MaxTtlSeconds`；`DefaultLockTtlSeconds <= MaxTtlSeconds`（默认值 1 / 86400 / 30 均落在合法范围内）；Provider 特定连接 / 凭证不在本 Validator             |
| 输入数据     | `CacheOptions` 实例                                                                                                                                                                                                                        |
| 输出数据     | `ValidateOptionsResult.Success` / `Fail(IEnumerable<string>)`                                                                                                                                                                              |
| 依赖模块     | `Microsoft.Extensions.Options` / `System.ComponentModel.DataAnnotations`                                                                                                                                                                   |
| 错误处理     | 同 [HD-001 §3.12](HD-001-Inkwell.Abstractions-foundation.md#312-optionsinkwelloptionsvalidatorcs)，校验失败 → `Fail` 含全部消息                                                                                                          |
| 日志要求     | 失败由 `OptionsValidationException` 抛出，host 打 fatal                                                                                                                                                                                    |
| 测试要求     | `CacheOptionsValidatorTests.cs`：(1) DataAnnotations 边界合格；(2) 跨字段：`MinTtlSeconds = 100 / MaxTtlSeconds = 50` 拒；(3) 默认值（1 / 86400 / 30）通过                                                                               |

## 4. BCL 异常与日志（端口段补充 HD-001 §4）

> **错误处理路径**：本端口与业务命名空间统一采用裸 `Task<T>` + .NET BCL 异常。Inkwell 不自建错误码常量集 / 不自建 `Result<T>` / `Error` 抽象 / 不自建端口异常基类，仅保留 `InkwellConfigurationException` / `InkwellBuilderException` 用于 DI 装配期校验。具体 BCL 异常映射 + OTel `exception.*` 五字段详见下表与 [HD-001 §5.3](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)。

### 4.1 错误码

本端口**不分配** `INK-CACHE-NNN` 错误码。与 [HD-002](HD-002-Inkwell.Abstractions-persistence-port.md) / [HD-003](HD-003-Inkwell.Abstractions-file-storage-port.md) 最终态一致，错误语义全部走 BCL 异常类型表达 + OTel `exception.*` 五字段。

### 4.2 BCL 异常分类（业务失败 vs 程序错误）

按 [HD-001 §5.3 BCL 对照表](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表) 的分类语义：

- **预期返回值（不是异常，调用方按值判断）**：
  - `GetAsync<T>` 缓存未命中 → 返回 `null`（[§1.4 一致性声明](#14-与-hd-001-51--52-命名约定的一致性声明)）
  - `RemoveAsync` 对象本不存在 → 返回 `false`（幂等）
  - `TryAcquireLockAsync` 锁竞争失败 → 返回 `null`
  - `ReleaseLockAsync` token 不匹配 / 锁已过期 → 返回 `false`（幂等）
- **业务失败 / 预期错误**（调用方应 try/catch 并按业务策略处理，**不**触发 P1 告警）：
  - `InvalidOperationException`：`IncrementAsync` 目标 key 持有非数字值（Redis `WRONGTYPE`）；message 前缀 `"Key holds non-numeric value"`
- **程序错误 / 失血告警**（运维介入修复，P1 / P2 告警）：
  - `IOException`：DNS / TLS / network 失败 / Redis 连接断开 / 中途传输中断；触发方法：全部 7 个；message 应含具体根因（如 `"Connection to Redis endpoint failed"`）
  - `TimeoutException`：单次远端调用超过 Provider 子 Options 的超时配置；触发方法：全部 7 个
  - [`JsonException`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonexception)：`GetAsync<T>` 反序列化失败（存储值与 `T` 不兼容，通常因业务侧序列化格式变更未做兼容处理）；`SetAsync<T>` 序列化失败（`T` 含不支持的循环引用 / 非 JSON 友好类型）
- **参数 / 取消错误**（调用方 bug，应在测试期暴露）：
  - `ArgumentException` / `ArgumentNullException`：`key` 为 null / empty；`lockToken` 为 null / empty
  - `ArgumentOutOfRangeException`：`CacheEntryOptions.AbsoluteExpirationRelativeToNow` 越出 `[MinTtlSeconds, MaxTtlSeconds]`（`SetAsync` 抛，paramName=`"options"`）；`TryAcquireLockAsync` 的 `ttl` 越界（paramName=`"ttl"`）
  - `OperationCanceledException`：所有方法响应 `ct`（[HD-001 §4.3](HD-001-Inkwell.Abstractions-foundation.md#43-取消传播)）

### 4.3 OTel span / 字段

每个方法在实现层（两 Provider HD）按 [picker Q-otel](#13-决策记录) 输出 span：

- `cache.get` ← `GetAsync`
- `cache.set` ← `SetAsync`
- `cache.remove` ← `RemoveAsync`
- `cache.exists` ← `ExistsAsync`
- `cache.increment` ← `IncrementAsync`
- `cache.acquire_lock` ← `TryAcquireLockAsync`
- `cache.release_lock` ← `ReleaseLockAsync`

**Inkwell 私有字段**（4 个）：

- `cache.provider`（`InMemory` / `Redis`）
- `cache.key`
- `cache.ttl_seconds`（仅 `SetAsync` / `IncrementAsync`（key 首次创建时） / `TryAcquireLockAsync` 填充）
- `cache.operation_outcome`：值域按方法区分——
  - `GetAsync`：`hit` / `miss`
  - `SetAsync` / `RemoveAsync` / `IncrementAsync`：`success` / `failed` / `cancelled`（`RemoveAsync` 额外含 `not_found` 表本不存在）
  - `TryAcquireLockAsync`：`lock_acquired` / `lock_contended` / `failed` / `cancelled`
  - `ReleaseLockAsync`：`lock_released` / `lock_not_owned` / `failed` / `cancelled`

**OTel 标准字段**（5 个，按 [`exception.*` 语义约定](https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/)，仅异常路径填充）：

- `exception.type`（如 `System.IO.IOException` / `System.TimeoutException` / `System.Text.Json.JsonException`）
- `exception.message`
- `exception.stacktrace`
- `exception.escaped`
- `exception.id`（`Guid.CreateVersion7().ToString()` 生成，便于 Grafana / Tempo 跨 span 关联）

> **PII 提示**：`cache.key` 可能含用户 ID / Agent ID / 对话 ID；这些字段允许进 OTel（Inkwell 自托管 Grafana 栈在边界内），调用方在写**额外**业务日志时应自行过滤（同 [HD-003 §4.3](HD-003-Inkwell.Abstractions-file-storage-port.md#43-otel-span--字段) PII 处理方式一致）。缓存**值本身**（`GetAsync` / `SetAsync` 的 `T value`）**不得**进入任何 OTel 字段——即使 `EnableSensitiveDataLogging=true` 也仅追加 `cache.value_size_bytes`（长度而非内容）。

## 5. 公共约定继承（HD-001）

### 5.1 命名

- `ICacheProvider` ↔ [HD-001 §5.1](HD-001-Inkwell.Abstractions-foundation.md#51-命名) `I<Capability>Provider`
- `GetAsync` / `SetAsync` 等 ↔ §5.1 异步方法以 `Async` 结尾；`GetAsync` 命名偏差详见 [§1.4](#14-与-hd-001-51--52-命名约定的一致性声明)
- `CacheEntryOptions` ↔ §5.1 DTO 命名（非 `<Action><Entity>Request/Response` 模式，因本 DTO 是跨方法复用的配置对象而非单方法请求/响应）
- `CacheOptions` ↔ §5.1 `<Provider>Options`

### 5.2 签名

- 7 个方法走裸 `Task<T?>` / `Task` / `Task<bool>` / `Task<long>` / `Task<string?>` + BCL 异常，↔ [HD-001 §5.3 BCL 对照表](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)
- `GetAsync<T>` 未命中返回 `null`（不抛异常）——[§1.4 Cache 领域 Get 前缀例外](#14-与-hd-001-51--52-命名约定的一致性声明)
- `RemoveAsync` / `ExistsAsync` 的 `bool` 返回值保留幂等语义（同 [HD-003 §5.2](HD-003-Inkwell.Abstractions-file-storage-port.md#52-签名)）
- `IncrementAsync` 返回 `Task<long>`（操作后新值），不视为查询也不视为写入失败语义的特例
- `TryAcquireLockAsync` / `ReleaseLockAsync` 是显式锁 pair，`CancellationToken ct = default` 全 7 方法必填 ↔ [HD-001 §4.3](HD-001-Inkwell.Abstractions-foundation.md#43-取消传播)

### 5.3 错误处理

- 业务失败 / 预期错误 → BCL 业务异常（`InvalidOperationException`）；调用方 try/catch 按业务策略处理
- 程序错误 / 失血告警 → BCL 程序异常（`IOException` / `TimeoutException` / `JsonException`）；触发运维告警
- 参数错误 → `ArgumentException` / `ArgumentNullException` / `ArgumentOutOfRangeException`
- 幂等查询 / 竞争型返回 → `RemoveAsync` / `TryAcquireLockAsync` / `ReleaseLockAsync` 返回值本身表达"未命中 / 未获取 / 未释放"语义，不抛异常
- 取消 → `OperationCanceledException`
- 实现层用 [`ActivitySource.StartActivity`](https://learn.microsoft.com/dotnet/api/system.diagnostics.activitysource.startactivity) 创建 span 后，**异常路径**自动用 `Activity.RecordException` 或 `Activity.SetStatus(ActivityStatusCode.Error, message)` 写入 `exception.*` 五字段（详 §4.3）

## 6. Builder DSL 钩子（给 Provider HD 的契约）

每个 Provider csproj 提供唯一入口扩展方法：

```csharp
// providers/Inkwell.Cache.Redis/RedisCacheBuilderExtensions.cs
public static class RedisCacheBuilderExtensions
{
    public static IInkwellBuilder UseRedisCache(
        this IInkwellBuilder builder,
        string connectionString);
}

// src/core/Inkwell.Core/Cache/InMemoryCacheBuilderExtensions.cs
public static class InMemoryCacheBuilderExtensions
{
    public static IInkwellBuilder UseInMemoryCache(
        this IInkwellBuilder builder,
        Action<InMemoryCacheOptions>? configure = null);
}
```

每个扩展方法**必须**：(1) 校验入参非 null；(2) 调用 `builder.Services.AddSingleton<ICacheProvider, XxxCacheProvider>()`；(3) 注册 `IValidateOptions<CacheOptions>` + 各自 Provider 子 Options 的 Validator；(4) 与 `InkwellProvidersOptions.Cache` 取值交叉校验（不一致抛 `InkwellBuilderException(message)`，[HD-001 §3.13](HD-001-Inkwell.Abstractions-foundation.md) 锁定的 BCL 程序错误子类）；(5) 返回 `builder`。

## 7. 性能 / 安全 / 可观测性

### 7.1 性能预算（[picker Q-perf-budget=A 宽松档](#13-决策记录)）

| 方法                   | facade overhead P50 | facade overhead P99 | 备注                          |
| ---------------------- | -------------------- | -------------------- | ----------------------------- |
| `GetAsync`             | < 20ms                | < 100ms               | Redis 内网 RTT 量级           |
| `SetAsync`             | < 20ms                | < 100ms               | 含序列化开销                  |
| `RemoveAsync`          | < 20ms                | < 100ms               | —                             |
| `ExistsAsync`          | < 20ms                | < 100ms               | —                             |
| `IncrementAsync`       | < 20ms                | < 100ms               | 原子操作，单次往返            |
| `TryAcquireLockAsync`  | < 20ms                | < 100ms               | `SET NX EX` 单次往返          |
| `ReleaseLockAsync`     | < 20ms                | < 100ms               | Lua CAS 脚本执行              |

> 上述为 facade overhead（端口实现自身代码 + 一次远端调用 RTT），`InMemoryCacheProvider` 场景应远低于该预算（进程内操作）。

### 7.2 安全

- `CacheOptions.EnableSensitiveDataLogging` 默认 `false`；启用后仅追加 `cache.value_size_bytes`（大小而非内容）——**缓存值本身永不进入 OTel**（详 [§4.3 PII 提示](#43-otel-span--字段)）
- 凭证（Redis `ConnectionString` / 密码）由 `providers/Inkwell.Cache.Redis` 自己的子 Options 承载，**不**在本 `CacheOptions`；走 [K8s Secret](https://kubernetes.io/docs/concepts/configuration/secret/) / Compose `.env`（[OQ-A006 closed §B](../../03-architecture/open-questions-arch.md)，v1 不引 Azure Key Vault）
- 分布式锁 token 由 Provider 实现生成（建议 `Guid.CreateVersion7()`），**不可预测**——防止第三方猜测 token 抢先释放他人持有的锁
- Redis 端口暴露面由 [ADR-005](../../03-architecture/adr/ADR-005-deployment-docker-compose-aks.md) 部署拓扑约束（dev Compose 内网 / prod AKS ClusterIP 或 Azure Cache for Redis Private Endpoint），本 HD 不重复约束

### 7.3 可观测性

- 4 私有 + 5 OTel 标准 `exception.*` 字段进 OTel；本 HD 不锁告警规则（H4 [TestCaseAuthor](../../../.github/agents/h4-test-case-author.agent.md) 反推时锁），但建议告警维度：
  - `exception.type` ∈ {`System.IO.IOException`, `System.TimeoutException`} 速率 > 5/min → P1（连接 / 超时类失血，直接关联 [RISK-012](../../03-architecture/risk-analysis.md) Redis 单点风险）
  - `cache.operation_outcome = lock_contended` 速率异常升高 → P2（业务侧临界区设计问题或锁 TTL 过短）
  - `cache.operation_outcome = miss` 占比对 rate-limit / 会话状态类 key 持续 > 50% → P3（缓存命中率异常，可能 invalidation 策略有误，呼应 [RISK-012](../../03-architecture/risk-analysis.md)）

## 8. 测试要求

### 8.1 单元测试（本 HD 范围内）

- 测试项目：`tests/core/Inkwell.Abstractions.Tests/Cache/`（与 HD-001 同 csproj，[MSTest.Sdk 4.x](https://github.com/microsoft/testfx) + MTP runner）
- 每个文件至少一个 `*Tests.cs` 配对（见 §3 各小节"测试要求"）
- 覆盖率门槛：`CacheEntryOptions` ≥ 95%；`CacheOptions` + Validator ≥ 90%；`ICacheProviderContractTests` 仅锁 ABI ≥ 100%

### 8.2 契约测试

- 接口 ABI 用 [`PublicApiAnalyzers`](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) 锁定
- `ICacheProvider` 形态变更 → 需新建 ADR + 影响 2 个 Provider HD

### 8.3 集成测试

- 本 HD **不**起集成测试（端口层无外部依赖）
- 两 Provider 行为测试在 `tests/core/Inkwell.Providers.Contract/Cache/`（统一跨 Provider 家族契约包，与 [HD-002 §8](HD-002-Inkwell.Abstractions-persistence-port.md) / [HD-003 §8.3](HD-003-Inkwell.Abstractions-file-storage-port.md) / [file-structure.md 总体拓扑](../file-structure.md) 拓扑一致；[RISK-011](../../03-architecture/risk-analysis.md) / [RISK-012](../../03-architecture/risk-analysis.md)），由 Provider HD 联合起草；CI matrix 跑 InMemory / [Testcontainers.Redis](https://testcontainers.com/modules/redis/) 两 Provider 同一套用例，含并发抢锁场景（多协程同时 `TryAcquireLockAsync` 同一 key，验证仅一方成功）

### 8.4 BannedSymbols（CI 强制）

- `Inkwell.Abstractions.Cache.*` 禁用引入 `StackExchange.Redis.*` 等具体 SDK 命名空间（写在 `BannedSymbols.txt`，违反 → 编译阻塞）

## 9. 部署 / 配置

`Inkwell.Abstractions.csproj` 与端口层一同打镜像（无独立部署）。`appsettings.json` 顶层段：

```json
{
  "Inkwell": {
    "Providers": {
      "Cache": "Redis"
    },
    "Cache": {
      "MinTtlSeconds": 1,
      "MaxTtlSeconds": 86400,
      "DefaultLockTtlSeconds": 30,
      "EnableSensitiveDataLogging": false
    },
    "Cache:Redis": {
      "ConnectionString": "..."
    }
  }
}
```

> Provider 特定子段（`Cache:Redis` / `Cache:InMemory`）由各 Provider HD 起草时锁定。

## 10. CI 自检命令（grep 列表）

| 编号 | 检查项                                                              | 命令（CI [GitHub Actions](https://docs.github.com/actions) 工作流引用）                                                                                                                          |
| ---- | -------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| C1   | 业务命名空间禁直接 `using StackExchange.Redis`                       | `rg -n -e 'using\s+StackExchange\.Redis' src/core/Inkwell.Core/` 期望 0 行                                                                                                                          |
| C2   | `ICacheProvider` 接口签名稳定                                        | [PublicApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) `PublicAPI.Shipped.txt` diff                                          |
| C3   | 端口层无 `Task<Result<` 残留（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)） | `rg -n -e 'Task<Result<' -e 'Task<Result>' src/core/Inkwell.Abstractions/Cache/` 期望 0 行                                                                                                          |
| C4   | 业务命名空间禁 `Result<T>` / `ErrorCodes` 引用                       | `rg -n -e 'Common\.Result' -e 'Common\.Error' -e 'ErrorCodes\.' src/core/Inkwell.Core/ src/core/Inkwell.WebApi/ src/core/Inkwell.Worker/ providers/Inkwell.Cache.Redis/` 期望 0 行                  |
| C5   | 缓存值不进 OTel（仅大小字段）                                        | `rg -n -e '"cache\.value"' -e 'cache\.value\s*=' src/core/ providers/Inkwell.Cache.Redis/` 期望 0 行（仅 `cache.value_size_bytes` 允许）                                                            |
| C6   | OTel span 字段名一致                                                 | `rg -n -e '"cache\.provider"' -e '"cache\.key"' -e '"cache\.ttl_seconds"' -e '"cache\.operation_outcome"' src/core/ providers/Inkwell.Cache.Redis/` 期望全部在实现层覆盖                            |

## 11. 待补 / 待评审

- **Key 长度上限**——本 HD 不锁 Key 最大长度；Redis 单 key 上限为 512MB（实践中远低于此），是否需要 Inkwell 层面额外限制留待 Provider HD 或后续 errata 决定
- **`CacheEntryOptions` 序列化的 `JsonSerializerOptions` 细节**（命名策略 / 多态类型处理 / 循环引用检测开关）——本 HD 仅锁定"统一 `System.Text.Json`"，具体 `JsonSerializerOptions` 由 Provider 实现层决定，需在两 Provider（`InMemoryCacheProvider` / `RedisCacheProvider`）之间保持一致，避免跨 Provider 切换时历史缓存数据不可读；建议下一轮 picker 补齐
- **Redis Key 命名空间隔离**（多租户前缀 / dev 与 prod 环境隔离）——留业务侧按 [ADR-016 §Key 命名约定](../../03-architecture/adr/ADR-016-cache-provider-redis.md) `{tenant}:{module}:{purpose}:{id}` 自行处理，本 HD 不提供强制机制（[picker Q-key-convention=A](#13-决策记录)）
- **锁续约 / 心跳机制**——v1 明确不做（§1.2），业务侧需保证临界区耗时 < 锁 TTL；若后续场景需要长临界区，需走 v2 backlog 或新 ADR
- **模型 response cache 的具体缓存键与 invalidation 触发点**——[ADR-016 §决策](../../03-architecture/adr/ADR-016-cache-provider-redis.md) 提及但默认关闭，由消费该能力的业务 HD（`Inkwell.Core.Agents` 或 `.Models`）起草时决定，呼应 [RISK-012](../../03-architecture/risk-analysis.md) H3 明确要求"为每个缓存键定义 invalidation 触发点"

## 12. 跨模块章节贡献

本 HD 在以下跨模块文件中追加一级章节 `## Inkwell.Abstractions.Cache`：

- `docs/04-detailed-design/file-structure.md` — 新增 `Inkwell.Abstractions/Cache/` 子目录树
- `docs/04-detailed-design/database-design.md` — **不贡献**（端口层不直接接 DB）

> 跨模块章节追加由本 HD 起草后**立即同步**到对应文件（**只追加**不改其他模块章节）。

## 13. 决策记录

### 13.1 起草期 picker 决策（2026-07-05）

| 字段                       | 选定值                                                                                             | picker 时间 |
| -------------------------- | ---------------------------------------------------------------------------------------------------- | ----------- |
| Q-scope                    | A：6 方法（Get/Set/Remove/Exists/Increment/TryAcquireLock+ReleaseLock）                              | 2026-07-05  |
| Q-lock-shape               | A：`TryAcquireLockAsync` 返回 `string? lockToken`，配 `ReleaseLockAsync(key, lockToken)` 显式释放     | 2026-07-05  |
| Q-ttl-policy                | A：强制要求通过 `CacheEntryOptions` 指定 TTL，不提供无限期选项                                        | 2026-07-05  |
| Q-serialization             | A：统一 `System.Text.Json`（`JsonSerializer`）                                                       | 2026-07-05  |
| Q-key-convention            | A：不锁，端口层只接受 `string key`，调用方自行拼接                                                    | 2026-07-05  |
| Q-increment-behavior        | A：不存在时自动创建并从 0 开始累加（`INCRBY` 语义）                                                   | 2026-07-05  |
| Q-perf-budget               | A：宽松档 P50 < 20ms / P99 < 100ms（对齐 HD-003 风格）                                                | 2026-07-05  |
| Q-otel                      | A：`cache.<verb>` + `cache.provider` / `cache.key` / `cache.ttl_seconds` / `cache.operation_outcome` | 2026-07-05  |
| Q-cache-entry-options       | A：仅绝对过期 `AbsoluteExpirationRelativeToNow`                                                       | 2026-07-05  |
| Q-ttl-bounds                | A：`MinTtlSeconds=1`，`MaxTtlSeconds=86400`（24h），`DefaultLockTtlSeconds=30`                        | 2026-07-05  |

### 13.2 候选与放弃理由

- **Q-scope**：备选 B（4 方法，不含 Increment / Lock）被否决——会把原子性保证转嫁给业务侧用 `Get + Set` 组合实现，在多副本部署下容易出竞态 bug，与 [ADR-016 §决策](../../03-architecture/adr/ADR-016-cache-provider-redis.md) 列出的 rate limit / 简单互斥场景直接冲突
- **Q-lock-shape**：备选 B（`IAsyncDisposable` 自动释放）被否决——虽更符合 .NET `using` 惯例，但需要 Provider 内部持有额外状态（后台续约或超时保护），与 v1"简单锁、不做续约"的范围声明（§1.2）矛盾；显式 token 释放更贴近 [ADR-016](../../03-architecture/adr/ADR-016-cache-provider-redis.md) 提及的 `SET NX EX` 简单锁原生语义
- **Q-ttl-policy**：备选 B（默认 30 min）/ 备选 C（默认永久）均被否决——[RISK-012](../../03-architecture/risk-analysis.md) 明确"业务写 DB 后忘记 invalidate 缓存键"是中风险项，强制显式 TTL 能从设计层面消除"忘记设置过期时间导致缓存永久堆积"这一子风险
- **Q-serialization**：备选 B（留给各 Provider 自选）被否决——会导致 `InMemoryCacheProvider`（共享引用）与 `RedisCacheProvider`（深拷贝）行为不一致，单元测试通过但 Redis 环境下出现"业务侧修改了 Get 到的对象、以为改的是缓存"的隐蔽 bug
- **Q-key-convention**：备选 B（`CacheKeyBuilder` 强制）被否决——与 [HD-003 picker Q3](HD-003-Inkwell.Abstractions-file-storage-port.md#13-关键决策摘要) 风格一致，端口层保持薄，Key 语义留业务侧决定
- **Q-increment-behavior**：备选 B（不存在抛异常）被否决——违反 Redis `INCRBY` 原生语义，且要求业务侧每次先 `SetAsync` 初始化，增加不必要的往返
- **Q-perf-budget**：备选 B（紧凑档 P50 < 10ms）未选——v1 未锁定 Redis 部署是否同 region，宽松档更稳妥，避免过早锁定不可达的 SLO
- **Q-cache-entry-options**：备选 B（同时支持滑动过期）被否决——v1 使用场景（rate limit 计数窗口、元数据缓存、会话状态）均适合绝对过期；滑动过期的 `EXPIRE` 刷新会增加每次 `GetAsync` 的额外往返，与 §7.1 性能预算冲突
- **Q-ttl-bounds**：备选 B（`MaxTtlSeconds=604800` 对齐 FileStorage 预签名 URL 7 天上限）未选——缓存层与预签名 URL 场景语义不同（缓存不应长期持有陈旧数据），24h 更贴近 rate limit / 会话状态 / 元数据缓存的典型时效

> 全部候选与放弃理由源自 2026-07-05 picker 会话；无历史 errata（本 HD 从起草第一天直接采用 ADR-023 最终态规约）。
