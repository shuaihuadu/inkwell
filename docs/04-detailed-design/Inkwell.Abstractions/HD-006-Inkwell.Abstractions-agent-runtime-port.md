---
id: HD-006
title: Inkwell.Abstractions 详细设计 — Agent Runtime Port（IAgentRuntime facade + DTO + Options）
stage: H3
status: draft
reviewers: []
upstream:
  - REQ-003
  - REQ-004
  - REQ-005
  - REQ-006
  - REQ-007
  - REQ-008
  - REQ-010
  - REQ-014
  - REQ-016
  - ADR-003
  - ADR-011
  - ADR-012
  - ADR-017
  - ADR-023
  - HD-001
  - HD-004
  - HD-005
---

<!-- markdownlint-disable MD060 -->
<!-- 中文 + 英文混排长表格在 markdownlint 列宽计算下字面对齐 ≠ 视觉对齐（详 /memories/markdown-lint.md，与 HD-004 / HD-005 同处理方式），表格仍按 docs-style §3 视觉对齐维护，机械 MD060 不予执行。 -->

> **错误处理约定**（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)，含 [errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) 废错误码、[errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 删 `Result<T>` / `Error` 抽象）：端口层与业务层统一采用裸 `Task<T>` + .NET BCL 异常。本 HD 与 [HD-004](HD-004-Inkwell.Abstractions-cache-port.md) / [HD-005](HD-005-Inkwell.Abstractions-queue-port.md) 同批次沿用最终态规约，全部签名从第一版直接采用裸 `Task<T>` / `Task<bool>` / `IAsyncEnumerable<T>` + BCL 异常，不存在"先 Result 后 errata"的历史包袱。
>
> **核心架构约束（[AGENTS.md §3.2](../../../AGENTS.md) 已锁）**：`Inkwell.Core.AgentRuntime` 命名空间是**唯一**允许 `using Microsoft.Agents.AI.*` 的位置（[ADR-017 §依赖规则第 3 条](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md)）。`IAgentRuntime` 接口本身放在 `Inkwell.Abstractions`（端口层，零外部包依赖），对外暴露给业务命名空间（`Inkwell.Core.Agents` / `.Conversations` / `.Orchestrations` 等）使用。**接口签名禁止泄漏任何 MAF 类型**——`AIAgent` / `AgentSession` / `ChatMessage` / `AgentResponse` / `AgentResponseUpdate` / `AgentRunOptions` 等 [Microsoft Agent Framework](../../../../../microsoft/agent-framework/) SDK 类型不得出现在 `IAgentRuntime` 方法参数或返回值里；本 HD 全部方法与 DTO 使用 Inkwell 自己的类型（`AgentRunRequest` / `AgentTurnResult` / `AgentRunEvent` / `AgentChatMessage` 等），由 `Inkwell.Core.AgentRuntime` 实现层负责与 MAF 类型的相互转换（详见 §4.4 防泄漏示例）。
>
> **范围切片**：本 HD 覆盖 `Inkwell.Abstractions/AgentRuntime/` 子层——`IAgentRuntime` facade（3 方法：`RunTurnAsync` / `RunTurnStreamingAsync` / `CancelRunAsync`，[picker Q-facade-scope=A](#131-起草期-picker-决策2026-07-05)）、请求 / 响应 / 流式事件 DTO（`AgentRunRequest` / `AgentTurnResult` / `AgentRunEvent` 封闭子类型族 / `AgentChatMessage` + `AgentMessageContentPart` 封闭子类型族 / `AgentToolDefinition` / `AgentModelParameters` / `AgentToolCallRecord`）、`AgentRuntimeOptions` + Validator。**不**实现 Provider 行为（`Inkwell.Core.AgentRuntime` 的具体实现——基于 [`Microsoft.Agents.AI.OpenAI`](../../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.OpenAI/) 的 Azure OpenAI 封装、`ChatClientAgent` 装配、`AgentSession` 序列化策略、`CancellationTokenSource` 注册表——在 `Inkwell.Core` 独立 HD 起草，[ADR-017 §依赖规则](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) 决定 `Inkwell.AgentRuntime` 合并进 `Inkwell.Core.AgentRuntime` 命名空间，不设独立 `providers/*` csproj）、**不**锁定 Skill Discovery/Activation/Execution 的具体加载逻辑（[ADR-010](../../03-architecture/adr/ADR-010-skill-loading-static-only-v1.md) v1 仅静态加载，不实例化 MAF SkillExecutor，Skill 元数据注入 Instructions 的具体拼接方式留 `Inkwell.Core.Skills` 业务 HD）、**不**锁定工具注册表 / Agent 配置持久化（`AgentDefinition` 已在 [HD-002](HD-002-Inkwell.Abstractions-persistence-port.md) 锁定，工具注册表留 `Inkwell.Core.Tools` 业务 HD）、**不**锁定 AG-UI 事件到本 HD `AgentRunEvent` 的具体映射代码（留 `Inkwell.WebApi` HD，本 HD 仅保证字段可 1:1 映射）。
>
> **跨 HD 关联**：本 HD 与 [HD-001 foundation](HD-001-Inkwell.Abstractions-foundation.md)（Builder DSL / OTel 字段 / `InkwellProvidersOptions.AgentRuntime` 选择器槽位，默认值 `"AzureOpenAI"`）+ [HD-004 Cache port](HD-004-Inkwell.Abstractions-cache-port.md) / [HD-005 Queue port](HD-005-Inkwell.Abstractions-queue-port.md)（同级端口模板，OTel span 命名风格对齐）形成 Abstractions 端口族的第五张 HD。**关键风险联动**：[ADR-011](../../03-architecture/adr/ADR-011-auto-lock-with-inflight-task-survival.md) 要求跨锁屏在途任务不中断——本 HD 的 `CancelRunAsync` 是**用户主动**中断（"停止生成"按钮），与 ADR-011 的自动锁屏保活场景是两条独立路径，互不冲突（详 §1.4）；[ADR-012](../../03-architecture/adr/ADR-012-client-server-protocol-rest-agui.md) 要求 AG-UI 事件类型（message / tool_call / state_delta / lifecycle）与本 HD `AgentRunEvent` 封闭子类型族 1:1 对齐（详 §3.3 / §7.3）。

## 1. 模块概述

### 1.1 职责

- `IAgentRuntime` facade（§3.1）：定义业务命名空间调用 Agent 执行引擎的统一入口；`RunTurnAsync`（同步取完整响应）/ `RunTurnStreamingAsync`（流式取增量事件）/ `CancelRunAsync`（显式中断在途 Run）3 个方法（[picker Q-facade-scope=A](#131-起草期-picker-决策2026-07-05)），覆盖 [REQ-004](../../01-requirements/requirements.md)（Instructions 生效）/ [REQ-005](../../01-requirements/requirements.md)（模型选择）/ [REQ-006](../../01-requirements/requirements.md)（模型参数）/ [REQ-007](../../01-requirements/requirements.md)（工具调用）/ [REQ-010](../../01-requirements/requirements.md)（多轮对话）/ [REQ-014](../../01-requirements/requirements.md)（trace 全链路）/ [REQ-016](../../01-requirements/requirements.md)（多模态输入）场景
- 请求 / 响应 DTO（§3.2 ~ §3.7）：`AgentRunRequest`（一次 Run 调用的全部已解析上下文）、`AgentTurnResult`（非流式最终结果）、`AgentChatMessage` + `AgentMessageContentPart` 封闭子类型族（`TextPart` / `ImagePart` / `DocumentPart`，[REQ-016](../../01-requirements/requirements.md) 多模态载体）、`AgentModelParameters`（[REQ-006](../../01-requirements/requirements.md) temperature / top_p / max_tokens）、`AgentToolDefinition`（[REQ-007](../../01-requirements/requirements.md) 工具描述 + 同进程调用委托）、`AgentToolCallRecord`（非流式路径下的工具调用回溯）
- `AgentRunEvent` 封闭子类型族（§3.8）：`TextDelta` / `ToolCallRequested` / `ToolCallResult` / `StateDelta` / `RunCompleted` / `RunError` 6 个子类型，1:1 对应 [ADR-012](../../03-architecture/adr/ADR-012-client-server-protocol-rest-agui.md) AG-UI 的 message / tool_call / state_delta / lifecycle 四大类事件
- `AgentRuntimeOptions` + Validator（§3.9 ~ §3.10）：模型参数默认值、单轮 Run 超时、`EnableSensitiveDataLogging` 开关

### 1.2 范围

- **在内**：facade 接口 + 请求 / 响应 / 流式事件 DTO + Options
- **不在内**：
  - `Inkwell.Core.AgentRuntime` 具体实现（基于 [`Microsoft.Agents.AI.OpenAI`](../../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.OpenAI/) 的 `ChatClientAgent` 装配、`AgentSession` 生命周期管理、`CancellationTokenSource` 注册表、MAF ↔ Inkwell DTO 互转）——独立 `Inkwell.Core` HD 起草，[ADR-017 §依赖规则](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) 不设独立 `providers/*` csproj
  - Skill Discovery → Activation → Execution 的具体加载 / 拼接逻辑（[ADR-010](../../03-architecture/adr/ADR-010-skill-loading-static-only-v1.md) v1 静态加载，留 `Inkwell.Core.Skills` 业务 HD）
  - 工具注册表本身（哪些工具存在、参数 schema 从哪里来）——留 `Inkwell.Core.Tools` 业务 HD；本 HD 仅定义 `AgentToolDefinition` 承载形态
  - `AgentDefinition` 业务 Model 与持久化（已在 [HD-002 §4.1.2](HD-002-Inkwell.Abstractions-persistence-port.md) 锁定）
  - `AgentSession` / 对话历史的持久化与重建策略（[`Inkwell.Conversations`](../../01-requirements/repo-impact-map.md) 业务 HD 负责查历史并组装成 `AgentRunRequest.Messages`；本 HD 端口不反向依赖 `IPersistenceProvider`，[picker Q-run-request-shape=A](#131-起草期-picker-决策2026-07-05)）
  - AG-UI 事件到 `AgentRunEvent` 的具体映射代码（留 `Inkwell.WebApi` HD；本 HD §3.8 / §7.3 仅保证字段可 1:1 映射）
  - DurableTask 编排持久化（[REQ-012](../../01-requirements/requirements.md) 多 Agent 编排属 `Inkwell.Core.Orchestrations` 独立 HD，[ADR-003](../../03-architecture/adr/ADR-003-agent-engine-microsoft-agent-framework.md) `Microsoft.Agents.AI.DurableTask` 用于编排级持久化，与本 HD 单轮 Run 的 `CancelRunAsync` 是两条独立机制，[picker Q-cancel-mechanism=A](#131-起草期-picker-决策2026-07-05) 否决了"全部 Run 走 DurableTask Terminate"的过度设计选项）

### 1.3 关键决策摘要

> 全部由 2026-07-05 picker 拍板，决策证据见本节"出处"列；详细候选与放弃理由见 [§13 决策记录](#13-决策记录)。

- **Q-facade-scope**：`IAgentRuntime` 含 3 方法（`RunTurnAsync` / `RunTurnStreamingAsync` / `CancelRunAsync`），不引入端口层 `CreateSessionAsync` / `DeserializeSessionAsync`（会话生命周期留 `Inkwell.Core.AgentRuntime` 内部管理）
- **Q-run-request-shape**：`AgentRunRequest` 携带全部已解析上下文（`RunId` / `AgentId` / `ConversationId?` / `Messages` / `Instructions?` / `ModelId?` / `ModelParameters?` / `Tools?`）；端口是 MAF 的薄封装，不反向依赖 `IPersistenceProvider` 查历史 / 配置
- **Q-streaming-event-shape**：`AgentRunEvent` 是抽象 `record` + 6 个封闭 `sealed record` 子类型（`TextDelta` / `ToolCallRequested` / `ToolCallResult` / `StateDelta` / `RunCompleted` / `RunError`），1:1 对应 AG-UI 四大类事件
- **Q-cancel-mechanism**：`Inkwell.Core.AgentRuntime` 内部维护 `ConcurrentDictionary<string, CancellationTokenSource>` 按 `RunId` 索引；`CancelRunAsync` 找到则 `Cancel()` 返回 `true`，未知 / 已结束返回 `false`（幂等，不抛异常，与 [HD-004](HD-004-Inkwell.Abstractions-cache-port.md#13-决策记录) / [HD-005](HD-005-Inkwell.Abstractions-queue-port.md#13-决策记录) 幂等 bool 惯例一致）
- **Q-perf-budget**：宽松档，仅 facade 开销（不含模型推理延迟）——`RunTurnAsync` / `RunTurnStreamingAsync` P50 < 50ms / P99 < 200ms；`CancelRunAsync` P50 < 20ms / P99 < 100ms
- **Q-otel**：OTel span 命名 `agentruntime.<verb>`（`run_turn` / `run_turn_streaming` / `cancel_run`）+ 6 个私有字段 `agentruntime.agent_id` / `agentruntime.run_id` / `agentruntime.conversation_id` / `agentruntime.model_id` / `agentruntime.tool_call_count` / `agentruntime.operation_outcome`
- **Q-tool-result-passthrough**：流式路径出 `ToolCallRequested` / `ToolCallResult` 事件；非流式路径 `AgentTurnResult.ToolCalls`（`IReadOnlyList<AgentToolCallRecord>`）同样携带完整工具调用记录，两条路径在调试 trace 中都可见（[REQ-007](../../01-requirements/requirements.md) 验收要求）
- **Q-multimodal-representation**：`AgentChatMessage.Content` 为 `IReadOnlyList<AgentMessageContentPart>` 封闭子类型（`TextPart` / `ImagePart` / `DocumentPart`）；语音已在上游转写为文本（[REQ-016 实现说明](../../01-requirements/requirements.md)），不设独立 `AudioPart`
- **Q-model-params-defaults**：`AgentRuntimeOptions` 默认 `DefaultTemperature = 1.0` / `DefaultTopP = 1.0` / `DefaultMaxTokens = 2048`（Azure OpenAI SDK 默认值 + 安全上限，不额外收紧创意度）
- **Q-run-timeout**：`AgentRuntimeOptions.RunTimeoutSeconds` 默认 `300`（5 分钟，facade 层安全超时，防止单轮对话永久挂起；长任务编排走 `Inkwell.Core.Orchestrations` + DurableTask，不复用本超时）
- **Q-tool-definition-shape**：`AgentToolDefinition { Name, Description, ParametersJsonSchema, Func<string, CancellationToken, Task<string>> InvokeAsync }`——`ArgumentsJson` / 返回值均为 `string`（JSON 文本），与 `AgentRunEvent.ToolCallRequested/ToolCallResult` 的 `Json` 字段同形态，便于统一序列化

### 1.4 ADR-011 自动锁屏保活 vs 本 HD 用户主动中断的边界声明

[ADR-011](../../03-architecture/adr/ADR-011-auto-lock-with-inflight-task-survival.md) 要求"锁屏期间在途任务不中断"——该保活机制作用于 **Electron 主进程的 SSE 订阅连接层**，与后端 Run 本身的生命周期无关：锁屏只影响客户端展示层，不触发任何 `CancelRunAsync` 调用。本 HD `CancelRunAsync` 仅由**用户主动**触发（如 UI"停止生成"按钮），二者是完全独立的两条路径，互不冲突：

- 锁屏 / 解锁：客户端主进程行为，**不**经过 `IAgentRuntime`
- 用户点击"停止生成"：客户端发起 `POST /api/runs/{runId}/cancel`（[`Inkwell.WebApi`](../../../AGENTS.md) HD 起草时定义该端点），后端调用 `IAgentRuntime.CancelRunAsync(runId)`

## 2. 文件结构

```text
src/core/Inkwell.Abstractions/
  AgentRuntime/                              # （新增子目录）
    IAgentRuntime.cs                         # 顶层 facade（3 方法）
    AgentRunRequest.cs                       # record，一次 Run 调用的全部已解析上下文
    AgentTurnResult.cs                       # record，非流式最终结果（含 ToolCalls 回溯）
    AgentChatMessage.cs                      # record，对话消息（Role + Content 封闭子类型族）
    AgentMessageContentPart.cs               # abstract record + 3 个 sealed 子类型（TextPart/ImagePart/DocumentPart）
    AgentModelParameters.cs                  # record，temperature/top_p/max_tokens（REQ-006）
    AgentToolDefinition.cs                   # record，工具描述 + 同进程调用委托（REQ-007）
    AgentToolCallRecord.cs                   # record，非流式路径下的工具调用回溯
    AgentRunEvent.cs                         # abstract record + 6 个 sealed 子类型（流式事件）
    AgentRuntimeOptions.cs                   # 详细配置
    AgentRuntimeOptionsValidator.cs          # IValidateOptions<AgentRuntimeOptions>
```

> **csproj 依赖白名单**：HD-006 不引入新依赖，仍仅 [HD-001 §2 锁定的](HD-001-Inkwell.Abstractions-foundation.md) `Microsoft.Extensions.{DependencyInjection,Configuration,Options,Logging}.Abstractions` + `Microsoft.Extensions.VectorData.Abstractions`（HD-008 起用）+ `System.Text.Json`（BCL 内置，`AgentToolDefinition.ParametersJsonSchema` / `AgentToolCallRecord` 序列化）。**严禁**因本 HD 引入 `Microsoft.Agents.AI.*` / `Microsoft.Agents.AI.Abstractions` / `Microsoft.Agents.AI.OpenAI` 等任何 MAF 包（违反 [ADR-017 §依赖规则第 3/4 条](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) + [RISK-001](../../03-architecture/risk-analysis.md) MAF 接触面收敛约束）。

## 3. 程序文件设计（10 字段 × 10 文件）

### 3.1 `AgentRuntime/IAgentRuntime.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/AgentRuntime/IAgentRuntime.cs`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| 职责         | 顶层 Agent 执行引擎 facade；3 个方法覆盖同步取完整响应 / 流式取增量事件 / 显式中断在途 Run（[picker Q-facade-scope=A](#13-关键决策摘要)）；全部签名走裸 `Task<T>` / `Task<bool>` / `IAsyncEnumerable<T>` + BCL 异常（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)），不走 `Result<T>`；接口本身**不**出现任何 `Microsoft.Agents.AI.*` 类型（[ADR-017 §依赖规则第 3 条](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md)）                                                                                                                                                                                                                                                    |
| 对外接口     | `public interface IAgentRuntime { Task<AgentTurnResult> RunTurnAsync(AgentRunRequest request, CancellationToken ct = default); IAsyncEnumerable<AgentRunEvent> RunTurnStreamingAsync(AgentRunRequest request, [EnumeratorCancellation] CancellationToken ct = default); Task<bool> CancelRunAsync(string runId, CancellationToken ct = default); }`                                                                                                                                                                                                                                                                                                                                                                                                  |
| 内部函数或类 | 接口本身；实现由 `Inkwell.Core.AgentRuntime` 命名空间独立 HD 提供（唯一允许 `using Microsoft.Agents.AI.*` 的位置，[ADR-017 §依赖规则第 3 条](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md)）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| 输入数据     | `AgentRunRequest request`（`RunTurnAsync` / `RunTurnStreamingAsync`） / `string runId`（`CancelRunAsync`） / `CancellationToken ct`（全部方法）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| 输出数据     | `Task<AgentTurnResult>`（`RunTurnAsync`） / `IAsyncEnumerable<AgentRunEvent>`（`RunTurnStreamingAsync`） / `Task<bool>`（`CancelRunAsync`，`true` = 找到并已请求中断，`false` = `runId` 未知或该 Run 已结束）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| 依赖模块     | `AgentRuntime/AgentRunRequest.cs` / `AgentRuntime/AgentTurnResult.cs` / `AgentRuntime/AgentRunEvent.cs`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| 错误处理     | 全部上抛 BCL 异常（见 [§4.2 BCL 异常分类](#42-bcl-异常分类业务失败-vs-程序错误)）：`request.AgentId` 对应的 Agent 不存在 → `KeyNotFoundException`（由实现层查内部 Agent 缓存 / `IPersistenceProvider` 判定，端口本身不查）；`request` 校验失败（`Messages` 为空且无 `Instructions`）→ `ArgumentException`；模型 Provider 侧故障（鉴权失败 / 限流 / 网络）→ `IOException`；单轮超时（超过 `AgentRuntimeOptions.RunTimeoutSeconds`）→ `TimeoutException`；取消 → `OperationCanceledException`；`CancelRunAsync` 对未知 / 已结束 `runId` **不抛异常**，返回 `false`（幂等语义）                                                                                                                                                                    |
| 日志要求     | 实现层在每个方法入口 / 出口写 OTel span，命名 `agentruntime.<verb>`（`run_turn` / `run_turn_streaming` / `cancel_run`）；6 个 Inkwell 私有字段（`agentruntime.agent_id` / `agentruntime.run_id` / `agentruntime.conversation_id` / `agentruntime.model_id` / `agentruntime.tool_call_count` / `agentruntime.operation_outcome`）+ 5 个 OTel 标准 `exception.*` 字段（详 §4.3）；`AgentChatMessage.Content` 的实际文本 / 图片数据**不得**进入任何 OTel 字段（详 §7.2 PII 提示）                                                                                                                                                                                                                                                                     |
| 测试要求     | `tests/core/Inkwell.Abstractions.Tests/AgentRuntime/IAgentRuntimeContractTests.cs`：契约测试（接口形态 ABI 锁定 via [`PublicApiAnalyzers`](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md)）；3 个方法签名 / 参数顺序 / 默认值 / 返回类型逐一验证；行为测试在 `tests/core/Inkwell.Providers.Contract/AgentRuntime/`（跨 Provider 家族契约包，与 [HD-002 §8](HD-002-Inkwell.Abstractions-persistence-port.md) / [HD-003 §8.3](HD-003-Inkwell.Abstractions-file-storage-port.md) / [HD-004 §8.3](HD-004-Inkwell.Abstractions-cache-port.md) / [HD-005 §8.3](HD-005-Inkwell.Abstractions-queue-port.md) 拓扑一致），验证 MAF 类型不泄漏（详 §4.4 CI 自检 Q1）由 `Inkwell.Core.AgentRuntime` HD 联合起草 |

### 3.2 `AgentRuntime/AgentRunRequest.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 文件路径     | `src/core/Inkwell.Abstractions/AgentRuntime/AgentRunRequest.cs`                                                                                                                                                                                                                                                                                                                                                                          |
| 职责         | 一次 Run 调用的全部已解析上下文；业务命名空间（`Inkwell.Core.Agents` / `.Conversations` / `.Tools` / `.Skills`）负责在调用前查好 Agent 配置 / 对话历史 / 工具注册表并组装本 DTO（[picker Q-run-request-shape=A](#13-关键决策摘要)），端口本身不反向依赖 `IPersistenceProvider`                                                                                                                                                       |
| 对外接口     | `public sealed record AgentRunRequest { public required string RunId { get; init; } public required Guid AgentId { get; init; } public Guid? ConversationId { get; init; } public required IReadOnlyList<AgentChatMessage> Messages { get; init; } public string? Instructions { get; init; } public string? ModelId { get; init; } public AgentModelParameters? ModelParameters { get; init; } public IReadOnlyList<AgentToolDefinition>? Tools { get; init; } }` |
| 内部函数或类 | 构造期校验 `RunId` 非空、`Messages` 非空集合（`Messages.Count == 0` 且 `Instructions` 为 `null` 时不合法——无输入无指令则无法产生响应）                                                                                                                                                                                                                                                                                                    |
| 输入数据     | 由调用方（业务命名空间）组装：`RunId`（客户端生成或 `Inkwell.WebApi` 端点生成，[REQ-014](../../01-requirements/requirements.md) trace 关联键）/ `AgentId`（[HD-002 `AgentDefinition`](HD-002-Inkwell.Abstractions-persistence-port.md) 主键） / `ConversationId?`（`null` = 新对话，[REQ-010](../../01-requirements/requirements.md)） / `Messages`（本轮 + 历史消息，`Inkwell.Conversations` 组装） / `Instructions?` / `ModelId?` / `ModelParameters?`（未提供则实现层套用 `AgentRuntimeOptions` 默认值，[REQ-006](../../01-requirements/requirements.md)"使用默认"） / `Tools?`（`Inkwell.Core.Tools` 解析勾选的工具，[REQ-007](../../01-requirements/requirements.md)）                                                            |
| 输出数据     | `AgentRunRequest` 实例                                                                                                                                                                                                                                                                                                                                                                                                                    |
| 依赖模块     | `AgentChatMessage.cs` / `AgentModelParameters.cs` / `AgentToolDefinition.cs`                                                                                                                                                                                                                                                                                                                                                             |
| 错误处理     | `RunId` 为 null/empty → `ArgumentException`；`Messages` 为空集合且 `Instructions` 为 `null` → `ArgumentException`（构造期校验，非运行期异常）                                                                                                                                                                                                                                                                                            |
| 日志要求     | DTO 自身不做日志；`RunTurnAsync` / `RunTurnStreamingAsync` 在 `agentruntime.run_turn(_streaming)` span 输出 `agentruntime.agent_id` / `agentruntime.run_id` / `agentruntime.conversation_id`；`Messages` 内容不进 OTel（详 §7.2）                                                                                                                                                                                                       |
| 测试要求     | `AgentRunRequestTests.cs`：(1) 全部字段可正常构造；(2) `Messages` 空集合 + `Instructions` 为 `null` 抛 `ArgumentException`；(3) `Messages` 空集合 + `Instructions` 非空合法（无历史仅系统指令场景）；(4) `ConversationId` 为 `null` 合法（新对话）；(5) record equality                                                                                                                                                                  |

### 3.3 `AgentRuntime/AgentTurnResult.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                     |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/AgentRuntime/AgentTurnResult.cs`                                                                                                                                                                                                                                                                            |
| 职责         | `RunTurnAsync`（非流式）返回的完整结果；携带最终响应消息 + 完整工具调用回溯，保证非流式路径与流式路径在调试 trace 中的可见性对等（[picker Q-tool-result-passthrough=A](#13-关键决策摘要)，[REQ-007](../../01-requirements/requirements.md) / [REQ-014](../../01-requirements/requirements.md) 验收要求）                                |
| 对外接口     | `public sealed record AgentTurnResult { public required string RunId { get; init; } public required AgentChatMessage Message { get; init; } public required IReadOnlyList<AgentToolCallRecord> ToolCalls { get; init; } public required string ModelIdUsed { get; init; } public required AgentModelParameters ModelParametersUsed { get; init; } }` |
| 内部函数或类 | record 自身；`ModelIdUsed` / `ModelParametersUsed` 回显实际生效值（若 `AgentRunRequest.ModelId` / `ModelParameters` 未提供，此处回显 `AgentRuntimeOptions` 默认值），满足 [REQ-006 验收标准](../../01-requirements/requirements.md)"这些参数最终在调试 trace 中可见出现在对应的模型调用入参中"                                          |
| 输入数据     | 由实现层构造（`RunTurnAsync` 内部调用 MAF 完成后组装）                                                                                                                                                                                                                                                                                     |
| 输出数据     | `AgentTurnResult` 实例                                                                                                                                                                                                                                                                                                                    |
| 依赖模块     | `AgentChatMessage.cs` / `AgentToolCallRecord.cs` / `AgentModelParameters.cs`                                                                                                                                                                                                                                                              |
| 错误处理     | DTO 自身不产生异常；构造失败（模型调用失败）由 `RunTurnAsync` 抛 §4.2 对应 BCL 异常，不返回本 DTO                                                                                                                                                                                                                                          |
| 日志要求     | DTO 自身不做日志；`RunTurnAsync` 在 `agentruntime.run_turn` span 输出 `agentruntime.model_id`（= `ModelIdUsed`）/ `agentruntime.tool_call_count`（= `ToolCalls.Count`）                                                                                                                                                                    |
| 测试要求     | `AgentTurnResultTests.cs`：(1) 全部字段可正常构造；(2) `ToolCalls` 空列表合法（无工具调用场景）；(3) record equality（`Message` / `ToolCalls` 参与比较）                                                                                                                                                                                   |

### 3.4 `AgentRuntime/AgentChatMessage.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                       |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/AgentRuntime/AgentChatMessage.cs`                                                                                                                                                                                                                            |
| 职责         | 对话消息 DTO；跨 `AgentRunRequest.Messages` / `AgentTurnResult.Message` / 流式 `TextDelta` 复用；不泄漏 MAF `ChatMessage` 类型                                                                                                                                                             |
| 对外接口     | `public enum AgentChatRole { System, User, Assistant, Tool }`；`public sealed record AgentChatMessage { public required AgentChatRole Role { get; init; } public required IReadOnlyList<AgentMessageContentPart> Content { get; init; } public string? AuthorName { get; init; } }`        |
| 内部函数或类 | 构造期校验 `Content` 非空集合（除 `Role = Tool` 场景外，`Tool` 角色消息可能仅承载 `ToolCallId` 关联，[picker 待补见 §11](#11-待补--待评审)）                                                                                                                                              |
| 输入数据     | `Role` / `Content` / 可选 `AuthorName`（多 Agent 场景标识发言方，[REQ-012](../../01-requirements/requirements.md) 编排预留）                                                                                                                                                              |
| 输出数据     | `AgentChatMessage` 实例                                                                                                                                                                                                                                                                    |
| 依赖模块     | `AgentMessageContentPart.cs`                                                                                                                                                                                                                                                              |
| 错误处理     | `Content` 为 `null` → `ArgumentNullException`                                                                                                                                                                                                                                             |
| 日志要求     | DTO 自身不做日志；实现层在写 trace 时应对 `Content` 做长度截断（详 §7.2），不完整原文写入 OTel                                                                                                                                                                                             |
| 测试要求     | `AgentChatMessageTests.cs`：(1) 四种 `Role` 均可构造；(2) `Content` 为 `null` 抛异常；(3) `Content` 为空集合合法（占位消息）；(4) record equality                                                                                                                                          |

### 3.5 `AgentRuntime/AgentMessageContentPart.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/AgentRuntime/AgentMessageContentPart.cs`                                                                                                                                                                                                                                                                                                                                                                                                                     |
| 职责         | 消息内容分片，承载 [REQ-016 多模态输入](../../01-requirements/requirements.md)（图片 / 文档）；语音已在上游 [`Inkwell.Multimodal`](../../01-requirements/repo-impact-map.md) 转写为文本（[REQ-016 实现说明](../../01-requirements/requirements.md)），不设独立 `AudioPart`（[picker Q-multimodal-representation=A](#13-关键决策摘要)）                                                                                                                                                  |
| 对外接口     | `public abstract record AgentMessageContentPart;` `public sealed record TextPart(string Text) : AgentMessageContentPart;` `public sealed record ImagePart(Uri ImageUri, string? MediaType = null) : AgentMessageContentPart;` `public sealed record DocumentPart(Uri DocumentUri, string FileName, string? MediaType = null) : AgentMessageContentPart;`                                                                                                                              |
| 内部函数或类 | 3 个 `sealed record`；`ImagePart` / `DocumentPart` 均用 `Uri` 引用（指向 [`IFileStorageProvider`](HD-003-Inkwell.Abstractions-file-storage-port.md) 已上传的对象，本 HD 不携带原始字节，避免大对象进入端口 DTO；`Inkwell.Core.Multimodal` 业务层负责先上传后传引用）                                                                                                                                                                                                                     |
| 输入数据     | `TextPart(string Text)` / `ImagePart(Uri ImageUri, string? MediaType)` / `DocumentPart(Uri DocumentUri, string FileName, string? MediaType)`                                                                                                                                                                                                                                                                                                                                              |
| 输出数据     | 对应 `sealed record` 实例                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| 依赖模块     | System.*（`Uri`）                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| 错误处理     | `TextPart.Text` 为 `null` → `ArgumentNullException`（空字符串合法）；`ImagePart.ImageUri` / `DocumentPart.DocumentUri` 为 `null` → `ArgumentNullException`；`DocumentPart.FileName` 为 null/empty → `ArgumentException`                                                                                                                                                                                                                                                                   |
| 日志要求     | `TextPart.Text` 原文**不得**进 OTel（详 §7.2）；`ImagePart` / `DocumentPart` 的 `Uri` 可进 OTel（内部对象引用，非公网可达，不含内容本身）                                                                                                                                                                                                                                                                                                                                                  |
| 测试要求     | `AgentMessageContentPartTests.cs`：(1) 三种子类型均可构造；(2) 各自必填字段为 `null` 抛异常；(3) 模式匹配（`switch` 表达式对三种子类型分流）；(4) record equality                                                                                                                                                                                                                                                                                                                        |

### 3.6 `AgentRuntime/AgentModelParameters.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                          |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 文件路径     | `src/core/Inkwell.Abstractions/AgentRuntime/AgentModelParameters.cs`                                                                                                                                                                                                                            |
| 职责         | 模型调用参数 DTO（[REQ-006](../../01-requirements/requirements.md)）；`AgentRunRequest.ModelParameters` 可选覆盖，未提供时实现层套用 `AgentRuntimeOptions` 默认值                                                                                                                             |
| 对外接口     | `public sealed record AgentModelParameters { [Range(0.0, 2.0)] public double? Temperature { get; init; } [Range(0.0, 1.0)] public double? TopP { get; init; } [Range(1, 128000)] public int? MaxTokens { get; init; } }`                                                                       |
| 内部函数或类 | 三字段均可空——`null` 表示"使用默认"（[REQ-006 验收标准](../../01-requirements/requirements.md)"UI 允许使用默认"）；DataAnnotations `[Range]` 仅在非空时生效                                                                                                                                    |
| 输入数据     | `Temperature` / `TopP` / `MaxTokens`，均可选                                                                                                                                                                                                                                                    |
| 输出数据     | `AgentModelParameters` 实例                                                                                                                                                                                                                                                                    |
| 依赖模块     | `System.ComponentModel.DataAnnotations`                                                                                                                                                                                                                                                        |
| 错误处理     | 越界值由实现层调用 `Validator.TryValidateObject` 校验，失败 → `ArgumentOutOfRangeException`（本 DTO 构造期不主动抛，交由调用点显式校验，因三字段均可空、`record` 默认构造不天然触发 DataAnnotations）                                                                                          |
| 日志要求     | `RunTurnAsync` / `RunTurnStreamingAsync` 完成后在 `AgentTurnResult.ModelParametersUsed` / 流式 `RunCompleted` 事件中回显最终生效值，供 [REQ-006 调试 trace](../../01-requirements/requirements.md) 查看                                                                                        |
| 测试要求     | `AgentModelParametersTests.cs`：(1) 全部字段为 `null` 合法（全用默认）；(2) 边界值（`Temperature=0.0/2.0`，`TopP=0.0/1.0`，`MaxTokens=1/128000`）；(3) record equality                                                                                                                        |

### 3.7 `AgentRuntime/AgentToolDefinition.cs` / `AgentToolCallRecord.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/AgentRuntime/AgentToolDefinition.cs`（含 `AgentToolCallRecord`，同文件两个紧密相关的小 DTO，参照 [HD-001 §2](HD-001-Inkwell.Abstractions-foundation.md) 单文件多小类型的既有做法）                                                                                                                                                                                                                                                          |
| 职责         | `AgentToolDefinition`：[REQ-007 工具调用](../../01-requirements/requirements.md) 的工具描述 + 同进程调用委托（"后端注册工具，运行期由后端代理调用"）；`AgentToolCallRecord`：非流式路径下单次工具调用的回溯记录（[picker Q-tool-result-passthrough=A](#13-关键决策摘要)）                                                                                                                                                                                                 |
| 对外接口     | `public sealed record AgentToolDefinition { public required string Name { get; init; } public required string Description { get; init; } public required string ParametersJsonSchema { get; init; } public required Func<string, CancellationToken, Task<string>> InvokeAsync { get; init; } }`；`public sealed record AgentToolCallRecord { public required string ToolCallId { get; init; } public required string ToolName { get; init; } public required string ArgumentsJson { get; init; } public required string ResultJson { get; init; } public required bool IsError { get; init; } }` |
| 内部函数或类 | `AgentToolDefinition.InvokeAsync` 入参 `string`（`ArgumentsJson`，工具调用参数的 JSON 文本）+ `CancellationToken`，返回 `Task<string>`（`ResultJson`）；委托仅同进程内传递，不跨网络序列化（[picker Q-tool-definition-shape=A](#13-关键决策摘要)）                                                                                                                                                                                                                          |
| 输入数据     | `AgentToolDefinition`：`Name` / `Description` / `ParametersJsonSchema`（[JSON Schema](https://json-schema.org/) 文本，供模型理解参数形态）/ `InvokeAsync` 委托（由 `Inkwell.Core.Tools` 业务层提供实现，桥接到具体工具执行逻辑）；`AgentToolCallRecord`：由实现层在工具调用完成后填充                                                                                                                                                                                    |
| 输出数据     | 对应 record 实例                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| 依赖模块     | System.*（`Func<>` / `Task<>`）                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| 错误处理     | `Name` / `Description` / `ParametersJsonSchema` 为 null/empty → `ArgumentException`；`InvokeAsync` 为 `null` → `ArgumentNullException`；工具执行期抛出的异常由实现层捕获并转换为 `AgentToolCallRecord.IsError = true` + `ResultJson` 携带错误描述（**不**向上抛出中断整个 Run，工具失败按 [`EX-003`](../../01-requirements/requirements.md) 处理，属业务层职责，本 DTO 仅承载结果）                                                                                    |
| 日志要求     | `agentruntime.tool_call_count` 字段统计 `ToolCalls.Count`；`ArgumentsJson` / `ResultJson` 原文**不得**进 OTel（可能含业务敏感参数，详 §7.2），仅记录 `ToolName` + 是否出错                                                                                                                                                                                                                                                                                                 |
| 测试要求     | `AgentToolDefinitionTests.cs` / `AgentToolCallRecordTests.cs`：(1) 必填字段校验；(2) `InvokeAsync` 委托可正常调用并返回值；(3) `AgentToolCallRecord.IsError = true` 场景的构造合法性；(4) record equality（`InvokeAsync` 委托字段参与 record 默认引用相等比较，测试中需显式说明不可依赖委托 value equality）                                                                                                                                                             |

### 3.8 `AgentRuntime/AgentRunEvent.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| ------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/AgentRuntime/AgentRunEvent.cs`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| 职责         | `RunTurnStreamingAsync` 产出的流式事件封闭子类型族；1:1 对应 [ADR-012](../../03-architecture/adr/ADR-012-client-server-protocol-rest-agui.md) AG-UI 的 message / tool_call / state_delta / lifecycle 四大类事件（[picker Q-streaming-event-shape=A](#13-关键决策摘要)），供 `Inkwell.WebApi` 的 AG-UI hosting 层 1:1 映射转发                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
| 对外接口     | `public abstract record AgentRunEvent { public required string RunId { get; init; } }`；`public sealed record TextDelta : AgentRunEvent { public required string DeltaText { get; init; } }`；`public sealed record ToolCallRequested : AgentRunEvent { public required string ToolCallId { get; init; } public required string ToolName { get; init; } public required string ArgumentsJson { get; init; } }`；`public sealed record ToolCallResult : AgentRunEvent { public required string ToolCallId { get; init; } public required string ResultJson { get; init; } public required bool IsError { get; init; } }`；`public sealed record StateDelta : AgentRunEvent { public required string StateJsonPatch { get; init; } }`；`public sealed record RunCompleted : AgentRunEvent { public required AgentTurnResult Result { get; init; } }`；`public sealed record RunError : AgentRunEvent { public required string ErrorMessage { get; init; } public required string ExceptionType { get; init; } }` |
| 内部函数或类 | 6 个 `sealed record`；`TextDelta` ↔ AG-UI `message`；`ToolCallRequested` / `ToolCallResult` ↔ AG-UI `tool_call`；`StateDelta` ↔ AG-UI `state_delta`；`RunCompleted` / `RunError` ↔ AG-UI `lifecycle`（`RunCompleted` 复用 `AgentTurnResult`，保证流式路径终态与非流式路径字段一致）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| 输入数据     | 由 `Inkwell.Core.AgentRuntime` 实现层在消费 MAF `AgentResponseUpdate` 流时逐条转换产出（详 §4.4 防泄漏转换）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| 输出数据     | 对应 `sealed record` 实例，通过 `IAsyncEnumerable<AgentRunEvent>` 逐条产出                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| 依赖模块     | `AgentTurnResult.cs`（`RunCompleted` 复用）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| 错误处理     | DTO 自身不产生异常；`RunError` 事件本身**是**错误的 DTO 表达（枚举结束前的终态事件），不是异常路径——流式枚举遇到不可恢复错误时应产出 `RunError` 事件后正常结束枚举，而非抛异常打断 `await foreach`（与 AG-UI `event: error` 语义对齐，[ADR-012 §决策](../../03-architecture/adr/ADR-012-client-server-protocol-rest-agui.md)）；仅 `ArgumentException` / `OperationCanceledException` 类程序性错误才通过异常路径中断枚举（详 §4.2）                                                                                                                                                                                                                                                                                                                                                                                                                       |
| 日志要求     | 每条事件产出时实现层写子 span（详 §4.3）；`TextDelta.DeltaText` / `ToolCallRequested.ArgumentsJson` / `ToolCallResult.ResultJson` 原文**不得**进 OTel（详 §7.2）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
| 测试要求     | `AgentRunEventTests.cs`：(1) 6 个子类型均可构造；(2) 模式匹配（`switch` 表达式 exhaustive 覆盖 6 种子类型，编译期 [`CS8509`](https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/cs8509) 警告作为非穷尽性提示）；(3) `RunCompleted.Result` 与非流式 `AgentTurnResult` 字段等价性；(4) record equality                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |

### 3.9 `AgentRuntime/AgentRuntimeOptions.cs`

> [HD-001 §3.11.1 `InkwellProvidersOptions`](HD-001-Inkwell.Abstractions-foundation.md#3111-optionsinkwellprovidersoptionscsf9-新增) 已承载 Provider 选择器 `Inkwell:Providers:AgentRuntime`（默认值 `"AzureOpenAI"`）；本 Options **不**重复承载 Provider 字段，也**不**承载 Azure OpenAI 端点 / 密钥等具体凭证——[ADR-017 §依赖规则](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) 下 `Inkwell.AgentRuntime` 合并进 `Inkwell.Core.AgentRuntime` 命名空间（不设独立 `providers/*` csproj），凭证类子 Options 由该命名空间的独立 HD 起草并挂载到自己的配置段（如 `Inkwell:AgentRuntime:AzureOpenAI`），与 [HD-004](HD-004-Inkwell.Abstractions-cache-port.md#9-部署--配置) / [HD-005](HD-005-Inkwell.Abstractions-queue-port.md#9-部署--配置) 凭证隔离模式一致。

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                        |
| ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/AgentRuntime/AgentRuntimeOptions.cs`                                                                                                                                                                                                                                                                                                                          |
| 职责         | Agent Runtime 端口详细配置；从 `appsettings.json` `"Inkwell:AgentRuntime"` 段绑定                                                                                                                                                                                                                                                                                                            |
| 对外接口     | `public sealed class AgentRuntimeOptions { [Range(0.0, 2.0)] public double DefaultTemperature { get; init; } = 1.0; [Range(0.0, 1.0)] public double DefaultTopP { get; init; } = 1.0; [Range(1, 128000)] public int DefaultMaxTokens { get; init; } = 2048; [Range(1, 3600)] public int RunTimeoutSeconds { get; init; } = 300; public bool EnableSensitiveDataLogging { get; init; } = false; }` |
| 内部函数或类 | DataAnnotations 校验；三个模型参数默认值 = Azure OpenAI SDK 默认值 + `MaxTokens` 安全上限（[picker Q-model-params-defaults=A](#13-关键决策摘要)）；`RunTimeoutSeconds` 默认 300（[picker Q-run-timeout=A](#13-关键决策摘要)）                                                                                                                                                              |
| 输入数据     | 由 `IConfiguration` 绑定                                                                                                                                                                                                                                                                                                                                                                    |
| 输出数据     | `AgentRuntimeOptions` 实例（DI 通过 `IOptions<AgentRuntimeOptions>` 注入）                                                                                                                                                                                                                                                                                                                  |
| 依赖模块     | `System.ComponentModel.DataAnnotations`                                                                                                                                                                                                                                                                                                                                                     |
| 错误处理     | DataAnnotations 校验失败 → `OptionsValidationException`，host 兜底；具体模型 Provider 凭证缺失由 `Inkwell.Core.AgentRuntime` 自己的子 Options Validator 负责，不在本文件                                                                                                                                                                                                                   |
| 日志要求     | DI 启动期 `IValidateOptions` 失败时输出 OTel `exception.type=Microsoft.Extensions.Options.OptionsValidationException`（[HD-001 §5.3](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)）                                                                                                                                                                                     |
| 测试要求     | `AgentRuntimeOptionsTests.cs`：默认值（1.0 / 1.0 / 2048 / 300 / false）、`appsettings.json` 绑定、`[Range]` 边界（`Temperature` 0.0/2.0、`TopP` 0.0/1.0、`MaxTokens` 1/128000、`RunTimeoutSeconds` 1/3600，越界）                                                                                                                                                                          |

### 3.10 `AgentRuntime/AgentRuntimeOptionsValidator.cs`

| 字段         | 内容                                                                                                                                                                     |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/AgentRuntime/AgentRuntimeOptionsValidator.cs`                                                                                              |
| 职责         | `IValidateOptions<AgentRuntimeOptions>` 实现；DataAnnotations 校验（无跨字段约束——四个数值参数互相独立，不同 `CacheOptions` 的 `MinTtl <= MaxTtl` 存在依赖关系）        |
| 对外接口     | `internal sealed class AgentRuntimeOptionsValidator : IValidateOptions<AgentRuntimeOptions> { public ValidateOptionsResult Validate(string? name, AgentRuntimeOptions options); }` |
| 内部函数或类 | `Validator.TryValidateObject` DataAnnotations；模型 Provider 特定凭证不在本 Validator                                                                                    |
| 输入数据     | `AgentRuntimeOptions` 实例                                                                                                                                               |
| 输出数据     | `ValidateOptionsResult.Success` / `Fail(IEnumerable<string>)`                                                                                                            |
| 依赖模块     | `Microsoft.Extensions.Options` / `System.ComponentModel.DataAnnotations`                                                                                                  |
| 错误处理     | 同 [HD-001 §3.12](HD-001-Inkwell.Abstractions-foundation.md#312-optionsinkwelloptionsvalidatorcs)，校验失败 → `Fail` 含全部消息                                          |
| 日志要求     | 失败由 `OptionsValidationException` 抛出，host 打 fatal                                                                                                                  |
| 测试要求     | `AgentRuntimeOptionsValidatorTests.cs`：(1) DataAnnotations 边界合格；(2) 默认值（1.0 / 1.0 / 2048 / 300 / false）通过；(3) 各字段单独越界均被拒                        |

## 4. BCL 异常与日志（端口段补充 HD-001 §4）

> **错误处理路径**：本端口与业务命名空间统一采用裸 `Task<T>` + .NET BCL 异常。具体 BCL 异常映射 + OTel `exception.*` 五字段详见下表与 [HD-001 §5.3](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)。

### 4.1 错误码

本端口**不分配** `INK-AGENTRUNTIME-NNN` 错误码。与 [HD-002](HD-002-Inkwell.Abstractions-persistence-port.md) / [HD-003](HD-003-Inkwell.Abstractions-file-storage-port.md) / [HD-004](HD-004-Inkwell.Abstractions-cache-port.md) / [HD-005](HD-005-Inkwell.Abstractions-queue-port.md) 最终态一致，错误语义全部走 BCL 异常类型表达 + OTel `exception.*` 五字段。

### 4.2 BCL 异常分类（业务失败 vs 程序错误）

按 [HD-001 §5.3 BCL 对照表](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表) 的分类语义：

- **预期返回值（不是异常，调用方按值判断）**：
  - `CancelRunAsync` 对未知 / 已结束 `runId` → 返回 `false`（幂等）
  - `RunTurnStreamingAsync` 遇到不可恢复错误 → 产出 `RunError` 事件后正常结束枚举（**不**抛异常，详 §3.8）
- **业务失败 / 预期错误**（调用方应 try/catch 并按业务策略处理，**不**触发 P1 告警）：
  - `KeyNotFoundException`：`AgentRunRequest.AgentId` 对应的 Agent 不存在（实现层查内部缓存 / `IPersistenceProvider` 判定后转换）
  - `ArgumentException`：`request.Messages` 为空且 `Instructions` 为 `null`（构造期已拦截，运行期防御性重复校验）
- **程序错误 / 失血告警**（运维介入修复，P1 / P2 告警）：
  - `IOException`：模型 Provider 侧网络故障 / 鉴权失败（如 Azure OpenAI `401` / `403`）/ 限流（`429`，SDK 提供表达性子类时优先原样上抛，如未来接入的 [`Azure.RequestFailedException`](https://learn.microsoft.com/dotnet/api/azure.requestfailedexception)）
  - `TimeoutException`：单轮 Run 超过 `AgentRuntimeOptions.RunTimeoutSeconds`（facade 层安全超时，非模型推理延迟预算，详 §7.1）
- **参数 / 取消错误**（调用方 bug，应在测试期暴露）：
  - `ArgumentException` / `ArgumentNullException`：`RunId` / `AgentId` / `Messages` 非法（详 §3.2 构造期校验）
  - `OperationCanceledException`：全部方法响应 `ct`（[HD-001 §4.3](HD-001-Inkwell.Abstractions-foundation.md#43-取消传播)）；`RunTurnStreamingAsync` 的流式枚举在 `ct` 触发时立即终止枚举（同 [HD-005 `DequeueAsync`](HD-005-Inkwell.Abstractions-queue-port.md#42-bcl-异常分类业务失败-vs-程序错误) 取消惯例）；用户主动 `CancelRunAsync` 触发的中断，实现层内部通过 `CancellationTokenSource.Cancel()` 使正在执行的 `RunTurnAsync` / `RunTurnStreamingAsync` 抛 `OperationCanceledException`，与被动 `ct` 取消走同一异常路径（详 §4.4 CancelRunAsync 内部机制）

### 4.3 OTel span / 字段

每个方法在实现层（`Inkwell.Core.AgentRuntime`）按 [picker Q-otel](#13-关键决策摘要) 输出 span：

- `agentruntime.run_turn` ← `RunTurnAsync`
- `agentruntime.run_turn_streaming` ← `RunTurnStreamingAsync`（每次 `AgentRunEvent` 产出各起一个子 span，而非整个枚举生命周期一个 span，与 [HD-005 `queue.dequeue`](HD-005-Inkwell.Abstractions-queue-port.md#43-otel-span--字段) 处理方式一致）
- `agentruntime.cancel_run` ← `CancelRunAsync`

**Inkwell 私有字段**（6 个）：

- `agentruntime.agent_id`（`AgentRunRequest.AgentId`）
- `agentruntime.run_id`（`AgentRunRequest.RunId` / `AgentRunEvent.RunId`）
- `agentruntime.conversation_id`（`AgentRunRequest.ConversationId`，可能为空）
- `agentruntime.model_id`（`AgentTurnResult.ModelIdUsed` 或流式路径实现层缓存的等价值）
- `agentruntime.tool_call_count`（`AgentTurnResult.ToolCalls.Count` 或流式路径累计的 `ToolCallRequested` 事件数）
- `agentruntime.operation_outcome`：值域按方法区分——
  - `RunTurnAsync` / `RunTurnStreamingAsync`：`completed` / `failed` / `cancelled`
  - `CancelRunAsync`：`cancelled` / `not_found`

**OTel 标准字段**（5 个，按 [`exception.*` 语义约定](https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/)，仅异常路径填充）：

- `exception.type`（如 `System.IO.IOException` / `System.TimeoutException` / `System.OperationCanceledException`）
- `exception.message`
- `exception.stacktrace`
- `exception.escaped`
- `exception.id`（[`Guid.CreateVersion7()`](https://learn.microsoft.com/dotnet/api/system.guid.createversion7) 生成，便于 Grafana / Tempo 跨 span 关联，与 [HD-004 §4.3](HD-004-Inkwell.Abstractions-cache-port.md#43-otel-span--字段) / [HD-005 §4.3](HD-005-Inkwell.Abstractions-queue-port.md#43-otel-span--字段) 一致）

> **PII 提示**：`AgentChatMessage.Content`（`TextPart.Text` / 图片与文档的实际内容）、`AgentToolDefinition` / `AgentToolCallRecord` 的 `ArgumentsJson` / `ResultJson` **不得**进入任何 OTel 字段——这些字段可能承载用户输入 / Instructions / 工具执行的业务敏感数据。需要观测内容规模时仅追加 `agentruntime.message_content_length`（字符数而非内容），与 [HD-005 §4.3](HD-005-Inkwell.Abstractions-queue-port.md#43-otel-span--字段) 消息载荷不进 OTel 的处理方式一致。[REQ-014 调试 trace](../../01-requirements/requirements.md) 要求"模型调用入参在 trace 中可见"——该可见性由 `Inkwell.Core.Traces` 业务模块通过**独立的、脱敏后**的调试样本存储实现（非 OTel 全局可观测性通道），本 HD 不承担该职责，仅保证 `AgentTurnResult` / `AgentRunEvent` DTO 里的字段完整可供业务层读取。

### 4.4 MAF 类型防泄漏机制（本 HD 最核心约束的落地示例）

`Inkwell.Core.AgentRuntime` 命名空间的 `IAgentRuntime` 实现内部负责本 HD 全部 DTO ↔ MAF 类型的双向转换，转换逻辑**不**在 `Inkwell.Abstractions` 内（端口层零 MAF 依赖）。示例（供 `Inkwell.Core.AgentRuntime` 独立 HD 起草时参照）：

```csharp
// 位于 Inkwell.Core.AgentRuntime 命名空间（唯一允许 using Microsoft.Agents.AI.* 的位置）
// using Microsoft.Agents.AI;
// using Microsoft.Extensions.AI;

internal sealed class AzureOpenAIAgentRuntime : IAgentRuntime
{
    public async Task<AgentTurnResult> RunTurnAsync(AgentRunRequest request, CancellationToken ct = default)
    {
        // 1. Inkwell DTO → MAF 类型（仅在本命名空间内可见）
        var mafMessages = request.Messages.Select(ToChatMessage).ToList(); // AgentChatMessage → ChatMessage
        var mafAgent = ResolveAgent(request.AgentId);                      // 内部缓存/工厂 → AIAgent
        var mafOptions = ToAgentRunOptions(request.ModelParameters);       // AgentModelParameters → AgentRunOptions

        // 2. 调用 MAF（MAF 类型仅存在于本方法作用域内）
        AgentResponse mafResponse = await mafAgent.RunAsync(mafMessages, session: null, mafOptions, ct);

        // 3. MAF 类型 → Inkwell DTO（跨越方法边界前必须转换完毕）
        return ToAgentTurnResult(request.RunId, mafResponse); // AgentResponse → AgentTurnResult
    }

    // 私有转换方法：AIAgent / AgentSession / ChatMessage / AgentResponse / AgentResponseUpdate
    // 均只出现在 private/internal 方法签名里，绝不作为 public 方法的参数或返回值
}
```

**CI 自检**（详 §10 Q1）：`rg -n -e 'Microsoft\.Agents\.AI' -e '\bAIAgent\b' -e '\bAgentSession\b' -e '\bChatMessage\b' -e '\bAgentResponse\b' -e '\bAgentResponseUpdate\b' -e '\bAgentRunOptions\b' src/core/Inkwell.Abstractions/AgentRuntime/` 期望 0 行——本 HD 目录下任何文件都不得出现这些标识符。

## 5. 公共约定继承（HD-001）

### 5.1 命名

- `IAgentRuntime` ↔ [HD-001 §5.1](HD-001-Inkwell.Abstractions-foundation.md#51-命名) `I<Capability>Provider`（本端口沿用既有的 `IAgentRuntime` 命名，[AGENTS.md §3.1](../../../AGENTS.md) 已锁定该名称，不套用 `I<Capability>Provider` 后缀模式，与 `IPersistenceProvider` / `ICacheProvider` / `IQueueProvider` 略有差异，属既定命名的例外记录，同 [HD-004 §1.4](HD-004-Inkwell.Abstractions-cache-port.md#14-与-hd-001-51--52-命名约定的一致性声明) / [HD-005 §1.4](HD-005-Inkwell.Abstractions-queue-port.md#14-与-hd-001-51--52-命名约定的一致性声明) 领域例外记录方式）
- `RunTurnAsync` / `RunTurnStreamingAsync` / `CancelRunAsync` ↔ §5.1 异步方法以 `Async` 结尾
- `AgentRunRequest` / `AgentTurnResult` ↔ §5.1 DTO 命名（非 `<Action><Entity>Request/Response` 模式，因本 DTO 是端口级请求 / 响应载体而非单一 CRUD 操作，与 [HD-005 `MessageEnvelope`](HD-005-Inkwell.Abstractions-queue-port.md#51-命名) 同类偏差记录方式）
- `AgentRuntimeOptions` ↔ §5.1 `<Provider>Options`

### 5.2 签名

- 3 个方法走裸 `Task<T>` / `Task<bool>` / `IAsyncEnumerable<T>` + BCL 异常，↔ [HD-001 §5.3 BCL 对照表](HD-001-Inkwell.Abstractions-foundation.md#53-bcl-异常类型对照表)
- `RunTurnStreamingAsync` 是流式签名 ↔ [HD-001 §5.2 流式签名约定](HD-001-Inkwell.Abstractions-foundation.md#52-签名)（`IAsyncEnumerable<T>` + `[EnumeratorCancellation]`）
- `CancelRunAsync` 的 `bool` 返回值保留幂等语义（同 [HD-004 `ReleaseLockAsync`](HD-004-Inkwell.Abstractions-cache-port.md#52-签名) / [HD-005 `AcknowledgeAsync`](HD-005-Inkwell.Abstractions-queue-port.md#52-签名) 风格）
- `CancellationToken ct = default` 全 3 方法必填 ↔ [HD-001 §4.3](HD-001-Inkwell.Abstractions-foundation.md#43-取消传播)

### 5.3 错误处理

- 业务失败 / 预期错误 → `KeyNotFoundException`（Agent 不存在）/ `ArgumentException`（请求非法）
- 程序错误 / 失血告警 → BCL 程序异常（`IOException` / `TimeoutException`）；触发运维告警
- 幂等确认型返回 → `CancelRunAsync` 返回值本身表达"已中断 / 未知 Run"语义，不抛异常
- 取消 → `OperationCanceledException`
- 实现层用 [`ActivitySource.StartActivity`](https://learn.microsoft.com/dotnet/api/system.diagnostics.activitysource.startactivity) 创建 span 后，**异常路径**自动用 `Activity.RecordException` 或 `Activity.SetStatus(ActivityStatusCode.Error, message)` 写入 `exception.*` 五字段（详 §4.3）

## 6. Builder DSL 钩子（给 `Inkwell.Core.AgentRuntime` 的契约）

`Inkwell.Core.AgentRuntime` 命名空间提供唯一入口扩展方法（无独立 Provider csproj，[ADR-017 §依赖规则](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md)）：

```csharp
// src/core/Inkwell.Core/AgentRuntime/AzureOpenAIAgentRuntimeBuilderExtensions.cs
public static class AzureOpenAIAgentRuntimeBuilderExtensions
{
    public static IInkwellBuilder UseAzureOpenAIAgentRuntime(
        this IInkwellBuilder builder,
        Action<AzureOpenAIAgentRuntimeOptions> configure);
}
```

扩展方法**必须**：(1) 校验入参非 null；(2) 调用 `builder.Services.AddSingleton<IAgentRuntime, AzureOpenAIAgentRuntime>()`；(3) 注册 `IValidateOptions<AgentRuntimeOptions>` + `AzureOpenAIAgentRuntimeOptions`（凭证子 Options）的 Validator；(4) 与 `InkwellProvidersOptions.AgentRuntime` 取值交叉校验（不一致抛 `InkwellBuilderException`，[HD-001 §3.13](HD-001-Inkwell.Abstractions-foundation.md) 锁定的 BCL 程序错误子类）；(5) 返回 `builder`。

> **未来多模型 Provider 扩展**：[ADR-003](../../03-architecture/adr/ADR-003-agent-engine-microsoft-agent-framework.md) 预留 OpenAI / Claude / Qwen / 智谱接入位；后续每新增一个模型云服务，`Inkwell.Core.AgentRuntime` 命名空间下新增对应 `UseXxxAgentRuntime()` 扩展方法即可，`IAgentRuntime` 接口本身**不需要**变更（`AgentRunRequest.ModelId` 已支持指定具体模型标识）。v1 仅交付 Azure OpenAI 真实实现，其余 Provider 为 v2 backlog（[ADR-003 后果](../../03-architecture/adr/ADR-003-agent-engine-microsoft-agent-framework.md)）。

## 7. 性能 / 安全 / 可观测性

### 7.1 性能预算（[picker Q-perf-budget=A 宽松档](#13-关键决策摘要)）

| 方法                       | 预算类型        | P50    | P99     | 备注                                                       |
| --------------------------- | --------------- | ------ | ------- | ------------------------------------------------------------ |
| `RunTurnAsync`             | facade overhead | < 50ms | < 200ms | 不含模型推理延迟；仅统计请求组装 + 工具调用编排的框架开销 |
| `RunTurnStreamingAsync`    | facade overhead | < 50ms | < 200ms | 同上，指首个事件产出前的框架开销，不含模型首 token 延迟   |
| `CancelRunAsync`           | facade overhead | < 20ms | < 100ms | 内部 `ConcurrentDictionary` 查找 + `CancellationTokenSource.Cancel()` |

> 模型推理延迟（首 token / 完整响应耗时）**不在本 HD 预算范围**——依赖具体模型 Provider（Azure OpenAI 区域 / 部署容量）与网络状况，由 `Inkwell.Core.AgentRuntime` 独立 HD 或运维层面另行监控（详 §7.3 建议告警维度）。

### 7.2 安全

- `AgentRuntimeOptions.EnableSensitiveDataLogging` 默认 `false`；启用后仅追加 `agentruntime.message_content_length`（长度而非内容）——**对话内容 / 工具调用参数与结果本身永不进入 OTel**（详 §4.3 PII 提示）
- Azure OpenAI 凭证（Endpoint / ApiKey / DeploymentName）由 `Inkwell.Core.AgentRuntime` 自己的子 Options（如 `AzureOpenAIAgentRuntimeOptions`）承载，**不**在本 `AgentRuntimeOptions`；走 [K8s Secret](https://kubernetes.io/docs/concepts/configuration/secret/) / Compose `.env`（[OQ-A006 closed §B](../../03-architecture/open-questions-arch.md)，v1 不引 Azure Key Vault）
- `AgentToolDefinition.InvokeAsync` 委托由 `Inkwell.Core.Tools` 业务层提供，工具执行期的权限校验（如某工具是否需要额外授权）是该业务层职责，本 HD 不重复约束
- [REQ-016 多模态](../../01-requirements/requirements.md) 的 `ImagePart` / `DocumentPart` 引用的对象由 [`IFileStorageProvider`](HD-003-Inkwell.Abstractions-file-storage-port.md) 存储与鉴权，本 HD 仅传递 `Uri` 引用，不重复实现文件访问控制

### 7.3 可观测性

- 6 私有 + 5 OTel 标准 `exception.*` 字段进 OTel；本 HD 不锁告警规则（H4 [TestCaseAuthor](../../../.github/agents/h4-test-case-author.agent.md) 反推时锁），但建议告警维度：
  - `exception.type` ∈ {`System.IO.IOException`, `System.TimeoutException`} 速率 > 5/min → P1（模型 Provider 侧连接 / 超时类失血）
  - `agentruntime.operation_outcome = failed` 速率异常升高 → P2（模型调用持续失败，可能是配额耗尽 / 部署下线）
  - 建议追加模型推理延迟维度指标（`agentruntime_model_latency_p50` / `agentruntime_model_latency_p99`，非本 HD 端口层职责，留 `Inkwell.Core.AgentRuntime` HD 起草时锁定，与 [HD-005 队列可靠性五项残余指标](HD-005-Inkwell.Abstractions-queue-port.md#73-可观测性) 处理方式一致——本 HD 仅记录索引）
  - **跨事件 trace correlation**：`agentruntime.run_id` 应与 [REQ-014 trace 全链路](../../01-requirements/requirements.md) 的顶层 trace 关联，使同一 Run 的 `agentruntime.run_turn_streaming` 子 span、工具调用、模型调用（MAF 自带 OTel instrumentation，[ADR-003 后果](../../03-architecture/adr/ADR-003-agent-engine-microsoft-agent-framework.md)）在同一条 trace 内呈现

## 8. 测试要求

### 8.1 单元测试（本 HD 范围内）

- 测试项目：`tests/core/Inkwell.Abstractions.Tests/AgentRuntime/`（与 HD-001 同 csproj，[MSTest.Sdk 4.x](https://github.com/microsoft/testfx) + MTP runner）
- 每个文件至少一个 `*Tests.cs` 配对（见 §3 各小节"测试要求"）
- 覆盖率门槛：DTO 类（`AgentRunRequest` / `AgentChatMessage` / `AgentMessageContentPart` / `AgentModelParameters` / `AgentToolDefinition` / `AgentToolCallRecord` / `AgentRunEvent`）≥ 95%；`AgentRuntimeOptions` + Validator ≥ 90%；`IAgentRuntimeContractTests` 仅锁 ABI ≥ 100%

### 8.2 契约测试

- 接口 ABI 用 [`PublicApiAnalyzers`](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) 锁定
- `IAgentRuntime` 形态变更 → 需新建 ADR + 影响 `Inkwell.Core.AgentRuntime` 实现 HD
- **MAF 类型防泄漏契约测试**（本 HD 专属，其他端口无此项）：`tests/core/Inkwell.Abstractions.Tests/AgentRuntime/NoMafTypeLeakageTests.cs` 用反射遍历 `IAgentRuntime` 全部公共方法的参数 / 返回类型 + 全部 DTO 公共属性类型，断言程序集全名不以 `Microsoft.Agents.AI` 开头

### 8.3 集成测试

- 本 HD **不**起集成测试（端口层无外部依赖）
- `Inkwell.Core.AgentRuntime` 行为测试在 `tests/core/Inkwell.Providers.Contract/AgentRuntime/`（跨 Provider 家族契约包，与 [HD-002 §8](HD-002-Inkwell.Abstractions-persistence-port.md) / [HD-003 §8.3](HD-003-Inkwell.Abstractions-file-storage-port.md) / [HD-004 §8.3](HD-004-Inkwell.Abstractions-cache-port.md) / [HD-005 §8.3](HD-005-Inkwell.Abstractions-queue-port.md) 拓扑一致），由 `Inkwell.Core.AgentRuntime` HD 联合起草；集成测试须覆盖：
  - 同步 / 流式两条路径产出的最终文本 + 工具调用记录一致性（[picker Q-tool-result-passthrough=A](#13-关键决策摘要)）
  - `CancelRunAsync` 对进行中 Run 的实际中断效果（模拟长耗时工具调用场景）+ 对未知 `runId` 返回 `false`
  - `RunTimeoutSeconds` 超时后抛 `TimeoutException`
  - `ModelParameters` 未提供时正确套用 `AgentRuntimeOptions` 默认值，且在 `AgentTurnResult.ModelParametersUsed` 中可见（[REQ-006 验收标准](../../01-requirements/requirements.md)）
  - `ImagePart` / `DocumentPart` 引用能被模型正确处理（[REQ-016](../../01-requirements/requirements.md)，需真实 Azure OpenAI 视觉模型部署或 mock `IChatClient`）

### 8.4 BannedSymbols（CI 强制）

- `Inkwell.Abstractions.AgentRuntime.*` 禁用引入 `Microsoft.Agents.AI.*` 等 MAF 命名空间（写在 `BannedSymbols.txt`，违反 → 编译阻塞，详 §4.4）
- 其他业务命名空间（除 `Inkwell.Core.AgentRuntime` 外）同样禁用 `Microsoft.Agents.AI.*`（[AGENTS.md §3.2](../../../AGENTS.md) 已锁，本 HD §10 Q1 重复声明该检查项以便独立验证）

## 9. 部署 / 配置

`Inkwell.Abstractions.csproj` 与端口层一同打镜像（无独立部署）。`appsettings.json` 顶层段：

```json
{
  "Inkwell": {
    "Providers": {
      "AgentRuntime": "AzureOpenAI"
    },
    "AgentRuntime": {
      "DefaultTemperature": 1.0,
      "DefaultTopP": 1.0,
      "DefaultMaxTokens": 2048,
      "RunTimeoutSeconds": 300,
      "EnableSensitiveDataLogging": false,
      "AzureOpenAI": {
        "Endpoint": "...",
        "ApiKey": "...",
        "DeploymentName": "..."
      }
    }
  }
}
```

> `AgentRuntime:AzureOpenAI` 子段（凭证 / 部署名）由 `Inkwell.Core.AgentRuntime` 独立 HD 起草时锁定字段细节，本 HD 仅在示例中占位说明其存在位置，不锁定字段名。

## 10. CI 自检命令（grep 列表）

| 编号 | 检查项                                                                                    | 命令（CI [GitHub Actions](https://docs.github.com/actions) 工作流引用）                                                                                                                                     |
| ---- | ------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Q1   | `Inkwell.Abstractions/AgentRuntime/` 目录禁 MAF 类型标识符（本 HD 核心约束）              | `rg -n -e 'Microsoft\.Agents\.AI' -e '\bAIAgent\b' -e '\bAgentSession\b' -e '\bChatMessage\b' -e '\bAgentResponse\b' -e '\bAgentResponseUpdate\b' -e '\bAgentRunOptions\b' src/core/Inkwell.Abstractions/AgentRuntime/` 期望 0 行 |
| Q2   | 业务命名空间（除 `Inkwell.Core.AgentRuntime`）禁直接 `using Microsoft.Agents.AI`           | `rg -n -e 'using\s+Microsoft\.Agents\.AI' src/core/Inkwell.Core/ --glob '!src/core/Inkwell.Core/AgentRuntime/**'` 期望 0 行                                                                                     |
| Q3   | `IAgentRuntime` 接口签名稳定                                                              | [PublicApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) `PublicAPI.Shipped.txt` diff                                                       |
| Q4   | 端口层无 `Task<Result<` 残留（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)） | `rg -n -e 'Task<Result<' -e 'Task<Result>' src/core/Inkwell.Abstractions/AgentRuntime/` 期望 0 行                                                                                                               |
| Q5   | 对话内容 / 工具调用参数不进 OTel（仅长度字段）                                            | `rg -n -e '"agentruntime\.message_content"' -e 'agentruntime\.message_content\s*=' src/core/` 期望 0 行（仅 `agentruntime.message_content_length` 允许）                                                       |
| Q6   | OTel span 字段名一致                                                                      | `rg -n -e '"agentruntime\.agent_id"' -e '"agentruntime\.run_id"' -e '"agentruntime\.conversation_id"' -e '"agentruntime\.model_id"' -e '"agentruntime\.tool_call_count"' -e '"agentruntime\.operation_outcome""' src/core/` 期望全部在实现层覆盖 |

## 11. 待补 / 待评审

以下条目为本轮起草后仍待后续 HD / H4 明确的开放事项，均已在 §1.2 范围声明为"不在本 HD 内"，此处仅作追踪索引：

- **`AgentChatMessage(Role = Tool)` 的 `Content` 语义**：`Tool` 角色消息是否携带 `ToolCallId` 关联字段（当前 §3.4 `AgentChatMessage` 未显式包含 `ToolCallId` 属性，工具调用关联完全依赖 `AgentToolCallRecord` / `ToolCallRequested`/`ToolCallResult` 事件的 `ToolCallId`）——若后续实现发现历史消息重放场景需要在 `AgentChatMessage` 本身携带 `ToolCallId`，需回补一次 errata；本 HD 暂不预判
- **`Inkwell.Core.AgentRuntime` 内部会话状态管理**：`AgentSession` 序列化 / 反序列化策略（是否每轮都从 `Messages` 全量重建 MAF 会话，还是缓存 `AgentSession` 实例）是纯实现细节，留独立 HD 决定，不影响本 HD 端口签名
- **`RunCompleted` 事件与流式路径下 `RunTimeoutSeconds` 的交互**：若超时发生在流式枚举中途，应产出 `RunError` 事件还是直接抛 `TimeoutException` 打断 `await foreach`——本 HD §4.2 / §3.8 已倾向"不可恢复错误走 `RunError` 事件"，但"超时"具体归类为可恢复业务错误还是不可恢复程序错误，留 `Inkwell.Core.AgentRuntime` HD 起草时结合实现细节最终确认，本 HD 不强制二选一
- **多 Agent 编排场景下 `IAgentRuntime` 的复用方式**：[REQ-012](../../01-requirements/requirements.md) DAG 编排如何调用多个 `IAgentRuntime.RunTurnAsync`（每节点一次调用 vs 批量调度）——留 `Inkwell.Core.Orchestrations` 业务 HD 决定，本 HD 仅保证单次 Run 语义完整
- **Skill 元数据注入 `Instructions` 的具体拼接规则**：[ADR-010](../../03-architecture/adr/ADR-010-skill-loading-static-only-v1.md) v1 静态加载，`SKILL.md` 内容如何拼入 `AgentRunRequest.Instructions`（直接字符串拼接 vs 结构化分段）——留 `Inkwell.Core.Skills` 业务 HD 决定，本 HD `Instructions` 字段仅为普通 `string?`，不预设拼接格式

## 12. 跨模块章节贡献

本 HD 在以下跨模块文件中追加一级章节 `## Inkwell.Abstractions.AgentRuntime`：

- `docs/04-detailed-design/file-structure.md` — 新增 `Inkwell.Abstractions/AgentRuntime/` 子目录树 + 累计文件计数更新
- `docs/04-detailed-design/database-design.md` — **不贡献**（端口层不直接接 DB；`AgentDefinition` 持久化已在 [HD-002](HD-002-Inkwell.Abstractions-persistence-port.md) 覆盖）

> 跨模块章节追加由本 HD 起草后**立即同步**到对应文件（**只追加**不改其他模块章节）。

## 13. 决策记录

### 13.1 起草期 picker 决策（2026-07-05）

| 字段                        | 选定值                                                                                                     | picker 时间 |
| --------------------------- | ------------------------------------------------------------------------------------------------------------ | ----------- |
| Q-facade-scope              | A：3 方法（RunTurnAsync / RunTurnStreamingAsync / CancelRunAsync），不引入端口层 Session 生命周期方法       | 2026-07-05  |
| Q-run-request-shape         | A：`AgentRunRequest` 携带全部已解析上下文，端口不反向依赖 `IPersistenceProvider`                            | 2026-07-05  |
| Q-streaming-event-shape     | A：抽象 record `AgentRunEvent` + 6 个封闭 sealed 子类型，1:1 对应 AG-UI 四大类事件                          | 2026-07-05  |
| Q-cancel-mechanism          | A：内部 `ConcurrentDictionary<string, CancellationTokenSource>` 按 `RunId` 索引，幂等 bool 返回              | 2026-07-05  |
| Q-perf-budget               | A：宽松档，仅 facade 开销，不含模型推理延迟                                                                | 2026-07-05  |
| Q-otel                      | A：`agentruntime.<verb>` + 6 个私有字段                                                                    | 2026-07-05  |
| Q-tool-result-passthrough   | A：流式 + 非流式两条路径均携带完整工具调用记录                                                              | 2026-07-05  |
| Q-multimodal-representation | A：`Content` 为封闭子类型族（TextPart / ImagePart / DocumentPart），不设独立 AudioPart                      | 2026-07-05  |
| Q-model-params-defaults     | A：`DefaultTemperature=1.0` / `DefaultTopP=1.0` / `DefaultMaxTokens=2048`                                    | 2026-07-05  |
| Q-run-timeout               | A：`RunTimeoutSeconds` 默认 300（5 分钟）                                                                   | 2026-07-05  |
| Q-tool-definition-shape     | A：`AgentToolDefinition` 用 `string` 载 `ArgumentsJson` / `ResultJson`，与 `AgentRunEvent` 同形态           | 2026-07-05  |

### 13.2 候选与放弃理由

- **Q-facade-scope**：备选 B（增加 `CreateSessionAsync` / `DeserializeSessionAsync` 暴露 MAF `AgentSession` 生命周期）被否决——会强迫调用方（业务命名空间）管理不透明的会话序列化 blob，与 [ADR-011](../../03-architecture/adr/ADR-011-auto-lock-with-inflight-task-survival.md)"不持久化 AG-UI 事件流、不引入 RunEventStore"的简化方向不一致；`Inkwell.Conversations` 已经持久化对话消息历史（业务层职责），每次调用重新组装 `Messages` 传入即可，无需端口暴露会话对象。备选 C（仅 2 方法，无显式 Cancel）被否决——ADR-011 明确主进程 SSE 连接是跨多个 Run 的长连接，不是每 Run 一个连接，无法靠"关闭连接"实现单个 Run 的用户主动停止
- **Q-run-request-shape**：备选 B（端口内部反查历史 / 配置）被否决——会让 `Inkwell.Abstractions` 反向依赖 `IPersistenceProvider`，破坏 [ADR-017 依赖规则第 4 条](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md)"端口层零外部包依赖、不依赖任何业务实现"的单向依赖箭头；备选 C（通用 key-value 包）被否决——违反 [HD-001 Q5](HD-001-Inkwell.Abstractions-foundation.md#13-关键决策摘要) 强类型 DTO 惯例，且 REQ-004~007/010/016 涉及的字段已明确、无需保留通用扩展槽位
- **Q-streaming-event-shape**：备选 B（单一扁平 record + Kind 枚举）未选——虽然序列化更简单，但会产生"6 种 Kind 对应的可空字段互相打架"的问题（如 `DeltaText` 只对 `TextDelta` 有效，其余 5 种全为 `null`），封闭子类型族用 C# `switch` 模式匹配可编译期保证穷尽性检查（[`CS8509`](https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/cs8509)），可读性与类型安全性更优，与 AG-UI 协议本身就是判别式事件族的语义天然吻合；备选 C（原始 JSON 字符串 Payload）被否决——完全放弃类型层，`Inkwell.WebApi` 的 AG-UI 映射层将无法编译期检查字段完整性
- **Q-cancel-mechanism**：备选 B（未知 `runId` 抛异常）被否决——与 [HD-004 `ReleaseLockAsync`](HD-004-Inkwell.Abstractions-cache-port.md#132-候选与放弃理由) / [HD-005 `AcknowledgeAsync`](HD-005-Inkwell.Abstractions-queue-port.md#132-候选与放弃理由) 已确立的"未知/已结束目标返回幂等 false"风格不一致，且客户端"停止生成"按钮点击时 Run 可能已自然完成，属正常竞态而非错误；备选 C（全部走 DurableTask Terminate）被否决——[ADR-003](../../03-architecture/adr/ADR-003-agent-engine-microsoft-agent-framework.md) 将 `Microsoft.Agents.AI.DurableTask` 定位为 [REQ-012](../../01-requirements/requirements.md) 编排级持久化机制，强迫每个单轮聊天都过编排引擎会显著增加延迟与部署复杂度，且与 v1 范围（[OQ-006](../../01-requirements/open-questions.md)）不符
- **Q-perf-budget**：备选 B（端到端 SLA，如首 token P99 < 3s）未选——理由同 [HD-004](HD-004-Inkwell.Abstractions-cache-port.md#132-候选与放弃理由) / [HD-005](HD-005-Inkwell.Abstractions-queue-port.md#132-候选与放弃理由)：模型推理延迟依赖具体 Provider / 部署容量 / 网络，不是端口层能控制或承诺的指标，锁定该数字会造成"端口层承诺了自己无法兑现的 SLA"的误导
- **Q-tool-result-passthrough**：备选 B（仅流式路径带工具调用细节）被否决——[REQ-007 验收标准](../../01-requirements/requirements.md)明确"调用与返回在调试 trace 中可见"，未区分同步 / 流式调用方式，若非流式路径丢失该信息，会在同步调用场景下产生验收缺口
- **Q-multimodal-representation**：备选 B（`Content` 为单一 string）被否决——[REQ-016](../../01-requirements/requirements.md)"图片在支持视觉的模型上能被处理"要求图片数据本身（或其引用）到达模型调用层，纯字符串无法承载；备选 C（通用 key-value 包）被否决，理由同 Q-run-request-shape
- **Q-model-params-defaults**：备选 B（更保守的 0.7/0.95/4096）未选——Owner 拍板选择与 SDK 官方默认值一致的档位，减少"Inkwell 默认值与用户预期的 SDK 默认行为不一致"的认知负担；备选 C（不设默认上限）被否决——`MaxTokens` 缺省上限存在成本失控风险
- **Q-run-timeout**：备选 B（60 秒）被否决——对含多轮工具调用的复杂对话可能过短；备选 C（不设超时）被否决——容易掩盖真正的挂起 bug，且与 [HD-004](HD-004-Inkwell.Abstractions-cache-port.md) / [HD-005](HD-005-Inkwell.Abstractions-queue-port.md) 均设置了显式超时 / 可靠性参数的一贯风格不符
- **Q-tool-definition-shape**：备选 B（`JsonElement` 替代 `string`）未选——`string` 形态与 `AgentRunEvent.ToolCallRequested/ToolCallResult` 的 `ArgumentsJson`/`ResultJson` 字段保持同形态，序列化 / 跨事件传递时无需额外转换，且 `JsonElement` 在 `record` 的 value equality 语义上有已知的比较陷阱（依赖底层 `JsonDocument` 生命周期），`string` 更简单可靠
