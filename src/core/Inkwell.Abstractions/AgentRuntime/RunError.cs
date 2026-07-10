namespace Inkwell;

/// <summary>
/// 对应 AG-UI <c>lifecycle</c>（error）。本事件是错误的 DTO 表达，不是异常路径——
/// 流式枚举遇到不可恢复错误应产出本事件后正常结束枚举，而非抛异常打断 <c>await foreach</c>。
/// </summary>
public sealed record class RunError : AgentRunEvent
{
    public required string ErrorMessage { get; init; }

    public required string ExceptionType { get; init; }
}
