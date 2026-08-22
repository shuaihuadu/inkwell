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
    IPersistenceProvider persistence,
    TimeProvider timeProvider,
    int? maxMessagesToRetrieve = null) : ChatHistoryProvider
{
    internal const string SessionIdStateKey = "Inkwell.SessionId";
    private const string OwnerUserIdStateKey = "Inkwell.OwnerUserId";
    private const string AgentIdStateKey = "Inkwell.AgentId";
    private const string ExecutionIdStateKey = "Inkwell.ExecutionId";

    private static readonly IReadOnlyList<string> stateKeys =
        [SessionIdStateKey, OwnerUserIdStateKey, AgentIdStateKey, ExecutionIdStateKey];

    private readonly IAgentChatMessageRepository _messages = persistence.GetRepository<IAgentChatMessageRepository>();
    private readonly IAgentConversationRepository _conversations = persistence.GetRepository<IAgentConversationRepository>();

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

        return history.Select(RemoveSkillToolApprovals).Where(message => message is not null)!;
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

        await persistence.ExecuteInTransactionAsync(
            IsolationLevel.Serializable,
            innerCancellationToken => this.AppendMessagesAsync(
                ownerUserId,
                agentId,
                conversationId,
                executionId,
                newMessages,
                innerCancellationToken),
            cancellationToken).ConfigureAwait(false);

        _ = session.StateBag.TryRemoveValue(OwnerUserIdStateKey);
        _ = session.StateBag.TryRemoveValue(AgentIdStateKey);
        _ = session.StateBag.TryRemoveValue(ExecutionIdStateKey);
    }

    /// <summary>
    /// 在可串行化事务中追加本轮消息，并回填会话派生字段。
    /// </summary>
    /// <param name="ownerUserId">会话所属参与用户标识。</param>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="executionId">服务端执行标识。</param>
    /// <param name="messages">本轮新增的请求与回复消息。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    private async Task AppendMessagesAsync(
        Guid ownerUserId,
        Guid agentId,
        Guid conversationId,
        string executionId,
        List<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        AgentConversation conversation = await this._conversations
            .GetConversation(conversationId, cancellationToken)
            .ConfigureAwait(false);
        if (conversation.OwnerUserId != ownerUserId || conversation.AgentId != agentId)
        {
            throw new UnauthorizedAccessException(
                $"User '{ownerUserId}' cannot access conversation '{conversationId}' for agent '{agentId}'.");
        }

        IReadOnlyList<AgentChatMessage> committedMessages = await this._messages
            .ListMessagesByRun(conversationId, executionId, cancellationToken)
            .ConfigureAwait(false);
        if (committedMessages.Count > 0)
        {
            return;
        }

        List<AgentChatMessage> batch = new(messages.Count);
        for (int index = 0; index < messages.Count; index++)
        {
            batch.Add(new AgentChatMessage
            {
                Id = Guid.CreateVersion7(),
                ConversationId = conversationId,
                RunId = executionId,
                RunMessageIndex = index,
                Message = messages[index],
                SequenceNumber = 0,
                CreatedTime = now,
                UpdatedTime = now,
            });
        }

        _ = await this._messages.AddMessages(batch, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<AgentChatMessage> allMessages = await this._messages
            .ListAllMessagesByConversation(conversationId, cancellationToken)
            .ConfigureAwait(false);
        AgentConversation updated = conversation with
        {
            Title = FindTitle(allMessages),
            LastActivityTime = now,
            UpdatedTime = now,
        };
        await this._conversations.UpdateConversation(updated, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 取首条非空 User 文本消息的前 30 个字符作为会话标题。
    /// </summary>
    /// <param name="messages">会话内的全部消息。</param>
    /// <returns>会话标题；没有可用 User 文本时返回 <see langword="null"/>。</returns>
    private static string? FindTitle(IReadOnlyList<AgentChatMessage> messages)
    {
        ChatMessage? firstUserMessage = messages.Select(message => message.Message)
            .FirstOrDefault(message => message.Role == ChatRole.User && !string.IsNullOrEmpty(message.Text));
        return firstUserMessage?.Text is { } text ? text[..Math.Min(30, text.Length)] : null;
    }

    private static ChatMessage? RemoveSkillToolApprovals(ChatMessage message)
    {
        List<AIContent> supportedContents = [.. message.Contents
            .Where(static content => !IsSkillToolApproval(content))];

        if (supportedContents.Count == message.Contents.Count)
        {
            return message;
        }

        if (supportedContents.Count == 0)
        {
            return null;
        }

        ChatMessage filtered = message.Clone();
        filtered.Contents = supportedContents;
        return filtered;
    }

    private static bool IsSkillToolApproval(AIContent content) => content is ToolApprovalRequestContent
    {
        ToolCall: FunctionCallContent
        {
            Name: AgentSkillsProvider.LoadSkillToolName
                or AgentSkillsProvider.ReadSkillResourceToolName
                or AgentSkillsProvider.RunSkillScriptToolName,
        },
    };

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
