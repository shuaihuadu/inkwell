// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>注册 <see cref="IAgentToolCatalogService"/> 默认实现 + 内置工具执行委托字典。</summary>
public static class AgentToolsBuilderExtensions
{
    /// <summary>
    /// 注册默认 Agent Tool 服务及内置工具执行委托。
    /// </summary>
    /// <param name="builder">Inkwell 构建器。</param>
    /// <param name="configure">Agent Tool 配置委托。</param>
    /// <returns>当前 Inkwell 构建器。</returns>
    public static IInkwellBuilder UseDefaultToolService(this IInkwellBuilder builder, Action<AgentToolOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddScoped<IAgentToolCatalogService, AgentToolCatalogService>();
        builder.Services.AddSingleton<IReadOnlyDictionary<Guid, Func<string, CancellationToken, Task<string>>>>(sp =>
        {
            AgentCurrentDateTimeToolExecutor currentDateTime = new AgentCurrentDateTimeToolExecutor(sp.GetRequiredService<TimeProvider>());

            return new Dictionary<Guid, Func<string, CancellationToken, Task<string>>>
            {
                [AgentCurrentDateTimeToolExecutor.ToolId] = currentDateTime.InvokeAsync,
            };
        });

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<AgentToolOptions>, AgentToolOptionsValidator>());

        OptionsBuilder<AgentToolOptions> optionsBuilder = builder.Services.AddOptions<AgentToolOptions>().Bind(builder.Configuration.GetSection("Inkwell:Tools"));

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        return builder;
    }
}
