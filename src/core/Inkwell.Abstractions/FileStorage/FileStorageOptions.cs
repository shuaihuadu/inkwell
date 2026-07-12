// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>
/// 提供文件存储行为配置。
/// </summary>
public sealed class FileStorageOptions
{
    /// <summary>
    /// 获取或设置预签名上传 URL 的有效期（分钟）。
    /// </summary>
    [Range(1, 1440)]
    public int UploadUrlTtlMinutes { get; set; } = 15;

    /// <summary>
    /// 获取或设置预签名下载 URL 的有效期（分钟）。
    /// </summary>
    [Range(1, 1440)]
    public int DownloadUrlTtlMinutes { get; set; } = 60;

    /// <summary>
    /// 获取或设置单个对象的最大大小（字节）。
    /// </summary>
    [Range(1, long.MaxValue)]
    public long MaxObjectSizeBytes { get; set; } = 50 * 1024 * 1024;

    /// <summary>
    /// 获取或设置对象列表的每页条数。
    /// </summary>
    [Range(1, 1000)]
    public int ListPageSize { get; set; } = 100;

    /// <summary>
    /// 获取或设置是否启用敏感数据日志记录。
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; }
}
