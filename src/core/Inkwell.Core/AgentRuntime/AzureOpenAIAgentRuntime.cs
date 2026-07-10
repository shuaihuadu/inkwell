using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.Agents.AI;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Inkwell;

/// <summary>
/// <see cref="IAgentRuntime"/> 唯一实现，基于 Azure OpenAI（<c>Microsoft.Agents.AI.OpenAI</c>）。
/// 唯一允许 <c>using Microsoft.Agents.AI.*</c> 的位置（ADR-017 §依赖规则第 3 条）。
/// </summary>
internal sealed class AzureOpenAIAgentRuntime : IAgentRuntime
{
    private readonly AzureOpenAIClient _client;
    private readonly AzureOpenAIAgentRuntimeOptions _runtimeOptions;
    private readonly AgentRuntimeOptions _defaults;
    private readonly ConcurrentDictionary<string, AIAgent> _agentsByDeployment = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeRuns = new();

    public AzureOpenAIAgentRuntime(IOptions<AzureOpenAIAgentRuntimeOptions> runtimeOptions, IOptions<AgentRuntimeOptions> defaults)
    {
        this._runtimeOptions = runtimeOptions.Value;
        this._defaults = defaults.Value;

        this._client = string.IsNullOrEmpty(this._runtimeOptions.ApiKey)
            ? throw new InvalidOperationException("AzureOpenAIAgentRuntimeOptions.ApiKey is required (v1 supports API key auth only).")
            : new AzureOpenAIClient(new Uri(this._runtimeOptions.Endpoint), new AzureKeyCredential(this._runtimeOptions.ApiKey));
    }

    public async Task<AgentTurnResult> RunAsync(AgentRunRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        (AIAgent? agent, ChatClientAgentRunOptions? options) = this.PrepareRun(request);
        (CancellationTokenSource? linkedCts, CancellationToken linkedToken) = this.RegisterRun(request.RunId, ct);

        try
        {
            AgentResponse response = await agent.RunAsync(ToChatMessages(request.Messages), session: null, options, linkedToken).ConfigureAwait(false);

            return ToTurnResult(request, response);
        }
        catch (OperationCanceledException) when (linkedCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw new OperationCanceledException("Run was cancelled via CancelRunAsync.", linkedToken);
        }
        finally
        {
            this.UnregisterRun(request.RunId);
        }
    }

    public async IAsyncEnumerable<AgentRunEvent> RunStreamingAsync(AgentRunRequest request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        (AIAgent? agent, ChatClientAgentRunOptions? options) = this.PrepareRun(request);
        (CancellationTokenSource _, CancellationToken linkedToken) = this.RegisterRun(request.RunId, ct);

        List<AgentResponseUpdate> updates = [];
        Exception? failure = null;

        try
        {
            await foreach (AgentResponseUpdate? update in agent.RunStreamingAsync(ToChatMessages(request.Messages), session: null, options, linkedToken).ConfigureAwait(false))
            {
                updates.Add(update);

                foreach (AgentRunEvent runEvent in ToRunEvents(request.RunId, update))
                {
                    yield return runEvent;
                }
            }
        }
        finally
        {
            this.UnregisterRun(request.RunId);
        }

        if (failure is not null)
        {
            yield return new RunError { RunId = request.RunId, ErrorMessage = failure.Message, ExceptionType = failure.GetType().FullName ?? failure.GetType().Name };
            yield break;
        }

        AgentResponse response = updates.ToAgentResponse();

        yield return new RunCompleted { RunId = request.RunId, Result = ToTurnResult(request, response) };
    }

