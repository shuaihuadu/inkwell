using Inkwell;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace Inkwell.Workflows.Executors;

/// <summary>
/// 翻译 Executor
/// 调用 AI Agent 将文本翻译为指定语言
/// </summary>
[SendsMessage(typeof(TranslationResult))]
internal sealed class TranslatorExecutor(AIAgent agent, string language) : Executor<string>($"Translator_{language}")
{
    /// <inheritdoc />
    public override async ValueTask HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        AgentResponse response = await agent.RunAsync(
            $"请将以下内容翻译为{language}：\n\n{message}",
            cancellationToken: cancellationToken);

        TranslationResult result = new()
        {
            Language = language,
            Content = response.Text
        };

        await context.SendMessageAsync(result, cancellationToken: cancellationToken);
    }
}
