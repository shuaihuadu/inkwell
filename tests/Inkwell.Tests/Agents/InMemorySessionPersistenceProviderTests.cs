using System.Text.Json;
using Inkwell.Agents;

namespace Inkwell.Tests.Agents;

[TestClass]
public sealed class InMemorySessionPersistenceProviderTests
{
    private InMemorySessionPersistenceProvider _provider = null!;

    [TestInitialize]
    public void Setup()
    {
        _provider = new InMemorySessionPersistenceProvider();
    }

    [TestMethod]
    public async Task SaveAndLoadSession_RoundTrips()
    {
        // Arrange
        string sessionId = "test-session-1";
        string agentId = "writer";
        JsonElement state = JsonSerializer.SerializeToElement(new { key = "value" });

        // Act
        await _provider.SaveSessionAsync(sessionId, agentId, state);
        JsonElement? loaded = await _provider.LoadSessionAsync(sessionId);

        // Assert
        Assert.IsNotNull(loaded);
        Assert.AreEqual("value", loaded.Value.GetProperty("key").GetString());
    }

    [TestMethod]
    public async Task LoadSession_ReturnsNull_WhenNotFound()
    {
        // Act
        JsonElement? result = await _provider.LoadSessionAsync("nonexistent");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ListSessions_ReturnsSessionIds_ForAgent()
    {
        // Arrange
        JsonElement state = JsonSerializer.SerializeToElement(new { });
        await _provider.SaveSessionAsync("s1", "writer", state);
        await _provider.SaveSessionAsync("s2", "writer", state);
        await _provider.SaveSessionAsync("s3", "critic", state);

        // Act
        IReadOnlyList<string> writerSessions = await _provider.ListSessionsAsync("writer");
        IReadOnlyList<string> criticSessions = await _provider.ListSessionsAsync("critic");

        // Assert
        Assert.AreEqual(2, writerSessions.Count);
        Assert.AreEqual(1, criticSessions.Count);
    }

    [TestMethod]
    public async Task DeleteSession_RemovesSessionAndMessages()
    {
        // Arrange
        JsonElement state = JsonSerializer.SerializeToElement(new { });
        await _provider.SaveSessionAsync("s1", "writer", state);
        await _provider.SaveMessagesAsync("s1", [
            new ChatMessageRecord("m1", "user", "hello", "done", DateTimeOffset.UtcNow)
        ]);

        // Act
        await _provider.DeleteSessionAsync("s1");

        // Assert
        Assert.IsNull(await _provider.LoadSessionAsync("s1"));
        IReadOnlyList<ChatMessageRecord> messages = await _provider.GetMessagesAsync("s1");
        Assert.AreEqual(0, messages.Count);
    }

    [TestMethod]
    public async Task GetSessionInfo_ReturnsCorrectMetadata()
    {
        // Arrange
        JsonElement state = JsonSerializer.SerializeToElement(new { });
        await _provider.SaveSessionAsync("s1", "writer", state);

        // Act
        SessionInfo? info = await _provider.GetSessionInfoAsync("s1");

        // Assert
        Assert.IsNotNull(info);
        Assert.AreEqual("s1", info.Id);
        Assert.AreEqual("writer", info.AgentId);
        Assert.IsNull(info.Title);
        Assert.AreEqual(0, info.MessageCount);
    }

    [TestMethod]
    public async Task UpdateSessionTitle_UpdatesTitle()
    {
        // Arrange
        JsonElement state = JsonSerializer.SerializeToElement(new { });
        await _provider.SaveSessionAsync("s1", "writer", state);

        // Act
        await _provider.UpdateSessionTitleAsync("s1", "My Title");
        SessionInfo? info = await _provider.GetSessionInfoAsync("s1");

        // Assert
        Assert.IsNotNull(info);
        Assert.AreEqual("My Title", info.Title);
    }

    [TestMethod]
    public async Task SaveAndGetMessages_Roundtrips()
    {
        // Arrange
        JsonElement state = JsonSerializer.SerializeToElement(new { });
        await _provider.SaveSessionAsync("s1", "writer", state);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        ChatMessageRecord[] messages =
        [
            new("m1", "user", "hello", "done", now),
            new("m2", "assistant", "hi there", "done", now.AddSeconds(1))
        ];

        // Act
        await _provider.SaveMessagesAsync("s1", messages);
        IReadOnlyList<ChatMessageRecord> loaded = await _provider.GetMessagesAsync("s1");

        // Assert
        Assert.AreEqual(2, loaded.Count);
        Assert.AreEqual("user", loaded[0].Role);
        Assert.AreEqual("hello", loaded[0].Content);
        Assert.AreEqual("assistant", loaded[1].Role);
    }

    [TestMethod]
    public async Task ListSessionInfos_ReturnsByUpdatedAtDesc()
    {
        // Arrange
        JsonElement state = JsonSerializer.SerializeToElement(new { });
        await _provider.SaveSessionAsync("s1", "writer", state);
        await Task.Delay(10);
        await _provider.SaveSessionAsync("s2", "writer", state);

        // Act
        IReadOnlyList<SessionInfo> infos = await _provider.ListSessionInfosAsync("writer");

        // Assert
        Assert.AreEqual(2, infos.Count);
        Assert.AreEqual("s2", infos[0].Id); // 最新的在前
        Assert.AreEqual("s1", infos[1].Id);
    }

    [TestMethod]
    public async Task SaveMessages_UpdatesMessageCount()
    {
        // Arrange
        JsonElement state = JsonSerializer.SerializeToElement(new { });
        await _provider.SaveSessionAsync("s1", "writer", state);

        // Act
        await _provider.SaveMessagesAsync("s1", [
            new("m1", "user", "hello", "done", DateTimeOffset.UtcNow),
            new("m2", "assistant", "hi", "done", DateTimeOffset.UtcNow)
        ]);
        SessionInfo? info = await _provider.GetSessionInfoAsync("s1");

        // Assert
        Assert.IsNotNull(info);
        Assert.AreEqual(2, info.MessageCount);
    }

    [TestMethod]
    public async Task SaveSession_Upsert_UpdatesExisting()
    {
        // Arrange
        JsonElement state1 = JsonSerializer.SerializeToElement(new { version = 1 });
        JsonElement state2 = JsonSerializer.SerializeToElement(new { version = 2 });

        // Act
        await _provider.SaveSessionAsync("s1", "writer", state1);
        await _provider.SaveSessionAsync("s1", "writer", state2);
        JsonElement? loaded = await _provider.LoadSessionAsync("s1");

        // Assert
        Assert.IsNotNull(loaded);
        Assert.AreEqual(2, loaded.Value.GetProperty("version").GetInt32());
    }
}