    public Task<bool> CancelRunAsync(string runId, CancellationToken ct = default)
    {
        if (this._activeRuns.TryGetValue(runId, out CancellationTokenSource? cts))
        {
            cts.Cancel();

            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    private (AIAgent Agent, ChatClientAgentRunOptions Options) PrepareRun(AgentRunRequest request)
    {
        string deploymentName = string.IsNullOrEmpty(request.ModelId) ? this._runtimeOptions.DeploymentName : request.ModelId;
        AIAgent agent = this._agentsByDeployment.GetOrAdd(deploymentName, name => this._client.GetChatClient(name).AsAIAgent());

        AgentModelParameters? modelParameters = request.ModelParameters;
        ChatOptions chatOptions = new ChatOptions
        {
            Instructions = request.Instructions,
            Temperature = (float?)(modelParameters?.Temperature ?? this._defaults.DefaultTemperature),
            TopP = (float?)(modelParameters?.TopP ?? this._defaults.DefaultTopP),
            MaxOutputTokens = modelParameters?.MaxTokens ?? this._defaults.DefaultMaxTokens,
        };

        if (request.Tools is { Count: > 0 })
        {
            chatOptions.Tools = [.. request.Tools];
        }

        return (agent, new ChatClientAgentRunOptions(chatOptions));
    }

    private (CancellationTokenSource Cts, CancellationToken Token) RegisterRun(string runId, CancellationToken ct)
    {
        CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        this._activeRuns[runId] = cts;

        return (cts, cts.Token);
    }

    private void UnregisterRun(string runId)
    {
        if (this._activeRuns.TryRemove(runId, out CancellationTokenSource? cts))
        {
            cts.Dispose();
        }
    }

    private static IEnumerable<ChatMessage> ToChatMessages(IReadOnlyList<AgentChatMessage> messages) => messages.Select(ToChatMessage);

    private static ChatMessage ToChatMessage(AgentChatMessage message)
    {
        ChatMessage chatMessage = new ChatMessage(message.Role, [.. message.Content]);

        if (message.AuthorName is not null)
        {
            chatMessage.AuthorName = message.AuthorName;
        }

        return chatMessage;
    }

    private static AgentChatMessage ToAgentChatMessage(ChatMessage message) => new()
    {
        Role = message.Role,
        Content = [.. message.Contents.Where(c => c is TextContent or UriContent or DataContent)],
        AuthorName = message.AuthorName,
    };

    private static AgentTurnResult ToTurnResult(AgentRunRequest request, AgentResponse response)
    {
        ChatMessage? lastMessage = response.Messages.LastOrDefault();
        AgentChatMessage message = lastMessage is null ? new AgentChatMessage { Role = ChatRole.Assistant, Content = [] } : ToAgentChatMessage(lastMessage);

        return new AgentTurnResult
        {
            RunId = request.RunId,
            Message = message,
            ToolCalls = ExtractToolCalls(response.Messages),
            ModelIdUsed = request.ModelId ?? string.Empty,
            ModelParametersUsed = request.ModelParameters ?? new AgentModelParameters(),
        };
    }

    private static List<AgentToolCallRecord> ExtractToolCalls(IEnumerable<ChatMessage> messages)
    {
        Dictionary<string, FunctionCallContent> calls = [];
        Dictionary<string, FunctionResultContent> results = [];

        foreach (AIContent? content in messages.SelectMany(m => m.Contents))
        {
            if (content is FunctionCallContent call)
            {
                calls[call.CallId] = call;
            }
            else if (content is FunctionResultContent result)
            {
                results[result.CallId] = result;
            }
        }

        List<AgentToolCallRecord> records = [];

        foreach ((string? callId, FunctionCallContent? call) in calls)
        {
            results.TryGetValue(callId, out FunctionResultContent? result);

            records.Add(new AgentToolCallRecord
            {
                ToolCallId = callId,
                ToolName = call.Name,
                ArgumentsJson = System.Text.Json.JsonSerializer.Serialize(call.Arguments),
                ResultJson = result is null ? string.Empty : System.Text.Json.JsonSerializer.Serialize(result.Result),
                IsError = result?.Exception is not null,
            });
        }

        return records;
    }

    private static IEnumerable<AgentRunEvent> ToRunEvents(string runId, AgentResponseUpdate update)
    {
        foreach (AIContent content in update.Contents)
        {
            switch (content)
            {
                case TextContent text:
                    yield return new TextDelta { RunId = runId, DeltaText = text.Text };
                    break;
                case FunctionCallContent call:
                    yield return new ToolCallRequested
                    {
                        RunId = runId,
                        ToolCallId = call.CallId,
                        ToolName = call.Name,
                        ArgumentsJson = System.Text.Json.JsonSerializer.Serialize(call.Arguments),
                    };
                    break;
                case FunctionResultContent result:
                    yield return new ToolCallResult
                    {
                        RunId = runId,
                        ToolCallId = result.CallId,
                        ResultJson = System.Text.Json.JsonSerializer.Serialize(result.Result),
                        IsError = result.Exception is not null,
                    };
                    break;
            }
        }
    }
}
