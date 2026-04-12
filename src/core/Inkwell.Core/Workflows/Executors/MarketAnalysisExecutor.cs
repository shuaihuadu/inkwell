using System.Text.Json;
using Inkwell;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Inkwell.Workflows.Executors;

/// <summary>
/// 市场趋势分析 Executor
/// 分析给定主题的市场趋势和受众特征
/// </summary>
internal sealed class MarketAnalysisExecutor(AIAgent agent) : Executor<string, TopicAnalysis>("MarketAnalysis")
{
    /// <inheritdoc />
    public override async ValueTask<TopicAnalysis> HandleAsync(string topic, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        AgentResponse response = await agent.RunAsync(
            $"请分析以下主题的市场趋势和目标受众：{topic}",
            cancellationToken: cancellationToken);

        TopicAnalysis analysis = JsonSerializer.Deserialize<TopicAnalysis>(response.Text)
            ?? new TopicAnalysis { Topic = topic };

        analysis.Topic = topic;

        return analysis;
    }
}
