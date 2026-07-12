// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 排序字段声明。<paramref name="Field"/> 由具名 Repository 校验合法字段名。
/// </summary>
/// <param name="Field">排序字段名称。</param>
/// <param name="Direction">排序方向。</param>
public sealed record class SortOrder(string Field, SortDirection Direction)
{
    /// <summary>
    /// 获取排序字段名称。
    /// </summary>
    public string Field { get; init; } = !string.IsNullOrWhiteSpace(Field)
        ? Field
        : throw new ArgumentException("Field must not be empty.", nameof(Field));

    /// <summary>
    /// 获取按创建时间降序排列的排序条件。
    /// </summary>
    public static SortOrder ByCreatedAtDesc => new("CreatedTime", SortDirection.Descending);
}
