using Inkwell;
using Inkwell.Persistence.EntityFrameworkCore.Entities;

namespace Inkwell.Persistence.EntityFrameworkCore;

/// <summary>
/// 基于 EF Core 的分析报告持久化提供程序
/// </summary>
public sealed class EfCoreAnalysisPersistenceProvider(InkwellDbContext dbContext)
    : EfCorePersistenceProvider<AnalysisEntity, AnalysisRecord, string>(dbContext), IAnalysisPersistenceProvider
{
    /// <inheritdoc />
    protected override string GetKey(AnalysisRecord model) => model.Id;

    /// <inheritdoc />
    protected override AnalysisEntity ToEntity(AnalysisRecord model) => new()
    {
        Id = model.Id,
        PipelineRunId = model.PipelineRunId,
        Topic = model.Topic,
        MarketTrends = model.MarketTrends,
        TargetAudience = model.TargetAudience,
        ContentAngles = model.ContentAngles,
        CreatedAt = model.CreatedAt
    };

    /// <inheritdoc />
    protected override AnalysisRecord ToModel(AnalysisEntity entity) => new()
    {
        Id = entity.Id,
        PipelineRunId = entity.PipelineRunId,
        Topic = entity.Topic,
        MarketTrends = entity.MarketTrends,
        TargetAudience = entity.TargetAudience,
        ContentAngles = entity.ContentAngles,
        CreatedAt = entity.CreatedAt
    };
}
