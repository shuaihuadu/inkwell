// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>模型注册表条目；供 UI 展示、筛选与 Agent 构建前校验。</summary>
public sealed record class ModelDefinition
{
    /// <summary>
    /// 获取模型标识。
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// 获取模型显示名称。
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// 获取模型发布方标识，例如 <c>openai</c> 或 <c>alibaba</c>。
    /// </summary>
    public string? PublisherId { get; init; }

    /// <summary>
    /// 获取模型发布方显示名称。
    /// </summary>
    public string? PublisherDisplayName { get; init; }

    /// <summary>
    /// 获取模型家族标识，例如 <c>gpt</c> 或 <c>qwen</c>。
    /// </summary>
    public string? FamilyId { get; init; }

    /// <summary>
    /// 获取模型家族显示名称。
    /// </summary>
    public string? FamilyDisplayName { get; init; }

    /// <summary>
    /// 获取模型配置来源标识。
    /// </summary>
    public required string SourceId { get; init; }

    /// <summary>
    /// 获取执行模型调用的运行时连接标识。
    /// </summary>
    public required string RuntimeId { get; init; }

    /// <summary>
    /// 获取传递给运行时连接的远端模型标识。
    /// </summary>
    public required string RemoteModelId { get; init; }

    /// <summary>
    /// 获取模型是否支持视觉输入。
    /// </summary>
    public bool SupportsVision { get; init; }

    /// <summary>
    /// 获取模型是否支持工具调用。
    /// </summary>
    public bool SupportsTools { get; init; }

    /// <summary>
    /// 获取模型是否支持结构化输出。
    /// </summary>
    public bool SupportsStructuredOutput { get; init; }

    /// <summary>
    /// 获取模型上下文窗口 token 数。
    /// </summary>
    public int? ContextWindowTokens { get; init; }

    /// <summary>
    /// 获取模型是否可用。
    /// </summary>
    public bool IsAvailable { get; init; }

    /// <summary>
    /// 获取模型不可用原因。
    /// </summary>
    public string? UnavailableReason { get; init; }
}
