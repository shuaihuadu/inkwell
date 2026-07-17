---
id: HD-019
title: LLM Provider 与模型发现详细设计
stage: H3
status: draft
reviewers: []
upstream:
  - REQ-005
  - REQ-006
  - ADR-003
  - ADR-017
  - ADR-026
downstream:
  - H5-013
---

# HD-019 LLM Provider 与模型发现详细设计

## 1. 目标与边界

本设计建立与 `IQueueProvider`、`ICacheProvider` 同方向的 LLM 接入端口，但按模型执行能力遵循接口隔离原则。

在内：

- 实时模型发现、按 ID 查询和连通性测试。
- Chat、Embedding、图片生成、视频生成的能力端口。
- LiteLLM 首个 Provider 实现。
- Agent Factory 使用 `IChatLLMProvider` 创建 `IChatClient`。
- WebApi 模型查询 API 使用公共 Provider 端口，不直接读取 LiteLLM 私有 DTO。

不在内：

- Inkwell 模型表、Repository、缓存副本或 appsettings 模型清单。
- 多 Provider 同时聚合、自动 fallback 和跨 Provider 模型别名迁移。
- 本任务实现 Embedding、图片和视频的实际执行流程；只定义端口和分类。
- 模型管理写操作；新增、修改和删除模型继续在 LiteLLM Portal 完成。

## 2. 公共契约

### 2.1 `LLMModelCategory`

```csharp
public enum LLMModelCategory
{
    Unknown = 0,
    Chat = 1,
    Embedding = 2,
    ImageGeneration = 3,
    VideoGeneration = 4,
}
```

枚举表达 Inkwell 产品分类，不替代 Provider 原始 mode。未知 mode 映射为 `Unknown`。

### 2.2 `LLMModel`

```csharp
public sealed record class LLMModel
{
    public required string Id { get; init; }
    public required LLMModelCategory Category { get; init; }
    public string? ProviderMode { get; init; }
    public string? OwnedBy { get; init; }
    public int? MaxInputTokens { get; init; }
    public int? MaxOutputTokens { get; init; }
    public bool? SupportsVision { get; init; }
    public bool? SupportsTools { get; init; }
    public bool? SupportsStructuredOutput { get; init; }
    public bool? SupportsReasoning { get; init; }
}
```

公共模型不包含 `SourceId`、`RuntimeId`、`RemoteModelId`、本地启用状态或上游连接配置。

### 2.3 Provider 接口

```csharp
public interface ILLMProvider
{
    Task<IReadOnlyList<LLMModel>> ListModelsAsync(CancellationToken cancellationToken = default);
    Task<LLMModel> GetModelAsync(string modelId, CancellationToken cancellationToken = default);
    Task<LLMModelTestResult> TestModelAsync(string modelId, CancellationToken cancellationToken = default);
}

public interface IChatLLMProvider
{
    IChatClient CreateChatClient(string modelId);
}
```

`IEmbeddingLLMProvider` 使用 `IEmbeddingGenerator<string, Embedding<float>>`。图片和视频端口分别使用领域请求/结果；首期不实现其 Provider 方法，避免未验证 API 被固化。

### 2.4 连通性结果

`LLMModelTestResult` 包含 `IsSuccess`、`Latency` 和脱敏后的 `ErrorMessage`。不返回原始响应、Prompt、API Key 或 Endpoint。

## 3. LiteLLM Provider

### 3.1 配置

`LiteLLMOptions` 只包含：

- `Endpoint`
- `ApiKey`

从 `Inkwell:LiteLLM` 绑定并在启动期校验。不存在 `Models` metadata 覆盖段。

### 3.2 列表算法

1. 调用 `/v1/models` 获取可访问模型。
2. 调用 `/model_group/info` 获取聚合能力。
3. 以大小写不敏感的模型 ID 合并。
4. 将 mode 映射为 `LLMModelCategory`，同时保留 `ProviderMode`。
5. 能力缺失时保留 `null`。
6. 按模型 ID 排序并返回，不写入数据库或本地缓存。

### 3.3 Chat Client

`LiteLLMProvider` 实现 `IChatLLMProvider`，复用指向 `<Endpoint>/v1/` 的 `OpenAIClient`，以 Agent 保存的 `modelId` 创建 `IChatClient`。Provider 不读取 LiteLLM deployment 或上游凭据。

### 3.4 连通性测试

首期只允许 Chat 类模型执行测试：发送固定、最小、无用户数据的请求并记录耗时。其他分类返回不支持测试的明确结果，等待对应能力端口实施后补齐。异常消息必须脱敏。

## 4. WebApi

- WebApi 与 Agent Factory 直接依赖公共 `ILLMProvider`，不增加只有转发行为的应用服务。
- `GET /api/models` 返回全部实时模型。
- `GET /api/models/{modelId}` 返回实时模型详情。
- `POST /api/models/{modelId}/test` 仅允许 Super 用户调用，避免普通用户触发计费请求。
- Agent 设计页按 `Category=Chat` 过滤对话模型。

## 5. Agent 执行

`ModelRoutingAgentFactory` 收敛为单 Provider Agent Factory：

1. 读取 `AgentModelOptions.ModelId`。
2. 通过 `ILLMProvider.GetModelAsync` 验证模型存在且分类为 `Chat`。
3. 通过 `IChatLLMProvider.CreateChatClient(modelId)` 获取客户端。
4. 组装 MAF `ChatClientAgentOptions`、History、Tools 和 Skills。

执行前的模型查询用于分类和清晰错误，不读取上游连接配置。模型在查询后被删除的竞态由实际调用错误处理。

## 6. 文件结构

```text
src/core/Inkwell.Abstractions/LLM/
  ILLMProvider.cs
  IChatLLMProvider.cs
  LLMModel.cs
  LLMModelCategory.cs
  LLMModelTestResult.cs
src/core/providers/LLM/Inkwell.LLM.LiteLLM/
  LiteLLMProvider.cs
  LiteLLMOptions.cs
  LiteLLMModelResponse.cs
  LiteLLMModelGroupResponse.cs
  LiteLLMBuilderExtensions.cs
src/core/Inkwell.Core/AgentRuntime/
  ModelRoutingAgentFactory.cs
```

LiteLLM Provider 独立 csproj，只依赖 `Inkwell.Abstractions`、OpenAI/Microsoft.Extensions.AI 和 HTTP/JSON 基础包；不依赖 `Inkwell.Core`。

## 7. 错误语义

| 场景 | 行为 |
| --- | --- |
| 空 `modelId` | `ArgumentException` |
| 模型不存在 | `KeyNotFoundException` |
| 非 Chat 模型用于 Agent | `InvalidOperationException` |
| LiteLLM 认证/网络/协议失败 | 保留可诊断异常链，API 由全局错误映射脱敏 |
| 连通性测试失败 | 返回失败结果，不把 API Key 或 Endpoint 写入响应 |

## 8. 测试设计

- Provider contract：模型列表合并、分类映射、未知 mode、三态能力、按 ID 查询。
- Chat Client：模型 ID 正确传给 OpenAI-compatible 客户端。
- Agent Factory：Chat 成功、Embedding/图片/视频拒绝、未知模型拒绝。
- WebApi：列表/详情授权，测试端点仅 Super 用户可用。
- 真实 LiteLLM：`/v1/models`、`/model_group/info`、Chat Completions 和 Responses 冒烟验证。

## 9. 决策状态

本文件 `status: draft`、`reviewers: []` 保持人工签字位不变。Owner 已在 2026-07-17 对本轮实施范围给出明确执行指令；AI 不代签文档状态。
