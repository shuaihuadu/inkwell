namespace Inkwell.Agents;

/// <summary>
/// Agent 注册表
/// 管理所有已注册的 Agent，支持按 ID 查找和枚举
/// </summary>
public sealed class AgentRegistry
{
    private readonly Dictionary<string, AgentRegistration> _agents = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 注册一个 Agent
    /// </summary>
    /// <param name="registration">Agent 注册信息</param>
    public void Register(AgentRegistration registration)
    {
        this._agents[registration.Id] = registration;
    }

    /// <summary>
    /// 根据 ID 获取 Agent
    /// </summary>
    /// <param name="id">Agent ID</param>
    /// <returns>Agent 注册信息，不存在时返回 null</returns>
    public AgentRegistration? GetById(string id)
    {
        this._agents.TryGetValue(id, out AgentRegistration? registration);
        return registration;
    }

    /// <summary>
    /// 获取所有已注册的 Agent
    /// </summary>
    /// <returns>Agent 注册信息列表</returns>
    public IReadOnlyList<AgentRegistration> GetAll()
    {
        return this._agents.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// 获取已注册的 Agent 数量
    /// </summary>
    public int Count => this._agents.Count;
}
