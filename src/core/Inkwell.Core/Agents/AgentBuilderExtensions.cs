using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Inkwell;

/// <summary>注册 <see cref="IAgentService"/> / <see cref="IAgentInvocationService"/> 默认实现。</summary>
public static class AgentBuilderExtensions
{
    public static IInkwellBuilder UseDefaultAgentService(this IInkwellBuilder builder, Action<AgentOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddScoped<IAgentService, AgentService>();
        builder.Services.AddScoped<IAgentInvocationService, AgentInvocationService>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<AgentOptions>, AgentOptionsValidator>());

        OptionsBuilder<AgentOptions> optionsBuilder = builder.Services.AddOptions<AgentOptions>().Bind(builder.Configuration.GetSection("Inkwell:Agents"));

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        return builder;
    }
}
