// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 表示当前 LLM Provider 可公开的管理入口。
/// </summary>
public sealed record class LLMProviderManagementInfo
{
    /// <summary>
    /// 获取 Provider 管理页面地址。
    /// </summary>
    public Uri? DashboardUrl { get; init; }
}