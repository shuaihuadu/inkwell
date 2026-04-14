namespace Inkwell.Tests.Queue;

[TestClass]
public sealed class InMemoryQueueProviderTests
{
    [TestMethod]
    public async Task EnqueueAndDequeue_Roundtrips()
    {
        // Arrange
        InMemoryQueueProvider<string> provider = new();

        // Act
        await provider.EnqueueAsync("item1");
        string? result = await provider.DequeueAsync();

        // Assert
        Assert.AreEqual("item1", result);
    }

    [TestMethod]
    public async Task Dequeue_ReturnsNull_WhenEmpty()
    {
        // Arrange
        InMemoryQueueProvider<string> provider = new();

        // Act
        string? result = await provider.DequeueAsync();

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetCount_ReturnsCorrectCount()
    {
        // Arrange
        InMemoryQueueProvider<string> provider = new();
        await provider.EnqueueAsync("a");
        await provider.EnqueueAsync("b");

        // Act
        long count = await provider.GetCountAsync();

        // Assert
        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public async Task Dequeue_IsFIFO()
    {
        // Arrange
        InMemoryQueueProvider<string> provider = new();
        await provider.EnqueueAsync("first");
        await provider.EnqueueAsync("second");

        // Act
        string? r1 = await provider.DequeueAsync();
        string? r2 = await provider.DequeueAsync();

        // Assert
        Assert.AreEqual("first", r1);
        Assert.AreEqual("second", r2);
    }

    [TestMethod]
    public async Task NamedQueues_AreIsolated()
    {
        // Arrange
        InMemoryQueueProvider<string> provider = new();
        await provider.EnqueueAsync("a", "q1");
        await provider.EnqueueAsync("b", "q2");

        // Act
        long count1 = await provider.GetCountAsync("q1");
        long count2 = await provider.GetCountAsync("q2");

        // Assert
        Assert.AreEqual(1, count1);
        Assert.AreEqual(1, count2);
    }
}
