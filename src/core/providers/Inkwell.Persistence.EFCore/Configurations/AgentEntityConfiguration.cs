// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inkwell.Persistence.EFCore.Configurations;

internal sealed class AgentEntityConfiguration : IEntityTypeConfiguration<AgentEntity>
{
    public void Configure(EntityTypeBuilder<AgentEntity> b)
    {
        b.ToTable("agents");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(50);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.ToolBindingsJson).IsRequired();
        b.Property(x => x.SkillBindingsJson).IsRequired();
        b.HasIndex(x => x.IsShared);
    }
}
