// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>工具目录业务 Model；Agent 前缀 + Definition 后缀双重撞名降级（避免与 Microsoft.Extensions.AI/Microsoft.Agents.AI 的运行时 Tool 语义冲突）。</summary>
public sealed record class AgentToolDefinition : IHasTimestamps
{
    /// <summary>
    /// 获取工具标识。
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// 获取工具名称。
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 获取工具描述。
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// 获取工具参数的 JSON Schema。
    /// </summary>
    public required string ParametersJsonSchema { get; init; }

    /// <summary>
    /// 获取工具创建时间。
    /// </summary>
    public required DateTimeOffset CreatedTime { get; init; }

    /// <summary>
    /// 获取工具更新时间。
    /// </summary>
    public required DateTimeOffset UpdatedTime { get; init; }
}
