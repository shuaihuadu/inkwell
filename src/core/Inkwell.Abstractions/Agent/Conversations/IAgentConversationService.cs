// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Agents.AI;

namespace Inkwell;

/// <summary>定义产品会话 CRUD 和 Owner 授权业务操作。</summary>
public interface IAgentConversationService
{
    /// <summary>创建并锁定当前可调用 Agent 版本的产品会话。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="ownerUserId">会话所属参与用户标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>创建的产品会话。</returns>
    Task<AgentConversation> CreateConversationAsync(Guid agentId, Guid ownerUserId, CancellationToken ct = default);

    /// <summary>列出当前参与用户在指定 Agent 下的产品会话。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="ownerUserId">会话所属参与用户标识。</param>
    /// <param name="pagination">分页参数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>产品会话列表项分页结果。</returns>
    Task<PagedResult<AgentConversationListItem>> ListConversationsAsync(Guid agentId, Guid ownerUserId, Pagination pagination, CancellationToken ct = default);

    /// <summary>获取已授权产品会话的消息。</summary>
    /// <param name="ownerUserId">会话所属参与用户标识。</param>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="pagination">分页参数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>产品会话消息分页结果。</returns>
    Task<PagedResult<AgentChatMessage>> GetMessagesAsync(Guid ownerUserId, Guid agentId, Guid conversationId, Pagination pagination, CancellationToken ct = default);

    /// <summary>删除已授权产品会话中的单条消息。</summary>
    /// <param name="ownerUserId">会话所属参与用户标识。</param>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="messageId">消息标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task DeleteMessageAsync(Guid ownerUserId, Guid agentId, Guid conversationId, Guid messageId, CancellationToken ct = default);

    /// <summary>清空已授权产品会话。</summary>
    /// <param name="ownerUserId">会话所属参与用户标识。</param>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task ClearConversationAsync(Guid ownerUserId, Guid agentId, Guid conversationId, CancellationToken ct = default);

    /// <summary>删除已授权产品会话。</summary>
    /// <param name="ownerUserId">会话所属参与用户标识。</param>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task DeleteConversationAsync(Guid ownerUserId, Guid agentId, Guid conversationId, CancellationToken ct = default);

    /// <summary>幂等提交消息批次。</summary>
    /// <param name="ownerUserId">会话所属参与用户标识。</param>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="executionId">服务端执行标识。</param>
    /// <param name="messages">待提交的消息列表。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>消息批次提交结果。</returns>
    Task<AgentChatMessageCommitResult> CommitRunMessagesAsync(Guid ownerUserId, Guid agentId, Guid conversationId, string executionId, IReadOnlyList<ChatMessage> messages, CancellationToken ct = default);

    /// <summary>执行产品会话并提交本轮消息。</summary>
    /// <param name="ownerUserId">会话所属参与用户标识。</param>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="messages">本次协议请求携带的消息。</param>
    /// <param name="options">Agent 运行选项。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Agent 响应。</returns>
    Task<AgentResponse> RunAsync(
        Guid ownerUserId,
        Guid agentId,
        Guid conversationId,
        IReadOnlyList<ChatMessage> messages,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>流式执行产品会话，并在完整成功后提交本轮消息。</summary>
    /// <param name="ownerUserId">会话所属参与用户标识。</param>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="messages">本次协议请求携带的消息。</param>
    /// <param name="options">Agent 运行选项。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Agent 响应更新流。</returns>
    IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync(
        Guid ownerUserId,
        Guid agentId,
        Guid conversationId,
        IReadOnlyList<ChatMessage> messages,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default);
}
