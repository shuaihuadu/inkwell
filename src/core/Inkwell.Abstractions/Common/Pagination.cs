// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 分页请求参数。<see cref="Page"/> 从 1 起。
/// </summary>
public sealed record class Pagination(int Page, int PageSize)
{
    /// <summary>默认单页条数。</summary>
    public const int DefaultPageSize = 20;

    /// <summary>单页最大条数（HD-001 锁定）。</summary>
    public const int MaxPageSize = 100;

    public int Page { get; init; } = Page >= 1
        ? Page
        : throw new ArgumentOutOfRangeException(nameof(Page), Page, "Page must be >= 1.");

    public int PageSize { get; init; } = PageSize is >= 1 and <= MaxPageSize
        ? PageSize
        : throw new ArgumentOutOfRangeException(nameof(PageSize), PageSize, $"PageSize must be between 1 and {MaxPageSize}.");

    public static Pagination Default => new(1, DefaultPageSize);
}
