// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>模型目录条目投影 DTO；UI 下拉展示 + Agent 侧 ModelId 校验共用同一形状。</summary>
public sealed record class ModelSummary
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
    /// 获取模型提供商类型。
    /// </summary>
    public required ModelProviderKind Provider { get; init; }

    /// <summary>
    /// 获取模型是否支持视觉输入。
    /// </summary>
    public bool SupportsVision { get; init; }

    /// <summary>
    /// 获取模型是否可用。
    /// </summary>
    public bool IsAvailable { get; init; }
}

/// <summary>
/// 指定模型提供商类型。
/// </summary>
public enum ModelProviderKind
{
    /// <summary>Azure OpenAI。</summary>
    AzureOpenAI,

    /// <summary>OpenAI。</summary>
    OpenAI,

    /// <summary>Anthropic。</summary>
    Anthropic,

    /// <summary>通义千问。</summary>
    Qwen,

    /// <summary>智谱 AI。</summary>
    Zhipu,

    /// <summary>其他模型提供商。</summary>
    Other,
}
