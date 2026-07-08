---
id: HD-003
title: Inkwell.Abstractions 详细设计 — FileStorage Port（IFileStorageProvider facade + 4 DTO + Options）
stage: H3
status: reviewed
reviewers: [Inkwell]
upstream:
  - REQ-003
  - REQ-009
  - REQ-016
  - REQ-017
  - NFR-006
  - ADR-002
  - ADR-005
  - ADR-009
  - ADR-015
  - ADR-017
  - HD-001
  - ADR-023
---

> **错误处理约定**（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) accepted by Inkwell 2026-05-11，含 [errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) 废错误码、[errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 删 `Result<T>` / `Error` 抽象）：端口层与业务层统一采用裸 `Task<T>` + .NET BCL 异常。Inkwell **不自建 `Result<T>` / `Error` 抽象** / 不自建错误码机制 / 不自建端口层异常基类；仅保留 `InkwellConfigurationException` / `InkwellBuilderException` 两个程序错误子类用于 DI 装配期校验。具体错误语义走 BCL 异常类型表达 + OTel [`exception.*` 五字段](https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/)，详 [HD-001 §5.3 BCL 对照表](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)。
>
> **2026-05-11 errata·第二+三+四轮**（[ADR-023 三轮翻新](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) accepted by Inkwell）：本 HD 同步翻签名 / 删错误码库 / 删 `Result<T>` 引用 / 重写 BCL 对照表。受影响章节：§1.1 / §1.3 Q1 / Q2 / Q6 / §1.4 / §2 / §3.1 / §3.4 / §3.6 / §3.8 整段废 / §4 整段重写 / §5.2 / §5.3 / §6 / §7.2 / §7.3 / §10 F3。详 §13 errata。§3 文件数从 8 个减为 7 个（§3.8 作为"已废"锁位保留不重用，§3.1 ~ §3.7 编号不调整以保追溯不断）；picker Q1=B 混合签名 / Q2 `Result<FileDownloadResponse>` / Q6 9 错误码三项 picker 决策同步 superseded。
>
> **2026-05-11 errata·第一轮**（[design-review-report §7](../design-review-report.md#7-hd-003-filestorage-port-增量评审2026-05-11)）：Owner 2026-05-11 picker 拍板五项 errata：(1) **B3=A** 测试包路径统一走 `tests/core/Inkwell.Providers.Contract/FileStorage/`（§3.1 / §8.3 / §10 同步）；(2) **B4=B** HD-003 保留 `metadata` / `keyPrefix` 参数名，[ADR-015](../../03-architecture/adr/ADR-015-object-storage-provider-switchable.md) 同步追加 errata 块声明接口草图由 H3 精化；(3) **N7** §1.4 偏离表补 ADR-015 接口形态 2 行；(4) **N8** §10 F1 / F5 `rg` 命令改多 `-e` flag避免 shell escape 失效；(5) **N9** [file-structure.md "端口接口文件"建议段](../file-structure.md) 添 errata 行指向 §Inkwell.Abstractions.FileStorage 完整章节。本轮 errata 后 HD-003 仍 `status: draft`，Owner 另行翻 `reviewed` + 填 `reviewers`。
>
> **范围切片**：本 HD 覆盖 `Inkwell.Abstractions/FileStorage/` 子层——`IFileStorageProvider` facade、4 个 DTO（`FileMetadata` / `FileUploadResult` / `FileDownloadResponse` / `FileObjectInfo`）、`FileStorageOptions` + Validator、`ErrorCodes.FileStore` 段。**不**定义容器名常量集（[picker Q3=B](#13-关键决策摘要)：业务命名空间各自传字符串，留到业务 HD 起草）、**不**实现 Provider 行为（三 Provider 实现在 `providers/Inkwell.FileStorage.MinIO/` / `Inkwell.FileStorage.AzureBlob/` / `Inkwell.Core` 的 `LocalFileSystemFileStorageProvider` 各自独立 HD 起草）。
>
> **跨 HD 关联**：本 HD 与 [HD-001 foundation](HD-001-Inkwell.Abstractions-foundation.md)（Result / Builder / OTel 字段）+ [HD-002 persistence port](HD-002-Inkwell.Abstractions-persistence-port.md)（同级端口模板）+ [HD-009 EFCore base](../Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)（兄弟 base，命名风格对齐）形成 Abstractions 三连环。

## 1. 模块概述

### 1.1 职责

- `IFileStorageProvider` facade（§3.1）：定义业务命名空间访问对象存储的统一入口；上传 / 下载 / 删除 / 探测 / 列表 / 预签名 URL 共 7 个方法
- 4 个 DTO（§3.2 ~ §3.5）：`FileMetadata`（上传时可选）/ `FileUploadResult`（上传回执）/ `FileDownloadResponse`（下载包装含元数据）/ `FileObjectInfo`（列表元素）
- `FileStorageOptions` + Validator（§3.6 ~ §3.7）：详细配置——预签名 TTL 默认 / 上限 / 下限、最大对象大小、列表页大小、SensitiveDataLogging
- ~~`ErrorCodes.FileStore`（§3.8）：本端口的错误码常量集，9 条 `INK-FILESTORE-001` ~ `INK-FILESTORE-009`~~（[ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) 废错误码后整段废止，§3.8 标"已废锁位保留"；具体错误语义改走 BCL 异常类型 + OTel `exception.*` 五字段，详 §4）

### 1.2 范围

- **在内**：facade 接口 + DTO + Options + 错误码常量
- **不在内**：
  - 三 Provider 实现（[ADR-015](../../03-architecture/adr/ADR-015-object-storage-provider-switchable.md) LocalFileSystem / AzureBlob / MinIO 各自独立 HD）
  - 客户端直传逻辑（[ADR-009 多模态](../../03-architecture/adr/ADR-009-multimodal-azure-speech.md)）
  - 容器名常量集 / 业务侧 PII 脱敏规则（前者按 [picker Q3=B](#13-关键决策摘要) 留业务 HD；后者留业务方自行定义）
  - Helm bucket 初始化 / Migration（由 `providers/Inkwell.FileStorage.MinIO/` HD + Helm chart 起草）
  - 内容类型探测 / 病毒扫描 / OCR 抽取（业务侧 `Inkwell.KnowledgeBase` HD）

### 1.3 关键决策摘要

> 全部由 2026-05-11 picker 拍板，决策证据见本节"出处"列。

| Q   | 决议                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      | 出处                                                                 |
| --- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------- |
| Q1  | 签名风格 superseded → 见 [§1.3.1](#131-q1--q2--q6-superseded-by-adr-023)                                                                                                                                                                                                                                                                                                                                                                                                                  | picker 2026-05-11 superseded by ADR-023                              |
| Q2  | `DownloadAsync` 返回 superseded → 见 [§1.3.1](#131-q1--q2--q6-superseded-by-adr-023)                                                                                                                                                                                                                                                                                                                                                                                                      | picker 2026-05-11 superseded by ADR-023 errata·02                    |
| Q3  | **不锁容器名常量集**：业务命名空间各自传字符串（如 `kb-source` / `uploads` / `kb-extracted`）。[ADR-015 §容器命名](../../03-architecture/adr/ADR-015-object-storage-provider-switchable.md) 已锁 4 容器名但**不**在 `Inkwell.Abstractions` 暴露 `InkwellContainers` 常量；业务 HD 起草时各自决定（`Inkwell.Core.KnowledgeBase` / `Inkwell.Core.Multimodal` / 等）                                                                                                                         | picker 2026-05-11；理由：端口层尽量薄 / 容器名是业务语义不是端口语义 |
| Q4  | **预签名 URL TTL**：`Default` = 30 min，`Max` = 7 day（10080 min，Azure Blob / S3 双家共同上限），`Min` = 1 min                                                                                                                                                                                                                                                                                                                                                                           | picker 2026-05-11；三 Provider 交集 + 多模态 / KB ingest 场景够用    |
| Q5  | **`ListAsync` = `IAsyncEnumerable<FileObjectInfo>`**：流式迭代，实现层内部隐藏 continuation token；业务侧 `await foreach` 即可，不支持跳页                                                                                                                                                                                                                                                                                                                                                | picker 2026-05-11；与 ADR-015 §抽象接口 原型一致                     |
| Q6  | 错误码 superseded → 见 [§1.3.1](#131-q1--q2--q6-superseded-by-adr-023)                                                                                                                                                                                                                                                                                                                                                                                                                    | picker 2026-05-11 superseded by ADR-023 errata·01                    |
| Q7  | **`FileMetadata` 字段**：`ContentType`（MIME）+ `CustomMetadata`（`IReadOnlyDictionary<string, string>?`）+ `ContentDisposition`（可选）；**不**锁 `CacheControl` / `ContentEncoding`（v2 backlog）                                                                                                                                                                                                                                                                                       | picker 2026-05-11                                                    |
| Q8  | **`FileUploadResult` 字段**：`Container` + `Key` + `SizeBytes` + `ETag` + `UploadedTime`；**不**锁 `VersionId` / `Checksum`（v1 不取版本控制；checksum 与 Q6 错误码同时撤）                                                                                                                                                                                                                                                                                                               | picker 2026-05-11                                                    |
| Q9  | **`MaxObjectSizeBytes` = 100 MiB**（104857600 bytes）；超出抛 `InvalidOperationException`（message 前缀 `"Object size exceeds MaxObjectSizeBytes"`，errata·01 前为 `QuotaExceeded` 错误码）；REQ-016 多模态 / REQ-009 中小型 KB 文档够用，大文件 KB ingest 走 v2 分片上传                                                                                                                                                                                                                 | picker 2026-05-11 + errata·01 修订                                   |
| Q10 | **性能预算宽松档**：`Presign*` P50 < 100ms / P99 < 500ms；`Exists` / `Delete` P50 < 200ms / P99 < 1s；`Upload` / `Download` 主体不锁（依赖网络与文件大小），仅 SLO 化 facade overhead                                                                                                                                                                                                                                                                                                     | picker 2026-05-11                                                    |
| Q11 | **OTel span**：`filestore.upload` / `filestore.download` / `filestore.delete` / `filestore.exists` / `filestore.presign_upload` / `filestore.presign_download` / `filestore.list`；5 + 5 字段 = `filestore.provider` / `.container` / `.key` / `.size_bytes` / `.operation_outcome` + OTel 标准 `exception.type` / `.message` / `.stacktrace` / `.escaped` / `.id`（[HD-001 §5.3](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)；errata·01 前最后字段为 `error.code`） | picker 2026-05-11 + errata·01 修订                                   |

#### 1.3.1 Q1 / Q2 / Q6 superseded by ADR-023

下列三项 picker 决策已被 [ADR-023 三轮翻新](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) accepted by Inkwell 2026-05-11 整段废止。原决策保留**仅作历史证据**，**生效**的签名 + 错误处理对照见 [§1.4 一致性声明](#14-与-hd-001-53-bcl-对照表的一致性声明) + [§4 BCL 异常与日志](#4-bcl-异常与日志端口段补充-hd-001-4)。

- **Q1 原决策**：~~混合签名风格（`Upload` / `Download` 走 `Task<Result<T>>`、`Exists` / `Delete` 走 `Task<bool>`、`Presign*` 走 `Task<Uri>`、`List` 走 `IAsyncEnumerable<>`）~~ → **现决策**：全部翻新为裸 `Task<T>` / `Task<bool>` / `Task<Uri>` / `IAsyncEnumerable<>` + BCL 异常分流；混合"风格"概念整体废止，所有方法错误语义统一走 BCL 异常 + OTel `exception.*` 五字段。superseded by ADR-023 主决策。
- **Q2 原决策**：~~`DownloadAsync` 返回 `Result<FileDownloadResponse>`~~ → **现决策**：`DownloadAsync` 返回 `Task<FileDownloadResponse>`；`FileDownloadResponse` DTO 自身不变（仍含 Stream + 元数据 + ETag + 大小 + 时间戳，避免二次 HEAD 调用）；下载失败上抛 BCL 异常（`KeyNotFoundException` / `IOException` / `TimeoutException` 等，详 [§4.2](#42-bcl-异常分类业务失败-vs-程序错误)）。superseded by ADR-023 errata·02。
- **Q6 原决策**：~~9 个错误码（`ContainerNotFound` / `ObjectNotFound` / `ObjectAlreadyExists` / `UploadFailed` / `DownloadFailed` / `PresignedUrlGenerationFailed` / `QuotaExceeded` / `ConnectionFailed` / `InvalidKeyFormat`）~~ → **现决策**：本端口**不再分配** `INK-FILESTORE-NNN` 错误码；具体错误语义走 BCL 异常类型 + 异常 message 自描述 + OTel `exception.*` 五字段表达。BCL 异常映射表见 [§4.2 BCL 异常分类](#42-bcl-异常分类业务失败-vs-程序错误)。superseded by ADR-023 errata·01。

### 1.4 与 HD-001 §5.3 BCL 对照表的一致性声明

> **历史背景**：[ADR-023 三轮翻新](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) 2026-05-11 accepted 之前，本节标题为"与 HD-001 §5.2 偏离声明"——彼时 HD-001 §5.2 规定全线 `Task<Result<T>>`，HD-003 picker Q1=B 选混合签名是"偏离"。ADR-023 主决策 + errata·02 把 HD-001 §5.2 全线 Result 整段废止，全部端口翻为裸 `Task<T>` + BCL 异常——本节随之改名为"一致性声明"，记录翻新后的对齐结果。

本 HD 7 个方法的签名与错误处理对照见下；与 [HD-001 §5.3 BCL 对照表](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表) 完全一致：

- **`UploadAsync`** → 返回 `Task<FileUploadResult>`。失败：超 `MaxObjectSizeBytes` → `InvalidOperationException`（message 前缀 `"Object size exceeds MaxObjectSizeBytes"`）；中途中断 / 5xx → `IOException`；超时 → `TimeoutException`；容器名错 → `KeyNotFoundException`；`If-None-Match: *` 冲突 → `InvalidOperationException`（message 前缀 `"Object already exists"`，仅 v1 业务侧显式开启时返回）；`container` / `key` 含非法字符 → `ArgumentException`（paramName=container/key，message 前缀 `"Invalid key format"`）；取消 → `OperationCanceledException`。
- **`DownloadAsync`** → 返回 `Task<FileDownloadResponse>`。失败：容器 / 对象不存在 → `KeyNotFoundException`；中途中断 / 5xx → `IOException`；超时 → `TimeoutException`；取消 → `OperationCanceledException`。
- **`ExistsAsync`** → 返回 `Task<bool>`（**保留幂等语义**）。失败：DNS / TLS / network → `IOException`；超时 → `TimeoutException`；`container` / `key` 非法 → `ArgumentException`。正常路径返回 `true` / `false`，不抛 `KeyNotFoundException`（"不存在"是预期返回值）。
- **`DeleteAsync`** → 返回 `Task<bool>`（**保留幂等语义**）。`true` = 实际删除，`false` = 对象本不存在（[ADR-015](../../03-architecture/adr/ADR-015-object-storage-provider-switchable.md) idempotent delete）；失败同 `Exists`。
- **`CreatePresignedUploadUrlAsync`** / **`CreatePresignedDownloadUrlAsync`** → 返回 `Task<Uri>`。失败：凭证不足权 / SAS 策略未配 → `InvalidOperationException`（message 前缀 `"Presigned URL generation failed"`，通常 inner = 具体 SDK 异常）；TTL 超 `MaxPresignedUrlTtlMinutes` / 低于 `MinPresignedUrlTtlMinutes` → `ArgumentOutOfRangeException`（paramName=ttl）；连接失败 → `IOException`。
- **`ListAsync`** → 返回 `IAsyncEnumerable<FileObjectInfo>`（**流式语义**）。失败时 `IAsyncEnumerator.MoveNextAsync` 抛 `IOException` / `TimeoutException` / `OperationCanceledException`；业务侧用 `try { await foreach (...) }` 接（[HD-001 §5.3](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)）。

与 [ADR-015 接口草图](../../03-architecture/adr/ADR-015-object-storage-provider-switchable.md) 的差异：

- **`UploadAsync` 形态**：ADR-015 line 46 草图为 `Task<FileUploadResult>`（裸返回），H3 历史曾精化为 `Task<Result<FileUploadResult>>`，ADR-023 errata·02 后**回到 ADR-015 草图形态**——形态最终对齐。
- **`DownloadAsync` 形态**：ADR-015 line 47 草图为 `Task<Stream>`（裸 Stream），H3 精化为 `Task<FileDownloadResponse>`（含元数据，避免二次 HEAD）——形态精化但**非偏离**，[picker Q2=A](#13-关键决策摘要) 仍生效，仅 ADR-023 errata·02 后去除外层 `Result<>` 包装。

> **Reviewer 反查**：本 HD 与 HD-001 / ADR-023 主决策完全一致；H4 [h3-detailed-design-reviewer](../../../.github/agents/h3-detailed-design-reviewer.agent.md) 校验时不再有"§5.2 偏离"项，仅需机械核对裸 `Task<T>` + BCL 异常的全面落地。

## 2. 文件结构

```text
src/core/Inkwell.Abstractions/
  FileStorage/                               # （新增子目录）
    IFileStorageProvider.cs                  # 顶层 facade（7 方法）
    FileMetadata.cs                          # record class，上传时可选元数据
    FileUploadResult.cs                      # record class，上传回执
    FileDownloadResponse.cs                  # class（持 Stream，IAsyncDisposable + IDisposable），下载返回包装
    FileObjectInfo.cs                        # record class，List 元素
    FileStorageOptions.cs                    # 详细配置
    FileStorageOptionsValidator.cs           # IValidateOptions<FileStorageOptions>
  # ErrorCodes.FileStore.cs 已废锁位保留（ADR-023 errata·01 废错误码后整段废止；编号不重用、不分配新含义）
```

> **csproj 依赖白名单**：HD-003 不引入新依赖，仍仅 [HD-001 §2 锁定的](HD-001-Inkwell.Abstractions-foundation.md) `Microsoft.Extensions.{DependencyInjection,Configuration,Options,Logging}.Abstractions` + `Microsoft.Extensions.VectorData.Abstractions`（HD-008 起用）。**严禁**因本 HD 引入 `Azure.Storage.Blobs` / `Minio.*` / `Amazon.S3.*` 等任何具体 SDK（违反 [ADR-017 零外部包约束](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) + [RISK-011 三 Provider contract 漏出](../../03-architecture/risk-analysis.md)）。

## 3. 程序文件设计（10 字段 × 7 文件 + 1 已废锁位）

> [ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) 废错误码后，§3.8 `ErrorCodes.FileStore.cs` 整段废止；§3.1 ~ §3.7 编号不调整，§3.8 保留为"已废锁位"以保追溯不断。

### 3.1 `FileStorage/IFileStorageProvider.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/FileStorage/IFileStorageProvider.cs`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               |
| 职责         | 顶层文件存储 facade；7 个方法覆盖上传 / 下载 / 删除 / 探测 / 列表 / 预签名 URL；三 Provider 实现完全相同 ABI（[ADR-015](../../03-architecture/adr/ADR-015-object-storage-provider-switchable.md)）；全 7 方法签名走裸 `Task<T>` / `Task<bool>` / `Task<Uri>` / `IAsyncEnumerable<T>` + BCL 异常（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)），不走 `Result<T>`                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| 对外接口     | `public interface IFileStorageProvider { Task<FileUploadResult> UploadAsync(string container, string key, Stream data, FileMetadata? metadata = null, CancellationToken ct = default); Task<FileDownloadResponse> DownloadAsync(string container, string key, CancellationToken ct = default); Task<bool> ExistsAsync(string container, string key, CancellationToken ct = default); Task<bool> DeleteAsync(string container, string key, CancellationToken ct = default); Task<Uri> CreatePresignedUploadUrlAsync(string container, string key, TimeSpan ttl, CancellationToken ct = default); Task<Uri> CreatePresignedDownloadUrlAsync(string container, string key, TimeSpan ttl, CancellationToken ct = default); IAsyncEnumerable<FileObjectInfo> ListAsync(string container, string? keyPrefix = null, CancellationToken ct = default); }` |
| 内部函数或类 | 接口本身；实现由三 Provider HD 各自提供（`LocalFileSystemFileStorageProvider` / `AzureBlobFileStorageProvider` / `MinIOFileStorageProvider`）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| 输入数据     | `string container` + `string key` + `Stream data`（Upload） / `FileMetadata? metadata` / `TimeSpan ttl`（Presign） / `string? keyPrefix`（List） / `CancellationToken ct`（全部方法）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| 输出数据     | `FileUploadResult` / `FileDownloadResponse` / `bool` / `Uri` / `IAsyncEnumerable<FileObjectInfo>`（全部裸返回，不包 `Result<>`）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| 依赖模块     | `FileStorage/FileMetadata.cs` / `FileStorage/FileUploadResult.cs` / `FileStorage/FileDownloadResponse.cs` / `FileStorage/FileObjectInfo.cs` / System.IO（`Stream`） / System.Collections.Generic（`IAsyncEnumerable<T>`）（[ADR-023 errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 后删除 `Common/Result.cs` / `Common/Error.cs` 依赖）                                                                                                                                                                                                                                                                                                                                                                      |
| 错误处理     | 全部上抛 BCL 异常（见 [§4.2 BCL 异常分类](#42-bcl-异常分类业务失败-vs-程序错误) + [§1.4 一致性声明](#14-与-hd-001-53-bcl-对照表的一致性声明)）：`UploadAsync` → `KeyNotFoundException`（容器不存在） / `InvalidOperationException`（超 `MaxObjectSizeBytes` / `If-None-Match` 冲突） / `IOException`（中途中断） / `ArgumentException`（key 非法）；`DownloadAsync` → `KeyNotFoundException`（容器 / 对象不存在） / `IOException`（中途中断） / `TimeoutException`；`ExistsAsync` / `DeleteAsync` 保留幂等返回（`true` / `false`，"不存在"是预期返回值不抛异常）；`Presign*` → `InvalidOperationException`（凭证不足权 / SAS 策略未配） / `ArgumentOutOfRangeException`（ttl 越界）；全部方法 → `OperationCanceledException`（取消） / `IOException`（网络故障，含 `ExistsAsync` / `DeleteAsync`）                                                |
| 日志要求     | 实现层（三 Provider HD）在每个方法入口 / 出口写 OTel span，命名 `filestore.<verb>`（`upload` / `download` / `delete` / `exists` / `presign_upload` / `presign_download` / `list`）；5 + 5 字段 = 5 个 Inkwell 私有字段（`filestore.provider` / `filestore.container` / `filestore.key` / `filestore.size_bytes` / `filestore.operation_outcome`）+ 5 个 OTel 标准 `exception.*` 字段（`.type` / `.message` / `.stacktrace` / `.escaped` / `.id`，仅异常路径填充，详 §4.3）；`filestore.key` 可能含 PII—— 实现层**直接打**，调用方在写额外业务日志前自行过滤（[HD-001 §7 安全](HD-001-Inkwell.Abstractions-foundation.md)）；`filestore.operation_outcome` 值域 = `success` / `not_found` / `conflict` / `quota_exceeded` / `failed` / `cancelled`                                                                                                 |
| 测试要求     | `tests/core/Inkwell.Abstractions.Tests/FileStorage/IFileStorageProviderContractTests.cs`：契约测试（接口形态 ABI 锁定 via [`PublicApiAnalyzers`](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md)）；7 个方法签名 / 参数顺序 / 默认值 / 返回类型逐一验证；行为测试在 `tests/core/Inkwell.Providers.Contract/FileStorage/`（统一跨 Provider 家族契约包，与 [HD-002 §8](HD-002-Inkwell.Abstractions-persistence-port.md) / [HD-009 §6](../Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) / [file-structure.md 总体拓扑](../file-structure.md) 拓扑一致；[RISK-011 公共 contract test 包](../../03-architecture/risk-analysis.md)），三 Provider 跑同一套用例                                                                                                          |

### 3.2 `FileStorage/FileMetadata.cs`

| 字段         | 内容                                                                                                                                                                                                         |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 文件路径     | `src/core/Inkwell.Abstractions/FileStorage/FileMetadata.cs`                                                                                                                                                  |
| 职责         | 上传时可选元数据；含 ContentType + 自定义键值对 + ContentDisposition；三 Provider 原生支持                                                                                                                   |
| 对外接口     | `public sealed record FileMetadata { public string? ContentType { get; init; } public IReadOnlyDictionary<string, string>? CustomMetadata { get; init; } public string? ContentDisposition { get; init; } }` |
| 内部函数或类 | record 自身；构造期不做校验（允许全 null 即默认）；`CustomMetadata` 键值映射到 Azure Blob `x-ms-meta-` / S3 `x-amz-meta-` / LocalFS sidecar `.meta.json`（由 Provider HD 各自实现）                          |
| 输入数据     | 调用方构造时填写                                                                                                                                                                                             |
| 输出数据     | `FileMetadata` 实例                                                                                                                                                                                          |
| 依赖模块     | System.Collections.Generic                                                                                                                                                                                   |
| 错误处理     | 本 DTO 自身无错误；Provider 层在写入时若 `CustomMetadata` 键含非法字符（Azure / S3 仅允许 ASCII alnum + dash）→ Provider 层抛 `ArgumentException`（实现细节，不入本 HD）                                     |
| 日志要求     | DTO 自身不做日志；实现层在 `filestore.upload` span 中可输出 `filestore.content_type` 字段（仅 ContentType 入 OTel，自定义键值不入避免 PII）                                                                  |
| 测试要求     | `FileMetadataTests.cs`：(1) 全 null 构造合法；(2) `CustomMetadata` 空字典与 null 区分；(3) 等值比较（record 默认 value equality）                                                                            |

### 3.3 `FileStorage/FileUploadResult.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                     |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 文件路径     | `src/core/Inkwell.Abstractions/FileStorage/FileUploadResult.cs`                                                                                                                                                                                                          |
| 职责         | 上传成功回执；含定位 + 大小 + ETag + 时间戳，供业务侧持久化（如 `Inkwell.KnowledgeBase` 记 `kb-source` 文件位置）                                                                                                                                                        |
| 对外接口     | `public sealed record FileUploadResult(string Container, string Key, long SizeBytes, string ETag, DateTimeOffset UploadedTime);`                                                                                                                                         |
| 内部函数或类 | record 自身；构造期校验：`Container` / `Key` / `ETag` 非 null / empty（违反 → `ArgumentException`）；`SizeBytes >= 0`（违反 → `ArgumentOutOfRangeException`）；`UploadedTime.Offset == TimeSpan.Zero`（强制 UTC，与 HD-002 §3.7 mixin `IHasTimestamps` 锁定的 UTC 一致） |
| 输入数据     | 由 Provider 实现层构造                                                                                                                                                                                                                                                   |
| 输出数据     | `FileUploadResult` 实例（包在 `Result<FileUploadResult>` 中由 `IFileStorageProvider.UploadAsync` 返回）                                                                                                                                                                  |
| 依赖模块     | System                                                                                                                                                                                                                                                                   |
| 错误处理     | 构造期 `ArgumentException` / `ArgumentOutOfRangeException`（程序错误，调用方传错）                                                                                                                                                                                       |
| 日志要求     | DTO 自身不做日志；上传成功时实现层在 `filestore.upload` span 输出 `filestore.size_bytes` / `filestore.operation_outcome=success`                                                                                                                                         |
| 测试要求     | `FileUploadResultTests.cs`：构造期校验 4 条边界（null Container / null Key / 负 SizeBytes / 非 UTC UploadedTime） + value equality                                                                                                                                       |

### 3.4 `FileStorage/FileDownloadResponse.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 文件路径     | `src/core/Inkwell.Abstractions/FileStorage/FileDownloadResponse.cs`                                                                                                                                                                                                                                                                                                                                                                              |
| 职责         | 下载返回包装；含 Stream + 元数据 + ETag + 大小 + 时间戳；调用方拿到后**必须**`using` 或 `await using` 释放 `Content` 流                                                                                                                                                                                                                                                                                                                          |
| 对外接口     | `public sealed class FileDownloadResponse : IAsyncDisposable, IDisposable { public Stream Content { get; } public FileMetadata Metadata { get; } public string ETag { get; } public long SizeBytes { get; } public DateTimeOffset UploadedTime { get; } public FileDownloadResponse(Stream content, FileMetadata metadata, string eTag, long sizeBytes, DateTimeOffset uploadedTime); public ValueTask DisposeAsync(); public void Dispose(); }` |
| 内部函数或类 | 不用 record（`Stream` 不可 record value equality）；构造期校验同 §3.3；`Dispose` / `DisposeAsync` 委托 `Content.Dispose` / `Content.DisposeAsync`；`Metadata` 至少含 `ContentType`（实现层从 Provider 元数据填充）；UTC 强制同 §3.3                                                                                                                                                                                                              |
| 输入数据     | 由 Provider 实现层构造                                                                                                                                                                                                                                                                                                                                                                                                                           |
| 输出数据     | `FileDownloadResponse` 实例（由 `IFileStorageProvider.DownloadAsync` 直接 `Task<FileDownloadResponse>` 返回；[ADR-023 errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 后不再包 `Result<>`）                                                                                                                  |
| 依赖模块     | System.IO（`Stream`） / System.Threading.Tasks（`ValueTask`）                                                                                                                                                                                                                                                                                                                                                                                    |
| 错误处理     | 构造期 `ArgumentNullException`（`content` / `metadata` / `eTag` null） / `ArgumentOutOfRangeException`（负 `sizeBytes`） / `ArgumentException`（非 UTC `uploadedTime` 或空 `eTag`）                                                                                                                                                                                                                                                              |
| 日志要求     | DTO 自身不做日志；下载成功时实现层在 `filestore.download` span 输出 `filestore.size_bytes` / `filestore.operation_outcome=success`                                                                                                                                                                                                                                                                                                               |
| 测试要求     | `FileDownloadResponseTests.cs`：(1) 构造期 5 条边界；(2) `Dispose` / `DisposeAsync` 实际释放 `Content`；(3) `using` / `await using` 语法均工作；(4) 重复 Dispose 不抛（与 `Stream.Dispose` 一致）                                                                                                                                                                                                                                                |

### 3.5 `FileStorage/FileObjectInfo.cs`

| 字段         | 内容                                                                                                                                                    |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/FileStorage/FileObjectInfo.cs`                                                                                           |
| 职责         | `ListAsync` 流式枚举元素；轻量元数据，**不**含 Stream（下载需另发 `DownloadAsync`）                                                                     |
| 对外接口     | `public sealed record FileObjectInfo(string Container, string Key, long SizeBytes, string ETag, DateTimeOffset LastModifiedTime, string? ContentType);` |
| 内部函数或类 | record 自身；构造期校验同 §3.3（`Container` / `Key` / `ETag` 非空，`SizeBytes >= 0`，`LastModifiedTime.Offset == TimeSpan.Zero`）                       |
| 输入数据     | 由 Provider 实现层构造（从 `BlobItem` / `S3Object` / `FileInfo` 转换）                                                                                  |
| 输出数据     | `FileObjectInfo` 实例，作为 `IAsyncEnumerable<FileObjectInfo>` 元素                                                                                     |
| 依赖模块     | System                                                                                                                                                  |
| 错误处理     | 构造期同 §3.3                                                                                                                                           |
| 日志要求     | DTO 自身不做日志；实现层在 `filestore.list` span 输出 `filestore.size_bytes`（累加） / `filestore.operation_outcome`                                    |
| 测试要求     | `FileObjectInfoTests.cs`：构造期边界 + value equality + `ContentType` 可选                                                                              |

### 3.6 `FileStorage/FileStorageOptions.cs`

> [HD-001 §3.11.1 `InkwellProvidersOptions`](HD-001-Inkwell.Abstractions-foundation.md) 已承载 Provider 选择器 `Inkwell:Providers:FileStorage ∈ {"LocalFileSystem","AzureBlob","MinIO"}`；本 Options **不**重复承载 Provider 字段。

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/FileStorage/FileStorageOptions.cs`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| 职责         | 文件存储端口详细配置；从 `appsettings.json` `"Inkwell:FileStorage"` 段绑定                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| 对外接口     | `public sealed class FileStorageOptions { [Range(1, 10080)] public int DefaultPresignedUploadTtlMinutes { get; init; } = 30; [Range(1, 10080)] public int DefaultPresignedDownloadTtlMinutes { get; init; } = 30; [Range(1, 10080)] public int MaxPresignedUrlTtlMinutes { get; init; } = 10080; [Range(1, 10080)] public int MinPresignedUrlTtlMinutes { get; init; } = 1; [Range(1, 5L * 1024 * 1024 * 1024)] public long MaxObjectSizeBytes { get; init; } = 100L * 1024 * 1024; [Range(1, 1000)] public int ListPageSize { get; init; } = 100; public bool EnableSensitiveDataLogging { get; init; } = false; }` |
| 内部函数或类 | DataAnnotations 校验；TTL 单位统一为分钟（避免 `TimeSpan` JSON 绑定歧义）；`MaxObjectSizeBytes` 默认 100 MiB（[picker Q9](#13-关键决策摘要)）；Provider 特定的连接字符串 / 端点 / 凭证由各 Provider HD 自己的子 Options 承载（如 `MinIOFileStorageOptions` / `AzureBlobFileStorageOptions` / `LocalFileSystemFileStorageOptions`）                                                                                                                                                                                                                                                                                   |
| 输入数据     | 由 `IConfiguration` 绑定                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| 输出数据     | `FileStorageOptions` 实例（DI 通过 `IOptions<FileStorageOptions>` 注入）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| 依赖模块     | `System.ComponentModel.DataAnnotations`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
| 错误处理     | DataAnnotations 校验失败 → `OptionsValidationException`，host 兜底；Provider 不一致由 Builder DSL 抓 `InkwellBuilderException`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| 日志要求     | DI 启动期 `IValidateOptions` 失败时输出 OTel `exception.type=Microsoft.Extensions.Options.OptionsValidationException`（HD-001 §5.3）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| 测试要求     | `FileStorageOptionsTests.cs`：默认值、`appsettings.json` 绑定、TTL 上下限边界（1 / 10080 / 越界）、`MaxObjectSizeBytes` 默认 100 MiB（104857600 字节）、`ListPageSize` 默认 100、`EnableSensitiveDataLogging` 默认 false                                                                                                                                                                                                                                                                                                                                                                                             |

### 3.7 `FileStorage/FileStorageOptionsValidator.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                      |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/FileStorage/FileStorageOptionsValidator.cs`                                                                                                                                                                                                                                                |
| 职责         | `IValidateOptions<FileStorageOptions>` 实现；DataAnnotations + 跨字段校验                                                                                                                                                                                                                                                 |
| 对外接口     | `internal sealed class FileStorageOptionsValidator : IValidateOptions<FileStorageOptions> { public ValidateOptionsResult Validate(string? name, FileStorageOptions options); }`                                                                                                                                           |
| 内部函数或类 | (1) `Validator.TryValidateObject` DataAnnotations；(2) 跨字段：`MinPresignedUrlTtlMinutes <= DefaultPresignedUploadTtlMinutes <= MaxPresignedUrlTtlMinutes`；`MinPresignedUrlTtlMinutes <= DefaultPresignedDownloadTtlMinutes <= MaxPresignedUrlTtlMinutes`（默认值落在范围内）；Provider 特定连接 / 凭证不在本 Validator |
| 输入数据     | `FileStorageOptions` 实例                                                                                                                                                                                                                                                                                                 |
| 输出数据     | `ValidateOptionsResult.Success` / `Fail(IEnumerable<string>)`                                                                                                                                                                                                                                                             |
| 依赖模块     | `Microsoft.Extensions.Options` / `System.ComponentModel.DataAnnotations`                                                                                                                                                                                                                                                  |
| 错误处理     | 同 HD-001 §3.12，校验失败 → `Fail` 含全部消息                                                                                                                                                                                                                                                                             |
| 日志要求     | 失败由 `OptionsValidationException` 抛出，host 打 fatal                                                                                                                                                                                                                                                                   |
| 测试要求     | `FileStorageOptionsValidatorTests.cs`：(1) DataAnnotations 边界合格；(2) 跨字段：`DefaultUpload = 11000 / MaxUrl = 10080` 拒；`Min = 100 / Default = 50` 拒（默认低于最小）；(3) 默认值（30 / 30 / 10080 / 1）通过                                                                                                        |

### 3.8 `ErrorCodes.FileStore.cs`（已废锁位）

> **整段废止**（[ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) accepted by Inkwell 2026-05-11）：本端口**不再分配** `INK-FILESTORE-NNN` 错误码段；保留编号锁位 `INK-FILESTORE-001` ~ `INK-FILESTORE-099` **仅作历史证据**，编号不重用、不分配新含义。`ErrorCodes.FileStore.cs` 文件**不再创建**（与 HD-002 §3.10 同步执行）。

| 字段     | 内容                                                                                                                                                              |
| -------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径 | ~~`src/core/Inkwell.Abstractions/ErrorCodes.FileStore.cs`~~（不再创建）                                                                                           |
| 职责     | ~~9 条错误码常量~~ → 改走 BCL 异常类型 + OTel `exception.*` 五字段表达；具体 BCL 映射见 §4.2                                                                      |
| 历史决策 | [picker Q6 = 9 错误码](#13-关键决策摘要) 已 superseded by ADR-023 errata·01；详 [§1.3.1](#131-q1--q2--q6-superseded-by-adr-023)                                   |
| 替代方案 | BCL 异常映射表 §4.2 + OTel 五字段 §4.3                                                                                                                            |
| 编号锁位 | `INK-FILESTORE-001` ~ `INK-FILESTORE-099` 保留为追溯证据，不重用、不分配新含义                                                                                    |
| 测试要求 | 无（文件不创建）；CI 在 [§10 F3](#10-ci-自检命令grep-列表) 反向校验业务层 / 测试代码不再硬编码 `INK-FILESTORE-NNN` 字面量 / `ErrorCodes.FileStore.*` 命名空间引用 |

## 4. BCL 异常与日志（端口段补充 HD-001 §4）

> **错误处理路径**：本端口与业务命名空间统一采用裸 `Task<T>` + .NET BCL 异常。Inkwell 不自建错误码常量集 / 不自建 `Result<T>` / `Error` 抽象 / 不自建端口异常基类，仅保留 `InkwellConfigurationException` / `InkwellBuilderException` 用于 DI 装配期校验。具体 BCL 异常映射 + OTel `exception.*` 五字段详见下表与 [HD-001 §5.3](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)。

### 4.1 错误码（已废锁位）

[ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) 废错误码后，本端口**不再分配** `INK-FILESTORE-NNN` 错误码段；保留 `INK-FILESTORE-001` ~ `INK-FILESTORE-099` 编号锁位**仅作历史证据**，编号不重用、不分配新含义。具体错误语义改走 BCL 异常类型表达 + OTel `exception.*` 五字段，详 §4.2。

### 4.2 BCL 异常分类（业务失败 vs 程序错误）

按 [HD-001 §5.3 BCL 对照表](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表) 的分类语义：

- **业务失败 / 预期错误**（调用方应 try/catch 并按业务策略处理，**不**触发 P1 告警）：
  - `KeyNotFoundException`：容器 / 对象不存在；触发方法：`UploadAsync`（容器不存在）、`DownloadAsync`（容器 / 对象不存在）、`CreatePresignedDownloadUrlAsync`；message 前缀 `"Container '<name>' not found"` 或 `"Object '<container>/<key>' not found"`。注意：`ExistsAsync` / `DeleteAsync` **不抛**此异常（"不存在"是预期返回值，幂等返回 `false`）。
  - `InvalidOperationException`（超 quota）：`UploadAsync` 文件大小超 `MaxObjectSizeBytes`；message 前缀 `"Object size exceeds MaxObjectSizeBytes"`。
  - `InvalidOperationException`（对象已存在）：`UploadAsync` 在 `If-None-Match: *` 模式下冲突；message 前缀 `"Object already exists"`；仅 v1 业务侧显式开启时返回（默认 Last-Write-Wins）。
- **程序错误 / 失血告警**（运维介入修复，P1 / P2 告警，message + OTel `exception.*` 五字段定位根因）：
  - `IOException`：DNS / TLS / network 失败 / 5xx 响应 / disk full / 中途传输中断；触发方法：全部 7 个；message 应含具体根因（如 `"Network connection to MinIO endpoint failed"`）。
  - `TimeoutException`：单次远端调用超过 Provider 子 Options 的 `RequestTimeoutSeconds`；触发方法：全部 7 个；message 应含调用维度（如 `"UploadAsync timeout after 30s"`）。
  - `InvalidOperationException`（凭证 / SAS 失败）：`CreatePresignedUploadUrlAsync` / `CreatePresignedDownloadUrlAsync` 凭证不足权 / SAS 策略未配；message 前缀 `"Presigned URL generation failed"`，通常 `InnerException` = 具体 SDK 异常（`Azure.RequestFailedException` / `Minio.Exceptions.MinioException`）。
- **参数 / 取消错误**（调用方 bug，应在测试期暴露）：
  - `ArgumentException` / `ArgumentNullException`：`container` / `key` 为 null / empty / 含非法字符（`..` / 控制字符 / 超长 / LocalFS 绝对路径）；触发方法：全部 7 个；paramName = `"container"` / `"key"` / 类似；message 前缀 `"Invalid key format"`（路径 traversal 等）。
  - `ArgumentOutOfRangeException`：`Presign*` 的 `ttl` 超 `MaxPresignedUrlTtlMinutes` 或低于 `MinPresignedUrlTtlMinutes`；paramName = `"ttl"`。
  - `OperationCanceledException`：所有方法响应 `ct`（[HD-001 §4.3](HD-001-Inkwell.Abstractions-foundation.md)）。

### 4.3 OTel span / 字段

每个方法在实现层（三 Provider HD）按 [picker Q11](#13-关键决策摘要) 输出 span：

- `filestore.upload` ← `UploadAsync`
- `filestore.download` ← `DownloadAsync`
- `filestore.delete` ← `DeleteAsync`
- `filestore.exists` ← `ExistsAsync`
- `filestore.presign_upload` ← `CreatePresignedUploadUrlAsync`
- `filestore.presign_download` ← `CreatePresignedDownloadUrlAsync`
- `filestore.list` ← `ListAsync`

**Inkwell 私有字段**（5 个）：

- `filestore.provider`（`LocalFileSystem` / `AzureBlob` / `MinIO`）
- `filestore.container`
- `filestore.key`（不在 `list` span，list 用 `filestore.key_prefix`）
- `filestore.size_bytes`（上传 / 下载 / list 累加）
- `filestore.operation_outcome`：值域 = `success` / `not_found` / `conflict` / `quota_exceeded` / `failed` / `cancelled`

**OTel 标准字段**（5 个，按 [`exception.*` 语义约定](https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/)，仅异常路径填充）：

- `exception.type`（如 `System.IO.IOException` / `System.TimeoutException` / `System.InvalidOperationException`）
- `exception.message`（异常 `Message` 属性原文，**禁止**截断或脱敏，PII 由 OTel collector 边界过滤）
- `exception.stacktrace`（异常 `StackTrace` 全文）
- `exception.escaped`（`true` 表示异常逃逸出 span 边界，调用方未 catch）
- `exception.id`（异常实例唯一 ID，便于 Grafana / Tempo 跨 span 关联，由实现层用 `Guid.CreateVersion7().ToString()` 生成）

可选字段：`filestore.content_type`（仅 upload / download）、`filestore.ttl_minutes`（仅 presign）、`filestore.key_prefix` / `filestore.items_returned`（仅 list）。

> **PII 提示**：`filestore.key` / `filestore.container` 可能含用户 ID / Agent ID / 对话 ID；这些字段允许进 OTel（Inkwell 自托管 Grafana 栈在边界内），但调用方在写**额外**业务日志时应自行过滤。

### 4.4 失败时 Stream 释放语义（端口契约）

- `UploadAsync(Stream data, ...)`：**调用方负责** `data` 的释放（端口实现层不 Dispose 入参 Stream）；建议调用方在 `using` / `await using` 中传入。
- `DownloadAsync(...) → Task<FileDownloadResponse>`：**调用方负责** `FileDownloadResponse.Content` 的释放；`FileDownloadResponse` 自己实现 `IAsyncDisposable` + `IDisposable`，`Dispose` / `DisposeAsync` 委托给底层 `Stream`；若 `DownloadAsync` 上抛 BCL 异常（`KeyNotFoundException` / `IOException` / `TimeoutException`），无 `FileDownloadResponse` 实例，调用方**无需**释放 Stream（实现层在 throw 前自行清理已分配的下游 Stream 资源）。

## 5. 公共约定继承（HD-001）

### 5.1 命名

- `IFileStorageProvider` ↔ HD-001 §5.1 `I<Capability>Provider`
- 方法 `UploadAsync` / `DownloadAsync` 等 ↔ §5.1 异步方法以 `Async` 结尾
- DTO `FileMetadata` / `FileUploadResult` / `FileDownloadResponse` / `FileObjectInfo` ↔ §5.1 命名空间内自闭合
- `FileStorageOptions` ↔ §5.1 `<Provider>Options`

### 5.2 签名

- 全 7 个方法走裸 `Task<T>` / `Task<bool>` / `Task<Uri>` / `IAsyncEnumerable<T>` + BCL 异常，↔ [HD-001 §5.3 BCL 对照表](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)
- `Exists` / `Delete` 的 `bool` 返回值保留幂等语义（不是"偏离"，是流式 / 幂等查询本身的语义）
- `List` 的 `IAsyncEnumerable<T>` 流式语义保留（不被裸 `Task<T>` 覆盖）
- `CancellationToken ct = default` 全 7 方法必填 ↔ HD-001 §4.3

### 5.3 错误处理

- 业务失败 / 预期错误 → BCL 业务异常（`KeyNotFoundException` / `InvalidOperationException`）；调用方 try/catch 按业务策略处理
- 程序错误 / 失血告警 → BCL 程序异常（`IOException` / `TimeoutException` / `InvalidOperationException`）；触发运维告警
- 参数错误 → `ArgumentException` / `ArgumentNullException` / `ArgumentOutOfRangeException`
- 幂等查询 → `ExistsAsync` / `DeleteAsync` 返回 `bool`，"不存在"是预期返回值（不抛 `KeyNotFoundException`）
- 流式枚举 → `IAsyncEnumerator.MoveNextAsync` 抛 BCL 异常，业务侧用 `try { await foreach (...) }` 接
- 取消 → `OperationCanceledException`
- 实现层用 [`ActivitySource.StartActivity`](https://learn.microsoft.com/dotnet/api/system.diagnostics.activitysource.startactivity) 创建 span 后，**异常路径**自动用 `Activity.RecordException` 或 `Activity.SetStatus(ActivityStatusCode.Error, message)` 写入 `exception.*` 五字段（详 §4.3）

## 6. Builder DSL 钩子（给 Provider HD 的契约）

每个 Provider csproj 提供唯一入口扩展方法：

```csharp
// providers/Inkwell.FileStorage.MinIO/MinIOFileStorageBuilderExtensions.cs
public static class MinIOFileStorageBuilderExtensions
{
    public static IInkwellBuilder UseMinIOFileStorage(
        this IInkwellBuilder builder,
        IConfigurationSection section);
}

// providers/Inkwell.FileStorage.AzureBlob/AzureBlobFileStorageBuilderExtensions.cs
public static class AzureBlobFileStorageBuilderExtensions
{
    public static IInkwellBuilder UseAzureBlobFileStorage(
        this IInkwellBuilder builder,
        IConfigurationSection section);
}

// src/core/Inkwell.Core/FileStorage/LocalFileSystemBuilderExtensions.cs
public static class LocalFileSystemBuilderExtensions
{
    public static IInkwellBuilder UseLocalFileSystemFileStorage(
        this IInkwellBuilder builder,
        Action<LocalFileSystemFileStorageOptions>? configure = null);
}
```

每个扩展方法**必须**：(1) 校验入参非 null；(2) 调用 `builder.Services.AddSingleton<IFileStorageProvider, XxxFileStorageProvider>()`；(3) 注册 `IValidateOptions<FileStorageOptions>` + 各自 Provider 子 Options 的 Validator；(4) 与 `InkwellProvidersOptions.FileStorage` 取值交叉校验（不一致抛 `InkwellBuilderException(message)` —— [ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) 后由 [HD-001 §3.13](HD-001-Inkwell.Abstractions-foundation.md) 锁定的 BCL 程序错误子类抛出，不再用 `INK-CORE-006`）；(5) 返回 `builder`。

## 7. 性能 / 安全 / 可观测性

### 7.1 性能预算（[picker Q10=B 宽松档](#13-关键决策摘要)）

| 方法                              | facade overhead P50      | facade overhead P99 | 备注                                           |
| --------------------------------- | ------------------------ | ------------------- | ---------------------------------------------- |
| `CreatePresignedUploadUrlAsync`   | < 100ms                  | < 500ms             | 与 Azure SAS 算法 + 网络 RTT 同档              |
| `CreatePresignedDownloadUrlAsync` | < 100ms                  | < 500ms             | 同上                                           |
| `ExistsAsync`                     | < 200ms                  | < 1s                | HEAD 调用                                      |
| `DeleteAsync`                     | < 200ms                  | < 1s                | DELETE 调用                                    |
| `UploadAsync` / `DownloadAsync`   | 不锁（依网络与文件大小） | —                   | facade 自身 overhead 应 < 50ms（不计实际传输） |
| `ListAsync`                       | 不锁（依对象数量与分页） | —                   | facade 自身 overhead per page < 200ms          |

> 上述为 facade overhead（端口实现自身代码 + 一次远端调用 RTT），不计实际数据传输时间。

### 7.2 安全

- `FileStorageOptions.EnableSensitiveDataLogging` 默认 `false`；启用后会在 `filestore.*` span 中追加 `filestore.metadata.*` 详细字段（含 `CustomMetadata` 全键值）—— **仅 dev / 排障**用，prod 禁用
- 凭证（AzureBlob `AccountKey` / `ConnectionString`；MinIO `SecretKey`）由各 Provider 子 Options 承载，**不**在本 Options；走 [K8s Secret](https://kubernetes.io/docs/concepts/configuration/secret/) / Compose `.env`（[OQ-A006 closed §B](../../03-architecture/open-questions-arch.md)，v1 不引 Azure Key Vault）
- 客户端直传（[ADR-009](../../03-architecture/adr/ADR-009-multimodal-azure-speech.md)）依赖预签名 URL TTL 短期失效；最长 7 day 是 SAS / S3 共同上限，TLS Always-on 由 Provider 配置层强制
- `ArgumentException`（message 前缀 `"Invalid key format"`）兜底拒绝 path traversal（`..`）等攻击向量；实现层应额外做 Provider-specific 校验（如 LocalFS 拒绝绝对路径 / 软链接出 root）。[ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) 后不再用 `InvalidKeyFormat` 错误码字面量

### 7.3 可观测性

- 5 私有 + 5 OTel 标准 `exception.*` 字段进 OTel；本 HD 不锁告警规则（H4 [TestCaseAuthor](../../../.github/agents/h4-test-case-author.agent.md) 反推时锁），但建议告警维度：
  - `filestore.operation_outcome = failed | quota_exceeded` 速率 > 5/min → P1（业务侧或服务端持续失败）
  - `exception.type` ∈ {`System.IO.IOException`, `System.TimeoutException`} 速率 > 5/min → P1（连接 / 超时类失血，[ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) 后由 OTel `exception.*` 标准字段表达，原 `error.code=INK-FILESTORE-008` `ConnectionFailed` 模式已废）
  - `filestore.operation_outcome = not_found` 速率 > 50/min → P2（业务侧引用错位）
  - `exception.type = System.InvalidOperationException` + `exception.message LIKE 'Presigned URL generation failed%'` 速率 > 5/min → P2（凭证 / SAS 策略问题，原 `error.code=INK-FILESTORE-006` 模式已废）
  - `filestore.presign_*` P99 > 1s → P3（运维）

## 8. 测试要求

### 8.1 单元测试（本 HD 范围内）

- 测试项目：`tests/core/Inkwell.Abstractions.Tests/FileStorage/`（与 HD-001 同 csproj）
- 每个文件至少一个 `*Tests.cs` 配对（见 §3 各小节"测试要求"）
- 覆盖率门槛：DTO（`FileMetadata` / `FileUploadResult` / `FileDownloadResponse` / `FileObjectInfo`）≥ 95%；Options + Validator ≥ 90%；`IFileStorageProviderContractTests` 仅锁 ABI ≥ 100%

### 8.2 契约测试

- 接口 ABI 用 [`PublicApiAnalyzers`](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) 锁定
- `IFileStorageProvider` 形态变更 → 需新建 ADR + 影响 3 个 Provider HD

### 8.3 集成测试

- 本 HD **不**起集成测试（端口层无外部依赖）
- 三 Provider 行为测试在 `tests/core/Inkwell.Providers.Contract/FileStorage/`（统一跨 Provider 家族契约包，与 [HD-002 §8](HD-002-Inkwell.Abstractions-persistence-port.md) / [HD-009 §6](../Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) / [file-structure.md 总体拓扑](../file-structure.md) 拓扑一致；[RISK-011](../../03-architecture/risk-analysis.md)），由 Provider HD 联合起草；CI matrix 跑 LocalFileSystem（临时目录） / Azurite（本地 AzureBlob 模拟） / MinIO（本地容器）三 Provider 同一套用例

### 8.4 BannedSymbols（CI 强制）

- `Inkwell.Abstractions.FileStorage.*` 禁用引入 `Azure.Storage.Blobs.*` / `Minio.*` / `Amazon.S3.*` / `Microsoft.Extensions.AzureSdk.*` 等具体 SDK 命名空间（写在 `BannedSymbols.txt`，违反 → 编译阻塞）

## 9. 部署 / 配置

`Inkwell.Abstractions.csproj` 与端口层一同打镜像（无独立部署）。`appsettings.json` 顶层段：

```json
{
  "Inkwell": {
    "Providers": {
      "FileStorage": "MinIO"
    },
    "FileStorage": {
      "DefaultPresignedUploadTtlMinutes": 30,
      "DefaultPresignedDownloadTtlMinutes": 30,
      "MaxPresignedUrlTtlMinutes": 10080,
      "MinPresignedUrlTtlMinutes": 1,
      "MaxObjectSizeBytes": 104857600,
      "ListPageSize": 100,
      "EnableSensitiveDataLogging": false
    },
    "FileStorage:MinIO": {
      "Endpoint": "http://minio:9000",
      "AccessKey": "...",
      "SecretKey": "...",
      "UseSsl": false
    }
  }
}
```

> Provider 特定子段（`FileStorage:MinIO` / `FileStorage:AzureBlob` / `FileStorage:LocalFileSystem`）由各 Provider HD 起草时锁定。

## 10. CI 自检命令（grep 列表）

| 编号 | 检查项                                                                                                  | 命令（CI [GitHub Actions](https://docs.github.com/actions) 工作流引用）                                                                                                                                                               |
| ---- | ------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| F1   | 业务命名空间禁直接 `using Azure.Storage.Blobs.*` / `Minio.*` / `Amazon.S3.*`                            | `rg -n -e 'using\s+Azure\.Storage\.Blobs' -e 'using\s+Minio' -e 'using\s+Amazon\.S3' src/core/Inkwell.Core/ -g '!**/AgentRuntime/**'` 期望 0 行（多 `-e` flag 避免 markdown table escape `\|` 在 shell 中失效，N8）                   |
| F2   | `IFileStorageProvider` 接口签名稳定                                                                     | [PublicApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) `PublicAPI.Shipped.txt` diff                                                                             |
| F3   | 错误码常量 / 字面量已废锁位（ADR-023 errata·01）                                                        | `rg -n -e 'ErrorCodes\.FileStore\.' -e 'INK-FILESTORE-' src/core/ tests/core/ providers/` 期望 0 行（除 docs 历史 errata 段；编号锁位仅作历史证据，不在业务 / 测试 / Provider 代码中引用）                                            |
| F4   | DTO 构造期 UTC 校验存在                                                                                 | `rg -n 'UploadedTime\.Offset' src/core/Inkwell.Abstractions/FileStorage/` 期望 ≥ 1 行（在 `FileUploadResult.cs` / `FileDownloadResponse.cs` / `FileObjectInfo.cs`）                                                                   |
| F5   | OTel span 字段名一致                                                                                    | `rg -n -e '"filestore\.provider"' -e '"filestore\.container"' -e '"filestore\.key"' -e '"filestore\.size_bytes"' -e '"filestore\.operation_outcome"' src/core/ providers/` 期望全部在实现层覆盖（多 `-e` flag，N8）                   |
| F6   | 业务命名空间禁 `Result<T>` / `ErrorCodes` 引用（ADR-023 errata·02）                                     | `rg -n -e 'Common\.Result' -e 'Common\.Error' -e 'ErrorCodes\.' src/core/Inkwell.Core/ src/core/Inkwell.WebApi/ src/core/Inkwell.Worker/ providers/` 期望 0 行（仅 docs / tests 的 errata 历史段允许出现）                            |
| F7   | 端口层 7 方法签名均为裸 `Task<T>` / `Task<bool>` / `Task<Uri>` / `IAsyncEnumerable<>`（ADR-023 主决策） | `rg -n -e 'Task<Result<' -e 'Task<Result>' src/core/Inkwell.Abstractions/FileStorage/` 期望 0 行（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) 全线翻新后端口层不应残留 `Result<>` 包装） |

## 11. 待补 / 待评审

- **预签名 URL 直传协议细节**（PUT vs POST form / 必备 header / CORS 规则）—— 留到 Provider HD（特别是 `Inkwell.FileStorage.AzureBlob` 与 `Inkwell.FileStorage.MinIO`）起草
- **客户端上传 SDK 接入**——前端 [ADR-001](../../03-architecture/adr/ADR-001-client-runtime-electron-react.md) Electron 端走 [`@ag-ui/client`](https://github.com/ag-ui-protocol) 还是直接 `fetch + PUT`，留到客户端 HD
- **大文件分片上传 / 多模态语音流式上传**——v1 单次上传，v2 backlog（[ADR-015 §中性](../../03-architecture/adr/ADR-015-object-storage-provider-switchable.md) 已声明）

## 12. 跨模块章节贡献

本 HD 在以下跨模块文件中追加一级章节 `## Inkwell.Abstractions.FileStorage`：

- `docs/04-detailed-design/file-structure.md` — 新增 `Inkwell.Abstractions/FileStorage/` 子目录树
- `docs/04-detailed-design/database-design.md` — **不贡献**（端口层不直接接 DB；如业务侧 `Inkwell.Core.KnowledgeBase` 表存文件位置，由该 HD 起草时贡献）

> 跨模块章节追加由本 HD 起草后**立即同步**到对应文件（**只追加**不改其他模块章节）。

## 13. 决策记录

### 13.1 起草期 picker 决策（2026-05-11）

> 下表为 HD-003 起草日（2026-05-11）通过 13 轮 picker 拍板的初版决策。**部分条目已被后续 errata 全部或部分推翻**，请配合 §13.2 / §13.3 / §13.4 errata chronicle 一并阅读：原表保留作为决策考古证据，**不得**作为编码参考。

| 字段                    | 选定值                                                                                                                                                                                | picker 时间 |
| ----------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------- |
| 签名风格                | 混合（重负荷 Result + 幂等查询 bool + Presign Uri + List IAsyncEnumerable）                                                                                                           | 2026-05-11  |
| Download 返回           | `Result<FileDownloadResponse>`（含元数据）                                                                                                                                            | 2026-05-11  |
| 容器名常量集            | 不锁（留业务 HD）                                                                                                                                                                     | 2026-05-11  |
| 预签名 URL TTL          | Default 30 min / Max 7 day / Min 1 min                                                                                                                                                | 2026-05-11  |
| List 分页风格           | `IAsyncEnumerable<FileObjectInfo>`（内部隐藏游标）                                                                                                                                    | 2026-05-11  |
| 错误码                  | 9 条（ContainerNotFound / ObjectNotFound / ObjectAlreadyExists / UploadFailed / DownloadFailed / PresignedUrlGenerationFailed / QuotaExceeded / ConnectionFailed / InvalidKeyFormat） | 2026-05-11  |
| `FileMetadata` 字段     | ContentType + CustomMetadata + ContentDisposition                                                                                                                                     | 2026-05-11  |
| `FileUploadResult` 字段 | Container + Key + SizeBytes + ETag + UploadedTime                                                                                                                                     | 2026-05-11  |
| `MaxObjectSizeBytes`    | 100 MiB                                                                                                                                                                               | 2026-05-11  |
| 性能预算                | 宽松（Presign P50 < 100ms / P99 < 500ms；Exists/Delete P50 < 200ms / P99 < 1s）                                                                                                       | 2026-05-11  |
| OTel span / 字段        | `filestore.<verb>` × 7 + 6 私有字段                                                                                                                                                   | 2026-05-11  |

### 13.2 errata·第二轮（2026-05-11，[ADR-023 主决策](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)）

- **触发**：H3 起草过程中暴露"端口层应用层 Result + Exception 双轨"在 [HD-001 §3.13](HD-001-Inkwell.Abstractions-foundation.md) BCL 异常分流方案落地后已无必要；混合签名（重负荷 Result + 幂等 bool + Presign Uri）在 ADR-023 主决策接受后被裁剪为"全线裸 `Task<T>` + BCL 异常"。
- **picker 结论**：A=A 全线翻新（不留 Result 兼容层）。
- **上游证据链**：
  - [ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)（accepted）
  - [HD-001 §5.3 BCL 异常映射表](HD-001-Inkwell.Abstractions-foundation.md)
  - [HD-002 §13.2](HD-002-Inkwell.Abstractions-persistence-port.md)（兄弟端口 Persistence 的对应 errata，先行案例）
- **落地清单（本 HD §1.3 / §1.4 / §3.1 / §5.2 / §5.3 已应用）**：
  - §1.3 Q1=B 混合签名 → 改为引用 §1.3.1，被 ADR-023 主决策推翻为"全 7 方法裸 `Task<T>` / `Task<bool>` / `Task<Uri>` / `IAsyncEnumerable<>`"
  - §1.3 Q2=A `Result<T>` 包装 → 改为引用 §1.3.1，被推翻为不再包装
  - §1.3 Q6=A `Common.Result + Common.Error` 抽象 → 改为引用 §1.3.1，被推翻为删除整个 `Common/` 抽象
  - §3.1 `IFileStorageProvider` 7 个方法签名全部去 `Result<>`，改裸 `Task<T>`；错误处理列从"业务失败 → Result.Failure(error)"改为"BCL 异常分类（KeyNotFoundException / IOException / TimeoutException / InvalidOperationException / ArgumentException / OperationCanceledException）"
  - §5.2 / §5.3 sample 代码全线去 `result.IsSuccess` 模式，改 `try { ... } catch (KeyNotFoundException) { ... } catch (IOException) { ... }`
  - §1.4 标题从"偏离声明"改"一致性声明"——本 HD 与 [ADR-015](../../03-architecture/adr/ADR-015-object-storage-provider-switchable.md) 在 ADR-023 框架下重新对齐
- **下批待办（已开）**：
  - [ADR-015 二次 errata](../../03-architecture/adr/ADR-015-object-storage-provider-switchable.md)：把 ADR-015 中“端口签名 `Result<T>`”段改为“裸 `Task<T>` + BCL 异常 + ADR-023 错误处理决策树”——由 [h2-architect-advisor](../../../.github/agents/h2-architect-advisor.agent.md) 起草

### 13.3 errata·第三轮（2026-05-11，[ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流)）

- **触发**：errata·第二轮把端口签名翻为裸 `Task<T>` 后，发现 `ErrorCodes.FileStore.cs` 9 条错误码常量在业务命名空间内**不再有任何调用方**（错误分流职责已转给 BCL 异常类型 `KeyNotFoundException` / `IOException` / `TimeoutException` / `InvalidOperationException` / `ArgumentException`）；继续保留只会让 H4 / H5 / Provider HD 误判为"应该用错误码"，污染 BCL 异常分流共识。
- **picker 结论**：A 整段废错误码机制（`ErrorCodes.FileStore.cs` 不再创建，编号 INK-FILESTORE-001 ~ INK-FILESTORE-009 仅作历史锁位，不进编译产物）。
- **上游证据链**：
  - [ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流)（accepted）
  - [HD-001 §3.13 BCL 异常子类锁位](HD-001-Inkwell.Abstractions-foundation.md)（`InkwellConfigurationException` / `InkwellBuilderException` 仅 DI 装配期保留，业务异常一律走 BCL）
  - [HD-002 §13.3](HD-002-Inkwell.Abstractions-persistence-port.md)（兄弟端口 Persistence 同步 errata）
- **落地清单（本 HD §0 / §1.1 / §3.8 / §6 / §7.2 / §7.3 / §10 已应用）**：
  - §0 增"错误处理约定"callout：业务命名空间不引用 `ErrorCodes.*` 字面量；DI 装配期可抛 `InkwellBuilderException` / `InkwellConfigurationException`
  - §1.1 文件清单加删除线：`ErrorCodes.FileStore.cs`（已废锁位）
  - §2 文件树注释标记 `ErrorCodes.FileStore.cs` 已废
  - §3 章节头说明"10 字段 × 7 文件 + 1 已废锁位"
  - §3.8 整段改为"已废锁位"bullet list（不再起草、不再创建）
  - §4 章节头从"错误码映射"改"BCL 异常与日志"；§4.2 从"错误分类"改"BCL 异常分类（业务失败 vs 程序错误）"，列出三类异常分类（业务失败 → `KeyNotFoundException` / `IOException` 等；程序错误 → `InvalidOperationException` / `ArgumentException`；超时 / 取消 → `TimeoutException` / `OperationCanceledException`）
  - §4.3 OTel span 字段从"6 私有 + error.code"改"5 私有 + 5 OTel 标准 `exception.*` 字段（`exception.type` / `exception.message` / `exception.stacktrace` / `exception.escaped` / `exception.id`）"
  - §6 Builder DSL 钩子第 (4) 项："不一致 → `INK-CORE-006`"改"不一致 → `InkwellBuilderException(message)`"
  - §7.2 安全：`InvalidKeyFormat` 错误码改 `ArgumentException(message: "Invalid key format ...")`
  - §7.3 可观测性告警规则增加 `exception.type` 维度（`System.IO.IOException` / `System.TimeoutException` 速率告警；原 `error.code=INK-FILESTORE-008/006` 模式声明已废）
  - §10 F3 翻为"错误码常量 / 字面量已废锁位"，期望 `rg 'ErrorCodes\.FileStore\.\|INK-FILESTORE-' src/core/ tests/core/ providers/` 0 行
- **下批待办（已开）**：
  - 同 §13.2 末：[ADR-015 二次 errata](../../03-architecture/adr/ADR-015-object-storage-provider-switchable.md) 同时更新

### 13.4 errata·第四轮（2026-05-11，[ADR-023 errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常)）

- **触发**：errata·第二轮翻完端口签名、errata·第三轮废了错误码常量后，`Common.Result<T>` / `Common.Error` 抽象在业务 / 测试 / Provider 中**已无单一调用方**；继续保留只会引诱 H5 / Provider HD 重新发明"Result + Error"轨道，与 ADR-023 BCL 异常分流共识冲突。
- **picker 结论**：A=A 删除整个 `Inkwell.Abstractions/Common/` 命名空间（`Result.cs` / `Error.cs` 不再创建，编号 INK-COMMON-001 ~ INK-COMMON-002 仅作历史锁位）。
- **上游证据链**：
  - [ADR-023 errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常)（accepted）
  - [HD-001 §3.16 / §3.17 已废锁位](HD-001-Inkwell.Abstractions-foundation.md)
  - [HD-002 §13.4](HD-002-Inkwell.Abstractions-persistence-port.md)（兄弟端口 Persistence 同步 errata）
- **落地清单（本 HD §0 / §3.4 / §3.6 / §10 已应用）**：
  - §0 增 errata·第二+三+四轮 callout 段（合并三轮总结）
  - §3.4 `FileDownloadResponse` 输出数据列：从"包在 `Result<FileDownloadResponse>` 中由 `IFileStorageProvider.DownloadAsync` 返回"改"由 `IFileStorageProvider.DownloadAsync` 直接 `Task<FileDownloadResponse>` 返回；ADR-023 errata·02 后不再包 `Result<>`"
  - §3.6 `FileStorageOptions` 错误处理列：删 `INK-CORE-006` 字面量，统一引用 `InkwellBuilderException`；日志要求列改 OTel `exception.type` 标准字段
  - §10 新增 F6 / F7：F6 grep `Common.Result` / `Common.Error` / `ErrorCodes.` 期望 0 行；F7 grep `Task<Result<` 期望端口层 0 行
- **下批待办**：无（HD-003 内 ADR-023 三轮 errata 全部应用完毕；跨模块层面待 [ADR-015 二次 errata](../../03-architecture/adr/ADR-015-object-storage-provider-switchable.md) 收尾）
