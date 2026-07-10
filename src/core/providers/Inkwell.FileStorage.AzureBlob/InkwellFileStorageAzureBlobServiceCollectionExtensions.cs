using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Inkwell;

namespace Inkwell.FileStorage.AzureBlob;

/// <summary><see cref="IInkwellBuilder"/> 的 Azure Blob Storage Provider 唯一入口扩展（ADR-015）。</summary>
public static class InkwellFileStorageAzureBlobServiceCollectionExtensions
{
    /// <summary>
    /// 注册基于 <see cref="Azure.Storage.Blobs"/> 的 <see cref="IFileStorageProvider"/> 实现。
    /// </summary>
    /// <param name="builder">Inkwell Builder DSL 入口。</param>
    /// <param name="connectionString">Azure Storage 账号连接字符串。</param>
    /// <param name="configure">可选的 <see cref="FileStorageOptions"/> 编程式追加配置。</param>
    /// <returns>供链式调用的 <paramref name="builder"/>。</returns>
    public static IInkwellBuilder UseAzureBlobFileStorage(this IInkwellBuilder builder, string connectionString, Action<FileStorageOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        OptionsBuilder<FileStorageOptions> optionsBuilder = builder.Services.AddOptions<FileStorageOptions>()
            .BindConfiguration("Inkwell:FileStorage")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        // 显式锁定一个 Azure Storage REST API 版本（而非 SDK 默认的最新版本）：Azurite 模拟器的 API
        // 版本支持通常滞后于 Azure.Storage.Blobs SDK 的最新版本，锁定较早但仍受官方支持的稳定版本
        // 可以同时兼容 Azurite（dev / unit test）与真实 Azure Storage（prod）。
        BlobClientOptions options = new(BlobClientOptions.ServiceVersion.V2025_01_05);

        builder.Services.AddSingleton(_ => new BlobServiceClient(connectionString, options));
        builder.Services.AddSingleton<IFileStorageProvider, AzureBlobFileStorageProvider>();

        return builder;
    }
}
