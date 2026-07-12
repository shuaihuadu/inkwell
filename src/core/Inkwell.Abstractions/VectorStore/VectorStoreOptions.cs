// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>
/// 向量库端口详细配置。接口复用 <c>Microsoft.Extensions.VectorData.VectorStore</c> /
/// <c>VectorStoreCollection&lt;TKey, TRecord&gt;</c>（ADR-020），本类不重发明接口。
/// </summary>
public sealed class VectorStoreOptions
{
    /// <summary>
    /// 获取或设置向量存储服务端点。
    /// </summary>
    [Required]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置嵌入向量维度。
    /// </summary>
    [Range(1, 4096)]
    public int EmbeddingDimensions { get; set; } = 1536;

    /// <summary>
    /// 获取或设置请求超时时间（秒）。
    /// </summary>
    [Range(1, 300)]
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 获取或设置是否启用敏感数据日志记录。
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; }
}
