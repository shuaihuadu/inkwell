// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 提供 Agent 管理与版本服务注册入口。
/// </summary>
public static class AgentServicesBuilderExtensions
{
    /// <summary>
    /// 注册默认 Agent 管理和版本服务。
    /// </summary>
    /// <param name="builder">Inkwell Builder DSL 入口。</param>
    /// <returns>供链式调用的 <paramref name="builder"/>。</returns>
    public static IInkwellBuilder UseDefaultAgentServices(this IInkwellBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddScoped<IAgentService, AgentService>();
        builder.Services.TryAddScoped<IAgentVersionService, AgentVersionService>();

        return builder;
    }
}