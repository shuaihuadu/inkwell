// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.FileStorage.AzureBlob;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Azurite;

namespace Inkwell.Providers.Contract;

/// <summary>
/// 针对 <see cref="AzureBlobFileStorageProvider"/> 的真实 Testcontainers 集成测试（基于
/// Azurite 模拟器）。覆盖上传 / 下载 / 存在性判断 / 删除的完整往返，而非仅编译期验证。
/// </summary>
[TestClass]
public sealed class AzureBlobFileStorageProviderTests
{
    private static AzuriteContainer? s_container;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext _)
    {
        s_container = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest").Build();

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
        byte[] payload = "hello azurite"u8.ToArray();

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

    private static IFileStorageProvider BuildFileStorageProvider()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        IInkwellBuilder builder = services.AddInkwell(new ConfigurationBuilder().Build());

        builder.UseAzureBlobFileStorage(s_container!.GetConnectionString());

        ServiceProvider provider = builder.Services.BuildServiceProvider();

        return provider.GetRequiredService<IFileStorageProvider>();
    }
}
