// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 分页查询结果。
/// </summary>
/// <typeparam name="T">结果项类型。</typeparam>
/// <param name="Items">当前页的结果项。</param>
/// <param name="TotalCount">符合查询条件的结果总数。</param>
/// <param name="Pagination">分页参数。</param>
public sealed record class PagedResult<T>(IReadOnlyList<T> Items, long TotalCount, Pagination Pagination)
{
    /// <summary>
    /// 获取总页数。
    /// </summary>
    public int TotalPages => this.Pagination.PageSize == 0 ? 0 : (int)Math.Ceiling(this.TotalCount / (double)this.Pagination.PageSize);

    /// <summary>
    /// 获取是否存在下一页。
    /// </summary>
    public bool HasNextPage => this.Pagination.Page < this.TotalPages;

    /// <summary>
    /// 获取是否存在上一页。
    /// </summary>
    public bool HasPreviousPage => this.Pagination.Page > 1;
}
