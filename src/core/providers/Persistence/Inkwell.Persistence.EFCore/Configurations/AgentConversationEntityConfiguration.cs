// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inkwell.Persistence.EFCore.Configurations;

internal sealed class AgentConversationEntityConfiguration : IEntityTypeConfiguration<AgentConversationEntity>
{
    public void Configure(EntityTypeBuilder<AgentConversationEntity> builder)
    {
        builder.HasKey(conversation => conversation.Id);
        builder.HasIndex(conversation => conversation.SessionKey).IsUnique();
        builder.HasIndex(conversation => new { conversation.AgentId, conversation.OwnerUserId, conversation.LastActivityTime });
        builder.HasIndex(conversation => conversation.AgentVersionId);
        builder.Property(conversation => conversation.SessionKey).HasMaxLength(64).IsRequired();
        builder.Property(conversation => conversation.Title).HasMaxLength(30);
        builder.Property(conversation => conversation.LastCommittedRunId).HasMaxLength(64);
        builder.HasOne<AgentVersionEntity>()
            .WithMany()
            .HasForeignKey(conversation => new { conversation.AgentId, conversation.AgentVersionId })
            .HasPrincipalKey(version => new { version.AgentId, version.Id })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(conversation => conversation.Messages)
            .WithOne(message => message.Conversation)
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(conversation => conversation.SessionState)
            .WithOne(state => state.Conversation)
            .HasForeignKey<AgentSessionStateEntity>(state => state.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}