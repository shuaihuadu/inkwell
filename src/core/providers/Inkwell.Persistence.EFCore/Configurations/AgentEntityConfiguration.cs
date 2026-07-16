// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inkwell.Persistence.EFCore.Configurations;

internal sealed class AgentEntityConfiguration : IEntityTypeConfiguration<AgentEntity>
{
    public void Configure(EntityTypeBuilder<AgentEntity> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.IsShared);
        b.HasIndex(x => x.CurrentPublishedVersionId);
        b.Property(x => x.Name).HasMaxLength(50).IsRequired();
        b.Property(x => x.AvatarUri).HasMaxLength(2048);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.BuildOptions).IsRequired();
    }
}
