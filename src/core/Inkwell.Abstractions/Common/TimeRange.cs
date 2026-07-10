// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 时间范围过滤条件，半开区间 [<see cref="Start"/>, <see cref="End"/>)；<c>Start == End</c> 合法。
/// </summary>
public sealed record class TimeRange(DateTimeOffset Start, DateTimeOffset End)
{
    public DateTimeOffset End { get; init; } = End >= Start
        ? End
        : throw new ArgumentException("End must not be before Start.", nameof(End));

    public TimeSpan Duration => this.End - this.Start;

    /// <summary>半开区间 [Start, End) 是否包含 <paramref name="point"/>。</summary>
    public bool Contains(DateTimeOffset point) => point >= this.Start && point < this.End;
}
