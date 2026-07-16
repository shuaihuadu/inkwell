// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Extensions.AI;

namespace Inkwell;

/// <summary>
/// 定义特定模型运行时的 Chat Client 提供程序。
/// </summary>
internal interface IModelRuntimeChatClientProvider
{
    /// <summary>
    /// 获取运行时连接标识。
    /// </summary>
    string RuntimeId { get; }

    /// <summary>
    /// 获取已解析模型对应的 Chat Client。
    /// </summary>
    /// <param name="model">已解析的模型定义。</param>
    /// <returns>模型运行时 Chat Client。</returns>
    IChatClient GetChatClient(ModelDefinition model);
}
