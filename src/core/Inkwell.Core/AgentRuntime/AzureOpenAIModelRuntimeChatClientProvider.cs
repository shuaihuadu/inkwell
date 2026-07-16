// Copyright (c) ShuaiHua Du. All rights reserved.

using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;

namespace Inkwell;

/// <summary>
/// 使用 Azure OpenAI Chat Completions 客户端构建 MAF Agent。
/// </summary>
internal sealed class AzureOpenAIModelRuntimeChatClientProvider
    : IModelRuntimeChatClientProvider
{
    private readonly AzureOpenAIClient _client;

    /// <inheritdoc />
    public string RuntimeId => "azure-openai";

    /// <summary>
    /// 初始化 <see cref="AzureOpenAIModelRuntimeChatClientProvider"/>。
    /// </summary>
    /// <param name="credential">Azure OpenAI 连接凭据。</param>
    public AzureOpenAIModelRuntimeChatClientProvider(AzureOpenAICredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);

        if (!Uri.TryCreate(credential.Endpoint, UriKind.Absolute, out Uri? endpoint))
        {
            throw new ArgumentException("Azure OpenAI endpoint must be an absolute URI.", nameof(credential));
        }

        if (string.IsNullOrWhiteSpace(credential.ApiKey))
        {
            throw new ArgumentException("Azure OpenAI API key is required.", nameof(credential));
        }

        this._client = new AzureOpenAIClient(endpoint, new AzureKeyCredential(credential.ApiKey));
    }

    /// <inheritdoc />
    public IChatClient GetChatClient(ModelDefinition model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return this._client.GetChatClient(model.RemoteModelId).AsIChatClient();
    }
}