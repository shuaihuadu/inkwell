using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Inkwell;

namespace Inkwell.FileStorage.MinIO;

/// <summary>基于 <see cref="Minio"/> SDK 的 <see cref="IFileStorageProvider"/> 实现（ADR-015）。</summary>
internal sealed class MinIOFileStorageProvider(IMinioClient client) : IFileStorageProvider
{
    /// <inheritdoc />
    public async Task<FileUploadResult> UploadAsync(string container, string key, Stream content, FileMetadata metadata, CancellationToken ct = default)
    {
        await this.EnsureBucketAsync(container, ct).ConfigureAwait(false);

        Dictionary<string, string> headers = new();

        if (metadata.CustomMetadata is not null)
        {
            foreach (KeyValuePair<string, string> pair in metadata.CustomMetadata)
            {
                headers[$"x-amz-meta-{pair.Key}"] = pair.Value;
            }
        }

        if (metadata.ContentDisposition is not null)
        {
            headers["Content-Disposition"] = metadata.ContentDisposition;
        }

        PutObjectArgs args = new PutObjectArgs()
            .WithBucket(container)
            .WithObject(key)
            .WithStreamData(content)
            .WithObjectSize(content.Length)
            .WithContentType(metadata.ContentType)
            .WithHeaders(headers);

        Minio.DataModel.Response.PutObjectResponse response = await client.PutObjectAsync(args, ct).ConfigureAwait(false);

        return new FileUploadResult(container, key, content.Length, response.Etag, DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public async Task<FileDownloadResponse> DownloadAsync(string container, string key, CancellationToken ct = default)
    {
        MemoryStream buffer = new();

        GetObjectArgs args = new GetObjectArgs()
            .WithBucket(container)
            .WithObject(key)
            .WithCallbackStream((stream, innerCt) => stream.CopyToAsync(buffer, innerCt));

        Minio.DataModel.ObjectStat stat = await client.GetObjectAsync(args, ct).ConfigureAwait(false);

        buffer.Position = 0;

        FileMetadata metadata = new(stat.ContentType, ToCustomMetadata(stat.MetaData));

        return new FileDownloadResponse(buffer, metadata, stat.ETag, stat.Size, stat.LastModified);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string container, string key, CancellationToken ct = default)
    {
        try
        {
            StatObjectArgs args = new StatObjectArgs().WithBucket(container).WithObject(key);

            await client.StatObjectAsync(args, ct).ConfigureAwait(false);

            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string container, string key, CancellationToken ct = default)
    {
        RemoveObjectArgs args = new RemoveObjectArgs().WithBucket(container).WithObject(key);

        await client.RemoveObjectAsync(args, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Uri> CreatePresignedUploadUrlAsync(string container, string key, TimeSpan ttl, CancellationToken ct = default)
    {
        PresignedPutObjectArgs args = new PresignedPutObjectArgs()
            .WithBucket(container)
            .WithObject(key)
            .WithExpiry((int)ttl.TotalSeconds);

        string url = await client.PresignedPutObjectAsync(args).ConfigureAwait(false);

        return new Uri(url);
    }

    /// <inheritdoc />
    public async Task<Uri> CreatePresignedDownloadUrlAsync(string container, string key, TimeSpan ttl, CancellationToken ct = default)
    {
        PresignedGetObjectArgs args = new PresignedGetObjectArgs()
            .WithBucket(container)
            .WithObject(key)
            .WithExpiry((int)ttl.TotalSeconds);

        string url = await client.PresignedGetObjectAsync(args).ConfigureAwait(false);

        return new Uri(url);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<FileObjectInfo> ListAsync(string container, string? prefix = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        ListObjectsArgs args = new ListObjectsArgs().WithBucket(container).WithRecursive(true);

        if (prefix is not null)
        {
            args = args.WithPrefix(prefix);
        }

        await foreach (Minio.DataModel.Item item in client.ListObjectsEnumAsync(args, ct).ConfigureAwait(false))
        {
            if (item.IsDir)
            {
                continue;
            }

            DateTimeOffset lastModified = DateTimeOffset.TryParse(item.LastModified, out DateTimeOffset parsed) ? parsed : DateTimeOffset.UtcNow;

            yield return new FileObjectInfo(container, item.Key, (long)item.Size, item.ETag, lastModified, null);
        }
    }

    /// <summary>确保 <paramref name="container"/> 对应的 bucket 已存在，不存在则创建。</summary>
    /// <param name="container">Bucket 名称。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task EnsureBucketAsync(string container, CancellationToken ct)
    {
        BucketExistsArgs existsArgs = new BucketExistsArgs().WithBucket(container);

        if (!await client.BucketExistsAsync(existsArgs, ct).ConfigureAwait(false))
        {
            MakeBucketArgs makeArgs = new MakeBucketArgs().WithBucket(container);

            await client.MakeBucketAsync(makeArgs, ct).ConfigureAwait(false);
        }
    }

    /// <summary>从对象的原始响应头中提取 <c>x-amz-meta-*</c> 自定义元数据，还原为不带前缀的键值对。</summary>
    /// <param name="metaData">MinIO SDK 返回的原始响应头字典。</param>
    /// <returns>还原后的自定义元数据；无自定义元数据时为 <see langword="null"/>。</returns>
    private static IReadOnlyDictionary<string, string>? ToCustomMetadata(IDictionary<string, string>? metaData)
    {
        if (metaData is null || metaData.Count == 0)
        {
            return null;
        }

        return metaData
            .Where(kv => kv.Key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(kv => kv.Key["x-amz-meta-".Length..], kv => kv.Value);
    }
}
