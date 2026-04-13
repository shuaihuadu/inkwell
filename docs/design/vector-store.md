# Inkwell 向量存储设计方案

## 1. 架构概览

### 1.1 .NET 向量存储抽象体系

.NET 已提供完整的统一抽象层，MAF 和 Semantic Kernel 生态均基于此构建：

```
Microsoft.Extensions.VectorData.Abstractions        ← 统一抽象（微软官方 NuGet 包）
                    │
                    │  定义
                    ▼
    ┌──────────────────────────────────────────┐
    │  VectorStore                              │  顶层入口，管理集合
    │  VectorStoreCollection<TKey, TRecord>     │  单个集合：CRUD + 语义搜索
    │  VectorStoreCollectionDefinition          │  集合 Schema 定义
    │  VectorStoreKeyProperty                   │  键字段
    │  VectorStoreDataProperty                  │  数据字段（支持索引）
    │  VectorStoreVectorProperty                │  向量字段（含维度）
    └──────────────────┬───────────────────────┘
                       │  实现（SK Connectors 生态）
         ┌─────────────┼─────────────┬──────────────┐
         ▼             ▼             ▼              ▼
     InMemory       Qdrant     AzureAISearch    Postgres
                                              (pgvector)  ...
```

### 1.2 核心接口

**`VectorStore`** — 顶层抽象，获取动态类型的集合：

```csharp
// 获取动态集合（ChatHistoryMemoryProvider 使用此方式）
VectorStoreCollection<object, Dictionary<string, object?>> collection =
    vectorStore.GetDynamicCollection(collectionName, definition);
```

**`VectorStoreCollection<TKey, TRecord>`** — 集合操作：

```csharp
await collection.EnsureCollectionExistsAsync();
await collection.UpsertAsync(record);
await foreach (var result in collection.SearchAsync(queryVector, top: 5, options))
{
    // result.Record, result.Score
}
```

**`IEmbeddingGenerator<string, Embedding<float>>`** — Embedding 生成（来自 `Microsoft.Extensions.AI`）：

```csharp
IEmbeddingGenerator<string, Embedding<float>> generator =
    new AzureOpenAIClient(endpoint, credential)
        .GetEmbeddingClient(deploymentName)
        .AsIEmbeddingGenerator();
```

---

## 2. 可用连接器

所有连接器均实现 `Microsoft.Extensions.VectorData.VectorStore` 抽象，通过 NuGet 引用即可切换：

| NuGet 包                                                 | 向量后端            | 适用场景                 |
| -------------------------------------------------------- | ------------------- | ------------------------ |
| `Microsoft.SemanticKernel.Connectors.InMemory`           | 内存                | 开发/测试，零依赖        |
| `Microsoft.SemanticKernel.Connectors.Qdrant`             | Qdrant              | 自托管高性能向量库       |
| `Microsoft.SemanticKernel.Connectors.AzureAISearch`      | Azure AI Search     | Azure 云原生搜索         |
| `Microsoft.SemanticKernel.Connectors.AzureCosmosDBNoSQL` | Cosmos DB NoSQL     | 已有 Cosmos 基础设施     |
| `Microsoft.SemanticKernel.Connectors.Postgres`           | PostgreSQL pgvector | 已有 PG 基础设施         |
| `Microsoft.SemanticKernel.Connectors.Redis`              | Redis               | 已有 Redis，需要极低延迟 |
| `Microsoft.SemanticKernel.Connectors.Pinecone`           | Pinecone            | 托管向量服务             |
| `Microsoft.SemanticKernel.Connectors.Weaviate`           | Weaviate            | 开源向量搜索引擎         |
| `Microsoft.SemanticKernel.Connectors.Sqlite`             | SQLite              | 嵌入式/边缘场景          |

---

## 3. MAF 中的消费者

### 3.1 ChatHistoryMemoryProvider

MAF 的长期记忆组件，直接接受 `VectorStore` 抽象：

```csharp
public ChatHistoryMemoryProvider(
    VectorStore vectorStore,           // 抽象类型，非具体实现
    string collectionName,
    int vectorDimensions,
    Func<AgentSession?, State> stateInitializer,
    ChatHistoryMemoryProviderOptions? options = null)
```

内部自动：
- 通过 `vectorStore.GetDynamicCollection()` 获取集合
- 构建 Schema（Key、Role、Content、ContentEmbedding 等字段）
- 在 `InvokingAsync` 时调用 `collection.SearchAsync()` 语义搜索
- 在 `InvokedAsync` 时调用 `collection.UpsertAsync()` 存储消息

### 3.2 TextSearchProvider

MAF 的 RAG 知识检索组件，不依赖 `VectorStore`，接受一个通用搜索委托：

```csharp
public TextSearchProvider(
    Func<string, CancellationToken, Task<IEnumerable<TextSearchResult>>> searchAsync,
    TextSearchProviderOptions? options = null)
```

可以将任何搜索后端（向量搜索、全文搜索、API 调用）包装为委托注入，灵活度更高。

---

## 4. Inkwell 集成方案

### 4.1 注册模式

采用扩展方法封装 DI 注册，保持与 `UseAzureOpenAI` 一致的风格：

