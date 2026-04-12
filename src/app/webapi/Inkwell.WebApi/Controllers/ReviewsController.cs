using Inkwell;
using Microsoft.AspNetCore.Mvc;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 审核记录控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ReviewsController(IReviewPersistenceProvider reviewProvider) : ControllerBase
{
    /// <summary>
    /// 根据文章 ID 获取审核记录
    /// </summary>
    /// <param name="articleId">文章 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审核记录列表</returns>
    [HttpGet("{articleId}")]
    [ProducesResponseType(typeof(IReadOnlyList<ReviewRecord>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByArticleIdAsync(string articleId, CancellationToken cancellationToken)
    {
        IReadOnlyList<ReviewRecord> reviews = await reviewProvider.GetByArticleIdAsync(articleId, cancellationToken);
        return this.Ok(reviews);
    }
}
