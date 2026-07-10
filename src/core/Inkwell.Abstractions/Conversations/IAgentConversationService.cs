
namespace Inkwell;

/// <summary>
/// 会话生命周期管理 + 消息持久化 / 检索 + "我使用过" / "最近使用时间"查询业务对外接口。
/// </summary>
public interface IAgentConversationService
{
    Task<Guid> StartConversationAsync(Guid agentId, Guid ownerUserId, CancellationToken ct = default);

    Task<IReadOnlyList<AgentConversationSummary>> ListConversationsAsync(Guid agentId, Guid ownerUserId, CancellationToken ct = default);

    /// <summary>供调用方（Inkwell.WebApi）直接拼进 AgentRunRequest.Messages。</summary>
    Task<IReadOnlyList<AgentChatMessage>> GetHistoryMessagesAsync(Guid conversationId, CancellationToken ct = default);

    Task<Guid> AppendMessageAsync(Guid conversationId, AgentChatMessage message, CancellationToken ct = default);

    Task DeleteMessageAsync(Guid conversationId, Guid messageId, Guid actorUserId, CancellationToken ct = default);

    Task ClearConversationAsync(Guid conversationId, Guid actorUserId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> ListUsedAgentIdsAsync(Guid ownerUserId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, DateTimeOffset>> GetLastActivityByAgentsAsync(IReadOnlyList<Guid> agentIds, Guid viewerUserId, CancellationToken ct = default);
}
