// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// Agent 版本的不可变运行配置快照；历史版本不得反向读取当前定义的可变字段。
/// </summary>
public sealed record class AgentSnapshot
{
    /// <summary>
    /// 获取 Agent 名称。
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 获取 Agent 头像地址。
    /// </summary>
    public Uri? AvatarUri { get; init; }

    /// <summary>
    /// 获取 Agent 描述。
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 获取 Agent 系统指令。
    /// </summary>
    public string? Instructions { get; init; }

    /// <summary>
    /// 获取完整、不可变的构建选项。
    /// </summary>
    public required AgentBuildOptions BuildOptions { get; init; }
}
