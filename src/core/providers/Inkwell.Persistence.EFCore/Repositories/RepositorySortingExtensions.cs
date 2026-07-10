using System.Linq.Expressions;
using Inkwell;

namespace Inkwell.Persistence.EFCore.Repositories;

/// <summary>各 Repository 共用的 <see cref="SortOrder"/> → <see cref="IOrderedQueryable{T}"/> 转换助手。</summary>
internal static class RepositorySortingExtensions
{
    public static IOrderedQueryable<TEntity> ApplySort<TEntity>(this IQueryable<TEntity> query, SortOrder sort, Func<string, Expression<Func<TEntity, object?>>> fieldSelector)
    {
        Expression<Func<TEntity, object?>> keySelector = fieldSelector(sort.Field);

        return sort.Direction == SortDirection.Descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
    }
}
