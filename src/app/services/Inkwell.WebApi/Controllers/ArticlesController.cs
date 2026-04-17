using Inkwell;
using Microsoft.AspNetCore.Mvc;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 文章管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ArticlesController(IArticlePersistenceProvider articleProvider) : ControllerBase
{
    /// <summary>
    /// 获取所有文章
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ArticleRecord> articles = await articleProvider.GetAllAsync(cancellationToken);
        return this.Ok(articles);
    }

    /// <summary>
    /// 根据 ID 获取文章
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        ArticleRecord? article = await articleProvider.GetByIdAsync(id, cancellationToken);
        return article is not null ? this.Ok(article) : this.NotFound();
    }

    /// <summary>
    /// 根据状态查询文章
    /// </summary>
    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetByStatusAsync(string status, CancellationToken cancellationToken)
    {
        IReadOnlyList<ArticleRecord> articles = await articleProvider.GetByStatusAsync(status, cancellationToken);
        return this.Ok(articles);
    }

    /// <summary>
    /// 创建文章（从对话中保存）
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateArticleRequest request, CancellationToken cancellationToken)
    {
        ArticleRecord article = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Topic = request.Topic ?? "",
            Title = request.Title,
            Content = request.Content,
            Status = nameof(ArticleStatus.Draft),
            Revision = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await articleProvider.AddAsync(article, cancellationToken);
        return this.Created($"/api/articles/{article.Id}", article);
    }

    /// <summary>
    /// 更新文章状态
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatusAsync(string id, [FromBody] UpdateArticleStatusRequest request, CancellationToken cancellationToken)
    {
        ArticleRecord? article = await articleProvider.GetByIdAsync(id, cancellationToken);
        if (article is null)
        {
            return this.NotFound();
        }

        article.Status = request.Status;
        article.UpdatedAt = DateTimeOffset.UtcNow;
        await articleProvider.UpdateAsync(article, cancellationToken);
        return this.Ok(article);
    }

    /// <summary>
    /// 更新文章内容
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAsync(string id, [FromBody] UpdateArticleRequest request, CancellationToken cancellationToken)
    {
        ArticleRecord? article = await articleProvider.GetByIdAsync(id, cancellationToken);
        if (article is null)
        {
            return this.NotFound();
        }

        article.Title = request.Title ?? article.Title;
        article.Content = request.Content ?? article.Content;
        article.Revision++;
        article.UpdatedAt = DateTimeOffset.UtcNow;
        await articleProvider.UpdateAsync(article, cancellationToken);
        return this.Ok(article);
    }

    /// <summary>
    /// 删除文章
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        bool deleted = await articleProvider.DeleteAsync(id, cancellationToken);
        return deleted ? this.NoContent() : this.NotFound();
    }
}

/// <summary>
/// 创建文章请求
/// </summary>
public sealed class CreateArticleRequest
{
    /// <summary>
    /// 获取或设置文章标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文章内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文章主题
    /// </summary>
    public string? Topic { get; set; }
}

/// <summary>
/// 更新文章状态请求
/// </summary>
public sealed class UpdateArticleStatusRequest
{
    /// <summary>
    /// 获取或设置目标状态
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// 更新文章内容请求
/// </summary>
public sealed class UpdateArticleRequest
{
    /// <summary>
    /// 获取或设置文章标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 获取或设置文章内容
    /// </summary>
    public string? Content { get; set; }
}
