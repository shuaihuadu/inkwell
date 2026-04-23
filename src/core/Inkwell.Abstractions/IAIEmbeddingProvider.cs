using Microsoft.Extensions.AI;

namespace Inkwell;

/// <summary>
/// AI Embedding 模型供应商插件接口
/// </summary>
public interface IAIEmbeddingProvider
{
    /// <summary>
    /// 获取 Provider 名称，与配置 <see cref="AIEndpointOptions.Provider"/> 字段大小写不敏感匹配
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 根据端点配置创建 Embedding 生成器实例
    /// </summary>
    /// <param name="options">端点配置</param>
    /// <returns>Embedding 生成器</returns>
    IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator(AIEndpointOptions options);
}
