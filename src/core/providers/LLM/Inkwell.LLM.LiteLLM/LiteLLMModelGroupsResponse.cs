// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Text.Json.Serialization;

namespace Inkwell;

/// <summary>
/// 表示 LiteLLM 模型组能力响应。
/// </summary>
internal sealed class LiteLLMModelGroupsResponse
{
    /// <summary>
    /// 获取模型组列表。
    /// </summary>
    [JsonPropertyName("data")]
    public IReadOnlyList<LiteLLMModelGroupResponse> Data { get; init; } = [];
}