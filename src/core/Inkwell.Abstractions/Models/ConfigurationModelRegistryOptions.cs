// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>配置文件模型来源；从 appsettings.json "Inkwell:Models" 段绑定。</summary>
public sealed class ConfigurationModelRegistryOptions
{
    /// <summary>
    /// 获取或设置模型配置条目列表。
    /// </summary>
    public IReadOnlyList<ConfigurationModelEntryOptions> Models { get; set; } = [];
}

/// <summary>
/// 表示单个模型的目录配置。
/// </summary>
public sealed class ConfigurationModelEntryOptions
{
    /// <summary>
    /// 获取或设置模型标识。
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string Id { get; set; }

    /// <summary>
    /// 获取或设置模型显示名称。
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string DisplayName { get; set; }

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
    /// 获取或设置执行模型调用的运行时连接标识。
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string RuntimeId { get; set; }

    /// <summary>
    /// 获取或设置传递给原生运行时的远端模型标识。
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string RemoteModelId { get; set; }

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
    /// 获取或设置模型是否可用。
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// 获取或设置模型不可用原因。
    /// </summary>
    public string? UnavailableReason { get; set; }
}
