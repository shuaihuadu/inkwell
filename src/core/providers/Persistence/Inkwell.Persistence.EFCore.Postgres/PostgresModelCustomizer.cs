// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Entities;

namespace Inkwell.Persistence.EFCore.Postgres;

internal sealed class PostgresModelCustomizer(ModelCustomizerDependencies dependencies) : ModelCustomizer(dependencies)
{
    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        modelBuilder.Entity<AgentChatMessageEntity>().Property(message => message.Message).HasColumnType("jsonb");
        modelBuilder.Entity<AgentSessionStateEntity>().Property(state => state.SerializedState).HasColumnType("jsonb");
        modelBuilder.Entity<AgentSkillEntity>().Property(skill => skill.ReferenceFileUris).HasColumnType("jsonb");
        modelBuilder.Entity<AgentSkillEntity>().Property(skill => skill.AssetFileUris).HasColumnType("jsonb");
        modelBuilder.Entity<AgentToolEntity>().Property(tool => tool.ParametersJsonSchema).HasColumnType("jsonb");
        modelBuilder.Entity<AgentEntity>().Property(agent => agent.BuildOptions).HasColumnType("jsonb");
        modelBuilder.Entity<AgentVersionEntity>().Property(version => version.Snapshot).HasColumnType("jsonb");
    }
}
