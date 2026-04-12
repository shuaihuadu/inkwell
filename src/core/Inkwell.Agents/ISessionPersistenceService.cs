using System.Text.Json;

namespace Inkwell.Agents;

/// <summary>
/// Agent 会话持久化服务
/// 负责序列化/反序列化 Agent 会话状态到外部存储
/// </summary>
public interface ISessionPersistenceService
{
    /// <summary>
    /// 保存会话状态
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="agentId">Agent ID</param>
    /// <param name="sessionState">序列化后的会话状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveSessionAsync(string sessionId, string agentId, JsonElement sessionState, CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载会话状态
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>序列化的会话状态，不存在时返回 null</returns>
    Task<JsonElement?> LoadSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定 Agent 的所有会话 ID
    /// </summary>
    /// <param name="agentId">Agent ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>会话 ID 列表</returns>
    Task<IReadOnlyList<string>> ListSessionsAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除会话
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}
