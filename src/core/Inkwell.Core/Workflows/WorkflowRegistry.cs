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
