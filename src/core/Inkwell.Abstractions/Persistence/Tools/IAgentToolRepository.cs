
namespace Inkwell;

/// <summary><see cref="AgentToolDefinition"/> 具名 Repository（只读目录 + Seed 写入，无 Update/Delete）。</summary>
public interface IAgentToolRepository
{
    Task<AgentToolDefinition> AddTool(AgentToolDefinition tool, CancellationToken ct = default);

    Task<AgentToolDefinition> GetTool(Guid id, CancellationToken ct = default);

    Task<AgentToolDefinition> GetToolByName(string name, CancellationToken ct = default);

    Task<PagedResult<AgentToolDefinition>> ListTools(Pagination pagination, SortOrder sort, CancellationToken ct = default);
}
