using System.Text.Json;
using Inkwell.Agents;
using Inkwell.Agents.Middleware;
using Inkwell.Persistence.InMemory;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Inkwell.Tests.Agents.Middleware;

[TestClass]
public sealed class SessionPersistenceMiddlewareTests
{
    private InMemorySessionPersistenceProvider _sessionProvider = null!;

    [TestInitialize]
    public void Setup()
    {
        _sessionProvider = new InMemorySessionPersistenceProvider();
    }

    [TestMethod]
    public async Task WithSessionPersistence_PersistsSessionState_AfterRun()
    {
        // Arrange
        AIAgent baseAgent = CreateFakeChatClient("response").AsAIAgent();
        AIAgent wrapped = baseAgent.WithSessionPersistence("writer", _sessionProvider);
        string threadId = "test-thread-1";

        ChatClientAgentRunOptions options = CreateOptionsWithThreadId(threadId);
        List<ChatMessage> messages = [new(ChatRole.User, "hello")];

        // Act
        await wrapped.RunAsync(messages, options: options);

        // Assert - session state should be persisted
        JsonElement? savedState = await _sessionProvider.LoadSessionAsync(threadId);
        Assert.IsNotNull(savedState, "Session state should be saved after RunAsync");
    }

    [TestMethod]
    public async Task WithSessionPersistence_PersistsSessionState_AfterStreaming()
    {
        // Arrange
        AIAgent baseAgent = CreateFakeChatClient("streamed response").AsAIAgent();
        AIAgent wrapped = baseAgent.WithSessionPersistence("writer", _sessionProvider);
        string threadId = "test-thread-2";

        ChatClientAgentRunOptions options = CreateOptionsWithThreadId(threadId);
        List<ChatMessage> messages = [new(ChatRole.User, "hello")];

        // Act - consume the entire stream
        await foreach (AgentResponseUpdate _ in wrapped.RunStreamingAsync(messages, options: options))
        {
            // consume
        }

        // Assert
        JsonElement? savedState = await _sessionProvider.LoadSessionAsync(threadId);
        Assert.IsNotNull(savedState, "Session state should be saved after streaming completes");
    }

    [TestMethod]
    public async Task WithSessionPersistence_SavesMessages()
    {
        // Arrange
        AIAgent baseAgent = CreateFakeChatClient("agent reply").AsAIAgent();
        AIAgent wrapped = baseAgent.WithSessionPersistence("writer", _sessionProvider);
        string threadId = "test-thread-3";

        ChatClientAgentRunOptions options = CreateOptionsWithThreadId(threadId);
        List<ChatMessage> messages = [new(ChatRole.User, "hello")];

        // Act
        await wrapped.RunAsync(messages, options: options);

        // Assert - messages should be saved
        IReadOnlyList<ChatMessageRecord> savedMessages = await _sessionProvider.GetMessagesAsync(threadId);
        Assert.IsTrue(savedMessages.Count > 0, "Messages should be saved after run");
    }

    [TestMethod]
    public async Task WithSessionPersistence_GeneratesTitle_FromFirstMessage()
    {
        // Arrange
        AIAgent baseAgent = CreateFakeChatClient("reply").AsAIAgent();
        AIAgent wrapped = baseAgent.WithSessionPersistence("writer", _sessionProvider);
        string threadId = "test-thread-4";

        ChatClientAgentRunOptions options = CreateOptionsWithThreadId(threadId);
        List<ChatMessage> messages = [new(ChatRole.User, "Write about AI in healthcare")];

        // Act
        await wrapped.RunAsync(messages, options: options);

        // Assert
        SessionInfo? info = await _sessionProvider.GetSessionInfoAsync(threadId);
        Assert.IsNotNull(info);
        Assert.IsNotNull(info.Title, "Title should be auto-generated from first user message");
        Assert.IsTrue(info.Title.Contains("AI") || info.Title.Contains("healthcare") || info.Title.Length > 0);
    }

    [TestMethod]
    public async Task WithSessionPersistence_RestoresSession_OnSecondRequest()
    {
        // Arrange
        AIAgent baseAgent = CreateFakeChatClient("first reply").AsAIAgent();
        AIAgent wrapped = baseAgent.WithSessionPersistence("writer", _sessionProvider);
        string threadId = "test-thread-5";

        ChatClientAgentRunOptions options = CreateOptionsWithThreadId(threadId);

        // Act - first request
        await wrapped.RunAsync([new ChatMessage(ChatRole.User, "hello")], options: options);

        // Verify session exists
        JsonElement? state1 = await _sessionProvider.LoadSessionAsync(threadId);
        Assert.IsNotNull(state1);

        // Second request with same threadId should load existing session
        await wrapped.RunAsync([new ChatMessage(ChatRole.User, "follow up")], options: options);

        // Assert - session still exists and was updated
        JsonElement? state2 = await _sessionProvider.LoadSessionAsync(threadId);
        Assert.IsNotNull(state2);
    }

    [TestMethod]
    public async Task WithSessionPersistence_NoThreadId_DoesNotPersist()
    {
        // Arrange
        AIAgent baseAgent = CreateFakeChatClient("reply").AsAIAgent();
        AIAgent wrapped = baseAgent.WithSessionPersistence("writer", _sessionProvider);

        // No AGUI options (no threadId)
        List<ChatMessage> messages = [new(ChatRole.User, "hello")];

        // Act
        await wrapped.RunAsync(messages);

        // Assert - nothing should be persisted (no threadId to key on)
        IReadOnlyList<SessionInfo> infos = await _sessionProvider.ListSessionInfosAsync("writer");
        Assert.AreEqual(0, infos.Count, "Without threadId, nothing should be persisted");
    }

    private static ChatClientAgentRunOptions CreateOptionsWithThreadId(string threadId)
    {
        return new ChatClientAgentRunOptions
        {
            ChatOptions = new ChatOptions
            {
                AdditionalProperties = new AdditionalPropertiesDictionary
                {
                    ["ag_ui_thread_id"] = threadId
                }
            }
        };
    }

    private static FakeChatClient CreateFakeChatClient(string responseText)
    {
        return new FakeChatClient(responseText);
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

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }
}
