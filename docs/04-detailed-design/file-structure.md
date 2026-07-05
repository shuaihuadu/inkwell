---
id: file-structure
title: Inkwell 后端文件结构汇总
stage: H3
status: draft
reviewers: []
upstream:
  - ADR-017
  - ADR-019
  - ADR-021
  - HD-001
downstream: []
---

> 本文件由 H3 起草过的所有模块**累加贡献**——每个模块的 HD 在自己 §12 同步追加 `## <module>` 一级章节。**只追加**，禁止删除或改动其他模块章节。
>
> 全文最终在所有 HD 起草完毕后由人工评审统一翻 `status: reviewed`。
>
> **命名空间 / file-scoped namespace / GlobalUsings / UTF-8 + LF 横切规约**：走 [HD-001 §14](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#14-命名空间与代码风格规约横切规约)。本文件的文件树章节只列路径不列命名空间，避免于§14 冲突。`.editorconfig` / `.gitattributes` 实体文件由 H5 起步任务在仓根创建——本文件**不**重复列出仓根文件。

## 总体拓扑（参考 [ADR-017](../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) + [ADR-019](../03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md) + [ADR-021](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）

```text
src/core/
  Inkwell.Abstractions/             # 端口层（HD-001 起头）
  Inkwell.Core/                     # 业务层 + 默认 Provider
  Inkwell.WebApi/                   # HTTP 入口（ASP.NET Core minimal-host）
  Inkwell.Worker/                   # 后台进程（.NET Generic Host + BackgroundService）
  providers/
    Inkwell.Persistence.EFCore/             # EFCore family shared base（ADR-021）
    Inkwell.Persistence.EFCore.InMemory/    # InMemory final adapter
    Inkwell.Persistence.EFCore.SqlServer/   # SqlServer final adapter
    Inkwell.Persistence.EFCore.Postgres/    # Postgres final adapter
    Inkwell.FileStorage.MinIO/
    Inkwell.FileStorage.AzureBlob/
    Inkwell.Cache.Redis/
    Inkwell.Queue.Redis/
    Inkwell.VectorStore.Qdrant/

tests/core/
  Inkwell.Abstractions.Tests/
  ...
  Inkwell.Providers.Contract/       # 跨 Provider 契约测试包（RISK-002 / 011 / 012 缓解地）
```

## Inkwell.Abstractions

> 由 [HD-001 §2 / §3](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) 锁定。
>
> **csproj 依赖白名单**：仅 `Microsoft.Extensions.{DependencyInjection,Configuration,Options,Logging}.Abstractions` + `Microsoft.Extensions.VectorData.Abstractions`（[ADR-020](../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md)，HD-008 起用）。

```text
src/core/Inkwell.Abstractions/
  Inkwell.Abstractions.csproj
  Common/
    InkwellException.cs                # class InkwellException + InkwellConfigurationException + InkwellBuilderException
    Pagination.cs                      # record class Pagination(int Page, int PageSize)
    SortOrder.cs                       # record class SortOrder(string Field, SortDirection Direction)
    TimeRange.cs                       # record class TimeRange(DateTimeOffset Start, DateTimeOffset End)
    AuditContext.cs                    # record class AuditContext(...)
  Builder/
    IInkwellBuilder.cs                 # public interface IInkwellBuilder
    InkwellBuilder.cs                  # internal sealed class，AddInkwell() 唯一实现
    InkwellServiceCollectionExtensions.cs   # public static AddInkwell() 入口
  Options/
    InkwellOptions.cs                  # 根 Options，含 ServiceName / Environment / Providers 选择器 + 6 个端口子 Options 槽位
    InkwellProvidersOptions.cs         # F9 选择器：Inkwell:Providers 段（6 个端口 Provider 名称）
    InkwellOptionsValidator.cs         # IValidateOptions<InkwellOptions>
  Persistence/                         # HD-002 锁定（端口契约层；Entity 不在此处，落 HD-009 providers/）
    IPersistenceProvider.cs            # 顶层 facade（ExecuteInTransactionAsync + SaveChangesAsync）
    IRepository.cs                     # IRepository<TModel, TKey> 纯 marker interface（20260-5-11 errata；具名后缀限定 `IXxxRepository`，F7）
    IUnitOfWork.cs                     # 事务作用域，仅在 ExecuteInTransactionAsync lambda 内可见
    PagedResult.cs                     # record class PagedResult<T>(IReadOnlyList<T>, long, Pagination)
    PersistenceOptions.cs              # ConnectionString / 超时 / AutoSeedOnStartup（不再含 Provider 字段，F9）
    PersistenceOptionsValidator.cs     # IValidateOptions<PersistenceOptions>（不再负责 Provider 白名单，F9）
    Mixins/
      IHasTimestamps.cs                # CreatedTime + UpdatedTime（F2：DateTimeOffset UTC，不再名 AtUtc）
      IHasRowVersion.cs                # byte[] RowVersion（乐观并发）
      IHasOwner.cs                     # Guid OwnerUserId
    Agents/                            # 业务 Model + 具名 Repository 接口共置（需业务 HD 起草后追加）
      AgentDefinition.cs               # 业务 Model，撞名降级 Definition（与 Microsoft.Agents.AI.AIAgent 区分，详 HD-002 §4.1.2）
      IAgentRepository.cs              # 继承 IRepository<AgentDefinition, Guid> + 6 个具名动词方法（§4.1.3）
```

**文件计数**：11（HD-001）+ 8（HD-002 本体）= 19 个 `*.cs` + 1 个 `.csproj`；**业务命名空间在 `Persistence/<Module>/` 下追加的 `<TypeName>.cs` + `IXxxRepository.cs` 双件套**由各业务 HD 独立计数，不计入 HD-002 本身。

> **2026-05-12 errata（[ADR-023 errata·01](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) + [errata·02](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常)）**：HD-001 删 `Common/Result.cs` + `Common/Error.cs` + `ErrorCodes.cs`，计数 13 → 11；HD-002 本体删 `ErrorCodes.Persist.cs`，计数 9 → 8；错误处理全走 .NET BCL 异常类型 + OTel `exception.*` 五字段（详 HD-002 §4.3 BCL 对照表）。
>
> **2026-05-10 errata**：HD-001 从 12 项 → 13 项（新增 `Options/InkwellProvidersOptions.cs`，F9）；HD-002 时间字段 `CreatedAtUtc` / `UpdatedAtUtc` → `CreatedTime` / `UpdatedTime`（F2 + F5 解释 A）。
>
> **2026-05-11 errata**：HD-002 `IRepository.cs` 从 generic CRUD base 退化为纯 marker interface（[ADR-022](../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md)）；同时锁定业务命名空间在 `Persistence/<Module>/` 子目录追加「业务 Model + 具名 Repository 接口」双件套模板（如 `Persistence/Agents/AgentDefinition.cs` + `IAgentRepository.cs`）——Model 默认无后缀，撞名场景降级 `<TypeName>Definition.cs`（详 HD-002 §4.1.2）。

**端口接口文件**（具体接口 `IFileStorageProvider` / `ICacheProvider` / `IQueueProvider` / `IAgentRuntime` / `IAuditLogger`）由 HD-003 ~ HD-007 各自追加，路径建议：

> **2026-05-11 errata**（[design-review-report §7 N9](design-review-report.md#7-hd-003-filestorage-port-增量评审2026-05-11)）：HD-003 已锁定 `Inkwell.Abstractions/FileStorage/` 子目录完整 8 文件清单（`IFileStorageProvider.cs` / 4 DTO / `FileStorageOptions.cs` + Validator / `ErrorCodes.FileStore.cs`），详 [§Inkwell.Abstractions.FileStorage](#inkwellabstractionsfilestorage)。下方 FileStorage 子目录 2 文件为 H1 草图占位，已被 §Inkwell.Abstractions.FileStorage 完整章节超越；HD-004 ~ HD-007 同样在起草后各自追加 `\#\# Inkwell.Abstractions.<Module>` 一级章节提供完整清单。

```text
src/core/Inkwell.Abstractions/
  FileStorage/
    IFileStorageProvider.cs            # HD-003 锁定
    FileStorageOptions.cs              # HD-003 锁定
  Cache/
    ICacheProvider.cs                  # HD-004 锁定
    CacheOptions.cs                    # HD-004 锁定
  Queue/
    IQueueProvider.cs                  # HD-005 锁定
    QueueOptions.cs                    # HD-005 锁定
    MessageEnvelope.cs                 # HD-005 锁定（含 traceparent 字段，RISK-015）
  AgentRuntime/
    IAgentRuntime.cs                   # HD-006 锁定
    AgentRuntimeOptions.cs             # HD-006 锁定
  Audit/
    IAuditLogger.cs                    # HD-007 锁定
    AuditLoggerOptions.cs              # HD-007 锁定
  VectorStore/                          # HD-008 锁定（type-alias + Builder DSL 钩子，不重新发明 IVectorStore）
```

> **关于 Entity 类的归属**：[ADR-021](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 锁定 Entity（`AgentEntity` / `ConversationEntity` / 等）**集中在 `providers/Inkwell.Persistence.EFCore/Entities/`**，不在 `Inkwell.Abstractions/`。HD-002 在 `Persistence/` 子目录仅定义抽象（facade + IRepository marker + mixin + Options），具名 `IXxxRepository` 与业务 Model（默认无后缀 / 撞名降级 `XxxDefinition`，详 [HD-002 §4.1.2](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)）由各业务命名空间 HD 起草时在 `Persistence/<Module>/` 追加。示例（仅 HD-002 锁定模板，具体文件由对应业务 HD 创建）：
>
> ```text
> src/core/Inkwell.Abstractions/Persistence/
>   Agents/
>     AgentDefinition.cs               # 撞 MAF Microsoft.Agents.AI.AIAgent 降级
>     IAgentRepository.cs              # 6 个具名动词方法
>   Tools/
>     ToolDefinition.cs                # 撞 "运行时 Tool" 语义降级
>     IToolRepository.cs
>   Skills/
>     SkillDefinition.cs               # 与 ADR-010 静态加载 Skill 区分
>     ISkillRepository.cs
>   Triggers/
>     TriggerDefinition.cs             # 与 运行时 Trigger 区分
>     ITriggerRepository.cs
>   Conversations/
>     Conversation.cs                  # 默认无后缀
>     IConversationRepository.cs
>     Message.cs
>     IMessageRepository.cs
>     AguiRunEvent.cs
>     IAguiRunEventRepository.cs
>   KnowledgeBase/
>     KnowledgeBase.cs
>     IKnowledgeBaseRepository.cs
>     KbDocument.cs
>     IKbDocumentRepository.cs
>     KbChunk.cs
>     IKbChunkRepository.cs
>   Memory/
>     MemoryItem.cs
>     IMemoryItemRepository.cs
>   Orchestrations/
>     Orchestration.cs
>     IOrchestrationRepository.cs
>     OrchestrationRun.cs
>     IOrchestrationRunRepository.cs
>   Traces/
>     Trace.cs
>     ITraceRepository.cs
>   AuditLogs/
>     AuditLog.cs
>     IAuditLogRepository.cs
>   Auth/
>     User.cs
>     IUserRepository.cs
>   PublicApi/
>     PublicApiToken.cs
>     IPublicApiTokenRepository.cs
>   Versioning/
>     AgentVersion.cs                  # AgentVersion 本身不撞，保持无后缀
>     IAgentVersionRepository.cs
> ```
>
> 业务 HD 在 `Persistence/<Module>/` 下遵循「filename = classname.cs」与「Model + 具名 Repo 同模块共置」原则（详 [HD-002 §4.1.4](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)）。

## Inkwell.Abstractions.FileStorage

> 由 [HD-003 §2 / §3](Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md) 锁定。
>
> 与 [HD-001 §Inkwell.Abstractions](#inkwellabstractions) 同 csproj；本节仅追加 `FileStorage/` 子目录与 `ErrorCodes.FileStore.cs` partial 文件——`Inkwell.Abstractions.csproj` 依赖白名单不变（仅 `Microsoft.Extensions.{DependencyInjection,Configuration,Options,Logging}.Abstractions` + `Microsoft.Extensions.VectorData.Abstractions`）。
>
> [picker Q3=B](Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md) 不锁容器名常量集——`InkwellContainers` **不**出现在端口层；业务命名空间（`Inkwell.Core.KnowledgeBase` / `.Multimodal` / `.AuditLogs`）各自传字符串。

```text
src/core/Inkwell.Abstractions/
  FileStorage/                           # 新增子目录
    IFileStorageProvider.cs              # 顶层 facade（7 方法：Upload/Download/Exists/Delete/Presign×2/List）
    FileMetadata.cs                      # record，ContentType + CustomMetadata + ContentDisposition
    FileUploadResult.cs                  # record，Container + Key + SizeBytes + ETag + UploadedTime
    FileDownloadResponse.cs              # class IAsyncDisposable + IDisposable，Stream + Metadata + ETag + SizeBytes + UploadedTime
    FileObjectInfo.cs                    # record，Container + Key + SizeBytes + ETag + LastModifiedTime + ContentType?
    FileStorageOptions.cs                # TTL × 4 + MaxObjectSizeBytes + ListPageSize + EnableSensitiveDataLogging
    FileStorageOptionsValidator.cs       # IValidateOptions<FileStorageOptions>，跨字段校验 TTL 上下限
```

**文件计数**：HD-003 新增 7 个 `*.cs`（FileStorage/ 7）；Abstractions csproj 累计 11（HD-001）+ 8（HD-002 本体）+ 7（HD-003）= 26 个 `*.cs` + 1 个 `.csproj`。

> **2026-05-12 errata（[ADR-023 errata·01](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流)）**：HD-003 删 `ErrorCodes.FileStore.cs`，HD-003 计数 8 → 7；FileStorage 端口错误处理同步翻走 .NET BCL 异常类型（详 HD-003 后续会话的 errata）。

**对接 Provider HD 的契约**：

```text
src/core/Inkwell.Core/FileStorage/
  LocalFileSystemFileStorageProvider.cs              # 默认 dev / unit test，进程内（HD-Core 起草）
  LocalFileSystemFileStorageOptions.cs               # RootPath + 子目录策略 + sidecar 元数据格式
  LocalFileSystemBuilderExtensions.cs                # UseLocalFileSystemFileStorage()

providers/Inkwell.FileStorage.MinIO/
  MinIOFileStorageProvider.cs                        # MinIO SDK 实现（独立 HD）
  MinIOFileStorageOptions.cs                         # Endpoint / AccessKey / SecretKey / UseSsl / BucketAutoCreate
  MinIOFileStorageBuilderExtensions.cs               # UseMinIOFileStorage(IConfigurationSection)

providers/Inkwell.FileStorage.AzureBlob/
  AzureBlobFileStorageProvider.cs                    # Azure.Storage.Blobs SDK 实现（独立 HD）
  AzureBlobFileStorageOptions.cs                     # ConnectionString | (AccountName + AccountKey) | (AccountName + ManagedIdentity)
  AzureBlobFileStorageBuilderExtensions.cs           # UseAzureBlobFileStorage(IConfigurationSection)
```

> Provider 子 Options（连接字符串 / 凭证 / SAS 策略 / 桶自动创建等）由各 Provider HD 独立锁定；**严禁**回填到 `Inkwell.Abstractions/FileStorage/FileStorageOptions.cs`（违反 [ADR-017 端口零外部包约束](../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) + [RISK-011 三 Provider contract 漏出](../03-architecture/risk-analysis.md)）。

## Inkwell.Abstractions.Cache

> 由 [HD-004 §2 / §3](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md) 锁定。
>
> 与 [HD-001 §Inkwell.Abstractions](#inkwellabstractions) 同 csproj；本节仅追加 `Cache/` 子目录——`Inkwell.Abstractions.csproj` 依赖白名单不变（仅 `Microsoft.Extensions.{DependencyInjection,Configuration,Options,Logging}.Abstractions` + `Microsoft.Extensions.VectorData.Abstractions` + BCL 内置 `System.Text.Json`）。
>
> [picker Q-key-convention=A](Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md) 不锁 Key 命名规范强制机制——`CacheKeyBuilder` **不**出现在端口层；业务命名空间各自按 [ADR-016 §Key 命名约定](../03-architecture/adr/ADR-016-cache-provider-redis.md) `{tenant}:{module}:{purpose}:{id}` 拼接字符串。

```text
src/core/Inkwell.Abstractions/
  Cache/                                  # 新增子目录
    ICacheProvider.cs                     # 顶层 facade（7 方法：Get/Set/Remove/Exists/Increment/TryAcquireLock/ReleaseLock）
    CacheEntryOptions.cs                  # record，SetAsync 强制 TTL 载体（AbsoluteExpirationRelativeToNow）
    CacheOptions.cs                       # MinTtlSeconds + MaxTtlSeconds + DefaultLockTtlSeconds + EnableSensitiveDataLogging
    CacheOptionsValidator.cs              # IValidateOptions<CacheOptions>，跨字段校验 TTL 上下限
```

**文件计数**：HD-004 新增 4 个 `*.cs`（Cache/ 4）；Abstractions csproj 累计 11（HD-001）+ 8（HD-002 本体）+ 7（HD-003）+ 4（HD-004）= 30 个 `*.cs` + 1 个 `.csproj`。

**对接 Provider HD 的契约**：

```text
src/core/Inkwell.Core/Cache/
  InMemoryCacheProvider.cs                           # 默认 dev / unit test，进程内 dictionary（独立 HD）
  InMemoryCacheOptions.cs                            # 无额外字段（占位，进程内实现无需连接配置）
  InMemoryCacheBuilderExtensions.cs                  # UseInMemoryCache()

providers/Inkwell.Cache.Redis/
  RedisCacheProvider.cs                              # StackExchange.Redis SDK 实现（独立 HD）
  RedisCacheOptions.cs                               # ConnectionString
  RedisCacheBuilderExtensions.cs                     # UseRedisCache(string connectionString)
```

> Provider 子 Options（连接字符串等）由各 Provider HD 独立锁定；**严禁**回填到 `Inkwell.Abstractions/Cache/CacheOptions.cs`（违反 [ADR-017 端口零外部包约束](../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) + [RISK-011 三 Provider contract 漏出](../03-architecture/risk-analysis.md) / [RISK-012 Redis 单点与 invalidation 一致性](../03-architecture/risk-analysis.md)）。

## Inkwell.Abstractions.Queue

> 由 [HD-005 §2 / §3](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md) 锁定。
>
> 与 [HD-001 §Inkwell.Abstractions](#inkwellabstractions) 同 csproj；本节仅追加 `Queue/` 子目录——`Inkwell.Abstractions.csproj` 依赖白名单不变（仅 `Microsoft.Extensions.{DependencyInjection,Configuration,Options,Logging}.Abstractions` + `Microsoft.Extensions.VectorData.Abstractions` + BCL 内置 `System.Text.Json` + `System.Diagnostics.Activity`）。
>
> [picker Q-queuename-convention=A](Inkwell.Abstractions/HD-005-Inkwell.Abstractions-queue-port.md) 不锁队列名命名规约——`InkwellQueues` **不**出现在端口层；业务命名空间各自拼接队列名（如 `kb-ingest` / `trigger-fanout`）。`MessageEnvelope.TraceParent` 字段满足 [RISK-015](../03-architecture/risk-analysis.md) 跨进程 trace 不断链要求。

```text
src/core/Inkwell.Abstractions/
  Queue/                                  # 新增子目录
    IQueueProvider.cs                     # 顶层 facade（4 方法：Enqueue/Dequeue/Acknowledge/NegativeAcknowledge）
    MessageEnvelope.cs                    # record<T>，MessageId + Payload + EnqueuedTime + DeliveryCount + TraceParent（RISK-015）
    QueueOptions.cs                       # MaxDeliveryAttempts + VisibilityTimeoutSeconds + DlqRetentionHours + EnableSensitiveDataLogging
    QueueOptionsValidator.cs              # IValidateOptions<QueueOptions>
```

**文件计数**：HD-005 新增 4 个 `*.cs`（Queue/ 4）；Abstractions csproj 累计 11（HD-001）+ 8（HD-002 本体）+ 7（HD-003）+ 4（HD-004）+ 4（HD-005）= 34 个 `*.cs` + 1 个 `.csproj`。

**对接 Provider HD 的契约**：

```text
src/core/Inkwell.Core/Queue/
  ChannelsQueueProvider.cs                           # 默认 dev / unit test，基于 System.Threading.Channels（独立 HD）
  ChannelsQueueOptions.cs                            # 无额外必填字段（占位，进程内实现无需连接配置）
  ChannelsQueueBuilderExtensions.cs                  # UseChannelsQueue()；未显式调用时 InkwellBuilder.Build() 默认自动注册（ADR-018）

providers/Inkwell.Queue.Redis/
  RedisStreamQueueProvider.cs                        # StackExchange.Redis Streams SDK 实现（独立 HD）
  RedisQueueOptions.cs                               # ConnectionString
  RedisQueueBuilderExtensions.cs                     # UseRedisQueue(string connectionString)
```

> Provider 子 Options（连接字符串等）由各 Provider HD 独立锁定；**严禁**回填到 `Inkwell.Abstractions/Queue/QueueOptions.cs`（违反 [ADR-017 端口零外部包约束](../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) + [RISK-011 三 Provider contract 漏出](../03-architecture/risk-analysis.md) / [RISK-014 队列可靠性残余风险](../03-architecture/risk-analysis.md) / [RISK-015 双进程版本漂移与 OTel 双 source](../03-architecture/risk-analysis.md)）。

## Inkwell.Abstractions.AgentRuntime

> 由 [HD-006 §2 / §3](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) 锁定。
>
> 与 [HD-001 §Inkwell.Abstractions](#inkwellabstractions) 同 csproj；本节仅追加 `AgentRuntime/` 子目录——`Inkwell.Abstractions.csproj` 依赖白名单不变（仅 `Microsoft.Extensions.{DependencyInjection,Configuration,Options,Logging}.Abstractions` + `Microsoft.Extensions.VectorData.Abstractions` + BCL 内置 `System.Text.Json`）。**严禁**引入 `Microsoft.Agents.AI.*` 等任何 MAF 包（[ADR-017 §依赖规则第 3/4 条](../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) + [RISK-001](../03-architecture/risk-analysis.md) MAF 接触面收敛约束）——`Inkwell.Core.AgentRuntime` 命名空间是**唯一**允许 `using Microsoft.Agents.AI.*` 的位置，本端口接口与 DTO 全部使用 Inkwell 自有类型，不泄漏 `AIAgent` / `AgentSession` / `ChatMessage` / `AgentResponse` / `AgentResponseUpdate` 等 MAF 类型。

```text
src/core/Inkwell.Abstractions/
  AgentRuntime/                          # 新增子目录
    IAgentRuntime.cs                     # 顶层 facade（3 方法：RunAsync/RunStreamingAsync/CancelRunAsync）
    AgentRunRequest.cs                   # record，一次 Run 调用的全部已解析上下文
    AgentTurnResult.cs                   # record，非流式最终结果（含 ToolCalls 回溯）
    AgentChatMessage.cs                  # record，对话消息（Role + Content 封闭子类型族）
    AgentMessageContentPart.cs           # abstract record + TextPart/ImagePart/DocumentPart
    AgentModelParameters.cs              # record，temperature/top_p/max_tokens（REQ-006）
    AgentToolDefinition.cs               # record，工具描述 + 同进程调用委托 + AgentToolCallRecord（REQ-007）
    AgentRunEvent.cs                     # abstract record + 6 个 sealed 子类型（流式事件，对应 AG-UI 四大类）
    AgentRuntimeOptions.cs               # DefaultTemperature/DefaultTopP/DefaultMaxTokens/RunTimeoutSeconds/EnableSensitiveDataLogging
    AgentRuntimeOptionsValidator.cs      # IValidateOptions<AgentRuntimeOptions>
```

**文件计数**：HD-006 新增 10 个 `*.cs`（AgentRuntime/ 10）；Abstractions csproj 累计 11（HD-001）+ 8（HD-002 本体）+ 7（HD-003）+ 4（HD-004）+ 4（HD-005）+ 10（HD-006）= 44 个 `*.cs` + 1 个 `.csproj`。

**对接 `Inkwell.Core.AgentRuntime` 的契约**（无独立 Provider csproj，[ADR-017 §依赖规则](../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) 下 `Inkwell.AgentRuntime` 合并进 `Inkwell.Core.AgentRuntime` 命名空间）：

```text
src/core/Inkwell.Core/AgentRuntime/
  AzureOpenAIAgentRuntime.cs                         # 唯一允许 using Microsoft.Agents.AI.* 的位置（独立 HD）
  AzureOpenAIAgentRuntimeOptions.cs                  # Endpoint / ApiKey / DeploymentName
  AzureOpenAIAgentRuntimeBuilderExtensions.cs        # UseAzureOpenAIAgentRuntime(...)
```

> Provider（模型云服务）特定凭证由 `Inkwell.Core.AgentRuntime` 独立锁定；**严禁**回填到 `Inkwell.Abstractions/AgentRuntime/AgentRuntimeOptions.cs`（违反 [ADR-017 端口零外部包约束](../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) + [RISK-001 MAF 接触面收敛约束](../03-architecture/risk-analysis.md)）。

## Inkwell.Abstractions.Audit

> 由 [HD-007 §2 / §3](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md) 锁定。
>
> 与 [HD-001 §Inkwell.Abstractions](#inkwellabstractions) 同 csproj；本节仅追加 `Audit/` 子目录——`Inkwell.Abstractions.csproj` 依赖白名单不变（仅 `Microsoft.Extensions.{DependencyInjection,Configuration,Options,Logging}.Abstractions` + `Microsoft.Extensions.VectorData.Abstractions` + BCL 内置 `System.Text.Json`）。本端口**无**可切换 Provider（[HD-007 §1.3 Q-implementation-topology](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#13-关键决策摘要)）——`InkwellProvidersOptions`（[HD-001 §3.11.1](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#3111-optionsinkwellprovidersoptionscsf9-新增)）**不**新增 `Audit` 字段。

```text
src/core/Inkwell.Abstractions/
  Audit/                                  # 新增子目录
    IAuditLogger.cs                       # 顶层 facade（2 方法：LogAsync/QueryAsync）
    AuditLogRequest.cs                    # record，写入请求（AuditContext + ActorType + AgentId? + ResultCode + ErrorCode?）
    AuditLogEntry.cs                      # record，查询结果单条记录（对齐 ADR-008 表结构关键字段）
    AuditLogQuery.cs                      # record，查询过滤条件（TimeRange + Pagination + 可选 ActorUserId/EventType/AgentId）
    AuditEnums.cs                         # AuditActorType（3 值闭集）+ AuditResultCode（2 值闭集）
    AuditLoggerOptions.cs                 # RetentionDays/MaxQueryTimeRangeDays/DefaultPageSize/MaxPageSize/EnableSensitiveDataLogging
    AuditLoggerOptionsValidator.cs        # IValidateOptions<AuditLoggerOptions>
```

**文件计数**：HD-007 新增 7 个 `*.cs`（Audit/ 7）；Abstractions csproj 累计 11（HD-001）+ 8（HD-002 本体）+ 7（HD-003）+ 4（HD-004）+ 4（HD-005）+ 10（HD-006）+ 7（HD-007）= 51 个 `*.cs` + 1 个 `.csproj`。

**对接 `Inkwell.Core.AuditLogs` 的契约**（无独立 Provider csproj，[HD-007 §1.3 Q-implementation-topology](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#13-关键决策摘要) 下 `Inkwell.AuditLogs` 合并进 `Inkwell.Core.AuditLogs` 命名空间）：

```text
src/core/Inkwell.Core/AuditLogs/
  DefaultAuditLogger.cs                              # 唯一 IAuditLogger 实现（独立 HD）
  AuditLoggerBuilderExtensions.cs                     # AddDefaultAuditLogger()
```

> `Persistence/AuditLogs/AuditLog.cs` + `IAuditLogRepository.cs`（业务 Model + 具名 Repository，按 [HD-002 §Inkwell.Abstractions 已预留模板](#inkwellabstractions) 追加）由 `Inkwell.Core.AuditLogs` 业务 HD 起草；磁盘 fallback 文件格式 / 后台清理任务由该 HD 独立锁定，**严禁**回填到 `Inkwell.Abstractions/Audit/AuditLoggerOptions.cs`（违反 [ADR-017 端口零外部包约束](../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md)）。

## Inkwell.Abstractions.VectorStore

> 由 [HD-008 §2 / §3](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md) 锁定。
>
> 与 [HD-001 §Inkwell.Abstractions](#inkwellabstractions) 同 csproj；本节仅追加 `VectorStore/` 子目录 + `GlobalUsings.cs` / `Options/InkwellOptions.cs` 两处既有文件追加行——`Inkwell.Abstractions.csproj` 依赖白名单：`Microsoft.Extensions.{DependencyInjection,Configuration,Options,Logging}.Abstractions` + `Microsoft.Extensions.VectorData.Abstractions`（HD-008 起草时新增）+ `Microsoft.Extensions.AI.Abstractions`（2026-07-06 errata·第六轮对称纳入，详 [HD-001 §13 B15](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#2026-07-06-errata第六轮b15-对称纳入-microsoftextensionsaiabstractions-白名单)）+ BCL 内置 `System.Text.Json`。本 HD **不设计新接口**——业务命名空间直接使用 `Microsoft.Extensions.VectorData.VectorStore` / `VectorStoreCollection<TKey, TRecord>`，不新增任何 `IVectorStore` 包装类型（[ADR-020](../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) + [HD-008 §1.1](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#11-职责)）。

```text
src/core/Inkwell.Abstractions/
  GlobalUsings.cs                        # （既有文件追加行）+= global using Microsoft.Extensions.VectorData;
  VectorStore/                           # 新增子目录
    VectorStoreOptions.cs                # Provider 无关：EmbeddingModelName/EmbeddingDimensions/DistanceMetric/EnableSensitiveDataLogging
    VectorStoreOptionsValidator.cs       # IValidateOptions<VectorStoreOptions>
  Options/
    InkwellOptions.cs                    # （既有文件追加字段）+= public VectorStoreOptions VectorStore { get; init; } = new();
```

**文件计数**：HD-008 新增 2 个 `*.cs`（`VectorStore/` 2）；`GlobalUsings.cs` / `Options/InkwellOptions.cs` 为既有文件追加行，不计入新增文件数。Abstractions csproj 累计 11（HD-001）+ 8（HD-002 本体）+ 7（HD-003）+ 4（HD-004）+ 4（HD-005）+ 10（HD-006）+ 7（HD-007）+ 2（HD-008）= **53** 个 `*.cs` + 1 个 `.csproj`。

**Builder DSL 扩展方法签名声明（不含实现，[HD-008 §4](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#4-builder-dsl-签名声明不含实现) 锁定签名）**：

```text
src/core/Inkwell.Core/VectorStore/                          # 独立 HD（无独立 Provider csproj，[HD-008 §1.3 Q1-embedding-topology](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#13-关键决策摘要)）
  InMemoryVectorStoreBuilderExtensions.cs                    # UseInMemoryVectorStore(...)
  AzureOpenAIEmbeddingOptions.cs                             # Endpoint / ApiKey / DeploymentName
  AzureOpenAIEmbeddingBuilderExtensions.cs                   # UseAzureOpenAIEmbeddings(...)

providers/Inkwell.VectorStore.Qdrant/                        # 独立 HD（[HD-008 §1.3 Q2-qdrant-options-loc](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#13-关键决策摘要)）
  QdrantVectorStoreOptions.cs                                # Host / Port / ApiKey / UseHttps
  QdrantVectorStoreBuilderExtensions.cs                      # UseQdrantVectorStore(...)
```

> Provider 子 Options（Qdrant 连接参数 / Azure OpenAI 凭据）由各自 Provider HD 独立锁定；**严禁**回填到 `Inkwell.Abstractions/VectorStore/VectorStoreOptions.cs`（违反 [ADR-017 端口零外部包约束](../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md)）。`IEmbeddingGenerator<string, Embedding<float>>` 在 KB / Memory 业务命名空间的直接消费方式**已确认**：`Microsoft.Extensions.AI.Abstractions` 对称纳入 `Inkwell.Abstractions.csproj` 依赖白名单 + `GlobalUsings.cs`，与 `Microsoft.Extensions.VectorData.Abstractions` 处理同构，允许直接注入、不新增门面接口（[HD-008 §13 Q5](Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md#13-决策记录) + [design-review-report.md §18 B15](../design-review-report.md#b15q5比照-vectordata-先例缺物理落地机制iembeddinggenerator-依赖白名单例外未实际生效c91)，已处理（2026-07-06）；不再是开放问题）。

## providers/Inkwell.Persistence.EFCore

> 由 [HD-009](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) 锁定。
>
> [ADR-022 §决策](../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 锁手写 `XxxMappingExtensions` 静态类扩展方法三件套（`ToModel()` / `ToEntity()` / `SelectAsModel()`）；具名 Repository 在 `Repositories/` 中以 `internal sealed class XxxRepository : IXxxRepository` 实现 6 个具名动词方法。

```text
providers/Inkwell.Persistence.EFCore/
  Inkwell.Persistence.EFCore.csproj   # 依赖 Microsoft.EntityFrameworkCore + Inkwell.Abstractions
  Entities/                           # 集中的 EF Entity 类（~30 个）
    AgentEntity.cs
    ConversationEntity.cs
    …
  Configurations/                     # IEntityTypeConfiguration<TEntity>（~30 个）
    AgentEntityConfiguration.cs
    ConversationEntityConfiguration.cs
    …
  Mapping/                            # ADR-022 锁手写 Extensions（~30 个）
    AgentMappingExtensions.cs         # ToModel() / ToEntity() / SelectAsModel()
    ConversationMappingExtensions.cs
    MessageMappingExtensions.cs
    KnowledgeBaseMappingExtensions.cs
    KbDocumentMappingExtensions.cs
    KbChunkMappingExtensions.cs
    MemoryItemMappingExtensions.cs
    ToolMappingExtensions.cs
    SkillMappingExtensions.cs
    TriggerMappingExtensions.cs
    OrchestrationMappingExtensions.cs
    OrchestrationRunMappingExtensions.cs
    TraceMappingExtensions.cs
    AuditLogMappingExtensions.cs
    UserMappingExtensions.cs
    PublicApiTokenMappingExtensions.cs
    AgentVersionMappingExtensions.cs
    AguiRunEventMappingExtensions.cs
    …·~30 个主实体（1 entity = 1 extensions 类）
  Repositories/                       # 具名 Repository 实现（~30 个）
    AgentRepository.cs                # internal sealed class : IAgentRepository
    ConversationRepository.cs
    MessageRepository.cs
    KnowledgeBaseRepository.cs
    KbDocumentRepository.cs
    KbChunkRepository.cs
    MemoryItemRepository.cs
    ToolRepository.cs
    SkillRepository.cs
    TriggerRepository.cs
    OrchestrationRepository.cs
    OrchestrationRunRepository.cs
    TraceRepository.cs
    AuditLogRepository.cs
    UserRepository.cs
    PublicApiTokenRepository.cs
    AgentVersionRepository.cs
    AguiRunEventRepository.cs
    …·~30 个业务（1 IXxxRepository = 1 XxxRepository 类）
  InkwellDbContext.cs                 # DbContext，OnModelCreating 扫描 Configurations/
  EfCorePersistenceProvider.cs        # 唯一 IPersistenceProvider 实现（ADR-021 D1）
  Interceptors/
    AuditingSaveChangesInterceptor.cs # 联动 IHasTimestamps / IHasRowVersion / IHasOwner 三 mixin（HD-009 §3.3）
  IDbContextInitializer.cs            # final adapter 之间 Migrate vs EnsureCreated 的抽象（HD-009 §3.6）
  InkwellSeeder.cs                    # 幂等 seed
  MigrationRunner.cs                  # 启动期 Migration runner
  DependencyInjection/
    InkwellPersistenceEfCoreServiceCollectionExtensions.cs # 注册 base 服务（HD-009 §3.11）
  BannedSymbols.txt                   # CI 强制 banlist（HD-009 §3.12）
```

> **计数估算**：base 8 个 `*.cs` + 1 个 `BannedSymbols.txt` + 1 个 `.csproj`（HD-009 锁定）；Entities / Configurations / Mapping / Repositories 四子目录每个约 ~18 个业务实体（详 [HD-009 §3.13](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)），共 18 × 4 = 72 个业务实体文件，随业务 HD 与对应 H5 编码任务陆续落地。`Mapping/` 与 `Repositories/` 是 [ADR-022](../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 2026-05-11 锁定的两个新增子目录（原 ADR-021 拓扑仅含 Entities / Configurations / DbContext / Provider / Seeder / MigrationRunner）。
>
> **2026-05-11 errata（HD-009 起草）**：本节由 HD-009 从「未起草」→「已起草」翻面；`Interceptors/AuditingSaveChangesInterceptor.cs` + `IDbContextInitializer.cs` + `DependencyInjection/InkwellPersistenceEfCoreServiceCollectionExtensions.cs` + `BannedSymbols.txt` 四项为 HD-009 落地新增。

## Errata 记录（2026-05-12：ADR-023 三轮 errata 跨 HD 同步）

本文件 `status: draft` 期间，根据 [ADR-023 主决策](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) + [errata·01](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) + [errata·02](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 三轮 accepted by Inkwell 2026-05-11，同步落以下变更（已嵌入 §Inkwell.Abstractions / §Inkwell.Abstractions.FileStorage 两节，本节是变更摘要）：

- **HD-001 `Common/Result.cs` + `Common/Error.cs` + `ErrorCodes.cs` 删除**（[ADR-023 errata·01 + errata·02](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)）：ADR-023 errata·01 废错误码机制（不再需 `ErrorCodes.cs`） + errata·02 删 `Result<T>` / `Error` 抽象（不再需 `Common/Result.cs` / `Common/Error.cs`）；HD-001 计数 13 → 11。
- **HD-002 `ErrorCodes.Persist.cs` 删除**（[ADR-023 errata·01](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流)）：INK-PERSIST-001 ~ 013 全部作废；Persist 端口错误走 [HD-002 §4.3](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) BCL 对照表；HD-002 本体计数 9 → 8。
- **HD-003 `ErrorCodes.FileStore.cs` 删除**（[ADR-023 errata·01](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流)）：INK-FILESTORE-001 ~ 009 全部作废；FileStorage 端口错误同步翻走 BCL 异常类型（详 HD-003 后续会话 errata）；HD-003 计数 8 → 7。
- **总计数**：Abstractions csproj 累计 11（HD-001）+ 8（HD-002 本体）+ 7（HD-003）= 26 个 `*.cs` + 1 个 `.csproj`（从 30 减 4）。
- **上游证据链**：[HD-001 §13 第三 / 第四轮 errata](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#13-errata-记录) + [HD-002 §13.3 / §13.4 errata](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#13-errata-记录-2026-05-10) + [HD-009 §13.3 / §13.4 errata](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)。
- **下游待办**：HD-003 下一会话起草 frontmatter 加二+三+四轮 errata callout + §1.3 picker Q1/Q2 标 superseded + §4.3 错误表重写 BCL 对照表 + §2 文件树删 `ErrorCodes.FileStore.cs`。
