// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Extensions.AI;

namespace Inkwell;

internal sealed class JsonDelegateAIFunction(
    string name,
    string description,
    string parametersJsonSchema,
    Func<string, CancellationToken, Task<string>> invoke) : AIFunction
{
    private readonly Func<string, CancellationToken, Task<string>> _invoke = invoke ?? throw new ArgumentNullException(nameof(invoke));

    public override string Name { get; } = !string.IsNullOrWhiteSpace(name)
        ? name
        : throw new ArgumentException("Function name is required.", nameof(name));

    public override string Description { get; } = !string.IsNullOrWhiteSpace(description)
        ? description
        : throw new ArgumentException("Function description is required.", nameof(description));

    public override JsonElement JsonSchema { get; } = ParseJsonSchema(parametersJsonSchema);

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        string argumentsJson = JsonSerializer.Serialize((IDictionary<string, object?>)arguments);
        string resultJson = await this._invoke(argumentsJson, cancellationToken).ConfigureAwait(false);

        using JsonDocument resultDocument = JsonDocument.Parse(resultJson);
        return resultDocument.RootElement.Clone();
    }

    private static JsonElement ParseJsonSchema(string parametersJsonSchema)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parametersJsonSchema);

        using JsonDocument schemaDocument = JsonDocument.Parse(parametersJsonSchema);
        return schemaDocument.RootElement.Clone();
    }
}