// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.WebApi.Conversations;

/// <summary>表示一次完成回复聚合后的 Token 用量。</summary>
public sealed record class AgentTokenUsageResponse
{
    /// <summary>获取 Provider 报告的输入 Token 数。</summary>
    public long? InputTokenCount { get; init; }

    /// <summary>获取 Provider 报告的输出 Token 数。</summary>
    public long? OutputTokenCount { get; init; }

    /// <summary>获取 Provider 报告的总 Token 数。</summary>
    public long? TotalTokenCount { get; init; }

    /// <summary>获取 Provider 报告的缓存输入 Token 数。</summary>
    public long? CachedInputTokenCount { get; init; }

    /// <summary>获取 Provider 报告的推理 Token 数。</summary>
    public long? ReasoningTokenCount { get; init; }

    /// <summary>获取 Provider 报告的其他分类计数。</summary>
    public IReadOnlyDictionary<string, long>? AdditionalCounts { get; init; }
}