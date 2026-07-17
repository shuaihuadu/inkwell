// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.FileStorage.Local;

/// <summary><see cref="IInkwellBuilder"/> 的进程内默认 FileStorage Provider 唯一入口扩展（dev / unit test）。</summary>
public static class LocalFileSystemBuilderExtensions
{
    /// <summary>注册本地磁盘默认的 <see cref="IFileStorageProvider"/> 实现。</summary>
    /// <param name="builder">Inkwell Builder DSL 入口。</param>
    /// <param name="configure">可选的 <see cref="LocalFileSystemFileStorageOptions"/> 编程式追加配置。</param>
    /// <returns>供链式调用的 <paramref name="builder"/>。</returns>
    public static IInkwellBuilder UseLocalFileSystemFileStorage(this IInkwellBuilder builder, Action<LocalFileSystemFileStorageOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<IFileStorageProvider, LocalFileSystemFileStorageProvider>();

        OptionsBuilder<LocalFileSystemFileStorageOptions> optionsBuilder = builder.Services.AddOptions<LocalFileSystemFileStorageOptions>();

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        return builder;
    }
}
