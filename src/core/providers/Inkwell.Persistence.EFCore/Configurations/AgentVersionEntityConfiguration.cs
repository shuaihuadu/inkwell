// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inkwell.Persistence.EFCore.Configurations;

internal sealed class AgentVersionEntityConfiguration : IEntityTypeConfiguration<AgentVersionEntity>
{
    public void Configure(EntityTypeBuilder<AgentVersionEntity> builder)
    {
        builder.HasKey(version => version.Id);
        builder.HasAlternateKey(version => new { version.AgentId, version.Id });
        builder.HasIndex(version => new { version.AgentId, version.VersionNumber }).IsUnique();
        builder.Property(version => version.Snapshot).IsRequired();
        builder.Property(version => version.ChangeSummary).HasMaxLength(500);
        builder.HasOne<AgentEntity>()
            .WithMany()
            .HasForeignKey(version => version.AgentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
