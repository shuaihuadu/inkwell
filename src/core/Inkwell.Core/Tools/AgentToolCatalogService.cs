using System.Text.Json.Nodes;

namespace Inkwell;

/// <summary><see cref="IAgentToolCatalogService"/> 唯一实现；只读查询 + 绑定参数校验。</summary>
internal sealed class AgentToolCatalogService(IAgentToolRepository tools) : IAgentToolCatalogService
{
    public async Task<IReadOnlyList<AgentToolDefinition>> ListAvailableToolsAsync(CancellationToken ct = default)
    {
        List<AgentToolDefinition> all = await PaginationHelper.CollectAllAsync(
            (pagination, innerCt) => tools.ListTools(pagination, SortOrder.ByCreatedAtDesc, innerCt),
            ct).ConfigureAwait(false);

        return all;
    }

    public async Task<AgentToolDefinition> GetToolAsync(Guid toolId, CancellationToken ct = default) =>
        await tools.GetTool(toolId, ct).ConfigureAwait(false);

    public async Task ValidateToolBindingAsync(Guid toolId, string? parametersJson, CancellationToken ct = default)
    {
        AgentToolDefinition tool = await this.GetToolAsync(toolId, ct).ConfigureAwait(false);
        IReadOnlyCollection<string> requiredFields = ExtractRequiredFields(tool.ParametersJsonSchema);
        IReadOnlySet<string> providedFields = ParseProvidedFieldNames(parametersJson);

        foreach (string field in requiredFields)
        {
            if (!providedFields.Contains(field))
            {
                throw new ArgumentException($"Tool '{tool.Name}' is missing required parameter: '{field}'.");
            }
        }
    }

    private static IReadOnlyCollection<string> ExtractRequiredFields(string parametersJsonSchema)
    {
        JsonNode? schema = JsonNode.Parse(parametersJsonSchema);
        JsonArray? required = schema?["required"]?.AsArray();

        if (required is null)
        {
            return [];
        }

        return [.. required.Select(n => n!.GetValue<string>())];
    }

    private static IReadOnlySet<string> ParseProvidedFieldNames(string? parametersJson)
    {
        if (string.IsNullOrEmpty(parametersJson))
        {
            return new HashSet<string>();
        }

        JsonObject? obj = JsonNode.Parse(parametersJson)?.AsObject();

        if (obj is null)
        {
            return new HashSet<string>();
        }

        return obj.Where(kvp => kvp.Value is not null).Select(kvp => kvp.Key).ToHashSet();
    }
}
