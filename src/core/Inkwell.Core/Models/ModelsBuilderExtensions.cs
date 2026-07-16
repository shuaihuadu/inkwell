// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Net.Http.Headers;

namespace Inkwell;

/// <summary>注册 <see cref="IModelRegistryService"/> 及模型来源。</summary>
public static class ModelsBuilderExtensions
{
    /// <summary>
    /// 注册默认模型注册表和配置文件模型来源。
    /// </summary>
    /// <param name="builder">Inkwell 构建器。</param>
    /// <param name="configure">配置文件模型来源配置委托。</param>
    /// <returns>当前 Inkwell 构建器。</returns>
    public static IInkwellBuilder AddModelRegistry(this IInkwellBuilder builder, Action<ConfigurationModelRegistryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<IModelRegistryService, ModelRegistryService>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IModelRegistrySource, ConfigurationModelRegistrySource>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<ConfigurationModelRegistryOptions>, ConfigurationModelRegistryOptionsValidator>());

        OptionsBuilder<ConfigurationModelRegistryOptions> optionsBuilder = builder.Services
            .AddOptions<ConfigurationModelRegistryOptions>()
            .Bind(builder.Configuration.GetSection("Inkwell:Models"));

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        optionsBuilder.ValidateOnStart();

        return builder;
    }

    /// <summary>
    /// 为模型注册表添加 LiteLLM 自动发现来源。
    /// </summary>
    /// <param name="builder">Inkwell 构建器。</param>
    /// <param name="configure">LiteLLM 模型发现配置委托。</param>
    /// <returns>用于链式调用的 Inkwell 构建器。</returns>
    public static IInkwellBuilder AddLiteLLMModelRegistrySource(
        this IInkwellBuilder builder,
        Action<LiteLLMModelRegistryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        OptionsBuilder<LiteLLMModelRegistryOptions> optionsBuilder = builder.Services
            .AddOptions<LiteLLMModelRegistryOptions>()
            .Bind(builder.Configuration.GetSection("Inkwell:LiteLLM"));

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<LiteLLMModelRegistryOptions>, LiteLLMModelRegistryOptionsValidator>());
        optionsBuilder.ValidateOnStart();
        builder.Services.AddHttpClient<LiteLLMModelRegistrySource>((serviceProvider, client) =>
        {
            LiteLLMModelRegistryOptions options = serviceProvider.GetRequiredService<IOptions<LiteLLMModelRegistryOptions>>().Value;
            client.BaseAddress = options.Endpoint;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        });
        builder.Services.AddSingleton<IModelRegistrySource>(serviceProvider =>
            serviceProvider.GetRequiredService<LiteLLMModelRegistrySource>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IModelRuntimeChatClientProvider, LiteLLMModelRuntimeChatClientProvider>());
        builder.Services.TryAddScoped<IAgentBuildOptionsResolver, AgentBuildOptionsResolver>();
        builder.Services.TryAddScoped<IAgentFactory, ModelRoutingAgentFactory>();
        builder.Services.TryAddScoped<IAgentBuildService, AgentBuildService>();

        return builder;
    }
}
