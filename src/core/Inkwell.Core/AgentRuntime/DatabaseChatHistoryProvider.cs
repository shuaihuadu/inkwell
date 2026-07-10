using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Inkwell;

/// <summary>
/// <see cref="ChatHistoryProvider"/> 的 Inkwell 实现：把 MAF Agent 执行管线的"取历史 / 存新消息"两个钩子
/// 委托给 <see cref="IAgentConversationService"/>，会话生命周期管理、分页、删除、清空、标题提取等业务功能
/// 仍完全由 <see cref="IAgentConversationService"/> 承担，这里只是一层薄适配。
/// </summary>
/// <remarks>
/// 单个实例会被所有会话共用（附着在 <see cref="ChatClientAgentOptions.ChatHistoryProvider"/> 上，随 Agent 实例
/// 一起缓存），因此不能把某一次调用的会话状态存成实例字段；本类通过 <see cref="AgentSession.StateBag"/> 中的
/// <see cref="ConversationIdStateKey"/> 读取当次调用对应的 <c>ConversationId</c>。<br/>
/// <see cref="IAgentConversationService"/> 以 Scoped 生命周期注册，而承载本类的 <see cref="AzureOpenAIAgentRuntime"/>
/// 是 Singleton，因此这里注入 <see cref="IServiceScopeFactory"/>，每次调用时创建一个独立的 DI Scope 解析
/// <see cref="IAgentConversationService"/>，避免 Singleton 直接持有 Scoped 依赖（captive dependency）。
/// </remarks>
internal sealed class DatabaseChatHistoryProvider(IServiceScopeFactory scopeFactory) : ChatHistoryProvider
{
    /// <summary><see cref="AgentSession.StateBag"/> 中存放 <c>ConversationId</c> 的键名。</summary>
    internal const string ConversationIdStateKey = "Inkwell.ConversationId";

    /// <inheritdoc />
    protected override async ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        if (!TryGetConversationId(context.Session, out Guid conversationId))
        {
            return [];
        }

        using IServiceScope scope = scopeFactory.CreateScope();
        IAgentConversationService conversationService = scope.ServiceProvider.GetRequiredService<IAgentConversationService>();

        IReadOnlyList<AgentChatMessage> history = await conversationService.GetHistoryMessagesAsync(conversationId, cancellationToken).ConfigureAwait(false);

        return history.Select(AgentChatMessageMapper.ToChatMessage);
    }

    /// <inheritdoc />
    protected override async ValueTask StoreChatHistoryAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        if (!TryGetConversationId(context.Session, out Guid conversationId))
        {
            return;
        }

        using IServiceScope scope = scopeFactory.CreateScope();
        IAgentConversationService conversationService = scope.ServiceProvider.GetRequiredService<IAgentConversationService>();

        foreach (ChatMessage message in context.RequestMessages.Concat(context.ResponseMessages ?? []))
        {
            _ = await conversationService.AppendMessageAsync(conversationId, AgentChatMessageMapper.ToAgentChatMessage(message), cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool TryGetConversationId(AgentSession? session, out Guid conversationId)
    {
        if (session is null)
        {
            conversationId = Guid.Empty;

            return false;
        }

        string? rawConversationId = session.StateBag.GetValue<string>(ConversationIdStateKey);

        return Guid.TryParse(rawConversationId, out conversationId);
    }
}
