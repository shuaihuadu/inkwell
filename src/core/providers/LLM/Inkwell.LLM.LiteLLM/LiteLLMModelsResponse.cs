// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Text.Json.Serialization;

namespace Inkwell;

/// <summary>
/// 表示 LiteLLM OpenAI-compatible 模型列表响应。
/// </summary>
internal sealed class LiteLLMModelsResponse
{
    /// <summary>
    /// 获取模型列表。
    /// </summary>
    [JsonPropertyName("data")]
    public IReadOnlyList<LiteLLMModelResponse> Data { get; init; } = [];
}