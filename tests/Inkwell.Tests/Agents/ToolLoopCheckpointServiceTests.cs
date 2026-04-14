using System.Text.Json;
using Inkwell.Agents;

namespace Inkwell.Tests.Agents;

[TestClass]
public sealed class ToolLoopCheckpointServiceTests
{
    private ToolLoopCheckpointService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        // Arrange
        InMemorySessionPersistenceProvider provider = new();
        _service = new ToolLoopCheckpointService(provider);
    }

    [TestMethod]
    public async Task SaveAndLoadCheckpoint_Roundtrips()
    {
        // Act
        await _service.SaveCheckpointAsync("writer", "s1", 0, "SearchTool", "result data");
        ToolLoopCheckpoint? loaded = await _service.LoadCheckpointAsync("s1", 0);

        // Assert
        Assert.IsNotNull(loaded);
        Assert.AreEqual("writer", loaded.AgentId);
        Assert.AreEqual("s1", loaded.SessionId);
        Assert.AreEqual(0, loaded.ToolCallIndex);
        Assert.AreEqual("SearchTool", loaded.ToolName);
        Assert.AreEqual("result data", loaded.ToolResult);
    }

    [TestMethod]
    public async Task LoadCheckpoint_ReturnsNull_WhenNotFound()
    {
        // Act
        ToolLoopCheckpoint? result = await _service.LoadCheckpointAsync("nonexistent", 0);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task FindLatestCheckpointIndex_ReturnsCorrectIndex()
    {
        // Arrange
        await _service.SaveCheckpointAsync("writer", "s1", 0, "Tool1", "r1");
        await _service.SaveCheckpointAsync("writer", "s1", 1, "Tool2", "r2");
        await _service.SaveCheckpointAsync("writer", "s1", 2, "Tool3", "r3");

        // Act
        int latest = await _service.FindLatestCheckpointIndexAsync("writer", "s1", maxIndex: 10);

        // Assert
        Assert.AreEqual(2, latest);
    }

    [TestMethod]
    public async Task FindLatestCheckpointIndex_ReturnsNegativeOne_WhenNoCheckpoints()
    {
        // Act
        int latest = await _service.FindLatestCheckpointIndexAsync("writer", "empty", maxIndex: 10);

        // Assert
        Assert.AreEqual(-1, latest);
    }
}
