using Inkwell;
using Inkwell.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inkwell.Persistence.EntityFrameworkCore;

/// <summary>
/// 基于 EF Core 的文章持久化提供程序
/// </summary>
public sealed class ArticlePersistenceProvider(InkwellDbContext dbContext)
    : PersistenceProvider<ArticleEntity, ArticleRecord, string>(dbContext), IArticlePersistenceProvider
{
    /// <inheritdoc />
    protected override string GetKey(ArticleRecord model) => model.Id;

    /// <inheritdoc />
    protected override ArticleEntity ToEntity(ArticleRecord model) => new()
    {
        Id = model.Id,
        Topic = model.Topic,
        Title = model.Title,
        Content = model.Content,
        Status = model.Status,
        Revision = model.Revision,
        CreatedAt = model.CreatedAt,
        UpdatedAt = model.UpdatedAt
    };

    /// <inheritdoc />
    protected override ArticleRecord ToModel(ArticleEntity entity) => new()
    {
        Id = entity.Id,
        Topic = entity.Topic,
        Title = entity.Title,
        Content = entity.Content,
        Status = entity.Status,
        Revision = entity.Revision,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    /// <inheritdoc />
    public async Task<IReadOnlyList<ArticleRecord>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        List<ArticleEntity> entities = await this.DbContext.Articles
            .Where(a => a.Status == status)
            .OrderByDescending(a => a.UpdatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities.Select(this.ToModel).ToList().AsReadOnly();
    }
}
