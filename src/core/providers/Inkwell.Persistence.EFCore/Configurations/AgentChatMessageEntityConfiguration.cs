// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inkwell.Persistence.EFCore.Configurations;

internal sealed class AgentChatMessageEntityConfiguration : IEntityTypeConfiguration<AgentChatMessageEntity>
{
    public void Configure(EntityTypeBuilder<AgentChatMessageEntity> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.SessionId, x.SequenceNumber }).IsUnique();
        b.Property(x => x.Message).IsRequired();
    }
}
