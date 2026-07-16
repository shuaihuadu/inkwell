// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 从服务器配置读取原生模型定义。
/// </summary>
internal sealed class ConfigurationModelRegistrySource(IOptions<ConfigurationModelRegistryOptions> options) : IModelRegistrySource
{
    /// <inheritdoc />
    public string SourceId => "configuration";

    /// <inheritdoc />
    public Task<IReadOnlyList<ModelDefinition>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<ModelDefinition> models = [.. options.Value.Models.Select(entry => new ModelDefinition
        {
            Id = entry.Id,
            DisplayName = entry.DisplayName,
            PublisherId = entry.PublisherId,
            PublisherDisplayName = entry.PublisherDisplayName,
            FamilyId = entry.FamilyId,
            FamilyDisplayName = entry.FamilyDisplayName,
            SourceId = this.SourceId,
            RuntimeId = entry.RuntimeId,
            RemoteModelId = entry.RemoteModelId,
            SupportsVision = entry.SupportsVision,
            SupportsTools = entry.SupportsTools,
            SupportsStructuredOutput = entry.SupportsStructuredOutput,
            ContextWindowTokens = entry.ContextWindowTokens,
            IsAvailable = entry.IsAvailable,
            UnavailableReason = entry.UnavailableReason,
        })];

        return Task.FromResult(models);
    }
}
