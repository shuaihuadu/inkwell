// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inkwell.Persistence.EFCore.Configurations;

internal sealed class AgentSessionStateEntityConfiguration : IEntityTypeConfiguration<AgentSessionStateEntity>
{
    public void Configure(EntityTypeBuilder<AgentSessionStateEntity> builder)
    {
        builder.HasKey(state => state.ConversationId);
        builder.Property(state => state.SerializedState).IsRequired();
        builder.Property(state => state.LastRunId).HasMaxLength(64);
    }
}