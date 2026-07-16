// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>表示消息 Run 批次提交结果。</summary>
public enum AgentChatMessageCommitResult
{
    /// <summary>批次已提交。</summary>
    Committed,

    /// <summary>相同批次此前已完整提交。</summary>
    AlreadyCommitted,

    /// <summary>相同幂等键已保存不同内容。</summary>
    Conflict,
}

/// <summary>表示 Session State 保存结果。</summary>
public enum AgentSessionStateSaveResult
{
    /// <summary>检查点已保存。</summary>
    Saved,

    /// <summary>修订号不连续。</summary>
    ConcurrencyConflict,
}
