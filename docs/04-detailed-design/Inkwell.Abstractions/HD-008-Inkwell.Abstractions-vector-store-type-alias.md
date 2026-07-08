---
id: HD-008
title: Inkwell.Abstractions 详细设计 — Vector Store Type-Alias + Builder DSL 钩子（复用 Microsoft.Extensions.VectorData）
stage: H3
status: reviewed
reviewers: [Inkwell]
upstream:
  - REQ-009
  - REQ-010
  - ADR-003
  - ADR-017
  - ADR-020
  - ADR-023
  - HD-001
  - HD-002
---

<!-- markdownlint-disable MD060 -->
<!-- 中文 + 英文混排长表格在 markdownlint 列宽计算下字面对齐 ≠ 视觉对齐（详 /memories/markdown-lint.md，与 HD-004 ~ HD-006 同处理方式），表格仍按 docs-style §3 视觉对齐维护，机械 MD060 不予执行。 -->

> **错误处理约定**（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)，含 [errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) 废错误码、[errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 删 `Result<T>` / `Error` 抽象）：本 HD 不定义任何运行期方法（见 §1.2），仅 Options + Builder DSL 装配期扩展方法；装配期失败仍统一走 `IValidateOptions` 失败结果 / `InkwellBuilderException`，与 [HD-001 §4.1](HD-001-Inkwell.Abstractions-foundation.md#41-错误表达机制) / [HD-001 §3.11.1](HD-001-Inkwell.Abstractions-foundation.md#3111-optionsinkwellprovidersoptionscsf9-新增) 规约一致。
>
> **核心性质声明（与 HD-002 ~ HD-006 五个端口不同）**：本 HD **不设计新接口**。[HD-001 §1.1](HD-001-Inkwell.Abstractions-foundation.md#11-职责) 与 [ADR-020 §决策](../../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) 已锁定：Inkwell **不重新发明** `IVectorStore`，业务命名空间（`Inkwell.Core.KnowledgeBase` / `.Memory`）直接使用 [`Microsoft.Extensions.VectorData.VectorStore`](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-vector-data) / `VectorStoreCollection<TKey, TRecord>` + `[VectorStoreKey]` / `[VectorStoreData]` / `[VectorStoreVector]` attribute model。本 HD 仅覆盖：(1) 让该类型在 `Inkwell.Abstractions` 内"可直接可用"的 type-alias 复用声明；(2) `Inkwell.Abstractions` 层级、Provider 无关的 `VectorStoreOptions`；(3) 3 个 Builder DSL 扩展方法（`UseQdrantVectorStore` / `UseInMemoryVectorStore` / `UseAzureOpenAIEmbeddings`）的**签名与物理落位**——具体扩展方法实现代码分别落在 `Inkwell.Core/VectorStore/`（2 个方法，2026-07-06 picker Q1）与 `providers/Inkwell.VectorStore.Qdrant/`（1 个方法，2026-07-06 picker Q2），不在 `Inkwell.Abstractions` 内实现（避免端口层引入 Qdrant / Azure OpenAI SDK 包，违反 [ADR-017 §依赖规则第 4 条](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md)）。
>
> **范围切片**：本 HD 覆盖 `Inkwell.Abstractions/VectorStore/` 子层——`VectorStoreOptions`（Provider 无关的 embedding 模型 / 维度 / 距离度量 / 敏感日志开关）+ `VectorStoreOptionsValidator` + `GlobalUsings.cs` 的 type-alias 追加行（[§3.3](#33-globalusingscs-追加行hd-001-既有文件)）。**不**实现 `UseQdrantVectorStore` / `UseAzureOpenAIEmbeddings` 的具体代码（分别在 `providers/Inkwell.VectorStore.Qdrant` 与 `Inkwell.Core/VectorStore` 独立 HD 起草，本 HD §4 仅锁定二者的**方法签名**供实现方遵循）、**不**实现 `InMemoryVectorStore`（`Inkwell.Core` 独立 HD 起草）、**不**锁定 KB / Memory 的 collection 命名约定 / chunking / retention（[ADR-020 §接口粒度](../../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) 明确"H3 详细设计落地，本 ADR 不锁"，留 `Inkwell.Core.KnowledgeBase` / `.Memory` 业务 HD）、**不**新增 `IEmbeddingProducer` 或任何包裹 `IEmbeddingGenerator<TInput, TEmbedding>` 的 Inkwell 门面接口（ADR-020 字面表述是 KB / Memory 业务层直接注入 `IEmbeddingGenerator<string, Embedding<float>>`；`Microsoft.Extensions.AI.Abstractions` 已对称纳入 `Inkwell.Abstractions.csproj` 依赖白名单 + `GlobalUsings.cs`，与 `Microsoft.Extensions.VectorData.Abstractions` 处理完全同构（[HD-001 §13 2026-07-06 errata·第六轮](HD-001-Inkwell.Abstractions-foundation.md#2026-07-06-errata第六轮b15-对称纳入-microsoftextensionsaiabstractions-白名单)，design-review-report.md §18 B15 Owner picker 选项 1），不需要 Inkwell 自己包一层门面接口，详 [§13 Q5](#13-决策记录) / [§6](#6-与-iembeddinggeneratortinput-tembedding-的衔接)）。
>
> **跨 HD 关联**：本 HD 复用 [HD-001 §2](HD-001-Inkwell.Abstractions-foundation.md#2-文件结构) 已锁定的 `Inkwell.Abstractions.csproj` 依赖白名单（`Microsoft.Extensions.VectorData.Abstractions` 已在 HD-001 起草时预留，本 HD 起用）+ `Builder/IInkwellBuilder.cs`（本 HD 不重新定义 `IInkwellBuilder`，仅新增挂在其上的扩展方法签名声明）+ `Options/InkwellOptions.cs`（[§3.4](#34-optionsinkwelloptionscs-追加字段hd-001-既有文件) 追加 `VectorStore` 子 Options 槽位）+ `Options/InkwellProvidersOptions.cs`（[HD-001 §3.11.1](HD-001-Inkwell.Abstractions-foundation.md#3111-optionsinkwellprovidersoptionscsf9-新增) 已有 `VectorStore` 字段，默认值 `"InMemory"`，本 HD 锁定其取值白名单 `{"InMemory", "Qdrant"}`）。

## 1. 模块概述

### 1.1 职责

- **Type-alias 复用**（§3.3）：`Inkwell.Abstractions` 通过 `GlobalUsings.cs` 追加 `global using Microsoft.Extensions.VectorData;`，让 `VectorStore` / `VectorStoreCollection<TKey, TRecord>` / `[VectorStoreKey]` / `[VectorStoreData]` / `[VectorStoreVector]` 等类型在项目内直接可用，不重新声明任何包装类型。C# `using` 别名（`using Foo = Bar;`）不支持开放泛型（`VectorStoreCollection<TKey, TRecord>` 无法被别名化），故采用**命名空间级别** `global using` 而非逐类型别名（详 [§3.3 技术说明](#33-globalusingscs-追加行hd-001-既有文件)）。
- **`VectorStoreOptions`**（§3.1）：Provider 无关的向量存储配置——`EmbeddingModelName` / `EmbeddingDimensions` / `DistanceMetric` / `EnableSensitiveDataLogging`（[picker Q3=A，2026-07-06](#13-关键决策摘要)，字面值对齐 [risk-analysis.md RISK-016 缓解 #4](../../03-architecture/risk-analysis.md#risk-016-inmemoryvectorstore-与-qdrant-语义偏移--microsoftextensionsvectordata-上游变化)"H3 锁定 embedding 模型 = `text-embedding-3-large`（1536 维，cosine）"）
- **`VectorStoreOptionsValidator`**（§3.2）：`IValidateOptions<VectorStoreOptions>` 启动期校验
- **Builder DSL 签名声明**（§4）：`UseQdrantVectorStore` / `UseInMemoryVectorStore` / `UseAzureOpenAIEmbeddings` 三个 `IInkwellBuilder` 扩展方法的公共签名（不含实现），供各自落地 HD 遵循

### 1.2 范围

- **在内**：`VectorStoreOptions` + Validator + `GlobalUsings.cs` 追加行 + 3 个 Builder DSL 扩展方法的签名声明（非实现）+ `InkwellOptions.VectorStore` 子槽位 + `InkwellProvidersOptions.VectorStore` 取值白名单锁定
- **不在内**：
  - `UseQdrantVectorStore(...)` 具体实现 + `QdrantVectorStoreOptions`（Host / Port / ApiKey / UseHttps）——`providers/Inkwell.VectorStore.Qdrant/` 独立 HD 起草（[picker Q2=A，2026-07-06](#13-关键决策摘要)）
  - `UseInMemoryVectorStore(...)` 具体实现——`Inkwell.Core/VectorStore/` 独立 HD 起草
  - `UseAzureOpenAIEmbeddings(...)` 具体实现 + `AzureOpenAIEmbeddingOptions`（Endpoint / ApiKey / DeploymentName）——`Inkwell.Core/VectorStore/` 独立 HD 起草（[picker Q1=A，2026-07-06](#13-关键决策摘要)，与 [HD-006 `AzureOpenAIAgentRuntimeOptions`](HD-006-Inkwell.Abstractions-agent-runtime-port.md) 单实现拓扑一致）
  - KB / Memory 的 collection 命名约定、chunking、retention、TTL、多租户 payload 字段（[ADR-020 §接口粒度](../../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) 明确留业务 HD）
  - `IEmbeddingGenerator<string, Embedding<float>>` 在 KB / Memory 业务命名空间的具体注入与调用代码（业务 HD 起草；本 HD 仅锁定 DI 注册的 Builder DSL 签名）
  - `tests/core/Inkwell.Providers.Contract` 的 vector matrix 用例（[RISK-016 缓解 #1](../../03-architecture/risk-analysis.md#risk-016-inmemoryvectorstore-与-qdrant-语义偏移--microsoftextensionsvectordata-上游变化)，H4 [TestCaseAuthor] 起草）

> 本 HD 不贡献 `database-design.md`（向量数据落 Qdrant / InMemory，非关系表，[ADR-020](../../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) 已锁定）。

### 1.3 关键决策摘要

> 全部由 2026-07-06 picker 拍板，决策证据见本节"出处"列；详细候选与放弃理由见 [§13 决策记录](#13-决策记录)。

| ID                     | 决策                                                                                                                                                                                                                                | 出处                                                                                                                                                                                                                                                                                                                                                                    |
| ---------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Q1-embedding-topology  | `UseAzureOpenAIEmbeddings(...)` 实现 + `AzureOpenAIEmbeddingOptions` 落在 `Inkwell.Core/VectorStore/`——单实现拓扑，不设独立 `providers/*` csproj                                                                                    | picker 2026-07-06；与 [HD-006 Q-implementation-topology](HD-006-Inkwell.Abstractions-agent-runtime-port.md) 同款                                                                                                                                                 |
| Q2-qdrant-options-loc  | `QdrantVectorStoreOptions`（Host / Port / ApiKey / UseHttps）落在 `providers/Inkwell.VectorStore.Qdrant/`——端口层 `VectorStoreOptions` 保持 Provider-agnostic                                                                       | picker 2026-07-06；与 [HD-005 Queue Provider 子 Options 拓扑](HD-005-Inkwell.Abstractions-queue-port.md) 同款                                                                                                                                                                                                                                                           |
| Q3-vectorstore-options | `VectorStoreOptions`：`EmbeddingModelName = "text-embedding-3-large"` / `EmbeddingDimensions = 1536` / `DistanceMetric = "CosineSimilarity"` / `EnableSensitiveDataLogging = false`                                                 | picker 2026-07-06；对齐 [`Microsoft.Extensions.VectorData.DistanceFunction.CosineSimilarity`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.vectordata.distancefunction) 字符串常量 + [risk-analysis.md RISK-016 缓解 #4](../../03-architecture/risk-analysis.md#risk-016-inmemoryvectorstore-与-qdrant-语义偏移--microsoftextensionsvectordata-上游变化) |
| Q4-registration-order  | `UseAzureOpenAIEmbeddings(...)` 与 `UseQdrantVectorStore(...)` / `UseInMemoryVectorStore(...)` 无强制调用顺序；DI 容器无先后依赖，二者注册不同服务类型（`IEmbeddingGenerator<,>` vs `VectorStore`），仅要求都在 `.Build()` 之前调用 | picker 2026-07-06                                                                                                                                                                                                                                                                                                                                                       |
| Q-provider-whitelist   | `InkwellProvidersOptions.VectorStore` 取值白名单 `{"InMemory", "Qdrant"}`（[HD-001 §3.11.1](HD-001-Inkwell.Abstractions-foundation.md#3111-optionsinkwellprovidersoptionscsf9-新增) 字段已存在，默认值 `"InMemory"`）               | 机械延续；[ADR-020 §决策 D3=B](../../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) v1 仅 Qdrant + InMemory 双 Provider，无第三方需要 picker                                                                                                                                                                                              |
| Q-global-using         | 采用命名空间级 `global using Microsoft.Extensions.VectorData;`（非逐类型 `using` 别名）——C# 不支持开放泛型 `using` 别名，`VectorStoreCollection<TKey, TRecord>` 无法被别名化                                                        | 机械延续（C# 语言限制，非架构选择）                                                                                                                                                                                                                                                                                                                                     |

> 决策**复议**：6 条决策若需重开，发起新 ADR（不在本 HD 内翻盘），同步发起 HD-008 修订——本 HD `status: draft` 由人工评审签字后翻 `reviewed`，签字之前任意一条决策都可在 reviewer 反馈中调回。

## 2. 文件结构

```text
src/core/Inkwell.Abstractions/
  GlobalUsings.cs                       # （HD-001 既有文件，本 HD 追加一行）+= global using Microsoft.Extensions.VectorData;
  VectorStore/
    VectorStoreOptions.cs               # record class，EmbeddingModelName/EmbeddingDimensions/DistanceMetric/EnableSensitiveDataLogging（新增子目录）
    VectorStoreOptionsValidator.cs      # IValidateOptions<VectorStoreOptions>
  Options/
    InkwellOptions.cs                   # （HD-001 既有文件，本 HD 追加字段）+= public VectorStoreOptions VectorStore { get; init; } = new();
```

**Builder DSL 签名声明**（本 HD 锁定签名，实现落在下列独立 HD）：

```text
src/core/Inkwell.Core/VectorStore/                          # 独立 HD（picker Q1=A）
  InMemoryVectorStoreBuilderExtensions.cs                    # UseInMemoryVectorStore(this IInkwellBuilder, Action<VectorStoreOptions>? = null)
  AzureOpenAIEmbeddingOptions.cs                             # Endpoint / ApiKey / DeploymentName（参照 HD-006 AzureOpenAIAgentRuntimeOptions 拓扑）
  AzureOpenAIEmbeddingBuilderExtensions.cs                   # UseAzureOpenAIEmbeddings(this IInkwellBuilder, Action<AzureOpenAIEmbeddingOptions>)

src/core/providers/Inkwell.VectorStore.Qdrant/               # 独立 HD（picker Q2=A）
  QdrantVectorStoreOptions.cs                                # Host / Port / ApiKey / UseHttps
  QdrantVectorStoreBuilderExtensions.cs                      # UseQdrantVectorStore(this IInkwellBuilder, Action<QdrantVectorStoreOptions>)
```

**文件计数**：HD-008 新增 2 个 `*.cs`（`VectorStore/` 2：`VectorStoreOptions.cs` + `VectorStoreOptionsValidator.cs`）；`GlobalUsings.cs` / `Options/InkwellOptions.cs` 为既有文件追加行，不计入新增文件数。Abstractions csproj 累计 11（HD-001）+ 8（HD-002 本体）+ 7（HD-003）+ 4（HD-004）+ 4（HD-005）+ 10（HD-006）+ 2（HD-008）= **46** 个 `*.cs` + 1 个 `.csproj`。

## 3. 程序文件设计（10 字段 × 2 文件 + 2 处既有文件追加）

### 3.1 `VectorStore/VectorStoreOptions.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                   |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 文件路径     | `src/core/Inkwell.Abstractions/VectorStore/VectorStoreOptions.cs`                                                                                                                                                                                                                                                                                      |
| 职责         | Provider 无关的向量存储配置；Qdrant / InMemory 两个 Provider 共用同一份 `VectorStoreOptions`，Provider 专属连接参数（Host / Port / ApiKey 等）不在本文件（[picker Q2=A](#13-关键决策摘要)）                                                                                                                                                            |
| 对外接口     | `public sealed class VectorStoreOptions { [Required] public string EmbeddingModelName { get; init; } = "text-embedding-3-large"; [Range(1, 8192)] public int EmbeddingDimensions { get; init; } = 1536; [Required] public string DistanceMetric { get; init; } = "CosineSimilarity"; public bool EnableSensitiveDataLogging { get; init; } = false; }` |
| 内部函数或类 | 无独立方法；四字段均为 `init` 属性                                                                                                                                                                                                                                                                                                                     |
| 输入数据     | 由 `appsettings.json` `"Inkwell:VectorStore"` 段绑定（`EmbeddingModelName` / `EmbeddingDimensions` / `DistanceMetric` / `EnableSensitiveDataLogging`）                                                                                                                                                                                                 |
| 输出数据     | `VectorStoreOptions` 实例（DI 通过 `IOptions<VectorStoreOptions>` 注入，供 `Inkwell.Core.KnowledgeBase` / `.Memory` 业务层与 `Inkwell.Core.VectorStore` / `providers/Inkwell.VectorStore.Qdrant` 装配代码读取）                                                                                                                                        |
| 依赖模块     | `System.ComponentModel.DataAnnotations`                                                                                                                                                                                                                                                                                                                |
| 错误处理     | 字段缺失 / `EmbeddingDimensions` 越界 / `DistanceMetric` 不在白名单 → `IValidateOptions` 校验失败抛 `OptionsValidationException`（外层 host catch 后转 `InkwellConfigurationException`，与 [HD-001 §3.11](HD-001-Inkwell.Abstractions-foundation.md#311-optionsinkwelloptionscs) 一致）                                                                |
| 日志要求     | DI 启动期 `IValidateOptions` 失败时由 host 打 fatal 日志；本文件自身不产生运行期日志（无运行期方法，[§1.2](#12-范围)）                                                                                                                                                                                                                                 |
| 测试要求     | `VectorStoreOptionsTests.cs`：默认值、`appsettings.json` 绑定、`EmbeddingDimensions` 越界（`< 1` / `> 8192`）、`DistanceMetric` 非法值校验失败                                                                                                                                                                                                         |

### 3.2 `VectorStore/VectorStoreOptionsValidator.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                   |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/VectorStore/VectorStoreOptionsValidator.cs`                                                                                                                                                                                                                                                             |
| 职责         | `IValidateOptions<VectorStoreOptions>` 实现，启动期校验 `VectorStoreOptions` 字段 + `DistanceMetric` 白名单                                                                                                                                                                                                                            |
| 对外接口     | `internal sealed class VectorStoreOptionsValidator : IValidateOptions<VectorStoreOptions> { public ValidateOptionsResult Validate(string? name, VectorStoreOptions options); }`                                                                                                                                                        |
| 内部函数或类 | 内部走 `Validator.TryValidateObject`（DataAnnotations）+ 自定义规则：`DistanceMetric` 必须 ∈ `{"CosineSimilarity", "DotProductSimilarity", "EuclideanDistance"}`（[`Microsoft.Extensions.VectorData.DistanceFunction`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.vectordata.distancefunction) 已定义的字符串常量集） |
| 输入数据     | `VectorStoreOptions` 实例                                                                                                                                                                                                                                                                                                              |
| 输出数据     | `ValidateOptionsResult.Success` / `ValidateOptionsResult.Fail(IEnumerable<string>)`                                                                                                                                                                                                                                                    |
| 依赖模块     | `Microsoft.Extensions.Options` / `System.ComponentModel.DataAnnotations` / `Microsoft.Extensions.VectorData`（`DistanceFunction` 常量引用）                                                                                                                                                                                            |
| 错误处理     | 校验失败 → `ValidateOptionsResult.Fail` 含全部错误消息（不抛异常，由 `IOptions` 链路转），与 [HD-001 §3.12](HD-001-Inkwell.Abstractions-foundation.md#312-optionsinkwelloptionsvalidatorcs) 同款                                                                                                                                       |
| 日志要求     | 失败消息会被 `OptionsValidationException` 抛出，host 兜底打 fatal                                                                                                                                                                                                                                                                      |
| 测试要求     | `VectorStoreOptionsValidatorTests.cs`：成功路径、`EmbeddingDimensions` 越界、`DistanceMetric` 非白名单值、`EmbeddingModelName` 空                                                                                                                                                                                                      |

### 3.3 `GlobalUsings.cs` 追加行（HD-001 既有文件）

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                |
| ------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/GlobalUsings.cs`（既有文件，HD-001 §2 已创建）                                                                                                                                                                                                                                                                                                                                                       |
| 职责         | 追加一行 `global using Microsoft.Extensions.VectorData;`，使 `VectorStore` / `VectorStoreCollection<TKey, TRecord>` / `[VectorStoreKey]` / `[VectorStoreData]` / `[VectorStoreVector]` 等类型在 `Inkwell.Abstractions` 项目内所有文件直接可用，不需逐文件写 `using` 语句                                                                                                                                                            |
| 对外接口     | 无新增公共类型；仅 `global using` 指令                                                                                                                                                                                                                                                                                                                                                                                              |
| 内部函数或类 | 无。技术说明：C# `using` 别名（`using Foo = Bar;`）**不支持开放泛型**——`VectorStoreCollection<TKey, TRecord>` 无法写成 `global using InkwellVectorCollection<TKey, TRecord> = Microsoft.Extensions.VectorData.VectorStoreCollection<TKey, TRecord>;`（编译错误）。因此本 HD 采用命名空间级 `global using`，而非 ADR-020 图示注释字面暗示的逐类型别名（[picker Q-global-using](#13-关键决策摘要)，机械延续 C# 语言限制，非架构选择） |
| 输入数据     | 无                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| 输出数据     | 无                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| 依赖模块     | `Microsoft.Extensions.VectorData.Abstractions`（[HD-001 §2](HD-001-Inkwell.Abstractions-foundation.md#2-文件结构) 已在 csproj 依赖白名单中标注"HD-008 起用"）                                                                                                                                                                                                                                                                       |
| 错误处理     | 不适用（编译期指令，无运行期行为）                                                                                                                                                                                                                                                                                                                                                                                                  |
| 日志要求     | 不适用                                                                                                                                                                                                                                                                                                                                                                                                                              |
| 测试要求     | 不适用（无独立单测；由 `VectorStoreOptionsTests.cs` 等消费方间接验证类型可解析）                                                                                                                                                                                                                                                                                                                                                    |

### 3.4 `Options/InkwellOptions.cs` 追加字段（HD-001 既有文件）

| 字段         | 内容                                                                                                                                                                                                                 |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Options/InkwellOptions.cs`（既有文件，HD-001 §3.11 已创建）                                                                                                                           |
| 职责         | 在根 `InkwellOptions` 追加 `VectorStore` 子 Options 槽位，与 `Persistence` / `FileStorage` / `Cache` / `Queue` / `AgentRuntime` 五个既有槽位同款（HD-002 ~ HD-006 依次追加的同一模式，本 HD 是第六次追加） |
| 对外接口     | 追加字段：`public VectorStoreOptions VectorStore { get; init; } = new();`（插入在 `AgentRuntimeOptions AgentRuntime` 之后）                                                                                                  |
| 内部函数或类 | 无新增类；`InkwellOptions` 类体追加一个属性                                                                                                                                                                          |
| 输入数据     | 由 `appsettings.json` `"Inkwell:VectorStore"` 段绑定                                                                                                                                                                 |
| 输出数据     | `InkwellOptions.VectorStore` 属性                                                                                                                                                                                    |
| 依赖模块     | `VectorStore/VectorStoreOptions.cs`（[§3.1](#31-vectorstorevectorstoreoptionscs)）                                                                                                                                   |
| 错误处理     | 字段缺失 → `IValidateOptions` 校验失败（详 [§3.1](#31-vectorstorevectorstoreoptionscs)）                                                                                                                             |
| 日志要求     | 与 [HD-001 §3.11](HD-001-Inkwell.Abstractions-foundation.md#311-optionsinkwelloptionscs) 一致，DI 启动期失败由 host 打 fatal                                                                                         |
| 测试要求     | `InkwellOptionsTests.cs`（[HD-001 §3.11](HD-001-Inkwell.Abstractions-foundation.md#311-optionsinkwelloptionscs) 既有测试文件）追加一条断言：`Providers` 段绑定后 `VectorStore` 子 Options 可正常解析默认值           |

## 4. Builder DSL 签名声明（不含实现）

> 本节仅锁定 3 个 `IInkwellBuilder` 扩展方法的**公共签名**；实现代码分别落在 [`Inkwell.Core/VectorStore/`](#12-范围)（`UseInMemoryVectorStore` / `UseAzureOpenAIEmbeddings`，[picker Q1=A](#13-关键决策摘要)）与 [`providers/Inkwell.VectorStore.Qdrant/`](#12-范围)（`UseQdrantVectorStore`，[picker Q2=A](#13-关键决策摘要)）独立 HD。

```csharp
// src/core/Inkwell.Core/VectorStore/InMemoryVectorStoreBuilderExtensions.cs（独立 HD）
public static IInkwellBuilder UseInMemoryVectorStore(
    this IInkwellBuilder builder,
    Action<VectorStoreOptions>? configure = null);

// src/core/Inkwell.Core/VectorStore/AzureOpenAIEmbeddingBuilderExtensions.cs（独立 HD）
public static IInkwellBuilder UseAzureOpenAIEmbeddings(
    this IInkwellBuilder builder,
    Action<AzureOpenAIEmbeddingOptions> configure);

// src/core/providers/Inkwell.VectorStore.Qdrant/QdrantVectorStoreBuilderExtensions.cs（独立 HD）
public static IInkwellBuilder UseQdrantVectorStore(
    this IInkwellBuilder builder,
    Action<QdrantVectorStoreOptions> configure);
```

装配示例（[ADR-020 §Builder DSL 形状](../../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) 落地，[picker Q4=A 无强制注册顺序](#13-关键决策摘要)）：

```csharp
// prod
builder.Services.AddInkwell()
    .UseSqlServer(...)
    .UseAzureBlob(...)
    .UseRedis(...)
    .UseRedisQueue(...)
    .UseAzureOpenAIEmbeddings(opts =>
    {
        opts.Endpoint = builder.Configuration["Inkwell:VectorStore:AzureOpenAI:Endpoint"]!;
        opts.ApiKey = builder.Configuration["Inkwell:VectorStore:AzureOpenAI:ApiKey"]!;
        opts.DeploymentName = "text-embedding-3-large";
    })
    .UseQdrantVectorStore(opts =>
    {
        opts.Host = builder.Configuration["Inkwell:VectorStore:Qdrant:Host"]!;
        opts.Port = 6334;
        opts.UseHttps = true;
    })
    .Build();

// dev / unit test
builder.Services.AddInkwell()
    .UsePostgres(builder.Configuration.GetConnectionString("Postgres")!) // Testcontainers 真实实例，2026-07-08 移除 InMemory 关系型 Provider
    .UseLocalFileSystem(...)
    .UseInMemoryCache()
    .UseChannelsQueue()
    .UseAzureOpenAIEmbeddings(opts => { /* 同 prod，embedding 生成仍需真实 Azure OpenAI 调用 */ })
    .UseInMemoryVectorStore()
    .Build();
```

> `UseAzureOpenAIEmbeddings` 在 dev / unit test 场景**仍需真实 Azure OpenAI 调用**（embedding 生成无 in-process mock，[RISK-016 残余风险 #1](../../03-architecture/risk-analysis.md#risk-016-inmemoryvectorstore-与-qdrant-语义偏移--microsoftextensionsvectordata-上游变化) 已隐含此约束）；unit test 层面 mock `IEmbeddingGenerator<string, Embedding<float>>` 接口本身（业务 HD 起草测试时处理），本 HD 不提供 in-process embedding 生成器替代品。

## 5. `InkwellProvidersOptions.VectorStore` 取值白名单

[HD-001 §3.11.1](HD-001-Inkwell.Abstractions-foundation.md#3111-optionsinkwellprovidersoptionscsf9-新增) 已声明 `InkwellProvidersOptions.VectorStore` 字段（默认值 `"InMemory"`）。本 HD 锁定其取值白名单：

- `"InMemory"` → 对应 `.UseInMemoryVectorStore()`（dev / unit test 默认）
- `"Qdrant"` → 对应 `.UseQdrantVectorStore(...)`（integration test / prod）

装配期校验规则与 [HD-002 §Provider 一致性校验](HD-002-Inkwell.Abstractions-persistence-port.md) 同款：`Providers.VectorStore` 取值与实际调用的 `.UseXxxVectorStore()` 不一致时，`Build()` 抛 `InkwellBuilderException($"Provider registration conflict for VectorStore: configured={Providers.VectorStore}, registered=...")`。

## 6. 与 `IEmbeddingGenerator<TInput, TEmbedding>` 的衔接

[ADR-020 §Embedding 生成](../../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) 锁定 embedding 生成通过 [`Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding>`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.ai.iembeddinggenerator-2) 完成（[Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai) 官方抽象，非 MAF 专属类型，与 [ADR-003 MAF](../../03-architecture/adr/ADR-003-agent-engine-microsoft-agent-framework.md) 同生态但包名独立）。`UseAzureOpenAIEmbeddings(...)` 在 DI 容器注册 `IEmbeddingGenerator<string, Embedding<float>>` 单例（具体注册代码属 [picker Q1=A](#13-关键决策摘要) 落地的 `Inkwell.Core/VectorStore/` 独立 HD）；`Inkwell.Core.KnowledgeBase` / `.Memory` 业务层通过标准 DI 注入该接口生成向量后写入 `VectorStoreCollection<TKey, TRecord>`。

**本 HD 不新增任何包裹该接口的 Inkwell 门面类型**（如 `IEmbeddingProducer`）——ADR-020 字面表述是业务层直接消费 `IEmbeddingGenerator<,>`，本 HD 遵此表述。`Microsoft.Extensions.AI.Abstractions` 已对称纳入 `Inkwell.Abstractions.csproj` 依赖白名单 + `GlobalUsings.cs` 追加 `global using Microsoft.Extensions.AI;`（[HD-001 §13 2026-07-06 errata·第六轮](HD-001-Inkwell.Abstractions-foundation.md#2026-07-06-errata第六轮b15-对称纳入-microsoftextensionsaiabstractions-白名单)），与 `Microsoft.Extensions.VectorData.Abstractions`（[§1.1](#11-职责) / [ADR-020](../../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md)）的处理完全同构，不再是模糊类比而是已落地的对称机制：业务命名空间通过依赖 `Inkwell.Abstractions`（符合 [AGENTS.md §3.2](../../../AGENTS.md) 依赖纯度规则）间接获得 `IEmbeddingGenerator<,>` 的编译期可见性，业务代码本身无需新增任何 `PackageReference`，不会被 CI `BannedSymbols.txt` 拦下（[§13 Q5](#13-决策记录)）。该白名单扩展局限于 `Inkwell.Abstractions.csproj` 本身的 `PackageReference` 清单，**不**改动 [AGENTS.md §3.2](../../../AGENTS.md) 依赖白名单原文——`AGENTS.md` 本来就未字面列举 csproj 级具体包名单（那是 `Inkwell.Abstractions` 自身设计的范畴），因此无需另行发起 ADR / `AGENTS.md` 修订。

## 7. 测试策略

- `VectorStoreOptionsTests.cs` / `VectorStoreOptionsValidatorTests.cs`（[§3.1](#31-vectorstorevectorstoreoptionscs) / [§3.2](#32-vectorstorevectorstoreoptionsvalidatorcs)）：本 HD 范围内的唯一运行期可测对象是 Options 绑定与校验；无运行期方法（[§1.2](#12-范围)），故无契约测试 / 无性能基准（与 HD-002 ~ HD-006 均定义了 facade 方法不同）
- `tests/core/Inkwell.Providers.Contract/VectorStore/` vector matrix 用例（InMemory / Qdrant 双 Provider 同一套用例，[RISK-016 缓解 #1](../../03-architecture/risk-analysis.md#risk-016-inmemoryvectorstore-与-qdrant-语义偏移--microsoftextensionsvectordata-上游变化)）：不在本 HD 范围，[H4 TestCaseAuthor] 起草时联动 KB / Memory 业务 HD

## 8. appsettings.json 示例

```json
{
  "Inkwell": {
    "Providers": {
      "VectorStore": "Qdrant"
    },
    "VectorStore": {
      "EmbeddingModelName": "text-embedding-3-large",
      "EmbeddingDimensions": 1536,
      "DistanceMetric": "CosineSimilarity",
      "EnableSensitiveDataLogging": false
    }
  }
}
```

> Qdrant / Azure OpenAI 连接参数（`Inkwell:VectorStore:Qdrant:*` / `Inkwell:VectorStore:AzureOpenAI:*`）由各自落地 HD 锁定字段名，不在本 HD 示例中展开（[picker Q1/Q2=A](#13-关键决策摘要)）。

## 9. 部署 / 安全说明

- Qdrant `ApiKey` / Azure OpenAI `ApiKey` 均走 K8s Secret（prod）/ Docker Compose `.env`（dev），与 [AGENTS.md §2.5](../../../AGENTS.md) "v1 不引入 Azure Key Vault"一致——本 HD 不新增凭据存储机制
- `VectorStoreOptions.EnableSensitiveDataLogging` 默认 `false`；启用时业务层查询文本 / embedding 向量本身仍**不得**进 OTel（与 [HD-006 §7.2 PII 提示](HD-006-Inkwell.Abstractions-agent-runtime-port.md) 同款约束，具体日志字段由业务 HD 落地）
- 具体监控指标（如 embedding 生成延迟、Qdrant 查询延迟、collection 大小）随 `Inkwell.Core/VectorStore/` 与 `providers/Inkwell.VectorStore.Qdrant/` 独立 HD 落地；OTel instrumentation 基线由 [`Microsoft.Extensions.VectorData` 内置 `ActivitySource`](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-vector-data) 提供（[ADR-020 §迁移路径 step 9](../../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md)）
- Qdrant collection 创建 / InMemory 无持久化等具体部署步骤，移交 `providers/Inkwell.VectorStore.Qdrant/` 与 `Inkwell.Core/VectorStore/` 独立 HD

## 10. 已知限制

- `VectorStoreOptions.EmbeddingDimensions` / `EmbeddingModelName` 一旦投产后变更，需要重建 Qdrant collection（[risk-analysis.md RISK-016 缓解 #4](../../03-architecture/risk-analysis.md#risk-016-inmemoryvectorstore-与-qdrant-语义偏移--microsoftextensionsvectordata-上游变化)"如未来换型号需同时以 ADR 记录并重建 collection"）——本 HD 不提供在线 re-embedding / collection 迁移工具
- InMemory 与 Qdrant 的语义子集差异（hybrid search / geo filter 等）不在本 HD 缓解范围，详 [RISK-016](../../03-architecture/risk-analysis.md#risk-016-inmemoryvectorstore-与-qdrant-语义偏移--microsoftextensionsvectordata-上游变化)

> **性能边界**：不适用（本 HD 无运行期方法，参见 [§1.2 范围声明](#12-范围)）。

## 11. 待补 / 待评审

> 原留的 2 条"需要 Owner 确认"开放问题已由 Owner 在 chat picker（2026-07-06）确认拍板，详见 [§13 Q5](#13-决策记录) / [§13 Q6](#13-决策记录)，不再是待评审项。

- **待补**：`tests/core/Inkwell.Providers.Contract/VectorStore/` vector matrix 用例的具体断言集（留 H4 TestCaseAuthor 起草）

## 12. 追溯

- [REQ-009 知识库](../../01-requirements/requirements.md) / [REQ-010 长期记忆](../../01-requirements/requirements.md)：向量检索能力的需求来源
- [ADR-003 Microsoft Agent Framework](../../03-architecture/adr/ADR-003-agent-engine-microsoft-agent-framework.md)：与 M.E.VectorData / M.E.AI 同生态的技术选型背景
- [ADR-017 Ports & Adapters](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md)：端口层零外部包依赖规则（本 HD 遵此规则，仅使用已白名单的 `Microsoft.Extensions.VectorData.Abstractions` 与 `Microsoft.Extensions.AI.Abstractions`[2026-07-06 errata，详 HD-001 §13]）
- [ADR-020 向量存储抽象](../../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md)：本 HD 的直接上游决策
- [ADR-023 端口签名规约](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)：本 HD 无运行期方法，装配期失败沿用 `IValidateOptions` / `InkwellBuilderException`
- [HD-001 Foundation](HD-001-Inkwell.Abstractions-foundation.md)：`IInkwellBuilder` / `InkwellOptions` / `InkwellProvidersOptions` / csproj 白名单复用
- [HD-002 Persistence Port](HD-002-Inkwell.Abstractions-persistence-port.md)：Provider 一致性校验模式参照

## 13. 决策记录

| ID                                 | 候选                                                                                                                                                                                                                                                                                                                                                                                                                    | 拍板 | 放弃理由（非拍板项）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
| ---------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Q1-embedding-topology              | A：`Inkwell.Core/VectorStore/`；B：独立 `providers/Inkwell.Embeddings.AzureOpenAI/`；C：合并进 Qdrant provider                                                                                                                                                                                                                                                                                                          | A    | B 拓扑对称但 v1 无第二 embedding provider 需要切换，属超前设计；C 语义耦合，未来 InMemory+AzureEmbedding 组合会被打散                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| Q2-qdrant-options-loc              | A：`providers/Inkwell.VectorStore.Qdrant/`；B：直接扩展进 `Inkwell.Abstractions/VectorStore/VectorStoreOptions.cs`                                                                                                                                                                                                                                                                                                      | A    | B 违反 [ADR-017 §依赖规则第 4 条](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) 端口零外部包约束 + 破坏 Provider-agnostic 原则                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| Q3-vectorstore-options             | A：锁 `text-embedding-3-large` / 1536 / `CosineSimilarity` / `false`；B：不锁默认模型名/维度，强制显式配置                                                                                                                                                                                                                                                                                                              | A    | B 增加部署配置负担，且 [risk-analysis.md RISK-016 缓解 #4](../../03-architecture/risk-analysis.md#risk-016-inmemoryvectorstore-与-qdrant-语义偏移--microsoftextensionsvectordata-上游变化) 已锁定该字面值，B 会与既有风险登记冲突                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| Q4-registration-order              | A：无强制顺序；B：强制 `UseAzureOpenAIEmbeddings` 先于 vector store 方法                                                                                                                                                                                                                                                                                                                                                | A    | B 增加使用者认知负担且无实际技术必要性（两者注册不同 DI 服务类型，互不依赖）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| Q5-embedding-generator-injection   | A：`IEmbeddingGenerator<string, Embedding<float>>` 允许 `Inkwell.Core.KnowledgeBase` / `.Memory` 业务命名空间直接注入，将 `Microsoft.Extensions.AI.Abstractions` 对称纳入 `Inkwell.Abstractions.csproj` 白名单 + `GlobalUsings.cs`，与 `Microsoft.Extensions.VectorData.Abstractions` 处理完全同构，不新增 Inkwell 门面接口；B：新增 `IEmbeddingProducer` 门面接口维持业务命名空间"只依赖 `Inkwell.Abstractions`"的纯度 | A    | Owner chat picker（2026-07-06）：与 `Microsoft.Extensions.VectorData.Abstractions` 对称处理，避免为语义等价的官方抽象重复造门面；另已因 [design-review-report.md §18 B15](../design-review-report.md#b15q5比照-vectordata-先例缺物理落地机制iembeddinggenerator-依赖白名单例外未实际生效c91) 发现物理落地机制缺失，Owner 二次 picker 拍板选项 1（对称纳入白名单，不改 AGENTS.md）完成物理落地（详 [HD-001 §13 2026-07-06 errata·第六轮](HD-001-Inkwell.Abstractions-foundation.md#2026-07-06-errata第六轮b15-对称纳入-microsoftextensionsaiabstractions-白名单)）；B 增加维护成本且偏离 [ADR-020](../../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) 现有字面表述 |
| Q6-embedding-credential-separation | A：`AzureOpenAIEmbeddingOptions`（本 HD / embedding）与 `AzureOpenAIAgentRuntimeOptions`（[HD-006](HD-006-Inkwell.Abstractions-agent-runtime-port.md) / chat）各自独立配置段，即使指向同一 Azure OpenAI 资源；B：共用同一份 Azure OpenAI 凭据配置                                                                                                                                                                       | A    | Owner chat picker（2026-07-06）：保留未来切换到不同 Azure OpenAI 资源承载 Chat 模型与 Embedding 模型的灵活性；B 会让两个独立端口的运维变更半径耦合在一起                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |

> 决策**复议**：6 条决策若需重开，发起新 ADR（不在本 HD 内翻盘），同步发起 HD-008 修订——本 HD `status: draft` 由人工评审签字后翻 `reviewed`，签字之前任意一条决策都可在 reviewer 反馈中调回。Q5 / Q6 系 Owner 在 chat picker（2026-07-06）中直接拍板，非本 Agent 起草时自拟候选，复议同样走上述流程。

---

**本次会话产生 / 修改的文件清单**：

- 新增：`docs/04-detailed-design/Inkwell.Abstractions/HD-008-Inkwell.Abstractions-vector-store-type-alias.md`
- 追加：`docs/04-detailed-design/file-structure.md`（新增 `## Inkwell.Abstractions.VectorStore` 章节 + 更新累计文件计数）
- 追加：[`HD-001 Foundation`](HD-001-Inkwell.Abstractions-foundation.md)（`Options/InkwellOptions.cs` 追加 `VectorStore` 字段说明 + `GlobalUsings.cs` 依赖白名单"HD-008 起用"标注已确认无需改动，§3.11 内部函数或类范围文字延伸至"HD-002 ~ HD-008"）

**推荐下一动作**：切到 `h3-detailed-design-reviewer` Agent 跑评审，`blocking` 为 0 后由人工把 `status: draft → reviewed` + `reviewers:` 添一行；原 [§11 两条开放问题](#11-待补--待评审) 已由 Owner 在 chat picker（2026-07-06）确认拍板，详见 [§13 Q5](#13-决策记录) / [§13 Q6](#13-决策记录)，不再阻塞评审。

**本次会话 picker 拍板字段清单**：Q1-embedding-topology / Q2-qdrant-options-loc / Q3-vectorstore-options / Q4-registration-order（详见 [§13 决策记录](#13-决策记录)）。
