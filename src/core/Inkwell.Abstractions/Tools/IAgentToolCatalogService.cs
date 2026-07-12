// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>工具目录只读查询 + 绑定参数必填校验业务对外接口。</summary>
public interface IAgentToolCatalogService
{
    /// <summary>获取可用工具列表。</summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>可用工具列表。</returns>
    Task<IReadOnlyList<AgentToolDefinition>> ListAvailableToolsAsync(CancellationToken ct = default);

    /// <summary>获取指定工具。</summary>
    /// <param name="toolId">工具标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>工具定义。</returns>
    Task<AgentToolDefinition> GetToolAsync(Guid toolId, CancellationToken ct = default);

    /// <summary>验证工具绑定参数。</summary>
    /// <param name="toolId">工具标识。</param>
    /// <param name="parametersJson">工具绑定参数 JSON。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task ValidateToolBindingAsync(Guid toolId, string? parametersJson, CancellationToken ct = default);
}
