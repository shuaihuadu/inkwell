// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 提供 Agent CRUD、列表、共享和克隆 API。
/// </summary>
[Route("api/agents")]
[Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
public sealed class AgentsController(IAgentService agentService) : InkwellControllerBase
{
    /// <summary>创建 Agent。</summary>
    /// <param name="request">Agent 创建请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>创建的 Agent。</returns>
    [HttpPost]
    [ProducesResponseType<AgentDefinition>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AgentDefinition>> CreateAsync(AgentUpsertRequest request, CancellationToken cancellationToken)
    {
        AgentDefinition agent = await agentService.CreateAgentAsync(request, this.GetRequiredUserId(), cancellationToken).ConfigureAwait(false);

        return this.CreatedAtAction(nameof(GetAsync), new { agentId = agent.Id }, agent);
    }

    /// <summary>获取当前用户拥有的 Agent。</summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Agent 摘要列表。</returns>
    [HttpGet("mine")]
    [ProducesResponseType<IReadOnlyList<AgentListItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AgentListItem>>> ListMineAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<AgentListItem> agents = await agentService.ListMyAgentsAsync(this.GetRequiredUserId(), cancellationToken).ConfigureAwait(false);

        return this.Ok(agents);
    }

    /// <summary>获取其他用户共享的 Agent。</summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Agent 摘要列表。</returns>
    [HttpGet("shared")]
    [ProducesResponseType<IReadOnlyList<AgentListItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AgentListItem>>> ListSharedAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<AgentListItem> agents = await agentService.ListSharedAgentsAsync(this.GetRequiredUserId(), cancellationToken).ConfigureAwait(false);

        return this.Ok(agents);
    }

    /// <summary>获取指定 Agent。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Agent 定义。</returns>
    [HttpGet("{agentId:guid}")]
    [ProducesResponseType<AgentDefinition>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AgentDefinition>> GetAsync(Guid agentId, CancellationToken cancellationToken)
    {
        AgentDefinition agent = await agentService.GetAgentAsync(agentId, this.GetRequiredUserId(), cancellationToken).ConfigureAwait(false);

        return this.Ok(agent);
    }

    /// <summary>更新指定 Agent。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="request">Agent 更新请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>更新后的 Agent。</returns>
    [HttpPut("{agentId:guid}")]
    [ProducesResponseType<AgentDefinition>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AgentDefinition>> UpdateAsync(Guid agentId, AgentUpsertRequest request, CancellationToken cancellationToken)
    {
        AgentDefinition agent = await agentService.UpdateAgentAsync(agentId, request, this.GetRequiredUserId(), cancellationToken).ConfigureAwait(false);

        return this.Ok(agent);
    }

    /// <summary>删除指定 Agent。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>无响应正文。</returns>
    [HttpDelete("{agentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid agentId, CancellationToken cancellationToken)
    {
        _ = await agentService.DeleteAgentAsync(agentId, this.GetRequiredUserId(), cancellationToken).ConfigureAwait(false);

        return this.NoContent();
    }

    /// <summary>共享指定 Agent。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>无响应正文。</returns>
    [HttpPost("{agentId:guid}/share")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ShareAsync(Guid agentId, CancellationToken cancellationToken)
    {
        await agentService.ShareAgentAsync(agentId, this.GetRequiredUserId(), cancellationToken).ConfigureAwait(false);

        return this.NoContent();
    }

    /// <summary>取消共享指定 Agent。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>无响应正文。</returns>
    [HttpDelete("{agentId:guid}/share")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnshareAsync(Guid agentId, CancellationToken cancellationToken)
    {
        await agentService.UnshareAgentAsync(agentId, this.GetRequiredUserId(), cancellationToken).ConfigureAwait(false);

        return this.NoContent();
    }

    /// <summary>管理员撤销指定 Agent 的团队共享。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>无响应正文。</returns>
    [HttpPost("{agentId:guid}/share/revoke")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeShareAsync(Guid agentId, CancellationToken cancellationToken)
    {
        await agentService.RevokeShareAsync(agentId, this.GetRequiredUserId(), cancellationToken).ConfigureAwait(false);

        return this.NoContent();
    }

    /// <summary>克隆指定 Agent。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>克隆后的 Agent。</returns>
    [HttpPost("{agentId:guid}/clone")]
    [ProducesResponseType<AgentDefinition>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AgentDefinition>> CloneAsync(Guid agentId, CancellationToken cancellationToken)
    {
        AgentDefinition clone = await agentService.CloneAgentAsync(agentId, this.GetRequiredUserId(), cancellationToken).ConfigureAwait(false);

        return this.CreatedAtAction(nameof(GetAsync), new { agentId = clone.Id }, clone);
    }
}