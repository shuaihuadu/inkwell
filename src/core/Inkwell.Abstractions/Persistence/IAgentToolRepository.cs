// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary><see cref="AgentToolDefinition"/> 具名 Repository（只读目录 + Seed 写入，无 Update/Delete）。</summary>
public interface IAgentToolRepository
{
    /// <summary>新增工具。</summary>
    /// <param name="tool">待新增的工具。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>已新增的工具。</returns>
    Task<AgentToolDefinition> AddTool(AgentToolDefinition tool, CancellationToken ct = default);

    /// <summary>获取指定工具。</summary>
    /// <param name="id">工具标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>工具定义。</returns>
    Task<AgentToolDefinition> GetTool(Guid id, CancellationToken ct = default);

    /// <summary>按名称获取工具。</summary>
    /// <param name="name">工具名称。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>工具定义。</returns>
    Task<AgentToolDefinition> GetToolByName(string name, CancellationToken ct = default);

    /// <summary>分页获取工具。</summary>
    /// <param name="pagination">分页参数。</param>
    /// <param name="sort">排序条件。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>工具分页结果。</returns>
    Task<PagedResult<AgentToolDefinition>> ListTools(Pagination pagination, SortOrder sort, CancellationToken ct = default);
}
