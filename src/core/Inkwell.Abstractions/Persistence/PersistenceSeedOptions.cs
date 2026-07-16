// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 持久化 Seed 配置。
/// </summary>
public sealed class PersistenceSeedOptions
{
    /// <summary>
    /// 获取或设置首次创建默认管理员账号时使用的密码。
    /// </summary>
    [Required]
    public string AdminPassword { get; set; } = "admin";
}