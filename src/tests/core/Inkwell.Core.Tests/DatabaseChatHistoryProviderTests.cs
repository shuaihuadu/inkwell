using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Inkwell.Core.Tests;

/// <summary>
/// 验证 <see cref="DatabaseChatHistoryProvider"/>：通过 <see cref="ChatHistoryProvider.InvokingAsync"/> /
/// <see cref="ChatHistoryProvider.InvokedAsync"/> 公共入口（而非反射调用受保护的 <c>ProvideChatHistoryAsync</c> /
/// <c>StoreChatHistoryAsync</c>）驱动，与 MAF 框架自身消费本类的方式一致。
/// </summary>
[TestClass]
public sealed class DatabaseChatHistoryProviderTests
{
#pragma warning disable MAAI001 // ChatHistoryProvider.InvokingContext/InvokedContext 构造函数标注为实验性 API。
    [TestMethod]
    public async Task InvokingAsync_PrependsHistory_WhenConversationIdPresentInStateBag()
    {
        // Arrange
        Guid conversationId = Guid.CreateVersion7();
        AgentChatMessage historyMessage = new() { Role = ChatRole.Assistant, Content = [new TextContent("上一轮回复")] };
        FakeAgentConversationService conversationService = new() { HistoryToReturn = [historyMessage] };
        DatabaseChatHistoryProvider provider = CreateProvider(conversationService);
        AgentSession session = CreateSessionWithConversationId(conversationId);
        ChatMessage requestMessage = new(ChatRole.User, "这一轮的新消息");

        ChatHistoryProvider.InvokingContext context = new(new NoopAIAgent(), session, [requestMessage]);

        // Act
        IEnumerable<ChatMessage> result = await provider.InvokingAsync(context).ConfigureAwait(false);

        // Assert
        List<ChatMessage> messages = [.. result];
        Assert.AreEqual(2, messages.Count);
        Assert.AreEqual("上一轮回复", messages[0].Text);
        Assert.AreEqual("这一轮的新消息", messages[1].Text);
        Assert.AreEqual(conversationId, conversationService.LastRequestedConversationId);
    }

    [TestMethod]
    public async Task InvokingAsync_ReturnsOnlyRequestMessages_WhenSessionHasNoConversationId()
    {
        // Arrange
        FakeAgentConversationService conversationService = new() { HistoryToReturn = [new AgentChatMessage { Role = ChatRole.Assistant, Content = [] }] };
        DatabaseChatHistoryProvider provider = CreateProvider(conversationService);
        AgentSession session = new TestAgentSession();
        ChatMessage requestMessage = new(ChatRole.User, "新消息");

        ChatHistoryProvider.InvokingContext context = new(new NoopAIAgent(), session, [requestMessage]);

        // Act
        IEnumerable<ChatMessage> result = await provider.InvokingAsync(context).ConfigureAwait(false);

        // Assert
        List<ChatMessage> messages = [.. result];
        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual("新消息", messages[0].Text);
        Assert.IsNull(conversationService.LastRequestedConversationId);
    }

    [TestMethod]
    public async Task InvokingAsync_ReturnsOnlyRequestMessages_WhenSessionIsNull()
    {
        // Arrange
        FakeAgentConversationService conversationService = new();
        DatabaseChatHistoryProvider provider = CreateProvider(conversationService);
        ChatMessage requestMessage = new(ChatRole.User, "新消息");

        ChatHistoryProvider.InvokingContext context = new(new NoopAIAgent(), session: null, [requestMessage]);

        // Act
        IEnumerable<ChatMessage> result = await provider.InvokingAsync(context).ConfigureAwait(false);

        // Assert
        List<ChatMessage> messages = [.. result];
        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual("新消息", messages[0].Text);
    }

    [TestMethod]
    public async Task InvokedAsync_AppendsRequestAndResponseMessages_WhenConversationIdPresent()
    {
        // Arrange
        Guid conversationId = Guid.CreateVersion7();
        FakeAgentConversationService conversationService = new();
        DatabaseChatHistoryProvider provider = CreateProvider(conversationService);
        AgentSession session = CreateSessionWithConversationId(conversationId);
        ChatMessage requestMessage = new(ChatRole.User, "用户新消息");
        ChatMessage responseMessage = new(ChatRole.Assistant, "助手回复");

        ChatHistoryProvider.InvokedContext context = new(new NoopAIAgent(), session, [requestMessage], [responseMessage]);

        // Act
        await provider.InvokedAsync(context).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(2, conversationService.AppendedMessages.Count);
        Assert.AreEqual(conversationId, conversationService.AppendedMessages[0].ConversationId);
        Assert.AreEqual("用户新消息", conversationService.AppendedMessages[0].Message.Content.OfType<TextContent>().Single().Text);
        Assert.AreEqual(conversationId, conversationService.AppendedMessages[1].ConversationId);
        Assert.AreEqual("助手回复", conversationService.AppendedMessages[1].Message.Content.OfType<TextContent>().Single().Text);
    }

