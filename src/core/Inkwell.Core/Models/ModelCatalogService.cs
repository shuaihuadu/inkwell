// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Extensions.Options;

namespace Inkwell;

/// <summary><see cref="IModelCatalogService"/> 唯一实现；从 <see cref="IOptions{TOptions}"/> 读取内存快照。</summary>
internal sealed class ModelCatalogService(IOptions<ModelCatalogOptions> options) : IModelCatalogService
{
    public Task<IReadOnlyList<ModelSummary>> ListModelsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult<IReadOnlyList<ModelSummary>>([.. options.Value.Models.Select(MapToSummary)]);
    }

    public Task<ModelSummary> GetModelAsync(string modelId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelId);
        ct.ThrowIfCancellationRequested();

        ModelEntryOptions? entry = options.Value.Models.FirstOrDefault(m => string.Equals(m.Id, modelId, StringComparison.OrdinalIgnoreCase));

        if (entry is null)
        {
            throw new KeyNotFoundException($"Model '{modelId}' is not registered in the catalog.");
        }

        return Task.FromResult(MapToSummary(entry));
    }

    private static ModelSummary MapToSummary(ModelEntryOptions entry) => new()
    {
        Id = entry.Id,
        DisplayName = entry.DisplayName,
        Provider = entry.Provider,
        SupportsVision = entry.SupportsVision,
        IsAvailable = entry.IsAvailable,
    };
}
