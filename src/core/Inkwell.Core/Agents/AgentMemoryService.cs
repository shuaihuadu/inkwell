using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

namespace Inkwell.Agents;

/// <summary>
/// Agent 记忆服务
/// 提供基于向量存储的对话历史记忆和 RAG 知识库检索能力
/// </summary>
public sealed class AgentMemoryService
{
    private readonly VectorStore _vectorStore;

    /// <summary>
    /// 初始化 Agent 记忆服务
    /// </summary>
    /// <param name="embeddingEndpoint">Embedding 模型端点</param>
    /// <param name="embeddingDeploymentName">Embedding 部署名称</param>
    /// <param name="embeddingGenerator">嵌入生成器（可选，用于自定义生成器）</param>
    public AgentMemoryService(IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator = null)
    {
        this._vectorStore = new InMemoryVectorStore(new InMemoryVectorStoreOptions
        {
            EmbeddingGenerator = embeddingGenerator
        });
    }

    /// <summary>
    /// 创建并返回用于 Agent 的 ChatHistoryMemoryProvider
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="collectionName">向量集合名称</param>
    /// <param name="vectorDimensions">向量维度，默认 1536（text-embedding-3-small）</param>
    /// <returns>ChatHistoryMemoryProvider 实例</returns>
    public ChatHistoryMemoryProvider CreateMemoryProvider(
        string userId = "default",
        string collectionName = "inkwell-chat-history",
        int vectorDimensions = 1536)
    {
        return new ChatHistoryMemoryProvider(
            this._vectorStore,
            collectionName: collectionName,
            vectorDimensions: vectorDimensions,
            session => new ChatHistoryMemoryProvider.State(
                storageScope: new() { UserId = userId, SessionId = Guid.NewGuid().ToString() },
                searchScope: new() { UserId = userId }));
    }

    /// <summary>
    /// 获取底层向量存储实例（用于 RAG 场景）
    /// </summary>
    public VectorStore VectorStore => this._vectorStore;
}
