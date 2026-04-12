using Inkwell;
using Inkwell.Agents;
using Inkwell.Workflows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// Dashboard 统计控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "EditorOrAdmin")]
public sealed class DashboardController(
    AgentRegistry agentRegistry,
    WorkflowRegistry workflowRegistry,
    IArticlePersistenceProvider articleProvider,
    IPipelineRunPersistenceProvider runProvider) : ControllerBase
{
    /// <summary>
    /// 获取 Dashboard 统计数据
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>统计数据</returns>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ArticleRecord> articles = await articleProvider.GetAllAsync(cancellationToken);
        IReadOnlyList<PipelineRunRecord> runs = await runProvider.GetAllAsync(cancellationToken);

        return this.Ok(new
        {
            agentCount = agentRegistry.Count,
            workflowCount = workflowRegistry.Count,
            totalRuns = runs.Count,
            publishedArticles = articles.Count(a => a.Status == nameof(ArticleStatus.Published)),
            totalArticles = articles.Count,
            completedRuns = runs.Count(r => r.Status == "Completed"),
            approvalRate = runs.Count > 0
                ? Math.Round(runs.Count(r => r.Status == "Completed") * 100.0 / runs.Count)
                : 0
        });
    }
}
