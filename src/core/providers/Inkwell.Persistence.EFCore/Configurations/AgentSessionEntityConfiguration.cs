// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inkwell.Persistence.EFCore.Configurations;

internal sealed class AgentSessionEntityConfiguration : IEntityTypeConfiguration<AgentSessionEntity>
{
    public void Configure(EntityTypeBuilder<AgentSessionEntity> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.AgentId, x.OwnerUserId });
        b.HasIndex(x => x.AgentVersionId);
        b.Property(x => x.Title).HasMaxLength(30);
        b.Property(x => x.SessionState);
        b.HasOne<AgentVersionEntity>()
            .WithMany()
            .HasForeignKey(x => x.AgentVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Messages)
            .WithOne(x => x.Session)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
