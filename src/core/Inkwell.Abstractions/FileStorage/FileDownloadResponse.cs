// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 下载响应；<see cref="Content"/> 的生命周期由调用方通过 <see cref="IDisposable"/> 管理。
/// </summary>
/// <param name="content">文件内容流。</param>
/// <param name="metadata">文件元数据。</param>
/// <param name="eTag">文件实体标记。</param>
/// <param name="sizeBytes">文件大小（字节）。</param>
/// <param name="uploadedTime">文件上传时间。</param>
public sealed class FileDownloadResponse(Stream content, FileMetadata metadata, string eTag, long sizeBytes, DateTimeOffset uploadedTime)
    : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// 获取文件内容流。
    /// </summary>
    public Stream Content { get; } = content;

    /// <summary>
    /// 获取文件元数据。
    /// </summary>
    public FileMetadata Metadata { get; } = metadata;

    /// <summary>
    /// 获取文件实体标记。
    /// </summary>
    public string ETag { get; } = eTag;

    /// <summary>
    /// 获取文件大小（字节）。
    /// </summary>
    public long SizeBytes { get; } = sizeBytes;

    /// <summary>
    /// 获取文件上传时间。
    /// </summary>
    public DateTimeOffset UploadedTime { get; } = uploadedTime;

    /// <summary>
    /// 释放文件内容流。
    /// </summary>
    public void Dispose() => this.Content.Dispose();

    /// <summary>
    /// 异步释放文件内容流。
    /// </summary>
    /// <returns>表示异步释放操作的值任务。</returns>
    public async ValueTask DisposeAsync() => await this.Content.DisposeAsync().ConfigureAwait(false);
}
