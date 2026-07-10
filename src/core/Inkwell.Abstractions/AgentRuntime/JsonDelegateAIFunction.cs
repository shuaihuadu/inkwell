using System.Text.Json;

namespace Inkwell;

/// <summary>
/// 把"工具描述 + 同进程调用委托"包装为真正可被调用的 <see cref="AIFunction"/>（REQ-007），
/// 零 Provider 依赖、零 <c>Microsoft.Agents.AI.*</c> 依赖。
/// </summary>
public sealed class JsonDelegateAIFunction : AIFunction
{
    private readonly Func<string, CancellationToken, Task<string>> _invokeAsync;
    private readonly JsonElement _jsonSchema;

    public JsonDelegateAIFunction(string name, string description, string parametersJsonSchema, Func<string, CancellationToken, Task<string>> invokeAsync)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Name must not be null or empty.", nameof(name));
        }

        if (string.IsNullOrEmpty(description))
        {
            throw new ArgumentException("Description must not be null or empty.", nameof(description));
        }

        if (string.IsNullOrEmpty(parametersJsonSchema))
        {
            throw new ArgumentException("ParametersJsonSchema must not be null or empty.", nameof(parametersJsonSchema));
        }

        ArgumentNullException.ThrowIfNull(invokeAsync);

        this.Name = name;
        this.Description = description;
        this._jsonSchema = JsonDocument.Parse(parametersJsonSchema).RootElement.Clone();
        this._invokeAsync = invokeAsync;
    }

    public override string Name { get; }

    public override string Description { get; }

    public override JsonElement JsonSchema => this._jsonSchema;

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        string argumentsJson = JsonSerializer.Serialize((IDictionary<string, object?>)arguments, this.JsonSerializerOptions);

        string resultJson = await this._invokeAsync(argumentsJson, cancellationToken).ConfigureAwait(false);

        return JsonDocument.Parse(resultJson).RootElement.Clone();
    }
}

