using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Inkwell.Persistence.EFCore.Entities;

namespace Inkwell.Persistence.EFCore.Configurations;

internal sealed class AgentConversationMessageEntityConfiguration : IEntityTypeConfiguration<AgentConversationMessageEntity>
{
    public void Configure(EntityTypeBuilder<AgentConversationMessageEntity> b)
    {
        b.ToTable("conversation_messages");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.ConversationId, x.SequenceNumber }).IsUnique();
        b.Property(x => x.ContentJson).IsRequired();
        b.Property(x => x.Role).IsRequired().HasMaxLength(64);
    }
}
