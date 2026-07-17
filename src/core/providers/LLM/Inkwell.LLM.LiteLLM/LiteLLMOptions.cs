// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>
/// 配置 LiteLLM Provider 连接。
/// </summary>
public sealed class LiteLLMOptions
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
}