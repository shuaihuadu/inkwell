// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary><see cref="IAgentSkillContentResolver"/> 唯一实现；缺失 SkillId 采用尽力而为、不中断策略。</summary>
internal sealed class AgentSkillContentResolver(IPersistenceProvider persistence) : IAgentSkillContentResolver
{
    private readonly IAgentSkillRepository _skills = persistence.GetRepository<IAgentSkillRepository>();

    public async Task<AgentSkillResolutionResult> ResolveAsync(IReadOnlyList<AgentSkillBinding> bindings, CancellationToken ct = default)
    {
        if (bindings.Count == 0)
        {
            return new AgentSkillResolutionResult([], []);
        }

        List<AgentSkillContent> resolved = [];
        List<Guid> missing = [];

        foreach (AgentSkillBinding binding in bindings)
        {
            try
            {
                AgentSkillDefinition skill = await this._skills.GetSkill(binding.SkillId, ct).ConfigureAwait(false);

                resolved.Add(ToSkillContent(skill));
            }
            catch (KeyNotFoundException)
            {
                missing.Add(binding.SkillId);
            }
        }

        return new AgentSkillResolutionResult(resolved, missing);
    }

    private static AgentSkillContent ToSkillContent(AgentSkillDefinition skill) =>
        new(
            skill.Id,
            skill.Name,
            skill.Description,
            skill.Content,
            skill.ReferenceFileUris,
            skill.AssetFileUris,
            skill.ScriptFileUris);
}
