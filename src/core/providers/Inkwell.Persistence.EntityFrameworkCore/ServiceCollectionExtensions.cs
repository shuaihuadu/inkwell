using Inkwell;
using Microsoft.Extensions.DependencyInjection;

namespace Inkwell.Persistence.EntityFrameworkCore;

/// <summary>
/// EF Core 持久化服务注册扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册 EF Core 持久化提供程序
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddInkwellEfCorePersistence(this IServiceCollection services)
    {
        services.AddScoped<IArticlePersistenceProvider, EfCoreArticlePersistenceProvider>();
        services.AddScoped<IPipelineRunPersistenceProvider, EfCorePipelineRunPersistenceProvider>();
        services.AddScoped<IAnalysisPersistenceProvider, EfCoreAnalysisPersistenceProvider>();
        services.AddScoped<IReviewPersistenceProvider, EfCoreReviewPersistenceProvider>();
        return services;
    }
}
