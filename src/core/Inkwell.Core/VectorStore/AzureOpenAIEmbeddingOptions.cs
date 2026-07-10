using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>Azure OpenAI Embeddings 凭证配置。</summary>
public sealed class AzureOpenAIEmbeddingOptions
{
    [Required]
    public string Endpoint { get; init; } = string.Empty;

    public string? ApiKey { get; init; }

    [Required]
    public string DeploymentName { get; init; } = string.Empty;

    [Range(1, 4096)]
    public int? Dimensions { get; init; }
}
