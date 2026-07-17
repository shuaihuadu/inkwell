// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 定义对话模型客户端创建能力。
/// </summary>
public interface IChatLLMProvider
{
    /// <summary>
    /// 为指定模型创建聊天客户端。
    /// </summary>
    /// <param name="modelId">模型标识。</param>
    /// <returns>聊天客户端。</returns>
    IChatClient CreateChatClient(string modelId);
}