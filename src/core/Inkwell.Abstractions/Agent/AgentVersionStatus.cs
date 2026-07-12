// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// Agent 版本生命周期状态。
/// </summary>
public enum AgentVersionStatus
{
    /// <summary>
    /// 尚未发布，可用于设计器试运行。
    /// </summary>
    Draft,

    /// <summary>
    /// 已发布，可由正式协议端点调用。
    /// </summary>
    Published,
}
