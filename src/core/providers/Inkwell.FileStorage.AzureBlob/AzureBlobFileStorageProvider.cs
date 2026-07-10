using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Inkwell;

namespace Inkwell.FileStorage.AzureBlob;

/// <summary>基于 <see cref="Azure.Storage.Blobs"/> 的 <see cref="IFileStorageProvider"/> 实现（ADR-015）。</summary>
internal sealed class AzureBlobFileStorageProvider(BlobServiceClient serviceClient) : IFileStorageProvider
{
    /// <inheritdoc />
    public async Task<FileUploadResult> UploadAsync(string container, string key, Stream content, FileMetadata metadata, CancellationToken ct = default)
    {
        BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(container);

        await containerClient.CreateIfNotExistsAsync(cancellationToken: ct).ConfigureAwait(false);

        BlobClient blobClient = containerClient.GetBlobClient(key);

        BlobUploadOptions uploadOptions = new()
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = metadata.ContentType,
                ContentDisposition = metadata.ContentDisposition,
            },
            Metadata = metadata.CustomMetadata?.ToDictionary(kv => kv.Key, kv => kv.Value),
        };

        Response<BlobContentInfo> response = await blobClient.UploadAsync(content, uploadOptions, ct).ConfigureAwait(false);

        return new FileUploadResult(container, key, content.Length, response.Value.ETag.ToString(), response.Value.LastModified);
    }

    /// <inheritdoc />
    public async Task<FileDownloadResponse> DownloadAsync(string container, string key, CancellationToken ct = default)
    {
        BlobClient blobClient = serviceClient.GetBlobContainerClient(container).GetBlobClient(key);

        Response<BlobDownloadStreamingResult> response = await blobClient.DownloadStreamingAsync(cancellationToken: ct).ConfigureAwait(false);
        BlobDownloadStreamingResult result = response.Value;

        FileMetadata metadata = new(result.Details.ContentType, result.Details.Metadata?.ToDictionary(kv => kv.Key, kv => kv.Value), result.Details.ContentDisposition);

        return new FileDownloadResponse(result.Content, metadata, result.Details.ETag.ToString(), result.Details.ContentLength, result.Details.LastModified);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string container, string key, CancellationToken ct = default)
    {
        BlobClient blobClient = serviceClient.GetBlobContainerClient(container).GetBlobClient(key);

        Response<bool> response = await blobClient.ExistsAsync(ct).ConfigureAwait(false);

        return response.Value;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string container, string key, CancellationToken ct = default)
    {
        BlobClient blobClient = serviceClient.GetBlobContainerClient(container).GetBlobClient(key);

        await blobClient.DeleteIfExistsAsync(cancellationToken: ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Uri> CreatePresignedUploadUrlAsync(string container, string key, TimeSpan ttl, CancellationToken ct = default) =>
        Task.FromResult(this.GenerateSasUri(container, key, ttl, BlobSasPermissions.Create | BlobSasPermissions.Write));

    /// <inheritdoc />
    public Task<Uri> CreatePresignedDownloadUrlAsync(string container, string key, TimeSpan ttl, CancellationToken ct = default) =>
        Task.FromResult(this.GenerateSasUri(container, key, ttl, BlobSasPermissions.Read));

    /// <inheritdoc />
    public async IAsyncEnumerable<FileObjectInfo> ListAsync(string container, string? prefix = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(container);

        await foreach (BlobItem item in containerClient.GetBlobsAsync(traits: BlobTraits.None, states: BlobStates.None, prefix: prefix, cancellationToken: ct).ConfigureAwait(false))
        {
            yield return new FileObjectInfo(
                container,
                item.Name,
                item.Properties.ContentLength ?? 0,
                item.Properties.ETag?.ToString() ?? string.Empty,
                item.Properties.LastModified ?? DateTimeOffset.UtcNow,
                item.Properties.ContentType);
        }
    }

    /// <summary>为指定 blob 生成一个带 SAS 令牌的可直接访问 URI。</summary>
    /// <param name="container">容器名称。</param>
    /// <param name="key">Blob 名称（对象键）。</param>
    /// <param name="ttl">URI 的有效期。</param>
    /// <param name="permissions">URI 授予的权限（读 / 写 / 创建）。</param>
    /// <returns>带 SAS 令牌的 URI。</returns>
    /// <exception cref="InvalidOperationException">当前 <see cref="BlobServiceClient"/> 的凭据不支持生成 SAS URI 时抛出。</exception>
    private Uri GenerateSasUri(string container, string key, TimeSpan ttl, BlobSasPermissions permissions)
    {
        BlobClient blobClient = serviceClient.GetBlobContainerClient(container).GetBlobClient(key);

        if (!blobClient.CanGenerateSasUri)
        {
            throw new InvalidOperationException("The configured BlobServiceClient credential does not support SAS URI generation.");
        }

        return blobClient.GenerateSasUri(permissions, DateTimeOffset.UtcNow.Add(ttl));
    }
}
