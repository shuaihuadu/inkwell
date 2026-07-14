#pragma warning disable IDE0073

using System.Text.Json;
using System.Text.RegularExpressions;

internal static partial class ContainerImageConfiguration
{
    private const string ConfigurationFileName = "container-images.json";
    private const string EnvironmentVariablePrefix = "INKWELL_CONTAINER_IMAGES__";
    private static readonly Dictionary<string, string> values = Load();

    internal static string GetRequired(string key)
    {
        string environmentVariableName = EnvironmentVariablePrefix + key.Replace(":", "__", StringComparison.Ordinal).ToUpperInvariant();
        string? environmentValue = Environment.GetEnvironmentVariable(environmentVariableName);
        string value = string.IsNullOrWhiteSpace(environmentValue)
            ? values.TryGetValue(key, out string? configuredValue)
                ? configuredValue
                : throw new InvalidOperationException($"Container image configuration '{key}' is required.")
            : environmentValue;

        ValidatePinnedReference(key, value);
        return value;
    }

    private static Dictionary<string, string> Load()
    {
        string path = Path.Combine(AppContext.BaseDirectory, ConfigurationFileName);
        using FileStream stream = File.OpenRead(path);
        using JsonDocument document = JsonDocument.Parse(stream);
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        Flatten(document.RootElement, null, values);
        return values;
    }

    private static void Flatten(JsonElement element, string? prefix, IDictionary<string, string> values)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            string key = prefix is null ? property.Name : $"{prefix}:{property.Name}";
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                Flatten(property.Value, key, values);
                continue;
            }

            if (property.Value.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException($"Container image configuration '{key}' must be a string.");
            }

            values.Add(key, property.Value.GetString()!);
        }
    }

    private static void ValidatePinnedReference(string key, string value)
    {
        string tag = value[(value.LastIndexOf(':') + 1)..];
        if (tag.Equals("latest", StringComparison.OrdinalIgnoreCase) || FloatingMajorTag().IsMatch(tag))
        {
            throw new InvalidOperationException($"Container image configuration '{key}' must use a pinned patch, release tag, or digest.");
        }
    }

    [GeneratedRegex("^\\d+(-alpine)?$", RegexOptions.IgnoreCase)]
    private static partial Regex FloatingMajorTag();
}