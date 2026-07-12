// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Agents.AI;

namespace Inkwell;

/// <summary>
/// 承载不属于版本快照、需要在每次构建时解析的运行时依赖。
/// </summary>
public sealed class AgentBuildOptions
{
    /// <summary>
    /// 获取或设置聊天历史 Provider；为 null 时由 MAF 使用默认实现。
    /// </summary>
    public ChatHistoryProvider? ChatHistoryProvider { get; set; }

    /// <summary>
    /// 获取或设置已经解析为可执行函数的工具集合。
    /// </summary>
    public IReadOnlyList<AIFunction> Tools { get; set; } = [];
}