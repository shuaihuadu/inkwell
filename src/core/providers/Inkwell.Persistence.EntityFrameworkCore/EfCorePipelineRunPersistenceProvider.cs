using Inkwell;
using Inkwell.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inkwell.Persistence.EntityFrameworkCore;

/// <summary>
/// 基于 EF Core 的流水线运行记录持久化提供程序
/// </summary>
public sealed class EfCorePipelineRunPersistenceProvider(InkwellDbContext dbContext)
    : EfCorePersistenceProvider<PipelineRunEntity, PipelineRunRecord, string>(dbContext), IPipelineRunPersistenceProvider
{
    /// <inheritdoc />
    protected override string GetKey(PipelineRunRecord model) => model.Id;

    /// <inheritdoc />
    protected override PipelineRunEntity ToEntity(PipelineRunRecord model) => new()
    {
        Id = model.Id,
        Topic = model.Topic,
        Status = model.Status,
        ArticleId = model.ArticleId,
        StartedAt = model.StartedAt,
        CompletedAt = model.CompletedAt,
        TotalRevisions = model.TotalRevisions
    };

    /// <inheritdoc />
    protected override PipelineRunRecord ToModel(PipelineRunEntity entity) => new()
    {
        Id = entity.Id,
        Topic = entity.Topic,
        Status = entity.Status,
        ArticleId = entity.ArticleId,
        StartedAt = entity.StartedAt,
        CompletedAt = entity.CompletedAt,
        TotalRevisions = entity.TotalRevisions
    };

    /// <inheritdoc />
    public async Task<IReadOnlyList<PipelineRunRecord>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        List<PipelineRunEntity> entities = await this.DbContext.PipelineRuns
            .OrderByDescending(r => r.StartedAt)
            .Take(count)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities.Select(this.ToModel).ToList().AsReadOnly();
    }
}
