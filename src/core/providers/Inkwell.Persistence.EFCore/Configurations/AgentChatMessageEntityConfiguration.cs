// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inkwell.Persistence.EFCore.Configurations;

internal sealed class AgentChatMessageEntityConfiguration : IEntityTypeConfiguration<AgentChatMessageEntity>
{
    public void Configure(EntityTypeBuilder<AgentChatMessageEntity> builder)
    {
        builder.HasKey(message => message.Id);
        builder.HasIndex(message => new { message.ConversationId, message.SequenceNumber }).IsUnique();
        builder.Property(message => message.RunId).HasMaxLength(64);
        builder.Property(message => message.Message).IsRequired();
    }
}
