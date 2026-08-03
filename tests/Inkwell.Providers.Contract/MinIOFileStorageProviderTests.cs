// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.FileStorage.MinIO;
using Testcontainers.Minio;

namespace Inkwell.Providers.Contract;

/// <summary>
/// 针对 <see cref="MinIOFileStorageProvider"/> 的真实 Testcontainers 集成测试。覆盖上传 / 下载 /
/// 存在性判断 / 删除 / 列举的完整往返，而非仅编译期验证。
/// </summary>
[TestClass]
public sealed class MinIOFileStorageProviderTests
{
    private static MinioContainer? minioContainer;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext _)
    {
        minioContainer = new MinioBuilder(ContainerImageConfiguration.GetRequired("Tests:MinIO")).Build();

        await minioContainer.StartAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        if (minioContainer is not null)
        {
            await minioContainer.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task UploadAsync_Then_DownloadAsync_Roundtrips_ContentAsync()
    {
        IFileStorageProvider storage = BuildFileStorageProvider();
        string container = $"inkwell-test-{Guid.NewGuid():N}";
        byte[] payload = "hello minio"u8.ToArray();

        await using (MemoryStream uploadStream = new(payload))
        {
            await storage.UploadAsync(container, "docs/a.txt", uploadStream, new FileMetadata("text/plain"));
        }

        FileDownloadResponse response = await storage.DownloadAsync(container, "docs/a.txt");

        await using MemoryStream downloaded = new();
        await response.Content.CopyToAsync(downloaded);

        CollectionAssert.AreEqual(payload, downloaded.ToArray());
    }

    [TestMethod]
    public async Task ExistsAsync_Then_DeleteAsync_Reflects_Object_LifecycleAsync()
    {
        IFileStorageProvider storage = BuildFileStorageProvider();
        string container = $"inkwell-test-{Guid.NewGuid():N}";
        byte[] payload = "to be deleted"u8.ToArray();

        await using (MemoryStream uploadStream = new(payload))
        {
            await storage.UploadAsync(container, "docs/b.txt", uploadStream, new FileMetadata("text/plain"));
        }

        bool existsBeforeDelete = await storage.ExistsAsync(container, "docs/b.txt");

        await storage.DeleteAsync(container, "docs/b.txt");

        bool existsAfterDelete = await storage.ExistsAsync(container, "docs/b.txt");

        Assert.IsTrue(existsBeforeDelete);
        Assert.IsFalse(existsAfterDelete);
    }

    [TestMethod]
    public async Task ListAsync_Returns_Uploaded_Objects_Under_PrefixAsync()
    {
        IFileStorageProvider storage = BuildFileStorageProvider();
        string container = $"inkwell-test-{Guid.NewGuid():N}";
        byte[] payload = "listing"u8.ToArray();

        await using (MemoryStream uploadStream = new(payload))
        {
            await storage.UploadAsync(container, "prefix/c.txt", uploadStream, new FileMetadata("text/plain"));
        }

        List<string> keys = [];

        await foreach (FileObjectInfo info in storage.ListAsync(container, "prefix/"))
        {
            keys.Add(info.Key);
        }

        CollectionAssert.Contains(keys, "prefix/c.txt");
    }

    private static IFileStorageProvider BuildFileStorageProvider()
    {
        ServiceCollection services = new();
        services.AddLogging();

        IInkwellBuilder builder = services.AddInkwell(new ConfigurationBuilder().Build());

        string endpoint = minioContainer!.GetConnectionString()
            .Replace("http://", string.Empty, StringComparison.Ordinal)
            .Replace("https://", string.Empty, StringComparison.Ordinal);

        builder.UseMinIOFileStorage(endpoint, minioContainer.GetAccessKey(), minioContainer.GetSecretKey(), useSsl: false);

        ServiceProvider provider = builder.Services.BuildServiceProvider();

        return provider.GetRequiredService<IFileStorageProvider>();
    }
}
