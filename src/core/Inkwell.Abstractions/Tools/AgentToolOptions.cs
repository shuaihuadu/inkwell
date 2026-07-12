// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

/// <summary>
/// 提供 Agent 工具绑定配置。
/// </summary>
public sealed class AgentToolOptions
{
    /// <summary>
    /// 获取或设置单个 Agent 可绑定的最大工具数量。
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? MaxToolsPerAgent { get; set; }

    /// <summary>
    /// 获取或设置是否启用敏感数据日志记录。
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; }
}
