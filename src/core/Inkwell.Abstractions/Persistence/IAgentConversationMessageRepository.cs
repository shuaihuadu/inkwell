// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary><see cref="AgentChatMessage"/> 具名 Repository。</summary>
public interface IAgentConversationMessageRepository
{
    Task<AgentChatMessage> AddMessage(AgentChatMessage message, CancellationToken ct = default);

    Task<PagedResult<AgentChatMessage>> ListMessagesByConversation(Guid conversationId, Pagination pagination, SortOrder sort, CancellationToken ct = default);

    /// <summary>幂等：<c>true</c> = 找到并删除，<c>false</c> = 未找到。</summary>
    Task<bool> DeleteMessage(Guid conversationId, Guid messageId, CancellationToken ct = default);

    /// <summary>返回实际删除的消息数；<c>0</c> 表示会话本就没有消息，非错误。</summary>
    Task<int> DeleteMessagesByConversation(Guid conversationId, CancellationToken ct = default);
}
