// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Core.Tests.Tools;

/// <summary>
/// 验证 JSON 委托与 MEAI Function Calling 的桥接语义。
/// </summary>
[TestClass]
public sealed class JsonDelegateAIFunctionTests
{
    /// <summary>
    /// 验证 JSON 结果以结构化值返回，不被当作字符串二次序列化。
    /// </summary>
    [TestMethod]
    public async Task InvokeAsync_WithJsonResult_ReturnsJsonElementAsync()
    {
        // Arrange
        JsonDelegateAIFunction function = new(
            "get_weather",
            "Gets the current weather.",
            "{\"type\":\"object\"}",
            (_, _) => Task.FromResult("{\"temperature\":21}"));

        // Act
        object? result = await function.InvokeAsync(new AIFunctionArguments());

        // Assert
        JsonElement resultElement = (JsonElement)result!;
        Assert.AreEqual(JsonValueKind.Object, resultElement.ValueKind);
        Assert.AreEqual(21, resultElement.GetProperty("temperature").GetInt32());
    }

    /// <summary>
    /// 验证执行器返回非法 JSON 时显式抛出异常。
    /// </summary>
    [TestMethod]
    public async Task InvokeAsync_WithInvalidJsonResult_ThrowsJsonExceptionAsync()
    {
        // Arrange
        JsonDelegateAIFunction function = new(
            "get_weather",
            "Gets the current weather.",
            "{\"type\":\"object\"}",
            (_, _) => Task.FromResult("not-json"));

        // Act and Assert
        await Assert.ThrowsAsync<JsonException>(async () => await function.InvokeAsync(new AIFunctionArguments()));
    }

    /// <summary>
    /// 验证非法参数 schema 在构造期立即失败。
    /// </summary>
    [TestMethod]
    public void Constructor_WithInvalidJsonSchema_ThrowsJsonException()
    {
        // Act and Assert
        Assert.Throws<JsonException>(() => new JsonDelegateAIFunction(
            "get_weather",
            "Gets the current weather.",
            "not-json",
            (_, _) => Task.FromResult("{}")));
    }
}