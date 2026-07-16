// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Inkwell.Abstractions.Tests;

/// <summary>
/// 验证 Agent Conversation 持久化模型的稳定序列化契约。
/// </summary>
[TestClass]
public sealed class AgentConversationModelTests
{
    /// <summary>
    /// 验证产品会话可完成 JSON 往返。
    /// </summary>
    [TestMethod]
    public void AgentConversation_RoundTripsJson()
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;
        AgentConversation expected = new()
        {
            Id = Guid.CreateVersion7(),
            SessionKey = Guid.CreateVersion7().ToString("D"),
            AgentId = Guid.CreateVersion7(),
            AgentVersionId = Guid.CreateVersion7(),
            OwnerUserId = Guid.CreateVersion7(),
            LastActivityTime = now,
            CreatedTime = now,
            UpdatedTime = now,
        };

        // Act
        string json = JsonSerializer.Serialize(expected);
        AgentConversation? actual = JsonSerializer.Deserialize<AgentConversation>(json);

        // Assert
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected, actual);
        Assert.IsNull(actual.LastCommittedRunId);
    }

    /// <summary>
    /// 验证完整 ChatMessage 及 Run 幂等字段可完成 JSON 往返。
    /// </summary>
    [TestMethod]
    public void AgentChatMessage_WithRunIdentity_RoundTripsJson()
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;
        AgentChatMessage expected = new()
        {
            Id = Guid.CreateVersion7(),
            ConversationId = Guid.CreateVersion7(),
            RunId = Guid.CreateVersion7().ToString("D"),
            RunMessageIndex = 0,
            Message = new ChatMessage(ChatRole.User, "hello"),
            SequenceNumber = 1,
            CreatedTime = now,
            UpdatedTime = now,
        };

        // Act
        string json = JsonSerializer.Serialize(expected);
        AgentChatMessage? actual = JsonSerializer.Deserialize<AgentChatMessage>(json);

        // Assert
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.Id, actual.Id);
        Assert.AreEqual(expected.ConversationId, actual.ConversationId);
        Assert.AreEqual(expected.RunId, actual.RunId);
        Assert.AreEqual(expected.RunMessageIndex, actual.RunMessageIndex);
        Assert.AreEqual(expected.Message.Text, actual.Message.Text);
    }

    /// <summary>
    /// 验证 Session 检查点保留 JSON、修订号和 nullable Run 标识。
    /// </summary>
    [TestMethod]
    public void AgentSessionState_WithNoLastRun_RoundTripsJson()
    {
        // Arrange
        AgentSessionState expected = new()
        {
            ConversationId = Guid.CreateVersion7(),
            SerializedState = JsonSerializer.SerializeToElement(new { value = "state" }),
            Revision = 1,
            UpdatedTime = DateTimeOffset.UtcNow,
        };

        // Act
        string json = JsonSerializer.Serialize(expected);
        AgentSessionState? actual = JsonSerializer.Deserialize<AgentSessionState>(json);

        // Assert
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.ConversationId, actual.ConversationId);
        Assert.AreEqual(expected.Revision, actual.Revision);
        Assert.AreEqual("state", actual.SerializedState.GetProperty("value").GetString());
        Assert.IsNull(actual.LastRunId);
    }
}
