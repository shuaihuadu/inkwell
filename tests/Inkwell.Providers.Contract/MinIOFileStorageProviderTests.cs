// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Testcontainers.Minio;
using Inkwell;
using Inkwell.FileStorage.MinIO;

namespace Inkwell.Providers.Contract;

/// <summary>
/// 针对 <see cref="MinIOFileStorageProvider"/> 的真实 Testcontainers 集成测试。覆盖上传 / 下载 /
/// 存在性判断 / 删除 / 列举的完整往返，而非仅编译期验证。
/// </summary>
[TestClass]
public sealed class MinIOFileStorageProviderTests
{
    private static MinioContainer? s_container;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext _)
    {
        s_container = new MinioBuilder(ContainerImageConfiguration.GetRequired("Tests:MinIO")).Build();

        await s_container.StartAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        if (s_container is not null)
        {
            await s_container.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task UploadAsync_Then_DownloadAsync_Roundtrips_Content()
    {
        IFileStorageProvider storage = BuildFileStorageProvider();
        string container = $"inkwell-test-{Guid.NewGuid():N}";
        byte[] payload = "hello minio"u8.ToArray();

        await using (MemoryStream uploadStream = new MemoryStream(payload))
        {
            await storage.UploadAsync(container, "docs/a.txt", uploadStream, new FileMetadata("text/plain"));
        }

        FileDownloadResponse response = await storage.DownloadAsync(container, "docs/a.txt");

        await using MemoryStream downloaded = new MemoryStream();
        await response.Content.CopyToAsync(downloaded);

        CollectionAssert.AreEqual(payload, downloaded.ToArray());
    }

    [TestMethod]
    public async Task ExistsAsync_Then_DeleteAsync_Reflects_Object_Lifecycle()
    {
        IFileStorageProvider storage = BuildFileStorageProvider();
        string container = $"inkwell-test-{Guid.NewGuid():N}";
        byte[] payload = "to be deleted"u8.ToArray();

        await using (MemoryStream uploadStream = new MemoryStream(payload))
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
    public async Task ListAsync_Returns_Uploaded_Objects_Under_Prefix()
    {
        IFileStorageProvider storage = BuildFileStorageProvider();
        string container = $"inkwell-test-{Guid.NewGuid():N}";
        byte[] payload = "listing"u8.ToArray();

        await using (MemoryStream uploadStream = new MemoryStream(payload))
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
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        IInkwellBuilder builder = services.AddInkwell(new ConfigurationBuilder().Build());

        string endpoint = s_container!.GetConnectionString()
            .Replace("http://", string.Empty, StringComparison.Ordinal)
            .Replace("https://", string.Empty, StringComparison.Ordinal);

        builder.UseMinIOFileStorage(endpoint, s_container.GetAccessKey(), s_container.GetSecretKey(), useSsl: false);

        ServiceProvider provider = builder.Services.BuildServiceProvider();

        return provider.GetRequiredService<IFileStorageProvider>();
    }
}
