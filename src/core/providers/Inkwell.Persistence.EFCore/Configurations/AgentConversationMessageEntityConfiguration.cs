// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inkwell.Persistence.EFCore.Configurations;

internal sealed class AgentConversationMessageEntityConfiguration : IEntityTypeConfiguration<AgentConversationMessageEntity>
{
    public void Configure(EntityTypeBuilder<AgentConversationMessageEntity> b)
    {
        b.ToTable("conversation_messages");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.ConversationId, x.SequenceNumber }).IsUnique();
        b.Property(x => x.MessageJson).IsRequired();
    }
}
