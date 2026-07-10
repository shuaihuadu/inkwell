using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Inkwell;

/// <summary>
/// <see cref="IInkwellBuilder"/> 的唯一内部实现；构造仅供 <see cref="InkwellServiceCollectionExtensions"/> 调用。
/// </summary>
internal sealed class InkwellBuilder(IServiceCollection services, IConfiguration configuration) : IInkwellBuilder
{
    private readonly List<Action<InkwellOptions>> _optionsConfigurators = [];
    private bool _built;

    public IServiceCollection Services { get; } = services;

    public IConfiguration Configuration { get; } = configuration;

    public IInkwellBuilder ConfigureOptions(Action<InkwellOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        this._optionsConfigurators.Add(configure);

        return this;
    }

    public IServiceCollection Build()
    {
        if (this._built)
        {
            throw new InvalidOperationException("AddInkwell().Build() 已调用，不可重入。");
        }

        this._built = true;

        foreach (Action<InkwellOptions> configurator in this._optionsConfigurators)
        {
            this.Services.Configure(configurator);
        }

        this.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<InkwellOptions>, InkwellOptionsValidator>());

        this.EnsureRequiredPortRegistered<IPersistenceProvider>("IPersistenceProvider", "UseSqlServer/UsePostgres");
        this.EnsureRequiredPortRegistered<IFileStorageProvider>("IFileStorageProvider", "UseLocalFileSystemFileStorage/UseMinIOFileStorage/UseAzureBlobFileStorage");
        this.EnsureRequiredPortRegistered<ICacheProvider>("ICacheProvider", "UseInMemoryCache/UseRedisCache");
        this.EnsureRequiredPortRegistered<IQueueProvider>("IQueueProvider", "UseChannelsQueue/UseRedisQueue");
        this.EnsureRequiredPortRegistered<IAgentRuntime>("IAgentRuntime", "UseAzureOpenAIAgentRuntime");

        return this.Services;
    }

    private void EnsureRequiredPortRegistered<TPort>(string portName, string suggestion)
        where TPort : class
    {
        if (!this.Services.Any(d => d.ServiceType == typeof(TPort)))
        {
            throw new InkwellBuilderException($"未注册 {portName}；请调用 .{suggestion} 之一。");
        }
    }
}
