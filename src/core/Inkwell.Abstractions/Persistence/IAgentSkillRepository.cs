// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary><see cref="AgentSkillDefinition"/> 具名 Repository（只读目录 + 上传写入，无 Update/Delete）。</summary>
public interface IAgentSkillRepository
{
    /// <summary>新增 Skill。</summary>
    /// <param name="skill">待新增的 Skill。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>已新增的 Skill。</returns>
    Task<AgentSkillDefinition> AddSkill(AgentSkillDefinition skill, CancellationToken ct = default);

    /// <summary>获取指定 Skill。</summary>
    /// <param name="id">Skill 标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>Skill 定义。</returns>
    Task<AgentSkillDefinition> GetSkill(Guid id, CancellationToken ct = default);

    /// <summary>分页获取 Skill。</summary>
    /// <param name="pagination">分页参数。</param>
    /// <param name="sort">排序条件。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>Skill 分页结果。</returns>
    Task<PagedResult<AgentSkillDefinition>> ListSkills(Pagination pagination, SortOrder sort, CancellationToken ct = default);
}
