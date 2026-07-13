// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inkwell.Persistence.EFCore.Configurations;

internal sealed class AgentSkillEntityConfiguration : IEntityTypeConfiguration<AgentSkillEntity>
{
    public void Configure(EntityTypeBuilder<AgentSkillEntity> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).IsRequired();
        b.Property(x => x.ContentMarkdown).IsRequired();
    }
}
