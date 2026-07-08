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
    Agents/                            # 业务 Model + 具名 Repository 接口共置（已由 HD-015 落地为真实文件，见下方 §Persistence/Agents 小节）
      AgentDefinition.cs               # 业务 Model，撞名降级 Definition（与 Microsoft.Agents.AI.AIAgent 区分，详 HD-002 §4.1.2）
      IAgentRepository.cs              # 继承 IRepository<AgentDefinition, Guid> + 7 个具名动词方法（§4.1.3）
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
>     AgentDefinition.cs               # 撞 MAF Microsoft.Agents.AI.AIAgent 降级（已由 HD-015 落地为真实文件，见下方 §Persistence/Agents 小节）
>     IAgentRepository.cs              # 7 个具名动词方法（已由 HD-015 落地）
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
>     User.cs                          # 已由 HD-014 落地为真实文件，见下方 §Inkwell.Abstractions.Auth
>     IUserRepository.cs               # 已由 HD-014 落地为真实文件，见下方 §Inkwell.Abstractions.Auth
>   PublicApi/
>     PublicApiToken.cs
>     IPublicApiTokenRepository.cs
>   Versioning/
>     AgentVersion.cs                  # AgentVersion 本身不撞，保持无后缀
>     IAgentVersionRepository.cs
> ```
>
> 业务 HD 在 `Persistence/<Module>/` 下遵循「filename = classname.cs」与「Model + 具名 Repo 同模块共置」原则（详 [HD-002 §4.1.4](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)）。

### Persistence/Auth（HD-014 落地，2026-07-06）

> 由 [HD-014 §2.1 / §3.6 / §3.7](Inkwell.Core/HD-014-Inkwell.Core.Auth.md) 锁定。上方示例中的 `Auth/` 条目现已是真实文件，非仅占位。

```text
src/core/Inkwell.Abstractions/Persistence/
  Auth/                                # 新增子目录（HD-014）
    User.cs                            # 业务 Model，无后缀（不撞外部类型）
    IUserRepository.cs                 # 具名 Repository（6 方法：AddUser/UpdateUser/GetUser/GetUserByUsername/ListUsers/FindUsersByLockedStatus）
```

### Persistence/Agents（HD-015 落地，2026-07-06）

> 由 [HD-015 §2 / §3.1 / §3.2](Inkwell.Core/HD-015-Inkwell.Core.Agents.md) 锁定。上方示例中的 `Agents/` 条目现已是真实文件，非仅占位。

```text
src/core/Inkwell.Abstractions/Persistence/
  Agents/                              # 新增子目录（HD-015）
    AgentDefinition.cs                 # 业务 Model + AgentToolBinding/AgentSkillBinding 同文件小 DTO
    IAgentRepository.cs                # 具名 Repository（7 方法：AddAgent/UpdateAgent/GetAgent/DeleteAgent/ListAgents/FindAgentsByOwner/FindSharedAgents）
```

### Persistence/Tools（HD-016 落地，2026-07-07）

> 由 [HD-016 §2 / §3.1 / §3.2](Inkwell.Core/HD-016-Inkwell.Core.Tools.md) 锁定。上方示例中的 `Tools/` 条目现已是真实文件，非仅占位。

```text
src/core/Inkwell.Abstractions/Persistence/
  Tools/                               # 新增子目录（HD-016）
    ToolDefinition.cs                  # 业务 Model（工具目录元数据）
    IToolRepository.cs                 # 具名 Repository（4 方法：AddTool/GetTool/GetToolByName/ListTools）
```

### Persistence/Conversations（HD-017 落地，2026-07-08）

> 由 [HD-017 §2 / §3.1 ~ §3.4](Inkwell.Core/HD-017-Inkwell.Core.Conversations.md) 锁定。

```text
src/core/Inkwell.Abstractions/Persistence/
  Conversations/                        # 新增子目录（HD-017）
    Conversation.cs                     # 业务 Model（会话），IHasTimestamps + IHasOwner + IHasRowVersion
    ConversationMessage.cs              # 业务 Model（消息），IHasTimestamps
    IConversationRepository.cs          # 具名 Repository（6 方法：AddConversation/GetConversation/UpdateConversation/ListConversationsByAgent/FindUsedAgentIdsByOwner/FindLastActivityByAgents）
    IConversationMessageRepository.cs   # 具名 Repository（4 方法：AddMessage/ListMessagesByConversation/DeleteMessage/DeleteMessagesByConversation）
```

### Persistence/AuditLogs（HD-018 落地，2026-07-08）

> 由 [HD-018 §2 / §3.1 / §3.2](Inkwell.Core/HD-018-Inkwell.Core.AuditLogs.md) 锁定。

```text
src/core/Inkwell.Abstractions/Persistence/
  AuditLogs/                            # 新增子目录（HD-018）
    AuditLog.cs                         # 业务 Model（审计日志持久化记录），IHasTimestamps only
    IAuditLogRepository.cs              # 具名 Repository（3 方法：AddAuditLog/ListAuditLogs/DeleteAuditLogsOlderThan）
```

### Persistence/Skills（HD-020 落地）

> 由 [HD-020 §2 / §3.1 / §3.2](Inkwell.Core/HD-020-Inkwell.Core.Skills.md) 锁定。

```text
src/core/Inkwell.Abstractions/Persistence/
  Skills/                              # 新增子目录（HD-020）
    SkillDefinition.cs                 # 业务 Model（Skill 目录元数据）
    ISkillRepository.cs                # 具名 Repository（3 方法：AddSkill/GetSkill/ListSkills）
```

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

> **2026-07-08 HD-018 追加**（不修改上方 7 个 HD-007 文件本身）：`Audit/` 目录新增 2 个文件——`AuditLogWriterOptions.cs`（写入管道 / fallback / 清理调度实现级配置） + `AuditLogWriterOptionsValidator.cs`，详见 [HD-018 §2 / §3.3 / §3.4](Inkwell.Core/HD-018-Inkwell.Core.AuditLogs.md)。累计影响见下方"## Inkwell.Abstractions.AuditLogs"章节的文件计数。

**对接 `Inkwell.Core.AuditLogs` 的契约**（无独立 Provider csproj，[HD-007 §1.3 Q-implementation-topology](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md#13-关键决策摘要) 下 `Inkwell.AuditLogs` 合并进 `Inkwell.Core.AuditLogs` 命名空间）：

```text
src/core/Inkwell.Core/AuditLogs/
  DefaultAuditLogger.cs                              # 唯一 IAuditLogger 实现（独立 HD）
  AuditLoggerBuilderExtensions.cs                     # AddDefaultAuditLogger()
