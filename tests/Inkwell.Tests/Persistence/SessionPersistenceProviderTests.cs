using Inkwell.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Inkwell.Tests.Persistence;

[TestClass]
public sealed class SessionPersistenceProviderTests
{
    private InkwellDbContext _db = null!;
    private SessionPersistenceProvider _provider = null!;

    [TestInitialize]
    public void Setup()
    {
        // Arrange - 每个测试使用独立的 InMemory 数据库
        DbContextOptions<InkwellDbContext> options = new DbContextOptionsBuilder<InkwellDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new InkwellDbContext(options);
        _provider = new SessionPersistenceProvider(_db);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _db.Dispose();
    }

    [TestMethod]
    public async Task SaveAndLoad_Roundtrips()
    {
        // Arrange
        JsonElement state = JsonSerializer.SerializeToElement(new { key = "value" });

        // Act
        await _provider.SaveSessionAsync("s1", "writer", state);
        JsonElement? loaded = await _provider.LoadSessionAsync("s1");

        // Assert
        Assert.IsNotNull(loaded);
        Assert.AreEqual("value", loaded.Value.GetProperty("key").GetString());
    }

    [TestMethod]
    public async Task SaveSession_Upsert_UpdatesExisting()
    {
        // Arrange
        JsonElement state1 = JsonSerializer.SerializeToElement(new { v = 1 });
        JsonElement state2 = JsonSerializer.SerializeToElement(new { v = 2 });

        // Act
        await _provider.SaveSessionAsync("s1", "writer", state1);
        await _provider.SaveSessionAsync("s1", "writer", state2);
        JsonElement? loaded = await _provider.LoadSessionAsync("s1");

        // Assert
        Assert.IsNotNull(loaded);
        Assert.AreEqual(2, loaded.Value.GetProperty("v").GetInt32());

        // 只有一条记录，不是两条
        Assert.AreEqual(1, await _db.ChatSessions.CountAsync());
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
    public async Task DeleteSession_CascadeDeletesMessages()
    {
        // Arrange
        JsonElement state = JsonSerializer.SerializeToElement(new { });
        await _provider.SaveSessionAsync("s1", "writer", state);
        await _provider.SaveMessagesAsync("s1",
        [
            new ChatMessageRecord("m1", "user", "hello", "done", DateTimeOffset.UtcNow),
            new ChatMessageRecord("m2", "assistant", "hi", "done", DateTimeOffset.UtcNow)
        ]);

        // Act
        await _provider.DeleteSessionAsync("s1");

        // Assert
        Assert.IsNull(await _provider.LoadSessionAsync("s1"));
        Assert.AreEqual(0, await _db.ChatMessages.CountAsync());
    }

    [TestMethod]
    public async Task ListSessionInfos_OrderedByUpdatedAtDesc()
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
        Assert.IsTrue(infos[0].UpdatedAt >= infos[1].UpdatedAt);
    }

    [TestMethod]
    public async Task ListSessionInfos_FiltersbyAgentId()
    {
        // Arrange
        JsonElement state = JsonSerializer.SerializeToElement(new { });
        await _provider.SaveSessionAsync("s1", "writer", state);
        await _provider.SaveSessionAsync("s2", "critic", state);

        // Act
        IReadOnlyList<SessionInfo> writerInfos = await _provider.ListSessionInfosAsync("writer");
        IReadOnlyList<SessionInfo> criticInfos = await _provider.ListSessionInfosAsync("critic");

        // Assert
        Assert.AreEqual(1, writerInfos.Count);
        Assert.AreEqual(1, criticInfos.Count);
    }

    [TestMethod]
    public async Task UpdateSessionTitle_PersistsTitle()
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
    public async Task SaveMessages_UpdatesMessageCount()
    {
        // Arrange
        JsonElement state = JsonSerializer.SerializeToElement(new { });
        await _provider.SaveSessionAsync("s1", "writer", state);

        // Act
        await _provider.SaveMessagesAsync("s1",
        [
            new ChatMessageRecord("m1", "user", "hello", "done", DateTimeOffset.UtcNow),
            new ChatMessageRecord("m2", "assistant", "hi", "done", DateTimeOffset.UtcNow)
        ]);
        SessionInfo? info = await _provider.GetSessionInfoAsync("s1");

        // Assert
        Assert.IsNotNull(info);
        Assert.AreEqual(2, info.MessageCount);
    }

    [TestMethod]
    public async Task GetMessages_OrderedByCreatedAt()
    {
        // Arrange
        JsonElement state = JsonSerializer.SerializeToElement(new { });
        await _provider.SaveSessionAsync("s1", "writer", state);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        await _provider.SaveMessagesAsync("s1",
        [
            new ChatMessageRecord("m1", "user", "first", "done", now),
            new ChatMessageRecord("m2", "assistant", "second", "done", now.AddSeconds(1))
        ]);

        // Act
        IReadOnlyList<ChatMessageRecord> messages = await _provider.GetMessagesAsync("s1");

        // Assert
        Assert.AreEqual(2, messages.Count);
        Assert.AreEqual("first", messages[0].Content);
        Assert.AreEqual("second", messages[1].Content);
    }

    [TestMethod]
    public async Task GetMessages_ReturnsEmpty_WhenNoMessages()
    {
        // Act
        IReadOnlyList<ChatMessageRecord> messages = await _provider.GetMessagesAsync("nonexistent");

        // Assert
        Assert.AreEqual(0, messages.Count);
    }
}
