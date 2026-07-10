
namespace Inkwell;

/// <summary>工具目录业务 Model；Agent 前缀 + Definition 后缀双重撞名降级（避免与 Microsoft.Extensions.AI/Microsoft.Agents.AI 的运行时 Tool 语义冲突）。</summary>
public sealed record class AgentToolDefinition : IHasTimestamps
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required string ParametersJsonSchema { get; init; }

    public required DateTimeOffset CreatedTime { get; init; }

    public required DateTimeOffset UpdatedTime { get; init; }
}
