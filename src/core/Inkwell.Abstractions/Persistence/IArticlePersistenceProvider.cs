namespace Inkwell;

/// <summary>
/// 文章持久化提供程序
/// </summary>
public interface IArticlePersistenceProvider : IPersistenceProvider<ArticleRecord, string>
{
    /// <summary>
    /// 根据状态查询文章
    /// </summary>
    /// <param name="status">文章状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的文章集合</returns>
    Task<IReadOnlyList<ArticleRecord>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
}
