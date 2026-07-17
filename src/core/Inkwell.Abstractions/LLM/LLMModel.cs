// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 表示 LLM Provider 实时发现的模型。
/// </summary>
public sealed record class LLMModel
{
    /// <summary>
    /// 获取模型标识。
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// 获取 Inkwell 归一化后的模型分类。
    /// </summary>
    public required LLMModelCategory Category { get; init; }

    /// <summary>
    /// 获取 Provider 返回的原始模型模式。
    /// </summary>
    public string? ProviderMode { get; init; }

    /// <summary>
    /// 获取 Provider 返回的模型所有者。
    /// </summary>
    public string? OwnedBy { get; init; }

    /// <summary>
    /// 获取模型最大输入 token 数。
    /// </summary>
    public int? MaxInputTokens { get; init; }

    /// <summary>
    /// 获取模型最大输出 token 数。
    /// </summary>
    public int? MaxOutputTokens { get; init; }

    /// <summary>
    /// 获取模型是否支持视觉输入；无法确定时为 <see langword="null"/>。
    /// </summary>
    public bool? SupportsVision { get; init; }

    /// <summary>
    /// 获取模型是否支持工具调用；无法确定时为 <see langword="null"/>。
    /// </summary>
    public bool? SupportsTools { get; init; }

    /// <summary>
    /// 获取模型是否支持结构化输出；无法确定时为 <see langword="null"/>。
    /// </summary>
    public bool? SupportsStructuredOutput { get; init; }

    /// <summary>
    /// 获取模型是否支持推理；无法确定时为 <see langword="null"/>。
    /// </summary>
    public bool? SupportsReasoning { get; init; }
}