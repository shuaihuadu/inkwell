// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>Skill 目录只读查询 + 上传注册业务对外接口。</summary>
public interface IAgentSkillCatalogService
{
    Task<IReadOnlyList<AgentSkillDefinition>> ListAvailableSkillsAsync(CancellationToken ct = default);

    Task<AgentSkillDefinition> GetSkillAsync(Guid skillId, CancellationToken ct = default);

    Task<AgentSkillDefinition> UploadSkillAsync(AgentSkillUploadRequest request, CancellationToken ct = default);
}
