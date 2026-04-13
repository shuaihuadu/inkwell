using System.Text.Json;

namespace Inkwell.Agents;

/// <summary>
/// 工具循环检查点服务（需求 2.16）
/// 在 Agent 执行长链工具调用时，保存中间检查点，支持失败后恢复
/// </summary>
public sealed class ToolLoopCheckpointService(ISessionPersistenceService sessionService)
{
    /// <summary>
    /// 保存工具调用循环的检查点
    /// </summary>
    /// <param name="agentId">Agent ID</param>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="toolCallIndex">当前工具调用索引（第几次调用）</param>
    /// <param name="toolName">工具名称</param>
    /// <param name="toolResult">工具调用结果</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveCheckpointAsync(
        string agentId,
        string sessionId,
        int toolCallIndex,
        string toolName,
        string toolResult,
        CancellationToken cancellationToken = default)
    {
        ToolLoopCheckpoint checkpoint = new()
        {
            AgentId = agentId,
            SessionId = sessionId,
            ToolCallIndex = toolCallIndex,
            ToolName = toolName,
            ToolResult = toolResult,
            Timestamp = DateTimeOffset.UtcNow
        };

        string checkpointKey = $"tool-checkpoint:{sessionId}:{toolCallIndex}";
        JsonElement state = JsonSerializer.SerializeToElement(checkpoint);

        await sessionService.SaveSessionAsync(checkpointKey, agentId, state, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 加载最近的工具调用检查点
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="toolCallIndex">要恢复的工具调用索引</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>检查点信息，不存在时返回 null</returns>
    public async Task<ToolLoopCheckpoint?> LoadCheckpointAsync(
        string sessionId,
        int toolCallIndex,
        CancellationToken cancellationToken = default)
    {
        string checkpointKey = $"tool-checkpoint:{sessionId}:{toolCallIndex}";
        JsonElement? state = await sessionService.LoadSessionAsync(checkpointKey, cancellationToken).ConfigureAwait(false);

        if (state is null)
        {
            return null;
        }

        return JsonSerializer.Deserialize<ToolLoopCheckpoint>(state.Value.GetRawText());
    }

    /// <summary>
    /// 查找指定会话的最新检查点索引
    /// </summary>
    /// <param name="agentId">Agent ID</param>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="maxIndex">搜索的最大索引</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最新有效的检查点索引，无检查点时返回 -1</returns>
    public async Task<int> FindLatestCheckpointIndexAsync(
        string agentId,
        string sessionId,
        int maxIndex = 100,
        CancellationToken cancellationToken = default)
    {
        // 从高到低搜索
        for (int i = maxIndex; i >= 0; i--)
        {
            ToolLoopCheckpoint? checkpoint = await this.LoadCheckpointAsync(sessionId, i, cancellationToken).ConfigureAwait(false);
            if (checkpoint is not null)
            {
                return i;
            }
        }

        return -1;
    }
}

/// <summary>
/// 工具循环检查点数据
/// </summary>
public sealed class ToolLoopCheckpoint
{
    /// <summary>
    /// 获取或设置 Agent ID
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置会话 ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置工具调用索引
    /// </summary>
    public int ToolCallIndex { get; set; }

    /// <summary>
    /// 获取或设置工具名称
    /// </summary>
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置工具调用结果
    /// </summary>
    public string ToolResult { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置检查点时间戳
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}
