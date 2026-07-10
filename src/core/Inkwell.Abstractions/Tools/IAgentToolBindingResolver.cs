// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 把 <see cref="AgentToolBinding"/> 列表解析翻译为 <see cref="AIFunction"/>（经 <c>JsonDelegateAIFunction</c> 桥接）列表。
/// </summary>
public interface IAgentToolBindingResolver
{
    Task<IReadOnlyList<AIFunction>> ResolveAsync(IReadOnlyList<AgentToolBinding> bindings, CancellationToken ct = default);
}
