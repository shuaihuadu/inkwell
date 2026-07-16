// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>定义 Agent Session 检查点读取、fenced CAS 保存和删除操作。</summary>
public interface IAgentSessionStateRepository
{
    /// <summary>获取检查点；不存在时返回 <see langword="null"/>。</summary>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>Session 检查点；不存在时为 <see langword="null"/>。</returns>
    Task<AgentSessionState?> GetSessionStateOrDefault(Guid conversationId, CancellationToken ct = default);

    /// <summary>新增检查点。</summary>
    /// <param name="state">待新增的 Session 检查点。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task AddSessionState(AgentSessionState state, CancellationToken ct = default);

    /// <summary>更新检查点。</summary>
    /// <param name="state">待更新的 Session 检查点。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task UpdateSessionState(AgentSessionState state, CancellationToken ct = default);

    /// <summary>幂等删除指定会话的检查点。</summary>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>实际删除检查点时为 <see langword="true"/>；不存在时为 <see langword="false"/>。</returns>
    Task<bool> DeleteSessionStateByConversation(Guid conversationId, CancellationToken ct = default);
}