using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using Inkwell;

namespace Inkwell.FileStorage.MinIO;

/// <summary><see cref="IInkwellBuilder"/> 的 MinIO Provider 唯一入口扩展（ADR-015）。</summary>
public static class InkwellFileStorageMinIOServiceCollectionExtensions
{
    /// <summary>
    /// 注册基于 <see cref="Minio"/> SDK 的 <see cref="IFileStorageProvider"/> 实现。
    /// </summary>
    /// <param name="builder">Inkwell Builder DSL 入口。</param>
    /// <param name="endpoint">MinIO 服务端点（形如 <c>host:port</c>，不含协议前缀）。</param>
    /// <param name="accessKey">访问密钥（Access Key）。</param>
    /// <param name="secretKey">秘密密钥（Secret Key）。</param>
    /// <param name="useSsl">是否通过 HTTPS 连接。</param>
    /// <param name="configure">可选的 <see cref="FileStorageOptions"/> 编程式追加配置。</param>
    /// <returns>供链式调用的 <paramref name="builder"/>。</returns>
    public static IInkwellBuilder UseMinIOFileStorage(this IInkwellBuilder builder, string endpoint, string accessKey, string secretKey, bool useSsl = false, Action<FileStorageOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);

        OptionsBuilder<FileStorageOptions> optionsBuilder = builder.Services.AddOptions<FileStorageOptions>()
            .BindConfiguration("Inkwell:FileStorage")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        builder.Services.AddSingleton<IMinioClient>(_ =>
            new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .WithSSL(useSsl)
                .Build());
        builder.Services.AddSingleton<IFileStorageProvider, MinIOFileStorageProvider>();

        return builder;
    }
}
