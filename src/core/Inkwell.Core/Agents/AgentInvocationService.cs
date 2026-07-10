using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Inkwell;

/// <summary><see cref="IAgentInvocationService"/> 唯一实现；<see cref="AgentDefinition"/> → <see cref="AgentRunRequest"/> 翻译 + 调用 <see cref="IAgentRuntime"/>。</summary>
internal sealed class AgentInvocationService(
    IAgentRepository agents,
    IAgentRuntime agentRuntime,
    IAgentToolBindingResolver toolBindingResolver) : IAgentInvocationService
{
    public async Task<AgentTurnResult> RunAsync(Guid agentId, Guid callerUserId, Guid? conversationId, IReadOnlyList<AgentChatMessage> messages, CancellationToken ct = default)
    {
        AgentRunRequest request = await this.PrepareRunRequestAsync(agentId, callerUserId, conversationId, messages, ct).ConfigureAwait(false);

        return await agentRuntime.RunAsync(request, ct).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<AgentRunEvent> RunStreamingAsync(Guid agentId, Guid callerUserId, Guid? conversationId, IReadOnlyList<AgentChatMessage> messages, [EnumeratorCancellation] CancellationToken ct = default)
    {
        AgentRunRequest request = await this.PrepareRunRequestAsync(agentId, callerUserId, conversationId, messages, ct).ConfigureAwait(false);

        await foreach (AgentRunEvent? runEvent in agentRuntime.RunStreamingAsync(request, ct).ConfigureAwait(false))
        {
            yield return runEvent;
        }
    }

    private async Task<AgentRunRequest> PrepareRunRequestAsync(Guid agentId, Guid callerUserId, Guid? conversationId, IReadOnlyList<AgentChatMessage> messages, CancellationToken ct)
    {
        AgentDefinition agent = await agents.GetAgent(agentId, ct).ConfigureAwait(false);

        ValidateInvocationAccess(agent, callerUserId);

        IReadOnlyList<AIFunction> tools = await toolBindingResolver.ResolveAsync(agent.ToolBindings, ct).ConfigureAwait(false);

        return BuildRunRequest(agent, conversationId, messages, tools);
    }

    private static AgentRunRequest BuildRunRequest(AgentDefinition agent, Guid? conversationId, IReadOnlyList<AgentChatMessage> messages, IReadOnlyList<AIFunction> tools) => new()
    {
        RunId = Guid.CreateVersion7().ToString(),
        AgentId = agent.Id,
        ConversationId = conversationId,
        Messages = messages,
        Instructions = agent.Instructions,
        ModelId = agent.ModelId,
        ModelParameters = agent.ModelParameters,
        Tools = tools,
    };

    private static void ValidateInvocationAccess(AgentDefinition agent, Guid callerUserId)
    {
        if (agent.OwnerUserId != callerUserId && !agent.IsShared)
        {
            throw new UnauthorizedAccessException($"User '{callerUserId}' is not authorized to invoke agent '{agent.Id}'.");
        }
    }
}
