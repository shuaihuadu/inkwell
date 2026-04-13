using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

namespace Inkwell;

/// <summary>
/// 向量存储服务注册扩展方法
/// </summary>
public static class VectorStoreServiceCollectionExtensions
{
    /// <summary>
    /// 使用内存向量存储（开发环境）
    /// </summary>
    /// <param name="coreBuilder">Inkwell 核心构建器</param>
    /// <returns>Inkwell 核心构建器</returns>
    public static InkwellCoreBuilder UseInMemoryVectorStore(this InkwellCoreBuilder coreBuilder)
    {
        coreBuilder.Services.AddSingleton<VectorStore>(sp =>
        {
            IEmbeddingGenerator<string, Embedding<float>>? embedding =
                sp.GetService<IEmbeddingGenerator<string, Embedding<float>>>();

            return new InMemoryVectorStore(new InMemoryVectorStoreOptions
            {
                EmbeddingGenerator = embedding
            });
        });

        return coreBuilder;
    }
}
