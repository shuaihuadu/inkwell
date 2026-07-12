// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// <see cref="AgentVersion"/> 具名 Repository。
/// </summary>
public interface IAgentVersionRepository
{
    /// <summary>
    /// 添加 Agent 版本。
    /// </summary>
    /// <param name="version">要添加的版本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>持久化后的版本。</returns>
    Task<AgentVersion> AddVersionAsync(AgentVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新尚未发布的 Agent 版本或执行发布状态转换。
    /// </summary>
    /// <param name="version">要更新的版本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>持久化后的版本。</returns>
    Task<AgentVersion> UpdateVersionAsync(AgentVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定 Agent 版本。
    /// </summary>
    /// <param name="versionId">版本标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>找到的版本。</returns>
    Task<AgentVersion> GetVersionAsync(Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定 Agent 的全部版本，按版本号降序排列。
    /// </summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>版本集合。</returns>
    Task<IReadOnlyList<AgentVersion>> ListVersionsByAgentAsync(Guid agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取版本，返回值以版本标识为键。
    /// </summary>
    /// <param name="versionIds">版本标识集合。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>找到的版本字典。</returns>
    Task<IReadOnlyDictionary<Guid, AgentVersion>> FindVersionsByIdsAsync(IReadOnlyList<Guid> versionIds, CancellationToken cancellationToken = default);
}