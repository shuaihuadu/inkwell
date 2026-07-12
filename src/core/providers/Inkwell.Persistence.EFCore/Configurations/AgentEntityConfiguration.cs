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
        b.HasIndex(x => x.IsShared);
        b.HasIndex(x => x.CurrentPublishedVersionId);
        b.HasIndex(x => x.DraftVersionId);
    }
}
