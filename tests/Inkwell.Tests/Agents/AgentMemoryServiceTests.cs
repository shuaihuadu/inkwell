using Inkwell.Agents;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

namespace Inkwell.Tests.Agents;

[TestClass]
public sealed class AgentMemoryServiceTests
{
    [TestMethod]
    public void VectorStore_ReturnsInjectedInstance()
    {
        // Arrange
        VectorStore vectorStore = new InMemoryVectorStore();
        AgentMemoryService service = new(vectorStore);

        // Act & Assert
        Assert.AreSame(vectorStore, service.VectorStore);
    }

    [TestMethod]
    public void Constructor_AcceptsVectorStore()
    {
        // Arrange & Act
        VectorStore vectorStore = new InMemoryVectorStore();
        AgentMemoryService service = new(vectorStore);

        // Assert
        Assert.IsNotNull(service);
    }
}
