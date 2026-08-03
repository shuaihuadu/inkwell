// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inkwell.Persistence.EFCore.Configurations;

internal sealed class AgentSessionStateEntityConfiguration : IEntityTypeConfiguration<AgentSessionStateEntity>
{
    public void Configure(EntityTypeBuilder<AgentSessionStateEntity> builder)
    {
        builder.HasKey(state => state.Id);
        builder.HasIndex(state => state.ConversationId).IsUnique();
        builder.Property(state => state.SessionState).IsRequired();
        builder.HasOne(state => state.Conversation)
            .WithOne()
            .HasForeignKey<AgentSessionStateEntity>(state => state.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
