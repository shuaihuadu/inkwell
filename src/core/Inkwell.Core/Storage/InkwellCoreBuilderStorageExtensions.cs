using Microsoft.Extensions.DependencyInjection;

namespace Inkwell;

/// <summary>
/// 文件存储服务注册扩展方法
/// </summary>
public static class InkwellCoreBuilderStorageExtensions
{
    /// <summary>
    /// 使用本地文件系统作为文件存储后端
    /// </summary>
    /// <param name="builder">Inkwell 核心构建器</param>
    /// <param name="rootPath">存储根目录路径，默认为当前目录下的 storage 文件夹</param>
    /// <returns>Inkwell 核心构建器</returns>
    public static InkwellCoreBuilder UseLocalFileStorage(this InkwellCoreBuilder builder, string rootPath = "storage")
    {
        builder.Services.AddSingleton<IFileStorageProvider>(new LocalFileStorageProvider(rootPath));
        return builder;
    }
}
