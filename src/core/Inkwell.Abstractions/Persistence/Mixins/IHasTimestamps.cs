// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 实现该 mixin 的业务 Model 会被 EFCore base 自动加 CreatedTime / UpdatedTime 列并在
/// SaveChanges 拦截器中自动填充（HD-009 §3.3）。
/// </summary>
public interface IHasTimestamps
{
    /// <summary>
    /// 获取创建时间。
    /// </summary>
    DateTimeOffset CreatedTime { get; init; }

    /// <summary>
    /// 获取更新时间。
    /// </summary>
    DateTimeOffset UpdatedTime { get; init; }
}
