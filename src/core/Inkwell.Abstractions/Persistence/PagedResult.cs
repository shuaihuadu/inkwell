
namespace Inkwell;

/// <summary>
/// 分页查询结果。
/// </summary>
public sealed record class PagedResult<T>(IReadOnlyList<T> Items, long TotalCount, Pagination Pagination)
{
    public int TotalPages => this.Pagination.PageSize == 0 ? 0 : (int)Math.Ceiling(this.TotalCount / (double)this.Pagination.PageSize);

    public bool HasNextPage => this.Pagination.Page < this.TotalPages;

    public bool HasPreviousPage => this.Pagination.Page > 1;

    public static PagedResult<T> Empty(Pagination pagination) => new([], 0, pagination);
}
