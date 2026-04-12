using Inkwell.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inkwell.Persistence.EntityFrameworkCore;

/// <summary>
/// Inkwell 数据库上下文
/// </summary>
public class InkwellDbContext(DbContextOptions<InkwellDbContext> options) : DbContext(options)
{
    /// <summary>
    /// 获取或设置文章数据集
    /// </summary>
    public DbSet<ArticleEntity> Articles => this.Set<ArticleEntity>();

    /// <summary>
    /// 获取或设置流水线运行记录数据集
    /// </summary>
    public DbSet<PipelineRunEntity> PipelineRuns => this.Set<PipelineRunEntity>();

    /// <summary>
    /// 获取或设置分析报告数据集
    /// </summary>
    public DbSet<AnalysisEntity> Analyses => this.Set<AnalysisEntity>();

    /// <summary>
    /// 获取或设置审核记录数据集
    /// </summary>
    public DbSet<ReviewEntity> Reviews => this.Set<ReviewEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Attribute 已处理 Key/Required/MaxLength/Table，Fluent API 仅补充索引
        modelBuilder.Entity<ArticleEntity>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<PipelineRunEntity>(entity =>
        {
            entity.HasIndex(e => e.StartedAt);
        });

        modelBuilder.Entity<AnalysisEntity>(entity =>
        {
            entity.HasIndex(e => e.PipelineRunId);
        });

        modelBuilder.Entity<ReviewEntity>(entity =>
        {
            entity.HasIndex(e => e.ArticleId);
        });
    }
}
