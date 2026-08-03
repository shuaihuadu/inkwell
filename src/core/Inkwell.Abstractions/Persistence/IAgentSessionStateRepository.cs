// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>定义 Agent Session 检查点的查询和持久化操作。</summary>
public interface IAgentSessionStateRepository
{
    /// <summary>新增会话状态。</summary>
    /// <param name="sessionState">待新增会话状态。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>已新增会话状态。</returns>
    Task<AgentSessionState> AddSessionState(AgentSessionState sessionState, CancellationToken ct = default);

    /// <summary>按产品会话标识查找会话状态。</summary>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>会话状态；不存在时为 <see langword="null"/>。</returns>
    Task<AgentSessionState?> FindSessionStateByConversation(Guid conversationId, CancellationToken ct = default);

    /// <summary>按产品会话标识覆盖写入会话状态。</summary>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="sessionState">序列化后的 Session 状态 JSON 内容。</param>
    /// <param name="updatedTime">更新时间。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>命中并写入时为 <see langword="true"/>；会话状态不存在时为 <see langword="false"/>。</returns>
    Task<bool> UpdateSessionState(Guid conversationId, string sessionState, DateTimeOffset updatedTime, CancellationToken ct = default);

    /// <summary>按产品会话标识删除会话状态。</summary>
    /// <param name="conversationId">产品会话标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>命中并删除时为 <see langword="true"/>；会话状态不存在时为 <see langword="false"/>。</returns>
    Task<bool> DeleteSessionState(Guid conversationId, CancellationToken ct = default);
}
