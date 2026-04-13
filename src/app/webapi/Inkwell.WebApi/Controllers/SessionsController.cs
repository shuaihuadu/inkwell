using Inkwell.Agents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 会话管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "EditorOrAdmin")]
public sealed class SessionsController(
    ISessionPersistenceProvider sessionProvider,
    ILogger<SessionsController> logger) : ControllerBase
{
    /// <summary>
    /// 获取指定 Agent 的会话列表
    /// </summary>
    /// <param name="agentId">Agent ID（可选，不传则返回所有）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>会话信息列表</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSessionsAsync(
        [FromQuery] string? agentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return this.Ok(Array.Empty<SessionInfo>());
        }

        IReadOnlyList<SessionInfo> sessions = await sessionProvider
            .ListSessionInfosAsync(agentId, cancellationToken)
            .ConfigureAwait(false);

        return this.Ok(sessions);
    }

    /// <summary>
    /// 获取会话详情
    /// </summary>
    /// <param name="threadId">会话 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>会话信息</returns>
    [HttpGet("{threadId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessionAsync(
        string threadId,
        CancellationToken cancellationToken)
    {
        SessionInfo? info = await sessionProvider
            .GetSessionInfoAsync(threadId, cancellationToken)
            .ConfigureAwait(false);

        if (info is null)
        {
            return this.NotFound();
        }

        return this.Ok(info);
    }

    /// <summary>
    /// 获取指定会话的消息列表
    /// </summary>
    /// <param name="threadId">会话 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>消息列表</returns>
    [HttpGet("{threadId}/messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessagesAsync(
        string threadId,
        CancellationToken cancellationToken)
    {
        SessionInfo? info = await sessionProvider
            .GetSessionInfoAsync(threadId, cancellationToken)
            .ConfigureAwait(false);

        if (info is null)
        {
            return this.NotFound();
        }

        IReadOnlyList<ChatMessageRecord> messages = await sessionProvider
            .GetMessagesAsync(threadId, cancellationToken)
            .ConfigureAwait(false);

        return this.Ok(messages);
    }

    /// <summary>
    /// 创建新会话
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新会话信息</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSessionAsync(
        [FromBody] CreateSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AgentId))
        {
            return this.BadRequest("agentId is required.");
        }

        string sessionId = Guid.NewGuid().ToString();

        // 保存一个空的 session 占位
        await sessionProvider.SaveSessionAsync(
            sessionId,
            request.AgentId,
            System.Text.Json.JsonSerializer.SerializeToElement(new { }),
            cancellationToken).ConfigureAwait(false);

        SessionInfo? info = await sessionProvider
            .GetSessionInfoAsync(sessionId, cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation("[Sessions] Created session {SessionId} for agent {AgentId}", sessionId, request.AgentId);

        return this.Created($"/api/sessions/{sessionId}", info);
    }

    /// <summary>
    /// 更新会话标题
    /// </summary>
    /// <param name="threadId">会话 ID</param>
    /// <param name="request">更新请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的会话信息</returns>
    [HttpPatch("{threadId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSessionTitleAsync(
        string threadId,
        [FromBody] UpdateSessionTitleRequest request,
        CancellationToken cancellationToken)
    {
        SessionInfo? existing = await sessionProvider
            .GetSessionInfoAsync(threadId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            return this.NotFound();
        }

        await sessionProvider.UpdateSessionTitleAsync(threadId, request.Title, cancellationToken).ConfigureAwait(false);

        SessionInfo? updated = await sessionProvider
            .GetSessionInfoAsync(threadId, cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation("[Sessions] Updated title for session {SessionId}", threadId);

        return this.Ok(updated);
    }

    /// <summary>
    /// 删除会话
    /// </summary>
    /// <param name="threadId">会话 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>无内容</returns>
    [HttpDelete("{threadId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSessionAsync(
        string threadId,
        CancellationToken cancellationToken)
    {
        SessionInfo? existing = await sessionProvider
            .GetSessionInfoAsync(threadId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            return this.NotFound();
        }

        await sessionProvider.DeleteSessionAsync(threadId, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("[Sessions] Deleted session {SessionId}", threadId);

        return this.NoContent();
    }
}

/// <summary>
/// 创建会话请求
/// </summary>
public sealed class CreateSessionRequest
{
    /// <summary>
    /// 获取或设置 Agent ID
    /// </summary>
    public string AgentId { get; set; } = string.Empty;
}

/// <summary>
/// 更新会话标题请求
/// </summary>
public sealed class UpdateSessionTitleRequest
{
    /// <summary>
    /// 获取或设置新标题
    /// </summary>
    public string Title { get; set; } = string.Empty;
}
