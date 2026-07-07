---
id: HD-016
title: Inkwell.Core.Tools 详细设计 — 工具目录元数据 / 绑定解析（不含执行编排）
stage: H3
status: draft
reviewers: []
upstream:
  - REQ-007
  - ADR-010
  - ADR-017
  - ADR-023
  - HD-001
  - HD-002
  - HD-006
  - HD-015
---

<!-- markdownlint-disable MD060 -->
<!-- 中文 + 英文混排长表格在 markdownlint 列宽计算下字面对齐 ≠ 视觉对齐（详 /memories/markdown-lint.md，与 HD-004 / HD-005 / HD-006 / HD-007 / HD-014 / HD-015 同处理方式），表格仍按 docs-style §3 视觉对齐维护，机械 MD060 不予执行。 -->

> **本 HD 是 H3 第三张业务命名空间（`Inkwell.Core.*`）详细设计**，紧接在已 reviewed 的 [HD-015 `Inkwell.Core.Agents`](HD-015-Inkwell.Core.Agents.md) 之后起草。起草优先级见下方"起草顺序说明"。
>
> **起草顺序说明**：本 HD 之所以在 H3 起草顺序上插队到 `.Models` / `.Skills` / `.KnowledgeBase` 等模块之前，是因为 [HD-015 §8 Q&A-2](HD-015-Inkwell.Core.Agents.md#8-需要-owner-确认的问题) 记录的 Owner 决议——本次由 prompt 直接告知这是已成立的既定结论，本文件不重新发起确认，仅据此安排起草顺序。
>
> **范围核实结论（非臆造，逐条附证据）**：
>
> - **REQ-007（Function Calling / 工具调用）在范围内，但仅"工具元数据目录管理 + Agent 侧工具绑定解析"两个子能力**——[requirements.md §5.1](../../01-requirements/requirements.md) 字面"后端注册工具，Agent 配置时勾选并传入参数；运行期由后端代理调用"；[acceptance-criteria.md AC-025](../../01-requirements/acceptance-criteria.md)~[AC-028](../../01-requirements/acceptance-criteria.md) 描述的用户可见行为均为"从既有工具列表中勾选 + 填参数 + 保存校验"，**没有任何一条**描述"用户 / 管理员创建 / 编辑 / 删除工具定义"的界面或流程；[ui-spec.md §4.3.4](../../01-requirements/ui-spec.md) 原文"列表展示**后端注册**的工具集合，每项可勾选/取消勾选挂到 Agent"+ [ui-spec.md 空态文案](../../01-requirements/ui-spec.md)"还没有挂载任何工具"+"去工具市场看看"链接（**指向工具列表，由后端注册**）——均指向"用户侧只读浏览 + 绑定"，不支持用户自建工具。本 HD 据此把"Tool 定义的 CRUD"范围收窄为：**只读目录查询（`ListAvailableToolsAsync` / `GetToolAsync`）+ 绑定参数校验（`ValidateToolBindingAsync`，对应 [AC-028](../../01-requirements/acceptance-criteria.md)）**；工具目录的写入（新增/禁用工具）v1 只通过 `InkwellSeeder`（[ADR-021](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) + [HD-009 §13.12](../Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) 已确立的硬编码 Seed 模式）完成，**不**暴露运行期管理 API——该范围收窄判断本身是否符合 Owner 真实预期，已列入 [§8 Q&A-A](#8-需要-owner-确认的问题)，不代答。
> - **REQ-007 中"运行期由后端代理调用"的执行编排部分不在本 HD**——该部分涉及 [Microsoft Agent Framework](../../../../../microsoft/agent-framework/) 的函数调用（function calling）协议本体（模型何时决定调用工具、`AgentToolCallRecord` / `AgentRunEvent.ToolCallRequested`/`ToolCallResult` 流式事件的产出、EX-003 失败态到对话上下文的注入），[AGENTS.md §3.2](../../../AGENTS.md) 已锁定 `Inkwell.Core.AgentRuntime` 是**唯一**允许 `using Microsoft.Agents.AI.*` 的命名空间，本 HD **不** `using` 该命名空间下任何 MAF 类型，详见 [§1.4 边界声明](#14-与-inkwellcoreagentruntime-的边界声明工具定义-vs-工具执行)。
> - **REQ-008（Skills）与本 HD 无关，`Inkwell.Core.Tools` ≠ `Inkwell.Core.Skills`**——[ADR-010](../../03-architecture/adr/ADR-010-skill-loading-static-only-v1.md)"负面"小节原文"想做'调用第三方 API'必须走 [REQ-007 工具调用](../../01-requirements/requirements.md) 路径，不是 Skill 路径"，明确区分二者；[AGENTS.md §3.3](../../../AGENTS.md) 锁定的"不实现 Skill Execution，不预留 `ISkillExecutor` 接口"这条禁区**仅约束 `Inkwell.Core.Skills`**（Skill 的 Discovery/Activation/Execution 静态加载模型），**不延伸约束 `Inkwell.Core.Tools`**——本 HD 的 `IToolExecutor`（§3.8，内部接口）是 Tools 域自己的执行委托抽象，与 ADR-010 禁止的"Skill 脚本执行"是两个不同层面的东西，不违反该禁区。
> - **REQ-005 / REQ-006 / REQ-015 / REQ-017 均不在本 HD 范围**——`ToolDefinition` 不持有 Agent 归属 / 版本 / 共享字段（这些字段属于已 reviewed 的 [HD-015 `AgentDefinition`](HD-015-Inkwell.Core.Agents.md)）。
>
> **依赖规则遵循**（[AGENTS.md §3.2](../../../AGENTS.md)）：`Inkwell.Core.Tools` 只依赖 `Inkwell.Abstractions` + BCL；**不** `using` 任何 Provider 包，**不** `using Microsoft.Agents.AI.*`；持久化经 `IPersistenceProvider.GetRepository<IToolRepository>()`（[HD-002 §13.3 Q1=A2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)）；本 HD 不写审计（[NFR-004](../../01-requirements/requirements.md) 未把"工具目录查询/绑定解析"列入审计事件清单，与 [HD-014](HD-014-Inkwell.Core.Auth.md) / [HD-015](HD-015-Inkwell.Core.Agents.md) 均有真实审计写入触点的场景不同，本条为作者判断，非 Owner 拍板，理由见 [§1.3 Q1](#13-关键决策摘要)）。
>
> **治理声明**：本文件全文不包含任何"已用 `vscode_askQuestions` 向 Owner 真实确认"的表述。除上方"起草顺序说明"引用的 [HD-015 §8 Q&A-2](HD-015-Inkwell.Core.Agents.md#8-需要-owner-确认的问题)（该决议记录于已 reviewed 的 HD-015，非本文件起草会话内发起）之外，本 HD 全部标注"作者判断"的条目均为作者基于现有证据链的判断；存在真实产品含义分歧、无法从现有文档判定的问题，原样列入 [§8](#8-需要-owner-确认的问题)，不代答、不假装已确认。
>
> **2026-07-07 更新（§8 三项已解决）**：Owner 在对话中直接逐条明确确认（非通过 `vscode_askQuestions` 工具弹窗）——Q&A-A 维持只读工具目录设计（不补运行期 CRUD API）、Q&A-B 维持"绑定参数优先"合并策略、Q&A-C 确认 v1 需要至少一个真实可用的内置工具。本轮据此新增 `CurrentDateTimeToolExecutor`（[§3.12](#312-inkwellcoretoolscurrentdatetimetoolexecutorcs)）作为 v1 唯一内置工具并补充对应 Seed 数据契约（[§6.1](#61-tools-表-seed-数据2026-07-07-新增)）；Q&A-A / Q&A-B 未改变既有设计，仅将确认状态由"未拍板"更新为"已确认"。frontmatter `status` 仍保持 `draft`（本轮为设计补充，非评审签字）。

## 1. 模块概述

### 1.1 职责

`Inkwell.Core.Tools` 承担：

- 工具目录元数据的只读查询（[REQ-007](../../01-requirements/requirements.md)"后端注册工具"部分，[AC-025](../../01-requirements/acceptance-criteria.md) 列表勾选场景）
- Agent 侧工具绑定参数的必填校验（[AC-028](../../01-requirements/acceptance-criteria.md)"缺少必填参数"保存时拒绝）
- 把 [HD-015 `AgentDefinition.ToolBindings`](HD-015-Inkwell.Core.Agents.md#31-persistenceagentsagentdefinitioncs)（`IReadOnlyList<AgentToolBinding>`）解析翻译为 [HD-006 `AgentRunRequest.Tools`](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#32-agentruntimeagentrunrequestcs) 需要的 `IReadOnlyList<AgentToolDefinition>` 形态（本 HD 的核心交付物，直接消费 [HD-015 §3.4"已知缺口"](HD-015-Inkwell.Core.Agents.md#34-agentsiagentinvocationservicecs)）
- 工具的具体执行委托（`Func<string, CancellationToken, Task<string>>` 函数体本身，如未来落地的计算器 / 日期时间等纯 BCL 逻辑）的内部注册与查找——**不**涉及 MAF 函数调用协议编排，详见 [§1.4](#14-与-inkwellcoreagentruntime-的边界声明工具定义-vs-工具执行)

`IToolCatalogService` / `IToolBindingResolver` 是本模块**业务对外接口**，落在 `Inkwell.Abstractions/Tools/`（[HD-001 §5.1](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#51-命名) `I<Module>Service` 命名约定；`IToolBindingResolver` 不完全是"顶层业务门面"形态，但同样是跨模块消费的业务端口，比照 [HD-015 `IAgentInvocationService`](HD-015-Inkwell.Core.Agents.md) 的"翻译 + 衔接"定位共置同目录，作者判断，非 Owner 拍板）。

### 1.2 范围

**在内**：

| 类别         | 文件（`Inkwell.Abstractions/`）                          |
| ------------ | --------------------------------------------------------- |
| 业务 Model   | `Persistence/Tools/ToolDefinition.cs`                     |
| 具名 Repository | `Persistence/Tools/IToolRepository.cs`                 |
| 业务对外接口 | `Tools/IToolCatalogService.cs` / `Tools/IToolBindingResolver.cs` |
| Options      | `Tools/ToolOptions.cs` + `Tools/ToolOptionsValidator.cs`  |

| 类别    | 文件（`Inkwell.Core/Tools/`）                                                                 |
| ------- | ---------------------------------------------------------------------------------------------- |
| 实现    | `ToolCatalogService.cs`（`IToolCatalogService` 唯一实现）/ `ToolBindingResolver.cs`（`IToolBindingResolver` 唯一实现） |
| 内部机制 | `IToolExecutor.cs`（内部执行委托接口，不暴露 Abstractions）/ `ToolExecutorRegistry.cs`（内部注册表） |
| DI 装配 | `ToolsBuilderExtensions.cs`（`UseDefaultToolService()`，风格对齐 [HD-015 `AgentBuilderExtensions.cs`](HD-015-Inkwell.Core.Agents.md)） |

**不在内**（逐条附去向）：

- Tool 目录的运行期管理 API（新增 / 编辑 / 禁用工具）——v1 只经 `InkwellSeeder` 硬编码 Seed，详见 [§1.3 Q2](#13-关键决策摘要) + [§8 Q&A-A](#8-需要-owner-确认的问题)（该范围判断本身待 Owner 确认）
- 具体内置工具的执行逻辑实现（如计算器 / 网页搜索的真实 `IToolExecutor` 落地类）——**2026-07-07 更新**：Owner 已确认 v1 需要至少一个真实可用的内置工具（[§8 Q&A-C](#8-需要-owner-确认的问题) 已解决），本 HD 已据此落地唯一内置工具 `CurrentDateTimeToolExecutor`（[§3.12](#312-inkwellcoretoolscurrentdatetimetoolexecutorcs)）；除该工具外，其余潜在内置工具（计算器 / 网页搜索等）仍不在本 HD 范围，留待后续独立任务按 Owner 实际需要清单补充
- MAF 函数调用协议编排（`ChatClientAgent` 何时决定调用工具、`AgentToolCallRecord` / `AgentRunEvent.ToolCallRequested`/`ToolCallResult` 产出、EX-003 失败态转换）——归 `Inkwell.Core.AgentRuntime`（[HD-006](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) 已锁定 DTO 形态），详见 [§1.4](#14-与-inkwellcoreagentruntime-的边界声明工具定义-vs-工具执行)
- Agent 侧 `AgentUpsertRequest.ToolBindings` 的接线校验触点（即"`Inkwell.WebApi` 在调用 `IAgentService.CreateAgentAsync`/`UpdateAgentAsync` 前循环调用本 HD `IToolCatalogService.ValidateToolBindingAsync`"这一编排步骤）——留给未起草的 `Inkwell.WebApi` HD；已 reviewed 的 [HD-015 `AgentService`](HD-015-Inkwell.Core.Agents.md#39-inkwellcoreagentsagentservicecs) 本身**不**校验 `ToolBinding`（[HD-015 顶部范围声明](HD-015-Inkwell.Core.Agents.md)原文"本 HD 不校验 `ToolBinding.ToolId` 是否存在、不校验必填参数是否齐全...归 `Inkwell.Core.Tools` 或 `Inkwell.WebApi` 组合层"），本 HD 不改写已 reviewed 的 HD-015 该结论
- Skill 相关的一切（Discovery / Activation / Execution）——归 `Inkwell.Core.Skills`（[ADR-010](../../03-architecture/adr/ADR-010-skill-loading-static-only-v1.md)，未起草）

### 1.3 关键决策摘要

> 以下全部为**作者判断，非 Owner 拍板**；有真实产品含义分歧的条目已单独列入 [§8](#8-需要-owner-确认的问题)，不在此重复。

| #  | 决策                                                                                        | 理由                                                                                                                                                                                                            |
| -- | ------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Q1 | 本 HD 不写审计日志（`IAuditLogger` 未注入任何实现）                                        | [NFR-004](../../01-requirements/requirements.md) 审计事件清单（登录/登出、Agent CRUD/共享/调用）未提及"工具目录查询"或"绑定解析"，本 HD 全部方法均为只读/纯函数式解析，不产生需要留痕的状态变更                |
| Q2 | Tool 目录 v1 只经 `InkwellSeeder` 写入，不提供运行期 Create/Update/Delete API               | [requirements.md](../../01-requirements/requirements.md) + [ui-spec.md §4.3.4](../../01-requirements/ui-spec.md) 均只描述"浏览 + 绑定"，未描述任何工具管理界面；`IToolRepository` 因此只声明 `AddTool`（供 Seed 使用）+ 只读方法，不声明 `UpdateTool`/`DeleteTool`（同 [HD-014 Q7](HD-014-Inkwell.Core.Auth.md#13-关键决策摘要)"不发明未被要求的能力"先例） |
| Q3 | `ToolDefinition` 不实现 `IHasOwner` / `IHasRowVersion`                                       | 工具目录是系统级只读目录（非用户私有资源），无并发写入场景（Seed 单次写入，无运行期 Update），不需要乐观并发字段；`IHasOwner` 语义（"谁拥有这条记录"）不适用于系统级目录                                          |
| Q4 | `ToolDefinition.ParametersJsonSchema` 校验只解析顶层 [JSON Schema](https://json-schema.org/) `"required"` 数组做存在性校验，不引入完整 JSON Schema 校验库 | [AC-028](../../01-requirements/acceptance-criteria.md) 验收范围仅"缺少必填参数"报错；[ui-spec.md §4.3.4](../../01-requirements/ui-spec.md)"字段类型由后端工具元数据决定（v1 仅支持 string/number/boolean/select 四类）"的类型级校验留给前端表单，不在本 HD 引入 [`NJsonSchema`](https://github.com/RicoSuter/NJsonSchema) 等新依赖（避免超出 [AGENTS.md §3.2](../../../AGENTS.md) 依赖白名单） |
| Q5 | `ToolBindingResolver.ResolveAsync` 遇到无法解析的绑定（`ToolId` 不存在 / 无匹配 `IToolExecutor`）时整体抛异常中断解析，不静默跳过 | 与 [HD-015](HD-015-Inkwell.Core.Agents.md) / [HD-014](HD-014-Inkwell.Core.Auth.md) 一贯的"配置错误应尽早显式失败，不吞错"风格一致（[HD-001 §4.4](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)）；若 Owner 认为"部分工具绑定损坏不应影响整个 Run"，需另行拍板（未发现现有 requirements/AC 支持"部分失败仍继续"的语义，故不预先做宽松处理） |
| Q6 | `IToolExecutor` 声明为 `Inkwell.Core.Tools` 内部接口（`internal`），不提升为 `Inkwell.Abstractions` 端口 | 当前仅 `Inkwell.Core.Tools` 自身消费（`ToolExecutorRegistry`/`ToolBindingResolver`），无其他业务命名空间需要注册工具执行器的证据；若未来出现跨模块注册需求（如 `Inkwell.Core.KnowledgeBase` 需要注册"知识库检索"工具），需将该接口提升至 Abstractions 并发起 errata，本 HD 不预先扩大端口面（[ADR-010](../../03-architecture/adr/ADR-010-skill-loading-static-only-v1.md)"不预留未落地接口"同类 YAGNI 理由） |

### 1.4 与 `Inkwell.Core.AgentRuntime` 的边界声明（工具定义 vs 工具执行）

[AGENTS.md §3.2](../../../AGENTS.md) 锁定 `Inkwell.Core.AgentRuntime` 是**唯一**允许 `using Microsoft.Agents.AI.*` 的命名空间。本 HD 严格遵守该边界，具体划分如下：

**`Inkwell.Core.Tools`（本 HD）负责**：

1. 工具目录元数据的存储与查询（`ToolDefinition` / `IToolRepository` / `IToolCatalogService`）——纯数据与查询逻辑，零 MAF 依赖
2. 工具绑定参数的必填校验（`IToolCatalogService.ValidateToolBindingAsync`）——纯 [JSON Schema](https://json-schema.org/) 字符串解析，零 MAF 依赖
3. `AgentToolBinding` → [HD-006 `AgentToolDefinition`](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#37-agentruntimeagenttooldefinitioncs--agenttoolcallrecordcs) 的**形态翻译**（`IToolBindingResolver.ResolveAsync`）——组装 `AgentToolDefinition` record 实例，`InvokeAsync` 委托体内部只做"合并静态绑定参数 + 转发给 `IToolExecutor`"这一层纯 BCL 逻辑，零 MAF 依赖
4. 具体工具的执行委托函数体本身（`IToolExecutor.InvokeAsync` 实现，如未来落地的计算器逻辑）——这是工具"做什么"的业务逻辑，与"MAF 如何在对话中调度它"无关，零 MAF 依赖

**`Inkwell.Core.AgentRuntime`（已由 [HD-006](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) 锁定接口形态，实现独立 HD 未起草）专属负责**：

1. MAF 函数调用协议本体——把 `AgentRunRequest.Tools`（`IReadOnlyList<AgentToolDefinition>`）转换为 MAF 期望的工具表示形式（如 [`Microsoft.Agents.AI`](../../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI/) 的 `AIFunction` / `AIFunctionFactory`），并注册进 `ChatClientAgent`
2. 决定模型何时触发工具调用、把模型生成的调用参数（`argumentsJson`）传给 `AgentToolDefinition.InvokeAsync` 委托、把返回值封装进 `AgentToolCallRecord`（非流式）或 `AgentRunEvent.ToolCallRequested`/`ToolCallResult`（流式）
3. `InvokeAsync` 委托执行期异常的捕获与转换（[HD-006 §3.7](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#37-agentruntimeagenttooldefinitioncs--agenttoolcallrecordcs)"工具执行期抛出的异常由实现层捕获并转换为 `AgentToolCallRecord.IsError = true`...不向上抛出中断整个 Run，工具失败按 [EX-003](../../01-requirements/requirements.md) 处理"——该"实现层"指 `Inkwell.Core.AgentRuntime`，本 HD 的 `IToolExecutor.InvokeAsync` 允许抛出异常，由 `Inkwell.Core.AgentRuntime` 侧统一捕获转换，本 HD 不重复实现该捕获逻辑）

**判定依据**：区分标准不是"是否与工具相关"，而是"是否触碰 MAF 类型/协议"——本 HD 全部代码路径可以脱离 [Microsoft Agent Framework](../../../../../microsoft/agent-framework/) 独立编译运行（`AgentToolDefinition.InvokeAsync` 只是一个 BCL `Func<string, CancellationToken, Task<string>>` 委托，对本 HD 而言只是"数据"，不是"协议"）；一旦涉及 MAF 如何驱动这个委托被调用，就越界进入 `Inkwell.Core.AgentRuntime` 的专属范围。

## 2. 文件结构

```text
src/core/Inkwell.Abstractions/
  Persistence/
    Tools/                                # 新增子目录（HD-016）
      ToolDefinition.cs                   # 业务 Model（工具目录元数据）
      IToolRepository.cs                  # 具名 Repository（4 方法：AddTool/GetTool/GetToolByName/ListTools）
  Tools/                                  # 新增子目录（HD-016）
    IToolCatalogService.cs                # 顶层业务门面（只读查询 + 绑定校验）
    IToolBindingResolver.cs               # AgentToolBinding → AgentToolDefinition 翻译
    ToolOptions.cs                        # MaxToolsPerAgent / EnableSensitiveDataLogging
    ToolOptionsValidator.cs               # IValidateOptions<ToolOptions>

src/core/Inkwell.Core/
  Tools/                                  # 新增子目录（HD-016）
    ToolCatalogService.cs                 # 唯一 IToolCatalogService 实现
    IToolExecutor.cs                      # 内部执行委托接口（不暴露 Abstractions，§1.3 Q6）
    ToolExecutorRegistry.cs               # 内部执行器注册表（聚合 IEnumerable<IToolExecutor>）
    ToolBindingResolver.cs                # 唯一 IToolBindingResolver 实现
    ToolsBuilderExtensions.cs             # UseDefaultToolService()
    CurrentDateTimeToolExecutor.cs        # v1 唯一内置工具：当前日期时间查询（2026-07-07 新增，§8 Q&A-C 已解决）
```

**文件计数**：`Persistence/Tools/` 新增 2 个 + `Tools/`（Abstractions）新增 4 个，合计 6 个；Abstractions csproj 累计 11（HD-001）+ 8（HD-002 本体）+ 7（HD-003）+ 4（HD-004）+ 4（HD-005）+ 10（HD-006）+ 7（HD-007）+ 2（HD-008）+ 7（HD-014）+ 8（HD-015）+ 6（HD-016）= **74** 个 `*.cs` + 1 个 `.csproj`。`Inkwell.Core.csproj` 在 `Tools/` 新增 6 个（2026-07-07 新增 `CurrentDateTimeToolExecutor.cs`，§8 Q&A-C 已解决），累计（HD-014 起）5（HD-014）+ 3（HD-015）+ 6（HD-016）= **14** 个 `*.cs` + 1 个 `.csproj`。

## 3. 程序文件设计（10 字段 × 12 文件）

### 3.1 `Persistence/Tools/ToolDefinition.cs`

| 字段         | 内容                                                                                                                                                                                                                                          |
| ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Persistence/Tools/ToolDefinition.cs`                                                                                                                                                                             |
| 职责         | 工具目录业务 Model；撞名降级 `ToolDefinition`（与运行时 `Tool` 语义区分，[HD-002 §4.1.2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 已知降级清单成员，命名沿用 [file-structure.md `Persistence/Tools/` 既有模板](../file-structure.md#inkwellabstractions)） |
| 对外接口     | `public sealed record class ToolDefinition : IHasTimestamps { public required Guid Id { get; init; } public required string Name { get; init; } public required string Description { get; init; } public required string ParametersJsonSchema { get; init; } public DateTimeOffset CreatedTime { get; init; } public DateTimeOffset UpdatedTime { get; init; } }` |
| 内部函数或类 | 无内部方法（纯数据 record）；不实现 `IHasOwner` / `IHasRowVersion`（[§1.3 Q3](#13-关键决策摘要)）                                                                                                                                              |
| 输入数据     | 由 `InkwellSeeder`（未来 errata 追加到 [HD-009](../Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)）硬编码构造（同 [HD-009 §13.12](../Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) 默认管理员账号 Seed 先例） |
| 输出数据     | `ToolDefinition` 实例                                                                                                                                                                                                                          |
| 依赖模块     | `Persistence/Mixins/IHasTimestamps.cs`（[HD-002](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)）                                                                                                                    |
| 错误处理     | Model 自身不做业务校验；`Name` 唯一性由 `IToolRepository.AddTool` 实现层校验（[§3.2](#32-persistencetoolsitoolrepositorycs) 错误处理）                                                                                                          |
| 日志要求     | Model 自身不做日志                                                                                                                                                                                                                              |
| 测试要求     | `ToolDefinitionTests.cs`：(1) 全部字段可正常构造；(2) record equality                                                                                                                                                                          |

### 3.2 `Persistence/Tools/IToolRepository.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                    |
| ------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Persistence/Tools/IToolRepository.cs`                                                                                                                                                                                                                                                                    |
| 职责         | 具名 Repository；继承 [HD-002 §3.2 `IRepository<TModel, TKey>`](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#32-persistenceirepositorycs) marker；4 个具名动词方法（`Add`/`Get`/`List`，不含 `Update`/`Delete`，[§1.3 Q2](#13-关键决策摘要)）                                                              |
| 对外接口     | `public interface IToolRepository : IRepository<ToolDefinition, Guid> { Task<ToolDefinition> AddTool(ToolDefinition tool, CancellationToken ct = default); Task<ToolDefinition> GetTool(Guid id, CancellationToken ct = default); Task<ToolDefinition> GetToolByName(string name, CancellationToken ct = default); Task<PagedResult<ToolDefinition>> ListTools(Pagination pagination, SortOrder sort, CancellationToken ct = default); }` |
| 内部函数或类 | 接口本身；实现由未来 `providers/Inkwell.Persistence.EFCore` errata 追加（同 [HD-014](HD-014-Inkwell.Core.Auth.md) / [HD-015](HD-015-Inkwell.Core.Agents.md) 遗留契约缺口处理方式，本 HD 不改写已 reviewed 的 [HD-009](../Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)）                                       |
| 输入数据     | `ToolDefinition` 实例（`AddTool`） / `Guid id`（`GetTool`） / `string name`（`GetToolByName`） / `Pagination`+`SortOrder`（`ListTools`）                                                                                                                                                                                                |
| 输出数据     | `Task<ToolDefinition>`（`AddTool`/`GetTool`/`GetToolByName`） / `Task<PagedResult<ToolDefinition>>`（`ListTools`）                                                                                                                                                                                                                      |
| 依赖模块     | `ToolDefinition.cs` / `Common/Pagination.cs` / `Common/SortOrder.cs` / `PagedResult.cs`（均 [HD-001](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) / [HD-002](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 已锁）                                                                    |
| 错误处理     | `GetTool`/`GetToolByName` 找不到 → `KeyNotFoundException`；`AddTool` `Name` 唯一约束冲突 → `InvalidOperationException`（message 前缀 `"Duplicate key: Name="`，同 [HD-014 `IUserRepository`](HD-014-Inkwell.Core.Auth.md#37-persistenceauthiuserrepositorycs) 惯例）；命令超时 → `TimeoutException`                                    |
| 日志要求     | 实现层（未来 errata）写 OTel span `db.repository.tool.<verb>`（同 [HD-002 §3.2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 既定模式）                                                                                                                                                                    |
| 测试要求     | `IToolRepositoryContractTests.cs`：契约测试（接口形态锁定）；行为测试留待 EFCore 实现 errata 追加时补齐                                                                                                                                                                                                                                |

### 3.3 `Tools/IToolCatalogService.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                    |
| ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Tools/IToolCatalogService.cs`                                                                                                                                                                                                                                                                            |
| 职责         | 顶层业务门面；工具目录只读查询（[REQ-007](../../01-requirements/requirements.md) / [AC-025](../../01-requirements/acceptance-criteria.md)）+ 绑定参数必填校验（[AC-028](../../01-requirements/acceptance-criteria.md)）；全部签名走裸 `Task<T>` + BCL 异常（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)） |
| 对外接口     | `public interface IToolCatalogService { Task<IReadOnlyList<ToolDefinition>> ListAvailableToolsAsync(CancellationToken ct = default); Task<ToolDefinition> GetToolAsync(Guid toolId, CancellationToken ct = default); Task ValidateToolBindingAsync(Guid toolId, string? parametersJson, CancellationToken ct = default); }`          |
| 内部函数或类 | 接口本身；实现由 `Inkwell.Core.Tools.ToolCatalogService` 提供（唯一实现）                                                                                                                                                                                                                                                               |
| 输入数据     | `Guid toolId` / `string? parametersJson`（`AgentToolBinding.ParametersJson` 原样传入，[HD-015 §3.1](HD-015-Inkwell.Core.Agents.md#31-persistenceagentsagentdefinitioncs)） / `CancellationToken`                                                                                                                                       |
| 输出数据     | `Task<IReadOnlyList<ToolDefinition>>`（`ListAvailableToolsAsync`） / `Task<ToolDefinition>`（`GetToolAsync`） / `Task`（`ValidateToolBindingAsync`）                                                                                                                                                                                   |
| 依赖模块     | `Persistence/Tools/ToolDefinition.cs`                                                                                                                                                                                                                                                                                                    |
| 错误处理     | `GetToolAsync`/`ValidateToolBindingAsync` 的 `toolId` 不存在 → `KeyNotFoundException`；`ValidateToolBindingAsync` 缺必填参数 → `ArgumentException`（message `"Tool '<name>' is missing required parameter: '<field>'"`，供 [`Inkwell.WebApi`](../../../AGENTS.md) 转换为 [ui-spec.md](../../01-requirements/ui-spec.md)"工具 <名称> 缺少必填参数：<字段>"文案，[AC-028](../../01-requirements/acceptance-criteria.md)）；取消 → `OperationCanceledException` |
| 日志要求     | 实现层写 OTel span `tools.<verb>`（`list_available` / `get` / `validate_binding`），字段 `tools.tool_id` / `tools.operation_outcome` + 5 个 `exception.*`（详 §4.3）                                                                                                                                                                    |
| 测试要求     | `tests/core/Inkwell.Abstractions.Tests/Tools/IToolCatalogServiceContractTests.cs`：契约测试（ABI 锁定）；行为测试由 `Inkwell.Core` 独立测试项目覆盖                                                                                                                                                                                    |

### 3.4 `Tools/IToolBindingResolver.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                    |
| ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Tools/IToolBindingResolver.cs`                                                                                                                                                                                                                                                                           |
| 职责         | 把 [HD-015 `AgentToolBinding`](HD-015-Inkwell.Core.Agents.md#31-persistenceagentsagentdefinitioncs) 列表解析翻译为 [HD-006 `AgentToolDefinition`](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#37-agentruntimeagenttooldefinitioncs--agenttoolcallrecordcs) 列表；本 HD 的核心交付物，消费方为 `Inkwell.Core.Agents.AgentInvocationService`（2026-07-07 errata，[HD-015 §3.4](HD-015-Inkwell.Core.Agents.md#34-agentsiagentinvocationservicecs)） |
| 对外接口     | `public interface IToolBindingResolver { Task<IReadOnlyList<AgentToolDefinition>> ResolveAsync(IReadOnlyList<AgentToolBinding> bindings, CancellationToken ct = default); }`                                                                                                                                                            |
| 内部函数或类 | 接口本身；实现（`Inkwell.Core.Tools.ToolBindingResolver`）内部：(1) `bindings` 为空集合 → 直接返回 `Array.Empty<AgentToolDefinition>()`；(2) 对每个 `binding` 经 `IPersistenceProvider.GetRepository<IToolRepository>().GetTool(binding.ToolId)` 取目录元数据；(3) 经内部 `ToolExecutorRegistry` 查找对应 `IToolExecutor`；(4) 组装 `AgentToolDefinition { Name = tool.Name, Description = tool.Description, ParametersJsonSchema = tool.ParametersJsonSchema, InvokeAsync = <合并委托，见 §3.10> }` |
| 输入数据     | `IReadOnlyList<AgentToolBinding> bindings`（[HD-015](HD-015-Inkwell.Core.Agents.md) 已锁定类型，`Guid ToolId` + `string? ParametersJson`）                                                                                                                                                                                              |
| 输出数据     | `Task<IReadOnlyList<AgentToolDefinition>>`（[HD-006](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) 已锁定 DTO）                                                                                                                                                                                            |
| 依赖模块     | `Persistence/Tools/IToolRepository.cs` / `Inkwell.Abstractions.AgentRuntime.AgentToolDefinition`（[HD-006](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md)） / `Inkwell.Abstractions.Persistence.Agents.AgentToolBinding`（[HD-015](HD-015-Inkwell.Core.Agents.md)）                                        |
| 错误处理     | `binding.ToolId` 不存在于目录 → `KeyNotFoundException`（不吞不包装，[§1.3 Q5](#13-关键决策摘要)）；无匹配 `IToolExecutor` → `KeyNotFoundException`（message `"No IToolExecutor registered for tool '<name>' (<id>)"`）；取消 → `OperationCanceledException`                                                                            |
| 日志要求     | OTel span `tools.resolve_bindings`，字段 `tools.binding_count` / `tools.operation_outcome`；`ParametersJson` 原文**不得**进 OTel（可能含业务敏感参数，同 [HD-006 §7.2](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) PII 提示）                                                                          |
| 测试要求     | `IToolBindingResolverContractTests.cs`：契约测试（ABI 锁定）；行为测试（含"空绑定列表返回空数组"、"未知 `ToolId` 抛 `KeyNotFoundException`"、"字段 1:1 映射正确性"、"`InvokeAsync` 合并逻辑"）在 `Inkwell.Core` 独立测试项目                                                                                                          |

### 3.5 `Tools/ToolOptions.cs`

| 字段         | 内容                                                                                                                                                                          |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Tools/ToolOptions.cs`                                                                                                                        |
| 职责         | Tools 模块详细配置；从 `appsettings.json` `"Inkwell:Tools"` 段绑定                                                                                                          |
| 对外接口     | `public sealed class ToolOptions { [Range(1, int.MaxValue)] public int? MaxToolsPerAgent { get; init; } public bool EnableSensitiveDataLogging { get; init; } = false; }`   |
| 内部函数或类 | DataAnnotations 校验；`MaxToolsPerAgent` 默认 `null`（不限，同 [HD-015 §1.3 Q8](HD-015-Inkwell.Core.Agents.md#13-关键决策摘要)`MaxAgentsPerOwner` 先例，作者判断）           |
| 输入数据     | 由 `IConfiguration` 绑定                                                                                                                                                     |
| 输出数据     | `ToolOptions` 实例（DI 通过 `IOptions<ToolOptions>` 注入）                                                                                                                   |
| 依赖模块     | `System.ComponentModel.DataAnnotations`                                                                                                                                     |
| 错误处理     | DataAnnotations 校验失败 → `OptionsValidationException`，host 兜底                                                                                                          |
| 日志要求     | DI 启动期 `IValidateOptions` 失败时输出 OTel `exception.type=Microsoft.Extensions.Options.OptionsValidationException`（[HD-001 §5.3](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)） |
| 测试要求     | `ToolOptionsTests.cs`：默认值（`null` / `false`）、`appsettings.json` 绑定、`MaxToolsPerAgent` 显式设置正值合法                                                             |

### 3.6 `Tools/ToolOptionsValidator.cs`

| 字段         | 内容                                                                                                                                                       |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Tools/ToolOptionsValidator.cs`                                                                                             |
| 职责         | `IValidateOptions<ToolOptions>` 实现；DataAnnotations 校验（无跨字段约束）                                                                                |
| 对外接口     | `internal sealed class ToolOptionsValidator : IValidateOptions<ToolOptions> { public ValidateOptionsResult Validate(string? name, ToolOptions options); }` |
| 内部函数或类 | `Validator.TryValidateObject` DataAnnotations                                                                                                             |
| 输入数据     | `ToolOptions` 实例                                                                                                                                        |
| 输出数据     | `ValidateOptionsResult.Success` / `Fail(IEnumerable<string>)`                                                                                             |
| 依赖模块     | `Microsoft.Extensions.Options` / `System.ComponentModel.DataAnnotations`                                                                                  |
| 错误处理     | 同 [HD-001 §3.12](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)，校验失败 → `Fail` 含全部消息                                        |
| 日志要求     | 失败由 `OptionsValidationException` 抛出，host 打 fatal                                                                                                   |
| 测试要求     | `ToolOptionsValidatorTests.cs`：(1) DataAnnotations 边界合格；(2) 默认值通过；(3) 各字段单独越界均被拒                                                     |

### 3.7 `Inkwell.Core/Tools/ToolCatalogService.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Core/Tools/ToolCatalogService.cs`                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| 职责         | `IToolCatalogService` 唯一实现；只读查询 + 绑定参数校验的完整业务逻辑                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| 对外接口     | `internal sealed class ToolCatalogService : IToolCatalogService { public ToolCatalogService(IPersistenceProvider persistence); /* 3 个接口方法实现 */ }`                                                                                                                                                                                                                                                                                                                                                     |
| 内部函数或类 | `private static IReadOnlyCollection<string> ExtractRequiredFields(string parametersJsonSchema)`：用 `System.Text.Json.Nodes.JsonNode` 解析 schema，读取顶层 `"required"` 数组（不存在则视为空集合，[§1.3 Q4](#13-关键决策摘要)）；`private static IReadOnlySet<string> ParseProvidedFieldNames(string? parametersJson)`：`parametersJson` 为 `null`/空 → 空集合；否则解析为 JSON 对象，返回值非 `null` 的属性名集合                                                                                     |
| 输入数据     | `IPersistenceProvider`（[HD-002](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)）                                                                                                                                                                                                                                                                                                                                                                                                    |
| 输出数据     | 见 §3.3 `IToolCatalogService` 各方法签名                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| 依赖模块     | `Inkwell.Abstractions.{Persistence.IPersistenceProvider,Persistence.Tools.IToolRepository,Tools.IToolCatalogService}` / `System.Text.Json.Nodes`（BCL）                                                                                                                                                                                                                                                                                                                                                       |
| 错误处理     | `ListAvailableToolsAsync`——`persistence.GetRepository<IToolRepository>().ListTools(new Pagination(1, 1000), ...)`（大页拉取后映射为 `IReadOnlyList<ToolDefinition>`，同 [HD-014 §1.3 Q8](HD-014-Inkwell.Core.Auth.md#13-关键决策摘要)"~100 用户软目标"式简化处理，理由：v1 工具目录预期规模远小于 1000）；`GetToolAsync`——`GetTool` 找不到 → `KeyNotFoundException` 原样上抛；`ValidateToolBindingAsync`——`GetTool` 找不到 → `KeyNotFoundException`；`ExtractRequiredFields` 结果中任一字段不在 `ParseProvidedFieldNames` 结果中 → `ArgumentException`（详 §3.3 错误处理） |
| 日志要求     | 见 §3.3 日志要求（OTel span `tools.<verb>`）                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| 测试要求     | `ToolCatalogServiceTests.cs`（`Inkwell.Core.Tests`）：(1) `ListAvailableToolsAsync`/`GetToolAsync` 成功路径；(2) `GetToolAsync` 未知 `toolId` 抛 `KeyNotFoundException`；(3) `ValidateToolBindingAsync` 缺必填参数抛 `ArgumentException`（覆盖"schema 无 `required` 字段"/"`parametersJson` 为 `null`"/"部分必填字段缺失"三种边界）；(4) 全部必填字段齐全时正常通过                                                                                                                                          |

### 3.8 `Inkwell.Core/Tools/IToolExecutor.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                    |
| ------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Core/Tools/IToolExecutor.cs`                                                                                                                                                                                                                                                          |
| 职责         | 内部执行委托接口；具体工具的执行逻辑实现契约（[§1.4](#14-与-inkwellcoreagentruntime-的边界声明工具定义-vs-工具执行)"工具做什么"层面，零 MAF 依赖）；本 HD 只定义接口，**不提供任何 v1 具体实现**（[§8 Q&A-C](#8-需要-owner-确认的问题)）                                                              |
| 对外接口     | `internal interface IToolExecutor { Guid ToolId { get; } Task<string> InvokeAsync(string argumentsJson, CancellationToken ct = default); }`                                                                                                                                                             |
| 内部函数或类 | 接口本身；`ToolId` 对应 `ToolDefinition.Id`，供 `ToolExecutorRegistry` 按 `Guid` 索引；未来落地的具体工具（如计算器）各自实现该接口，经 DI `AddScoped<IToolExecutor, XxxToolExecutor>()`（多重注册，[HD-001](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) `IEnumerable<T>` 消费惯例） |
| 输入数据     | `string argumentsJson`（模型生成的调用参数 JSON 文本，与 [HD-006 `AgentToolDefinition.InvokeAsync`](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) 同形态） / `CancellationToken`                                                                                          |
| 输出数据     | `Task<string>`（结果 JSON 文本）                                                                                                                                                                                                                                                                        |
| 依赖模块     | System.\*（BCL `Func<>`/`Task<>`）                                                                                                                                                                                                                                                                       |
| 错误处理     | 具体实现自行决定异常语义；异常由调用方（`Inkwell.Core.AgentRuntime`，经 `AgentToolDefinition.InvokeAsync` 委托间接调用）捕获转换（[§1.4](#14-与-inkwellcoreagentruntime-的边界声明工具定义-vs-工具执行)），本接口自身不规定异常类型                                                                     |
| 日志要求     | 具体实现自行决定；`argumentsJson`/返回值原文不得进 OTel（同 [HD-006 §7.2](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) PII 提示）                                                                                                                                        |
| 测试要求     | 接口本身无需契约测试（`internal`，非跨项目 ABI）；具体实现落地时各自补充单测（v1 无具体实现，暂无测试文件）                                                                                                                                                                                            |

### 3.9 `Inkwell.Core/Tools/ToolExecutorRegistry.cs`

| 字段         | 内容                                                                                                                                                                                                     |
| ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Core/Tools/ToolExecutorRegistry.cs`                                                                                                                                                    |
| 职责         | 内部执行器注册表；把 DI 收集的 `IEnumerable<IToolExecutor>` 聚合为按 `Guid ToolId` 索引的查找表，供 `ToolBindingResolver` 消费                                                                          |
| 对外接口     | `internal sealed class ToolExecutorRegistry { public ToolExecutorRegistry(IEnumerable<IToolExecutor> executors); public bool TryGetExecutor(Guid toolId, out IToolExecutor executor); }`               |
| 内部函数或类 | 构造函数内把 `executors` 按 `ToolId` 建 `Dictionary<Guid, IToolExecutor>`；`ToolId` 重复注册 → 构造期 `InvalidOperationException`（message 前缀 `"Duplicate IToolExecutor registration for tool id"`） |
| 输入数据     | `IEnumerable<IToolExecutor>`（DI 容器解析，[HD-001](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) 多重注册消费惯例）                                                              |
| 输出数据     | `bool`（`TryGetExecutor`）+ `out IToolExecutor`                                                                                                                                                          |
| 依赖模块     | `IToolExecutor.cs`（同文件夹）                                                                                                                                                                           |
| 错误处理     | 构造期重复 `ToolId` → `InvalidOperationException`（fail-fast，避免运行期歧义路由）；`TryGetExecutor` 未命中 → 返回 `false`，不抛异常（调用方 `ToolBindingResolver` 自行决定是否升级为 `KeyNotFoundException`，[§3.4](#34-toolsitoolbindingresolvercs)） |
| 日志要求     | 构造失败时输出 OTel `exception.type=System.InvalidOperationException`                                                                                                                                    |
| 测试要求     | `ToolExecutorRegistryTests.cs`：(1) 单一/多个 executor 正确注册与查找；(2) 未知 `toolId` 返回 `false`；(3) 重复 `ToolId` 构造期抛异常                                                                     |

### 3.10 `Inkwell.Core/Tools/ToolBindingResolver.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Core/Tools/ToolBindingResolver.cs`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| 职责         | `IToolBindingResolver` 唯一实现；`AgentToolBinding` → `AgentToolDefinition` 的翻译 + 执行委托合并逻辑（详 §3.4 内部逻辑描述）                                                                                                                                                                                                                                                                                                                                                                                                          |
| 对外接口     | `internal sealed class ToolBindingResolver : IToolBindingResolver { public ToolBindingResolver(IPersistenceProvider persistence, ToolExecutorRegistry executorRegistry); public Task<IReadOnlyList<AgentToolDefinition>> ResolveAsync(IReadOnlyList<AgentToolBinding> bindings, CancellationToken ct = default); }`                                                                                                                                                                                                                 |
| 内部函数或类 | `private static Func<string, CancellationToken, Task<string>> BuildInvokeDelegate(IToolExecutor executor, string? boundParametersJson)`：返回一个委托，调用时先执行 `private static string MergeParameters(string modelArgumentsJson, string? boundParametersJson)`——**合并规则（本 HD 默认实现，未获 Owner 拍板，见 [§8 Q&A-B](#8-需要-owner-确认的问题)）**：`boundParametersJson` 解析为 `JsonObject` 后，逐属性覆盖/追加进 `modelArgumentsJson` 解析出的 `JsonObject`（绑定的静态参数优先于模型运行时生成的同名参数），合并结果序列化回字符串后转发给 `executor.InvokeAsync(mergedJson, ct)` |
| 输入数据     | `IPersistenceProvider` / `ToolExecutorRegistry`（同文件夹）                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| 输出数据     | 见 §3.4                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| 依赖模块     | `Inkwell.Abstractions.{Persistence.IPersistenceProvider,Persistence.Tools.IToolRepository,Persistence.Agents.AgentToolBinding,AgentRuntime.AgentToolDefinition,Tools.IToolBindingResolver}` / `IToolExecutor.cs` / `ToolExecutorRegistry.cs`（同文件夹） / `System.Text.Json.Nodes`（BCL）                                                                                                                                                                                                                                          |
| 错误处理     | 见 §3.4（`binding.ToolId` 不存在 → `KeyNotFoundException`；`ToolExecutorRegistry.TryGetExecutor` 未命中 → `KeyNotFoundException`）                                                                                                                                                                                                                                                                                                                                                                                                    |
| 日志要求     | 见 §3.4                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| 测试要求     | `ToolBindingResolverTests.cs`（`Inkwell.Core.Tests`）：(1) 空绑定列表返回空数组；(2) 字段 1:1 映射正确性（`Name`/`Description`/`ParametersJsonSchema` 透传）；(3) 未知 `ToolId` 抛 `KeyNotFoundException`；(4) 无匹配 `IToolExecutor` 抛 `KeyNotFoundException`；(5) `MergeParameters` 合并正确性（绑定静态参数覆盖同名运行时参数、无重叠字段两者共存两种场景）                                                                                                                                                                     |

### 3.11 `Inkwell.Core/Tools/ToolsBuilderExtensions.cs`

| 字段         | 内容                                                                                                                                                                                                                       |
| ------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Core/Tools/ToolsBuilderExtensions.cs`                                                                                                                                                                   |
| 职责         | Builder DSL 扩展方法，注册 `IToolCatalogService` / `IToolBindingResolver` / `ToolExecutorRegistry` 默认实现（风格对齐 [HD-015 `AgentBuilderExtensions.UseDefaultAgentService()`](HD-015-Inkwell.Core.Agents.md)）        |
| 对外接口     | `public static class ToolsBuilderExtensions { public static IInkwellBuilder UseDefaultToolService(this IInkwellBuilder builder, Action<ToolOptions>? configure = null); }`                                             |
| 内部函数或类 | (1) 校验 `builder` 非 null；(2) `builder.Services.AddScoped<IToolCatalogService, ToolCatalogService>()` + `AddScoped<IToolBindingResolver, ToolBindingResolver>()` + `AddSingleton<ToolExecutorRegistry>()` + `AddScoped<IToolExecutor, CurrentDateTimeToolExecutor>()`（2026-07-07 新增，v1 唯一内置工具，[§8 Q&A-C](#8-需要-owner-确认的问题) 已解决）；(3) 注册 `IValidateOptions<ToolOptions>` + 绑定 `configure`；(4) 返回 `builder` |
| 输入数据     | `IInkwellBuilder builder` / `Action<ToolOptions>? configure`                                                                                                                                                             |
| 输出数据     | `IInkwellBuilder`（支持链式调用）                                                                                                                                                                                        |
| 依赖模块     | `Inkwell.Abstractions.Builder.IInkwellBuilder` / `Microsoft.Extensions.DependencyInjection`                                                                                                                             |
| 错误处理     | `builder` 为 `null` → `ArgumentNullException`                                                                                                                                                                            |
| 日志要求     | 无（DI 装配期不产生运行时日志）                                                                                                                                                                                          |
| 测试要求     | `ToolsBuilderExtensionsTests.cs`：(1) 调用后 `IToolCatalogService`/`IToolBindingResolver`/`ToolExecutorRegistry` 可从 `IServiceProvider` 解析；(2) `configure` 回调生效；(3) `builder` 为 `null` 抛异常；(4) `IServiceProvider` 可解析出至少一个 `IToolExecutor`（`CurrentDateTimeToolExecutor`，2026-07-07 新增） |

### 3.12 `Inkwell.Core/Tools/CurrentDateTimeToolExecutor.cs`

> **2026-07-07 新增**：回应 [§8 Q&A-C](#8-需要-owner-确认的问题)——Owner 在对话中直接明确确认 v1 需要至少一个真实可用的内置工具，确保 [REQ-007](../../01-requirements/requirements.md) 端到端场景能跑通；具体选型（当前日期时间查询）由本 HD 作者判断，约束满足："不依赖外部 API key / 不需要额外第三方服务 / 可在 InMemory、单元测试环境下真实跑通"。

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                     |
| ------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Core/Tools/CurrentDateTimeToolExecutor.cs`                                                                                                                                                                                                                                                                                                                                             |
| 职责         | v1 **唯一**内置 `IToolExecutor` 落地实现——查询当前日期时间，可选按 [IANA](https://www.iana.org/time-zones) / Windows 时区标识符转换；零外部依赖、零密钥配置，纯 BCL [`TimeProvider`](https://learn.microsoft.com/dotnet/standard/datetime/timeprovider-overview) + [`TimeZoneInfo`](https://learn.microsoft.com/dotnet/api/system.timezoneinfo)                                                     |
| 对外接口     | `internal sealed class CurrentDateTimeToolExecutor : IToolExecutor { internal static readonly Guid ToolId = Guid.Parse("00000000-0000-0000-0000-000000000101"); public CurrentDateTimeToolExecutor(TimeProvider timeProvider); Guid IToolExecutor.ToolId { get; } public Task<string> InvokeAsync(string argumentsJson, CancellationToken ct = default); }`                                          |
| 内部函数或类 | `private static string? TryReadTimeZoneId(string argumentsJson)`：`argumentsJson` 为空/空白 → `null`；否则用 `System.Text.Json.Nodes.JsonNode.Parse` 解析，读取顶层 `"timeZoneId"` 字符串属性（不存在 → `null`）                                                                                                                                                                                        |
| 输入数据     | `string argumentsJson`（模型生成的调用参数 JSON，形态 `{"timeZoneId"?: string}`，`timeZoneId` 可选，缺省 → `UTC`）                                                                                                                                                                                                                                                                                       |
| 输出数据     | `Task<string>`（JSON 文本，形态 `{"utc": "<ISO 8601>", "timeZoneId": "<已解析时区 id>", "localTime": "<ISO 8601，已转换时区的本地时间>"}`）                                                                                                                                                                                                                                                              |
| 依赖模块     | `IToolExecutor.cs`（同文件夹） / `System.TimeProvider`（BCL，同 [HD-009 §3.3 `AuditingSaveChangesInterceptor`](../Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md#33-interceptorsauditingsavechangesinterceptorcs) / [HD-014 `AuthService`](HD-014-Inkwell.Core.Auth.md) 已建立的时钟注入惯例，便于单测用 [`FakeTimeProvider`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.time.testing.faketimeprovider) mock 固定时间点） / `System.TimeZoneInfo`（BCL） / `System.Text.Json.Nodes`（BCL）                                                                                                                                                                                                                                                          |
| 错误处理     | `argumentsJson` 非法 JSON → `JsonException`（BCL，原样上抛）；`timeZoneId` 未知/无法识别 → `TimeZoneInfo.FindSystemTimeZoneById` 原生抛出的 `TimeZoneNotFoundException` / `InvalidTimeZoneException`（BCL，原样上抛）；三者均由 `Inkwell.Core.AgentRuntime` 侧统一捕获转换为 `AgentToolCallRecord.IsError = true`（[§1.4](#14-与-inkwellcoreagentruntime-的边界声明工具定义-vs-工具执行)），本类自身不捕获                                                                                                                        |
| 日志要求     | 本类自身不写日志/OTel span（无状态纯函数式执行，交由 `Inkwell.Core.AgentRuntime` 侧统一记录工具调用 span）；`argumentsJson`/返回值原文不得进 OTel（同 [§3.8](#38-inkwellcoretoolsitoolexecutorcs) PII 提示）                                                                                                                                                                                             |
| 测试要求     | `CurrentDateTimeToolExecutorTests.cs`（`Inkwell.Core.Tests`）：(1) 无 `timeZoneId` 参数（空字符串 / `null` / `"{}"`）均返回 `timeZoneId="UTC"` 且 `utc == localTime`；(2) 指定合法 IANA/Windows `timeZoneId`（用 `FakeTimeProvider` 固定 `GetUtcNow()` 返回值断言转换后的 `localTime` 正确）；(3) 未知 `timeZoneId` 抛 `TimeZoneNotFoundException`；(4) `argumentsJson` 非法 JSON 抛 `JsonException`；(5) `ToolId` 字面量值与 [§6.1 Seed 数据](#61-tools-表-seed-数据2026-07-07-新增) 的 `Id` 字面量一致（回归测试，防止两侧硬编码字面量漂移） |

## 4. BCL 异常与日志（补充 HD-001 §4 / HD-002 §4 / HD-006 §4 / HD-015 §4）

### 4.1 错误码

本模块**不分配** `INK-TOOLS-NNN` 错误码，与全仓其余 HD 一致（[ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流)），全部错误语义走 BCL 异常类型 + OTel `exception.*` 五字段。

### 4.2 BCL 异常分类（业务失败 vs 程序错误）

- **业务失败 / 预期错误**：`KeyNotFoundException`（工具不存在 / 无匹配 `IToolExecutor`）/ `ArgumentException`（`ValidateToolBindingAsync` 缺必填参数）/ `InvalidOperationException`（`AddTool` 唯一约束冲突、`ToolExecutorRegistry` 构造期重复注册）/ `TimeZoneNotFoundException`、`InvalidTimeZoneException`（`CurrentDateTimeToolExecutor` 未知 `timeZoneId`，2026-07-07 新增）/ `JsonException`（`CurrentDateTimeToolExecutor` `argumentsJson` 格式错误，2026-07-07 新增）
- **程序错误**：`TimeoutException`（Repository 命令超时）
- **取消**：`OperationCanceledException`（[HD-001 §4.3](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#43-取消传播)）

### 4.3 OTel span / 字段

- `tools.list_available` / `tools.get` / `tools.validate_binding`（`ToolCatalogService`）
- `tools.resolve_bindings`（`ToolBindingResolver`）

**Inkwell 私有字段**：`tools.tool_id` / `tools.binding_count` / `tools.operation_outcome`（`succeeded` / `failed` / `not_found` / `validation_failed`）

**OTel 标准字段**：同 [HD-006 §4.3](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#43-otel-span--字段) 5 个 `exception.*` 字段（`exception.type` / `exception.message` / `exception.stacktrace` / `exception.escaped` / `exception.id`）

> **PII 提示**：`AgentToolBinding.ParametersJson` / `ToolDefinition.ParametersJsonSchema` 原文**不得**进入任何 OTel 字段，同 [HD-006 §4.3](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#43-otel-span--字段) PII 处理方式。

## 5. 公共约定继承（HD-001）

- 命名：`IToolCatalogService` ↔ [HD-001 §5.1](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#51-命名) `I<Module>Service`；`ToolDefinition` ↔ [HD-002 §4.1.2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#412-model-类命名规则2026-05-11-errataf6--adr-022) 撞名降级；`IToolRepository` ↔ [HD-002 §4.1.1](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) `I<TypeName>Repository`
- 签名：全部方法裸 `Task<T>` / `Task` + BCL 异常（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)）；`CancellationToken ct = default` 全方法必填
- Repository 具名动词：`Add`/`Get`/`List` 三选一开头，无 `Async` 后缀（[HD-002 §4.1.3](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#413-repository-方法动词白名单2026-05-11-errataf6--adr-022)）

## 6. 数据库设计增量（追加至 `database-design.md`）

### 表 `tools`（[REQ-007](../../01-requirements/requirements.md)）

- `Id`：`Guid` v7，主键
- `Name`：`string`，唯一索引，长度上限 100（作者判断，非 Owner 拍板，需求未指定具体上限，同 [HD-014 `users.Username`](HD-014-Inkwell.Core.Auth.md) 先例）
- `Description`：`string`，无长度上限（供模型理解工具用途，同 `Instructions` 处理方式）
- `ParametersJsonSchema`：`string`，无长度上限（[JSON Schema](https://json-schema.org/) 文本）
- `CreatedTime` / `UpdatedTime`：`IHasTimestamps`

**索引**：`Name` 唯一索引。**不**包含 `RowVersion`（[§1.3 Q3](Inkwell.Core/HD-016-Inkwell.Core.Tools.md#13-关键决策摘要)，本表 v1 无运行期 Update 场景）；**不**包含 `OwnerUserId`（系统级目录，非用户私有资源）。

**Entity / Mapping / Repository 实现物理位置**：`providers/Inkwell.Persistence.EFCore/{Entities,Mapping,Repositories}/`（[ADR-021](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) + [ADR-022](../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md)）——本节仅记录契约缺口，具体实现留待通过 errata 追加到已 reviewed 的 [HD-009](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)，本次提交不改写 HD-009。

### 6.1 `tools` 表 Seed 数据（2026-07-07 新增）

> 回应 [§8 Q&A-C](#8-需要-owner-确认的问题)（已解决）。本节仅锁定 Seed 数据的**内容契约**（segment 名 / 幂等键 / 各字段字面量）；`InkwellSeeder.SeedAsync()` 的实际接线（新增 `SeedDefaultToolsAsync` 段落调用）留待通过 errata 追加到已 reviewed 的 [HD-009 §3.4](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md#34-inkwellseedercs)，本次提交不改写 HD-009（同 [§6 Entity/Mapping/Repository 缺口](#6-数据库设计增量追加至-database-designmd) 一贯处理方式）。

- **Segment 名**：`DefaultTools`（幂等模式同 [HD-009 §13.12](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md#1312-2026-07-06-errata第十二轮治理修正1311-范围声明失实记述更正与-inkwellseeder-默认管理员账号-seed-落地) `SeedDefaultAdminAsync` 先例：先查唯一键是否存在，不存在则 `Add`，**禁**用 `Id` 主键判定）
- **幂等键**：`tools.Name == "get_current_datetime"`
- **字段字面量**：

  | 字段                   | 值                                                                                                                                                              |
  | ---------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
  | `Id`                   | `Guid.Parse("00000000-0000-0000-0000-000000000101")`——**固定字面量**，必须与 [§3.12 `CurrentDateTimeToolExecutor.ToolId`](#312-inkwellcoretoolscurrentdatetimetoolexecutorcs) 完全一致 |
  | `Name`                 | `"get_current_datetime"`                                                                                                                                      |
  | `Description`          | `"获取当前日期时间，可选指定 IANA 或 Windows 时区标识符（timeZoneId），缺省返回 UTC。"`                                                                        |
  | `ParametersJsonSchema` | `{"type":"object","properties":{"timeZoneId":{"type":"string","description":"IANA 或 Windows 时区标识符，如 Asia/Shanghai，缺省 UTC"}},"required":[]}`        |
  | `CreatedTime`/`UpdatedTime` | Seed 执行时的 `TimeProvider.GetUtcNow()`（同 [HD-009 §3.4](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md#34-inkwellseedercs) `now` 变量惯例） |

> **为何 `Id` 用固定字面量而非 `Guid.CreateVersion7()`**：与 [HD-009 §13.12](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md#1312-2026-07-06-errata第十二轮治理修正1311-范围声明失实记述更正与-inkwellseeder-默认管理员账号-seed-落地) 默认管理员账号（`Id = Guid.CreateVersion7()`，仅按 `Username` 幂等键判定，无需跨文件匹配）不同，`CurrentDateTimeToolExecutor.ToolId` 是**编译期硬编码常量**（[§3.12](#312-inkwellcoretoolscurrentdatetimetoolexecutorcs)），必须与数据库中该行的 `Id` 相等，`ToolExecutorRegistry`（[§3.9](#39-inkwellcoretoolstoolexecutorregistrycs)）才能按 `Guid` 正确匹配到 `IToolExecutor`。两侧（`providers/Inkwell.Persistence.EFCore` 的 Seed 代码 与 `Inkwell.Core.Tools` 的 `CurrentDateTimeToolExecutor`）分处不同 csproj、不允许跨层共享常量（[AGENTS.md §3.2](../../../AGENTS.md)），故采用与 [HD-009 §13.12](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md#1312-2026-07-06-errata第十二轮治理修正1311-范围声明失实记述更正与-inkwellseeder-默认管理员账号-seed-落地) `DefaultAdminPasswordHash`（离线预计算字面量硬编码）同类模式——**两侧各自独立硬编码同一 `Guid` 字面量**，并互相注释交叉引用，防止漂移（回归测试见 [§3.12 测试要求第 5 条](#312-inkwellcoretoolscurrentdatetimetoolexecutorcs)）。

## 7. 文件结构增量（追加至 `file-structure.md`）

### `### Persistence/Tools`（追加至 `## Inkwell.Abstractions` 章节）

```text
src/core/Inkwell.Abstractions/Persistence/
  Tools/                                 # 新增子目录（HD-016；此前仅 HD-002 §4.1.2 示例占位）
    ToolDefinition.cs                    # 业务 Model
    IToolRepository.cs                   # 4 个具名动词方法
```

### `## Inkwell.Abstractions.Tools`（新增一级章节）

```text
src/core/Inkwell.Abstractions/
  Tools/                                 # 新增子目录（HD-016）
    IToolCatalogService.cs               # 顶层业务门面（只读查询 + 绑定校验）
    IToolBindingResolver.cs              # AgentToolBinding → AgentToolDefinition 翻译
    ToolOptions.cs                       # MaxToolsPerAgent / EnableSensitiveDataLogging
    ToolOptionsValidator.cs              # IValidateOptions<ToolOptions>
```

```text
src/core/Inkwell.Core/
  Tools/                                 # 新增子目录（HD-016）
    ToolCatalogService.cs                # 唯一 IToolCatalogService 实现
    IToolExecutor.cs                     # 内部执行委托接口（不暴露 Abstractions）
    ToolExecutorRegistry.cs              # 内部执行器注册表
    ToolBindingResolver.cs               # 唯一 IToolBindingResolver 实现
    ToolsBuilderExtensions.cs            # UseDefaultToolService()
    CurrentDateTimeToolExecutor.cs       # v1 唯一内置工具：当前日期时间查询（2026-07-07 新增，§8 Q&A-C 已解决）
```

## 8. 需要 Owner 确认的问题

> 三项均于本 HD 起草时处于未拍板状态。**2026-07-07 Owner 在对话中直接逐条明确确认**（非通过 `vscode_askQuestions` 工具弹窗），确认结果见各条目"已解决"标注。为保留决策过程与追溯证据，原候选列表与背景说明保留不删除。

### Q&A-A：Tool 目录是否需要运行期管理 API（Admin CRUD）（已解决，2026-07-07）

- **背景**：[§1 顶部范围核实结论](#1-模块概述) 依据 requirements.md / ui-spec.md 现有文本判断 v1 只需"只读目录 + Seed 写入"，但 [REQ-017](../../01-requirements/requirements.md) Admin 最小管理页范围本身在 [HD-014](HD-014-Inkwell.Core.Auth.md) / [HD-015](HD-015-Inkwell.Core.Agents.md) 起草时也未提及"工具管理"，无法排除是遗漏而非明确排除。
- **候选**：
  - **A. 维持本 HD 现有设计**——只读目录 + `InkwellSeeder` 硬编码维护，不提供运行期增删改 API；新增/下线工具需要改代码 + 重新部署
  - **B. 补充最小 Admin CRUD API**（`CreateToolAsync`/`UpdateToolAsync`/`DeleteToolAsync`，比照 [HD-014](HD-014-Inkwell.Core.Auth.md) `IsSuper` 权限模型），对应需要在 [ui-spec.md](../../01-requirements/ui-spec.md) 补一个管理界面，属于新的产品范围，需要另行走 H1 补充需求 / UI 设计
  - **C. 其他折中方案**（如仅支持"禁用/启用工具"而不支持"新增自定义工具"）

**已解决（2026-07-07）**：Owner 在对话中直接明确确认——维持候选 A： Tool 目录 v1 保持只读，不补充运行期管理 API；新增/下线工具继续通过修改 `InkwellSeeder` Seed 数据 + 重新部署完成，不引入 Admin 管理界面。本决议**未改变现有设计**（[§1.3 Q2](#13-关键决策摘要) / [§3.2 `IToolRepository`](#32-persistencetoolsitoolrepositorycs) 方法集维持不变），仅将确认状态从"未拍板"更新为"已确认"。

### Q&A-B：静态绑定参数与模型运行时参数的合并优先级（已解决，2026-07-07）

- **背景**：`AgentToolBinding.ParametersJson`（[HD-015](HD-015-Inkwell.Core.Agents.md) 已锁定字段，Agent 配置时由用户在 [ui-spec.md §4.3.4](../../01-requirements/ui-spec.md) 工具参数表单填写）与模型在对话中生成的运行时调用参数（`AgentToolDefinition.InvokeAsync(argumentsJson, ct)` 的 `argumentsJson`）可能存在同名字段重叠，[requirements.md](../../01-requirements/requirements.md) / [ui-spec.md](../../01-requirements/ui-spec.md) 均未描述这种重叠场景应如何处理。
- **候选**：
  - **A.（本 HD 默认实现，[§3.10](#310-inkwellcoretoolstoolbindingresolvercs)）绑定的静态参数优先**——同名字段被静态值覆盖，防止模型在对话中意外/被诱导覆盖管理员/Agent Owner 设定的固定值（更安全，如 region / 白名单类配置）
  - **B. 模型运行时参数优先**——静态参数仅作为"未提供时的默认值"，模型可覆盖
  - **C. 视为配置错误**——同名字段重叠时直接抛异常，拒绝执行该次工具调用，倒逼工具 schema 设计避免重叠字段

**已解决（2026-07-07）**：Owner 在对话中直接明确确认——维持候选 A（[§3.10](#310-inkwellcoretoolstoolbindingresolvercs) `MergeParameters` 现有实现）：绑定的静态参数优先于模型运行时生成的同名参数，理由：防止模型在对话中意外/被诱导覆盖 Agent Owner 设定的固定值，安全语义按更严格口径处理。本决议**未改变现有设计**，仅将确认状态从"未拍板"更新为"已确认"。

### Q&A-C：v1 是否需要至少一个真实可用的内置工具（已解决，2026-07-07）

- **背景**：本 HD 只锁定 `IToolExecutor` 注册机制（[§3.8](#38-inkwellcoretoolsitoolexecutorcs) / [§3.9](#39-inkwellcoretoolstoolexecutorregistrycs)），**不提供任何具体工具实现**，因为 [requirements.md](../../01-requirements/requirements.md) 未枚举 v1 具体内置工具清单（如计算器 / 网页搜索 / 日期时间等）。若无任何 `IToolExecutor` 落地，[REQ-007](../../01-requirements/requirements.md) 端到端场景（[AC-026](../../01-requirements/acceptance-criteria.md)"触发该工具的对话"）在 v1 将**无法真正跑通**（即使 `tools` 表可以 Seed 一条 `ToolDefinition` 元数据，没有对应 `IToolExecutor` 注册，`ToolBindingResolver.ResolveAsync` 会抛 `KeyNotFoundException`）。
- **候选**：
  - **A. 本次不额外落地具体工具**——留待后续单独任务（H5 编码阶段）按 Owner 实际需要的工具清单实现 `IToolExecutor` + 补 Seed 数据，本 HD 仅完成机制设计
  - **B. 现在就明确至少 1~2 个 v1 必须可用的具体工具**（如"当前时间"这类纯 BCL、无外部依赖的最小示例），需要 Owner 给出具体工具清单与其参数 schema

**已解决（2026-07-07）**：Owner 在对话中直接明确确认——选候选 B 的收窄版：v1 需要至少一个真实可用的内置工具，确保 [REQ-007](../../01-requirements/requirements.md) 端到端场景能跑通；具体选哪个工具由本 HD 作者判断（约束：不依赖外部 API key / 不需要额外第三方服务 / 可在 InMemory、单元测试环境下真实跑通）。本 HD 已据此设计并落地 `CurrentDateTimeToolExecutor`（[§3.12](#312-inkwellcoretoolscurrentdatetimetoolexecutorcs)），并同步补充 Seed 数据（[§6.1](#61-tools-表-seed-数据2026-07-07-新增)）。

## 9. 下一步

- 本 HD 起草完成后，已同步对已 reviewed 的 [HD-015 `IAgentInvocationService`](HD-015-Inkwell.Core.Agents.md#34-agentsiagentinvocationservicecs) 发起 2026-07-07 errata，消费本 HD 的 `IToolBindingResolver`，把 `AgentRunRequest.Tools` 从恒为 `null` 改为真实解析结果（详见 HD-015 对应章节的 errata 标注）。
- **2026-07-07 更新**：[§8](#8-需要-owner-确认的问题) 三个开放问题均已由 Owner 在对话中直接确认解决，不再是评审前置阻塞项。切到 `h3-detailed-design-reviewer` Agent 对本 HD 跑机械化评审时，请重点核查新增的 [§3.12 `CurrentDateTimeToolExecutor`](#312-inkwellcoretoolscurrentdatetimetoolexecutorcs) 与 [§6.1 Seed 数据](#61-tools-表-seed-数据2026-07-07-新增)——尤其两侧 `Guid` 字面量（`00000000-0000-0000-0000-000000000101`）是否一致、`InkwellSeeder` errata 接线是否仍需在 H5 编码阶段回填。
