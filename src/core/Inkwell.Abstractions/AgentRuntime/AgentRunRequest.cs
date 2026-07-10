namespace Inkwell;

/// <summary>
/// 一次 Run 调用的已解析上下文；由调用方（<c>Inkwell.WebApi</c>）在调用前查好 Agent 配置 /
/// 工具注册表并组装。对话历史不再需要调用方预先拼装——<see cref="IAgentRuntime"/> 实现内部的
/// <c>ChatHistoryProvider</c> 会根据 <see cref="ConversationId"/> 自动从持久化拉取并拼接。端口本身不反向依赖 <c>IPersistenceProvider</c>。
/// </summary>
public sealed record class AgentRunRequest
{
    public required string RunId { get; init; }

    public required Guid AgentId { get; init; }

    /// <summary>
    /// 对话 ID。为 <see langword="null"/> 时代表一次无持久化历史的一次性调用，<see cref="IAgentRuntime"/>
    /// 实现既不会拉取历史也不会写入任何新消息。
    /// </summary>
    public Guid? ConversationId { get; init; }

    /// <summary>
    /// 本次调用新提交的消息（通常只有最新一条用户消息），<strong>不包含历史消息</strong>。
    /// 历史消息由 <see cref="IAgentRuntime"/> 实现内部的 <c>ChatHistoryProvider</c> 根据 <see cref="ConversationId"/>
    /// 自动拼接在前面，本次提交的新消息也由它在 Run 结束后自动持久化，调用方不再需要手动调用
    /// <c>IAgentConversationService.AppendMessageAsync</c>。
    /// </summary>
    public required IReadOnlyList<AgentChatMessage> Messages { get; init; }

    public string? Instructions { get; init; }

    public string? ModelId { get; init; }

    public AgentModelParameters? ModelParameters { get; init; }

    public IReadOnlyList<AIFunction>? Tools { get; init; }
}
