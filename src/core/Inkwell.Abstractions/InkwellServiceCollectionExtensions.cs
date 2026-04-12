using Microsoft.Extensions.DependencyInjection;

namespace Inkwell;

/// <summary>
/// Inkwell 核心服务注册扩展方法
/// </summary>
public static class InkwellServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Inkwell 核心服务，返回构建器用于链式配置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>Inkwell 核心服务构建器</returns>
    public static InkwellCoreBuilder AddInkwellCore(this IServiceCollection services)
    {
        // 注册核心服务（未来可添加更多）
        return new InkwellCoreBuilder(services);
    }
}
