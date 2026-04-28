using Microsoft.Agents.AI.Workflows;

namespace Inkwell.Workflows;

/// <summary>
/// Workflow 注册表项
/// </summary>
public sealed class WorkflowRegistration
{
    /// <summary>
    /// 获取或设置 Workflow 唯一标识
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// 获取或设置 Workflow 名称
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 获取或设置 Workflow 描述
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// 获取或设置 Workflow 实例
    /// </summary>
    public required Workflow Workflow { get; init; }

    /// <summary>
    /// 获取或设置 Workflow 能力标签
    /// </summary>
    public WorkflowCapabilities Capabilities { get; init; } = new();

    /// <summary>
    /// 获取或设置 Workflow 使用文档，用于 UI 给终端用户呈现 "这个工作流是干啥的、该输入什么、会输出什么"
    /// </summary>
    /// <remarks>
    /// 可选。为空时前端只能看到 <see cref="Description"/>，建议每个对外开放的 Workflow 都填上。
    /// </remarks>
    public WorkflowDocumentation? Documentation { get; init; }
}

/// <summary>
/// Workflow 用户文档
/// 不参与运行时调度，只用于 UI 展示，告诉用户这个工作流解决什么问题、该如何输入、会得到什么输出
/// </summary>
public sealed class WorkflowDocumentation
{
    /// <summary>
    /// 获取或设置一句话用途（比 <see cref="WorkflowRegistration.Description"/> 更直白，面向终端用户）
    /// </summary>
    public string? Purpose { get; init; }

    /// <summary>
    /// 获取或设置输入提示，用作输入框 placeholder
    /// </summary>
    public string? InputHint { get; init; }

    /// <summary>
    /// 获取或设置示例输入。前端可提供 "填入示例" 按钮，将该值写入输入框
    /// </summary>
    public string? InputExample { get; init; }

    /// <summary>
    /// 获取或设置输出说明，让用户对最终产物形态（一段文本 / 多语言版本 / JSON 评分等）有预期
    /// </summary>
    public string? OutputHint { get; init; }

    /// <summary>
    /// 获取或设置能力标签（如 GroupChat、Fan-Out/Fan-In、HITL、MapReduce、Loop、SubWorkflow、Handoff、Declarative）
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}

/// <summary>
/// Workflow 能力标签
/// 描述一个 Workflow 支持的运行模式，决定 AG-UI 适配层和前端 UI 的行为
/// </summary>
public sealed class WorkflowCapabilities
{
    /// <summary>
    /// 获取或设置是否支持多轮对话
    /// true 时 WorkflowChatClient 会将完整 ChatMessage 列表传给 Workflow（Workflow 入口 Executor 需接受 List&lt;ChatMessage&gt;）
    /// false 时仅取最后一条用户消息作为输入（OneShot 语义）
    /// </summary>
    public bool SupportsMultiTurn { get; init; }

    /// <summary>
    /// 获取或设置是否包含人工介入节点（Human-in-the-loop）
    /// true 时 Workflow 可能产生 RequestInfoEvent，需要外部响应才能继续
    /// 前端可据此展示"继续/批准/退回"按钮
    /// </summary>
    public bool SupportsHumanInLoop { get; init; }

    /// <summary>
    /// 获取或设置输入适配器
    /// </summary>
    /// <remarks>
    /// <para>
    /// WorkflowChatClient 默认把 Chat 对话压缩成一个 <see cref="string"/> 作为 Workflow 输入，
    /// 这只适用于入口 Executor 类型为 <c>Executor&lt;string&gt;</c> 的 Workflow。
    /// </para>
    /// <para>
    /// 当入口 Executor 期望的消息类型不是 <c>string</c>（典型如：GroupChat / Handoff 要 <c>List&lt;ChatMessage&gt;</c>、
    /// BatchEvaluation 要 <c>List&lt;ArticleEvaluation&gt;</c>），
    /// 需要在注册时提供本适配器，把用户原始文本转换成入口 Executor 期望的类型。
    /// </para>
    /// <para>
    /// 不设置时按 <c>string</c> 直传，保持向后兼容。
    /// </para>
    /// </remarks>
    public Func<string, object>? InputAdapter { get; init; }

    /// <summary>
    /// 获取或设置是否需要在送入消息后追加一个 <c>TurnToken</c> 触发轮次执行
    /// </summary>
    /// <remarks>
    /// <para>
    /// MAF 的 <c>ChatProtocolExecutor</c>（<c>GroupChatHost</c>、<c>HandoffHost</c> 等）把消息累积与触发轮次拆分成两步：
    /// 单纯发送 <c>List&lt;ChatMessage&gt;</c> 只把消息加入内部缓冲；只有再发送一个 <c>TurnToken</c>，host 才会调用
    /// <c>TakeTurnAsync</c> 选择下一个参与者并真正开始对话。
    /// </para>
    /// <para>
    /// 标记为 <c>true</c> 时，<see cref="WorkflowChatClient"/> 会在
    /// <c>InProcessExecution.RunStreamingAsync</c> 之后立即通过 <c>TrySendMessageAsync</c> 发出 <c>TurnToken(emitEvents: true)</c>。
    /// 仅 GroupChat / Handoff 这类基于 ChatProtocolExecutor 的工作流需要开启。
    /// </para>
    /// </remarks>
    public bool RequiresTurnToken { get; init; }
}

/// <summary>
/// Workflow 注册表
/// 管理所有已注册的 Workflow
/// </summary>
public sealed class WorkflowRegistry : Registry<WorkflowRegistration>
{
    /// <inheritdoc />
    protected override string GetId(WorkflowRegistration item) => item.Id;
}
