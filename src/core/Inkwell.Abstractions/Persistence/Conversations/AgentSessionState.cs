// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 表示与产品会话关联的可丢弃 Agent Session 检查点。
/// </summary>
public sealed record class AgentSessionState
{
    /// <summary>获取关联的产品会话标识。</summary>
    public required Guid ConversationId { get; init; }

    /// <summary>获取绑定 Agent 版本生成的序列化 Session 状态。</summary>
    public required JsonElement SerializedState { get; init; }

    /// <summary>获取从 1 开始严格递增的状态修订号。</summary>
    public required long Revision { get; init; }

    /// <summary>获取该检查点覆盖的最后一个完整成功服务端执行标识。</summary>
    public string? LastRunId { get; init; }

    /// <summary>获取更新时间。</summary>
    public required DateTimeOffset UpdatedTime { get; init; }

}
