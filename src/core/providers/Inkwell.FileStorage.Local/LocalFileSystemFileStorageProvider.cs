// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.FileStorage.Local;

/// <summary>默认 dev / unit test 实现，进程内本地磁盘 + sidecar 元数据文件，无外部依赖。</summary>
internal sealed class LocalFileSystemFileStorageProvider(IOptions<LocalFileSystemFileStorageOptions> options) : IFileStorageProvider
{
    private static readonly JsonSerializerOptions metadataSerializerOptions = new() { WriteIndented = false };

    /// <inheritdoc />
    public async Task<FileUploadResult> UploadAsync(string container, string key, Stream content, FileMetadata metadata, CancellationToken ct = default)
    {
        string path = this.ResolvePath(container, key);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using (FileStream fileStream = File.Create(path))
        {
            await content.CopyToAsync(fileStream, ct).ConfigureAwait(false);
        }

        DateTimeOffset uploadedTime = DateTimeOffset.UtcNow;
        string eTag = Guid.NewGuid().ToString();

        await WriteSidecarAsync(path, metadata, eTag, uploadedTime, ct).ConfigureAwait(false);

        return new FileUploadResult(container, key, new FileInfo(path).Length, eTag, uploadedTime);
    }

    /// <inheritdoc />
    public async Task<FileDownloadResponse> DownloadAsync(string container, string key, CancellationToken ct = default)
    {
        string path = this.ResolvePath(container, key);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"File not found: {container}/{key}", path);
        }

        SidecarMetadata sidecar = await ReadSidecarAsync(path, ct).ConfigureAwait(false);
        FileStream stream = File.OpenRead(path);

        return new FileDownloadResponse(stream, sidecar.Metadata, sidecar.ETag, stream.Length, sidecar.UploadedTime);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string container, string key, CancellationToken ct = default) =>
        Task.FromResult(File.Exists(this.ResolvePath(container, key)));

    /// <inheritdoc />
    public Task DeleteAsync(string container, string key, CancellationToken ct = default)
    {
        string path = this.ResolvePath(container, key);

        File.Delete(path);
        File.Delete(SidecarPath(path));

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>本地实现无 HTTP 层可供签发真实预签名 URL；返回 <c>file://</c> URI，<paramref name="ttl"/> 不做强制过期。</remarks>
    public Task<Uri> CreatePresignedUploadUrlAsync(string container, string key, TimeSpan ttl, CancellationToken ct = default) =>
        Task.FromResult(new Uri(this.ResolvePath(container, key)));

    /// <inheritdoc />
    public Task<Uri> CreatePresignedDownloadUrlAsync(string container, string key, TimeSpan ttl, CancellationToken ct = default) =>
        Task.FromResult(new Uri(this.ResolvePath(container, key)));

    /// <inheritdoc />
    public async IAsyncEnumerable<FileObjectInfo> ListAsync(string container, string? prefix = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        string containerPath = Path.Combine(options.Value.RootPath, container);

        if (!Directory.Exists(containerPath))
        {
            yield break;
        }

        foreach (string path in Directory.EnumerateFiles(containerPath, "*", SearchOption.AllDirectories))
        {
            if (path.EndsWith(".meta.json", StringComparison.Ordinal))
            {
                continue;
            }

            ct.ThrowIfCancellationRequested();

            string key = Path.GetRelativePath(containerPath, path).Replace(Path.DirectorySeparatorChar, '/');

            if (prefix is not null && !key.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            SidecarMetadata sidecar = await ReadSidecarAsync(path, ct).ConfigureAwait(false);
            long length = new FileInfo(path).Length;

            yield return new FileObjectInfo(container, key, length, sidecar.ETag, sidecar.UploadedTime, sidecar.Metadata.ContentType);
        }
    }

    /// <summary>把 <paramref name="container"/>/<paramref name="key"/> 解析为受 <see cref="LocalFileSystemFileStorageOptions.RootPath"/> 约束的绝对路径。</summary>
    /// <param name="container">容器（顶层目录）名称。</param>
    /// <param name="key">对象键（相对路径）。</param>
    /// <returns>解析出的绝对路径。</returns>
    /// <exception cref="ArgumentException">解析结果逃逸出配置的根目录时抛出（路径穿越防护）。</exception>
    private string ResolvePath(string container, string key)
    {
        string root = Path.GetFullPath(options.Value.RootPath);
        string full = Path.GetFullPath(Path.Combine(root, container, key));
        string relativePath = Path.GetRelativePath(root, full);

        if (relativePath.Equals("..", StringComparison.Ordinal)
            || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException("Resolved path escapes the configured RootPath.", nameof(key));
        }

        return full;
    }

    /// <summary>获取指定文件对应的 sidecar 元数据文件路径。</summary>
    /// <param name="path">原始文件路径。</param>
    /// <returns>sidecar 元数据文件路径。</returns>
    private static string SidecarPath(string path) => $"{path}.meta.json";

    /// <summary>把元数据写入 sidecar 文件。</summary>
    /// <param name="path">原始文件路径。</param>
    /// <param name="metadata">文件元数据。</param>
    /// <param name="eTag">生成的 ETag。</param>
    /// <param name="uploadedTime">上传时间。</param>
    /// <param name="ct">取消令牌。</param>
    private static async Task WriteSidecarAsync(string path, FileMetadata metadata, string eTag, DateTimeOffset uploadedTime, CancellationToken ct)
    {
        SidecarMetadata sidecar = new(metadata, eTag, uploadedTime);

        await using FileStream stream = File.Create(SidecarPath(path));
        await JsonSerializer.SerializeAsync(stream, sidecar, metadataSerializerOptions, ct).ConfigureAwait(false);
    }

    /// <summary>读取 sidecar 元数据文件；不存在时用文件系统自身信息构造一份兜底元数据。</summary>
    /// <param name="path">原始文件路径。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>读取或兜底构造出的元数据。</returns>
    private static async Task<SidecarMetadata> ReadSidecarAsync(string path, CancellationToken ct)
    {
        string sidecarPath = SidecarPath(path);

        if (!File.Exists(sidecarPath))
        {
            FileInfo info = new(path);

            return new SidecarMetadata(new FileMetadata("application/octet-stream"), string.Empty, info.LastWriteTimeUtc);
        }

        await using FileStream stream = File.OpenRead(sidecarPath);

        return await JsonSerializer.DeserializeAsync<SidecarMetadata>(stream, metadataSerializerOptions, ct).ConfigureAwait(false)
            ?? new SidecarMetadata(new FileMetadata("application/octet-stream"), string.Empty, DateTimeOffset.UtcNow);
    }

    private sealed record class SidecarMetadata(FileMetadata Metadata, string ETag, DateTimeOffset UploadedTime);
}
