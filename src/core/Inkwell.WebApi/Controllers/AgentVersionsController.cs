// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.WebApi.Agents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 提供 Agent 草稿、发布版本、历史版本与回滚 API。
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

    /// <summary>保存 Agent 草稿。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="request">草稿保存请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>保存后的草稿版本。</returns>
    [HttpPut("draft")]
    [ProducesResponseType<AgentVersion>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AgentVersion>> SaveDraftAsync(
        Guid agentId,
        SaveAgentDraftRequest request,
        CancellationToken cancellationToken)
    {
        AgentVersion version = await versionService.SaveDraftAsync(
            agentId,
            request.Snapshot,
            this.GetRequiredUserId(),
            request.ChangeSummary,
            cancellationToken).ConfigureAwait(false);

        return this.Ok(version);
    }

    /// <summary>发布 Agent 当前草稿。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>发布后的 Agent 版本。</returns>
    [HttpPost("publish")]
    [ProducesResponseType<AgentVersion>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AgentVersion>> PublishAsync(Guid agentId, CancellationToken cancellationToken)
    {
        AgentVersion version = await versionService.PublishDraftAsync(agentId, this.GetRequiredUserId(), cancellationToken).ConfigureAwait(false);

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