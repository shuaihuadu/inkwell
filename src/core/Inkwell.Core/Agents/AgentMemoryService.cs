using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace Inkwell.Agents;

/// <summary>
/// Agent 记忆服务
/// 提供基于向量存储的对话历史记忆能力
/// </summary>
public sealed class AgentMemoryService(VectorStore vectorStore)
{
    /// <summary>
    /// 创建并返回用于 Agent 的 ChatHistoryMemoryProvider
    /// </summary>
    /// <param name="agentId">Agent ID（用于集合命名和搜索范围）</param>
    /// <param name="vectorDimensions">向量维度，默认 1536（text-embedding-3-small）</param>
    /// <returns>ChatHistoryMemoryProvider 实例</returns>
    public ChatHistoryMemoryProvider CreateMemoryProvider(
        string agentId,
        int vectorDimensions = 1536)
    {
        return new ChatHistoryMemoryProvider(
            vectorStore,
            collectionName: $"inkwell_memory_{agentId}",
            vectorDimensions: vectorDimensions,
            session => new ChatHistoryMemoryProvider.State(
                storageScope: new() { AgentId = agentId },
                searchScope: new() { AgentId = agentId }));
    }

    /// <summary>
    /// 获取底层向量存储实例
    /// </summary>
    public VectorStore VectorStore => vectorStore;
}
