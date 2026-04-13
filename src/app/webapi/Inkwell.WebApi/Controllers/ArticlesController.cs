using Inkwell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 文章管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "EditorOrAdmin")]
public sealed class ArticlesController(IArticlePersistenceProvider articleProvider) : ControllerBase
{
    /// <summary>
    /// 获取所有文章
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文章列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ArticleRecord>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ArticleRecord> articles = await articleProvider.GetAllAsync(cancellationToken);
        return this.Ok(articles);
    }

    /// <summary>
    /// 根据 ID 获取文章
    /// </summary>
    /// <param name="id">文章 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文章详情</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ArticleRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        ArticleRecord? article = await articleProvider.GetByIdAsync(id, cancellationToken);
        return article is not null ? this.Ok(article) : this.NotFound();
    }

    /// <summary>
    /// 根据状态查询文章
    /// </summary>
    /// <param name="status">文章状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的文章列表</returns>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(IReadOnlyList<ArticleRecord>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatusAsync(string status, CancellationToken cancellationToken)
    {
        IReadOnlyList<ArticleRecord> articles = await articleProvider.GetByStatusAsync(status, cancellationToken);
        return this.Ok(articles);
    }
}
