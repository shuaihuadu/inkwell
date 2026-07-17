// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Net.Http.Headers;

namespace Inkwell;

/// <summary>
/// 注册 LiteLLM Provider。
/// </summary>
public static class LiteLLMBuilderExtensions
{
    private const string HttpClientName = "Inkwell.LiteLLM";

    /// <summary>
    /// 使用 LiteLLM 作为当前部署的 LLM Provider。
    /// </summary>
    /// <param name="builder">Inkwell 构建器。</param>
    /// <param name="configure">LiteLLM 连接配置委托。</param>
    /// <returns>当前 Inkwell 构建器。</returns>
    public static IInkwellBuilder UseLiteLLM(
        this IInkwellBuilder builder,
        Action<LiteLLMOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        OptionsBuilder<LiteLLMOptions> optionsBuilder = builder.Services
            .AddOptions<LiteLLMOptions>()
            .Bind(builder.Configuration.GetSection("Inkwell:LiteLLM"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        builder.Services
            .AddHttpClient(HttpClientName, (serviceProvider, client) =>
            {
                LiteLLMOptions value = serviceProvider.GetRequiredService<IOptions<LiteLLMOptions>>().Value;
                client.BaseAddress = value.Endpoint;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", value.ApiKey);
            })
            .UseSocketsHttpHandler((handler, _) =>
                handler.PooledConnectionLifetime = TimeSpan.FromMinutes(2))
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
        builder.Services.AddSingleton(serviceProvider =>
            new LiteLLMProvider(
                serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName),
                serviceProvider.GetRequiredService<IOptions<LiteLLMOptions>>()));
        builder.Services.AddSingleton<ILLMProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<LiteLLMProvider>());
        builder.Services.AddSingleton<IChatLLMProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<LiteLLMProvider>());

        return builder;
    }
}