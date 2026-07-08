---
id: ADR-015-object-storage-provider-switchable
stage: H2
status: reviewed
authors:
  - name: H2-ArchitectAdvisor
    role: agent
reviewers: [Inkwell]
created: 2026-05-08
updated: 2026-05-08
upstream:
  - REQ-inkwell-agent-platform
  - repo-impact-map-inkwell-agent-platform
  - ADR-002
  - ADR-005
  - OQ-A005
downstream:
  - ADR-009
---

# ADR-015 文件存储：Provider 可切换（本地 / Azure Blob / MinIO）

## 上下文

[REQ-009 知识库](../../01-requirements/requirements.md) + [REQ-016 多模态](../../01-requirements/requirements.md) 都需要一个文件存储层来承载用户上传文件、知识库原始文档 + 抽取产物等大文件。

[OQ-A005](../open-questions-arch.md) 最初的默认值 A 是单一 Azure Blob（dev = Azurite emulator），这与 [ADR-004 IPersistenceProvider 切换](./ADR-004-data-store-provider-switchable-ef-core.md) 的思路不一致：关系数据已经支持 SQL Server / PostgreSQL 两 Provider 切换，但文件存储被锁死在 Azure 上，会让“非 Azure 客户”的部署路径断裂；同时 dev 拉 Azurite 容器对单元测试 / 离线开发也是不必要的成本。

Owner 在 H2 阶段提出新决议：文件存储应与关系层同构，提供三 Provider 切换；抽象接口名采用与 [`IPersistenceProvider`](./ADR-004-data-store-provider-switchable-ef-core.md) / [`ICacheProvider`](./ADR-016-cache-provider-redis.md) 一致的 `*Provider` 后缀。本 ADR 落实这一决议。

## 决策

**文件存储采用 `IFileStorageProvider` 抽象 + 三 Provider 切换实现：**

