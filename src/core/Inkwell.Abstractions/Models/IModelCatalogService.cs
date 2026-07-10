// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>模型目录顶层业务门面；配置文件驱动的只读查询，非持久化实体。</summary>
public interface IModelCatalogService
{
    Task<IReadOnlyList<ModelSummary>> ListModelsAsync(CancellationToken ct = default);

    Task<ModelSummary> GetModelAsync(string modelId, CancellationToken ct = default);
}
