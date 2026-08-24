// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Extensions.AI;

namespace Inkwell;

#pragma warning disable MEAI001

/// <summary>聚合一个 Agent Run 内由模型 Provider 报告的 Token 用量。</summary>
internal static class AgentTokenUsageAggregator
{
    /// <summary>按类别累加两组用量，并保留未报告字段的空值语义。</summary>
    /// <param name="current">当前聚合用量。</param>
    /// <param name="incoming">待合入的用量。</param>
    /// <returns>新的聚合用量；两组输入均为空时返回 <see langword="null"/>。</returns>
    public static UsageDetails? Add(UsageDetails? current, UsageDetails? incoming)
    {
        if (current is null)
        {
            return incoming is null ? null : Copy(incoming);
        }

        if (incoming is null)
        {
            return Copy(current);
        }

        return new UsageDetails
        {
            InputTokenCount = Add(current.InputTokenCount, incoming.InputTokenCount),
            OutputTokenCount = Add(current.OutputTokenCount, incoming.OutputTokenCount),
            TotalTokenCount = Add(current.TotalTokenCount, incoming.TotalTokenCount),
            CachedInputTokenCount = Add(current.CachedInputTokenCount, incoming.CachedInputTokenCount),
            ReasoningTokenCount = Add(current.ReasoningTokenCount, incoming.ReasoningTokenCount),
            InputAudioTokenCount = Add(current.InputAudioTokenCount, incoming.InputAudioTokenCount),
            InputTextTokenCount = Add(current.InputTextTokenCount, incoming.InputTextTokenCount),
            OutputAudioTokenCount = Add(current.OutputAudioTokenCount, incoming.OutputAudioTokenCount),
            OutputTextTokenCount = Add(current.OutputTextTokenCount, incoming.OutputTextTokenCount),
            AdditionalCounts = Add(current.AdditionalCounts, incoming.AdditionalCounts),
        };
    }

    private static UsageDetails Copy(UsageDetails usage) => new()
    {
        InputTokenCount = usage.InputTokenCount,
        OutputTokenCount = usage.OutputTokenCount,
        TotalTokenCount = usage.TotalTokenCount,
        CachedInputTokenCount = usage.CachedInputTokenCount,
        ReasoningTokenCount = usage.ReasoningTokenCount,
        InputAudioTokenCount = usage.InputAudioTokenCount,
        InputTextTokenCount = usage.InputTextTokenCount,
        OutputAudioTokenCount = usage.OutputAudioTokenCount,
        OutputTextTokenCount = usage.OutputTextTokenCount,
        AdditionalCounts = usage.AdditionalCounts is null ? null : new AdditionalPropertiesDictionary<long>(usage.AdditionalCounts),
    };

    private static long? Add(long? current, long? incoming) =>
        current.HasValue && incoming.HasValue ? current.Value + incoming.Value : current ?? incoming;

    private static AdditionalPropertiesDictionary<long>? Add(
        AdditionalPropertiesDictionary<long>? current,
        AdditionalPropertiesDictionary<long>? incoming)
    {
        if (current is null && incoming is null)
        {
            return null;
        }

        AdditionalPropertiesDictionary<long> result = current is null
            ? []
            : new AdditionalPropertiesDictionary<long>(current);
        if (incoming is not null)
        {
            foreach ((string key, long value) in incoming)
            {
                result[key] = result.TryGetValue(key, out long currentValue) ? currentValue + value : value;
            }
        }

        return result;
    }
}

#pragma warning restore MEAI001
