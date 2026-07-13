// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>
/// LiteLLM 模型的 Inkwell 业务元数据。
/// </summary>
public sealed class LiteLLMModelMetadataOptions
{
    /// <summary>
    /// 获取或设置 LiteLLM model_name。
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string Id { get; set; }

    /// <summary>
    /// 获取或设置模型显示名称。
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 获取或设置模型发布方标识。
    /// </summary>
    public string? PublisherId { get; set; }

    /// <summary>
    /// 获取或设置模型发布方显示名称。
    /// </summary>
    public string? PublisherDisplayName { get; set; }

    /// <summary>
    /// 获取或设置模型家族标识。
    /// </summary>
    public string? FamilyId { get; set; }

    /// <summary>
    /// 获取或设置模型家族显示名称。
    /// </summary>
    public string? FamilyDisplayName { get; set; }

    /// <summary>
    /// 获取或设置模型是否支持视觉输入。
    /// </summary>
    public bool SupportsVision { get; set; }

    /// <summary>
    /// 获取或设置模型是否支持工具调用。
    /// </summary>
    public bool SupportsTools { get; set; }

    /// <summary>
    /// 获取或设置模型是否支持结构化输出。
    /// </summary>
    public bool SupportsStructuredOutput { get; set; }

    /// <summary>
    /// 获取或设置模型上下文窗口 token 数。
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? ContextWindowTokens { get; set; }

    /// <summary>
    /// 获取或设置模型是否允许在 Inkwell 中使用。
    /// </summary>
    public bool IsEnabled { get; set; }
}