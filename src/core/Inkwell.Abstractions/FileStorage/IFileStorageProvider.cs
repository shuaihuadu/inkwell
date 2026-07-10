// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 文件存储端口 facade。三 Provider 切换：LocalFileSystem / AzureBlob / MinIO（ADR-015）。
/// </summary>
public interface IFileStorageProvider
{
    Task<FileUploadResult> UploadAsync(string container, string key, Stream content, FileMetadata metadata, CancellationToken ct = default);

    Task<FileDownloadResponse> DownloadAsync(string container, string key, CancellationToken ct = default);

    Task<bool> ExistsAsync(string container, string key, CancellationToken ct = default);

    Task DeleteAsync(string container, string key, CancellationToken ct = default);

    Task<Uri> CreatePresignedUploadUrlAsync(string container, string key, TimeSpan ttl, CancellationToken ct = default);

    Task<Uri> CreatePresignedDownloadUrlAsync(string container, string key, TimeSpan ttl, CancellationToken ct = default);

    IAsyncEnumerable<FileObjectInfo> ListAsync(string container, string? prefix = null, CancellationToken ct = default);
}
