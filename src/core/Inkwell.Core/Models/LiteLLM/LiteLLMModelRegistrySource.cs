// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Inkwell;

/// <summary>
/// 通过 LiteLLM OpenAI 兼容接口自动发现模型。
/// </summary>
internal sealed class LiteLLMModelRegistrySource(HttpClient httpClient, IOptions<LiteLLMModelRegistryOptions> options) : IModelRegistrySource
{
    /// <inheritdoc />
    public string SourceId => "litellm";

    /// <inheritdoc />
    public async Task<IReadOnlyList<ModelDefinition>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        LiteLLMModelsResponse? response = await httpClient
            .GetFromJsonAsync<LiteLLMModelsResponse>("v1/models", cancellationToken)
            .ConfigureAwait(false);

        if (response is null)
        {
            throw new InvalidOperationException("LiteLLM returned an empty model list response.");
        }

        Dictionary<string, LiteLLMModelMetadataOptions> metadataById = options.Value.Models
            .ToDictionary(model => model.Id, StringComparer.OrdinalIgnoreCase);

        return [.. response.Data.Select(model => this.MapModel(model, metadataById))];
    }

    private ModelDefinition MapModel(
        LiteLLMModelResponse model,
        Dictionary<string, LiteLLMModelMetadataOptions> metadataById)
    {
        if (!metadataById.TryGetValue(model.Id, out LiteLLMModelMetadataOptions? metadata))
        {
            return new ModelDefinition
            {
                Id = model.Id,
                DisplayName = model.Id,
                PublisherId = model.OwnedBy,
                PublisherDisplayName = model.OwnedBy,
                SourceId = this.SourceId,
                RuntimeId = "litellm",
                RemoteModelId = model.Id,
                IsAvailable = false,
                UnavailableReason = "Model metadata has not been configured in Inkwell.",
            };
        }

        bool hasRequiredMetadata = !string.IsNullOrWhiteSpace(metadata.PublisherId)
            && !string.IsNullOrWhiteSpace(metadata.PublisherDisplayName)
            && !string.IsNullOrWhiteSpace(metadata.FamilyId)
            && !string.IsNullOrWhiteSpace(metadata.FamilyDisplayName);
        bool isAvailable = metadata.IsEnabled && hasRequiredMetadata;

        return new ModelDefinition
        {
            Id = model.Id,
            DisplayName = metadata.DisplayName ?? model.Id,
            PublisherId = metadata.PublisherId,
            PublisherDisplayName = metadata.PublisherDisplayName,
            FamilyId = metadata.FamilyId,
            FamilyDisplayName = metadata.FamilyDisplayName,
            SourceId = this.SourceId,
            RuntimeId = "litellm",
            RemoteModelId = model.Id,
            SupportsVision = metadata.SupportsVision,
            SupportsTools = metadata.SupportsTools,
            SupportsStructuredOutput = metadata.SupportsStructuredOutput,
            ContextWindowTokens = metadata.ContextWindowTokens,
            IsAvailable = isAvailable,
            UnavailableReason = isAvailable
                ? null
                : hasRequiredMetadata
                    ? "Model is disabled in Inkwell configuration."
                    : "Model publisher and family metadata are incomplete.",
        };
    }
}
