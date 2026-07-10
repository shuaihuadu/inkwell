using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Inkwell;

/// <summary>注册 <see cref="IAgentRuntime"/> 的 Azure OpenAI 默认实现。</summary>
public static class AzureOpenAIAgentRuntimeBuilderExtensions
{
    public static IInkwellBuilder UseAzureOpenAIAgentRuntime(this IInkwellBuilder builder, Action<AzureOpenAIAgentRuntimeOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<IAgentRuntime, AzureOpenAIAgentRuntime>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<AgentRuntimeOptions>, AgentRuntimeOptionsValidator>());
        builder.Services.AddOptions<AgentRuntimeOptions>().Bind(builder.Configuration.GetSection("Inkwell:AgentRuntime"));

        OptionsBuilder<AzureOpenAIAgentRuntimeOptions> optionsBuilder = builder.Services.AddOptions<AzureOpenAIAgentRuntimeOptions>().Bind(builder.Configuration.GetSection("Inkwell:AgentRuntime:AzureOpenAI"));

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        return builder;
    }
}
