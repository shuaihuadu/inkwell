// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Data;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Inkwell;

/// <summary>
/// 使用 Inkwell 持久化端口加载和保存 MAF 聊天历史。
/// </summary>
/// <remarks>
/// Provider 实例不保存任何会话特定状态；业务 Session 标识存储在 <see cref="AgentSession.StateBag"/> 中。
/// </remarks>
internal sealed class InkwellChatHistoryProvider(
    IAgentSessionMessageRepository messages,
    IPersistenceProvider persistence,
    int? maxMessagesToRetrieve = null) : ChatHistoryProvider
{
    internal const string SessionIdStateKey = "Inkwell.SessionId";

    private static readonly IReadOnlyList<string> stateKeys = [SessionIdStateKey];

    /// <inheritdoc />
    public override IReadOnlyList<string> StateKeys => stateKeys;

    /// <summary>
    /// 将 Inkwell 业务 Session 标识附加到 MAF Session。
    /// </summary>
    /// <param name="session">MAF Session。</param>
    /// <param name="sessionId">Inkwell 业务 Session 标识。</param>
    internal static void AttachSession(AgentSession session, Guid sessionId)
    {
        ArgumentNullException.ThrowIfNull(session);
        session.StateBag.SetValue(SessionIdStateKey, sessionId.ToString());
    }

    /// <inheritdoc />
    protected override async ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        Guid sessionId = GetSessionId(context.Session);
        IReadOnlyList<ChatMessage> history = await messages.ListHistoryMessagesAsync(sessionId, maxMessagesToRetrieve, cancellationToken).ConfigureAwait(false);

        return history;
    }

    /// <inheritdoc />
    protected override async ValueTask StoreChatHistoryAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        Guid sessionId = GetSessionId(context.Session);
        List<ChatMessage> newMessages = [.. context.RequestMessages.Concat(context.ResponseMessages ?? [])];

        if (newMessages.Count == 0)
        {
            return;
        }

        await persistence.ExecuteInTransactionAsync(
            IsolationLevel.Serializable,
            async innerCancellationToken =>
            {
                await messages.AppendMessagesAsync(sessionId, newMessages, innerCancellationToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static Guid GetSessionId(AgentSession? session)
    {
        if (session?.StateBag.GetValue<string>(SessionIdStateKey) is not string sessionIdValue
            || !Guid.TryParse(sessionIdValue, out Guid sessionId))
        {
            throw new InvalidOperationException("The MAF AgentSession is not attached to an Inkwell session.");
        }

        return sessionId;
    }
}
