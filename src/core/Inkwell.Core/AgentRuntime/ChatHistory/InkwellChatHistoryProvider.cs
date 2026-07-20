// Copyright (c) ShuaiHua Du. All rights reserved.

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
    IPersistenceProvider persistence,
    AgentConversationMessageCommitter messageCommitter,
    int? maxMessagesToRetrieve = null) : ChatHistoryProvider
{
    internal const string SessionIdStateKey = "Inkwell.SessionId";
    private const string OwnerUserIdStateKey = "Inkwell.OwnerUserId";
    private const string AgentIdStateKey = "Inkwell.AgentId";
    private const string ExecutionIdStateKey = "Inkwell.ExecutionId";

    private static readonly IReadOnlyList<string> stateKeys =
        [SessionIdStateKey, OwnerUserIdStateKey, AgentIdStateKey, ExecutionIdStateKey];

    private readonly IAgentChatMessageRepository _messages = persistence.GetRepository<IAgentChatMessageRepository>();

    /// <inheritdoc />
    public override IReadOnlyList<string> StateKeys => stateKeys;

    /// <summary>
    /// 将 Inkwell 业务 Session 标识附加到 MAF Session。
    /// </summary>
    /// <param name="session">MAF Session。</param>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="ownerUserId">会话所属参与用户标识。</param>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="executionId">服务端执行标识。</param>
    internal static void AttachSession(
        AgentSession session,
        Guid conversationId,
        Guid ownerUserId,
        Guid agentId,
        string executionId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(executionId);
        session.StateBag.SetValue(SessionIdStateKey, conversationId.ToString("D"));
        session.StateBag.SetValue(OwnerUserIdStateKey, ownerUserId.ToString("D"));
        session.StateBag.SetValue(AgentIdStateKey, agentId.ToString("D"));
        session.StateBag.SetValue(ExecutionIdStateKey, executionId);
    }

    /// <inheritdoc />
    protected override async ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        Guid conversationId = GetStateGuid(context.Session, SessionIdStateKey);
        IReadOnlyList<ChatMessage> history = await this._messages.ListHistoryMessagesAsync(conversationId, maxMessagesToRetrieve, cancellationToken).ConfigureAwait(false);

        return history;
    }

    /// <inheritdoc />
    protected override async ValueTask StoreChatHistoryAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        AgentSession session = context.Session
            ?? throw new InvalidOperationException("The MAF AgentSession is required for Inkwell chat history.");
        Guid conversationId = GetStateGuid(session, SessionIdStateKey);
        Guid ownerUserId = GetStateGuid(session, OwnerUserIdStateKey);
        Guid agentId = GetStateGuid(session, AgentIdStateKey);
        string executionId = GetStateString(session, ExecutionIdStateKey);
        List<ChatMessage> newMessages = [.. context.RequestMessages.Concat(context.ResponseMessages ?? [])];

        if (newMessages.Count == 0)
        {
            return;
        }

        AgentChatMessageCommitResult result = await messageCommitter
            .CommitAsync(ownerUserId, agentId, conversationId, executionId, newMessages, cancellationToken)
            .ConfigureAwait(false);
        if (result == AgentChatMessageCommitResult.Conflict)
        {
            throw new InvalidOperationException($"Conversation run message conflict: executionId={executionId}");
        }

        _ = session.StateBag.TryRemoveValue(OwnerUserIdStateKey);
        _ = session.StateBag.TryRemoveValue(AgentIdStateKey);
        _ = session.StateBag.TryRemoveValue(ExecutionIdStateKey);
    }

    private static Guid GetStateGuid(AgentSession? session, string key)
    {
        string value = GetStateString(session, key);
        if (!Guid.TryParse(value, out Guid result))
        {
            throw new InvalidOperationException($"The MAF AgentSession contains an invalid '{key}' value.");
        }

        return result;
    }

    private static string GetStateString(AgentSession? session, string key)
    {
        if (session?.StateBag.GetValue<string>(key) is not string value || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"The MAF AgentSession does not contain '{key}'.");
        }

        return value;
    }
}
