// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Core.Tests.Tools;

/// <summary>
/// 验证当前日期时间工具的强类型调用行为。
/// </summary>
[TestClass]
public sealed class AgentCurrentDateTimeToolTests
{
    /// <summary>
    /// 验证未指定时区时使用 UTC。
    /// </summary>
    [TestMethod]
    public void GetCurrentDateTime_WithoutTimeZone_UsesUtc()
    {
        // Arrange
        DateTimeOffset utcNow = new(2026, 8, 23, 10, 30, 0, TimeSpan.Zero);
        AgentCurrentDateTimeTool tool = new(new StubTimeProvider(utcNow));

        // Act
        JsonElement result = JsonSerializer.SerializeToElement(tool.GetCurrentDateTime());

        // Assert
        Assert.AreEqual("UTC", result.GetProperty("timeZoneId").GetString());
        Assert.AreEqual(utcNow, result.GetProperty("utc").GetDateTimeOffset());
        Assert.AreEqual(utcNow, result.GetProperty("localTime").GetDateTimeOffset());
    }

    /// <summary>
    /// 验证模型调用时可指定时区。
    /// </summary>
    [TestMethod]
    public void GetCurrentDateTime_WithTimeZone_UsesRequestedValue()
    {
        // Arrange
        DateTimeOffset utcNow = new(2026, 8, 23, 10, 30, 0, TimeSpan.Zero);
        AgentCurrentDateTimeTool tool = new(new StubTimeProvider(utcNow));

        // Act
        JsonElement result = JsonSerializer.SerializeToElement(tool.GetCurrentDateTime("Asia/Shanghai"));

        // Assert
        Assert.AreEqual("Asia/Shanghai", result.GetProperty("timeZoneId").GetString());
        Assert.AreEqual(
            new DateTimeOffset(2026, 8, 23, 18, 30, 0, TimeSpan.FromHours(8)),
            result.GetProperty("localTime").GetDateTimeOffset());
    }

    /// <summary>
    /// 验证未知时区由 BCL 异常表达。
    /// </summary>
    [TestMethod]
    public void GetCurrentDateTime_WithUnknownTimeZone_Throws()
    {
        // Arrange
        AgentCurrentDateTimeTool tool = new(TimeProvider.System);

        // Act
        void Act() => tool.GetCurrentDateTime("Unknown/TimeZone");

        // Assert
        Assert.ThrowsExactly<TimeZoneNotFoundException>(Act);
    }

    private sealed class StubTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
