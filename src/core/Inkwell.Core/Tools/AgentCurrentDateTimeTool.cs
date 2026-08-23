// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel;
using System.Text.Json.Nodes;

namespace Inkwell;

/// <summary>
/// 提供当前日期时间查询能力。
/// </summary>
internal sealed class AgentCurrentDateTimeTool(TimeProvider timeProvider)
{
    /// <summary>
    /// 获取指定时区的当前日期时间。
    /// </summary>
    /// <param name="timeZoneId">IANA 或 Windows 时区标识；未指定时使用 UTC。</param>
    /// <returns>包含 UTC 时间、时区标识和本地时间的 JSON 对象。</returns>
    [Description("Get the current date and time in an optional IANA or Windows time zone.")]
    public JsonObject GetCurrentDateTime(
        [Description("The optional IANA or Windows time zone ID. Uses UTC when omitted.")]
        string? timeZoneId = null)
    {
        string resolvedTimeZoneId = timeZoneId ?? "UTC";
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(resolvedTimeZoneId);
        DateTimeOffset utcNow = timeProvider.GetUtcNow();
        DateTimeOffset localTime = TimeZoneInfo.ConvertTime(utcNow, timeZone);

        return new JsonObject
        {
            ["utc"] = utcNow.ToString("O"),
            ["timeZoneId"] = resolvedTimeZoneId,
            ["localTime"] = localTime.ToString("O"),
        };
    }
}