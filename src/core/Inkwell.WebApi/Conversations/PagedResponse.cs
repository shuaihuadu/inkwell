// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.WebApi.Conversations;

/// <summary>表示 API 分页响应。</summary>
/// <typeparam name="TItem">列表项类型。</typeparam>
public sealed record class PagedResponse<TItem>
{
    /// <summary>获取当前页项目。</summary>
    public required IReadOnlyList<TItem> Items { get; init; }

    /// <summary>获取总项目数。</summary>
    public required long TotalCount { get; init; }

    /// <summary>获取当前页码。</summary>
    public required int Page { get; init; }

    /// <summary>获取每页条数。</summary>
    public required int PageSize { get; init; }
}