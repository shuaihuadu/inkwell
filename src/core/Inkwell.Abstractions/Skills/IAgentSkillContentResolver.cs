// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 把 <see cref="AgentSkillBinding"/> 列表解析为 <see cref="AgentSkillContent"/> 列表；对缺失 SkillId 采用尽力而为、不中断策略（EX-008）。
/// </summary>
public interface IAgentSkillContentResolver
{
    /// <summary>解析 Agent 绑定的 Skill 内容。</summary>
    /// <param name="bindings">Skill 绑定列表。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>包含已解析内容和缺失标识的解析结果。</returns>
    Task<AgentSkillResolutionResult> ResolveAsync(IReadOnlyList<AgentSkillBinding> bindings, CancellationToken ct = default);
}

/// <summary>
/// 表示 Agent Skill 内容解析结果。
/// </summary>
/// <param name="ResolvedSkills">已成功解析的 Skill 内容。</param>
/// <param name="MissingSkillIds">未找到的 Skill 标识。</param>
public sealed record class AgentSkillResolutionResult(IReadOnlyList<AgentSkillContent> ResolvedSkills, IReadOnlyList<Guid> MissingSkillIds);
