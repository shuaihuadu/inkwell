// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>Skill 目录只读查询 + 上传注册业务对外接口。</summary>
public interface IAgentSkillCatalogService
{
    /// <summary>获取可用 Skill 列表。</summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>可用 Skill 列表。</returns>
    Task<IReadOnlyList<AgentSkillDefinition>> ListAvailableSkillsAsync(CancellationToken ct = default);

    /// <summary>获取指定 Skill。</summary>
    /// <param name="skillId">Skill 标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>Skill 定义。</returns>
    Task<AgentSkillDefinition> GetSkillAsync(Guid skillId, CancellationToken ct = default);

    /// <summary>上传并注册 Skill。</summary>
    /// <param name="request">Skill 上传请求。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>已注册的 Skill 定义。</returns>
    Task<AgentSkillDefinition> UploadSkillAsync(AgentSkillUploadRequest request, CancellationToken ct = default);
}
