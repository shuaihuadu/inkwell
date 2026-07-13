// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>统一模型注册表门面；聚合多个模型来源并提供只读查询。</summary>
public interface IModelRegistryService
{
    /// <summary>
    /// 获取可配置的模型列表。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>模型摘要列表。</returns>
    Task<IReadOnlyList<ModelDefinition>> ListModelsAsync(CancellationToken ct = default);

    /// <summary>
    /// 获取指定模型。
    /// </summary>
    /// <param name="modelId">模型标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>模型摘要。</returns>
    Task<ModelDefinition> GetModelAsync(string modelId, CancellationToken ct = default);
}
