using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Inkwell;

/// <summary>
/// 公开 <c>AddInkwell()</c> 静态扩展方法，唯一用户入口。
/// </summary>
public static class InkwellServiceCollectionExtensions
{
    public static IInkwellBuilder AddInkwell(this IServiceCollection services, IConfiguration configuration, string sectionName = "Inkwell")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // TryAddSingleton：真实 WebApi/Worker 场景下 WebApplication.CreateBuilder()/Host.CreateApplicationBuilder()
        // 已经注册过 IConfiguration，这里不覆盖；裸 IServiceCollection（如单元测试）场景下补上，
        // 否则各 Use*() Provider 扩展内部的 BindConfiguration(...) 在 DI 容器里解析不到 IConfiguration 会运行时崩溃
        // （2026-07-09 Testcontainers spike 验证坐实的真实 bug）。
        services.TryAddSingleton(configuration);

        services.AddOptions<InkwellOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateOnStart();

        return new InkwellBuilder(services, configuration);
    }
}
