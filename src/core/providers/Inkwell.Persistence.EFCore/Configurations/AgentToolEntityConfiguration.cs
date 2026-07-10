// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inkwell.Persistence.EFCore.Configurations;

internal sealed class AgentToolEntityConfiguration : IEntityTypeConfiguration<AgentToolEntity>
{
    public void Configure(EntityTypeBuilder<AgentToolEntity> b)
    {
        b.ToTable("tools");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.Name).IsUnique();
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).IsRequired();
        b.Property(x => x.ParametersJsonSchema).IsRequired();
    }
}
