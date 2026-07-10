// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary><see cref="AgentSkillDefinition"/> 具名 Repository（只读目录 + 上传写入，无 Update/Delete）。</summary>
public interface IAgentSkillRepository
{
    Task<AgentSkillDefinition> AddSkill(AgentSkillDefinition skill, CancellationToken ct = default);

    Task<AgentSkillDefinition> GetSkill(Guid id, CancellationToken ct = default);

    Task<PagedResult<AgentSkillDefinition>> ListSkills(Pagination pagination, SortOrder sort, CancellationToken ct = default);
}
