using Microsoft.Extensions.DependencyInjection;

namespace Inkwell;

/// <summary>
/// InMemory 队列服务注册扩展方法
/// </summary>
public static class InkwellCoreBuilderQueueExtensions
{
    /// <summary>
    /// 使用内存队列（适用于单进程开发调试）
    /// </summary>
    /// <param name="builder">Inkwell 核心构建器</param>
    /// <returns>Inkwell 核心构建器</returns>
    public static InkwellCoreBuilder UseInMemoryQueue(this InkwellCoreBuilder builder)
    {
        builder.Services.AddSingleton(typeof(IQueueProvider<>), typeof(InMemoryQueueProvider<>));
        builder.Services.AddSingleton(typeof(IPubSubProvider<>), typeof(InMemoryPubSubProvider<>));
        return builder;
    }
}
