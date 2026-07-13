---
id: HD-019
title: Inkwell.Core.Models 详细设计 — 多来源模型注册表（IModelRegistryService）
stage: H3
status: draft
reviewers: []
upstream:
  - REQ-005
  - REQ-006
  - ADR-003
  - ADR-017
  - ADR-023
  - ADR-026
  - HD-001
  - HD-006
  - HD-015
---

<!-- markdownlint-disable MD060 -->
<!-- 中文 + 英文混排长表格在 markdownlint 列宽计算下字面对齐 ≠ 视觉对齐（详 /memories/markdown-lint.md，与 HD-004 / HD-005 / HD-006 / HD-007 / HD-014 / HD-015 / HD-016 / HD-017 同处理方式），表格仍按 docs-style §3 视觉对齐维护，机械 MD060 不予执行。 -->

> **本 HD 是 H3 第六张业务命名空间（`Inkwell.Core.*`）详细设计**，紧接在已 reviewed 的 [HD-017 `Inkwell.Core.Conversations`](HD-017-Inkwell.Core.Conversations.md) 之后起草（HD-018 `Inkwell.Core.AuditLogs` 原为第五张，因 2026-07-09 审计日志功能整体取消已删除，本 HD 直接接续 HD-017）。
>
> **治理声明**：本文件全文不包含任何"已用 `vscode_askQuestions` 向 Owner 真实确认"的表述——本次起草会话未发起任何 `vscode_askQuestions` 交互。全部标注"作者判断"的条目均为作者基于现有证据链的判断；存在真实产品 / 技术含义分歧、无法从现有文档判定的问题，原样列入 [§7](#7-需要-owner-确认的问题)，不代答、不假装已确认。
>
> **2026-07-13 替代性 errata（ADR-026 LiteLLM 模型网关）**：下方原设计中的 Catalog 命名、`ModelProviderKind`、单一配置来源和逐厂商 Agent Runtime 描述，已被 [ADR-026](../../03-architecture/adr/ADR-026-model-gateway-litellm.md) 取代；旧章节保留为历史审计依据，不再代表当前契约。当前契约如下：
>
> - **公共门面**改为 `IModelRegistryService`，DTO 改为 `ModelDefinition`。Agent `ModelId` / `AgentModelParameters`、模型可用性和能力元数据继续保留。
> - **来源聚合**：`ConfigurationModelRegistrySource` 读取 `Inkwell:Models` 原生模型；`LiteLLMModelRegistrySource` 通过稳定的 `GET /v1/models` 自动发现当前凭据可调用的 LiteLLM `model_name`。聚合后 `Id` 按大小写不敏感全局唯一，跨来源重名直接失败。
> - **身份维度分离**：删除 `ModelProviderKind`；`ModelDefinition` 分别记录 Publisher、Family、`SourceId`、`RuntimeId` 与 `RemoteModelId`。配置来源不等于运行时连接，用户可看到 OpenAI / Alibaba 与 GPT / Qwen 身份。
> - **能力字段**为 `SupportsVision` / `SupportsTools` / `SupportsStructuredOutput` / `ContextWindowTokens?`。LiteLLM 自动发现但缺少 Publisher / Family / 能力元数据的模型仍返回给 UI，但 `IsAvailable=false`；补齐元数据并显式启用后才可选择。
> - **运行时边界**：`ModelRoutingAgentFactory` 通过 Registry 解析业务 `ModelId`，按 `RuntimeId` 选择 LiteLLM 或显式原生连接器，再由 MAF 执行模型调用；不存在负责聊天调用的 `ILLMService`，也不在 LiteLLM 故障时自动旁路到原生连接。
> - **装配与 API**：`AddModelRegistry()` 注册配置来源，`AddLiteLLMModelRegistrySource()` 同时注册 LiteLLM 发现来源和 MAF 运行时连接；`GET /api/models` / `GET /api/models/{modelId}` 向已认证客户端提供聚合模型定义。
> - **启动期校验**：`ConfigurationModelRegistryOptionsValidator` 与 `LiteLLMModelRegistryOptionsValidator` 均通过 `ValidateOnStart()` 执行；列表项 DataAnnotations 与大小写不敏感重复 ID 在进程开始服务前失败，不推迟到首次模型查询。
> - **文件结构**：Abstractions 使用 `IModelRegistryService.cs` / `ModelDefinition.cs` / `ConfigurationModelRegistryOptions*.cs`；Core 使用 `ModelRegistryService.cs`、`ConfigurationModelRegistrySource.cs`、`IModelRegistrySource.cs`、`Models/LiteLLM/*` 与 `AgentRuntime/ModelRoutingAgentFactory.cs`。下方 §0 以后出现的旧文件名与旧约束均由本 errata 覆盖。

## 0. 范围核实：模型是否为持久化实体？REQ-005 / REQ-006 归属边界

**核查结论（逐条附证据，非臆造）**：

1. **`requirements.md` §5.2"后端配置项"原文明确把"模型路由策略（默认模型、降级顺序）"与"各模型厂商的 API Key"并列列为"不在产品 UI 内配置，由后端配置文件管理"的条目**（[requirements.md 第 149~150 行](../../01-requirements/requirements.md)），与"数据库连接 / 对象存储"等基础设施配置同类归档——这是判定"模型清单 = 配置文件驱动、非数据库表"的最直接证据。
2. **`acceptance-criteria.md` AC-021 字面**："UI-004 模型下拉的所有选项与后端配置文件声明的模型集合一致；客户端代码中**不**硬编码任何模型名（可由 H4 通过'切换后端配置后客户端列表随之变化'用例验证）"（[acceptance-criteria.md 第 68 行](../../01-requirements/acceptance-criteria.md)）——验收标准本身把"配置驱动"钉死为可测试行为，且验证手段是"切换配置文件"而非"写数据库记录"。
3. **`database-design.md` 顶层"表清单"（H2 architecture.md §4 锁定的业务范围）不含任何 `models` / `model_definitions` 之类的行**（[database-design.md 表清单](../database-design.md)，逐行核对 `users`/`agents`/`agent_versions`/`skills`/`tools`/`knowledge_bases`/`kb_documents`/`kb_chunks`/`memory_items`/`triggers`/`orchestrations`/`orchestration_runs`/`conversations`/`messages`/`traces`/`agui_run_events`/`public_api_tokens`，具体张数以 database-design.md 最新版为准，无一张对应"模型"）——这不是本 HD 起草期才发现的疏漏，而是 H2 架构阶段从一开始就没有把模型清单当作持久化实体规划。
4. **已 reviewed 的 [HD-015 §1.3 Q4](../Inkwell.Core/HD-015-Inkwell.Core.Agents.md#13-关键决策摘要) 对 [HD-006 `AgentRunRequest.ModelId: string?`](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#32-agentruntimeagentrunrequestcs) 的转述**（2026-07-08 修正引用归属——该句原文出自 HD-015 而非 HD-006 本体）："对应后端配置文件中的模型标识符，非数据库主键"——与上述三点结论完全一致。
5. **已 reviewed 的 [HD-015 §1.3](HD-015-Inkwell.Core.Agents.md#13-关键决策摘要) Q4** 引用同一 HD-006 注释，并在 [HD-015 §1.2"不在内"](HD-015-Inkwell.Core.Agents.md#12-范围) 明确写："模型注册表本身（后端配置文件声明的模型清单、厂商路由、可用性校验，AC-020 ~ AC-022）不在本 HD——归 `Inkwell.Core.Models`（[AGENTS.md §3.1](../../../AGENTS.md) 已锁定的独立业务命名空间，未起草）。本 HD **不**校验 `ModelId` 是否为已注册的合法模型，该校验职责留给 `Inkwell.Core.Models`（或 `Inkwell.WebApi` 组合两者时）。"

**结论：模型清单 v1 是纯配置文件驱动，不是持久化实体，本 HD 不新建任何数据库表、不新建 `Persistence/Models/` 业务 Model、不新建具名 Repository。** 本 HD 的核心交付物是"从 `IOptions<ModelCatalogOptions>` 读取模型清单 + 供 UI 展示与 Agent 侧校验的只读查询服务"。这一结论有充分证据支持，**不**列入 §7"需要 Owner 确认的问题"。

**REQ-005 / REQ-006 归属边界（与 HD-006 / HD-015 已锁定范围对照）**：

- **REQ-006（模型参数配置：temperature / top_p / max_tokens）的存储与运行时载体已由 [HD-006 `AgentModelParameters`](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#36-agentruntimeagentmodelparameterscs) + [HD-015 `AgentDefinition.ModelParameters`](HD-015-Inkwell.Core.Agents.md#33-persistenceagentsagentdefinitioncs) 完整锁定**，本 HD 不重复定义、不新建同类 DTO。
- **本 HD 承担的是 REQ-005"模型注册表"本身**：模型清单（Id / DisplayName / Provider / 是否支持视觉 / 是否可用）的只读查询能力，供（未起草的）`Inkwell.WebApi` 在两处消费：(a) UI-004 下拉框展示（[ui-spec.md §4.3.3](../../01-requirements/ui-spec.md)）；(b) 创建 / 更新 Agent 时校验 `ModelId` 合法性（HD-015 §1.2 已声明委托给本 HD）。
- **"其他厂商专有参数"的 schema 声明**（[ui-spec.md §4.3.3](../../01-requirements/ui-spec.md)"由后端配置定义渲染"）**是否属于本 HD**存在真实缺口——`AgentModelParameters`（HD-006）是固定字段的 DTO，没有可扩展字段位。本 HD **不**擅自扩展已 reviewed 的 HD-006，该缺口原样列入 [§7 Q&A-A](#7-需要-owner-确认的问题)。
- **模型路由策略（默认模型、降级顺序，[requirements.md §5.2](../../01-requirements/requirements.md)）与 EX-002"模型厂商不可用/限流/超时后端按配置的降级策略处理"** 是否属于本 HD（静态声明降级顺序配置）还是完全属于未起草的 `Inkwell.Core.AgentRuntime`（运行时故障转移执行逻辑）——本 HD 判断**执行**逻辑必然在 `AgentRuntime`（因为只有它在实际调用模型时才能感知"厂商不可用"），但**是否需要在 Models 目录里声明"降级顺序"这份静态配置数据**存在真实分歧，列入 [§7 Q&A-B](#7-需要-owner-确认的问题)，本 HD 当前**不**包含降级顺序字段。
- **模型连接信息（Azure OpenAI Endpoint / Deployment Name / API Key 等）不在本 HD 范围**——作者判断：[requirements.md §5.2](../../01-requirements/requirements.md) 把"API Key"与"模型路由策略"分别列为两个独立配置条目，且[requirements.md §5 数据分类](../../01-requirements/requirements.md#5-数据分类)"API Key（模型厂商）仅存在于后端配置文件，不下发到客户端"的安全原则要求：本 HD 对外暴露的 `ModelSummary` DTO（供 UI 展示 / WebApi 校验消费）**绝不能**携带连接密钥。连接信息应归属未起草的 `Inkwell.Core.AgentRuntime` 自己的配置绑定（它才是实际发起模型调用、需要密钥的模块），与本 HD 的"目录元数据"是两个独立的配置段。该边界判断的技术合理性较强，但因 `AgentRuntime` 尚未起草、无法核实该模块是否已有相反假设，仍列入 [§7 Q&A-C](#7-需要-owner-确认的问题) 供 Owner 知悉。

**依赖规则遵循**（[AGENTS.md §3.2](../../../AGENTS.md)）：`Inkwell.Core.Models` 只依赖 `Inkwell.Abstractions` + BCL（含 `Microsoft.Extensions.Options` / `Microsoft.Extensions.DependencyInjection`）；**不** `using` 任何 Provider 包，**不** `using Microsoft.Agents.AI.*`；**不**依赖 `IPersistenceProvider`（本 HD 无持久化需求）。

**跨业务命名空间依赖边界（重要架构结论，非 Owner 拍板，直接依据 AGENTS.md 条文）**：[AGENTS.md §3.2](../../../AGENTS.md) 原文——"业务命名空间 → 端口层：`Inkwell.Core.*`（除 `Inkwell.Core.AgentRuntime`）业务命名空间只能依赖 `Inkwell.Abstractions` + 进程内 BCL"——这意味着 `Inkwell.Core.Agents`（HD-015）**不能**直接依赖 `Inkwell.Core.Models`（本 HD）来做 `ModelId` 校验，即使二者同属 `Inkwell.Core.csproj` 物理项目。因此 HD-015 §1.2 提出的"或 `Inkwell.WebApi` 组合两者"是唯一符合既有依赖规则的路径：`ModelId` 合法性校验必须发生在（未起草的）`Inkwell.WebApi` 层——由它先调用本 HD `IModelCatalogService.GetModelAsync(modelId)` 校验存在性 / 可用性，校验通过后再调用 `Inkwell.Core.Agents.IAgentService.CreateAgentAsync/UpdateAgentAsync`。本 HD 在 §1.2 / §7 中记录这一结论供未起草的 `Inkwell.WebApi` HD 直接引用，不代替该 HD 做接口设计。

## 1. 模块概述

### 1.1 职责

`Inkwell.Core.Models` 承担：

- **`IModelCatalogService` 唯一实现**（`ModelCatalogService`）：`ListModelsAsync`——从 `IOptions<ModelCatalogOptions>` 读取全部已配置模型（含不可用项，供 UI 灰显，[AC-022](../../01-requirements/acceptance-criteria.md)）；`GetModelAsync`——按 `modelId` 查询单个模型定义，供（未起草的）`Inkwell.WebApi` 校验 `ModelId` 合法性（[HD-015 §1.2](HD-015-Inkwell.Core.Agents.md#12-范围) 委托）
- **`ModelCatalogOptions` + Validator**：从 `appsettings.json` `"Inkwell:Models"` 段绑定模型清单；启动期校验无重复 `Id`、至少一项 `Provider=AzureOpenAI` 且 `IsAvailable=true`（[REQ-005](../../01-requirements/requirements.md)"v1 必须支持 Azure OpenAI" + [AC-020](../../01-requirements/acceptance-criteria.md)）

### 1.2 范围

**在内**：

| 类别    | 文件（`Inkwell.Abstractions/Models/`）    |
| ------- | ------------------------------------------ |
| 门面接口 | `IModelCatalogService.cs`                  |
| DTO     | `ModelSummary.cs`（含 `ModelProviderKind` 枚举） |
| Options | `ModelCatalogOptions.cs` + `ModelCatalogOptionsValidator.cs` |

| 类别    | 文件（`Inkwell.Core/Models/`）        |
| ------- | --------------------------------------- |
| 实现    | `ModelCatalogService.cs`                |
| DI 装配 | `ModelsBuilderExtensions.cs`            |

**不在内**（明确排除，逐条附去向）：

- **`AgentModelParameters`（temperature / top_p / max_tokens 存储与传输）**——已由 [HD-006](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#36-agentruntimeagentmodelparameterscs) + [HD-015](HD-015-Inkwell.Core.Agents.md#33-persistenceagentsagentdefinitioncs) 锁定，本 HD 不重复定义
- **"其他厂商专有参数"的可扩展 schema**——真实缺口，未决，见 [§0](#0-范围核实模型是否为持久化实体req-005--req-006-归属边界) + [§7 Q&A-A](#7-需要-owner-确认的问题)
- **模型路由降级顺序的具体执行逻辑（运行时故障转移）**——归未起草的 `Inkwell.Core.AgentRuntime`；本 HD 是否需要声明静态降级顺序配置见 [§7 Q&A-B](#7-需要-owner-确认的问题)
- **模型连接信息（Endpoint / Deployment Name / API Key）**——作者判断归未起草的 `Inkwell.Core.AgentRuntime` 自己的配置段，本 HD `ModelSummary` DTO 不携带任何连接密钥字段，见 [§7 Q&A-C](#7-需要-owner-确认的问题)
- **`ModelId` 合法性校验发生的具体位置（`Inkwell.WebApi` 组合调用）**——归未起草的 `Inkwell.WebApi` HD；本 HD 仅提供 `GetModelAsync` 供其调用，见 [§0 跨业务命名空间依赖边界](#0-范围核实模型是否为持久化实体req-005--req-006-归属边界)
- **`Microsoft.Agents.AI.OpenAI`/`.Anthropic` 等 MAF Provider 包的具体 `IChatClient` 装配**——归未起草的 `Inkwell.Core.AgentRuntime`（[ADR-017 §依赖规则第 3 条](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md)"唯一允许 `using Microsoft.Agents.AI.*` 的位置"），本 HD 全文不 `using` 该命名空间

### 1.3 关键决策摘要

> 以下全部为**作者判断，非 Owner 拍板**；有真实产品 / 技术含义分歧的条目已单独列入 [§7](#7-需要-owner-确认的问题)，不在此重复。

| #  | 决策                                                                                                                                  | 依据                                                                                                                                                                        |
| -- | --------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Q1 | `ModelId` 类型为 `string`（非 `Guid`），与 [HD-006 `AgentRunRequest.ModelId`](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#32-agentruntimeagentrunrequestcs) / [HD-015 `AgentDefinition.ModelId`](HD-015-Inkwell.Core.Agents.md#33-persistenceagentsagentdefinitioncs) 保持一致 | 三处类型必须一致才能免翻译层比较，[§0 证据 4/5](#0-范围核实模型是否为持久化实体req-005--req-006-归属边界)已核实两处均为 `string?`                                            |
| Q2 | `ListModelsAsync` 返回**全部**已配置模型（含 `IsAvailable=false` 的项），不做服务端过滤                                                | [ui-spec.md §4.5](../../01-requirements/ui-spec.md)"模型不可用：模型下拉项灰显"要求前端能看到不可用项并展示提示，若服务端过滤则前端拿不到该信息                            |
| Q3 | `ListModelsAsync`/`GetModelAsync` 不分页、不暴露 `Pagination` 参数                                                                    | 数据源是 `IOptions<ModelCatalogOptions>` 绑定的进程内内存列表（[§0](#0-范围核实模型是否为持久化实体req-005--req-006-归属边界) 已确认非数据库查询），规模由运维配置文件手工维护，量级为个位数到几十个模型条目，远低于 [HD-001 `Pagination.MaxPageSize=100`](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#34-commonpaginationcs) 门槛；引入分页参数对该场景是过度设计，不构成"一次性拉取超过 100 条"的反模式（本仓库 2026-07-08 多次踩坑的 `Pagination(1, 1000)` 反模式是"用大页码硬拉数据库全量"，与本 HD"内存列表整体返回"性质不同） |
| Q4 | `ModelProviderKind` 为封闭 `enum`（`AzureOpenAI`/`OpenAI`/`Anthropic`/`Qwen`/`Zhipu`/`Other`），非开放字符串                            | 字面枚举值直接取自 [REQ-005](../../01-requirements/requirements.md)"OpenAI / Claude / Qwen / 智谱 / 等"，`Anthropic` 对应 Claude（[ADR-003](../../03-architecture/adr/ADR-003-agent-engine-microsoft-agent-framework.md) `Microsoft.Agents.AI.Anthropic` 包命名一致）；`Other` 兜底未来新增厂商，新增厂商仍需要代码改动（`AgentRuntime` 侧新增 `IChatClient` 实现），枚举扩展成本与新增 Provider 本身成本同数量级，非阻塞 |
| Q5 | `IModelCatalogService`/`ModelCatalogService` 均用 `AddSingleton` 注册，不需要 `IServiceScopeFactory` 间接 scope                       | 无任何 Scoped 依赖（不依赖 `IPersistenceProvider`），仅依赖 `IOptions<ModelCatalogOptions>`（本身是 Singleton 兼容的只读快照），不存在 HD-010 B16 / HD-016 首轮 B-1 那类"Singleton 消费 Scoped"风险 |
| Q6 | `GetModelAsync` 找不到匹配 `modelId` 时抛 `KeyNotFoundException`，`ListModelsAsync` 恒不抛该类异常                                     | [ADR-023 errata·01 BCL 对照表](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流)"实体不存在 → `KeyNotFoundException`" |
| Q7 | `Get*Async` 命名而非 `Find*Async`                                                                                                      | [ADR-023 调用方语义约定](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)"`Get*Async` 实体不存在则抛异常，`Find*Async` 返回 `null`"——本 HD 场景是"校验合法性"，不存在则应视为错误而非静默 `null`，故用 `Get*` 而非 `Find*` |

### 1.4 与 HD-006 / HD-015 的边界声明（本 HD 不改动任何已 reviewed 文件）

- `Inkwell.Core.Models` **不**修改 [HD-006](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) 的 `AgentModelParameters` / `AgentRunRequest.ModelId` 任何字段
- `Inkwell.Core.Models` **不**修改 [HD-015](HD-015-Inkwell.Core.Agents.md) 的 `AgentDefinition.ModelId` / `AgentDefinition.ModelParameters` 任何字段，也**不**在 `Inkwell.Core.Agents` 内新增对本 HD 的直接依赖（[§0 跨业务命名空间依赖边界](#0-范围核实模型是否为持久化实体req-005--req-006-归属边界)已说明该校验应发生在 `Inkwell.WebApi`）
- 本 HD 与 HD-006 / HD-015 之间**没有**需要 errata 的接口不一致——三处 `ModelId: string?` 类型完全对齐，无需同步修改

## 2. 文件结构

```text
src/core/Inkwell.Abstractions/
  Models/                                    # 新增子目录（HD-019）
    IModelCatalogService.cs                  # 顶层业务门面（2 方法）
    ModelSummary.cs                          # 模型目录条目 DTO + ModelProviderKind 枚举
    ModelCatalogOptions.cs                   # 模型清单配置（含 ModelEntryOptions 嵌套 DTO）
    ModelCatalogOptionsValidator.cs          # IValidateOptions<ModelCatalogOptions>

src/core/Inkwell.Core/
  Models/                                    # 新增子目录（HD-019）
    ModelCatalogService.cs                   # 唯一 IModelCatalogService 实现
    ModelsBuilderExtensions.cs               # AddDefaultModelCatalog()
```

**文件计数**：`Models/`（Abstractions）新增 4 个 + `Models/`（Core）新增 2 个，合计 6 个；累计文件总数（HD-001~HD-017 + 本 HD-019，HD-018 已因 2026-07-09 审计日志功能取消删除）具体数值以各自 HD 文档最终版为准，不在此处逐一重算。

**无新增数据库表 / 无新增 Persistence 文件**——[§0](#0-范围核实模型是否为持久化实体req-005--req-006-归属边界) 已核实模型清单为配置驱动，`database-design.md` 顶层表清单不追加任何行。

## 3. 程序文件设计（10 字段 × 6 文件）

### 3.1 `Models/IModelCatalogService.cs`

| 字段         | 内容                                                                                                                                                                                                     |
| ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Models/IModelCatalogService.cs`                                                                                                                                            |
| 职责         | 模型目录顶层业务门面；供业务命名空间外部消费方（未起草的 `Inkwell.WebApi`）查询可选模型清单 / 校验单个模型合法性（[§1.1](#11-职责)）                                                                       |
| 对外接口     | `public interface IModelCatalogService { Task<IReadOnlyList<ModelSummary>> ListModelsAsync(CancellationToken ct = default); Task<ModelSummary> GetModelAsync(string modelId, CancellationToken ct = default); }` |
| 内部函数或类 | 接口本身，无默认实现                                                                                                                                                                                        |
| 输入数据     | `string modelId`（`GetModelAsync`）/ `CancellationToken ct`                                                                                                                                                |
| 输出数据     | `Task<IReadOnlyList<ModelSummary>>`（`ListModelsAsync`） / `Task<ModelSummary>`（`GetModelAsync`）                                                                                                        |
| 依赖模块     | `Models/ModelSummary.cs`（本 HD）                                                                                                                                                                          |
| 错误处理     | 接口层不声明异常契约本身；具体分类见 [§4](#4-bcl-异常与日志)                                                                                                                                              |
| 日志要求     | 接口不做日志；实现层写 OTel span（[§4.3](#43-otel-span--字段)）                                                                                                                                            |
| 测试要求     | 接口无独立单测（由 `ModelCatalogServiceTests.cs` 覆盖，[§3.5](#35-inkwellcoremodelsmodelcatalogservicecs)）                                                                                              |

### 3.2 `Models/ModelSummary.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                            |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Models/ModelSummary.cs`                                                                                                                                                                                                                                                          |
| 职责         | 模型目录条目投影 DTO（[REQ-005](../../01-requirements/requirements.md) UI 下拉展示 + Agent 侧 `ModelId` 校验共用同一形状，[§1.3 Q2](#13-关键决策摘要)）；同文件声明 `ModelProviderKind` 枚举（[§1.3 Q4](#13-关键决策摘要)）                                                                                    |
| 对外接口     | `public sealed record ModelSummary { public required string Id { get; init; } public required string DisplayName { get; init; } public required ModelProviderKind Provider { get; init; } public bool SupportsVision { get; init; } public bool IsAvailable { get; init; } } public enum ModelProviderKind { AzureOpenAI, OpenAI, Anthropic, Qwen, Zhipu, Other }` |
| 内部函数或类 | 无内部方法（纯数据 record + 枚举）                                                                                                                                                                                                                                                                              |
| 输入数据     | 由 `ModelCatalogService`（[§3.5](#35-inkwellcoremodelsmodelcatalogservicecs)）从 `ModelEntryOptions`（[§3.3](#33-modelsmodelcatalogoptionscs)）映射构造                                                                                                                                                        |
| 输出数据     | `ModelSummary` 实例                                                                                                                                                                                                                                                                                             |
| 依赖模块     | 无（纯 BCL 类型）                                                                                                                                                                                                                                                                                                |
| 错误处理     | Model 自身不做业务校验                                                                                                                                                                                                                                                                                          |
| 日志要求     | DTO 自身不做日志                                                                                                                                                                                                                                                                                                |
| 测试要求     | `ModelSummaryTests.cs`：(1) 全部字段可正常构造；(2) record equality；(3) `ModelProviderKind` 全部枚举值可正确赋值                                                                                                                                                                                              |

### 3.3 `Models/ModelCatalogOptions.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                            |
| ------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Models/ModelCatalogOptions.cs`                                                                                                                                                                                                                                                                     |
| 职责         | 模型清单配置；从 `appsettings.json` `"Inkwell:Models"` 段绑定；同文件声明 `ModelEntryOptions` 嵌套 DTO（每个模型条目的配置形状）                                                                                                                                                                                                  |
| 对外接口     | `public sealed class ModelCatalogOptions { [Required, MinLength(1)] public IReadOnlyList<ModelEntryOptions> Models { get; init; } = []; } public sealed class ModelEntryOptions { [Required, MinLength(1)] public required string Id { get; init; } [Required, MinLength(1)] public required string DisplayName { get; init; } public required ModelProviderKind Provider { get; init; } public bool SupportsVision { get; init; } public bool IsAvailable { get; init; } = true; }` |
| 内部函数或类 | DataAnnotations 校验（单字段级）；跨条目校验（去重 / 至少一项可用 Azure OpenAI）由 [§3.4](#34-modelsmodelcatalogoptionsvalidatorcs) 承担                                                                                                                                                                                          |
| 输入数据     | 由 `IConfiguration` 绑定                                                                                                                                                                                                                                                                                                         |
| 输出数据     | `ModelCatalogOptions` 实例（DI 通过 `IOptions<ModelCatalogOptions>` 注入）                                                                                                                                                                                                                                                       |
| 依赖模块     | `System.ComponentModel.DataAnnotations` / `Models/ModelSummary.cs`（复用 `ModelProviderKind`）                                                                                                                                                                                                                                   |
| 错误处理     | 单字段 DataAnnotations 校验失败 → `OptionsValidationException`；跨条目校验失败同样 → `OptionsValidationException`（由 [§3.4](#34-modelsmodelcatalogoptionsvalidatorcs) 触发）                                                                                                                                                    |
| 日志要求     | DI 启动期校验失败时输出 OTel `exception.type=Microsoft.Extensions.Options.OptionsValidationException`（[HD-001 §4.2](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#42-日志结构化字段)）                                                                                                                     |
| 测试要求     | `ModelCatalogOptionsTests.cs`：(1) `appsettings.json` 绑定；(2) `IsAvailable` 默认 `true`；(3) `[Required]`/`[MinLength]` 边界                                                                                                                                                                                                   |

### 3.4 `Models/ModelCatalogOptionsValidator.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Models/ModelCatalogOptionsValidator.cs`                                                                                                                                                                                                                                                                                                                                                                                    |
| 职责         | `IValidateOptions<ModelCatalogOptions>` 实现；除单字段 DataAnnotations 外，额外做 3 项跨条目校验：(1) `Models` 非空；(2) `Id`（大小写不敏感）不重复；(3) 至少一项 `Provider == AzureOpenAI && IsAvailable == true`（[REQ-005](../../01-requirements/requirements.md)"v1 必须支持 Azure OpenAI" + [AC-020](../../01-requirements/acceptance-criteria.md)）                                                                                          |
| 对外接口     | `internal sealed class ModelCatalogOptionsValidator : IValidateOptions<ModelCatalogOptions> { public ValidateOptionsResult Validate(string? name, ModelCatalogOptions options); }`                                                                                                                                                                                                                                                                    |
| 内部函数或类 | `Validate` 内部：(1) 对 `options` 本体与每个 `ModelEntryOptions` 元素分别执行 `Validator.TryValidateObject`（DataAnnotations，同 [HD-014 `AuthOptionsValidator`](HD-014-Inkwell.Core.Auth.md) 模式）；(2) `options.Models.GroupBy(m => m.Id, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1)` 非空则失败并列出重复 `Id`；(3) `!options.Models.Any(m => m.Provider == ModelProviderKind.AzureOpenAI && m.IsAvailable)` 则失败；全部失败原因合并进单次 `ValidateOptionsResult.Fail(IEnumerable<string>)` |
| 输入数据     | `ModelCatalogOptions` 实例                                                                                                                                                                                                                                                                                                                                                                                                                              |
| 输出数据     | `ValidateOptionsResult.Success` / `Fail(IEnumerable<string>)`                                                                                                                                                                                                                                                                                                                                                                                            |
| 依赖模块     | `Microsoft.Extensions.Options` / `System.ComponentModel.DataAnnotations` / `Models/ModelCatalogOptions.cs`                                                                                                                                                                                                                                                                                                                                              |
| 错误处理     | 同 [HD-001 §3.12](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)，校验失败 → `Fail` 含全部消息，由框架在首次访问 `IOptions<T>.Value` 时抛 `OptionsValidationException`                                                                                                                                                                                                                                                             |
| 日志要求     | 失败由 `OptionsValidationException` 抛出，host 打 fatal                                                                                                                                                                                                                                                                                                                                                                                                 |
| 测试要求     | `ModelCatalogOptionsValidatorTests.cs`：(1) 合法清单通过；(2) 空清单被拒；(3) 重复 `Id`（含大小写变体）被拒；(4) 无任何可用 Azure OpenAI 条目被拒；(5) 存在 Azure OpenAI 但 `IsAvailable=false` 时仍被拒（验证"可用性"而非仅"存在性"）                                                                                                                                                                                                                |

### 3.5 `Inkwell.Core/Models/ModelCatalogService.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                     |
| ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Core/Models/ModelCatalogService.cs`                                                                                                                                                                                                                                                                                                                                                                    |
| 职责         | `IModelCatalogService` 唯一实现；直接从 `IOptions<ModelCatalogOptions>.Value` 读取内存快照并投影为 `ModelSummary`（[§1.3 Q3](#13-关键决策摘要) 无分页 / 无持久化查询）                                                                                                                                                                                                                                                  |
| 对外接口     | `internal sealed class ModelCatalogService : IModelCatalogService { public ModelCatalogService(IOptions<ModelCatalogOptions> options); public Task<IReadOnlyList<ModelSummary>> ListModelsAsync(CancellationToken ct = default); public Task<ModelSummary> GetModelAsync(string modelId, CancellationToken ct = default); }`                                                                                          |
| 内部函数或类 | `private static ModelSummary MapToSummary(ModelEntryOptions entry) => new() { Id = entry.Id, DisplayName = entry.DisplayName, Provider = entry.Provider, SupportsVision = entry.SupportsVision, IsAvailable = entry.IsAvailable };`。`ListModelsAsync`：`ArgumentNullException.ThrowIfNull` 均不适用（无入参）；`ct.ThrowIfCancellationRequested()` 后 `Task.FromResult<IReadOnlyList<ModelSummary>>(options.Value.Models.Select(MapToSummary).ToList())`（同步计算，`Task.FromResult` 包装以满足接口异步签名，无实际 I/O）。`GetModelAsync`：`ArgumentException.ThrowIfNullOrEmpty(modelId)` → `ct.ThrowIfCancellationRequested()` → `var entry = options.Value.Models.FirstOrDefault(m => string.Equals(m.Id, modelId, StringComparison.OrdinalIgnoreCase));` → `entry is null` 则 `throw new KeyNotFoundException($"Model '{modelId}' is not registered in the catalog.")`（[§1.3 Q6](#13-关键决策摘要)）；否则 `Task.FromResult(MapToSummary(entry))` |
| 输入数据     | `string modelId`（`GetModelAsync`） / `CancellationToken ct`                                                                                                                                                                                                                                                                                                                                                            |
| 输出数据     | `Task<IReadOnlyList<ModelSummary>>`（`ListModelsAsync`） / `Task<ModelSummary>`（`GetModelAsync`）                                                                                                                                                                                                                                                                                                                      |
| 依赖模块     | `Inkwell.Abstractions.Models.{IModelCatalogService,ModelSummary,ModelCatalogOptions}` / `Microsoft.Extensions.Options`（BCL 生态包，非 Provider）                                                                                                                                                                                                                                                                        |
| 错误处理     | `modelId` 为 `null`/空 → `ArgumentException`；找不到匹配项 → `KeyNotFoundException`；取消 → `OperationCanceledException`（`ct.ThrowIfCancellationRequested()`）                                                                                                                                                                                                                                                       |
| 日志要求     | OTel span `model.list`（`ListModelsAsync`，字段 `model.catalog_count`） / `model.get`（`GetModelAsync`，字段 `model.id`；异常时补 5 个 `exception.*`，[HD-001 §4.2](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#42-日志结构化字段)）                                                                                                                                                              |
| 测试要求     | `ModelCatalogServiceTests.cs`：(1) `ListModelsAsync` 返回全部条目（含 `IsAvailable=false` 的项，不过滤）；(2) `GetModelAsync` 已知 `modelId`（含大小写变体）返回正确 `ModelSummary`；(3) `GetModelAsync` 未知 `modelId` 抛 `KeyNotFoundException`；(4) `modelId` 为 `null`/空字符串抛 `ArgumentException`；(5) 取消令牌触发 `OperationCanceledException`                                                              |

### 3.6 `Inkwell.Core/Models/ModelsBuilderExtensions.cs`

| 字段         | 内容                                                                                                                                                                                                                                                            |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Core/Models/ModelsBuilderExtensions.cs`                                                                                                                                                                                                       |
| 职责         | Builder DSL 扩展方法，装配 `IModelCatalogService` 及其 Options 校验                                                                                                                                                                                              |
| 对外接口     | `public static class ModelsBuilderExtensions { public static IInkwellBuilder AddDefaultModelCatalog(this IInkwellBuilder builder, Action<ModelCatalogOptions>? configure = null); }`                                                                          |
| 内部函数或类 | (1) `ArgumentNullException.ThrowIfNull(builder)`；(2) `builder.Services.AddSingleton<IModelCatalogService, ModelCatalogService>()`（[§1.3 Q5](#13-关键决策摘要) 无 Scoped 依赖）；(3) `builder.Services.AddSingleton<IValidateOptions<ModelCatalogOptions>, ModelCatalogOptionsValidator>()` + `builder.Services.Configure<ModelCatalogOptions>(builder.Configuration.GetSection("Inkwell:Models"))` + `if (configure is not null) builder.Services.Configure(configure);`；(4) 返回 `builder` |
| 输入数据     | `IInkwellBuilder builder` / `Action<ModelCatalogOptions>? configure`                                                                                                                                                                                            |
| 输出数据     | `IInkwellBuilder`（支持链式调用）                                                                                                                                                                                                                                |
| 依赖模块     | `Inkwell.Abstractions.Builder.IInkwellBuilder` / `Microsoft.Extensions.DependencyInjection` / `Microsoft.Extensions.Options`                                                                                                                                    |
| 错误处理     | `builder` 为 `null` → `ArgumentNullException`                                                                                                                                                                                                                   |
| 日志要求     | 无（DI 装配期不产生运行时日志）                                                                                                                                                                                                                                  |
| 测试要求     | `ModelsBuilderExtensionsTests.cs`：(1) 调用后 `IModelCatalogService` 可从 `IServiceProvider` 解析；(2) `configure` 回调生效；(3) `builder` 为 `null` 抛异常；(4) 未调用 `AddDefaultModelCatalog()` 时解析 `IModelCatalogService` 应失败（本 HD **不**承诺零配置默认，与 HD-007/HD-014 等单实现拓扑不同——模型清单必须显式配置才有意义，无"默认模型"可预置） |

## 4. BCL 异常与日志

### 4.1 错误码

延续全仓 [ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) 最终态规约——本 HD 不分配任何 `ErrorCodes` 常量，错误语义全部走 BCL 异常类型 + OTel `exception.*` 五字段。

### 4.2 BCL 异常分类

| 场景                              | 异常类型                    | 说明                                                                                     |
| --------------------------------- | --------------------------- | ----------------------------------------------------------------------------------------- |
| `GetModelAsync` 传入 `null`/空字符串 | `ArgumentException`         | 参数校验                                                                                 |
| `GetModelAsync` 找不到匹配 `modelId` | `KeyNotFoundException`      | [ADR-023 BCL 对照表](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)"实体不存在" |
| 调用取消                           | `OperationCanceledException` | 标准取消传播                                                                             |
| `ModelCatalogOptions` 校验失败    | `OptionsValidationException` | 框架标准行为（[Microsoft.Extensions.Options](https://learn.microsoft.com/dotnet/api/microsoft.extensions.options.optionsvalidationexception)） |

### 4.3 OTel span / 字段

| Span         | 触发点               | 字段                                                                 |
| ------------ | --------------------- | ---------------------------------------------------------------------- |
| `model.list` | `ListModelsAsync`     | `model.catalog_count`                                                 |
| `model.get`  | `GetModelAsync`       | `model.id`；异常时补 [HD-001 §4.2](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#42-日志结构化字段) 5 个 `exception.*` 标准字段 |

命名沿用既有业务命名空间"单数域名词前缀"惯例（`agent.*`/`auth.*`，[HD-016 §24 C-5](../design-review-report.md) 曾指出 `tools.*` 复数不一致，本 HD 用单数 `model.*` 避免同类问题）。

## 5. 跨模块章节贡献

本 HD 在以下跨模块文件中追加内容：

- `docs/04-detailed-design/file-structure.md` — 追加 `## Inkwell.Abstractions.Models` 新一级章节（`Models/` 4 文件 + `Inkwell.Core/Models/` 2 文件树 + 文件计数）
- `docs/04-detailed-design/database-design.md` — **不追加任何内容**（[§0](#0-范围核实模型是否为持久化实体req-005--req-006-归属边界) 已确认无新增数据库表）

> 跨模块章节追加由本 HD 起草后**立即同步**到对应文件（**只追加**不改其他模块章节）。

## 6. 决策记录

| #  | 字段                                                             | 选定值           |
| -- | ------------------------------------------------------------------ | ---------------- |
| Q1 | `ModelId` 类型 `string`                                            | 作者判断（[§1.3](#13-关键决策摘要)） |
| Q2 | `ListModelsAsync` 返回全部（含不可用）条目                        | 作者判断          |
| Q3 | 不引入 `Pagination` 参数                                          | 作者判断          |
| Q4 | `ModelProviderKind` 封闭枚举 6 值                                  | 作者判断          |
| Q5 | `AddSingleton` 注册，无 Scoped 依赖                                | 作者判断          |
| Q6 | `GetModelAsync` 未命中抛 `KeyNotFoundException`                    | 作者判断，依据 ADR-023 BCL 对照表 |
| Q7 | 方法命名用 `Get*` 而非 `Find*`                                     | 作者判断，依据 ADR-023 调用方语义约定 |

## 7. 需要 Owner 确认的问题

- **Q&A-A（已解决，2026-07-08）**：[ui-spec.md §4.3.3](../../01-requirements/ui-spec.md)"其他厂商专有参数：由后端配置定义渲染（v1 仅渲染配置中显式声明的参数；未声明的不出现）"要求模型目录能声明"额外参数 schema"，但已 reviewed 的 [HD-006 `AgentModelParameters`](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#36-agentruntimeagentmodelparameterscs) 是固定字段 DTO（仅 `Temperature`/`TopP`/`MaxTokens`），没有可扩展字段位。Owner 在本次会话中通过 `vscode_askQuestions` 真实确认选**方案 B——本 HD 现状维持**：不声明额外参数 schema，v1 UI 该区段暂不渲染任何厂商专有参数，留 v2 backlog。未修改 HD-006。
- **Q&A-B（已解决，2026-07-08）**：[requirements.md §5.2](../../01-requirements/requirements.md)"模型路由策略（默认模型、降级顺序）"是否需要本 HD 在 `ModelEntryOptions`/`ModelCatalogOptions` 里声明静态"降级顺序"字段，供未起草的 `Inkwell.Core.AgentRuntime` 在运行时读取并执行故障转移。Owner 在本次会话中通过 `vscode_askQuestions` 真实确认选**方案 A——保持现状，延后判断**：本 HD 不包含任何路由/降级字段，待未来 `Inkwell.Core.AgentRuntime` 起草时再判断是否需要回来给本 HD 发 errata。
- **Q&A-C（已解决，2026-07-08）**：本 HD 判断模型连接信息（Azure OpenAI Endpoint / Deployment Name / API Key）不属于本 HD 的 `ModelCatalogOptions`，理由见 [§0](#0-范围核实模型是否为持久化实体req-005--req-006-归属边界)。Owner 在本次会话中通过 `vscode_askQuestions` 真实确认**接受**这个边界判断：连接信息归未起草的 `Inkwell.Core.AgentRuntime` 自己的配置段，本 HD 不纳入。待未来 `AgentRuntime` 起草时再核实是否与其假设冲突。

## 8. CI 自检命令（grep 列表）

| 编号 | 检查项                                                                                                                   | 命令                                                                                                                                              |
| ---- | ------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| C1   | `Inkwell.Core.Models` 不 `using` 任何 Provider 包                                                                        | `rg -n -e 'using Microsoft\.EntityFrameworkCore' -e 'using StackExchange\.Redis' -e 'using Npgsql' -e 'using Minio' -e 'using Azure\.Storage' src/core/Inkwell.Core/Models/` 期望 0 行 |
| C2   | 不 `using Microsoft.Agents.AI.*`                                                                                          | `rg -n -e 'using Microsoft\.Agents\.AI' src/core/Inkwell.Core/Models/ src/core/Inkwell.Abstractions/Models/` 期望 0 行                              |
| C3   | 不依赖 `IPersistenceProvider`（本 HD 无持久化）                                                                          | `rg -n -e 'IPersistenceProvider' -e 'IUnitOfWork' src/core/Inkwell.Core/Models/ src/core/Inkwell.Abstractions/Models/` 期望 0 行                    |
| C4   | 不出现自建 `ErrorCodes`/`InkwellException`（[ADR-023 最终态](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)） | `rg -n -e 'ErrorCodes\.' -e 'new InkwellException' src/core/Inkwell.Core/Models/ src/core/Inkwell.Abstractions/Models/` 期望 0 行                   |
| C5   | 不出现 `Pagination` 参数（[§1.3 Q3](#13-关键决策摘要)）                                                                   | `rg -n -e 'Pagination' src/core/Inkwell.Core/Models/ src/core/Inkwell.Abstractions/Models/` 期望 0 行                                                |
| C6   | `Inkwell.Core.Agents` 不直接依赖 `Inkwell.Core.Models`（[§0 跨业务命名空间依赖边界](#0-范围核实模型是否为持久化实体req-005--req-006-归属边界)） | `rg -n -e 'using Inkwell\.Core\.Models' -e 'IModelCatalogService' src/core/Inkwell.Core/Agents/` 期望 0 行                                          |
