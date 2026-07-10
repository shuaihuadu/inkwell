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

        builder.Services.AddSingleton(_ => new BlobServiceClient(connectionString));
        builder.Services.AddSingleton<IFileStorageProvider, AzureBlobFileStorageProvider>();

        return builder;
    }
}
