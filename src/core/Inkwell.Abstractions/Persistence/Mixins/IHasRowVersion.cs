// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 实现该 mixin 的业务 Model 会被 EFCore base 自动加 RowVersion 列并启用乐观并发控制。
/// </summary>
public interface IHasRowVersion
{
    /// <summary>
    /// 获取用于乐观并发控制的行版本。
    /// </summary>
    byte[] RowVersion { get; init; }
}