```csharp
public static class VectorStoreServiceCollectionExtensions
{
    /// <summary>
    /// 使用内存向量存储（开发环境）
    /// </summary>
    public static InkwellCoreBuilder UseInMemoryVectorStore(this InkwellCoreBuilder builder)
    {
        builder.Services.AddSingleton<VectorStore>(sp =>
        {
            IEmbeddingGenerator<string, Embedding<float>> embedding =
                sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            return new InMemoryVectorStore(new() { EmbeddingGenerator = embedding });
        });
        return builder;
    }

    /// <summary>
    /// 使用 Qdrant 向量存储（生产环境）
    /// </summary>
    public static InkwellCoreBuilder UseQdrantVectorStore(
        this InkwellCoreBuilder builder, string host, int port = 6334)
    {
        builder.Services.AddSingleton<VectorStore>(sp =>
        {
            IEmbeddingGenerator<string, Embedding<float>> embedding =
                sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            return new QdrantVectorStore(
                new QdrantClient(host, port),
                new() { EmbeddingGenerator = embedding });
        });
        return builder;
    }

    /// <summary>
    /// 使用 Azure AI Search 向量存储（Azure 云环境）
    /// </summary>
    public static InkwellCoreBuilder UseAzureAISearchVectorStore(
        this InkwellCoreBuilder builder, Uri endpoint, TokenCredential credential)
    {
        builder.Services.AddSingleton<VectorStore>(sp =>
        {
            IEmbeddingGenerator<string, Embedding<float>> embedding =
                sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            return new AzureAISearchVectorStore(
                new SearchIndexClient(endpoint, credential),
                new() { EmbeddingGenerator = embedding });
        });
        return builder;
    }

    /// <summary>
    /// 注册 Embedding 生成器（基于 Azure OpenAI）
    /// </summary>
    public static InkwellCoreBuilder UseAzureOpenAIEmbedding(
        this InkwellCoreBuilder builder, IConfiguration configuration)
    {
        AzureOpenAIOptions options = new();
        configuration.GetSection(AzureOpenAIOptions.SectionName).Bind(options);

        builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            AzureOpenAIClient client = string.IsNullOrWhiteSpace(options.Embedding.ApiKey)
                ? new(new Uri(options.Embedding.Endpoint), new AzureCliCredential())
                : new(new Uri(options.Embedding.Endpoint), new AzureKeyCredential(options.Embedding.ApiKey));
            return client
                .GetEmbeddingClient(options.Embedding.DeploymentName)
                .AsIEmbeddingGenerator();
        });
        return builder;
    }
}
```

### 4.2 Program.cs 使用

```csharp
var builder = WebApplication.CreateBuilder(args);

InkwellCoreBuilder coreBuilder = builder.Services
    .AddInkwell()
    .UseAzureOpenAI(builder.Configuration)
    .UseAzureOpenAIEmbedding(builder.Configuration)
    .UseInMemoryVectorStore();   // 开发环境
    // .UseQdrantVectorStore("localhost", 6334)          // 或 Qdrant
    // .UseAzureAISearchVectorStore(endpoint, credential) // 或 Azure AI Search
```

### 4.3 配置结构

扩展现有 `AzureOpenAIOptions`，新增 Embedding 配置段：

```json
{
  "AzureOpenAI": {
    "Primary": { "Endpoint": "...", "DeploymentName": "gpt-4o", "ApiKey": "..." },
    "Secondary": { "Endpoint": "...", "DeploymentName": "gpt-4o-mini" },
    "Embedding": { "Endpoint": "...", "DeploymentName": "text-embedding-3-large", "ApiKey": "..." }
  }
}
```

---

## 5. 设计决策

### 5.1 为什么不自定义 `IVectorProvider`

| 考虑因素         | 结论                                                              |
| ---------------- | ----------------------------------------------------------------- |
| 是否有现成抽象   | `Microsoft.Extensions.VectorData.Abstractions` 是微软官方统一标准 |
| MAF 是否直接使用 | `ChatHistoryMemoryProvider` 构造函数接受 `VectorStore` 抽象类     |
| 连接器生态       | SK Connectors 提供 10+ 实现，均遵循同一抽象                       |
| 自定义收益       | 仅增加一层无意义的转接，不提供额外价值                            |
| 维护成本         | 自定义接口需要跟踪上游抽象变化并适配                              |

直接使用 `VectorStore` 抽象，通过 DI 注入切换实现即可。

### 5.2 封装层次

只封装**注册便利方法**（`UseInMemoryVectorStore` / `UseQdrantVectorStore` 等），不封装抽象接口。这与 Inkwell 现有的 `UseAzureOpenAI`、`UseInMemoryPersistence`、`UseSqlServerPersistence` 风格一致。

### 5.3 Embedding 与向量存储的解耦

`IEmbeddingGenerator` 和 `VectorStore` 分开注册：
- Embedding 模型可以独立切换（OpenAI / Azure OpenAI / 本地模型）
- 向量存储可以独立切换（InMemory / Qdrant / AzureAISearch）
- 连接器在构造时通过 `EmbeddingGenerator` 参数接收嵌入能力

---

## 6. 实施计划

| 步骤 | 任务                                            | 涉及文件                                    |
| ---- | ----------------------------------------------- | ------------------------------------------- |
| 1    | `AzureOpenAIOptions` 新增 `Embedding` 配置段    | `AzureOpenAIOptions.cs`                     |
| 2    | 实现 `UseAzureOpenAIEmbedding` 注册方法         | `AzureOpenAIServiceCollectionExtensions.cs` |
| 3    | 实现 `UseInMemoryVectorStore` 注册方法          | `VectorStoreServiceCollectionExtensions.cs` |
| 4    | `Program.cs` 接入 Embedding + VectorStore       | `Program.cs`                                |
| 5    | 创建 `ChatHistoryMemoryProvider` 并挂载到 Agent | `InkwellAgents.cs`                          |
| 6    | 验证跨 session 语义检索                         | 手动测试                                    |
| 7    | 按需引入生产连接器（Qdrant / AzureAISearch）    | 新增 NuGet 引用 + 注册方法                  |
