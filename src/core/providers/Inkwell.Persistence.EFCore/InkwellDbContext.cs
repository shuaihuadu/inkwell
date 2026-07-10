// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Reflection;
using Inkwell.Persistence.EFCore.Entities;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Logging;

namespace Inkwell.Persistence.EFCore;

/// <summary>
/// base <see cref="DbContext"/>；登记全部 <c>DbSet&lt;XxxEntity&gt;</c>，<see cref="OnModelCreating"/> 扫描三 mixin
/// 并应用全部 <see cref="IEntityTypeConfiguration{TEntity}"/>。final adapter 通过继承调整 Provider-specific 行为。
/// </summary>
public class InkwellDbContext(DbContextOptions<InkwellDbContext> options) : DbContext(options)
{
    internal DbSet<AgentEntity> Agents => this.Set<AgentEntity>();

    internal DbSet<UserEntity> Users => this.Set<UserEntity>();

    internal DbSet<AgentToolEntity> Tools => this.Set<AgentToolEntity>();

    internal DbSet<AgentConversationEntity> Conversations => this.Set<AgentConversationEntity>();

    internal DbSet<AgentConversationMessageEntity> ConversationMessages => this.Set<AgentConversationMessageEntity>();

    internal DbSet<AgentSkillEntity> Skills => this.Set<AgentSkillEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InkwellDbContext).Assembly);

        try
        {
            ApplyTimestamps(modelBuilder);
            this.ApplyRowVersion(modelBuilder);
            ApplyOwnerIndex(modelBuilder);
            ApplyEnumAsString(modelBuilder);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException($"Failed to apply mixin to entity types: {ex.Message}", ex);
        }

        ILogger<InkwellDbContext> logger = this.GetService<ILoggerFactory>().CreateLogger<InkwellDbContext>();
        List<IMutableEntityType> entityTypes = modelBuilder.Model.GetEntityTypes().ToList();

        logger.LogInformation(
            "EFCore model created: {EntityCount} entities, {TimestampedCount} with IHasTimestamps, {RowVersionedCount} with IHasRowVersion",
            entityTypes.Count,
            entityTypes.Count(t => typeof(IHasTimestamps).IsAssignableFrom(t.ClrType)),
            entityTypes.Count(t => typeof(IHasRowVersion).IsAssignableFrom(t.ClrType)));
    }

    private static void ApplyTimestamps(ModelBuilder mb)
    {
        foreach (IMutableEntityType? entityType in mb.Model.GetEntityTypes().Where(t => typeof(IHasTimestamps).IsAssignableFrom(t.ClrType)))
        {
            mb.Entity(entityType.ClrType).Property(nameof(IHasTimestamps.CreatedTime)).IsRequired();
            mb.Entity(entityType.ClrType).Property(nameof(IHasTimestamps.UpdatedTime)).IsRequired();
        }
    }

    /// <summary>
    /// SqlServer 原生 <c>rowversion</c> 列由数据库引擎自增生成，<c>IsRowVersion()</c>（隐含
    /// <c>ValueGeneratedOnAddOrUpdate</c>）语义正确；但 Postgres 没有对应的数据库端自增机制，
    /// <c>ValueGeneratedOnAddOrUpdate</c> 会让 EF Core 认为"数据库会生成该值"从而在 INSERT 语句里
    /// 整列跳过——实测会导致 <c>NOT NULL constraint violation</c>（2026-07-09 Testcontainers spike
    /// 验证坐实 design-review-report.md §21 B20 标注的未验证假设：此前实现在真实 PostgreSQL 上
    /// 连 INSERT 都会失败）。Postgres 场景改为只标记 <c>IsConcurrencyToken()</c>，新值完全由
    /// <c>PostgresRowVersionInterceptor.SavingChangesAsync</c> 在 Added/Modified 时手动赋值，
    /// 交给正常的 INSERT/UPDATE 语句当作客户端提供的普通列值发送。
    /// </summary>
    private void ApplyRowVersion(ModelBuilder mb)
    {
        bool isPostgres = this.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

        foreach (IMutableEntityType? entityType in mb.Model.GetEntityTypes().Where(t => typeof(IHasRowVersion).IsAssignableFrom(t.ClrType)))
        {
            PropertyBuilder property = mb.Entity(entityType.ClrType).Property(nameof(IHasRowVersion.RowVersion));

            if (isPostgres)
            {
                property.IsConcurrencyToken();
            }
            else
            {
                property.IsRowVersion();
            }
        }
    }

    private static void ApplyOwnerIndex(ModelBuilder mb)
    {
        foreach (IMutableEntityType? entityType in mb.Model.GetEntityTypes().Where(t => typeof(IHasOwner).IsAssignableFrom(t.ClrType)))
        {
            mb.Entity(entityType.ClrType).HasIndex(nameof(IHasOwner.OwnerUserId));
        }
    }

    private static void ApplyEnumAsString(ModelBuilder mb)
    {
        foreach (IMutableEntityType entityType in mb.Model.GetEntityTypes())
        {
            foreach (PropertyInfo property in entityType.ClrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.PropertyType.IsEnum)
                {
                    mb.Entity(entityType.ClrType).Property(property.Name).HasConversion<string>().HasMaxLength(64);
                }
            }
        }
    }
}
