// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Core.Tests.AgentRuntime;

#pragma warning disable MEAI001

/// <summary>验证 Agent Run Token 用量聚合语义。</summary>
[TestClass]
public sealed class AgentTokenUsageAggregatorTests
{
    /// <summary>验证两组完整用量按类别累加且扩展计数按键合并。</summary>
    [TestMethod]
    public void Add_WithMultipleUsageDetails_AggregatesEveryCategory()
    {
        // Arrange
        UsageDetails current = CreateUsage(1, 2, 3, 4, 5, 6, 7, 8, 9, new() { ["shared"] = 10, ["current"] = 11 });
        UsageDetails incoming = CreateUsage(11, 12, 13, 14, 15, 16, 17, 18, 19, new() { ["shared"] = 20, ["incoming"] = 21 });

        // Act
        UsageDetails? result = AgentTokenUsageAggregator.Add(current, incoming);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(12, result.InputTokenCount);
        Assert.AreEqual(14, result.OutputTokenCount);
        Assert.AreEqual(16, result.TotalTokenCount);
        Assert.AreEqual(18, result.CachedInputTokenCount);
        Assert.AreEqual(20, result.ReasoningTokenCount);
        Assert.AreEqual(22, result.InputAudioTokenCount);
        Assert.AreEqual(24, result.InputTextTokenCount);
        Assert.AreEqual(26, result.OutputAudioTokenCount);
        Assert.AreEqual(28, result.OutputTextTokenCount);
        Assert.AreEqual(30, result.AdditionalCounts!["shared"]);
        Assert.AreEqual(11, result.AdditionalCounts["current"]);
        Assert.AreEqual(21, result.AdditionalCounts["incoming"]);
    }

    /// <summary>验证缺失字段保持空值且不会根据输入输出推导总计。</summary>
    [TestMethod]
    public void Add_WithPartialUsage_PreservesNullSemantics()
    {
        // Arrange
        UsageDetails current = new() { InputTokenCount = 2 };
        UsageDetails incoming = new() { OutputTokenCount = 3 };

        // Act
        UsageDetails? result = AgentTokenUsageAggregator.Add(current, incoming);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.InputTokenCount);
        Assert.AreEqual(3, result.OutputTokenCount);
        Assert.IsNull(result.TotalTokenCount);
        Assert.IsNull(result.AdditionalCounts);
    }

    /// <summary>验证聚合结果不共享或修改任一输入的扩展计数字典。</summary>
    [TestMethod]
    public void Add_WithSingleUsage_ReturnsIndependentCopy()
    {
        // Arrange
        UsageDetails incoming = new()
        {
            InputTokenCount = 4,
            AdditionalCounts = new() { ["provider"] = 5 },
        };

        // Act
        UsageDetails? result = AgentTokenUsageAggregator.Add(null, incoming);
        result!.AdditionalCounts!["provider"] = 6;

        // Assert
        Assert.AreNotSame(incoming, result);
        Assert.AreEqual(5, incoming.AdditionalCounts!["provider"]);
        Assert.IsNull(AgentTokenUsageAggregator.Add(null, null));
    }

    private static UsageDetails CreateUsage(
        long input,
        long output,
        long total,
        long cachedInput,
        long reasoning,
        long inputAudio,
        long inputText,
        long outputAudio,
        long outputText,
        AdditionalPropertiesDictionary<long> additionalCounts) => new()
        {
            InputTokenCount = input,
            OutputTokenCount = output,
            TotalTokenCount = total,
            CachedInputTokenCount = cachedInput,
            ReasoningTokenCount = reasoning,
            InputAudioTokenCount = inputAudio,
            InputTextTokenCount = inputText,
            OutputAudioTokenCount = outputAudio,
            OutputTextTokenCount = outputText,
            AdditionalCounts = additionalCounts,
        };
}

#pragma warning restore MEAI001