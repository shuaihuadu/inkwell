// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>提供 Agent Conversation 业务服务注册入口。</summary>
public static class ConversationBuilderExtensions
{
    /// <summary>注册默认 Agent Conversation 业务服务。</summary>
    /// <param name="builder">Inkwell Builder DSL 入口。</param>
    /// <returns>供链式调用的 <paramref name="builder"/>。</returns>
    public static IInkwellBuilder UseDefaultConversationService(this IInkwellBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.TryAddScoped<IAgentConversationService, AgentConversationService>();

        return builder;
    }
}