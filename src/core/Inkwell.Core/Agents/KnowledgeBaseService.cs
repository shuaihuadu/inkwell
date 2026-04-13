using System.Collections.Concurrent;
using Microsoft.Agents.AI;

namespace Inkwell.Agents;

/// <summary>
/// RAG 知识库服务
/// 管理文档的存储和检索，为 Agent 提供知识增强
/// 当前使用简单的关键词匹配检索，后续可接入向量检索
/// </summary>
public sealed class KnowledgeBaseService
{
    private readonly ConcurrentDictionary<string, KnowledgeDocument> _documents = new();

    /// <summary>
    /// 添加文档到知识库
    /// </summary>
    /// <param name="title">文档标题</param>
    /// <param name="content">文档内容</param>
    /// <param name="sourceLink">来源链接</param>
    public void AddDocument(string title, string content, string? sourceLink = null)
    {
        string id = Guid.NewGuid().ToString("N");

        this._documents[id] = new KnowledgeDocument
        {
            Id = id,
            Title = title,
            Content = content,
            SourceLink = sourceLink ?? $"inkwell://kb/{id}",
            AddedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// 删除文档
    /// </summary>
    /// <param name="id">文档 ID</param>
    /// <returns>是否删除成功</returns>
    public bool RemoveDocument(string id)
    {
        return this._documents.TryRemove(id, out _);
    }

    /// <summary>
    /// 获取所有文档
    /// </summary>
    /// <returns>文档列表</returns>
    public IReadOnlyList<KnowledgeDocument> GetAllDocuments()
    {
        return this._documents.Values.OrderByDescending(d => d.AddedAt).ToList().AsReadOnly();
    }

    /// <summary>
    /// 创建用于 Agent 的 TextSearchProvider
    /// </summary>
    /// <param name="topK">每次检索返回的最大文档数</param>
    /// <returns>TextSearchProvider 实例</returns>
    public TextSearchProvider CreateSearchProvider(int topK = 3)
    {
        TextSearchProviderOptions options = new()
        {
            SearchTime = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke,
        };

        return new TextSearchProvider(
            (query, ct) =>
            {
                // 简单关键词匹配检索（后续可替换为向量相似度检索）
                List<TextSearchProvider.TextSearchResult> results = this._documents.Values
                    .Where(d => d.Content.Contains(query, StringComparison.OrdinalIgnoreCase)
                             || d.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Take(topK)
                    .Select(d => new TextSearchProvider.TextSearchResult
                    {
                        SourceName = d.Title,
                        SourceLink = d.SourceLink,
                        Text = d.Content[..Math.Min(d.Content.Length, 2000)]
                    })
                    .ToList();

                return Task.FromResult<IEnumerable<TextSearchProvider.TextSearchResult>>(results);
            },
            options);
    }
}

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
    /// 获取或设置来源链接
    /// </summary>
    public string SourceLink { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置添加时间
    /// </summary>
    public DateTimeOffset AddedAt { get; set; }
}
