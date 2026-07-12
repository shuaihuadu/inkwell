// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.FileStorage.Local;

/// <summary>本地磁盘存储根目录配置。</summary>
public sealed class LocalFileSystemFileStorageOptions
{
    /// <summary>获取或设置本地磁盘存储根目录。</summary>
    [Required]
    public string RootPath { get; set; } = Path.Combine(Path.GetTempPath(), "inkwell", "uploads");
}
