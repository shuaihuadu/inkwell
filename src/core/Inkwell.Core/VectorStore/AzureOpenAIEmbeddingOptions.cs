// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>Azure OpenAI Embeddings 凭证配置。</summary>
public sealed class AzureOpenAIEmbeddingOptions
{
    /// <summary>
    /// 获取或设置 Azure OpenAI 凭证。
    /// </summary>
    [Required]
    public required AzureOpenAICredential Credential { get; set; }

    /// <summary>
    /// 获取或设置嵌入向量维度。
    /// </summary>
    [Range(1, 4096)]
    public int? Dimensions { get; init; }
}
