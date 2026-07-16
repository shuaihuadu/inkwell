// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Collections.Immutable;

namespace Inkwell;

/// <inheritdoc />
internal sealed class AgentBuildOptionsResolver(IPersistenceProvider persistence) : IAgentBuildOptionsResolver
{
    private readonly IAgentSkillRepository _skills = persistence.GetRepository<IAgentSkillRepository>();

    /// <inheritdoc />
    public async Task<AgentBuildOptions> ResolveAsync(AgentUpsertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ImmutableArray<AgentSkillDefinition>.Builder skills = ImmutableArray.CreateBuilder<AgentSkillDefinition>();
        foreach (AgentSkillBinding binding in request.SkillBindings ?? [])
        {
            AgentSkillDefinition definition = await this._skills.GetSkill(binding.SkillId, cancellationToken).ConfigureAwait(false);
            skills.Add(definition);
        }

        return new AgentBuildOptions
        {
            ModelOptions = request.ModelOptions,
            ChatHistoryOptions = request.ChatHistoryOptions,
            ToolBindings = [.. request.ToolBindings ?? []],
            Skills = skills.ToImmutable(),
        };
    }
}