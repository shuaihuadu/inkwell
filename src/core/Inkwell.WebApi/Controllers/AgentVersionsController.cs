// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.WebApi.Agents;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 提供 Agent 发布、历史版本与回滚 API。
/// </summary>
[Route("api/agents/{agentId:guid}")]
[Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
public sealed class AgentVersionsController(IAgentVersionService versionService) : InkwellControllerBase
{
    /// <summary>获取 Agent 的版本列表。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Agent 版本列表。</returns>
    [HttpGet("versions")]
    [ProducesResponseType<IReadOnlyList<AgentVersion>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AgentVersion>>> ListAsync(Guid agentId, CancellationToken cancellationToken)
    {
        IReadOnlyList<AgentVersion> versions = await versionService.ListVersionsAsync(agentId, this.GetRequiredUserId(), cancellationToken).ConfigureAwait(false);

        return this.Ok(versions);
    }

    /// <summary>获取 Agent 的指定版本。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="versionId">版本标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Agent 版本。</returns>
    [HttpGet("versions/{versionId:guid}")]
    [ProducesResponseType<AgentVersion>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AgentVersion>> GetAsync(Guid agentId, Guid versionId, CancellationToken cancellationToken)
    {
        AgentVersion version = await versionService.GetVersionAsync(agentId, versionId, this.GetRequiredUserId(), cancellationToken).ConfigureAwait(false);

        return this.Ok(version);
    }

    /// <summary>发布 Agent 当前定义。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>发布后的 Agent 版本。</returns>
    [HttpPost("publish")]
    [ProducesResponseType<AgentVersion>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AgentVersion>> PublishAsync(Guid agentId, CancellationToken cancellationToken)
    {
        AgentVersion version = await versionService.PublishAsync(agentId, this.GetRequiredUserId(), cancellationToken).ConfigureAwait(false);

        return this.Ok(version);
    }

    /// <summary>从指定历史版本回滚 Agent。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="versionId">回滚来源版本标识。</param>
    /// <param name="request">回滚请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新生成的发布版本。</returns>
    [HttpPost("versions/{versionId:guid}/rollback")]
    [ProducesResponseType<AgentVersion>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AgentVersion>> RollbackAsync(
        Guid agentId,
        Guid versionId,
        RollbackAgentVersionRequest request,
        CancellationToken cancellationToken)
    {
        AgentVersion version = await versionService.RollbackAsync(
            agentId,
            versionId,
            this.GetRequiredUserId(),
            request.ChangeSummary,
            cancellationToken).ConfigureAwait(false);

        return this.Ok(version);
    }
}