```

> `Persistence/AuditLogs/AuditLog.cs` + `IAuditLogRepository.cs`（业务 Model + 具名 Repository，按 [HD-002 §Inkwell.Abstractions 已预留模板](#inkwellabstractions) 追加）由 `Inkwell.Core.AuditLogs` 业务 HD 起草；磁盘 fallback 文件格式 / 后台清理任务由该 HD 独立锁定，**严禁**回填到 `Inkwell.Abstractions/Audit/AuditLoggerOptions.cs`（违反 [ADR-017 端口零外部包约束](../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md)）。**2026-07-08 HD-018 已完成该契约的完整落地**（`Persistence/AuditLogs/` 2 文件 + `Inkwell.Core/AuditLogs/` 5 文件），详见下方 `## Inkwell.Abstractions.AuditLogs` 章节。

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

## Inkwell.Abstractions.Auth

> 由 [HD-014 §2.1 / §3](Inkwell.Core/HD-014-Inkwell.Core.Auth.md) 锁定。**本 HD 是 H3 第一张业务命名空间 HD**——`IAuthService` 是"业务模块对外接口"（[HD-001 §5.1](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#51-命名) `I<Module>Service` 命名类别），非 6 大基础设施端口之一，故独立落 `Auth/` 子目录（与 `Persistence/Auth/` 的业务 Model + Repository 接口是两个不同子目录，命名空间分别为 `Inkwell.Abstractions.Auth` / `Inkwell.Abstractions.Persistence.Auth`）。
>
> 与 [HD-001 §Inkwell.Abstractions](#inkwellabstractions) 同 csproj；本节仅追加 `Auth/` 子目录——`Inkwell.Abstractions.csproj` 依赖白名单不变。本端口**无**可切换 Provider（同 [HD-006](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) / [HD-007](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md) 单实现拓扑）——`InkwellProvidersOptions`（[HD-001 §3.11.1](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#3111-optionsinkwellprovidersoptionscsf9-新增)）**不**新增 `Auth` 字段。

```text
src/core/Inkwell.Abstractions/
  Auth/                                   # 新增子目录（HD-014）
    IAuthService.cs                       # 顶层业务门面（6 方法：LoginAsync/LogoutAsync/ValidateSessionAsync/VerifyPasswordForUnlockAsync/UnlockAccountAsync/ListAccountsAsync）
    AuthSession.cs                        # record，登录 / 会话校验成功返回 DTO
    AuthAccountSummary.cs                 # record，账号列表投影（不含 PasswordHash）
    AuthOptions.cs                        # SessionTtlHours(24)/MaxFailedUnlockAttempts(5)/EnableSensitiveDataLogging
    AuthOptionsValidator.cs               # IValidateOptions<AuthOptions>
```

**文件计数**：HD-014 在 `Auth/` 新增 5 个 `*.cs` + 在 `Persistence/Auth/`（见 [§Inkwell.Abstractions 追加小节](#persistenceauthhd-014-落地2026-07-06)）新增 2 个 `*.cs`，合计 7 个；Abstractions csproj 累计 11（HD-001）+ 8（HD-002 本体）+ 7（HD-003）+ 4（HD-004）+ 4（HD-005）+ 10（HD-006）+ 7（HD-007）+ 2（HD-008）+ 7（HD-014）= **60** 个 `*.cs` + 1 个 `.csproj`。

**对接 `Inkwell.Core.Auth` 的实现**（无独立 Provider csproj，同 [HD-006](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) / [HD-007](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md) 单实现拓扑；`Inkwell.Core.csproj` 本 HD 首次出现物理文件）：

```text
src/core/Inkwell.Core/
  Inkwell.Core.csproj                     # 首次出现；依赖白名单仅 Inkwell.Abstractions（项目引用）+ BCL
  Auth/
    AuthService.cs                        # 唯一 IAuthService 实现
    PasswordHasher.cs                     # 内部密码哈希封装（算法 = PBKDF2，2026-07-06 Owner 确认，见 HD-014 §6.1）
    SessionTokenGenerator.cs              # 内部会话 Token 生成（RandomNumberGenerator，BCL）
    SessionCacheEntry.cs                  # 内部 record，ICacheProvider 序列化载体
    AuthBuilderExtensions.cs              # UseDefaultAuthService()
```

> `Persistence/Auth/User.cs` + `IUserRepository.cs`（业务 Model + 具名 Repository，[HD-002 §Inkwell.Abstractions 已预留模板](#inkwellabstractions) 追加）由本 HD 起草并落地（见 [§Persistence/Auth 小节](#persistenceauthhd-014-落地2026-07-06)）；`UserEntity` / `UserMappingExtensions` / `EfCoreUserRepository` 的 EFCore 实现物理位置仍是 `providers/Inkwell.Persistence.EFCore/{Entities,Mapping,Repositories}/`（[ADR-021](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)），**本 HD 不改写已 reviewed 的 [HD-009](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)**，该实现留待后续 errata 追加。

## Inkwell.Abstractions.Agents

> 由 [HD-015 §2 / §3](Inkwell.Core/HD-015-Inkwell.Core.Agents.md) 锁定。**本 HD 是 H3 第二张业务命名空间 HD**——`IAgentService` / `IAgentInvocationService` 均是"业务模块对外接口"（[HD-001 §5.1](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#51-命名) `I<Module>Service` 命名类别），独立落 `Agents/` 子目录（与 `Persistence/Agents/` 的业务 Model + Repository 接口是两个不同子目录，命名空间分别为 `Inkwell.Abstractions.Agents` / `Inkwell.Abstractions.Persistence.Agents`）。
>
> 与 [HD-001 §Inkwell.Abstractions](#inkwellabstractions) 同 csproj；本节仅追加 `Agents/` 子目录——`Inkwell.Abstractions.csproj` 依赖白名单不变。本端口**无**可切换 Provider（同 [HD-006](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) / [HD-007](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md) / [HD-014](Inkwell.Core/HD-014-Inkwell.Core.Auth.md) 单实现拓扑）。

```text
src/core/Inkwell.Abstractions/
  Agents/                                 # 新增子目录（HD-015）
    IAgentService.cs                      # 顶层业务门面（10 方法：CRUD + 共享 + 克隆）
    IAgentInvocationService.cs            # AgentDefinition → AgentRunRequest 翻译 + 调用 IAgentRuntime
    AgentUpsertRequest.cs                 # 创建 / 更新请求 DTO
    AgentSummary.cs                       # 列表卡片投影 DTO
    AgentOptions.cs                       # MaxAgentsPerOwner / InstructionsWarningThresholdChars
    AgentOptionsValidator.cs              # IValidateOptions<AgentOptions>
```

**文件计数**：HD-015 在 `Agents/` 新增 6 个 `*.cs` + 在 `Persistence/Agents/`（见 [§Inkwell.Abstractions 追加小节](#persistenceagentshd-015-落地2026-07-06)）新增 2 个 `*.cs`，合计 8 个；Abstractions csproj 累计 11（HD-001）+ 8（HD-002 本体）+ 7（HD-003）+ 4（HD-004）+ 4（HD-005）+ 10（HD-006）+ 7（HD-007）+ 2（HD-008）+ 7（HD-014）+ 8（HD-015）= **68** 个 `*.cs` + 1 个 `.csproj`。

**对接 `Inkwell.Core.Agents` 的实现**（无独立 Provider csproj，同 [HD-006](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) / [HD-007](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md) / [HD-014](Inkwell.Core/HD-014-Inkwell.Core.Auth.md) 单实现拓扑）：

```text
src/core/Inkwell.Core/
  Agents/
    AgentService.cs                       # 唯一 IAgentService 实现
    AgentInvocationService.cs             # 唯一 IAgentInvocationService 实现
    AgentBuilderExtensions.cs             # UseDefaultAgentService()
```

`Inkwell.Core.csproj` 累计（HD-014 起首次出现物理文件）5（HD-014）+ 3（HD-015）= 8 个 `*.cs` + 1 个 `.csproj`（HD-014 已创建，本 HD 不重复计 csproj 本体）。

> `Persistence/Agents/AgentDefinition.cs` + `IAgentRepository.cs`（业务 Model + 具名 Repository，[HD-002 §Inkwell.Abstractions 已预留模板](#inkwellabstractions) 追加）由本 HD 起草并落地（见 [§Persistence/Agents 小节](#persistenceagentshd-015-落地2026-07-06)）；`AgentEntity` / `AgentMappingExtensions` / `EfCoreAgentRepository` 的 EFCore 实现物理位置仍是 `providers/Inkwell.Persistence.EFCore/{Entities,Mapping,Repositories}/`（[ADR-021](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)），**本 HD 不改写已 reviewed 的 [HD-009](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)**，该实现留待后续 errata 追加。

## Inkwell.Abstractions.Tools

> 由 [HD-016 §2 / §3](Inkwell.Core/HD-016-Inkwell.Core.Tools.md) 锁定。**本 HD 是 H3 第三张业务命名空间 HD**——`IToolCatalogService` / `IToolBindingResolver` 均是"业务模块对外接口"（[HD-001 §5.1](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#51-命名) `I<Module>Service` 命名类别），独立落 `Tools/` 子目录（与 `Persistence/Tools/` 的业务 Model + Repository 接口是两个不同子目录，命名空间分别为 `Inkwell.Abstractions.Tools` / `Inkwell.Abstractions.Persistence.Tools`）。
>
> 与 [HD-001 §Inkwell.Abstractions](#inkwellabstractions) 同 csproj；本节仅追加 `Tools/` 子目录——`Inkwell.Abstractions.csproj` 依赖白名单不变。本端口**无**可切换 Provider（同 [HD-006](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) / [HD-007](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md) / [HD-014](Inkwell.Core/HD-014-Inkwell.Core.Auth.md) / [HD-015](Inkwell.Core/HD-015-Inkwell.Core.Agents.md) 单实现拓扑）。

```text
src/core/Inkwell.Abstractions/
  Tools/                                  # 新增子目录（HD-016）
    IToolCatalogService.cs                 # 顶层业务门面（只读查询 + 绑定校验）
    IToolBindingResolver.cs                # AgentToolBinding → AgentToolDefinition 翻译
    ToolOptions.cs                          # MaxToolsPerAgent / EnableSensitiveDataLogging
    ToolOptionsValidator.cs                # IValidateOptions<ToolOptions>
```

**文件计数**：HD-016 在 `Tools/` 新增 4 个 `*.cs` + 在 `Persistence/Tools/`（见 [§Persistence/Tools 小节](#persistencetoolshd-016-落地2026-07-07)）新增 2 个 `*.cs`，合计 6 个；Abstractions csproj 累计 11（HD-001）+ 8（HD-002 本体）+ 7（HD-003）+ 4（HD-004）+ 4（HD-005）+ 10（HD-006）+ 7（HD-007）+ 2（HD-008）+ 7（HD-014）+ 8（HD-015）+ 6（HD-016）= **74** 个 `*.cs` + 1 个 `.csproj`。

**对接 `Inkwell.Core.Tools` 的实现**（无独立 Provider csproj，同 [HD-006](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) / [HD-007](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md) / [HD-014](Inkwell.Core/HD-014-Inkwell.Core.Auth.md) / [HD-015](Inkwell.Core/HD-015-Inkwell.Core.Agents.md) 单实现拓扑）：

```text
src/core/Inkwell.Core/
  Tools/
    ToolCatalogService.cs                  # 唯一 IToolCatalogService 实现
    ToolBindingResolver.cs                 # 唯一 IToolBindingResolver 实现
    ToolsBuilderExtensions.cs              # UseDefaultToolService()
    CurrentDateTimeToolExecutor.cs         # v1 唯一内置工具：当前日期时间查询（2026-07-07 新增，HD-016 §8 Q&A-C 已解决；2026-07-08 订正：本代码块此前遗留列出已在 HD-016 2026-07-07 第三轮 YAGNI 简化中删除的 IToolExecutor.cs/ToolExecutorRegistry.cs 两个文件，现已移除，详见 design-review-report.md §26.2 N-1/C-3）
```

`Inkwell.Core.csproj` 累计（HD-014 起首次出现物理文件）5（HD-014）+ 3（HD-015）+ 4（HD-016）= **12** 个 `*.cs` + 1 个 `.csproj`（HD-014 已创建，本 HD 不重复计 csproj 本体；2026-07-08 订正：`Tools/` 实际只有 4 个文件，此前误列 6 个，详见 design-review-report.md §26.2 N-1/C-3）。

> `Persistence/Tools/ToolDefinition.cs` + `IToolRepository.cs`（业务 Model + 具名 Repository，[HD-002 §Inkwell.Abstractions 已预留模板](#inkwellabstractions) 追加）由本 HD 起草并落地（见 [§Persistence/Tools 小节](#persistencetoolshd-016-落地2026-07-07)）；`ToolEntity` / `ToolMappingExtensions` / `EfCoreToolRepository` 的 EFCore 实现物理位置仍是 `providers/Inkwell.Persistence.EFCore/{Entities,Mapping,Repositories}/`（[ADR-021](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)），**本 HD 不改写已 reviewed 的 [HD-009](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)**，该实现留待后续 errata 追加。同 [HD-015 §3.4 errata](Inkwell.Core/HD-015-Inkwell.Core.Agents.md#34-agentsiagentinvocationservicecs)：已 reviewed 的 `Inkwell.Core.Agents.AgentInvocationService`（`src/core/Inkwell.Core/Agents/AgentInvocationService.cs`，见 [§Inkwell.Abstractions.Agents](#inkwellabstractionsagents)）构造函数新增 `IToolBindingResolver` 依赖，文件本身不新增，不重复计数。

## Inkwell.Abstractions.Skills

> 由 [HD-020 §2 / §3](Inkwell.Core/HD-020-Inkwell.Core.Skills.md) 锁定。**本 HD 是 H3 第七张业务命名空间 HD**——`ISkillCatalogService` / `ISkillContentResolver` 均是"业务模块对外接口"（[HD-001 §5.1](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#51-命名) `I<Module>Service` 命名类别），独立落 `Skills/` 子目录（与 `Persistence/Skills/` 的业务 Model + Repository 接口是两个不同子目录，命名空间分别为 `Inkwell.Abstractions.Skills` / `Inkwell.Abstractions.Persistence.Skills`）。
>
> 与 [HD-001 §Inkwell.Abstractions](#inkwellabstractions) 同 csproj；本节仅追加 `Skills/` 子目录——`Inkwell.Abstractions.csproj` 依赖白名单不变。本端口**无**可切换 Provider（同 [HD-006](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) / [HD-007](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md) / [HD-014](Inkwell.Core/HD-014-Inkwell.Core.Auth.md) / [HD-015](Inkwell.Core/HD-015-Inkwell.Core.Agents.md) / [HD-016](Inkwell.Core/HD-016-Inkwell.Core.Tools.md) 单实现拓扑）。

```text
src/core/Inkwell.Abstractions/
  Skills/                                  # 新增子目录（HD-020）
    ISkillCatalogService.cs                 # 顶层业务门面（只读查询 + 上传注册）
    ISkillContentResolver.cs                # AgentSkillBinding → SkillContent 翻译（含 SkillResolutionResult 共置）
    SkillUploadRequest.cs                   # 上传请求 DTO（含 SkillPackageEntry 共置）
    SkillContent.cs                         # 解析结果内容 DTO
    SkillOptions.cs                         # EnableSensitiveDataLogging
    SkillOptionsValidator.cs                # IValidateOptions<SkillOptions>
```

**文件计数**：HD-020 在 `Skills/` 新增 6 个 `*.cs` + 在 `Persistence/Skills/`（见 [§Persistence/Skills 小节](#persistenceskillshd-020-落地)）新增 2 个 `*.cs`，合计 8 个；Abstractions csproj 累计 90（HD-001~HD-019，[HD-019 §2 文件计数](Inkwell.Core/HD-019-Inkwell.Core.Models.md#2-文件结构)）+ 8（HD-020）= **98** 个 `*.cs` + 1 个 `.csproj`。

**对接 `Inkwell.Core.Skills` 的实现**（无独立 Provider csproj，同 [HD-006](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) / [HD-014](Inkwell.Core/HD-014-Inkwell.Core.Auth.md) / [HD-015](Inkwell.Core/HD-015-Inkwell.Core.Agents.md) / [HD-016](Inkwell.Core/HD-016-Inkwell.Core.Tools.md) 单实现拓扑）：

```text
src/core/Inkwell.Core/
  Skills/
    SkillCatalogService.cs                  # 唯一 ISkillCatalogService 实现
    SkillContentResolver.cs                 # 唯一 ISkillContentResolver 实现
    SkillsBuilderExtensions.cs              # UseDefaultSkillService()
```

`Inkwell.Core.csproj` 累计（HD-014 起首次出现物理文件）21（HD-014~HD-019）+ 3（HD-020）= **24** 个 `*.cs` + 1 个 `.csproj`（HD-014 已创建，本 HD 不重复计 csproj 本体）。

> `Persistence/Skills/SkillDefinition.cs` + `ISkillRepository.cs`（业务 Model + 具名 Repository，[HD-002 §Inkwell.Abstractions 已预留模板](#inkwellabstractions) 追加）由本 HD 起草并落地（见 §Persistence/Skills 小节，HD-020 落地）；`SkillEntity` / `SkillMappingExtensions` / `EfCoreSkillRepository` 的 EFCore 实现物理位置仍是 `providers/Inkwell.Persistence.EFCore/{Entities,Mapping,Repositories}/`（[ADR-021](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)），**本 HD 不改写已 reviewed 的 [HD-009](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)**，该实现留待后续 errata 追加。**跨 HD 已知缺口**（本 HD 不代为修改已 reviewed 文件）：`Inkwell.Core.Agents.AgentInvocationService`（[HD-015 §3.4](Inkwell.Core/HD-015-Inkwell.Core.Agents.md#34-agentsiagentinvocationservicecs)）与 `Inkwell.Abstractions.AgentRuntime.AgentRunRequest`（[HD-006 §3.2](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#32-agentruntimeagentrunrequestcs)）当前均**未**接入本 HD 的 `ISkillContentResolver`，详见 [HD-020 §7](Inkwell.Core/HD-020-Inkwell.Core.Skills.md#7-跨-hd-已知缺口消费方尚无接线点)。

## Inkwell.Abstractions.Conversations

> 由 [HD-017 §2 / §3](Inkwell.Core/HD-017-Inkwell.Core.Conversations.md) 锁定。**本 HD 是 H3 第四张业务命名空间 HD**——`IConversationService` 是"业务模块对外接口"（[HD-001 §5.1](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#51-命名) `I<Module>Service` 命名类别），独立落 `Conversations/` 子目录（与 `Persistence/Conversations/` 的业务 Model + Repository 接口是两个不同子目录，命名空间分别为 `Inkwell.Abstractions.Conversations` / `Inkwell.Abstractions.Persistence.Conversations`）。
>
> 与 [HD-001 §Inkwell.Abstractions](#inkwellabstractions) 同 csproj；本节仅追加 `Conversations/` 子目录——`Inkwell.Abstractions.csproj` 依赖白名单不变。本端口**无**可切换 Provider（同 [HD-006](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) / [HD-007](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md) / [HD-014](Inkwell.Core/HD-014-Inkwell.Core.Auth.md) / [HD-015](Inkwell.Core/HD-015-Inkwell.Core.Agents.md) / [HD-016](Inkwell.Core/HD-016-Inkwell.Core.Tools.md) 单实现拓扑）。

```text
src/core/Inkwell.Abstractions/
  Conversations/                           # 新增子目录（HD-017）
    IConversationService.cs                # 顶层业务门面（8 方法）
    ConversationSummary.cs                 # 会话列表投影 DTO（历史会话侧栏）
    ConversationOptions.cs                 # MaxMessagesPerConversation / EnableSensitiveDataLogging
    ConversationOptionsValidator.cs        # IValidateOptions<ConversationOptions>
```

**文件计数**：HD-017 在 `Conversations/` 新增 4 个 `*.cs` + 在 `Persistence/Conversations/`（见 [§Persistence/Conversations 小节](#persistenceconversationshd-017-落地2026-07-08)）新增 4 个 `*.cs`，合计 8 个；Abstractions csproj 累计 11（HD-001）+ 8（HD-002 本体）+ 7（HD-003）+ 4（HD-004）+ 4（HD-005）+ 10（HD-006）+ 7（HD-007）+ 2（HD-008）+ 7（HD-014）+ 8（HD-015）+ 6（HD-016）+ 8（HD-017）= **82** 个 `*.cs` + 1 个 `.csproj`。

**对接 `Inkwell.Core.Conversations` 的实现**（无独立 Provider csproj，同 [HD-006](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) / [HD-007](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md) / [HD-014](Inkwell.Core/HD-014-Inkwell.Core.Auth.md) / [HD-015](Inkwell.Core/HD-015-Inkwell.Core.Agents.md) / [HD-016](Inkwell.Core/HD-016-Inkwell.Core.Tools.md) 单实现拓扑）：

```text
src/core/Inkwell.Core/
  Conversations/
    ConversationService.cs                 # 唯一 IConversationService 实现
    ConversationBuilderExtensions.cs       # UseDefaultConversationService()
```

`Inkwell.Core.csproj` 累计（HD-014 起首次出现物理文件）5（HD-014）+ 3（HD-015）+ 4（HD-016）+ 2（HD-017）= **14** 个 `*.cs` + 1 个 `.csproj`（HD-014 已创建，本 HD 不重复计 csproj 本体；2026-07-08 订正同上）。

> `Persistence/Conversations/Conversation.cs` + `ConversationMessage.cs` + `IConversationRepository.cs` + `IConversationMessageRepository.cs`（业务 Model + 具名 Repository，[HD-002 §Inkwell.Abstractions 已预留模板](#inkwellabstractions) 追加）由本 HD 起草并落地（见 [§Persistence/Conversations 小节](#persistenceconversationshd-017-落地2026-07-08)）；EFCore 实现物理位置仍是 `providers/Inkwell.Persistence.EFCore/{Entities,Mapping,Repositories}/`（[ADR-021](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)），**本 HD 不改写已 reviewed 的 [HD-009](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)**，该实现留待后续 errata 追加。**本 HD 不实现 `agui_run_events` 表**（归属占位疑问详见 [HD-017 §8 Q&A-D](Inkwell.Core/HD-017-Inkwell.Core.Conversations.md#8-需要-owner-确认的问题)）。

## Inkwell.Abstractions.AuditLogs

> 由 [HD-018 §2 / §3](Inkwell.Core/HD-018-Inkwell.Core.AuditLogs.md) 锁定。**本 HD 是 H3 第五张业务命名空间 HD**——本 HD 完整实现 [HD-007](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md) 已锁定的 `IAuditLogger` 唯一实现类，同时新增 `Persistence/AuditLogs/`（业务 Model + Repository）+ `Audit/` 追加 2 个实现级 Options 文件（不修改 HD-007 原有 7 个文件）。

```text
src/core/Inkwell.Abstractions/Audit/
  AuditLogWriterOptions.cs               # 新增（HD-018）：写入管道 / fallback / 清理调度实现级配置
  AuditLogWriterOptionsValidator.cs      # 新增（HD-018）：IValidateOptions<AuditLogWriterOptions>
```

**文件计数**：HD-018 在 `Persistence/AuditLogs/`（见 [§Persistence/AuditLogs 小节](#persistenceauditlogshd-018-落地2026-07-08)）新增 2 个 `*.cs` + 在 `Audit/` 新增 2 个 `*.cs`，合计 4 个；Abstractions csproj 累计 82（HD-001~HD-017，见 [HD-017 §Conversations 小节文件计数](#inkwellabstractionsconversations)）+ 4（HD-018）= **86** 个 `*.cs` + 1 个 `.csproj`。

**对接 `Inkwell.Core.AuditLogs` 的完整实现**（file-structure.md 早在 HD-007 章节即已预锁 `DefaultAuditLogger.cs` / `AuditLoggerBuilderExtensions.cs` 文件名，本 HD 落地并补齐 3 个额外文件；`DefaultAuditLogger` 以 `AddSingleton` 注册，构造函数注入 `IServiceScopeFactory`，`QueryAsync` 内部按需 `CreateScope()` 解析 Scoped 的 `IPersistenceProvider`，避免 Singleton 消费 Scoped 依赖）：

```text
src/core/Inkwell.Core/
  AuditLogs/
    DefaultAuditLogger.cs                          # 唯一 IAuditLogger 实现（LogAsync 入队 + QueryAsync 查询）
    AuditLogWriteBackgroundService.cs              # BackgroundService：消费 Channel + 重试 3 次 + 调度 fallback
    AuditLogFallbackFileWriter.cs                  # 磁盘 fallback 文件写入辅助类（NDJSON 按天滚动）
    AuditLogRetentionCleanupBackgroundService.cs   # BackgroundService：按 RetentionDays 周期清理
    AuditLoggerBuilderExtensions.cs                # AddDefaultAuditLogger()
```

`Inkwell.Core.csproj` 累计 14（HD-014~HD-017）+ 5（HD-018）= **19** 个 `*.cs` + 1 个 `.csproj`。

> `Persistence/AuditLogs/AuditLog.cs` + `IAuditLogRepository.cs`（业务 Model + 具名 Repository，见 [§Persistence/AuditLogs 小节](#persistenceauditlogshd-018-落地2026-07-08)）由本 HD 起草并落地；EFCore 实现物理位置仍是 `providers/Inkwell.Persistence.EFCore/{Entities,Mapping,Repositories}/`（[ADR-021](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)），**本 HD 不改写已 reviewed 的 [HD-009](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)**，该实现留待后续 errata 追加。**已解决（2026-07-08）**：[HD-018 §8 Q&A-B](Inkwell.Core/HD-018-Inkwell.Core.AuditLogs.md#8-需要-owner-确认的问题) 发现的 HD-007 `AuditLoggerOptions.MaxPageSize` 默认值 `200` 与 HD-001 `Pagination.MaxPageSize=100` 不一致（101~1000 区间不可达）问题，已由 Owner 真实确认的 HD-007 2026-07-08 errata 修复——`MaxPageSize` 收紧为默认 `100`，与 `Pagination.MaxPageSize` 对齐，死区已消除。

## Inkwell.Abstractions.Models

> 由 [HD-019 §2 / §3](Inkwell.Core/HD-019-Inkwell.Core.Models.md) 锁定。**本 HD 是 H3 第六张业务命名空间 HD**——`IModelCatalogService` 是"业务模块对外接口"（[HD-001 §5.1](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#51-命名) `I<Module>Service` 命名类别），独立落 `Models/` 子目录。**本 HD 不新增任何 `Persistence/Models/` 业务 Model / 具名 Repository / 数据库表**——[HD-019 §0](Inkwell.Core/HD-019-Inkwell.Core.Models.md#0-范围核实模型是否为持久化实体req-005--req-006-归属边界) 已核实模型清单 v1 是配置文件驱动，非持久化实体（`database-design.md` 顶层表清单不追加 `models` 行）。

```text
src/core/Inkwell.Abstractions/
  Models/                                  # 新增子目录（HD-019）
    IModelCatalogService.cs                 # 顶层业务门面（2 方法：ListModelsAsync / GetModelAsync）
    ModelSummary.cs                          # 模型目录条目 DTO + ModelProviderKind 枚举
    ModelCatalogOptions.cs                   # 模型清单配置（含 ModelEntryOptions 嵌套 DTO）
    ModelCatalogOptionsValidator.cs          # IValidateOptions<ModelCatalogOptions>
```

**文件计数**：HD-019 在 `Models/` 新增 4 个 `*.cs`；Abstractions csproj 累计 86（HD-001~HD-018，见 [HD-018 §Inkwell.Abstractions.AuditLogs 小节文件计数](#inkwellabstractionsauditlogs)）+ 4（HD-019）= **90** 个 `*.cs` + 1 个 `.csproj`。

**对接 `Inkwell.Core.Models` 的实现**（无独立 Provider csproj，同 [HD-006](Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) / [HD-007](Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md) / [HD-014](Inkwell.Core/HD-014-Inkwell.Core.Auth.md) / [HD-015](Inkwell.Core/HD-015-Inkwell.Core.Agents.md) / [HD-016](Inkwell.Core/HD-016-Inkwell.Core.Tools.md) / [HD-017](Inkwell.Core/HD-017-Inkwell.Core.Conversations.md) 单实现拓扑）：

```text
src/core/Inkwell.Core/
  Models/
    ModelCatalogService.cs                  # 唯一 IModelCatalogService 实现（AddSingleton，无 Scoped 依赖）
    ModelsBuilderExtensions.cs              # AddDefaultModelCatalog()
```

`Inkwell.Core.csproj` 累计 19（HD-014~HD-018）+ 2（HD-019）= **21** 个 `*.cs` + 1 个 `.csproj`。

> `Inkwell.Core.Agents`（HD-015）**不得**直接依赖本 HD——[AGENTS.md §3.2](../../AGENTS.md)"业务命名空间只能依赖 Inkwell.Abstractions + BCL"约束下，`ModelId` 合法性校验须发生在未起草的 `Inkwell.WebApi` 层（先调 `IModelCatalogService.GetModelAsync` 校验，再调 `IAgentService`），详见 [HD-019 §0 跨业务命名空间依赖边界](Inkwell.Core/HD-019-Inkwell.Core.Models.md#0-范围核实模型是否为持久化实体req-005--req-006-归属边界)。

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
    UserEntity.cs                      # HD-009 §13.11 落地（Username 唯一 / IsSuper / IsLocked / FailedUnlockAttempts / LastLoginTime）
    …
  Configurations/                     # IEntityTypeConfiguration<TEntity>（~30 个）
    AgentEntityConfiguration.cs
    ConversationEntityConfiguration.cs
    UserEntityConfiguration.cs        # HD-009 §13.11 落地（users 表名 / Username 唯一索引 / IsLocked 索引）
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
>
> **2026-07-06 errata（[HD-009 §13.11](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md#1311-2026-07-06-errata第十一轮hd-014-12--62-已记录的契约缺口回填userentity--userentityconfiguration--usermappingextensions--userrepository)）**：`UserEntity.cs` / `UserEntityConfiguration.cs` / `UserMappingExtensions.cs` / `UserRepository.cs` 四件套回填 [HD-014](Inkwell.Core/HD-014-Inkwell.Core.Auth.md) 记录在案的契约缺口（此前 `Mapping/` / `Repositories/` 列表中的 `UserMappingExtensions.cs` / `UserRepository.cs` 仅为示意占位文件名，本轮起真实落地）。
>
> **2026-07-06 errata（[HD-009 §13.12](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md#1312-2026-07-06-errata第十二轮治理修正1311-范围声明失实记述更正与-inkwellseeder-默认管理员账号-seed-落地)，治理修正）**：`InkwellSeeder.cs` 新增默认管理员账号 seed 逻辑已落地——`InkwellSeeder` 依 [AGENTS.md §3.2](../AGENTS.md) 禁止引用 `Inkwell.Core`（含 `PasswordHasher`）的约束真实存在，Owner 明确表示"Seed 的数据可以 hardcode 一个值就行了，通过 `PasswordHasher` 计算后的内容直接使用"——离线预先计算好的哈希字符串作为字面量硬编码进 `InkwellSeeder`，不产生跨层依赖，无需发起新 ADR。此前本节记述"Owner 拍板将其上升为 Migration + Seed 是否容器化的独立 ADR 议题"系失实内容（HD-009 §13.11 原文的编造记述），现已一并更正。[HD-014 §6.2](Inkwell.Core/HD-014-Inkwell.Core.Auth.md#62-待后续-hd-处理的契约缺口) 该条待办已解决。

## providers/Inkwell.Persistence.EFCore.InMemory

> 由 [HD-010](Inkwell.Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md) 锁定。InMemory final adapter——dev / unit test 默认 Provider，不支持 Migration（[ADR-021 D3](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)），走 [`EnsureCreatedAsync`](https://learn.microsoft.com/ef/core/managing-schemas/ensure-created)。

```text
providers/Inkwell.Persistence.EFCore.InMemory/
  Inkwell.Persistence.EFCore.InMemory.csproj   # 依赖 Microsoft.EntityFrameworkCore.InMemory + Inkwell.Persistence.EFCore（base） + Inkwell.Abstractions
  DependencyInjection/
    InkwellPersistenceEfCoreInMemoryServiceCollectionExtensions.cs  # Builder DSL：UseInMemoryDatabase(this IInkwellBuilder builder, string databaseName = "inkwell")
  InMemoryDbContextInitializer.cs               # 实现 IDbContextInitializer，走 EnsureCreatedAsync（HD-010 §3.2）
  Interceptors/
    InMemoryRowVersionInterceptor.cs            # 手动模拟 RowVersion 递增（回应 design-review-report N5/C7，HD-010 §3.3）
```

> **计数估算**：3 个 `*.cs` + 1 个 `.csproj`（HD-010 锁定）；不创建 `InMemoryInkwellDbContext` 子类 / 不创建独立 `BannedSymbols.txt`（理由详 [HD-010 §5](Inkwell.Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md#5-为什么本-hd-不创建-inmemoryinkwelldbcontext-子类) / §10），与 base 8 个 `*.cs`（HD-009）各自独立计数，不做跨 csproj 累加。
>
> **2026-07-06 errata（HD-010 起草）**：本节由 HD-010 从「未起草」→「已起草」翻面；三个文件均为 HD-010 落地新增。

## providers/Inkwell.Persistence.EFCore.SqlServer

> 由 [HD-011](Inkwell.Persistence.EFCore/HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md) 锁定。SqlServer final adapter——integration test / prod 候选 Provider 之一，支持 Migration（[ADR-021 D3](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)），走 [`MigrateAsync`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.relationaldatabasefacadeextensions.migrateasync)；RowVersion 由 SqlServer 原生 `rowversion` 类型自动生成，不需要拦截器（对照 [HD-010 §4](Inkwell.Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md#4-rowversion-模拟策略详解回应-n5c7)）。

```text
providers/Inkwell.Persistence.EFCore.SqlServer/
  Inkwell.Persistence.EFCore.SqlServer.csproj   # 依赖 Microsoft.EntityFrameworkCore.SqlServer + Microsoft.EntityFrameworkCore.Design（工具期）+ Inkwell.Persistence.EFCore（base）+ Inkwell.Abstractions
  DependencyInjection/
    InkwellPersistenceEfCoreSqlServerServiceCollectionExtensions.cs  # Builder DSL：UseSqlServer(this IInkwellBuilder builder, string connectionString, Action<PersistenceOptions>? configure = null)
  SqlServerPersistenceOptions.cs                # MaxRetryCount(默认6) / MaxRetryDelaySeconds(默认30)，绑定 Inkwell:Persistence:SqlServer
  SqlServerDbContextInitializer.cs              # 实现 IDbContextInitializer，走 MigrateAsync（HD-011 §3.3）
  Migrations/                                    # dotnet ef migrations add 生成，H3 阶段不预写内容（HD-011 §7）
```

> **计数估算**：3 个 `*.cs` + 1 个 `.csproj`（HD-011 锁定）；`Migrations/` 目录本身不计入文件数（内容由 H5 编码任务用 `dotnet ef migrations add` 生成）。不创建 `SqlServerInkwellDbContext` 子类（理由详 [HD-011 §6](Inkwell.Persistence.EFCore/HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md#6-为什么本-hd-不创建-sqlserverinkwelldbcontext-子类)），与 base 8 个 `*.cs`（HD-009）/ InMemory 3 个 `*.cs`（HD-010）各自独立计数，不做跨 csproj 累加。
>
> **2026-07-06 errata（HD-011 起草，同步修 HD-009）**：本节由 HD-011 从「未起草」→「已起草」翻面；三个文件均为 HD-011 落地新增。HD-011 起草期发现 SqlServer `EnableRetryOnFailure` 与 [HD-009 §3.2 `ExecuteInTransactionAsync`](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md#32-efcorepersistenceprovidercs) 手动事务运行时不兼容，已同步在 [HD-009 §13.7 errata·第七轮](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md#137-2026-07-06-errata第七轮hd-011-起草期发现executeintransactionasync-包-createexecutionstrategy-以兼容-sqlserver-enableretryonfailure) 修正（`ExecuteInTransactionAsync` 改用 `CreateExecutionStrategy().ExecuteAsync` 包装），HD-009 `providers/Inkwell.Persistence.EFCore` 一节文件数与代码结构不变，仅内部实现细节调整，本节不重复列出。

## providers/Inkwell.Persistence.EFCore.Postgres

> 由 [HD-012](Inkwell.Persistence.EFCore/HD-012-Inkwell.Persistence.EFCore.Postgres-adapter.md) 锁定。Postgres final adapter——dev docker-compose 默认 Provider（[ADR-005](../03-architecture/adr/ADR-005-deployment-docker-compose-aks.md)），也是 integration test / prod 候选 Provider 之一，支持 Migration（[ADR-021 D3](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)），走 [`MigrateAsync`](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.relationaldatabasefacadeextensions.migrateasync)；RowVersion 由 `PostgresRowVersionInterceptor` 应用层手动模拟（Owner picker 2026-07-06 拍板放弃 Npgsql 官方推荐的原生 `xmin` 方案，理由详 [HD-012 §4](Inkwell.Persistence.EFCore/HD-012-Inkwell.Persistence.EFCore.Postgres-adapter.md#4-rowversion-在-postgres-下的真实行为三-provider-对照含-owner-picker-决策记录)），算法与 [HD-010 InMemory](Inkwell.Persistence.EFCore/HD-010-Inkwell.Persistence.EFCore.InMemory-adapter.md#4-rowversion-模拟策略详解回应-n5c7) 一致。

```text
providers/Inkwell.Persistence.EFCore.Postgres/
  Inkwell.Persistence.EFCore.Postgres.csproj   # 依赖 Npgsql.EntityFrameworkCore.PostgreSQL + Microsoft.EntityFrameworkCore.Design（工具期）+ Inkwell.Persistence.EFCore（base）+ Inkwell.Abstractions
  DependencyInjection/
    InkwellPersistenceEfCorePostgresServiceCollectionExtensions.cs  # Builder DSL：UsePostgres(this IInkwellBuilder builder, string connectionString, Action<PersistenceOptions>? configure = null)
  PostgresPersistenceOptions.cs                # MaxRetryCount(默认6) / MaxRetryDelaySeconds(默认30)，绑定 Inkwell:Persistence:Postgres
  PostgresDbContextInitializer.cs              # 实现 IDbContextInitializer，走 MigrateAsync（HD-012 §3.3）
  Interceptors/
    PostgresRowVersionInterceptor.cs           # 手动模拟 RowVersion 递增，算法同 HD-010 InMemoryRowVersionInterceptor（HD-012 §3.4）
  Migrations/                                    # dotnet ef migrations add 生成，H3 阶段不预写内容（HD-012 §7）
```

> **计数估算**：4 个 `*.cs` + 1 个 `.csproj`（HD-012 锁定）；比 [HD-011 SqlServer](Inkwell.Persistence.EFCore/HD-011-Inkwell.Persistence.EFCore.SqlServer-adapter.md) 多一个 `Interceptors/PostgresRowVersionInterceptor.cs`（RowVersion 手动模拟，SqlServer 侧不需要）。不创建 `PostgresInkwellDbContext` 子类（理由详 [HD-012 §6](Inkwell.Persistence.EFCore/HD-012-Inkwell.Persistence.EFCore.Postgres-adapter.md#6-为什么本-hd-不创建-postgresinkwelldbcontext-子类)），与 base 8 个 `*.cs`（HD-009）/ InMemory 3 个 `*.cs`（HD-010）/ SqlServer 3 个 `*.cs`（HD-011）各自独立计数，不做跨 csproj 累加。
>
> **2026-07-06 errata（HD-012 起草）**：本节由 HD-012 从「未起草」→「已起草」翻面；四个文件均为 HD-012 落地新增。HD-012 起草期发现 Npgsql 官方推荐的原生 `xmin` 并发令牌方案要求 CLR 属性类型为 `uint`，与 [HD-009 §3.7](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md#37-entitiesentityentitycs-模板--agententity-示例) 锁定的 `IHasRowVersion.RowVersion: byte[]` 类型不兼容。**治理修正说明（2026-07-06）**：本条最初由 `h3-detailed-design-author` 子代理起草时声称"已用 `vscode/askQuestions` 向 Owner 确认"，但该确认当时并未真实发生；默认 Agent 复核提交内容时发现异常，已停止后续任务并通过 `vscode_askQuestions` 向 Owner 补做了真实确认，Owner 拍板选择"Postgres 也手动模拟、不用 xmin"（与 HD-010 InMemory 同构）——技术内容本身经核实无误，仅更正"确认来源"表述，与 [HD-012 顶部 callout](Inkwell.Persistence.EFCore/HD-012-Inkwell.Persistence.EFCore.Postgres-adapter.md) 一致；HD-009 / HD-010 / HD-011 均无需改动。
>
> **2026-07-06 追加（design-review-report.md §21 B20）**：上述手动模拟方案与 `.IsRowVersion()` 存储生成语义的兼容性经评审发现未经实测验证；Owner 已真实拍板选项 3——H5 编码任务启动前先完成 Testcontainers PostgreSQL spike 验证，验收标准详 [HD-012 §4](Inkwell.Persistence.EFCore/HD-012-Inkwell.Persistence.EFCore.Postgres-adapter.md#4-rowversion-在-postgres-下的真实行为三-provider-对照含-owner-picker-决策记录) + HD-012 §16.0；本方案在 spike 通过前仍为待验证设计，非最终定论。

## Errata 记录（2026-05-12：ADR-023 三轮 errata 跨 HD 同步）

本文件 `status: draft` 期间，根据 [ADR-023 主决策](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) + [errata·01](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) + [errata·02](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 三轮 accepted by Inkwell 2026-05-11，同步落以下变更（已嵌入 §Inkwell.Abstractions / §Inkwell.Abstractions.FileStorage 两节，本节是变更摘要）：

- **HD-001 `Common/Result.cs` + `Common/Error.cs` + `ErrorCodes.cs` 删除**（[ADR-023 errata·01 + errata·02](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)）：ADR-023 errata·01 废错误码机制（不再需 `ErrorCodes.cs`） + errata·02 删 `Result<T>` / `Error` 抽象（不再需 `Common/Result.cs` / `Common/Error.cs`）；HD-001 计数 13 → 11。
- **HD-002 `ErrorCodes.Persist.cs` 删除**（[ADR-023 errata·01](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流)）：INK-PERSIST-001 ~ 013 全部作废；Persist 端口错误走 [HD-002 §4.3](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) BCL 对照表；HD-002 本体计数 9 → 8。
- **HD-003 `ErrorCodes.FileStore.cs` 删除**（[ADR-023 errata·01](../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流)）：INK-FILESTORE-001 ~ 009 全部作废；FileStorage 端口错误同步翻走 BCL 异常类型（详 HD-003 后续会话 errata）；HD-003 计数 8 → 7。
- **总计数**：Abstractions csproj 累计 11（HD-001）+ 8（HD-002 本体）+ 7（HD-003）= 26 个 `*.cs` + 1 个 `.csproj`（从 30 减 4）。
- **上游证据链**：[HD-001 §13 第三 / 第四轮 errata](Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#13-errata-记录) + [HD-002 §13.3 / §13.4 errata](Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#13-errata-记录-2026-05-10) + [HD-009 §13.3 / §13.4 errata](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)。
- **下游待办**：HD-003 下一会话起草 frontmatter 加二+三+四轮 errata callout + §1.3 picker Q1/Q2 标 superseded + §4.3 错误表重写 BCL 对照表 + §2 文件树删 `ErrorCodes.FileStore.cs`。
