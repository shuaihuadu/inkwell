// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 公开 <c>AddInkwell()</c> 静态扩展方法，唯一用户入口。
/// </summary>
public static class InkwellServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Inkwell 服务并创建 Builder。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configuration">应用配置。</param>
    /// <param name="sectionName">根配置节名称。</param>
    /// <returns>用于继续装配 Provider 的 Builder。</returns>
    public static IInkwellBuilder AddInkwell(this IServiceCollection services, IConfiguration configuration, string sectionName = "Inkwell")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton(configuration);

        services.AddOptions<InkwellOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateOnStart();

        return new InkwellBuilder(services, configuration);
    }
}
