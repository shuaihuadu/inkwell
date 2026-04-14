using Inkwell.Agents;

namespace Inkwell.Tests.Agents;

[TestClass]
public sealed class AgentRegistryTests
{
    private AgentRegistry _registry = null!;

    [TestInitialize]
    public void Setup()
    {
        // Arrange
        _registry = new AgentRegistry();
    }

    [TestMethod]
    public void Register_AddsAgent()
    {
        // Act
        _registry.Register(CreateRegistration("writer"));

        // Assert
        Assert.AreEqual(1, _registry.Count);
    }

    [TestMethod]
    public void GetById_ReturnsRegisteredAgent()
    {
        // Arrange
        _registry.Register(CreateRegistration("writer"));

        // Act
        AgentRegistration? result = _registry.GetById("writer");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("writer", result.Id);
    }

    [TestMethod]
    public void GetById_ReturnsNull_WhenNotFound()
    {
        // Act
        AgentRegistration? result = _registry.GetById("nonexistent");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetAll_ReturnsAllRegistered()
    {
        // Arrange
        _registry.Register(CreateRegistration("writer"));
        _registry.Register(CreateRegistration("critic"));

        // Act
        IReadOnlyList<AgentRegistration> all = _registry.GetAll();

        // Assert
        Assert.AreEqual(2, all.Count);
    }

    [TestMethod]
    public void Count_ReturnsCorrectCount()
    {
        // Assert
        Assert.AreEqual(0, _registry.Count);

        // Act
        _registry.Register(CreateRegistration("writer"));

        // Assert
        Assert.AreEqual(1, _registry.Count);
    }

    private static AgentRegistration CreateRegistration(string id)
    {
        return new AgentRegistration
        {
            Id = id,
            Name = id,
            Description = $"Test {id}",
            Agent = new Microsoft.Agents.AI.ChatClientAgent(new FakeChatClient()),
            AguiRoute = $"/api/agui/{id}"
        };
    }

    private sealed class FakeChatClient : Microsoft.Extensions.AI.IChatClient
    {
        public Microsoft.Extensions.AI.ChatClientMetadata Metadata => new("fake");

        public Task<Microsoft.Extensions.AI.ChatResponse> GetResponseAsync(
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> chatMessages,
            Microsoft.Extensions.AI.ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new Microsoft.Extensions.AI.ChatResponse(
                new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.Assistant, "ok")));

        public IAsyncEnumerable<Microsoft.Extensions.AI.ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> chatMessages,
            Microsoft.Extensions.AI.ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }
}
