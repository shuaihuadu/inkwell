// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Core.Tests.AgentRuntime;

/// <summary>
/// 验证 <see cref="AzureOpenAIModelRuntimeChatClientProvider"/> 的 Chat Client 提供行为。
/// </summary>
[TestClass]
public sealed class AzureOpenAIModelRuntimeChatClientProviderTests
{
    /// <summary>
    /// 验证已解析模型会生成对应的 Chat Client。
    /// </summary>
    [TestMethod]
    public void GetChatClient_WithResolvedModel_ReturnsChatClient()
    {
        // Arrange
        AzureOpenAIModelRuntimeChatClientProvider provider = CreateProvider();
        ModelDefinition model = CreateModel("business-model", "azure-deployment");

        // Act
        IChatClient chatClient = provider.GetChatClient(model);

        // Assert
        Assert.IsNotNull(chatClient);
    }

    /// <summary>
    /// 验证 Azure OpenAI 连接器声明稳定的运行时标识。
    /// </summary>
    [TestMethod]
    public void RuntimeId_ReturnsAzureOpenAI()
    {
        // Arrange
        AzureOpenAIModelRuntimeChatClientProvider provider = CreateProvider();

        // Act
        string runtimeId = provider.RuntimeId;

        // Assert
        Assert.AreEqual("azure-openai", runtimeId);
    }

    private static AzureOpenAIModelRuntimeChatClientProvider CreateProvider() => new(new AzureOpenAICredential
    {
        Endpoint = "https://example.openai.azure.com",
        ApiKey = "test-api-key",
        DeploymentName = "default-model",
    });

    private static ModelDefinition CreateModel(string id, string remoteModelId) => new()
    {
        Id = id,
        DisplayName = id,
        SourceId = "configuration",
        RuntimeId = "azure-openai",
        RemoteModelId = remoteModelId,
        IsAvailable = true,
    };
}
