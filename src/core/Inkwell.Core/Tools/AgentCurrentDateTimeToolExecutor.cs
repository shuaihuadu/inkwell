using System.Text.Json;
using System.Text.Json.Nodes;

namespace Inkwell;

/// <summary>v1 唯一内置工具：当前日期时间查询；零外部依赖、零密钥配置。</summary>
internal sealed class AgentCurrentDateTimeToolExecutor(TimeProvider timeProvider)
{
    internal static readonly Guid ToolId = Guid.Parse("00000000-0000-0000-0000-000000000101");

    public Task<string> InvokeAsync(string argumentsJson, CancellationToken ct = default)
    {
        string timeZoneId = TryReadTimeZoneId(argumentsJson) ?? "UTC";
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        DateTimeOffset utcNow = timeProvider.GetUtcNow();
        DateTimeOffset localTime = TimeZoneInfo.ConvertTime(utcNow, timeZone);

        JsonObject result = new JsonObject
        {
            ["utc"] = utcNow.ToString("O"),
            ["timeZoneId"] = timeZoneId,
            ["localTime"] = localTime.ToString("O"),
        };

        return Task.FromResult(result.ToJsonString());
    }

    private static string? TryReadTimeZoneId(string argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return null;
        }

        JsonNode? node = JsonNode.Parse(argumentsJson);

        return node?["timeZoneId"]?.GetValue<string>();
    }
}
