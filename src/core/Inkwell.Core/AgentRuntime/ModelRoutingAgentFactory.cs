// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Collections.Immutable;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Inkwell;

/// <summary>
/// 通过模型注册表选择运行时连接并构建 MAF Agent。
/// </summary>
internal sealed class ModelRoutingAgentFactory(
    IModelRegistryService modelRegistry,
    IEnumerable<IModelRuntimeChatClientProvider> runtimeClientProviders,
    IPersistenceProvider persistence) : IAgentFactory
{
    /// <inheritdoc />
    public ValueTask<AIAgent> BuildAsync(AgentDefinition agent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);

        return this.BuildCoreAsync(
            agent.Id.ToString(),
            agent.Name,
            agent.Description,
            agent.Instructions,
            agent.BuildOptions,
            cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<AIAgent> BuildAsync(AgentVersion version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);

        AgentSnapshot snapshot = version.Snapshot;

        return this.BuildCoreAsync(
            version.Id.ToString(),
            snapshot.Name,
            snapshot.Description,
            snapshot.Instructions,
            snapshot.BuildOptions,
            cancellationToken);
    }

    private async ValueTask<AIAgent> BuildCoreAsync(
        string id,
        string name,
        string? description,
        string? instructions,
        AgentBuildOptions buildOptions,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string? modelId = buildOptions.ModelOptions.ModelId;
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new InvalidOperationException("Agent ModelId is required.");
        }

        ModelDefinition model = await modelRegistry.GetModelAsync(modelId, cancellationToken).ConfigureAwait(false);
        if (!model.IsAvailable)
        {
            throw new InvalidOperationException(
                $"Model '{model.Id}' is unavailable: {model.UnavailableReason ?? "No reason was provided."}");
        }

        IModelRuntimeChatClientProvider? runtimeClientProvider = runtimeClientProviders.FirstOrDefault(
            provider => string.Equals(provider.RuntimeId, model.RuntimeId, StringComparison.OrdinalIgnoreCase));
        if (runtimeClientProvider is null)
        {
            throw new InvalidOperationException(
                $"No model runtime is registered for RuntimeId '{model.RuntimeId}' used by model '{model.Id}'.");
        }

        AgentModelOptions modelOptions = buildOptions.ModelOptions;
        ChatClientAgentOptions options = new()
        {
            Id = id,
            Name = name,
            Description = description,
            ChatOptions = new ChatOptions
            {
                ModelId = model.RemoteModelId,
                Instructions = instructions,
                Temperature = (float?)modelOptions.Temperature,
                TopP = (float?)modelOptions.TopP,
                MaxOutputTokens = modelOptions.MaxTokens,
            },
            ChatHistoryProvider = this.CreateChatHistoryProvider(buildOptions.ChatHistoryOptions),
            AIContextProviders = CreateContextProviders(buildOptions.Skills),
        };

        IChatClient chatClient = runtimeClientProvider.GetChatClient(model);
        return chatClient.AsAIAgent(options);
    }

    private InkwellChatHistoryProvider? CreateChatHistoryProvider(AgentChatHistoryOptions? options) =>
        options is null
            ? null
            : new InkwellChatHistoryProvider(persistence, options.MaxMessagesToRetrieve);

    private static List<AIContextProvider> CreateContextProviders(
        ImmutableArray<AgentSkillDefinition> skillDefinitions)
    {
        List<AIContextProvider> contextProviders = [];

        if (skillDefinitions.Length > 0)
        {
            List<AgentSkill> skills = skillDefinitions
                .Select(definition => (AgentSkill)new AgentInlineSkill(definition.Name, definition.Description, definition.Content))
                .ToList();
            contextProviders.Add(new AgentSkillsProvider(skills));
        }

        return contextProviders;
    }
}
