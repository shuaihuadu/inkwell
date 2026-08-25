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

    /// <summary>
    /// 获取或设置是否创建 Inkwell 小助手示例数据。
    /// </summary>
    public bool SampleDataEnabled { get; set; }

    /// <summary>
    /// 获取或设置 Inkwell 小助手使用的 LiteLLM Chat 模型标识。
    /// </summary>
    public string AgentModelId { get; set; } = string.Empty;
}