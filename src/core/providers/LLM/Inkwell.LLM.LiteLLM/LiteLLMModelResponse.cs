// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Text.Json.Serialization;

namespace Inkwell;

/// <summary>
/// 表示 LiteLLM OpenAI-compatible 模型项。
/// </summary>
internal sealed class LiteLLMModelResponse
{
    /// <summary>
    /// 获取 LiteLLM model_name。
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// 获取 OpenAI-compatible 所有者字段。
    /// </summary>
    [JsonPropertyName("owned_by")]
    public string? OwnedBy { get; init; }
}