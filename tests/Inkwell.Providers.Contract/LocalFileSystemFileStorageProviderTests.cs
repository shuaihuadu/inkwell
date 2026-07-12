// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.FileStorage.Local;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inkwell.Providers.Contract;

/// <summary>
/// 验证本地文件系统 Provider 的对象生命周期、列举行为和路径穿越防护。
/// </summary>
[TestClass]
public sealed class LocalFileSystemFileStorageProviderTests
{
    [TestMethod]
    public async Task UploadAsync_Then_DownloadAsync_RoundtripsContentAsync()
    {
        // Arrange
        string rootPath = CreateRootPath();
        IFileStorageProvider storage = BuildFileStorageProvider(rootPath);
        byte[] payload = "hello local storage"u8.ToArray();

        try
        {
            // Act
            await using (MemoryStream uploadStream = new(payload))
            {
                await storage.UploadAsync("documents", "notes/a.txt", uploadStream, new FileMetadata("text/plain"));
            }

            FileDownloadResponse response = await storage.DownloadAsync("documents", "notes/a.txt");
            await using MemoryStream downloaded = new();
            await response.Content.CopyToAsync(downloaded);

            // Assert
            CollectionAssert.AreEqual(payload, downloaded.ToArray());
            Assert.AreEqual("text/plain", response.Metadata.ContentType);
            Assert.AreEqual(36, response.ETag.Length);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [TestMethod]
    public async Task DeleteAsync_Then_ListAsync_RemovesObjectAndSidecarAsync()
    {
        // Arrange
        string rootPath = CreateRootPath();
        IFileStorageProvider storage = BuildFileStorageProvider(rootPath);

        try
        {
            await using (MemoryStream uploadStream = new("delete me"u8.ToArray()))
            {
                await storage.UploadAsync("documents", "notes/b.txt", uploadStream, new FileMetadata("text/plain"));
            }

            // Act
            await storage.DeleteAsync("documents", "notes/b.txt");
            bool exists = await storage.ExistsAsync("documents", "notes/b.txt");
            List<FileObjectInfo> objects = [];

            await foreach (FileObjectInfo fileObject in storage.ListAsync("documents", "notes/"))
            {
                objects.Add(fileObject);
            }

            // Assert
            Assert.IsFalse(exists);
            Assert.HasCount(0, objects);
            Assert.IsFalse(File.Exists(Path.Combine(rootPath, "documents", "notes", "b.txt.meta.json")));
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [TestMethod]
    public async Task UploadAsync_WhenKeyEscapesRoot_ThrowsArgumentExceptionAsync()
    {
        // Arrange
        string rootPath = CreateRootPath();
        IFileStorageProvider storage = BuildFileStorageProvider(rootPath);
        string escapedDirectoryName = $"{Path.GetFileName(rootPath)}-escape";

        try
        {
            await using MemoryStream uploadStream = new("blocked"u8.ToArray());

            // Act
            Task<FileUploadResult> action = storage.UploadAsync(
                "..",
                Path.Combine(escapedDirectoryName, "escaped.txt"),
                uploadStream,
                new FileMetadata("text/plain"));

            // Assert
            await Assert.ThrowsExactlyAsync<ArgumentException>(() => action);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    private static string CreateRootPath()
    {
        string rootPath = Path.Combine(Path.GetTempPath(), "inkwell-file-storage-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(rootPath);
        return rootPath;
    }

    private static IFileStorageProvider BuildFileStorageProvider(string rootPath)
    {
        ServiceCollection services = new();
        IInkwellBuilder builder = services.AddInkwell(new ConfigurationBuilder().Build());

        builder.UseLocalFileSystemFileStorage(options => options.RootPath = rootPath);

        ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IFileStorageProvider>();
    }
}