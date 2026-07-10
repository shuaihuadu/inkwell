
namespace Inkwell;

/// <summary>工具目录只读查询 + 绑定参数必填校验业务对外接口。</summary>
public interface IAgentToolCatalogService
{
    Task<IReadOnlyList<AgentToolDefinition>> ListAvailableToolsAsync(CancellationToken ct = default);

    Task<AgentToolDefinition> GetToolAsync(Guid toolId, CancellationToken ct = default);

    Task ValidateToolBindingAsync(Guid toolId, string? parametersJson, CancellationToken ct = default);
}
