// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>定义聊天消息的查询和 CRUD 操作。</summary>
public interface IAgentChatMessageRepository
{
    /// <summary>分页列出会话消息。</summary>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="pagination">分页参数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>会话消息分页结果。</returns>
    Task<PagedResult<AgentChatMessage>> ListMessagesByConversation(Guid conversationId, Pagination pagination, CancellationToken ct = default);

    /// <summary>列出供聊天历史 Provider 使用的规范消息。</summary>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="maxMessages">最多返回的消息数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>规范聊天消息列表。</returns>
    Task<IReadOnlyList<ChatMessage>> ListHistoryMessagesAsync(Guid conversationId, int? maxMessages = null, CancellationToken ct = default);

    /// <summary>按会话内序号升序列出全部持久化消息。</summary>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>全部持久化消息。</returns>
    Task<IReadOnlyList<AgentChatMessage>> ListAllMessagesByConversation(Guid conversationId, CancellationToken ct = default);

    /// <summary>列出指定 Run 已持久化的消息。</summary>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="runId">服务端执行标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>指定 Run 的持久化消息。</returns>
    Task<IReadOnlyList<AgentChatMessage>> ListMessagesByRun(Guid conversationId, string runId, CancellationToken ct = default);

    /// <summary>批量新增消息并连续分配会话内序号。</summary>
    /// <param name="messages">待新增的消息列表。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>已新增并分配序号的消息。</returns>
    Task<IReadOnlyList<AgentChatMessage>> AddMessages(IReadOnlyList<AgentChatMessage> messages, CancellationToken ct = default);

    /// <summary>删除指定会话内的单条消息。</summary>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="messageId">消息标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>删除成功时为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
    Task<bool> DeleteMessage(Guid conversationId, Guid messageId, CancellationToken ct = default);

    /// <summary>删除指定会话的全部消息。</summary>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>删除的消息数量。</returns>
    Task<int> DeleteMessagesByConversation(Guid conversationId, CancellationToken ct = default);
}