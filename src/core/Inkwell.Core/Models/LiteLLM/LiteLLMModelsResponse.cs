// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Text.Json.Serialization;

namespace Inkwell;

/// <summary>
/// LiteLLM OpenAI 兼容模型列表响应。
/// </summary>
internal sealed class LiteLLMModelsResponse
{
    /// <summary>
    /// 获取模型列表。
    /// </summary>
    [JsonPropertyName("data")]
    public IReadOnlyList<LiteLLMModelResponse> Data { get; init; } = [];
}

/// <summary>
/// LiteLLM OpenAI 兼容模型项。
/// </summary>
internal sealed class LiteLLMModelResponse
{
    /// <summary>
    /// 获取 LiteLLM model_name。
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// 获取 OpenAI 兼容所有者字段。
    /// </summary>
    [JsonPropertyName("owned_by")]
    public string? OwnedBy { get; init; }
}
