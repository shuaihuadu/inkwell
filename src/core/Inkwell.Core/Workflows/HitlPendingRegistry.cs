using System.Collections.Concurrent;
using Microsoft.Agents.AI.Workflows;

namespace Inkwell.Workflows;

/// <summary>
/// 等待人工审核的 HITL 请求注册表
/// Workflow 在命中 <see cref="RequestInfoEvent"/> 时把 (StreamingRun, RequestInfoEvent) 写入这里
/// 由外部 HTTP 端点（/api/hitl/{requestId}/respond）按 RequestId 取出并回写决策
/// </summary>
public sealed class HitlPendingRegistry
{
    private readonly ConcurrentDictionary<string, HitlPendingEntry> _pending =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 注册一个挂起的 HITL 请求
    /// </summary>
    /// <param name="entry">挂起项</param>
    public void Register(HitlPendingEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        this._pending[entry.RequestId] = entry;
    }

    /// <summary>
    /// 取出并移除一个挂起请求（已响应则返回 null）
    /// </summary>
    /// <param name="requestId">请求 ID</param>
    /// <returns>挂起项；不存在返回 null</returns>
    public HitlPendingEntry? TakeOut(string requestId)
    {
        return this._pending.TryRemove(requestId, out HitlPendingEntry? entry) ? entry : null;
    }

    /// <summary>
    /// 查看当前挂起数量（监控 / 健康检查用途）
    /// </summary>
    /// <returns>挂起项数量</returns>
    public int Count => this._pending.Count;
}

/// <summary>
/// HITL 挂起项
/// </summary>
/// <param name="RequestId">请求 ID（用于前端与后端对齐）</param>
/// <param name="Run">对应的 Workflow 流式运行句柄</param>
/// <param name="Event">原始 RequestInfoEvent，用于 CreateResponse</param>
/// <param name="Payload">供前端展示的数据负载（通常是 Article）</param>
public sealed record HitlPendingEntry(
    string RequestId,
    StreamingRun Run,
    RequestInfoEvent Event,
    object? Payload);
