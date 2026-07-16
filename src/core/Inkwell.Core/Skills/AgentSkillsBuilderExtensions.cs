// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>注册 <see cref="IAgentSkillCatalogService"/> / <see cref="IAgentSkillContentResolver"/> 默认实现。</summary>
public static class AgentSkillsBuilderExtensions
{
    /// <summary>
    /// 注册默认 Agent Skill 服务。
    /// </summary>
    /// <param name="builder">Inkwell 构建器。</param>
    /// <param name="configure">Agent Skill 配置委托。</param>
    /// <returns>当前 Inkwell 构建器。</returns>
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
