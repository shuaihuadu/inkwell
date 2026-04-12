namespace Inkwell;

/// <summary>
/// 审核记录持久化提供程序
/// </summary>
public interface IReviewPersistenceProvider : IPersistenceProvider<ReviewRecord, string>
{
    /// <summary>
    /// 根据文章 ID 获取审核记录
    /// </summary>
    /// <param name="articleId">文章 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审核记录集合</returns>
    Task<IReadOnlyList<ReviewRecord>> GetByArticleIdAsync(string articleId, CancellationToken cancellationToken = default);
}
