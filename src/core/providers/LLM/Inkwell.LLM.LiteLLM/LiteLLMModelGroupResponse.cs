// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Text.Json.Serialization;

namespace Inkwell;

/// <summary>
/// 表示 LiteLLM 模型组能力项。
/// </summary>
internal sealed class LiteLLMModelGroupResponse
{
    /// <summary>
    /// 获取 LiteLLM model_name。
    /// </summary>
    [JsonPropertyName("model_group")]
    public required string ModelGroup { get; init; }

    /// <summary>
    /// 获取模型模式。
    /// </summary>
    [JsonPropertyName("mode")]
    public string? Mode { get; init; }

    /// <summary>
    /// 获取最大输入 token 数。
    /// </summary>
    [JsonPropertyName("max_input_tokens")]
    public int? MaxInputTokens { get; init; }

    /// <summary>
    /// 获取最大输出 token 数。
    /// </summary>
    [JsonPropertyName("max_output_tokens")]
    public int? MaxOutputTokens { get; init; }

    /// <summary>
    /// 获取是否支持函数调用。
    /// </summary>
    [JsonPropertyName("supports_function_calling")]
    public bool? SupportsFunctionCalling { get; init; }

    /// <summary>
    /// 获取是否支持视觉输入。
    /// </summary>
    [JsonPropertyName("supports_vision")]
    public bool? SupportsVision { get; init; }

    /// <summary>
    /// 获取是否支持响应 Schema。
    /// </summary>
    [JsonPropertyName("supports_response_schema")]
    public bool? SupportsResponseSchema { get; init; }

    /// <summary>
    /// 获取 LiteLLM 报告的 OpenAI-compatible 参数。
    /// </summary>
    [JsonPropertyName("supported_openai_params")]
    public IReadOnlyList<string>? SupportedOpenAIParameters { get; init; }

    /// <summary>
    /// 获取是否支持推理。
    /// </summary>
    [JsonPropertyName("supports_reasoning")]
    public bool? SupportsReasoning { get; init; }
}