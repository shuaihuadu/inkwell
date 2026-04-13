using Inkwell;
using Inkwell.Agents;
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

        // SessionPersistenceProvider 注册为 Singleton（因为被 Singleton 中间件和服务引用）
        // 内部每次操作通过 IServiceScopeFactory 创建 scope 来获取 DbContext
        services.AddSingleton<ISessionPersistenceProvider>(sp =>
        {
            IServiceScopeFactory scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            return new EfCoreScopedSessionPersistenceProvider(scopeFactory);
        });
        return services;
    }
}
