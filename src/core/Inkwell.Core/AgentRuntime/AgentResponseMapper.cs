using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Inkwell;

/// <summary>
/// <see cref="AgentTurnResult"/>/<see cref="AgentRunEvent"/>（Inkwell 自有 Run 级 DTO）反向转换为 MAF
/// <see cref="AgentResponse"/>/<see cref="AgentResponseUpdate"/> 的映射。供 <see cref="RoutingAgent"/>
/// 使用，使其能满足 <see cref="AIAgent"/> 的公共契约，从而喂给 MAF 官方 <c>MapAGUI</c> 等托管管线。
/// </summary>
internal static class AgentResponseMapper
{
    /// <summary>将非流式 <see cref="AgentTurnResult"/> 转换为 <see cref="AgentResponse"/>。</summary>
    public static AgentResponse ToAgentResponse(AgentTurnResult result) => new(AgentChatMessageMapper.ToChatMessage(result.Message));

    /// <summary>
    /// 把 <see cref="IAgentInvocationService.RunStreamingAsync"/> 产出的 <see cref="AgentRunEvent"/> 流转换为
    /// <see cref="AgentResponseUpdate"/> 流。
    /// </summary>
    /// <remarks>
    /// <see cref="StateDelta"/> 在 <see cref="ChatMessage"/>/<see cref="AIContent"/> 体系下没有对应表达，跳过不产出；
    /// <see cref="RunCompleted"/> 仅是流结束标记，不单独产出；<see cref="RunError"/> 按 <see cref="AIAgent"/> 的错误契约
    /// （异常传播，而非特殊事件类型）转换为异常抛出——MAF 官方 AG-UI 管线会把传播上来的异常自动转换为 AG-UI 的
    /// <c>lifecycle</c>(error) 事件，不需要在这里手工产出。
    /// </remarks>
    public static async IAsyncEnumerable<AgentResponseUpdate> ToAgentResponseUpdatesAsync(IAsyncEnumerable<AgentRunEvent> events)
    {
        await foreach (AgentRunEvent runEvent in events.ConfigureAwait(false))
        {
            switch (runEvent)
            {
                case TextDelta textDelta:
                    yield return new AgentResponseUpdate(ChatRole.Assistant, textDelta.DeltaText);
                    break;

                case ToolCallRequested toolCallRequested:
                    yield return new AgentResponseUpdate(ChatRole.Assistant, [ToFunctionCallContent(toolCallRequested)]);
                    break;

                case ToolCallResult toolCallResult:
                    yield return new AgentResponseUpdate(ChatRole.Tool, [ToFunctionResultContent(toolCallResult)]);
                    break;

                case StateDelta:
                    // AG-UI state_delta 在 ChatMessage/AIContent 体系下无对应表达，跳过。
                    break;

                case RunCompleted:
                    // 仅是流结束标记，枚举本身结束即可，不需要额外产出。
                    break;

                case RunError runError:
                    throw new InvalidOperationException($"Agent run failed ({runError.ExceptionType}): {runError.ErrorMessage}");
            }
        }
    }

    private static FunctionCallContent ToFunctionCallContent(ToolCallRequested toolCallRequested)
    {
        IDictionary<string, object?>? arguments = JsonSerializer.Deserialize<IDictionary<string, object?>>(toolCallRequested.ArgumentsJson);

        return new FunctionCallContent(toolCallRequested.ToolCallId, toolCallRequested.ToolName, arguments);
    }

    private static FunctionResultContent ToFunctionResultContent(ToolCallResult toolCallResult)
    {
        JsonElement result = JsonSerializer.Deserialize<JsonElement>(toolCallResult.ResultJson);

        return new FunctionResultContent(toolCallResult.ToolCallId, result)
        {
            Exception = toolCallResult.IsError ? new InvalidOperationException("Tool call failed.") : null,
        };
    }
}
