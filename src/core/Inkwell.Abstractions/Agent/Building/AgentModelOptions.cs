// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 定义 Agent 使用的模型及其调用选项。
/// </summary>
public sealed record class AgentModelOptions
{
    /// <summary>
    /// 获取模型目录中的模型标识。
    /// </summary>
    public string? ModelId { get; init; }

    /// <summary>
    /// 获取采样温度；未设置时使用默认值。
    /// </summary>
    [Range(0.0, 2.0)]
    public double? Temperature { get; init; }

    /// <summary>
    /// 获取核采样概率阈值；未设置时使用默认值。
    /// </summary>
    [Range(0.0, 1.0)]
    public double? TopP { get; init; }

    /// <summary>
    /// 获取最大输出令牌数；未设置时使用默认值。
    /// </summary>
    [Range(1, 128000)]
    public int? MaxTokens { get; init; }
}