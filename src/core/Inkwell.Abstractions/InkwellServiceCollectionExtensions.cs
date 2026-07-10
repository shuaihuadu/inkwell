// Copyright (c) ShuaiHua Du. All rights reserved.

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

        services.TryAddSingleton(configuration);

        services.AddOptions<InkwellOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateOnStart();

        return new InkwellBuilder(services, configuration);
    }
}