- **`LocalFileSystem`**：写入本地磁盘（默认路径 `./data/objects/`，可通过 `Inkwell:FileStorage:Local:RootPath` 配置）。用于单元测试 / 单体部署 / 完全离线场景；类比 [ADR-004](./ADR-004-data-store-provider-switchable-ef-core.md) 的 InMemory Provider。
- **`AzureBlob`**：[Azure Blob Storage](https://learn.microsoft.com/azure/storage/blobs/)，prod 锁 Azure 客户使用；dev 也可对接 [Azurite emulator](https://learn.microsoft.com/azure/storage/common/storage-use-azurite)。
- **`MinIO`**：[MinIO](https://min.io/)（S3 兼容），dev 默认跑在 Compose；prod 给“自建 K8s / 不出 Azure”的客户使用。

切换由 `appsettings.json` 的 `Inkwell:FileStorage:Provider` 字段（值域 `LocalFileSystem` / `AzureBlob` / `MinIO`）控制。

### 抽象接口（最小必要面）

> **2026-05-11 errata**（[HD-003 §1.4 偏离表](../../04-detailed-design/Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md) + [design-review-report §7.3 B4](../../04-detailed-design/design-review-report.md#7-hd-003-filestorage-port-增量评审2026-05-11)）：下方代码块为 H2 示意骨架。H3 [HD-003 §3.1](../../04-detailed-design/Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md) 以 ABI 锁定为准，在本骨架上作三处精化（不动本 ADR 主决策及状态）：(1) 参数名 `meta` → `metadata`（避 MAF `Microsoft.Agents.AI.Metadata` 同名冲突 + 源代码可读性）；(2) 参数名 `prefix` → `keyPrefix`（表达 key prefix 语义）；(3) `UploadAsync` 返回类型 `Task<FileUploadResult>` → `Task<Result<FileUploadResult>>` + `DownloadAsync` `Task<Stream>` → `Task<Result<FileDownloadResponse>>`（按 [HD-001 §5.2](../../04-detailed-design/Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) 重负荷锁 `Result<T>` + picker Q2 含元数据避免二次 HEAD）。三 Provider 实现及下游 HD 均以 [HD-003 §3.1](../../04-detailed-design/Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md) ABI 为准。

```csharp
public interface IFileStorageProvider
{
    Task<FileUploadResult> UploadAsync(string container, string key, Stream data, FileMetadata? meta = null, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string container, string key, CancellationToken ct = default);
    Task<bool> DeleteAsync(string container, string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string container, string key, CancellationToken ct = default);
    Task<Uri> CreatePresignedUploadUrlAsync(string container, string key, TimeSpan ttl, CancellationToken ct = default);
    Task<Uri> CreatePresignedDownloadUrlAsync(string container, string key, TimeSpan ttl, CancellationToken ct = default);
    IAsyncEnumerable<FileObjectInfo> ListAsync(string container, string? prefix = null, CancellationToken ct = default);
}
```

- 三 Provider 都要实现完整接口（包含预签名 URL）；本地存储通过后端中转 + 短期 Token 模拟"预签名"语义。
- 客户端上传走"后端发预签名 URL → 客户端直传 Provider"模式（[ADR-009 多模态](./ADR-009-multimodal-azure-speech.md)），三 Provider 客户端代码完全一致。

### 容器命名约定

- `uploads/` — 用户即时上传（多模态 / 临时）
- `kb-source/` — 知识库原始文件
- `kb-extracted/` — 知识库抽取产物（文本 / OCR / 元数据）

三 Provider 在底层概念上分别映射为：

- `LocalFileSystem` → `<RootPath>/<container>/<key>`
- `AzureBlob` → Container（容器）+ Blob
- `MinIO` → Bucket + Object（容器名即 bucket 名，部署时由 [Helm post-install hook](https://helm.sh/docs/topics/charts_hooks/) 创建）

### 部署组合（默认）

| 环境                        | 默认 Provider       | 备注                                                                |
| --------------------------- | ------------------- | ------------------------------------------------------------------- |
| 本地单元测试                | `LocalFileSystem`   | 临时目录 + MSTest `[TestCleanup]` 清理，零依赖                      |
| dev Docker Compose          | `MinIO`             | 跨平台跨开发机一致                                                  |
| prod AKS（Azure 客户）      | `AzureBlob`         | 与 [Q-A5 AKS](./ADR-005-deployment-docker-compose-aks.md) 同 region |
| prod AKS（自建 / 非 Azure） | `MinIO` StatefulSet | Helm values 切换                                                    |

> 客户根据自身合规 / 网络条件在 Helm values 中选择 prod Provider，三 Provider 客户端代码 **不变**。

## 备选项

### 备选 A：单一 Azure Blob（OQ-A005 §A，本 ADR 取代）

- **放弃理由**：(1) 把客户锁死在 Azure 上，与 [ADR-004 EF Provider 切换](./ADR-004-data-store-provider-switchable-ef-core.md) 的"客户可选部署形态"理念不一致；(2) dev 必须拉 Azurite 容器，单元测试场景下额外成本；(3) Owner 已明确提出新决议。

### 备选 B：单一 MinIO（OQ-A005 §B）

- **放弃理由**：(1) 锁定自托管 MinIO 反过来失去 Azure 客户的"managed service"红利（无运维、自动备份、跨区复制由 Azure 提供）；(2) 单元测试仍要拉 MinIO 容器，仍未解决纯 LocalFileSystem 场景；(3) 与"Provider 可切换"思路相比是另一种锁定。

### 备选 C：数据库 BLOB 字段（OQ-A005 §C）

- **放弃理由**：(1) 大文件写入数据库会爆破业务表 IO 与备份大小；(2) 与 [ADR-004 IPersistenceProvider 切换](./ADR-004-data-store-provider-switchable-ef-core.md) 的关系层目标背离（关系层不应承担流式大对象）；(3) 预签名 URL 直传无法实现，所有上传都得过后端，吞吐瓶颈明显。

### 备选 D：本 ADR 决议 — 三 Provider 切换

- **被选用**：(1) 与 [ADR-004 IPersistenceProvider](./ADR-004-data-store-provider-switchable-ef-core.md) 同构，认知负担小；(2) 客户端代码零差异；(3) `LocalFileSystem` 解决单元测试 + 单体部署 + 离线场景。

### 备选 E：仅 LocalFileSystem + AzureBlob（不要 MinIO）

- **放弃理由**：缺 MinIO → 自建 K8s 客户没有"可水平扩展、对象存储语义齐备"的 prod 选项；纯 LocalFileSystem 在多副本部署下无法共享，prod 不可用。

## 后果

### 正面

- 客户可选部署形态：Azure 客户用 AzureBlob，自建客户用 MinIO，离线 / 测试用 LocalFileSystem。
- 与 [ADR-004 IPersistenceProvider](./ADR-004-data-store-provider-switchable-ef-core.md) + [ADR-016 ICacheProvider](./ADR-016-cache-provider-redis.md) 同构 — 命名、配置 key、Migration 思路、CI matrix 全部复用。
- 客户端 [ADR-009](./ADR-009-multimodal-azure-speech.md) 直传逻辑零差异，UI 不感知 Provider。
- 单元测试用 `LocalFileSystem` + 临时目录 → 不依赖任何容器，CI 启动快。
- 与 [Q-A5 AKS](./ADR-005-deployment-docker-compose-aks.md) 部署形态兼容（Helm values 切 Provider）。

### 负面

- 三 Provider 实现 + 集成测试 matrix 是真实工作量；通过把“预签名 URL + Stream 上传 + 列表分页”等公共契约写死在 [`Inkwell.FileStorage.Tests.Contract`](../../01-requirements/repo-impact-map.md) 包里、所有 Provider 都跑相同 contract test 来缓解。详见 [RISK-011](../risk-analysis.md)。
- `LocalFileSystem` 的"预签名 URL"是模拟实现（短期签名 Token + 后端中转）— 性能弱于真正的对象存储直传；该 Provider 仅推荐单实例 / 测试场景。
- Helm Chart 需要支持三 Provider 的 values 模板（凭据来源 / 持久化 / Bucket 初始化）；H3 详细设计要把这部分作为独立任务交付。

### 中性

- MinIO 是 [Apache 2.0 / AGPLv3 双协议](https://github.com/minio/minio/blob/master/LICENSE)，AGPL 部分需要客户自行评估合规边界（v1 仅作为 dev / 自托管 prod 选项，无外部分发场景）。
- 知识库大文件（[REQ-009](../../01-requirements/requirements.md)）的"分片上传"语义在三 Provider 上行为接近，但本地 Provider 的并发写入需要文件锁 — 这是 LocalFileSystem 的实现细节，对调用方透明。

## 状态

- **状态**：accepted（接受 [OQ-A005](../open-questions-arch.md) 重开 + 新决议 §D）
- **首次发布**：2026-05-08
- **关联**：supersedes 无；上游 [ADR-002](./ADR-002-backend-runtime-dotnet10-aspnetcore.md) / [ADR-005](./ADR-005-deployment-docker-compose-aks.md) / [OQ-A005](../open-questions-arch.md)；下游 [ADR-009](./ADR-009-multimodal-azure-speech.md)
- **置信度**：medium（依赖 H3 详细设计验证三 Provider contract test 的覆盖完整性 → [RISK-011](../risk-analysis.md)）
