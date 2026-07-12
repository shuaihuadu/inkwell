// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 实现该 mixin 的业务 Model 会被 EFCore base 自动加 OwnerUserId 列 + 索引，
/// 并在 SaveChanges 拦截器中校验非空（HD-009 §3.3）。
/// </summary>
public interface IHasOwner
{
    /// <summary>
    /// 获取所有者用户标识。
    /// </summary>
    Guid OwnerUserId { get; init; }
}
