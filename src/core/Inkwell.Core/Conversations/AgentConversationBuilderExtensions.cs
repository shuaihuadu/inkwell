using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Inkwell;

/// <summary>注册 <see cref="IAgentConversationService"/> 默认实现。</summary>
public static class AgentConversationBuilderExtensions
{
    public static IInkwellBuilder UseDefaultConversationService(this IInkwellBuilder builder, Action<AgentConversationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddScoped<IAgentConversationService, AgentConversationService>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<AgentConversationOptions>, AgentConversationOptionsValidator>());

        OptionsBuilder<AgentConversationOptions> optionsBuilder = builder.Services.AddOptions<AgentConversationOptions>().Bind(builder.Configuration.GetSection("Inkwell:Conversations"));

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        return builder;
    }
}
