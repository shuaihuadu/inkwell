// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>模型目录顶层业务门面；配置文件驱动的只读查询，非持久化实体。</summary>
public interface IModelCatalogService
{
    /// <summary>
    /// 获取可配置的模型列表。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>模型摘要列表。</returns>
    Task<IReadOnlyList<ModelSummary>> ListModelsAsync(CancellationToken ct = default);

    /// <summary>
    /// 获取指定模型。
    /// </summary>
    /// <param name="modelId">模型标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>模型摘要。</returns>
    Task<ModelSummary> GetModelAsync(string modelId, CancellationToken ct = default);
}
