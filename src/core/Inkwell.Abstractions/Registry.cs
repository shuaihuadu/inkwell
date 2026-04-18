using System.Collections;

namespace Inkwell;

/// <summary>
/// 通用注册表基类
/// AgentRegistry / WorkflowRegistry 共享的"按 Id 注册 + 列举"行为
/// </summary>
/// <typeparam name="T">注册项类型</typeparam>
public abstract class Registry<T> : IReadOnlyCollection<T> where T : class
{
    private readonly Dictionary<string, T> _items = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 从注册项中提取 Id（由派生类决定 Id 字段）
    /// </summary>
    /// <param name="item">注册项</param>
    /// <returns>注册项的唯一 Id</returns>
    protected abstract string GetId(T item);

    /// <summary>
    /// 注册一项（同 Id 时覆盖）
    /// </summary>
    /// <param name="item">注册项</param>
    public void Register(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        this._items[this.GetId(item)] = item;
    }

    /// <summary>
    /// 根据 Id 查找
    /// </summary>
    /// <param name="id">注册项 Id</param>
    /// <returns>对应的项；不存在返回 null</returns>
    public T? GetById(string id)
    {
        this._items.TryGetValue(id, out T? item);
        return item;
    }

    /// <summary>
    /// 获取所有注册项的只读快照
    /// </summary>
    /// <returns>注册项列表</returns>
    public IReadOnlyList<T> GetAll() => this._items.Values.ToList().AsReadOnly();

    /// <inheritdoc />
    public int Count => this._items.Count;

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => this._items.Values.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
