using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Inkwell.Workflows;

/// <summary>
/// 将 Workflow 包装为 IChatClient，使其能通过 ChatClientAgent 接入 AG-UI 协议
/// 设计目标：
///   1. 接口统一 —— 与 Agent 走相同的 AG-UI / Session 持久化链路
///   2. 能力感知 —— 根据 WorkflowCapabilities 决定是否传递多轮历史
///   3. HITL 友好 —— 将 RequestInfoEvent 通过 <see cref="HitlPendingRegistry"/> 暴露给外部端点，
///                  让前端可通过 /api/hitl/{requestId}/respond 决策通过/退回
/// </summary>
public sealed class WorkflowChatClient(
    Workflow workflow,
    WorkflowCapabilities? capabilities = null,
    ILogger<WorkflowChatClient>? logger = null,
    HitlPendingRegistry? hitlRegistry = null) : IChatClient
{
    private readonly WorkflowCapabilities _capabilities = capabilities ?? new();
    private readonly ILogger<WorkflowChatClient> _logger = logger ?? NullLogger<WorkflowChatClient>.Instance;
    private readonly HitlPendingRegistry? _hitlRegistry = hitlRegistry;

    /// <summary>HITL 请求前端传输标记（用同一枚举在前端搜索匹配）</summary>
    internal const string HitlMarkerPrefix = "<<<HITL_REQUEST:";

    /// <summary>HITL 标记结束</summary>
    internal const string HitlMarkerSuffix = ">>>";

    /// <summary>工具调用开始 / 参数 标记前缀</summary>
    internal const string ToolCallMarkerPrefix = "<<<TOOL_CALL:";

    /// <summary>工具调用结果标记前缀</summary>
    internal const string ToolResultMarkerPrefix = "<<<TOOL_RESULT:";

    /// <summary>工具调用标记结束（与 HITL 共用，节省前端正则复杂度）</summary>
    internal const string ToolMarkerSuffix = ">>>";

    private static readonly JsonSerializerOptions s_jsonOutput = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>紧凑 JSON（用于 HITL 标记内嵌 payload，无缩进无空格避免解析失败）</summary>
    private static readonly JsonSerializerOptions s_jsonCompact = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <inheritdoc />
    public ChatClientMetadata Metadata { get; } = new("WorkflowChatClient");

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        StringBuilder outputBuilder = new();

        await foreach (string fragment in this.RunWorkflowAsync(chatMessages, cancellationToken).ConfigureAwait(false))
        {
            outputBuilder.AppendLine(fragment);
        }

        string output = outputBuilder.Length > 0
            ? outputBuilder.ToString().TrimEnd()
            : "Workflow 执行完成，无输出内容。";

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, output));
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (string fragment in this.RunWorkflowAsync(chatMessages, cancellationToken).ConfigureAwait(false))
        {
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                Contents = [new TextContent(fragment)]
            };
        }
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <summary>
    /// 执行 Workflow 并把关键事件翻译成可读文本片段
    /// 事件处理策略：
    ///   - AgentResponseUpdateEvent   子 Agent 流式 token，直接透传（让前端感知"正在生成"）
    ///   - AgentResponseEvent         子 Agent 最终完整响应，跳过（与 Update 重复）
    ///   - WorkflowOutputEvent        终态产物：string 直出；复杂对象用 JSON 序列化
    ///   - RequestInfoEvent           HITL 节点自动批准（P2 将由前端按钮接管）
    ///   - WorkflowErrorEvent         Workflow 内部异常，把错误可见化，避免"什么都没输出"
    /// </summary>
    private async IAsyncEnumerable<string> RunWorkflowAsync(
        IEnumerable<ChatMessage> chatMessages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string rawInput = ExtractWorkflowInput(chatMessages, this._capabilities);

        // 默认以 string 直接喂给 Workflow；对入口类型不是 string 的 Workflow（GroupChat / Handoff / BatchEvaluation 等），
        // 由注册方提供 InputAdapter 把原始文本转换成入口 Executor 期望的类型。
        object input = this._capabilities.InputAdapter is { } adapter
            ? (adapter(rawInput) ?? rawInput)
            : rawInput;

        this._logger.LogInformation(
            "[WorkflowChatClient] Run started. MultiTurn={MultiTurn} HITL={HITL} InputLen={InputLen} InputType={InputType}",
            this._capabilities.SupportsMultiTurn, this._capabilities.SupportsHumanInLoop, rawInput.Length, input.GetType().Name);

        // 先让前端立即看到"已开始"的反馈，避免长耗时 Workflow 前几秒什么都不显示
        yield return "[系统] Workflow 已启动，正在执行...\n\n";

        StreamingRun? run = null;
        string? startupError = null;
        try
        {
            // 使用 dynamic 触发运行时泛型分派，让 MAF 根据 input 的实际类型选择
            // InProcessExecution.RunStreamingAsync&lt;TInput&gt; 的正确实例化
            run = await InProcessExecution.RunStreamingAsync(workflow, (dynamic)input, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // 入口类型不匹配等启动异常
            startupError = ex.Message;
            this._logger.LogError(ex, "[WorkflowChatClient] Workflow startup failed. InputType={InputType}", input.GetType().Name);
        }

        if (run is null)
        {
            yield return $"[错误] Workflow 启动失败：{startupError ?? "未知原因"}";
            yield break;
        }

        await using (run.ConfigureAwait(false))
        {
            int outputCount = 0;
            int hitlCount = 0;
            // 记录上一次发出 token 的 Executor，用于在切换时插入段落头
            // 让前端 parseWorkflowSteps 能把每个 Executor 的流式输出渲染成独立的 ThoughtChain 项
            string? lastExecutorId = null;

            await foreach (WorkflowEvent evt in run.WatchStreamAsync(cancellationToken).ConfigureAwait(false))
            {
                switch (evt)
                {
                    // 1) 子 Agent 的流式 token（最先匹配，因为它继承自 WorkflowOutputEvent）
                    case AgentResponseUpdateEvent updateEvent:
                        {
                            AgentResponseUpdate? update = updateEvent.Update;
                            if (update is null)
                            {
                                break;
                            }

                            // 1a) 扫描 Contents 里的工具调用 / 结果，转成前端可识别的文本标记
                            if (update.Contents is { Count: > 0 })
                            {
                                foreach (AIContent content in update.Contents)
                                {
                                    switch (content)
                                    {
                                        case FunctionCallContent call:
                                            this._logger.LogInformation(
                                                "[WorkflowChatClient] FunctionCall emitted. Executor={Executor} CallId={CallId} Name={Name}",
                                                updateEvent.ExecutorId, call.CallId, call.Name);
                                            yield return EmitToolCallMarker(updateEvent.ExecutorId, call);
                                            break;
                                        case FunctionResultContent result:
                                            this._logger.LogInformation(
                                                "[WorkflowChatClient] FunctionResult emitted. Executor={Executor} CallId={CallId} HasException={HasException}",
                                                updateEvent.ExecutorId, result.CallId, result.Exception is not null);
                                            yield return EmitToolResultMarker(updateEvent.ExecutorId, result);
                                            break;
                                    }
                                }
                            }

                            // 1b) 正文 token 继续透传
                            string? text = update.Text;
                            if (!string.IsNullOrEmpty(text))
                            {
                                // 切换到新 Executor 时先发一个段落头，让前端能把每个 Executor 的流式输出
                                // 渲染成独立的 ThoughtChain 项，而不是把所有 token 堆进同一个块
                                if (!string.Equals(lastExecutorId, updateEvent.ExecutorId, StringComparison.Ordinal))
                                {
                                    yield return $"\n\n[{updateEvent.ExecutorId}]\n";
                                    lastExecutorId = updateEvent.ExecutorId;
                                }

                                yield return text;
                            }

                            break;
                        }

                    // 2) 子 Agent 的最终响应：跳过，避免与 Update 的 token 重复
                    case AgentResponseEvent:
                        break;

                    // 3) 终态产物（执行 YieldOutputAsync 产生的）
                    case WorkflowOutputEvent outputEvent when outputEvent.Data is not null:
                        outputCount++;
                        yield return FormatOutput(outputEvent);
                        break;

                    // 4) HITL 请求：注册到 Registry 并发标记给前端，等待外部 /api/hitl/{id}/respond
                    case RequestInfoEvent requestEvent when this._capabilities.SupportsHumanInLoop:
                        hitlCount++;
                        yield return this.EmitHitlRequest(run, requestEvent);
                        break;

                    // 5) Workflow 内部异常：必须可见化
                    case WorkflowErrorEvent errorEvent:
                        {
                            string msg = errorEvent.Exception?.Message ?? "Unknown workflow error";
                            this._logger.LogError(errorEvent.Exception, "[WorkflowChatClient] WorkflowErrorEvent received");
                            yield return $"\n\n[错误] Workflow 执行异常：{msg}";
                            break;
                        }

                    // 6) 其他事件（ExecutorInvoked/Completed/Failed 等）——统一记录日志方便排查
                    default:
                        {
                            string evtType = evt.GetType().Name;
                            if (evtType.Contains("Failure", StringComparison.OrdinalIgnoreCase)
                                || evtType.Contains("Error", StringComparison.OrdinalIgnoreCase))
                            {
                                this._logger.LogError("[WorkflowChatClient] Unhandled failure event: {EventType} Payload={Payload}",
                                    evtType, evt.ToString());
                                yield return $"\n\n[错误] {evtType}：{evt}";
                            }
                            else
                            {
                                this._logger.LogInformation("[WorkflowChatClient] Event: {EventType}", evtType);
                            }
                            break;
                        }
                }
            }

            this._logger.LogInformation("[WorkflowChatClient] Run completed. Outputs={Outputs} HITL={HITL}",
                outputCount, hitlCount);

            if (outputCount == 0)
            {
                yield return "\n\n[系统] Workflow 已结束，但未产出任何终态输出（YieldOutputAsync）";
            }
        }
    }

    /// <summary>
    /// 把 <see cref="WorkflowOutputEvent.Data"/> 格式化为人类可读文本
    /// 优先级：string/Enum → 直出；AgentResponse/Update → .Text；其他 → JSON 序列化
    /// 避免 <c>obj.ToString()</c> 对普通类返回类名（如 "Inkwell.Article"）
    /// </summary>
    private static string FormatOutput(WorkflowOutputEvent outputEvent)
    {
        object? data = outputEvent.Data;
        string executorLabel = string.IsNullOrWhiteSpace(outputEvent.ExecutorId)
            ? string.Empty
            : $"\n\n[{outputEvent.ExecutorId}]\n";

        string body = data switch
        {
            null => string.Empty,
            string s => s,
            Enum e => e.ToString(),
            AgentResponse r => r.Text,
            AgentResponseUpdate u => u.Text,
            _ => SafeSerialize(data)
        };

        return executorLabel + body;
    }

    /// <summary>
    /// 把 <see cref="RequestInfoEvent"/> 注册到 Registry 并生成供前端解析的文本标记
    /// 标记格式：<c>&lt;&lt;&lt;HITL_REQUEST:{json}&gt;&gt;&gt;</c>
    /// Registry 不可用（如未注册服务）时回退到自动批准，避免卡死
    /// </summary>
    private string EmitHitlRequest(StreamingRun run, RequestInfoEvent requestEvent)
    {
        string requestId = requestEvent.Request.RequestId;

        if (this._hitlRegistry is null)
        {
            this._logger.LogWarning("[WorkflowChatClient] HitlPendingRegistry not registered, falling back to auto-approve. RequestId={RequestId}", requestId);
            _ = Task.Run(async () =>
            {
                try
                {
                    await run.SendResponseAsync(requestEvent.Request.CreateResponse(true)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "[WorkflowChatClient] Fallback auto-approve failed");
                }
            });
            return "\n\n[系统] HITL Registry 未注册，已自动批准\n\n";
        }

        // 尝试把 PortableValue 解成可序列化的典型类型（Article / string / 其他）
        object? payload = ExtractPortablePayload(requestEvent.Request.Data);

        this._hitlRegistry.Register(new HitlPendingEntry(requestId, run, requestEvent, payload));

        string payloadJson = SafeSerializeCompact(new
        {
            id = requestId,
            payload
        });

        this._logger.LogInformation("[WorkflowChatClient] HITL pending. RequestId={RequestId}", requestId);

        return $"\n\n{HitlMarkerPrefix}{payloadJson}{HitlMarkerSuffix}\n\n";
    }

    /// <summary>
    /// 生成"工具调用开始"标记。前端会把它从正文中剥离，转成工具调用气泡。
    /// </summary>
    private static string EmitToolCallMarker(string executorId, FunctionCallContent call)
    {
        string argumentsText = call.Arguments is null
            ? "{}"
            : SafeSerializeCompact(call.Arguments);

        string payload = SafeSerializeCompact(new
        {
            executor = executorId,
            callId = call.CallId,
            name = call.Name,
            arguments = argumentsText
        });

        return $"\n\n{ToolCallMarkerPrefix}{payload}{ToolMarkerSuffix}\n\n";
    }

    /// <summary>
    /// 生成"工具调用结果"标记。前端按 callId 与 TOOL_CALL 匹配闭合。
    /// </summary>
    private static string EmitToolResultMarker(string executorId, FunctionResultContent result)
    {
        object? value = result.Result;
        string resultText = value switch
        {
            null => string.Empty,
            string s => s,
            _ => SafeSerializeCompact(value)
        };

        string payload = SafeSerializeCompact(new
        {
            executor = executorId,
            callId = result.CallId,
            result = resultText,
            exception = result.Exception?.Message
        });

        return $"\n\n{ToolResultMarkerPrefix}{payload}{ToolMarkerSuffix}\n\n";
    }

    /// <summary>
    /// 从 <see cref="PortableValue"/> 中提取出典型业务对象用于前端展示
    /// </summary>
    private static object? ExtractPortablePayload(PortableValue data)
    {
        if (data is null)
        {
            return null;
        }

        // 目前内容流水线只会走 Article 这个端口，直接尝试
        if (data.Is(out Article? article) && article is not null)
        {
            return article;
        }

        // 兜底：返回 TypeId 字符串，至少让前端能显示有东西等待审核
        return new { type = data.TypeId?.ToString() ?? "unknown" };
    }

    private static string SafeSerializeCompact(object data)
    {
        try
        {
            return JsonSerializer.Serialize(data, data.GetType(), s_jsonCompact);
        }
        catch (Exception)
        {
            return "{}";
        }
    }

    private static string SafeSerialize(object data)
    {
        try
        {
            return JsonSerializer.Serialize(data, data.GetType(), s_jsonOutput);
        }
        catch (Exception)
        {
            return data.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// 根据能力标签决定 Workflow 入口的输入
    /// 多轮：拼接历史（占位，需入口 Executor 接受 List&lt;ChatMessage&gt; 才能真正发挥作用）
    /// 单轮：只取最后一条 User 消息
    /// </summary>
    private static string ExtractWorkflowInput(IEnumerable<ChatMessage> chatMessages, WorkflowCapabilities capabilities)
    {
        if (capabilities.SupportsMultiTurn)
        {
            StringBuilder builder = new();
            foreach (ChatMessage msg in chatMessages)
            {
                if (string.IsNullOrWhiteSpace(msg.Text))
                {
                    continue;
                }

                builder.Append(msg.Role.Value).Append(": ").AppendLine(msg.Text);
            }

            return builder.Length > 0 ? builder.ToString().TrimEnd() : "请输入主题";
        }

        string? lastUserMessage = null;
        foreach (ChatMessage msg in chatMessages)
        {
            if (msg.Role == ChatRole.User && !string.IsNullOrWhiteSpace(msg.Text))
            {
                lastUserMessage = msg.Text;
            }
        }

        return lastUserMessage ?? "请输入主题";
    }
}
