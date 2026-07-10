namespace Inkwell;

/// <summary>
/// 下载响应；<see cref="Content"/> 的生命周期由调用方通过 <see cref="IDisposable"/> 管理。
/// </summary>
public sealed class FileDownloadResponse(Stream content, FileMetadata metadata, string eTag, long sizeBytes, DateTimeOffset uploadedTime)
    : IDisposable, IAsyncDisposable
{
    public Stream Content { get; } = content;

    public FileMetadata Metadata { get; } = metadata;

    public string ETag { get; } = eTag;

    public long SizeBytes { get; } = sizeBytes;

    public DateTimeOffset UploadedTime { get; } = uploadedTime;

    public void Dispose() => this.Content.Dispose();

    public async ValueTask DisposeAsync() => await this.Content.DisposeAsync().ConfigureAwait(false);
}
