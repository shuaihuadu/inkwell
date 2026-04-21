using System.Text.Json;
using Inkwell;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Inkwell.Workflows.Executors;

/// <summary>
/// 竞品内容分析 Executor
/// 分析同类主题的竞品内容，提供差异化建议
/// </summary>
internal sealed class CompetitorAnalysisExecutor(AIAgent agent) : Executor<string, TopicAnalysis>("CompetitorAnalysis")
{
    /// <inheritdoc />
    public override async ValueTask<TopicAnalysis> HandleAsync(string topic, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        string text = await agent.RunAndStreamAsync(
            $"请分析以下主题的竞品内容，并提出差异化的内容角度建议：{topic}",
            this.Id, context, cancellationToken);

        TopicAnalysis analysis = JsonSerializer.Deserialize<TopicAnalysis>(text)
            ?? new TopicAnalysis { Topic = topic };

        analysis.Topic = topic;

        return analysis;
    }
}
