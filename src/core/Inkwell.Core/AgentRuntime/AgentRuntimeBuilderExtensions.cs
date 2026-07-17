// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 注册默认 Agent 运行时服务。
/// </summary>
public static class AgentRuntimeBuilderExtensions
{
    /// <summary>
    /// 注册使用当前 LLM Provider 的默认 Agent 运行时。
    /// </summary>
    /// <param name="builder">Inkwell 构建器。</param>
    /// <returns>当前 Inkwell 构建器。</returns>
    public static IInkwellBuilder UseDefaultAgentRuntime(this IInkwellBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddScoped<IAgentBuildOptionsResolver, AgentBuildOptionsResolver>();
        builder.Services.TryAddScoped<IAgentFactory, ModelRoutingAgentFactory>();
        builder.Services.TryAddScoped<IAgentBuildService, AgentBuildService>();

        return builder;
    }
}