// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Inkwell;

/// <summary>注册 <see cref="IAgentSkillCatalogService"/> / <see cref="IAgentSkillContentResolver"/> 默认实现。</summary>
public static class AgentSkillsBuilderExtensions
{
    public static IInkwellBuilder UseDefaultSkillService(this IInkwellBuilder builder, Action<AgentSkillOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddScoped<IAgentSkillCatalogService, AgentSkillCatalogService>();
        builder.Services.AddScoped<IAgentSkillContentResolver, AgentSkillContentResolver>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<AgentSkillOptions>, AgentSkillOptionsValidator>());

        OptionsBuilder<AgentSkillOptions> optionsBuilder = builder.Services.AddOptions<AgentSkillOptions>().Bind(builder.Configuration.GetSection("Inkwell:Skills"));

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        return builder;
    }
}
