// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 聊天历史策略配置。
/// </summary>
public sealed record class AgentChatHistoryOptions
{
    /// <summary>
    /// 获取最大消息数量。
    /// </summary>
    public int? MaxMessages { get; init; }

    /// <summary>
    /// 获取消息缩减器类型。
    /// </summary>
    public string? ReducerType { get; init; }

    /// <summary>
    /// 获取单次取回消息的数量上限。
    /// </summary>
    public int? MaxMessagesToRetrieve { get; init; }
}
