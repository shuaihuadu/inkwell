// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using AGUI.Abstractions;
using AGUI.Server;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Inkwell.WebApi.Protocols;

internal sealed class RoutingAgent(
    IHttpContextAccessor httpContextAccessor,
    IServiceScopeFactory scopeFactory) : AIAgent
{
    private const string RunModeHeaderName = "X-Inkwell-Agent-Run-Mode";
    private const string ConversationIdHeaderName = "X-Inkwell-Conversation-Id";
    private const int MaxProtocolRunIdLength = 64;

    public override string? Name => "inkwell-routed-agent";

    public override string? Description => "Resolves the published or owner draft Inkwell agent selected by the current route.";

    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<AgentSession>(new StatelessAgentSession());

    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
        AgentSession session,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(JsonSerializer.SerializeToElement(new { }, jsonSerializerOptions));

    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
        JsonElement serializedState,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<AgentSession>(new StatelessAgentSession());

    protected override async Task<Microsoft.Agents.AI.AgentResponse> RunCoreAsync(
        IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
        Microsoft.Agents.AI.AgentSession? session = null,
        Microsoft.Agents.AI.AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // RoutingAgent 由 Hosting 以 Singleton 持有；每次 Run 单独创建 Scope，避免跨请求复用构建服务及其持久化依赖。
        using IServiceScope scope = scopeFactory.CreateScope();
        RecordProtocolRunTags(options);
        IReadOnlyList<Microsoft.Extensions.AI.ChatMessage> requestMessages = messages as IReadOnlyList<Microsoft.Extensions.AI.ChatMessage> ?? messages.ToList();
        if (this.TryGetPublishedConversationId(options, out Guid conversationId))
        {
            IAgentConversationService conversationService = scope.ServiceProvider.GetRequiredService<IAgentConversationService>();
            return await conversationService
                .RunAsync(
                    this.GetRequiredUserId(),
                    this.GetRouteAgentId(),
                    conversationId,
                    requestMessages,
                    options,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        IAgentBuildService buildService = scope.ServiceProvider.GetRequiredService<IAgentBuildService>();
        AIAgent agent = await this
            .BuildAgentAsync(buildService, cancellationToken)
            .ConfigureAwait(false);

        return await agent.RunAsync(requestMessages, null, options, cancellationToken).ConfigureAwait(false);
    }

    protected override async IAsyncEnumerable<Microsoft.Agents.AI.AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
        Microsoft.Agents.AI.AgentSession? session = null,
        Microsoft.Agents.AI.AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Scope 必须覆盖整个流式枚举，确保构建服务及其持久化依赖在响应结束前保持有效。
        using IServiceScope scope = scopeFactory.CreateScope();
        RecordProtocolRunTags(options);
        IReadOnlyList<Microsoft.Extensions.AI.ChatMessage> requestMessages = messages as IReadOnlyList<Microsoft.Extensions.AI.ChatMessage> ?? messages.ToList();
        if (this.TryGetPublishedConversationId(options, out Guid conversationId))
        {
            IAgentConversationService conversationService = scope.ServiceProvider.GetRequiredService<IAgentConversationService>();
            await foreach (Microsoft.Agents.AI.AgentResponseUpdate update in conversationService
                .RunStreamingAsync(
                    this.GetRequiredUserId(),
                    this.GetRouteAgentId(),
                    conversationId,
                    requestMessages,
                    options,
                    cancellationToken)
                .ConfigureAwait(false))
            {
                yield return update;
            }

            yield break;
        }

        IAgentBuildService buildService = scope.ServiceProvider.GetRequiredService<IAgentBuildService>();
        AIAgent agent = await this
            .BuildAgentAsync(buildService, cancellationToken)
            .ConfigureAwait(false);

        await foreach (Microsoft.Agents.AI.AgentResponseUpdate update in agent
            .RunStreamingAsync(requestMessages, null, options, cancellationToken)
            .ConfigureAwait(false))
        {
            yield return update;
        }
    }

    private ValueTask<AIAgent> BuildAgentAsync(
        IAgentBuildService buildService,
        CancellationToken cancellationToken)
    {
        Guid agentId = this.GetRouteAgentId();
        Guid requestingUserId = this.GetRequiredUserId();
        HttpRequest? request = httpContextAccessor.HttpContext?.Request;
        string requestedVersion = request?.Headers[RunModeHeaderName].ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(requestedVersion))
        {
            requestedVersion = request?.Query["version"].ToString() ?? string.Empty;
        }

        return string.Equals(requestedVersion, "draft", StringComparison.OrdinalIgnoreCase)
            ? buildService.BuildDraftTrialAsync(agentId, requestingUserId, cancellationToken)
            : buildService.BuildPublishedTrialAsync(agentId, requestingUserId, cancellationToken);
    }

    private bool TryGetPublishedConversationId(Microsoft.Agents.AI.AgentRunOptions? options, out Guid conversationId)
    {
        HttpRequest? request = httpContextAccessor.HttpContext?.Request;
        string runMode = request?.Headers[RunModeHeaderName].ToString() ?? string.Empty;
        conversationId = Guid.Empty;
        if (string.Equals(runMode, "draft", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string conversationValue = request?.Headers[ConversationIdHeaderName].ToString() ?? string.Empty;
        if (!string.IsNullOrEmpty(conversationValue))
        {
            // 调用方已显式声明会话意图，格式非法时必须拒绝，不得静默降级为不落库的试运行。
            return Guid.TryParse(conversationValue, out conversationId)
                ? true
                : throw new ArgumentException(
                    $"The '{ConversationIdHeaderName}' header is not a valid conversation identifier.");
        }

        // AG-UI 的 threadId 与 Inkwell 会话标识同值；无协议字段的入口（如 Chat Completions）回退到请求头。
        if (TryGetRunAgentInput(options, out RunAgentInput? runAgentInput)
            && !string.IsNullOrEmpty(runAgentInput.ThreadId))
        {
            return Guid.TryParse(runAgentInput.ThreadId, out conversationId)
                ? true
                : throw new ArgumentException(
                    "The AG-UI 'threadId' is not a valid Inkwell conversation identifier.");
        }

        return false;
    }

    private static bool TryGetRunAgentInput(
        Microsoft.Agents.AI.AgentRunOptions? options,
        [NotNullWhen(true)] out RunAgentInput? runAgentInput)
    {
        if (options is ChatClientAgentRunOptions { ChatOptions: { } chatOptions }
            && chatOptions.TryGetRunAgentInput(out RunAgentInput? input)
            && input is not null)
        {
            runAgentInput = input;
            return true;
        }

        runAgentInput = null;
        return false;
    }

    private static void RecordProtocolRunTags(Microsoft.Agents.AI.AgentRunOptions? options)
    {
        if (Activity.Current is not { } activity || !TryGetRunAgentInput(options, out RunAgentInput? runAgentInput))
        {
            return;
        }

        // 客户端 runId 不可信且可重放，只作为前后端日志对账标签，不进入授权、幂等键或持久化标识。
        _ = activity.SetTag("agui.protocol_run_id", Truncate(runAgentInput.RunId));
        _ = activity.SetTag("agui.protocol_parent_run_id", Truncate(runAgentInput.ParentRunId));
    }

    private static string? Truncate(string? value) => value is null || value.Length <= MaxProtocolRunIdLength
        ? value
        : value[..MaxProtocolRunIdLength];

    private Guid GetRouteAgentId()
    {
        string? routeValue = httpContextAccessor.HttpContext?.Request.RouteValues["agentId"]?.ToString();

        return Guid.TryParse(routeValue, out Guid agentId)
            ? agentId
            : throw new InvalidOperationException("The route does not contain a valid agentId.");
    }

    private Guid GetRequiredUserId()
    {
        string? claimValue = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(claimValue, out Guid userId)
            ? userId
            : throw new UnauthorizedAccessException("The authenticated user identifier is missing or invalid.");
    }

    private sealed class StatelessAgentSession : AgentSession;
}
