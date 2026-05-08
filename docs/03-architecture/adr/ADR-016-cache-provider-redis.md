---
id: ADR-016-cache-provider-redis
stage: H2
status: reviewed
authors:
  - name: H2-ArchitectAdvisor
    role: agent
reviewers: [ Inkwell ]
created: 2026-05-09
updated: 2026-05-09
upstream:
  - REQ-inkwell-agent-platform
  - repo-impact-map-inkwell-agent-platform
  - ADR-002
  - ADR-005
  - ADR-007
  - OQ-A004
downstream: []
---

# ADR-016 缓存层：`ICacheProvider` 抽象 + Redis

## 上下文

[OQ-A004](../open-questions-arch.md) 起初的 Agent 默认值 A 是"v1 不引入独立 Redis；用 ASP.NET Core `IMemoryCache` + Microsoft Agent Framework 内置 thread state；Public API rate limit 用 `Microsoft.AspNetCore.RateLimiting`（基于内存 token bucket）"。Owner 在 H2 评审中反转为 §B：v1 即引入 Redis，并要求与 [`IPersistenceProvider`](./ADR-004-data-store-provider-switchable-ef-core.md) / [`IFileStorageProvider`](./ADR-015-object-storage-provider-switchable.md) 保持 `*Provider` 后缀的命名一致性，新增 `ICacheProvider` 抽象。

驱动因素：

