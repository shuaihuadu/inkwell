// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 标识一个 Agent 的不可变配置版本。
/// </summary>
public sealed record class AgentVersion : IHasTimestamps
{
    /// <summary>
    /// 获取版本记录标识。
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// 获取所属 Agent 标识。
    /// </summary>
    public required Guid AgentId { get; init; }

    /// <summary>
    /// 获取从 1 开始递增的版本号。
    /// </summary>
    public required int VersionNumber { get; init; }

    /// <summary>
    /// 获取该版本固化的完整运行配置。
    /// </summary>
    public required AgentSnapshot Snapshot { get; init; }

    /// <summary>
    /// 获取创建该版本的用户标识。
    /// </summary>
    public required Guid CreatedByUserId { get; init; }

    /// <summary>
    /// 获取版本变更摘要。
    /// </summary>
    public string? ChangeSummary { get; init; }

    /// <summary>
    /// 获取版本创建时间。
    /// </summary>
    public required DateTimeOffset CreatedTime { get; init; }

    /// <summary>
    /// 获取版本更新时间。
    /// </summary>
    public required DateTimeOffset UpdatedTime { get; init; }

    /// <summary>
    /// 获取版本发布时间。
    /// </summary>
    public DateTimeOffset? PublishedTime { get; init; }

}
