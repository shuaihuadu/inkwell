// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Core.Tests.AgentRuntime;

/// <summary>
/// 验证 <see cref="AzureOpenAIAgentFactory"/> 的版本快照映射行为。
/// </summary>
[TestClass]
public sealed class AzureOpenAIAgentFactoryTests
{
    /// <summary>
    /// 验证构建时会把版本快照与聊天历史 Provider 映射到 MAF Agent。
    /// </summary>
    [TestMethod]
    public async Task BuildAsync_WithSnapshotAndHistoryProvider_MapsAgentMetadataAsync()
    {
        // Arrange
        AzureOpenAIAgentFactory factory = CreateFactory();
        InMemoryChatHistoryProvider historyProvider = new();
        AgentVersion version = CreateVersion("configured-model");
        AgentBuildOptions buildOptions = new() { ChatHistoryProvider = historyProvider };

        // Act
        AIAgent agent = await factory.BuildAsync(version, buildOptions);

        // Assert
        ChatClientAgent chatClientAgent = (ChatClientAgent)agent;
        Assert.AreEqual(version.Id.ToString(), agent.Id);
        Assert.AreEqual("Research assistant", agent.Name);
        Assert.AreEqual("Finds verifiable sources.", agent.Description);
        Assert.AreEqual("Answer with concise citations.", chatClientAgent.Instructions);
        Assert.AreSame(historyProvider, chatClientAgent.ChatHistoryProvider);
    }

    /// <summary>
    /// 验证快照未指定模型时仍可使用 Factory 默认部署构建 Agent。
    /// </summary>
    [TestMethod]
    public async Task BuildAsync_WithoutSnapshotModel_UsesDefaultDeploymentAsync()
    {
        // Arrange
        AzureOpenAIAgentFactory factory = CreateFactory();
        AgentVersion version = CreateVersion(modelId: null);

        // Act
        AIAgent agent = await factory.BuildAsync(version, new AgentBuildOptions());

        // Assert
        Assert.IsInstanceOfType<ChatClientAgent>(agent);
        Assert.AreEqual("Research assistant", agent.Name);
    }

    private static AzureOpenAIAgentFactory CreateFactory() => new(new AzureOpenAICredential
    {
        Endpoint = "https://example.openai.azure.com",
        ApiKey = "test-api-key",
        DeploymentName = "default-model",
    });

    private static AgentVersion CreateVersion(string? modelId) => new()
    {
        Id = Guid.CreateVersion7(),
        AgentId = Guid.CreateVersion7(),
        VersionNumber = 1,
        Status = AgentVersionStatus.Draft,
        CreatedByUserId = Guid.CreateVersion7(),
        Snapshot = new AgentSnapshot
        {
            Name = "Research assistant",
            Description = "Finds verifiable sources.",
            Instructions = "Answer with concise citations.",
            ModelId = modelId,
            ModelParameters = new AgentModelParameters
            {
                Temperature = 0.2,
                TopP = 0.9,
                MaxTokens = 1024,
            },
        },
        CreatedTime = DateTimeOffset.UtcNow,
        UpdatedTime = DateTimeOffset.UtcNow,
    };
}