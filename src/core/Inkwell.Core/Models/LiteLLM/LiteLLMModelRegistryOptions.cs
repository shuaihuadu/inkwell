// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>
/// LiteLLM 模型发现配置。
/// </summary>
public sealed class LiteLLMModelRegistryOptions
{
    /// <summary>
    /// 获取或设置 LiteLLM Proxy 地址。
    /// </summary>
    [Required]
    public required Uri Endpoint { get; set; }

    /// <summary>
    /// 获取或设置 LiteLLM Virtual Key 或 Master Key。
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string ApiKey { get; set; }

    /// <summary>
    /// 获取或设置 LiteLLM 模型的 Inkwell 业务元数据覆盖。
    /// </summary>
    public IReadOnlyList<LiteLLMModelMetadataOptions> Models { get; set; } = [];
}
