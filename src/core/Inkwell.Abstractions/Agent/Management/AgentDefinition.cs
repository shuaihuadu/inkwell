// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// Agent 聚合根，管理所有权、共享状态以及当前草稿和已发布版本指针。
/// </summary>
public sealed record class AgentDefinition : IHasOwner, IHasTimestamps
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
    /// 获取 Agent 构建选项。
    /// </summary>
    public required AgentBuildOptions BuildOptions { get; init; }

    /// <summary>
    /// 获取当前正式生效的已发布版本标识；尚未首次发布时为 null。
    /// </summary>
    public Guid? CurrentPublishedVersionId { get; init; }

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

}

/// <summary>
/// 表示 Agent 绑定的工具及其参数配置。
/// </summary>
/// <param name="ToolId">工具标识。</param>
/// <param name="ParametersJson">工具参数的 JSON；未配置时为 <see langword="null"/>。</param>
public sealed record class AgentToolBinding(Guid ToolId, string? ParametersJson);

/// <summary>
/// 表示 Agent 绑定的 Skill。
/// </summary>
/// <param name="SkillId">Skill 标识。</param>
public sealed record class AgentSkillBinding(Guid SkillId);
