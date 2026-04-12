using System.Text.Json;
using Inkwell.Core;
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
        AgentResponse response = await agent.RunAsync(
            $"Analyze competitor content and suggest unique angles for the topic: {topic}",
            cancellationToken: cancellationToken);

        TopicAnalysis analysis = JsonSerializer.Deserialize<TopicAnalysis>(response.Text)
            ?? new TopicAnalysis { Topic = topic };

        analysis.Topic = topic;

        return analysis;
    }
}
