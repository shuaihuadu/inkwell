// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Agents.AI;

namespace Inkwell;

/// <summary>
/// 通过模型注册表选择运行时连接并构建 MAF Agent。
/// </summary>
internal sealed class ModelRoutingAgentFactory(
    IModelRegistryService modelRegistry,
    IEnumerable<IModelRuntimeAgentBuilder> runtimeBuilders) : IAgentFactory
{
    /// <inheritdoc />
    public async ValueTask<AIAgent> BuildAsync(
        AgentVersion agentVersion,
        AgentBuildOptions agentBuildOptions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agentVersion);
        ArgumentNullException.ThrowIfNull(agentBuildOptions);
        cancellationToken.ThrowIfCancellationRequested();

        string? modelId = agentVersion.Snapshot.ModelId;
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new InvalidOperationException("Agent snapshot ModelId is required.");
        }

        ModelDefinition model = await modelRegistry.GetModelAsync(modelId, cancellationToken).ConfigureAwait(false);
        if (!model.IsAvailable)
        {
            throw new InvalidOperationException(
                $"Model '{model.Id}' is unavailable: {model.UnavailableReason ?? "No reason was provided."}");
        }

        IModelRuntimeAgentBuilder? runtimeBuilder = runtimeBuilders.FirstOrDefault(
            builder => string.Equals(builder.RuntimeId, model.RuntimeId, StringComparison.OrdinalIgnoreCase));
        if (runtimeBuilder is null)
        {
            throw new InvalidOperationException(
                $"No model runtime is registered for RuntimeId '{model.RuntimeId}' used by model '{model.Id}'.");
        }

        return runtimeBuilder.Build(model, agentVersion, agentBuildOptions, cancellationToken);
    }
}
