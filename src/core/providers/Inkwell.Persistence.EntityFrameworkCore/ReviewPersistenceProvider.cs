using Inkwell;
using Inkwell.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inkwell.Persistence.EntityFrameworkCore;

/// <summary>
/// 基于 EF Core 的审核记录持久化提供程序
/// </summary>
public sealed class ReviewPersistenceProvider(InkwellDbContext dbContext)
    : PersistenceProvider<ReviewEntity, ReviewRecord, string>(dbContext), IReviewPersistenceProvider
{
    /// <inheritdoc />
    protected override string GetKey(ReviewRecord model) => model.Id;

    /// <inheritdoc />
    protected override ReviewEntity ToEntity(ReviewRecord model) => new()
    {
        Id = model.Id,
        ArticleId = model.ArticleId,
        Revision = model.Revision,
        ReviewerType = model.ReviewerType,
        Approved = model.Approved,
        Feedback = model.Feedback,
        Score = model.Score,
        CreatedAt = model.CreatedAt
    };

    /// <inheritdoc />
    protected override ReviewRecord ToModel(ReviewEntity entity) => new()
    {
        Id = entity.Id,
        ArticleId = entity.ArticleId,
        Revision = entity.Revision,
        ReviewerType = entity.ReviewerType,
        Approved = entity.Approved,
        Feedback = entity.Feedback,
        Score = entity.Score,
        CreatedAt = entity.CreatedAt
    };

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReviewRecord>> GetByArticleIdAsync(string articleId, CancellationToken cancellationToken = default)
    {
        List<ReviewEntity> entities = await this.DbContext.Reviews
            .Where(r => r.ArticleId == articleId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities.Select(this.ToModel).ToList().AsReadOnly();
    }
}
