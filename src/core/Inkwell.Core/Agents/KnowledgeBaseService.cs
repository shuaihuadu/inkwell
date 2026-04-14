using System.Collections.Concurrent;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace Inkwell.Agents;

/// <summary>
/// 知识库文档
/// </summary>
public sealed class KnowledgeDocument
{
    /// <summary>
    /// 获取或设置文档 ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文档标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文档内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置文件类型（txt / md）
    /// </summary>
    public string FileType { get; set; } = "txt";

    /// <summary>
    /// 获取或设置来源链接
    /// </summary>
    public string SourceLink { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置切片数量
    /// </summary>
    public int ChunkCount { get; set; }

    /// <summary>
    /// 获取或设置添加时间
    /// </summary>
    public DateTimeOffset AddedAt { get; set; }
}

/// <summary>
/// 知识库切片
/// </summary>
public sealed class KnowledgeChunk
{
    /// <summary>
    /// 获取或设置切片 ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置所属文档 ID
    /// </summary>
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置切片序号
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// 获取或设置切片内容
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// RAG 知识库服务
/// 管理文档的存储、切片和检索，为 Agent 提供知识增强
/// 支持关键词检索和向量语义检索双模式
/// </summary>
public sealed class KnowledgeBaseService
{
    private readonly ConcurrentDictionary<string, KnowledgeDocument> _documents = new();
    private readonly ConcurrentDictionary<string, List<KnowledgeChunk>> _chunks = new();
    private readonly VectorStore? _vectorStore;
    private IEmbeddingGenerator<string, Embedding<float>>? _embeddingGenerator;

    /// <summary>
    /// 默认切片大小（字符数）
    /// </summary>
    private const int DefaultChunkSize = 500;

    /// <summary>
    /// 切片重叠大小（字符数）
    /// </summary>
    private const int DefaultChunkOverlap = 50;

    /// <summary>
    /// 向量集合名称
    /// </summary>
    private const string VectorCollectionName = "inkwell_knowledge";

    /// <summary>
    /// 初始化知识库服务（无向量存储，仅关键词检索）
    /// </summary>
    public KnowledgeBaseService()
    {
    }

    /// <summary>
    /// 初始化知识库服务（带向量存储，支持语义检索）
    /// </summary>
    /// <param name="vectorStore">向量存储实例</param>
    /// <param name="embeddingGenerator">嵌入生成器（可为 null，运行时可通过 SetEmbeddingGenerator 注入）</param>
    public KnowledgeBaseService(
        VectorStore vectorStore,
        IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator = null)
    {
        this._vectorStore = vectorStore;
        this._embeddingGenerator = embeddingGenerator;
    }

    /// <summary>
    /// 运行时注入 EmbeddingGenerator（用于 DI 延迟解析场景）
    /// </summary>
    public void SetEmbeddingGenerator(IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        this._embeddingGenerator = generator;
    }

    /// <summary>
    /// 是否支持向量检索
    /// </summary>
    public bool IsVectorSearchEnabled => this._vectorStore is not null && this._embeddingGenerator is not null;

    /// <summary>
    /// 添加文档到知识库（自动切片，如有向量存储则做 embedding）
    /// </summary>
    /// <param name="title">文档标题</param>
    /// <param name="content">文档内容</param>
    /// <param name="fileType">文件类型（txt / md）</param>
    /// <param name="sourceLink">来源链接</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文档 ID</returns>
    public async Task<string> AddDocumentAsync(
        string title,
        string content,
        string fileType = "txt",
        string? sourceLink = null,
        CancellationToken cancellationToken = default)
    {
        string id = Guid.NewGuid().ToString("N");

        // 切片
        List<KnowledgeChunk> chunks = ChunkText(id, content);

        KnowledgeDocument doc = new()
        {
            Id = id,
            Title = title,
            Content = content,
            FileType = fileType,
            SourceLink = sourceLink ?? $"inkwell://kb/{id}",
            ChunkCount = chunks.Count,
            AddedAt = DateTimeOffset.UtcNow
        };

        this._documents[id] = doc;
        this._chunks[id] = chunks;

        // 如果有向量存储，做 embedding
        if (this.IsVectorSearchEnabled)
        {
            await EmbedChunksAsync(chunks, cancellationToken).ConfigureAwait(false);
        }

        return id;
    }

