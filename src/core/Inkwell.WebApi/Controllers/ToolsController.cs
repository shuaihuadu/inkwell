// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 提供系统注册工具的只读目录 API。
/// </summary>
[Route("api/tools")]
[Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
public sealed class ToolsController(IAgentToolCatalogService toolCatalogService) : InkwellControllerBase
{
    /// <summary>
    /// 获取全部可用工具。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>系统注册的工具目录。</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AgentToolDefinition>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AgentToolDefinition>>> ListAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AgentToolDefinition> tools = await toolCatalogService
            .ListAvailableToolsAsync(cancellationToken)
            .ConfigureAwait(false);

        return this.Ok(tools);
    }
}