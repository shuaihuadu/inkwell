using Microsoft.Agents.AI;

namespace Inkwell.Agents;

/// <summary>
/// Agent 注册表项
/// </summary>
public sealed class AgentRegistration
{
    /// <summary>
    /// 获取或设置 Agent 唯一标识
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// 获取或设置 Agent 名称
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 获取或设置 Agent 描述
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// 获取或设置 Agent 实例
    /// </summary>
    public required AIAgent Agent { get; init; }

    /// <summary>
    /// 获取或设置 AG-UI 端点路径（如 /api/agui/writer）
    /// </summary>
    public required string AguiRoute { get; init; }
}
