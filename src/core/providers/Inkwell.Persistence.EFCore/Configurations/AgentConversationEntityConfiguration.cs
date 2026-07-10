// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inkwell.Persistence.EFCore.Configurations;

internal sealed class AgentConversationEntityConfiguration : IEntityTypeConfiguration<AgentConversationEntity>
{
    public void Configure(EntityTypeBuilder<AgentConversationEntity> b)
    {
        b.ToTable("conversations");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.AgentId, x.OwnerUserId });
        b.Property(x => x.Title).HasMaxLength(30);
        b.HasMany(x => x.Messages)
            .WithOne(x => x.Conversation)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
