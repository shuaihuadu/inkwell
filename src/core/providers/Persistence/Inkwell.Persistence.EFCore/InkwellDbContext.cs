// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace Inkwell.Persistence.EFCore;

/// <summary>
/// 表示 EF Core 共享数据库上下文，登记全部实体集合并应用时间戳、所有者索引和实体类型配置。
/// 具体数据库适配器可通过继承调整数据库提供程序专属行为。
/// </summary>
/// <param name="options">数据库上下文配置。</param>
public class InkwellDbContext(DbContextOptions<InkwellDbContext> options) : DbContext(options)
{
    internal DbSet<AgentEntity> Agents => this.Set<AgentEntity>();

    internal DbSet<AgentVersionEntity> AgentVersions => this.Set<AgentVersionEntity>();

    internal DbSet<UserEntity> Users => this.Set<UserEntity>();

    internal DbSet<AgentToolEntity> AgentTools => this.Set<AgentToolEntity>();

    internal DbSet<AgentConversationEntity> AgentConversations => this.Set<AgentConversationEntity>();

    internal DbSet<AgentChatMessageEntity> AgentChatMessages => this.Set<AgentChatMessageEntity>();

    internal DbSet<AgentSkillEntity> AgentSkills => this.Set<AgentSkillEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InkwellDbContext).Assembly);

        try
        {
            ApplyTimestamps(modelBuilder);
            ApplyOwnerIndex(modelBuilder);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException($"Failed to apply mixin to entity types: {ex.Message}", ex);
        }

        ILogger<InkwellDbContext> logger = this.GetService<ILoggerFactory>().CreateLogger<InkwellDbContext>();
        List<IMutableEntityType> entityTypes = modelBuilder.Model.GetEntityTypes().ToList();

        logger.LogInformation(
            "EFCore model created: {EntityCount} entities, {TimestampedCount} with IHasTimestamps",
            entityTypes.Count,
            entityTypes.Count(t => typeof(IHasTimestamps).IsAssignableFrom(t.ClrType)));
    }

    private static void ApplyTimestamps(ModelBuilder mb)
    {
        foreach (IMutableEntityType? entityType in mb.Model.GetEntityTypes().Where(t => typeof(IHasTimestamps).IsAssignableFrom(t.ClrType)))
        {
            mb.Entity(entityType.ClrType).Property(nameof(IHasTimestamps.CreatedTime)).IsRequired();
            mb.Entity(entityType.ClrType).Property(nameof(IHasTimestamps.UpdatedTime)).IsRequired();
        }
    }

    private static void ApplyOwnerIndex(ModelBuilder mb)
    {
        foreach (IMutableEntityType? entityType in mb.Model.GetEntityTypes().Where(t => typeof(IHasOwner).IsAssignableFrom(t.ClrType)))
        {
            mb.Entity(entityType.ClrType).HasIndex(nameof(IHasOwner.OwnerUserId));
        }
    }
}
