
namespace Inkwell;

/// <summary>
/// 把 <see cref="AgentSkillBinding"/> 列表解析为 <see cref="AgentSkillContent"/> 列表；对缺失 SkillId 采用尽力而为、不中断策略（EX-008）。
/// </summary>
public interface IAgentSkillContentResolver
{
    Task<AgentSkillResolutionResult> ResolveAsync(IReadOnlyList<AgentSkillBinding> bindings, CancellationToken ct = default);
}

public sealed record class AgentSkillResolutionResult(IReadOnlyList<AgentSkillContent> ResolvedSkills, IReadOnlyList<Guid> MissingSkillIds);
