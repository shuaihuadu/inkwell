// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>
/// 模型调用参数 DTO（REQ-006）。三字段均可空——<c>null</c> 表示"使用默认"。
/// </summary>
public sealed record class AgentModelParameters
{
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
