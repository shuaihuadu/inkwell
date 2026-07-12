// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// Agent 聚合根，管理所有权、共享状态以及当前草稿和已发布版本指针。
/// </summary>
public sealed record class AgentDefinition : IHasOwner, IHasTimestamps, IHasRowVersion
{
    /// <summary>
    /// 获取 Agent 标识。
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// 获取 Agent 所有者用户标识。
    /// </summary>
    public required Guid OwnerUserId { get; init; }

    /// <summary>
    /// 获取当前正式生效的已发布版本标识；尚未首次发布时为 null。
    /// </summary>
    public Guid? CurrentPublishedVersionId { get; init; }

    /// <summary>
    /// 获取当前可编辑草稿版本标识；没有在途编辑时为 null。
    /// </summary>
    public Guid? DraftVersionId { get; init; }

    /// <summary>
    /// 获取最近一次成功发布的版本号；尚未首次发布时为 0。
    /// </summary>
    public int LatestPublishedVersionNumber { get; init; }

    /// <summary>
    /// 获取 Agent 是否对团队共享。
    /// </summary>
    public bool IsShared { get; init; }

    /// <summary>
    /// 获取管理员最近一次强制撤销共享的时间。
    /// </summary>
    public DateTimeOffset? SharedRevokedByAdminTime { get; init; }

    /// <summary>
    /// 获取 Agent 创建时间。
    /// </summary>
    public required DateTimeOffset CreatedTime { get; init; }

    /// <summary>
    /// 获取 Agent 更新时间。
    /// </summary>
    public required DateTimeOffset UpdatedTime { get; init; }

    /// <summary>
    /// 获取用于乐观并发控制的行版本。
    /// </summary>
    public byte[] RowVersion { get; init; } = [];
}

public sealed record class AgentToolBinding(Guid ToolId, string? ParametersJson);

public sealed record class AgentSkillBinding(Guid SkillId);
