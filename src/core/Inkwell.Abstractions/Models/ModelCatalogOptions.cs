using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>模型清单配置；从 appsettings.json "Inkwell:Models" 段绑定。</summary>
public sealed class ModelCatalogOptions
{
    [Required]
    [MinLength(1)]
    public IReadOnlyList<ModelEntryOptions> Models { get; set; } = [];
}

public sealed class ModelEntryOptions
{
    [Required]
    [MinLength(1)]
    public required string Id { get; set; }

    [Required]
    [MinLength(1)]
    public required string DisplayName { get; set; }

    public required ModelProviderKind Provider { get; set; }

    public bool SupportsVision { get; set; }

    public bool IsAvailable { get; set; } = true;
}
