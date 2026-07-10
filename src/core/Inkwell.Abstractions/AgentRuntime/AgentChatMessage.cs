using Microsoft.Extensions.AI;

namespace Inkwell;

/// <summary>
/// 对话消息 DTO；跨 <see cref="AgentRunRequest.Messages"/> / <see cref="AgentTurnResult.Message"/> /
/// 流式 <see cref="TextDelta"/> 复用；不泄漏 MAF <c>ChatMessage</c> 类型。
/// </summary>
public sealed record class AgentChatMessage
{
    public required ChatRole Role { get; init; }

    public required IReadOnlyList<AIContent> Content { get; init; }

    /// <summary>多 Agent 场景标识发言方（REQ-012 编排预留）。</summary>
    public string? AuthorName { get; init; }
}
