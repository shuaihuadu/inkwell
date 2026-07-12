// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Inkwell.Abstractions.Tests;

/// <summary>
/// 验证 <see cref="AgentChatMessage.Message"/> 用纯 <see cref="JsonSerializer"/> 序列化 / 反序列化后，
/// 消息元数据与多态内容均被完整保留。
/// </summary>
[TestClass]
public sealed class AgentChatMessageContentSerializationTests
{
    [TestMethod]
    public void ChatMessage_Roundtrips_Metadata_And_Polymorphic_Content()
    {
        // Arrange
        ChatMessage original = new(
            ChatRole.Assistant,
            [
                new TextContent("checking weather"),
                new FunctionCallContent("call-1", "get_weather", new Dictionary<string, object?> { ["city"] = "Seattle" }),
            ])
        {
            MessageId = "message-1",
            AuthorName = "weather-agent",
        };

        // Act
        string json = JsonSerializer.Serialize(original);
        ChatMessage? roundtripped = JsonSerializer.Deserialize<ChatMessage>(json);

        // Assert
        Assert.IsNotNull(roundtripped);
        Assert.AreEqual(ChatRole.Assistant, roundtripped.Role);
        Assert.AreEqual("message-1", roundtripped.MessageId);
        Assert.AreEqual("weather-agent", roundtripped.AuthorName);
        Assert.HasCount(2, roundtripped.Contents);
        Assert.AreEqual("checking weather", ((TextContent)roundtripped.Contents[0]).Text);
        FunctionCallContent functionCall = (FunctionCallContent)roundtripped.Contents[1];
        Assert.AreEqual("call-1", functionCall.CallId);
        Assert.AreEqual("get_weather", functionCall.Name);
        Assert.AreEqual("Seattle", functionCall.Arguments?["city"]?.ToString());
    }

    [TestMethod]
    public void ChatMessage_Roundtrips_UriContent()
    {
        // Arrange
        ChatMessage original = new(
            ChatRole.User,
            [new UriContent(new Uri("https://example.com/doc.pdf"), "application/pdf")]);

        // Act
        string json = JsonSerializer.Serialize(original);
        ChatMessage? roundtripped = JsonSerializer.Deserialize<ChatMessage>(json);

        // Assert
        Assert.IsNotNull(roundtripped);
        Assert.HasCount(1, roundtripped.Contents);
        UriContent uriContent = (UriContent)roundtripped.Contents[0];
        Assert.AreEqual(new Uri("https://example.com/doc.pdf"), uriContent.Uri);
        Assert.AreEqual("application/pdf", uriContent.MediaType);
    }
}
