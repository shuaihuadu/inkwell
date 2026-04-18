using Inkwell.Agents;
using Inkwell.Persistence.EntityFrameworkCore;
using Inkwell.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Inkwell.WebApi;

/// <summary>
/// 知识库持久化同步服务
/// 启动时从 DB 加载文档到 KnowledgeBaseService 内存
/// 提供方法将内存中的变更同步到 DB
/// </summary>
public sealed class KnowledgePersistenceService(
    KnowledgeBaseService knowledgeBase,
    IServiceScopeFactory scopeFactory,
    ILogger<KnowledgePersistenceService> logger) : IHostedService
{
    /// <summary>
    /// 启动时从 DB 加载已有文档
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        InkwellDbContext db = scope.ServiceProvider.GetRequiredService<InkwellDbContext>();

        List<KnowledgeDocumentEntity> docs = await db.KnowledgeDocuments
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        List<KnowledgeChunkEntity> allChunks = await db.KnowledgeChunks
            .OrderBy(c => c.ChunkIndex)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, List<KnowledgeChunkEntity>> chunksByDoc = allChunks
            .GroupBy(c => c.DocumentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (KnowledgeDocumentEntity doc in docs)
        {
            // 使用 DB 中的 ID 加载到内存，避免 ID 漂移导致后续切片孤儿
            knowledgeBase.AddDocumentWithId(doc.Id, doc.Title, doc.Content, doc.FileType, doc.SourceLink);
        }

        logger.LogInformation("[Knowledge] Loaded {Count} documents from database", docs.Count);
    }

    /// <summary>
    /// 将文档保存到 DB
    /// </summary>
    public async Task SaveDocumentAsync(
        string documentId,
        string title,
        string content,
        string fileType,
        string? sourceLink,
        IReadOnlyList<KnowledgeChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        InkwellDbContext db = scope.ServiceProvider.GetRequiredService<InkwellDbContext>();

        // 保存文档
        db.KnowledgeDocuments.Add(new KnowledgeDocumentEntity
        {
            Id = documentId,
            Title = title,
            Content = content,
            FileType = fileType,
            SourceLink = sourceLink,
            ChunkCount = chunks.Count,
            CreatedAt = DateTimeOffset.UtcNow
        });

        // 保存切片
        foreach (KnowledgeChunk chunk in chunks)
        {
            db.KnowledgeChunks.Add(new KnowledgeChunkEntity
            {
                Id = chunk.Id,
                DocumentId = chunk.DocumentId,
                ChunkIndex = chunk.ChunkIndex,
                Content = chunk.Content,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("[Knowledge] Saved document {Id}: {Title} ({ChunkCount} chunks) to DB",
            documentId, title, chunks.Count);
    }

    /// <summary>
    /// 从 DB 删除文档及切片
    /// </summary>
    public async Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        InkwellDbContext db = scope.ServiceProvider.GetRequiredService<InkwellDbContext>();

        // 删切片
        List<KnowledgeChunkEntity> chunks = await db.KnowledgeChunks
            .Where(c => c.DocumentId == documentId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (chunks.Count > 0)
        {
            db.KnowledgeChunks.RemoveRange(chunks);
        }

        // 删文档
        KnowledgeDocumentEntity? doc = await db.KnowledgeDocuments
            .FindAsync([documentId], cancellationToken)
            .ConfigureAwait(false);

        if (doc is not null)
        {
            db.KnowledgeDocuments.Remove(doc);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