    /// <summary>
    /// 同步添加文档（向后兼容，不做 embedding）
    /// </summary>
    public void AddDocument(string title, string content, string? sourceLink = null)
    {
        string id = Guid.NewGuid().ToString("N");
        List<KnowledgeChunk> chunks = ChunkText(id, content);

        this._documents[id] = new KnowledgeDocument
        {
            Id = id,
            Title = title,
            Content = content,
            FileType = "txt",
            SourceLink = sourceLink ?? $"inkwell://kb/{id}",
            ChunkCount = chunks.Count,
            AddedAt = DateTimeOffset.UtcNow
        };
        this._chunks[id] = chunks;
    }

    /// <summary>
    /// 从文件内容添加文档（支持 txt 和 md）
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="fileContent">文件文本内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文档 ID</returns>
    public Task<string> AddFromFileAsync(
        string fileName,
        string fileContent,
        CancellationToken cancellationToken = default)
    {
        string ext = Path.GetExtension(fileName).ToLowerInvariant();
        string fileType = ext switch
        {
            ".md" or ".markdown" => "md",
            ".txt" or ".text" => "txt",
            _ => "txt"
        };

        string title = Path.GetFileNameWithoutExtension(fileName);

        return this.AddDocumentAsync(title, fileContent, fileType, $"file://{fileName}", cancellationToken);
    }

    /// <summary>
    /// 删除文档及其切片
    /// </summary>
    public bool RemoveDocument(string id)
    {
        this._chunks.TryRemove(id, out _);
        return this._documents.TryRemove(id, out _);
    }

    /// <summary>
    /// 获取所有文档
    /// </summary>
    public IReadOnlyList<KnowledgeDocument> GetAllDocuments()
    {
        return this._documents.Values.OrderByDescending(d => d.AddedAt).ToList().AsReadOnly();
    }

    /// <summary>
    /// 获取文档的切片
    /// </summary>
    public IReadOnlyList<KnowledgeChunk> GetChunks(string documentId)
    {
        return this._chunks.TryGetValue(documentId, out List<KnowledgeChunk>? chunks)
            ? chunks.AsReadOnly()
            : (IReadOnlyList<KnowledgeChunk>)Array.Empty<KnowledgeChunk>();
    }

