// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 把 <see cref="AgentToolBinding"/> 列表解析翻译为 <see cref="AIFunction"/>（经 <c>JsonDelegateAIFunction</c> 桥接）列表。
/// </summary>
public interface IAgentToolBindingResolver
{
    /// <summary>解析 Agent 绑定的工具函数。</summary>
    /// <param name="bindings">工具绑定列表。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>解析后的 AI 函数列表。</returns>
    Task<IReadOnlyList<AIFunction>> ResolveAsync(IReadOnlyList<AgentToolBinding> bindings, CancellationToken ct = default);
}
