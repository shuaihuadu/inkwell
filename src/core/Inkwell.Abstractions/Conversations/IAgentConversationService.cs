
namespace Inkwell;

/// <summary>
/// 会话生命周期管理 + 消息持久化 / 检索 + "我使用过" / "最近使用时间"查询业务对外接口。
/// </summary>
public interface IAgentConversationService
{
    Task<Guid> StartConversationAsync(Guid agentId, Guid ownerUserId, CancellationToken ct = default);

    Task<IReadOnlyList<AgentConversationSummary>> ListConversationsAsync(Guid agentId, Guid ownerUserId, CancellationToken ct = default);

    /// <summary>供 Agent 执行运行时（<c>Inkwell.Core.AgentRuntime</c> 内的 ChatHistoryProvider 实现）自动拉取历史，
    /// 也可供 <c>Inkwell.WebApi</c> 用于渲染对话历史列表等展示场景。</summary>
    Task<IReadOnlyList<AgentChatMessage>> GetHistoryMessagesAsync(Guid conversationId, CancellationToken ct = default);

    Task<Guid> AppendMessageAsync(Guid conversationId, AgentChatMessage message, CancellationToken ct = default);

    Task DeleteMessageAsync(Guid conversationId, Guid messageId, Guid actorUserId, CancellationToken ct = default);

    Task ClearConversationAsync(Guid conversationId, Guid actorUserId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> ListUsedAgentIdsAsync(Guid ownerUserId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, DateTimeOffset>> GetLastActivityByAgentsAsync(IReadOnlyList<Guid> agentIds, Guid viewerUserId, CancellationToken ct = default);
}
