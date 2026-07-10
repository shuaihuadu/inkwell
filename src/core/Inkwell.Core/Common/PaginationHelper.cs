
namespace Inkwell;

/// <summary>
/// 循环拉取分页结果直至取尽（<see cref="PagedResult{T}.HasNextPage"/> = <see langword="false"/>）；
/// 收敛各业务服务里重复的"while + Pagination.MaxPageSize"样板代码到唯一位置。
/// </summary>
internal static class PaginationHelper
{
    public static async Task<List<T>> CollectAllAsync<T>(
        Func<Pagination, CancellationToken, Task<PagedResult<T>>> fetchPage,
        CancellationToken ct = default)
    {
        List<T> all = [];
        int page = 1;

        while (true)
        {
            PagedResult<T> result = await fetchPage(new Pagination(page, Pagination.MaxPageSize), ct).ConfigureAwait(false);

            all.AddRange(result.Items);

            if (!result.HasNextPage)
            {
                break;
            }

            page++;
        }

        return all;
    }
}
