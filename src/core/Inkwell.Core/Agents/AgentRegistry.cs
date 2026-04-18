namespace Inkwell.Agents;

/// <summary>
/// Agent 注册表
/// 管理所有已注册的 Agent，支持按 ID 查找和枚举
/// </summary>
public sealed class AgentRegistry : Registry<AgentRegistration>
{
    /// <inheritdoc />
    protected override string GetId(AgentRegistration item) => item.Id;
}
