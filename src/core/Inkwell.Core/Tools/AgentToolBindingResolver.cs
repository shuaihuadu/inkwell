// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace Inkwell;

/// <summary><see cref="IAgentToolBindingResolver"/> 唯一实现；<see cref="AgentToolBinding"/> → <see cref="AIFunction"/> 翻译 + 执行委托合并逻辑。</summary>
internal sealed class AgentToolBindingResolver(
    IAgentToolRepository tools,
    IReadOnlyDictionary<Guid, Func<string, CancellationToken, Task<string>>> toolExecutors) : IAgentToolBindingResolver
{
    public async Task<IReadOnlyList<AIFunction>> ResolveAsync(IReadOnlyList<AgentToolBinding> bindings, CancellationToken ct = default)
    {
        if (bindings.Count == 0)
        {
            return [];
        }

        List<AIFunction> resolved = [];

        foreach (AgentToolBinding binding in bindings)
        {
            AgentToolDefinition tool = await tools.GetTool(binding.ToolId, ct).ConfigureAwait(false);

            if (!toolExecutors.TryGetValue(binding.ToolId, out Func<string, CancellationToken, Task<string>>? invoke))
            {
                throw new KeyNotFoundException($"No tool executor registered for tool '{tool.Name}' ({binding.ToolId}).");
            }

            Func<string, CancellationToken, Task<string>> mergedInvoke = BuildInvokeDelegate(invoke, binding.ParametersJson);
        }

        return resolved;
    }

    private static Func<string, CancellationToken, Task<string>> BuildInvokeDelegate(Func<string, CancellationToken, Task<string>> invoke, string? boundParametersJson) =>
        (modelArgumentsJson, ct) => invoke(MergeParameters(modelArgumentsJson, boundParametersJson), ct);

    /// <summary>绑定的静态参数优先于模型运行时生成的同名参数。</summary>
    private static string MergeParameters(string modelArgumentsJson, string? boundParametersJson)
    {
        JsonObject merged = JsonNode.Parse(modelArgumentsJson)?.AsObject() ?? [];

        if (!string.IsNullOrEmpty(boundParametersJson))
        {
            JsonObject? bound = JsonNode.Parse(boundParametersJson)?.AsObject();

            if (bound is not null)
            {
                foreach ((string? key, JsonNode? value) in bound)
                {
                    merged[key] = value?.DeepClone();
                }
            }
        }

        return merged.ToJsonString();
    }
}
