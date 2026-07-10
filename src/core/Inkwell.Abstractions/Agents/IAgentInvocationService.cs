
namespace Inkwell;

/// <summary>
/// 把 <see cref="Persistence.Agents.AgentDefinition"/>（持久化配置）翻译为 <see cref="AgentRunRequest"/> 并调用
/// <see cref="IAgentRuntime"/>；调用前对 <c>callerUserId</c> 做调用权限校验。
/// </summary>
public interface IAgentInvocationService
{
    Task<AgentTurnResult> RunAsync(Guid agentId, Guid callerUserId, Guid? conversationId, IReadOnlyList<AgentChatMessage> messages, CancellationToken ct = default);

    IAsyncEnumerable<AgentRunEvent> RunStreamingAsync(Guid agentId, Guid callerUserId, Guid? conversationId, IReadOnlyList<AgentChatMessage> messages, CancellationToken ct = default);
}
