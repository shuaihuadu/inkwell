// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 将 Agent 编辑请求中的绑定解析为完整构建选项。
/// </summary>
public interface IAgentBuildOptionsResolver
{
    /// <summary>
    /// 解析 Agent 构建选项。
    /// </summary>
    /// <param name="request">Agent 编辑请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>仅包含 Inkwell 配置与定义的构建选项。</returns>
    Task<AgentBuildOptions> ResolveAsync(AgentUpsertRequest request, CancellationToken cancellationToken = default);
}