// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace Inkwell;

/// <summary>
/// 使用 LiteLLM OpenAI-compatible Chat Completions 构建 MAF Agent。
/// </summary>
internal sealed class LiteLLMModelRuntimeAgentBuilder : IModelRuntimeAgentBuilder
{
    private readonly OpenAIClient _client;

    /// <summary>
    /// 初始化 <see cref="LiteLLMModelRuntimeAgentBuilder"/>。
    /// </summary>
    /// <param name="options">LiteLLM 连接配置。</param>
    public LiteLLMModelRuntimeAgentBuilder(IOptions<LiteLLMModelRegistryOptions> options)
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
    public AIAgent Build(
        ModelDefinition model,
        AgentVersion agentVersion,
        AgentBuildOptions agentBuildOptions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(agentVersion);
        ArgumentNullException.ThrowIfNull(agentBuildOptions);
        cancellationToken.ThrowIfCancellationRequested();

        AgentSnapshot snapshot = agentVersion.Snapshot;
        AgentModelParameters? parameters = snapshot.ModelParameters;
        ChatOptions chatOptions = new()
        {
            ModelId = model.RemoteModelId,
            Instructions = snapshot.Instructions,
            Temperature = (float?)parameters?.Temperature,
            TopP = (float?)parameters?.TopP,
            MaxOutputTokens = parameters?.MaxTokens,
            Tools = [.. agentBuildOptions.Tools],
        };
        ChatClientAgentOptions options = new()
        {
            Id = agentVersion.Id.ToString(),
            Name = snapshot.Name,
            Description = snapshot.Description,
            ChatOptions = chatOptions,
            ChatHistoryProvider = agentBuildOptions.ChatHistoryProvider,
        };

        return this._client.GetChatClient(model.RemoteModelId).AsAIAgent(options);
    }
}
