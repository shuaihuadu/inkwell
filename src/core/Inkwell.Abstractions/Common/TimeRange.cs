// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 时间范围过滤条件，半开区间 [<see cref="Start"/>, <see cref="End"/>)；<c>Start == End</c> 合法。
/// </summary>
/// <param name="Start">范围起始时间。</param>
/// <param name="End">范围结束时间。</param>
public sealed record class TimeRange(DateTimeOffset Start, DateTimeOffset End)
{
    /// <summary>
    /// 获取范围结束时间。
    /// </summary>
    public DateTimeOffset End { get; init; } = End >= Start
        ? End
        : throw new ArgumentException("End must not be before Start.", nameof(End));

    /// <summary>
    /// 获取时间范围的持续时长。
    /// </summary>
    public TimeSpan Duration => this.End - this.Start;

    /// <summary>半开区间 [Start, End) 是否包含 <paramref name="point"/>。</summary>
    /// <param name="point">待检查的时间点。</param>
    /// <returns>包含指定时间点时为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
    public bool Contains(DateTimeOffset point) => point >= this.Start && point < this.End;
}
