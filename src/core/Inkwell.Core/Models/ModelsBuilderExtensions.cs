// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Inkwell;

/// <summary>注册 <see cref="IModelCatalogService"/> 默认实现。</summary>
public static class ModelsBuilderExtensions
{
    /// <summary>
    /// 注册默认模型目录服务。
    /// </summary>
    /// <param name="builder">Inkwell 构建器。</param>
    /// <param name="configure">模型目录配置委托。</param>
    /// <returns>当前 Inkwell 构建器。</returns>
    public static IInkwellBuilder AddDefaultModelCatalog(this IInkwellBuilder builder, Action<ModelCatalogOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<IModelCatalogService, ModelCatalogService>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<ModelCatalogOptions>, ModelCatalogOptionsValidator>());

        OptionsBuilder<ModelCatalogOptions> optionsBuilder = builder.Services.AddOptions<ModelCatalogOptions>().Bind(builder.Configuration.GetSection("Inkwell:Models"));

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        return builder;
    }
}
