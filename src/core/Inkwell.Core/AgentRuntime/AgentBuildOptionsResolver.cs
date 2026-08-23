// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Collections.Immutable;

namespace Inkwell;

/// <inheritdoc />
internal sealed class AgentBuildOptionsResolver(
    IPersistenceProvider persistence,
    IAgentToolCatalogService toolCatalogService) : IAgentBuildOptionsResolver
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

        IReadOnlyList<AgentToolBinding> toolBindings = request.ToolBindings ?? [];
        HashSet<Guid> toolIds = [];
        foreach (AgentToolBinding binding in toolBindings)
        {
            if (!toolIds.Add(binding.ToolId))
            {
                throw new ArgumentException($"Tool '{binding.ToolId}' cannot be bound more than once.", nameof(request));
            }

            await toolCatalogService.ValidateToolBindingAsync(binding.ToolId, binding.ParametersJson, cancellationToken).ConfigureAwait(false);
        }

        return new AgentBuildOptions
        {
            ModelOptions = request.ModelOptions,
            ChatHistoryOptions = request.ChatHistoryOptions,
            ToolBindings = [.. toolBindings],
            Skills = skills.ToImmutable(),
        };
    }
}