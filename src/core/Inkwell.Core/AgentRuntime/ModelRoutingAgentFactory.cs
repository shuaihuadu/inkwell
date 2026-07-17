// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Collections.Immutable;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Inkwell;

/// <summary>
/// 通过当前 LLM Provider 构建 MAF Agent。
/// </summary>
internal sealed class ModelRoutingAgentFactory(
    ILLMProvider llmProvider,
    IChatLLMProvider chatLLMProvider,
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

        LLMModel model = await llmProvider.GetModelAsync(modelId, cancellationToken).ConfigureAwait(false);
        if (model.Category != LLMModelCategory.Chat)
        {
            throw new InvalidOperationException(
                $"Model '{model.Id}' cannot be used by a chat agent because its category is '{model.Category}'.");
        }

        AgentModelOptions modelOptions = buildOptions.ModelOptions;
        ChatClientAgentOptions options = new()
        {
            Id = id,
            Name = name,
            Description = description,
            ChatOptions = new ChatOptions
            {
                ModelId = model.Id,
                Instructions = instructions,
                Temperature = (float?)modelOptions.Temperature,
                TopP = (float?)modelOptions.TopP,
                MaxOutputTokens = modelOptions.MaxTokens,
            },
            ChatHistoryProvider = this.CreateChatHistoryProvider(buildOptions.ChatHistoryOptions),
            AIContextProviders = CreateContextProviders(buildOptions.Skills),
        };

        IChatClient chatClient = chatLLMProvider.CreateChatClient(model.Id);
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
