// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 聊天历史策略配置
/// </summary>
public class AgentChatHistoryOptions
{
    /// <summary>
    /// 获取或设置最大消息数量
    /// </summary>
    public int? MaxMessages { get; set; }

    /// <summary>
    /// 获取或设置消息缩减器类型
    /// </summary>
    public string? ReducerType { get; set; }

    /// <summary>
    /// 获取或设置单次取回消息的数量上限。
    /// </summary>
    public int? MaxMessagesToRetrieve { get; set; }
}
