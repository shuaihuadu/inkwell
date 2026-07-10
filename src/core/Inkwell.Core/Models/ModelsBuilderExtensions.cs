using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Inkwell;

/// <summary>注册 <see cref="IModelCatalogService"/> 默认实现。</summary>
public static class ModelsBuilderExtensions
{
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
