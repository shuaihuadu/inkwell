// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>
/// 模型调用参数 DTO（REQ-006）。三字段均可空——<c>null</c> 表示"使用默认"。
/// </summary>
public sealed record class AgentModelParameters
{
    [Range(0.0, 2.0)]
    public double? Temperature { get; init; }

    [Range(0.0, 1.0)]
    public double? TopP { get; init; }

    [Range(1, 128000)]
    public int? MaxTokens { get; init; }
}