- v1 后端 [HPA](./ADR-005-deployment-docker-compose-aks.md) 默认 min 2 副本——基于 [`IMemoryCache`](https://learn.microsoft.com/aspnet/core/performance/caching/memory) 的进程内缓存与基于 [`Microsoft.AspNetCore.RateLimiting`](https://learn.microsoft.com/aspnet/core/performance/rate-limit) 的内存 token bucket 在多副本下语义不一致（每副本一份计数 / 一份缓存）。
- [REQ-013 Public API rate limit](../../01-requirements/requirements.md) 要求"60 req/min"是租户维度限额，不是单副本限额，必须强一致。
- Agent 会话状态（[Microsoft Agent Framework `AgentThread`](../../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI/) 默认在内存）跨 Pod 替换 / Run resume / 客户端重连存在丢状态风险——以 Redis 持有可消除。

## 决策

**引入 `ICacheProvider` 抽象 + 单 Redis 实现（v1）：**

- **抽象**：后端业务代码仅依赖 [`Inkwell.Cache.ICacheProvider`](../../01-requirements/repo-impact-map.md)（Get / Set / Remove / Exists / Increment / TryAcquireLock 等最小必要面），不直接 `using StackExchange.Redis`。
- **实现**：v1 仅 `RedisCacheProvider`（基于 [StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/)）。`InMemoryCacheProvider` 仅作为单元测试 fallback（与 `IPersistenceProvider` 的 InMemory Provider 思路同构）。
- **部署组合**：

  | 环境             | 默认 Cache Provider                                                                                                       | 备注                       |
  | ---------------- | ------------------------------------------------------------------------------------------------------------------------- | -------------------------- |
  | 本地单元测试     | `InMemory`                                                                                                                | 进程内 dictionary，零依赖  |
  | dev Compose      | `Redis`（[redis:8](https://hub.docker.com/_/redis) 容器）                                                                 | 单实例，named volume       |
  | prod AKS（Azure）| `Redis` → [Azure Cache for Redis](https://learn.microsoft.com/azure/azure-cache-for-redis/) Standard / Premium tier       | Private Endpoint 接入      |
  | prod AKS（自建） | `Redis` → 自建 `redis` StatefulSet + PVC                                                                                  | Helm values 切换           |

- **配置 key**：`Inkwell:Cache:Provider`（值域 `InMemory` / `Redis`）+ `Inkwell:Cache:Redis:ConnectionString`。
- **使用场景（v1）**：
  - **Public API rate limit**（[ADR-007](./ADR-007-public-api-token-auth.md)）：将 `Microsoft.AspNetCore.RateLimiting` 的 [`PartitionedRateLimiter`](https://learn.microsoft.com/dotnet/api/system.threading.ratelimiting.partitionedratelimiter) 后端从内存 token bucket 切换为 Redis 分布式 token bucket（[`AspNetCoreRateLimit` 或 `RateLimiter` + `IDistributedCache` 适配](https://github.com/stefanprodan/AspNetCoreRateLimit)）。
  - **Agent 会话短期状态**（`AgentThread` 当前对话窗口的最近 N 条消息缓存，避免每次 Run 都从 [`IPersistenceProvider`](./ADR-004-data-store-provider-switchable-ef-core.md) 读取）。
  - **Skill registry 与 Agent 配置**等读多写少的元数据缓存——TTL 5 min。
  - **模型 response cache**（仅当 prompt + parameters 完全一致时复用）——按 [REQ-005](../../01-requirements/requirements.md) 模型层接口判断启用范围。
- **Key 命名约定**：`{tenant}:{module}:{purpose}:{id}`，例如 `default:ratelimit:public-api:{token-hash}`。
- **不在 v1 范围**：Redis Cluster / Sentinel HA / Lua 脚本式分布式锁（v1 用 `SET NX EX` 简单锁）；这些列入 v2 backlog。

## 备选项

### 备选 A（OQ-A004 §A，原 Agent 默认值）：v1 不引入独立 Redis

- **放弃理由**：(1) 多副本下 `IMemoryCache` 与基于内存的 `RateLimiter` 行为不一致——同一租户在不同 Pod 上"额度独立计数"，[REQ-013 60 req/min](../../01-requirements/requirements.md) 实际上限为 60 × 副本数；(2) Agent 会话状态在 HPA 缩容 / Pod 替换时丢失，触发 [ADR-011 主进程长 SSE](./ADR-011-auto-lock-with-inflight-task-survival.md) 已经覆盖不了的"会话级丢"场景；(3) 与 `IPersistenceProvider` / `IFileStorageProvider` 的 Provider 抽象家族不一致，命名风格不统一。

### 备选 B：本 ADR 决议 — 引入 Redis + `ICacheProvider`

- **被选用**：(1) 与现有两条 Provider 抽象同构（`*Provider` 后缀、`Inkwell:<Family>:Provider` 配置 key）；(2) 解决多副本一致性；(3) Redis 是行业事实标准，运维资料充分。

### 备选 C：用 [Microsoft.AspNetCore.OutputCaching + IDistributedCache](https://learn.microsoft.com/aspnet/core/performance/caching/output) 不抽象成自有 Provider

- **放弃理由**：(1) 命名与 `IPersistenceProvider` / `IFileStorageProvider` 风格不一致；(2) `IDistributedCache` 接口语义弱于业务需要（不直接支持 Increment / TryAcquireLock，需要在多处直接 `using StackExchange.Redis`，违反"业务代码不依赖具体 Provider"原则）；(3) 无法在单元测试场景下零依赖跑（必须接 InMemory 实现，等于多绕一层）。

### 备选 D：Memcached / NCache / Hazelcast

- **放弃理由**：(1) Memcached 不支持持久化与丰富数据结构（rate limit 需要 atomic increment + TTL，Memcached 半支持但语义差）；(2) NCache / Hazelcast 在 .NET 生态资料 < Redis；(3) 无 Azure 托管对应物。

## 后果

### 正面

- 多副本部署下 Public API rate limit / Agent 会话短期状态 / 元数据缓存 全部一致。
- 与 [`IPersistenceProvider`](./ADR-004-data-store-provider-switchable-ef-core.md) + [`IFileStorageProvider`](./ADR-015-object-storage-provider-switchable.md) 同构 — 三 Provider 家族命名风格一致，新人接入认知一致。
- 业务代码不直接依赖 `StackExchange.Redis`，v2 切换实现（如分片或换底）只改 `RedisCacheProvider`。
- `InMemoryCacheProvider` 让单元测试零依赖。

### 负面

- v1 多一个运维项：dev Compose 多 1 个 service；prod AKS 多 1 个外部资源（Azure Cache for Redis）或 StatefulSet。
- 启动时序复杂度上升：API 启动需要等 Redis ready；通过 [Helm `dependsOn`](https://helm.sh/docs/topics/charts_hooks/) + 健康检查缓解。
- 凭据多一份：Redis 连接串 / 密码 / TLS 证书走 [OQ-A006 closed §B](../open-questions-arch.md) 的 K8s Secret 路径，详见 [RISK-013](../risk-analysis.md)。
- 缓存一致性陷阱：业务写 DB 后必须显式 invalidate 缓存键 — 这是 contract，需要在 H3 详细设计中明确每个缓存键的 invalidation 策略。详见 [RISK-012](../risk-analysis.md)。

### 中性

- v1 不上 Redis Cluster / Sentinel HA。Azure Cache for Redis 已自带 Standard / Premium 多可用区；自建场景下 v1 仅单实例，列入 v2 backlog（[RISK-012](../risk-analysis.md)）。
- 模型 response cache 默认关闭，仅在 H3 详细设计明确"安全可缓存的模型调用集合"后再启用——避免误命中导致用户看到陈旧或不应共享的内容。

## 状态

- **状态**：accepted（接受 [OQ-A004 closed §B](../open-questions-arch.md) 决议）
- **首次发布**：2026-05-09
- **关联**：supersedes 无；上游 [ADR-002](./ADR-002-backend-runtime-dotnet10-aspnetcore.md) / [ADR-005](./ADR-005-deployment-docker-compose-aks.md) / [ADR-007](./ADR-007-public-api-token-auth.md) / [OQ-A004 closed §B](../open-questions-arch.md)
- **置信度**：medium（依赖 Redis 在多副本场景下的稳定性 + 缓存 invalidation 一致性 → [RISK-012](../risk-analysis.md)）
