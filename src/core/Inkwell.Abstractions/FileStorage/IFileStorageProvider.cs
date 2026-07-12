// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 文件存储端口 facade。三 Provider 切换：LocalFileSystem / AzureBlob / MinIO（ADR-015）。
/// </summary>
public interface IFileStorageProvider
{
    /// <summary>
    /// 上传文件对象。
    /// </summary>
    /// <param name="container">容器名称。</param>
    /// <param name="key">对象键。</param>
    /// <param name="content">文件内容流。</param>
    /// <param name="metadata">文件元数据。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>文件上传结果。</returns>
    Task<FileUploadResult> UploadAsync(string container, string key, Stream content, FileMetadata metadata, CancellationToken ct = default);

    /// <summary>
    /// 下载文件对象。
    /// </summary>
    /// <param name="container">容器名称。</param>
    /// <param name="key">对象键。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>包含内容流和元数据的下载响应。</returns>
    Task<FileDownloadResponse> DownloadAsync(string container, string key, CancellationToken ct = default);

    /// <summary>
    /// 检查文件对象是否存在。
    /// </summary>
    /// <param name="container">容器名称。</param>
    /// <param name="key">对象键。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>对象存在时为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
    Task<bool> ExistsAsync(string container, string key, CancellationToken ct = default);

    /// <summary>
    /// 删除文件对象。
    /// </summary>
    /// <param name="container">容器名称。</param>
    /// <param name="key">对象键。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task DeleteAsync(string container, string key, CancellationToken ct = default);

    /// <summary>
    /// 创建预签名上传 URL。
    /// </summary>
    /// <param name="container">容器名称。</param>
    /// <param name="key">对象键。</param>
    /// <param name="ttl">URL 有效期。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>预签名上传 URL。</returns>
    Task<Uri> CreatePresignedUploadUrlAsync(string container, string key, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>
    /// 创建预签名下载 URL。
    /// </summary>
    /// <param name="container">容器名称。</param>
    /// <param name="key">对象键。</param>
    /// <param name="ttl">URL 有效期。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>预签名下载 URL。</returns>
    Task<Uri> CreatePresignedDownloadUrlAsync(string container, string key, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>
    /// 异步枚举容器中的文件对象。
    /// </summary>
    /// <param name="container">容器名称。</param>
    /// <param name="prefix">对象键前缀筛选条件。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>文件对象信息的异步序列。</returns>
    IAsyncEnumerable<FileObjectInfo> ListAsync(string container, string? prefix = null, CancellationToken ct = default);
}
