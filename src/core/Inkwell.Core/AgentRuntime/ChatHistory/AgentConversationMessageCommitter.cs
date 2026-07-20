// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Data;
using Microsoft.Extensions.AI;

namespace Inkwell;

internal sealed class AgentConversationMessageCommitter(
    IPersistenceProvider persistence,
    TimeProvider timeProvider)
{
    private readonly IAgentConversationRepository _conversations = persistence.GetRepository<IAgentConversationRepository>();
    private readonly IAgentChatMessageRepository _messages = persistence.GetRepository<IAgentChatMessageRepository>();

    internal Task<AgentChatMessageCommitResult> CommitAsync(
        Guid ownerUserId,
        Guid agentId,
        Guid conversationId,
        string executionId,
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionId);
        ArgumentNullException.ThrowIfNull(messages);

        return persistence.ExecuteInTransactionAsync(
            IsolationLevel.Serializable,
            async innerCancellationToken =>
            {
                DateTimeOffset now = timeProvider.GetUtcNow();
                AgentConversation conversation = await this._conversations
                    .GetConversation(conversationId, innerCancellationToken)
                    .ConfigureAwait(false);
                if (conversation.OwnerUserId != ownerUserId || conversation.AgentId != agentId)
                {
                    throw new UnauthorizedAccessException(
                        $"User '{ownerUserId}' cannot access conversation '{conversationId}' for agent '{agentId}'.");
                }

                IReadOnlyList<AgentChatMessage> existing = await this._messages
                    .ListMessagesByRun(conversationId, executionId, innerCancellationToken)
                    .ConfigureAwait(false);
                if (existing.Count > 0)
                {
                    return MessageBatchesEqual(existing, messages)
                        ? AgentChatMessageCommitResult.AlreadyCommitted
                        : AgentChatMessageCommitResult.Conflict;
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

                _ = await this._messages.AddMessages(batch, innerCancellationToken).ConfigureAwait(false);
                IReadOnlyList<AgentChatMessage> allMessages = await this._messages
                    .ListAllMessagesByConversation(conversationId, innerCancellationToken)
                    .ConfigureAwait(false);
                AgentConversation updated = conversation with
                {
                    Title = FindTitle(allMessages),
                    LastCommittedRunId = executionId,
                    LastActivityTime = now,
                    UpdatedTime = now,
                };
                await this._conversations.UpdateConversation(updated, innerCancellationToken).ConfigureAwait(false);
                return AgentChatMessageCommitResult.Committed;
            },
            cancellationToken);
    }

    private static string? FindTitle(IReadOnlyList<AgentChatMessage> messages)
    {
        ChatMessage? firstUserMessage = messages.Select(message => message.Message)
            .FirstOrDefault(message => message.Role == ChatRole.User && !string.IsNullOrEmpty(message.Text));
        return firstUserMessage?.Text is { } text ? text[..Math.Min(30, text.Length)] : null;
    }

    private static bool MessageBatchesEqual(
        IReadOnlyList<AgentChatMessage> existing,
        IReadOnlyList<ChatMessage> expected) =>
        existing.Count == expected.Count
        && existing.Select((message, index) => JsonElement.DeepEquals(
            JsonSerializer.SerializeToElement(message.Message),
            JsonSerializer.SerializeToElement(expected[index]))).All(equal => equal);
}
