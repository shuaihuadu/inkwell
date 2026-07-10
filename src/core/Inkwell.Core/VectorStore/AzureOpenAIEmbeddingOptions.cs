// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>Azure OpenAI Embeddings 凭证配置。</summary>
public sealed class AzureOpenAIEmbeddingOptions
{
    [Required]
    public required AzureOpenAICredential Credential { get; set; }

    [Range(1, 4096)]
    public int? Dimensions { get; init; }
}
