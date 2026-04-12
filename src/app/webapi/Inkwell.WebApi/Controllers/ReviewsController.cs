using Inkwell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 审核记录控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ReviewsController(
    IReviewPersistenceProvider reviewProvider,
    IArticlePersistenceProvider articleProvider) : ControllerBase
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

    /// <summary>
    /// 审核通过
    /// </summary>
    /// <param name="articleId">文章 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPost("{articleId}/approve")]
    [Authorize(Policy = "ReviewerOrAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveAsync(string articleId, CancellationToken cancellationToken)
    {
        ArticleRecord? article = await articleProvider.GetByIdAsync(articleId, cancellationToken);
        if (article is null)
        {
            return this.NotFound();
        }

        ReviewRecord review = new()
        {
            Id = Guid.NewGuid().ToString(),
            ArticleId = articleId,
            Approved = true,
            Feedback = "审核通过",
            ReviewerType = "Human",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await reviewProvider.AddAsync(review, cancellationToken);

        return this.Ok(new { status = "approved", articleId, reviewId = review.Id });
    }

    /// <summary>
    /// 审核退回
    /// </summary>
    /// <param name="articleId">文章 ID</param>
    /// <param name="request">退回原因</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPost("{articleId}/reject")]
    [Authorize(Policy = "ReviewerOrAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectAsync(string articleId, [FromBody] RejectRequest? request, CancellationToken cancellationToken)
    {
        ArticleRecord? article = await articleProvider.GetByIdAsync(articleId, cancellationToken);
        if (article is null)
        {
            return this.NotFound();
        }

        ReviewRecord review = new()
        {
            Id = Guid.NewGuid().ToString(),
            ArticleId = articleId,
            Approved = false,
            Feedback = request?.Reason ?? "审核退回",
            ReviewerType = "Human",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await reviewProvider.AddAsync(review, cancellationToken);

        return this.Ok(new { status = "rejected", articleId, reviewId = review.Id, reason = review.Feedback });
    }
}

/// <summary>
/// 退回请求
/// </summary>
public sealed class RejectRequest
{
    /// <summary>
    /// 获取或设置退回原因
    /// </summary>
    public string? Reason { get; set; }
}
