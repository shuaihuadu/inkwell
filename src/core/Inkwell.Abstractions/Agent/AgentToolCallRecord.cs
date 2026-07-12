// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>非流式路径下单次工具调用的回溯记录。</summary>
public sealed record class AgentToolCallRecord
{
    /// <summary>
    /// 获取工具调用标识。
    /// </summary>
    public required string ToolCallId { get; init; }

    /// <summary>
    /// 获取工具名称。
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// 获取调用参数 JSON。
    /// </summary>
    public required string ArgumentsJson { get; init; }

    /// <summary>
    /// 获取调用结果 JSON。
    /// </summary>
    public required string ResultJson { get; init; }

    /// <summary>
    /// 获取工具调用是否失败。
    /// </summary>
    public required bool IsError { get; init; }
}
