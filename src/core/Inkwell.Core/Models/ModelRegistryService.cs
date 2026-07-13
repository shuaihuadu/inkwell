// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary><see cref="IModelRegistryService"/> 唯一实现；聚合所有已注册模型来源。</summary>
internal sealed class ModelRegistryService(IEnumerable<IModelRegistrySource> sources) : IModelRegistryService
{
    public async Task<IReadOnlyList<ModelDefinition>> ListModelsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        List<ModelDefinition> models = [];

        foreach (IModelRegistrySource source in sources)
        {
            IReadOnlyList<ModelDefinition> sourceModels = await source.ListModelsAsync(ct).ConfigureAwait(false);
            models.AddRange(sourceModels);
        }

        IGrouping<string, ModelDefinition>? duplicate = models
            .GroupBy(model => model.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            string sourceIds = string.Join(", ", duplicate.Select(model => model.SourceId).Distinct(StringComparer.OrdinalIgnoreCase));
            throw new InvalidOperationException($"Duplicate model ID '{duplicate.Key}' was found in sources: {sourceIds}.");
        }

        return models.OrderBy(model => model.DisplayName, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<ModelDefinition> GetModelAsync(string modelId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelId);
        ct.ThrowIfCancellationRequested();

        IReadOnlyList<ModelDefinition> models = await this.ListModelsAsync(ct).ConfigureAwait(false);
        ModelDefinition? model = models.FirstOrDefault(item => string.Equals(item.Id, modelId, StringComparison.OrdinalIgnoreCase));

        if (model is null)
        {
            throw new KeyNotFoundException($"Model '{modelId}' is not registered in the registry.");
        }

        return model;
    }
}
