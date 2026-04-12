namespace Inkwell;

/// <summary>
/// 文件存储提供程序接口
/// </summary>
public interface IFileStorageProvider
{
    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="path">文件路径（相对路径）</param>
    /// <param name="content">文件内容流</param>
    /// <param name="contentType">文件 MIME 类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件访问 URI</returns>
    Task<string> UploadAsync(string path, Stream content, string? contentType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="path">文件路径（相对路径）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件内容流，文件不存在时返回 null</returns>
    Task<Stream?> DownloadAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="path">文件路径（相对路径）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功删除</returns>
    Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    /// <param name="path">文件路径（相对路径）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件是否存在</returns>
    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文件的公开访问 URI
    /// </summary>
    /// <param name="path">文件路径（相对路径）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件访问 URI，文件不存在时返回 null</returns>
    Task<string?> GetUriAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出指定目录下的文件
    /// </summary>
    /// <param name="directory">目录路径（相对路径）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件路径列表</returns>
    Task<IReadOnlyList<string>> ListAsync(string? directory = null, CancellationToken cancellationToken = default);
}
