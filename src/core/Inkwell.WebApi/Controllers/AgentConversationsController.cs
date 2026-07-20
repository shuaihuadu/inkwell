// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.WebApi.Conversations;

namespace Inkwell.WebApi.Controllers;

/// <summary>提供产品会话创建、查询和删除 API。</summary>
/// <param name="conversationService">产品会话业务服务。</param>
[Route("api/agents/{agentId:guid}/conversations")]
[Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
public sealed class AgentConversationsController(IAgentConversationService conversationService) : InkwellControllerBase
{
    private const string GetMessagesRouteName = "AgentConversations.GetMessages";

    /// <summary>创建并锁定当前发布版本的产品会话。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>创建的会话。</returns>
    [HttpPost]
    [ProducesResponseType<AgentConversationResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AgentConversationResponse>> CreateAsync(Guid agentId, CancellationToken cancellationToken)
    {
        AgentConversation conversation = await conversationService.CreateConversationAsync(agentId, this.GetRequiredUserId(), cancellationToken).ConfigureAwait(false);
        AgentConversationResponse response = ToResponse(conversation);

        return this.CreatedAtRoute(GetMessagesRouteName, new { agentId, conversationId = response.Id }, response);
    }

    /// <summary>分页列出当前参与用户在指定 Agent 下的会话。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="page">从 1 开始的页码。</param>
    /// <param name="pageSize">每页条数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>会话分页结果。</returns>
    [HttpGet]
    [ProducesResponseType<PagedResult<AgentConversationListItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AgentConversationListItem>>> ListAsync(
        Guid agentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        PagedResult<AgentConversationListItem> result = await conversationService.ListConversationsAsync(
            agentId,
            this.GetRequiredUserId(),
            new Pagination(page, pageSize),
            cancellationToken).ConfigureAwait(false);

        return this.Ok(result);
    }

    /// <summary>分页获取指定产品会话的消息。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="page">从 1 开始的页码。</param>
    /// <param name="pageSize">每页条数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>消息分页结果。</returns>
    [HttpGet("{conversationId:guid}/messages", Name = GetMessagesRouteName)]
    [ProducesResponseType<PagedResponse<AgentChatMessageResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AgentChatMessageResponse>>> GetMessagesAsync(
        Guid agentId,
        Guid conversationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        PagedResult<AgentChatMessage> messages = await conversationService.GetMessagesAsync(
            this.GetRequiredUserId(),
            agentId,
            conversationId,
            new Pagination(page, pageSize),
            cancellationToken).ConfigureAwait(false);

        return this.Ok(ToPagedResponse(messages, ToMessageResponse));
    }

    /// <summary>删除指定产品会话内的单条消息。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="messageId">消息标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>无响应正文。</returns>
    [HttpDelete("{conversationId:guid}/messages/{messageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMessageAsync(Guid agentId, Guid conversationId, Guid messageId, CancellationToken cancellationToken)
    {
        await conversationService.DeleteMessageAsync(this.GetRequiredUserId(), agentId, conversationId, messageId, cancellationToken).ConfigureAwait(false);
        return this.NoContent();
    }

    /// <summary>清空指定产品会话的消息和检查点。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>无响应正文。</returns>
    [HttpPost("{conversationId:guid}/clear")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearAsync(Guid agentId, Guid conversationId, CancellationToken cancellationToken)
    {
        await conversationService.ClearConversationAsync(this.GetRequiredUserId(), agentId, conversationId, cancellationToken).ConfigureAwait(false);
        return this.NoContent();
    }

    /// <summary>删除指定产品会话。</summary>
    /// <param name="agentId">Agent 标识。</param>
    /// <param name="conversationId">会话标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>无响应正文。</returns>
    [HttpDelete("{conversationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid agentId, Guid conversationId, CancellationToken cancellationToken)
    {
        await conversationService.DeleteConversationAsync(this.GetRequiredUserId(), agentId, conversationId, cancellationToken).ConfigureAwait(false);
        return this.NoContent();
    }

    private static AgentConversationResponse ToResponse(AgentConversation conversation) => new()
    {
        Id = conversation.Id,
        AgentId = conversation.AgentId,
        AgentVersionId = conversation.AgentVersionId,
        Title = conversation.Title,
        LastActivityTime = conversation.LastActivityTime,
        CreatedTime = conversation.CreatedTime,
        UpdatedTime = conversation.UpdatedTime,
    };

    private static AgentChatMessageResponse ToMessageResponse(AgentChatMessage message) => new()
    {
        Id = message.Id,
        Message = message.Message,
        SequenceNumber = message.SequenceNumber,
        CreatedTime = message.CreatedTime,
        UpdatedTime = message.UpdatedTime,
    };

    private static PagedResponse<TResponse> ToPagedResponse<TModel, TResponse>(PagedResult<TModel> result, Func<TModel, TResponse> map) => new()
    {
        Items = [.. result.Items.Select(map)],
        TotalCount = result.TotalCount,
        Page = result.Pagination.Page,
        PageSize = result.Pagination.PageSize,
    };
}
