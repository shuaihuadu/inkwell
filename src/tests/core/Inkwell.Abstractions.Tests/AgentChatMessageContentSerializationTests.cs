using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Inkwell;

namespace Inkwell.Abstractions.Tests;

/// <summary>
/// 验证 <see cref="AgentChatMessage.Content"/>（<see cref="IReadOnlyList{AIContent}"/>）用纯
/// <see cref="System.Text.Json.JsonSerializer"/>（不带任何自定义 <see cref="JsonSerializerOptions"/>）
/// 序列化 / 反序列化后，具体类型（<see cref="TextContent"/> / <see cref="UriContent"/>）是否被正确保留——
/// 这是 <c>AgentConversationMessage.ContentJson</c> 持久化列的实际读写方式。
/// </summary>
[TestClass]
public sealed class AgentChatMessageContentSerializationTests
{
    [TestMethod]
    public void TextContent_Roundtrips_Through_Plain_JsonSerializer()
    {
        // Arrange
        IReadOnlyList<AIContent> original = [new TextContent("hello world")];

        // Act
        string json = JsonSerializer.Serialize(original);
        IReadOnlyList<AIContent>? roundtripped = JsonSerializer.Deserialize<IReadOnlyList<AIContent>>(json);

        // Assert
        Assert.IsNotNull(roundtripped);
        Assert.AreEqual(1, roundtripped.Count);
        TextContent? text = roundtripped[0] as TextContent;
        Assert.IsNotNull(text);
        Assert.AreEqual("hello world", text.Text);
    }

    [TestMethod]
    public void UriContent_Roundtrips_Through_Plain_JsonSerializer()
    {
        // Arrange
        IReadOnlyList<AIContent> original = [new UriContent(new Uri("https://example.com/a.png"), "image/png")];

        // Act
        string json = JsonSerializer.Serialize(original);
        IReadOnlyList<AIContent>? roundtripped = JsonSerializer.Deserialize<IReadOnlyList<AIContent>>(json);

        // Assert
        Assert.IsNotNull(roundtripped);
        Assert.AreEqual(1, roundtripped.Count);
        UriContent? uri = roundtripped[0] as UriContent;
        Assert.IsNotNull(uri);
        Assert.AreEqual(new Uri("https://example.com/a.png"), uri.Uri);
        Assert.AreEqual("image/png", uri.MediaType);
    }

    [TestMethod]
    public void Mixed_Content_List_Preserves_Each_Concrete_Type()
    {
        // Arrange
        IReadOnlyList<AIContent> original =
        [
            new TextContent("caption"),
            new UriContent(new Uri("https://example.com/doc.pdf"), "application/pdf"),
        ];

        // Act
        string json = JsonSerializer.Serialize(original);
        IReadOnlyList<AIContent>? roundtripped = JsonSerializer.Deserialize<IReadOnlyList<AIContent>>(json);

        // Assert
        Assert.IsNotNull(roundtripped);
        Assert.AreEqual(2, roundtripped.Count);
        Assert.IsInstanceOfType<TextContent>(roundtripped[0]);
        Assert.IsInstanceOfType<UriContent>(roundtripped[1]);
    }
}
