// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics;
using System.Net.Http.Json;
using OpenAI;

namespace Inkwell;

/// <summary>
/// 通过 LiteLLM 实时发现模型并创建 OpenAI-compatible 模型客户端。
/// </summary>
public sealed class LiteLLMProvider(HttpClient httpClient, IOptions<LiteLLMOptions> options)
    : ILLMProvider, IChatLLMProvider, IDisposable
{
    private readonly OpenAIClient _openAIClient = CreateOpenAIClient(httpClient, options);

    /// <inheritdoc />
    public LLMProviderManagementInfo GetManagementInfo() => new()
    {
        DashboardUrl = options.Value.DashboardUrl,
    };

    /// <inheritdoc />
    public async Task<IReadOnlyList<LLMModel>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        LiteLLMModels response = await httpClient
            .GetFromJsonAsync<LiteLLMModels>("v1/models", cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("LiteLLM returned an empty model list response.");
        LiteLLMModelGroups groupsResponse = await httpClient
            .GetFromJsonAsync<LiteLLMModelGroups>("model_group/info", cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("LiteLLM returned an empty model group response.");
        Dictionary<string, LiteLLMModelGroupDataItem> groupsById = groupsResponse.Data
            .ToDictionary(group => group.ModelGroup, StringComparer.OrdinalIgnoreCase);

        return [.. response.Data
            .Select(model => MapModel(model, groupsById))
            .OrderBy(model => model.Id, StringComparer.OrdinalIgnoreCase)];
    }

    /// <inheritdoc />
    public async Task<LLMModel> GetModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

        IReadOnlyList<LLMModel> models = await this.ListModelsAsync(cancellationToken).ConfigureAwait(false);
        return models.FirstOrDefault(model => string.Equals(model.Id, modelId, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Model '{modelId}' was not found by the configured LLM provider.");
    }

    /// <inheritdoc />
    public async Task<LLMModelTestResult> TestModelAsync(
        string modelId,
        CancellationToken cancellationToken = default)
    {
        LLMModel model = await this.GetModelAsync(modelId, cancellationToken).ConfigureAwait(false);
        if (model.Category != LLMModelCategory.Chat)
        {
            return new LLMModelTestResult
            {
                ModelId = model.Id,
                IsSuccess = false,
                Latency = TimeSpan.Zero,
                ErrorMessage = "Connectivity testing is currently supported only for chat models.",
            };
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                "v1/chat/completions",
                new
                {
                    model = model.Id,
                    messages = new[] { new { role = "user", content = "Reply with OK." } },
                    max_completion_tokens = 8,
                },
                cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return new LLMModelTestResult
            {
                ModelId = model.Id,
                IsSuccess = true,
                Latency = stopwatch.Elapsed,
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return new LLMModelTestResult
            {
                ModelId = model.Id,
                IsSuccess = false,
                Latency = stopwatch.Elapsed,
                ErrorMessage = "The LLM provider connectivity test failed.",
            };
        }
    }

    /// <inheritdoc />
    public IChatClient CreateChatClient(string modelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

        return this._openAIClient.GetChatClient(modelId).AsIChatClient();
    }

    /// <inheritdoc />
    public void Dispose() => httpClient.Dispose();

    private static OpenAIClient CreateOpenAIClient(
        HttpClient httpClient,
        IOptions<LiteLLMOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        LiteLLMOptions value = options.Value;
        Uri endpoint = new(value.Endpoint, "v1/");
        OpenAIClientOptions clientOptions = new()
        {
            Endpoint = endpoint,
            Transport = new HttpClientPipelineTransport(httpClient),
        };
        return new OpenAIClient(new ApiKeyCredential(value.ApiKey), clientOptions);
    }

    private static LLMModel MapModel(
        LiteLLMModelDataItem model,
        Dictionary<string, LiteLLMModelGroupDataItem> groupsById)
    {
        _ = groupsById.TryGetValue(model.Id, out LiteLLMModelGroupDataItem? group);

        return new LLMModel
        {
            Id = model.Id,
            Category = MapCategory(group?.Mode),
            ProviderMode = group?.Mode,
            OwnedBy = model.OwnedBy,
            MaxInputTokens = NormalizeTokenLimit(group?.MaxInputTokens),
            MaxOutputTokens = NormalizeTokenLimit(group?.MaxOutputTokens),
            SupportsVision = group?.SupportsVision,
            SupportsTools = group?.SupportsFunctionCalling,
            SupportsStructuredOutput = GetSupportsStructuredOutput(group),
            SupportsReasoning = group?.SupportsReasoning,
        };
    }

    private static LLMModelCategory MapCategory(string? mode) => mode?.ToLowerInvariant() switch
    {
        "chat" or "completion" or "responses" => LLMModelCategory.Chat,
        "embedding" => LLMModelCategory.Embedding,
        "image_generation" or "image_edit" => LLMModelCategory.ImageGeneration,
        "video_generation" => LLMModelCategory.VideoGeneration,
        _ => LLMModelCategory.Unknown,
    };

    private static int? NormalizeTokenLimit(double? value) =>
        value is double tokenLimit &&
        double.IsFinite(tokenLimit) &&
        tokenLimit >= 0 &&
        tokenLimit <= int.MaxValue &&
        tokenLimit == Math.Truncate(tokenLimit)
            ? (int)tokenLimit
            : null;

    private static bool? GetSupportsStructuredOutput(LiteLLMModelGroupDataItem? group)
    {
        if (group?.SupportsResponseSchema is not null)
        {
            return group.SupportsResponseSchema;
        }

        return group?.SupportedOpenAIParameters is null
            ? null
            : group.SupportedOpenAIParameters.Contains("response_format", StringComparer.OrdinalIgnoreCase);
    }
}
