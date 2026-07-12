// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Text.Json;

namespace Inkwell;

/// <summary>
/// 会话业务 Model；按 (AgentId, OwnerUserId) 二元组归属一个团队成员与某个 Agent 之间的独立会话历史。
/// <see cref="OwnerUserId"/> 语义 = 会话参与用户，非 <c>AgentDefinition.OwnerUserId</c>（Agent 创建者）。
/// </summary>
public sealed record class AgentSessionDefinition : IHasTimestamps, IHasOwner, IHasRowVersion
{
    /// <summary>获取会话标识。</summary>
    public required Guid Id { get; init; }

    /// <summary>获取会话所属 Agent 标识。</summary>
    public required Guid AgentId { get; init; }

    /// <summary>获取创建会话时锁定的 Agent 版本标识。</summary>
    public required Guid AgentVersionId { get; init; }

    /// <summary>获取会话参与用户标识。</summary>
    public required Guid OwnerUserId { get; init; }

    /// <summary>获取会话标题。</summary>
    public string? Title { get; init; }

    /// <summary>
    /// 获取由对应 <c>AIAgent.SerializeSessionAsync</c> 生成的 MAF Session 状态。
    /// 恢复时必须交由同一 Agent 版本构建出的 <c>AIAgent.DeserializeSessionAsync</c> 处理。
    /// </summary>
    public JsonElement? MafSessionState { get; init; }

    /// <summary>获取创建时间。</summary>
    public required DateTimeOffset CreatedTime { get; init; }

    /// <summary>获取更新时间。</summary>
    public required DateTimeOffset UpdatedTime { get; init; }

    /// <summary>获取乐观并发令牌。</summary>
    public byte[] RowVersion { get; init; } = [];
}
