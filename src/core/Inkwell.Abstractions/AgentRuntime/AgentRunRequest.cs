namespace Inkwell;

/// <summary>
/// 一次 Run 调用的全部已解析上下文；由调用方（<c>Inkwell.WebApi</c>）在调用前查好 Agent 配置 /
/// 对话历史 / 工具注册表并组装。端口本身不反向依赖 <c>IPersistenceProvider</c>。
/// </summary>
public sealed record class AgentRunRequest
{
    public required string RunId { get; init; }

    public required Guid AgentId { get; init; }

    public Guid? ConversationId { get; init; }

    public required IReadOnlyList<AgentChatMessage> Messages { get; init; }

    public string? Instructions { get; init; }

    public string? ModelId { get; init; }

    public AgentModelParameters? ModelParameters { get; init; }

    public IReadOnlyList<AIFunction>? Tools { get; init; }
}
