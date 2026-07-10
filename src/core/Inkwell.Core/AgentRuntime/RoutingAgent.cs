using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Inkwell;

/// <summary>
/// 多协议路由壳：自身是一个合法的 <see cref="AIAgent"/>，可作为静态实例喂给 MAF 官方
/// <c>MapAGUI</c>（未来可扩展 <c>MapOpenAIChatCompletions</c>/<c>MapOpenAIResponses</c>/<c>MapA2AHttpJson</c>），
/// 复用官方各协议的完整托管管线；内部按当前 HTTP 请求的路由值动态解析 <c>agentId</c>/<c>conversationId</c>，
/// 从已鉴权的 <see cref="HttpContext.User"/> 取 <c>callerUserId</c>，委托 <see cref="IAgentInvocationService"/> 执行。
/// </summary>
/// <remarks>
/// <para>
/// MAF 各协议官方 Map 扩展都是"一个路由绑一个在注册时确定的静态 <see cref="AIAgent"/>"，没有提供按请求
/// 动态换 Agent 的官方钩子。本类把"动态解析"做成 <see cref="AIAgent"/> 自身的实现细节，从而可以被当作
/// 那个静态实例使用。
/// </para>
/// <para>
/// <c>conversationId</c> 走 URL 路由值（<see cref="ConversationIdRouteKey"/>），不依赖 MAF AG-UI 的
/// <c>ThreadId</c>/<see cref="Microsoft.Agents.AI.Hosting.AgentSessionStore"/> 机制——这条链路目前没有注册自定义
/// <c>AgentSessionStore</c>，会话历史的实际读写由更内层的 <see cref="DatabaseChatHistoryProvider"/> 按
/// <c>ConversationId</c> 完成，本类返回的 <see cref="RoutingAgentSession"/> 只是满足 <see cref="AIAgent"/>
/// 契约的占位对象。
/// </para>
/// <para>
/// <strong>生命周期</strong>：本类以 Singleton 注册（<c>MapAGUI</c> 在端点注册时就会从根 <c>IServiceProvider</c>
/// 解析一次并长期复用，不是每请求重建），因此不能构造函数注入 Scoped 的
/// <see cref="IAgentInvocationService"/>（会形成 captive dependency）；改为每次调用时从
/// <see cref="HttpContext.RequestServices"/>（当前请求自己的 Scope）现取，与 <see cref="IHttpContextAccessor"/>
/// 本身安全被 Singleton 构造函数注入不同，不会产生同样的问题。
/// </para>
/// </remarks>
internal sealed class RoutingAgent(IHttpContextAccessor httpContextAccessor) : AIAgent
{
    /// <summary>本 Agent 的固定名称，供 DI 按名注册 / 日志识别。</summary>
    public const string AgentName = "RoutingAgent";

    /// <summary>URL 路由值中承载目标 <c>AgentId</c> 的键名。</summary>
    public const string AgentIdRouteKey = "agentId";

    /// <summary>URL 路由值中承载 <c>ConversationId</c> 的键名；缺省表示新对话。</summary>
    public const string ConversationIdRouteKey = "conversationId";

    /// <inheritdoc />
    public override string? Name => AgentName;

    /// <inheritdoc />
    public override string? Description => "Routes AG-UI (and future OpenAI ChatCompletions/Responses/A2A) requests to the Inkwell agent identified by the request route.";

    /// <inheritdoc />
    protected override async Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        HttpContext httpContext = ResolveHttpContext(httpContextAccessor);
        (Guid agentId, Guid callerUserId, Guid? conversationId) = ResolveRequestContext(httpContext);
        IAgentInvocationService agentInvocationService = httpContext.RequestServices.GetRequiredService<IAgentInvocationService>();
        IReadOnlyList<AgentChatMessage> agentChatMessages = [.. messages.Select(AgentChatMessageMapper.ToAgentChatMessage)];

        AgentTurnResult result = await agentInvocationService
            .RunAsync(agentId, callerUserId, conversationId, agentChatMessages, cancellationToken)
            .ConfigureAwait(false);

        return AgentResponseMapper.ToAgentResponse(result);
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        HttpContext httpContext = ResolveHttpContext(httpContextAccessor);
        (Guid agentId, Guid callerUserId, Guid? conversationId) = ResolveRequestContext(httpContext);
        IAgentInvocationService agentInvocationService = httpContext.RequestServices.GetRequiredService<IAgentInvocationService>();
        IReadOnlyList<AgentChatMessage> agentChatMessages = [.. messages.Select(AgentChatMessageMapper.ToAgentChatMessage)];

        IAsyncEnumerable<AgentRunEvent> events = agentInvocationService.RunStreamingAsync(agentId, callerUserId, conversationId, agentChatMessages, cancellationToken);

        await foreach (AgentResponseUpdate update in AgentResponseMapper.ToAgentResponseUpdatesAsync(events).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return update;
        }
    }

    /// <inheritdoc />
    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<AgentSession>(new RoutingAgentSession());

    /// <inheritdoc />
    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
        JsonElement serializedState,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<AgentSession>(new RoutingAgentSession());

    /// <inheritdoc />
    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
        AgentSession session,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(JsonSerializer.SerializeToElement(new { }));

    private static (Guid AgentId, Guid CallerUserId, Guid? ConversationId) ResolveRequestContext(HttpContext httpContext)
    {
        Guid agentId = ResolveRouteGuid(httpContext, AgentIdRouteKey)
            ?? throw new InvalidOperationException($"Route value '{AgentIdRouteKey}' is missing or not a valid GUID.");

        Guid? conversationId = ResolveRouteGuid(httpContext, ConversationIdRouteKey);
        Guid callerUserId = ResolveCallerUserId(httpContext);

        return (agentId, callerUserId, conversationId);
    }

    private static HttpContext ResolveHttpContext(IHttpContextAccessor httpContextAccessor) =>
        httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("No HttpContext available; RoutingAgent must be invoked within an HTTP request.");

    private static Guid? ResolveRouteGuid(HttpContext httpContext, string routeKey)
    {
        string? rawValue = httpContext.GetRouteValue(routeKey)?.ToString();

        return Guid.TryParse(rawValue, out Guid value) ? value : null;
    }

    private static Guid ResolveCallerUserId(HttpContext httpContext)
    {
        string? rawUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(rawUserId, out Guid userId)
            ? userId
            : throw new UnauthorizedAccessException("Caller identity is missing or invalid.");
    }
}
