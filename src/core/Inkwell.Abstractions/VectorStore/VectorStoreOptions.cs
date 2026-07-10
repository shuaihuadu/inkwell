// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>
/// 向量库端口详细配置。接口复用 <c>Microsoft.Extensions.VectorData.VectorStore</c> /
/// <c>VectorStoreCollection&lt;TKey, TRecord&gt;</c>（ADR-020），本类不重发明接口。
/// </summary>
public sealed class VectorStoreOptions
{
    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Range(1, 4096)]
    public int EmbeddingDimensions { get; set; } = 1536;

    [Range(1, 300)]
    public int RequestTimeoutSeconds { get; set; } = 30;

    public bool EnableSensitiveDataLogging { get; set; }
}