    [TestMethod]
    public async Task InvokedAsync_DoesNotAppend_WhenConversationIdAbsent()
    {
        // Arrange
        FakeAgentConversationService conversationService = new();
        DatabaseChatHistoryProvider provider = CreateProvider(conversationService);
        AgentSession session = new TestAgentSession();
        ChatMessage requestMessage = new(ChatRole.User, "用户新消息");
        ChatMessage responseMessage = new(ChatRole.Assistant, "助手回复");

        ChatHistoryProvider.InvokedContext context = new(new NoopAIAgent(), session, [requestMessage], [responseMessage]);

        // Act
        await provider.InvokedAsync(context).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(0, conversationService.AppendedMessages.Count);
    }

    [TestMethod]
    public async Task InvokedAsync_DoesNotAppend_WhenInvocationFailed()
    {
        // Arrange
        Guid conversationId = Guid.CreateVersion7();
        FakeAgentConversationService conversationService = new();
        DatabaseChatHistoryProvider provider = CreateProvider(conversationService);
        AgentSession session = CreateSessionWithConversationId(conversationId);
        ChatMessage requestMessage = new(ChatRole.User, "用户新消息");

        ChatHistoryProvider.InvokedContext context = new(new NoopAIAgent(), session, [requestMessage], new InvalidOperationException("模拟 Run 失败"));

        // Act
        await provider.InvokedAsync(context).ConfigureAwait(false);

        // Assert（框架基类 InvokedCoreAsync 默认实现在 InvokeException 非空时直接跳过，不会调用到 StoreChatHistoryAsync）
        Assert.AreEqual(0, conversationService.AppendedMessages.Count);
    }
#pragma warning restore MAAI001

    private static DatabaseChatHistoryProvider CreateProvider(IAgentConversationService conversationService)
    {
        ServiceCollection services = new();
        services.AddSingleton(conversationService);

        ServiceProvider provider = services.BuildServiceProvider();

        return new DatabaseChatHistoryProvider(provider.GetRequiredService<IServiceScopeFactory>());
    }

    private static AgentSession CreateSessionWithConversationId(Guid conversationId)
    {
        TestAgentSession session = new();

        session.StateBag.SetValue(DatabaseChatHistoryProvider.ConversationIdStateKey, conversationId.ToString());

        return session;
    }

    private sealed class TestAgentSession : AgentSession;

    private sealed class NoopAIAgent : AIAgent
    {
        protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("测试用 NoopAIAgent 不支持该操作。");

        protected override ValueTask<JsonElement> SerializeSessionCoreAsync(AgentSession session, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("测试用 NoopAIAgent 不支持该操作。");

        protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(JsonElement serializedState, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("测试用 NoopAIAgent 不支持该操作。");

        protected override Task<AgentResponse> RunCoreAsync(IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("测试用 NoopAIAgent 不支持该操作。");

        protected override IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("测试用 NoopAIAgent 不支持该操作。");
    }

    private sealed class FakeAgentConversationService : IAgentConversationService
    {
        public IReadOnlyList<AgentChatMessage> HistoryToReturn { get; init; } = [];

        public List<(Guid ConversationId, AgentChatMessage Message)> AppendedMessages { get; } = [];

        public Guid? LastRequestedConversationId { get; private set; }

        public Task<IReadOnlyList<AgentChatMessage>> GetHistoryMessagesAsync(Guid conversationId, CancellationToken ct = default)
        {
            this.LastRequestedConversationId = conversationId;

            return Task.FromResult(this.HistoryToReturn);
        }

        public Task<Guid> AppendMessageAsync(Guid conversationId, AgentChatMessage message, CancellationToken ct = default)
        {
            this.AppendedMessages.Add((conversationId, message));

            return Task.FromResult(Guid.CreateVersion7());
        }

        public Task<Guid> StartConversationAsync(Guid agentId, Guid ownerUserId, CancellationToken ct = default) =>
            throw new NotSupportedException("测试用 FakeAgentConversationService 不支持该操作。");

        public Task<IReadOnlyList<AgentConversationSummary>> ListConversationsAsync(Guid agentId, Guid ownerUserId, CancellationToken ct = default) =>
            throw new NotSupportedException("测试用 FakeAgentConversationService 不支持该操作。");

        public Task DeleteMessageAsync(Guid conversationId, Guid messageId, Guid actorUserId, CancellationToken ct = default) =>
            throw new NotSupportedException("测试用 FakeAgentConversationService 不支持该操作。");

        public Task ClearConversationAsync(Guid conversationId, Guid actorUserId, CancellationToken ct = default) =>
            throw new NotSupportedException("测试用 FakeAgentConversationService 不支持该操作。");

        public Task<IReadOnlyList<Guid>> ListUsedAgentIdsAsync(Guid ownerUserId, CancellationToken ct = default) =>
            throw new NotSupportedException("测试用 FakeAgentConversationService 不支持该操作。");

        public Task<IReadOnlyDictionary<Guid, DateTimeOffset>> GetLastActivityByAgentsAsync(IReadOnlyList<Guid> agentIds, Guid viewerUserId, CancellationToken ct = default) =>
            throw new NotSupportedException("测试用 FakeAgentConversationService 不支持该操作。");
    }
}