    /// <summary>
    /// 创建用于 Agent 的 TextSearchProvider
    /// 优先使用向量语义检索，回退到关键词匹配
    /// </summary>
    public TextSearchProvider CreateSearchProvider(int topK = 3)
    {
        TextSearchProviderOptions options = new()
        {
            SearchTime = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke,
        };

        return new TextSearchProvider(
            async (query, ct) =>
            {
                // 优先向量检索
                if (this.IsVectorSearchEnabled)
                {
                    IReadOnlyList<TextSearchProvider.TextSearchResult> vectorResults =
                        await VectorSearchAsync(query, topK, ct).ConfigureAwait(false);

                    if (vectorResults.Count > 0)
                    {
                        return vectorResults;
                    }
                }

                // 回退到关键词匹配
                return this._chunks.Values
                    .SelectMany(c => c)
                    .Where(chunk => chunk.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Take(topK)
                    .Select(chunk =>
                    {
                        this._documents.TryGetValue(chunk.DocumentId, out KnowledgeDocument? doc);
                        return new TextSearchProvider.TextSearchResult
                        {
                            SourceName = doc?.Title ?? chunk.DocumentId,
                            SourceLink = doc?.SourceLink ?? "",
                            Text = chunk.Content
                        };
                    })
                    .ToList();
            },
            options);
    }

    /// <summary>
    /// 文本切片：按固定大小 + 重叠窗口
    /// </summary>
    private static List<KnowledgeChunk> ChunkText(
        string documentId,
        string text,
        int chunkSize = DefaultChunkSize,
        int overlap = DefaultChunkOverlap)
    {
        List<KnowledgeChunk> chunks = [];

        if (string.IsNullOrWhiteSpace(text))
        {
            return chunks;
        }

        int index = 0;
        int position = 0;

        while (position < text.Length)
        {
            int length = Math.Min(chunkSize, text.Length - position);
            string chunkContent = text.Substring(position, length).Trim();

            if (!string.IsNullOrWhiteSpace(chunkContent))
            {
                chunks.Add(new KnowledgeChunk
                {
                    Id = $"{documentId}-{index}",
                    DocumentId = documentId,
                    ChunkIndex = index,
                    Content = chunkContent
                });

                index++;
            }

            position += chunkSize - overlap;
        }

        return chunks;
    }

    /// <summary>
    /// 将切片做 embedding 并写入向量存储
    /// </summary>
    private async Task EmbedChunksAsync(
        List<KnowledgeChunk> chunks,
        CancellationToken cancellationToken)
    {
        if (this._embeddingGenerator is null || this._vectorStore is null)
        {
            return;
        }

        // 生成 embeddings
        string[] texts = chunks.Select(c => c.Content).ToArray();
        GeneratedEmbeddings<Embedding<float>> embeddings = await this._embeddingGenerator
            .GenerateAsync(texts, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // 写入向量存储
        VectorStoreCollectionDefinition definition = new()
        {
            Properties =
            [
                new VectorStoreKeyProperty("Key"),
                new VectorStoreDataProperty("DocumentId") { IsIndexed = true },
                new VectorStoreDataProperty("ChunkIndex"),
                new VectorStoreDataProperty("Content"),
                new VectorStoreVectorProperty("ContentEmbedding", embeddings[0].Vector.Length)
            ]
        };

        VectorStoreCollection<object, Dictionary<string, object?>> collection =
            this._vectorStore.GetDynamicCollection(VectorCollectionName, definition);

        await collection.EnsureCollectionExistsAsync(cancellationToken).ConfigureAwait(false);

        for (int i = 0; i < chunks.Count; i++)
        {
            Dictionary<string, object?> record = new()
            {
                ["Key"] = chunks[i].Id,
                ["DocumentId"] = chunks[i].DocumentId,
                ["ChunkIndex"] = chunks[i].ChunkIndex,
                ["Content"] = chunks[i].Content,
                ["ContentEmbedding"] = embeddings[i].Vector
            };

            await collection.UpsertAsync(record, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 向量语义检索
    /// </summary>
    private async Task<IReadOnlyList<TextSearchProvider.TextSearchResult>> VectorSearchAsync(
        string query,
        int topK,
        CancellationToken cancellationToken)
    {
        if (this._embeddingGenerator is null || this._vectorStore is null)
        {
            return Array.Empty<TextSearchProvider.TextSearchResult>();
        }

        VectorStoreCollectionDefinition definition = new()
        {
            Properties =
            [
                new VectorStoreKeyProperty("Key"),
                new VectorStoreDataProperty("DocumentId") { IsIndexed = true },
                new VectorStoreDataProperty("ChunkIndex"),
                new VectorStoreDataProperty("Content"),
                new VectorStoreVectorProperty("ContentEmbedding", 1536)
            ]
        };

        VectorStoreCollection<object, Dictionary<string, object?>> collection =
            this._vectorStore.GetDynamicCollection(VectorCollectionName, definition);

        bool exists = await collection.CollectionExistsAsync(cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            return Array.Empty<TextSearchProvider.TextSearchResult>();
        }

        List<TextSearchProvider.TextSearchResult> results = [];

        await foreach (VectorSearchResult<Dictionary<string, object?>> result in
            collection.SearchAsync(query, top: topK, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            string content = result.Record["Content"]?.ToString() ?? "";
            string docId = result.Record["DocumentId"]?.ToString() ?? "";

            this._documents.TryGetValue(docId, out KnowledgeDocument? doc);

            results.Add(new TextSearchProvider.TextSearchResult
            {
                SourceName = doc?.Title ?? docId,
                SourceLink = doc?.SourceLink ?? "",
                Text = content
            });
        }

        return results;
    }
}
