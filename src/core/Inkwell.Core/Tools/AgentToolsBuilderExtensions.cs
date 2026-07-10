// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Inkwell;

/// <summary>注册 <see cref="IAgentToolCatalogService"/> / <see cref="IAgentToolBindingResolver"/> 默认实现 + 内置工具执行委托字典。</summary>
public static class AgentToolsBuilderExtensions
{
    public static IInkwellBuilder UseDefaultToolService(this IInkwellBuilder builder, Action<AgentToolOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddScoped<IAgentToolCatalogService, AgentToolCatalogService>();
        builder.Services.AddScoped<IAgentToolBindingResolver, AgentToolBindingResolver>();
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
