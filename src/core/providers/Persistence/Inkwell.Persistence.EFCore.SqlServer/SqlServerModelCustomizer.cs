// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Entities;

namespace Inkwell.Persistence.EFCore.SqlServer;

internal sealed class SqlServerModelCustomizer(ModelCustomizerDependencies dependencies) : ModelCustomizer(dependencies)
{
    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        modelBuilder.Entity<AgentChatMessageEntity>().Property(message => message.Message).HasColumnType("json");
        modelBuilder.Entity<AgentSessionStateEntity>().Property(state => state.SerializedState).HasColumnType("json");
        modelBuilder.Entity<AgentSkillEntity>().Property(skill => skill.ReferenceFileUris).HasColumnType("json");
        modelBuilder.Entity<AgentSkillEntity>().Property(skill => skill.AssetFileUris).HasColumnType("json");
        modelBuilder.Entity<AgentToolEntity>().Property(tool => tool.ParametersJsonSchema).HasColumnType("json");
        modelBuilder.Entity<AgentEntity>().Property(agent => agent.BuildOptions).HasColumnType("json");
        modelBuilder.Entity<AgentVersionEntity>().Property(version => version.Snapshot).HasColumnType("json");
    }
}
