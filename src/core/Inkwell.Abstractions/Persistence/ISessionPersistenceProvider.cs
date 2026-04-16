using System.Text.Json;

namespace Inkwell;

/// <summary>
/// Agent 会话持久化提供程序
/// 负责序列化/反序列化 Agent 会话状态到外部存储
/// </summary>
public interface ISessionPersistenceProvider
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

    /// <summary>
    /// 获取会话详情
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>会话信息，不存在时返回 null</returns>
    Task<SessionInfo?> GetSessionInfoAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定 Agent 的会话列表（含元数据）
    /// </summary>
    /// <param name="agentId">Agent ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>会话信息列表，按更新时间倒序</returns>
    Task<IReadOnlyList<SessionInfo>> ListSessionInfosAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新会话标题
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="title">新标题</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateSessionTitleAsync(string sessionId, string title, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存聊天消息
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="messages">消息列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveMessagesAsync(string sessionId, IEnumerable<ChatMessageRecord> messages, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定会话的消息列表
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>消息列表，按创建时间正序</returns>
    Task<IReadOnlyList<ChatMessageRecord>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default);
}
