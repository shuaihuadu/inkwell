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
        b.HasIndex(x => x.AgentVersionId);
        b.Property(x => x.Title).HasMaxLength(30);
        b.Property(x => x.MafSessionStateJson);
        b.HasOne<AgentVersionEntity>()
            .WithMany()
            .HasForeignKey(x => x.AgentVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Messages)
            .WithOne(x => x.Conversation)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
