using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Qdrant.Client;
using Inkwell;

namespace Inkwell.VectorStore.Qdrant;

/// <summary>注册 <see cref="Microsoft.Extensions.VectorData.VectorStore"/> 的 Qdrant 实现（ADR-020，integration test / prod 默认）。</summary>
public static class InkwellVectorStoreQdrantServiceCollectionExtensions
{
    /// <summary>
    /// 注册基于 <see cref="Qdrant.Client.QdrantClient"/> 的 <see cref="Microsoft.Extensions.VectorData.VectorStore"/> 实现。
    /// </summary>
    /// <param name="builder">Inkwell Builder DSL 入口。</param>
    /// <param name="host">Qdrant 服务主机名。</param>
    /// <param name="port">Qdrant gRPC 端口，默认 <c>6334</c>。</param>
    /// <param name="https">是否通过 HTTPS/TLS 连接。</param>
    /// <param name="apiKey">可选的 API Key。</param>
    /// <returns>供链式调用的 <paramref name="builder"/>。</returns>
    public static IInkwellBuilder UseQdrantVectorStore(this IInkwellBuilder builder, string host, int port = 6334, bool https = false, string? apiKey = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(host);

        builder.Services.AddSingleton(_ => new QdrantClient(host, port, https, apiKey));
        builder.Services.AddSingleton<Microsoft.Extensions.VectorData.VectorStore>(sp =>
            new QdrantVectorStore(sp.GetRequiredService<QdrantClient>(), ownsClient: true));

        return builder;
    }
}
