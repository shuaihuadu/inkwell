// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Inkwell;

/// <summary>
/// 使用 LiteLLM OpenAI-compatible Chat Completions 构建 MAF Agent。
/// </summary>
internal sealed class LiteLLMModelRuntimeChatClientProvider : IModelRuntimeChatClientProvider
{
    private readonly OpenAIClient _client;

    /// <summary>
    /// 初始化 <see cref="LiteLLMModelRuntimeChatClientProvider"/>。
    /// </summary>
    /// <param name="options">LiteLLM 连接配置。</param>
    public LiteLLMModelRuntimeChatClientProvider(IOptions<LiteLLMModelRegistryOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        LiteLLMModelRegistryOptions value = options.Value;
        Uri openAIEndpoint = new(value.Endpoint, "v1/");
        OpenAIClientOptions clientOptions = new() { Endpoint = openAIEndpoint };
        this._client = new OpenAIClient(new ApiKeyCredential(value.ApiKey), clientOptions);
    }

    /// <inheritdoc />
    public string RuntimeId => "litellm";

    /// <inheritdoc />
    public IChatClient GetChatClient(ModelDefinition model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return this._client.GetChatClient(model.RemoteModelId).AsIChatClient();
    }
}
