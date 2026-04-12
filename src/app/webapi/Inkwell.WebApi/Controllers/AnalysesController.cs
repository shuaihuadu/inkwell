using Inkwell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 分析报告控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "EditorOrAdmin")]
public sealed class AnalysesController(IAnalysisPersistenceProvider analysisProvider) : ControllerBase
{
    /// <summary>
    /// 根据流水线运行 ID 获取分析报告
    /// </summary>
    /// <param name="pipelineRunId">流水线运行 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分析报告列表</returns>
    [HttpGet("{pipelineRunId}")]
    [ProducesResponseType(typeof(IReadOnlyList<AnalysisRecord>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByPipelineRunIdAsync(string pipelineRunId, CancellationToken cancellationToken)
    {
        IReadOnlyList<AnalysisRecord> analyses = await analysisProvider.QueryAsync(
            a => a.PipelineRunId == pipelineRunId, cancellationToken);
        return this.Ok(analyses);
    }
}
