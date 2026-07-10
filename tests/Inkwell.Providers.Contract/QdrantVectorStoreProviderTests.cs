// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.VectorStore.Qdrant;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Testcontainers.Qdrant;

namespace Inkwell.Providers.Contract;

/// <summary>
/// 针对 <c>UseQdrantVectorStore</c> 注册的 <see cref="VectorStore"/> 实现的真实 Testcontainers
/// 集成测试。覆盖集合创建 / 写入 / 按 Key 读取的完整往返，而非仅编译期验证。
/// </summary>
[TestClass]
public sealed class QdrantVectorStoreProviderTests
{
    private static QdrantContainer? container;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext _)
    {
        container = new QdrantBuilder("qdrant/qdrant:latest").Build();

        await container.StartAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task UpsertAsync_Then_GetAsync_Roundtrips_RecordAsync()
    {
        Microsoft.Extensions.VectorData.VectorStore vectorStore = BuildVectorStore();
        VectorStoreCollection<Guid, SampleDocument> collection = vectorStore.GetCollection<Guid, SampleDocument>("inkwell-test-documents");

        await collection.EnsureCollectionExistsAsync();

        Guid id = Guid.NewGuid();
        SampleDocument document = new()
        {
            Id = id,
            Title = "H5 编码执行简报",
            Embedding = new ReadOnlyMemory<float>([0.1f, 0.2f, 0.3f, 0.4f]),
        };

        await collection.UpsertAsync(document);

        SampleDocument? retrieved = await collection.GetAsync(id);

        Assert.IsNotNull(retrieved);
        Assert.AreEqual("H5 编码执行简报", retrieved.Title);
    }

    private static Microsoft.Extensions.VectorData.VectorStore BuildVectorStore()
    {
        ServiceCollection services = new();
        services.AddLogging();

        IInkwellBuilder builder = services.AddInkwell(new ConfigurationBuilder().Build());

        Uri grpcEndpoint = new(container!.GetGrpcConnectionString());

        builder.UseQdrantVectorStore(grpcEndpoint.Host, grpcEndpoint.Port);

        ServiceProvider provider = builder.Services.BuildServiceProvider();

        return provider.GetRequiredService<Microsoft.Extensions.VectorData.VectorStore>();
    }

    /// <summary>用于验证向量存储往返读写的最小示例记录。</summary>
    private sealed class SampleDocument
    {
        [VectorStoreKey]
        public Guid Id { get; set; }

        [VectorStoreData]
        public string Title { get; set; } = string.Empty;

        [VectorStoreVector(dimensions: 4)]
        public ReadOnlyMemory<float> Embedding { get; set; }
    }
}
