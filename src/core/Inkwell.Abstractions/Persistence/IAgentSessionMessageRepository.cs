// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary><see cref="AgentChatMessage"/> 具名 Repository。</summary>
public interface IAgentSessionMessageRepository
{
    /// <summary>新增会话消息。</summary>
    /// <param name="message">待新增的会话消息。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>已新增的会话消息。</returns>
    Task<AgentChatMessage> AddMessage(AgentChatMessage message, CancellationToken ct = default);

    /// <summary>分页获取指定会话的消息。</summary>
    /// <param name="sessionId">会话标识。</param>
    /// <param name="pagination">分页参数。</param>
    /// <param name="sort">排序条件。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>会话消息分页结果。</returns>
    Task<PagedResult<AgentChatMessage>> ListMessagesBySession(Guid sessionId, Pagination pagination, SortOrder sort, CancellationToken ct = default);

    /// <summary>
    /// 按时间顺序读取供 Agent 调用使用的完整聊天历史。
    /// </summary>
    /// <param name="sessionId">业务会话标识。</param>
    /// <param name="maxMessages">最多读取的最近消息数量；为空表示读取全部。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>按 SequenceNumber 升序排列的完整 Chat Message 集合。</returns>
    Task<IReadOnlyList<ChatMessage>> ListHistoryMessagesAsync(Guid sessionId, int? maxMessages = null, CancellationToken ct = default);

    /// <summary>
    /// 在当前事务内批量追加消息，并从持久化历史的最大序号后连续分配 SequenceNumber。
    /// 调用方必须使用可串行化事务，保证同一会话并发追加时不产生重复序号。
    /// </summary>
    /// <param name="sessionId">业务会话标识。</param>
    /// <param name="messages">按生成顺序排列的完整 Chat Message 集合。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>已持久化的消息集合。</returns>
    Task<IReadOnlyList<AgentChatMessage>> AppendMessagesAsync(Guid sessionId, IReadOnlyList<ChatMessage> messages, CancellationToken ct = default);

    /// <summary>幂等：<c>true</c> = 找到并删除，<c>false</c> = 未找到。</summary>
    /// <param name="sessionId">会话标识。</param>
    /// <param name="messageId">消息标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>实际删除时为 <see langword="true"/>；消息未找到时为 <see langword="false"/>。</returns>
    Task<bool> DeleteMessage(Guid sessionId, Guid messageId, CancellationToken ct = default);

    /// <summary>返回实际删除的消息数；<c>0</c> 表示会话本就没有消息，非错误。</summary>
    /// <param name="sessionId">会话标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>实际删除的消息数。</returns>
    Task<int> DeleteMessagesBySession(Guid sessionId, CancellationToken ct = default);
}
