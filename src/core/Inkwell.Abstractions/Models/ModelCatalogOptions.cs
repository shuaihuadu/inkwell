// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>模型清单配置；从 appsettings.json "Inkwell:Models" 段绑定。</summary>
public sealed class ModelCatalogOptions
{
    /// <summary>
    /// 获取或设置模型配置条目列表。
    /// </summary>
    [Required]
    [MinLength(1)]
    public IReadOnlyList<ModelEntryOptions> Models { get; set; } = [];
}

/// <summary>
/// 表示单个模型的目录配置。
/// </summary>
public sealed class ModelEntryOptions
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
    /// 获取或设置模型提供商类型。
    /// </summary>
    public required ModelProviderKind Provider { get; set; }

    /// <summary>
    /// 获取或设置模型是否支持视觉输入。
    /// </summary>
    public bool SupportsVision { get; set; }

    /// <summary>
    /// 获取或设置模型是否可用。
    /// </summary>
    public bool IsAvailable { get; set; } = true;
}
