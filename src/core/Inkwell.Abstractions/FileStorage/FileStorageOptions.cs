using System.ComponentModel.DataAnnotations;

namespace Inkwell;

public sealed class FileStorageOptions
{
    [Range(1, 1440)]
    public int UploadUrlTtlMinutes { get; set; } = 15;

    [Range(1, 1440)]
    public int DownloadUrlTtlMinutes { get; set; } = 60;

    [Range(1, long.MaxValue)]
    public long MaxObjectSizeBytes { get; set; } = 50 * 1024 * 1024;

    [Range(1, 1000)]
    public int ListPageSize { get; set; } = 100;

    public bool EnableSensitiveDataLogging { get; set; }
}
