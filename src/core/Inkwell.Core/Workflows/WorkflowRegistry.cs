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
}

/// <summary>
/// Workflow 注册表
/// 管理所有已注册的 Workflow
/// </summary>
public sealed class WorkflowRegistry
{
    private readonly Dictionary<string, WorkflowRegistration> _workflows = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 注册一个 Workflow
    /// </summary>
    /// <param name="registration">Workflow 注册信息</param>
    public void Register(WorkflowRegistration registration)
    {
        this._workflows[registration.Id] = registration;
    }

    /// <summary>
    /// 根据 ID 获取 Workflow
    /// </summary>
    /// <param name="id">Workflow ID</param>
    /// <returns>Workflow 注册信息，不存在时返回 null</returns>
    public WorkflowRegistration? GetById(string id)
    {
        this._workflows.TryGetValue(id, out WorkflowRegistration? registration);
        return registration;
    }

    /// <summary>
    /// 获取所有已注册的 Workflow
    /// </summary>
    /// <returns>Workflow 注册信息列表</returns>
    public IReadOnlyList<WorkflowRegistration> GetAll()
    {
        return this._workflows.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// 获取已注册的 Workflow 数量
    /// </summary>
    public int Count => this._workflows.Count;
}
