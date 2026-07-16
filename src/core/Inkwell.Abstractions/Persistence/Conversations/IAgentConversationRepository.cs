// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>定义产品会话查询和持久化操作。</summary>
public interface IAgentConversationRepository
{
    /// <summary>新增产品会话。</summary>
    /// <param name="conversation">待新增会话。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>已新增会话。</returns>
    Task<AgentConversation> AddConversation(AgentConversation conversation, CancellationToken ct = default);

    /// <summary>获取指定产品会话。</summary>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>产品会话。</returns>
    Task<AgentConversation> GetConversation(Guid conversationId, CancellationToken ct = default);

    /// <summary>按不透明 Session Key 获取产品会话。</summary>
    /// <param name="sessionKey">不透明 Session Key。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>产品会话。</returns>
    Task<AgentConversation> GetConversationBySessionKey(string sessionKey, CancellationToken ct = default);

    /// <summary>分页列出指定参与用户与 Agent 的产品会话。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="ownerUserId">会话所属参与用户标识。</param>
    /// <param name="pagination">分页参数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>会话列表项分页结果。</returns>
    Task<PagedResult<AgentConversationListItem>> ListConversations(Guid agentId, Guid ownerUserId, Pagination pagination, CancellationToken ct = default);

    /// <summary>更新产品会话。</summary>
    /// <param name="conversation">待更新的产品会话。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task UpdateConversation(AgentConversation conversation, CancellationToken ct = default);

    /// <summary>删除指定会话。</summary>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>操作成功时为 <see langword="true"/>。</returns>
    Task<bool> DeleteConversation(Guid conversationId, CancellationToken ct = default);
}