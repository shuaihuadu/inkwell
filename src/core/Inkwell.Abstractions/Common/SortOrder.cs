// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 排序字段声明。<paramref name="Field"/> 由具名 Repository 校验合法字段名。
/// </summary>
public sealed record class SortOrder(string Field, SortDirection Direction)
{
    public string Field { get; init; } = !string.IsNullOrWhiteSpace(Field)
        ? Field
        : throw new ArgumentException("Field must not be empty.", nameof(Field));

    public static SortOrder ByCreatedAtDesc => new("CreatedTime", SortDirection.Descending);
}
