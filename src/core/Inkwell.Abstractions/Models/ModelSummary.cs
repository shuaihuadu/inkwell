// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>模型目录条目投影 DTO；UI 下拉展示 + Agent 侧 ModelId 校验共用同一形状。</summary>
public sealed record class ModelSummary
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required ModelProviderKind Provider { get; init; }

    public bool SupportsVision { get; init; }

    public bool IsAvailable { get; init; }
}

public enum ModelProviderKind
{
    AzureOpenAI,
    OpenAI,
    Anthropic,
    Qwen,
    Zhipu,
    Other,
}
