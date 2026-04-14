using Inkwell.Agents.Middleware;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Inkwell.Tests.Agents.Middleware;

[TestClass]
public sealed class ContentGuardrailMiddlewareTests
{
    [TestMethod]
    public async Task InvokeAsync_BlocksContentWithSensitiveTerms()
    {
        // Arrange
        AIAgent agent = CreateFakeAgent("这篇文章讨论了暴力相关的话题");
        List<ChatMessage> messages = [new(ChatRole.User, "test")];

        // Act
        AgentResponse response = await ContentGuardrailMiddleware.InvokeAsync(
            messages, null, null, agent, CancellationToken.None);

        // Assert
        Assert.IsTrue(response.Text.Contains("安全系统拦截"));
    }

    [TestMethod]
    public async Task InvokeAsync_AllowsCleanContent()
    {
        // Arrange
        AIAgent agent = CreateFakeAgent("这是一篇关于AI的优质文章");
        List<ChatMessage> messages = [new(ChatRole.User, "test")];

        // Act
        AgentResponse response = await ContentGuardrailMiddleware.InvokeAsync(
            messages, null, null, agent, CancellationToken.None);

        // Assert
        Assert.AreEqual("这是一篇关于AI的优质文章", response.Text);
    }

    [TestMethod]
    public async Task InvokeStreamingAsync_BlocksOnSensitiveTerm()
    {
        // Arrange
        AIAgent agent = CreateFakeAgent("包含赌博的文章内容");
        List<ChatMessage> messages = [new(ChatRole.User, "test")];

        // Act
        List<AgentResponseUpdate> updates = [];
        await foreach (AgentResponseUpdate update in ContentGuardrailMiddleware.InvokeStreamingAsync(
            messages, null, null, agent, CancellationToken.None))
        {
            updates.Add(update);
        }

        // Assert
        string allText = string.Join("", updates.Select(u => u.Text ?? ""));
        Assert.IsTrue(allText.Contains("安全系统拦截"));
    }

    [TestMethod]
    public async Task InvokeStreamingAsync_PassesThroughCleanContent()
    {
        // Arrange
        AIAgent agent = CreateFakeAgent("正常的AI技术文章");
        List<ChatMessage> messages = [new(ChatRole.User, "test")];

        // Act
        List<AgentResponseUpdate> updates = [];
        await foreach (AgentResponseUpdate update in ContentGuardrailMiddleware.InvokeStreamingAsync(
            messages, null, null, agent, CancellationToken.None))
        {
            updates.Add(update);
        }

        // Assert
        Assert.IsTrue(updates.Count > 0);
    }

    [TestMethod]
    public async Task InvokeAsync_ChecksMultipleSensitiveTerms()
    {
        // Arrange - 测试所有敏感词
        string[] sensitiveTerms = ["暴力", "赌博", "毒品", "色情"];

        foreach (string term in sensitiveTerms)
        {
            AIAgent agent = CreateFakeAgent($"文章包含{term}相关内容");
            List<ChatMessage> messages = [new(ChatRole.User, "test")];

            // Act
            AgentResponse response = await ContentGuardrailMiddleware.InvokeAsync(
                messages, null, null, agent, CancellationToken.None);

            // Assert
            Assert.IsTrue(response.Text.Contains("安全系统拦截"),
                $"Should block content containing: {term}");
        }
    }

    private static AIAgent CreateFakeAgent(string responseText)
    {
        return new FakeChatClient(responseText).AsAIAgent();
    }

    private sealed class FakeChatClient(string responseText) : IChatClient
    {
        public ChatClientMetadata Metadata => new("fake");

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, responseText)));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new ChatResponseUpdate(ChatRole.Assistant, responseText);
        }

        public TService? GetService<TService>(object? key = null) where TService : class => null;

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }
}
