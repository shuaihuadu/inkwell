// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 定义 LLM Provider 的实时模型发现和连通性测试能力。
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// 获取当前 Provider 可公开的管理入口。
    /// </summary>
    /// <returns>Provider 管理信息。</returns>
    LLMProviderManagementInfo GetManagementInfo();

    /// <summary>
    /// 获取当前 Provider 可访问的模型。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>实时模型列表。</returns>
    Task<IReadOnlyList<LLMModel>> ListModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定模型。
    /// </summary>
    /// <param name="modelId">模型标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>实时模型定义。</returns>
    Task<LLMModel> GetModelAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 测试指定模型的连通性。
    /// </summary>
    /// <param name="modelId">模型标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>连通性测试结果。</returns>
    Task<LLMModelTestResult> TestModelAsync(string modelId, CancellationToken cancellationToken = default);
}