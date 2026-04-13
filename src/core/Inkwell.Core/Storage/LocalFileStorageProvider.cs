namespace Inkwell;

/// <summary>
/// 本地文件系统存储提供程序
/// 使用本地磁盘作为文件存储后端，目录不存在时自动创建
/// </summary>
public sealed class LocalFileStorageProvider : IFileStorageProvider
{
    private readonly string _rootPath;

    /// <summary>
    /// 初始化本地文件存储提供程序
    /// </summary>
    /// <param name="rootPath">根目录路径</param>
    public LocalFileStorageProvider(string rootPath)
    {
        this._rootPath = Path.GetFullPath(rootPath);
        EnsureDirectoryExists(this._rootPath);
    }

    /// <inheritdoc />
    public async Task<string> UploadAsync(string path, Stream content, string? contentType = null, CancellationToken cancellationToken = default)
    {
        string fullPath = this.GetFullPath(path);
        string? directory = Path.GetDirectoryName(fullPath);

        if (directory is not null)
        {
            EnsureDirectoryExists(directory);
        }

        using FileStream fileStream = new(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);

        return fullPath;
    }

    /// <inheritdoc />
    public Task<Stream?> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        string fullPath = this.GetFullPath(path);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        string fullPath = this.GetFullPath(path);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult(false);
        }

        File.Delete(fullPath);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        string fullPath = this.GetFullPath(path);
        return Task.FromResult(File.Exists(fullPath));
    }

    /// <inheritdoc />
    public Task<string?> GetUriAsync(string path, CancellationToken cancellationToken = default)
    {
        string fullPath = this.GetFullPath(path);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<string?>(null);
        }

        return Task.FromResult<string?>(new Uri(fullPath).AbsoluteUri);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListAsync(string? directory = null, CancellationToken cancellationToken = default)
    {
        string targetDirectory = directory is not null
            ? this.GetFullPath(directory)
            : this._rootPath;

        if (!Directory.Exists(targetDirectory))
        {
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        IReadOnlyList<string> files = Directory.GetFiles(targetDirectory, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(this._rootPath, f).Replace('\\', '/'))
            .ToList()
            .AsReadOnly();

        return Task.FromResult(files);
    }

    private string GetFullPath(string relativePath)
    {
        // 防止路径遍历攻击
        string fullPath = Path.GetFullPath(Path.Combine(this._rootPath, relativePath));

        if (!fullPath.StartsWith(this._rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Access denied: path '{relativePath}' is outside the root directory.");
        }

        return fullPath;
    }

    private static void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }
}
