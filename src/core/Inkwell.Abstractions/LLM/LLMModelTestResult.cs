// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 表示模型连通性测试结果。
/// </summary>
public sealed record class LLMModelTestResult
{
    /// <summary>
    /// 获取模型标识。
    /// </summary>
    public required string ModelId { get; init; }

    /// <summary>
    /// 获取测试是否成功。
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// 获取测试耗时。
    /// </summary>
    public required TimeSpan Latency { get; init; }

    /// <summary>
    /// 获取已脱敏的失败信息。
    /// </summary>
    public string? ErrorMessage { get; init; }
}