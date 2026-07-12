// Copyright (c) ShuaiHua Du. All rights reserved.

using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

namespace Inkwell;

/// <summary>
/// 使用 Azure OpenAI Chat Completions 客户端构建 MAF Agent。
/// </summary>
internal sealed class AzureOpenAIAgentFactory
    : IAgentFactory
{
    private readonly AzureOpenAIClient _client;
    private readonly string _defaultDeploymentName;

    /// <summary>
    /// 初始化 <see cref="AzureOpenAIAgentFactory"/>。
    /// </summary>
    /// <param name="credential">Azure OpenAI 连接凭据。</param>
    public AzureOpenAIAgentFactory(AzureOpenAICredential credential)
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
        this._defaultDeploymentName = credential.DeploymentName;
    }

    /// <inheritdoc />
    public ValueTask<AIAgent> BuildAsync(
        AgentVersion agentVersion,
        AgentBuildOptions agentBuildOptions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agentVersion);
        ArgumentNullException.ThrowIfNull(agentBuildOptions);
        cancellationToken.ThrowIfCancellationRequested();

        AgentSnapshot snapshot = agentVersion.Snapshot;
        string modelId = string.IsNullOrWhiteSpace(snapshot.ModelId) ? this._defaultDeploymentName : snapshot.ModelId;

        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new InvalidOperationException("Agent snapshot ModelId or the Azure OpenAI default deployment name is required.");
        }

        AgentModelParameters? parameters = snapshot.ModelParameters;
        ChatOptions chatOptions = new()
        {
            ModelId = modelId,
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

        AIAgent agent = this._client.GetChatClient(modelId).AsAIAgent(options);

        return ValueTask.FromResult(agent);
    }
}