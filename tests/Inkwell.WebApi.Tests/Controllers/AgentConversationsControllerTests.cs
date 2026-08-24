// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Security.Claims;
using Inkwell.WebApi.Controllers;
using Inkwell.WebApi.Conversations;

namespace Inkwell.WebApi.Tests.Controllers;

/// <summary>验证 Conversation API 的历史消息响应映射。</summary>
[TestClass]
public sealed class AgentConversationsControllerTests
{
    /// <summary>验证历史消息返回稳定的 camelCase Token 用量响应。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task GetMessagesAsync_WithUsage_MapsStableTokenUsageResponseAsync()
    {
        // Arrange
        Guid userId = Guid.CreateVersion7();
        Guid agentId = Guid.CreateVersion7();
        Guid conversationId = Guid.CreateVersion7();
        DateTimeOffset now = new(2026, 8, 25, 0, 0, 0, TimeSpan.Zero);
        RecordingConversationService service = new(new AgentChatMessage
        {
            Id = Guid.CreateVersion7(),
            ConversationId = conversationId,
            Message = new ChatMessage(ChatRole.Assistant, "answer"),
            Usage = new UsageDetails
            {
                InputTokenCount = 10,
                OutputTokenCount = 20,
                TotalTokenCount = 30,
                CachedInputTokenCount = 4,
                ReasoningTokenCount = 5,
                AdditionalCounts = new() { ["providerCount"] = 6 },
            },
            SequenceNumber = 1,
            CreatedTime = now,
            UpdatedTime = now,
        });
        AgentConversationsController controller = CreateController(userId, service);

        // Act
        ActionResult<PagedResponse<AgentChatMessageResponse>> result = await controller.GetMessagesAsync(
            agentId,
            conversationId,
            cancellationToken: CancellationToken.None);

        // Assert
        OkObjectResult ok = (OkObjectResult)result.Result!;
        PagedResponse<AgentChatMessageResponse> response = (PagedResponse<AgentChatMessageResponse>)ok.Value!;
        AgentTokenUsageResponse usage = response.Items[0].Usage!;
        Assert.AreEqual(10, usage.InputTokenCount);
        Assert.AreEqual(20, usage.OutputTokenCount);
        Assert.AreEqual(30, usage.TotalTokenCount);
        Assert.AreEqual(4, usage.CachedInputTokenCount);
        Assert.AreEqual(5, usage.ReasoningTokenCount);
        Assert.AreEqual(6, usage.AdditionalCounts!["providerCount"]);
        string json = JsonSerializer.Serialize(response, JsonSerializerOptions.Web);
        StringAssert.Contains(json, "\"usage\"");
        StringAssert.Contains(json, "\"inputTokenCount\":10");
        Assert.AreEqual(userId, service.OwnerUserId);
    }

    private static AgentConversationsController CreateController(Guid userId, RecordingConversationService service)
    {
        ClaimsIdentity identity = new([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Test");
        return new AgentConversationsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
            },
        };
    }

    private sealed class RecordingConversationService(AgentChatMessage message) : IAgentConversationService
    {
        public Guid? OwnerUserId { get; private set; }

        public Task<PagedResult<AgentChatMessage>> GetMessagesAsync(
            Guid ownerUserId,
            Guid agentId,
            Guid conversationId,
            Pagination pagination,
            CancellationToken ct = default)
        {
            this.OwnerUserId = ownerUserId;
            return Task.FromResult(new PagedResult<AgentChatMessage>([message], 1, pagination));
        }

        public Task<AgentConversation> CreateConversationAsync(Guid agentId, Guid ownerUserId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<PagedResult<AgentConversationListItem>> ListConversationsAsync(Guid agentId, Guid ownerUserId, Pagination pagination, CancellationToken ct = default) => throw new NotSupportedException();

        public Task DeleteMessageAsync(Guid ownerUserId, Guid agentId, Guid conversationId, Guid messageId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task ClearConversationAsync(Guid ownerUserId, Guid agentId, Guid conversationId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task DeleteConversationAsync(Guid ownerUserId, Guid agentId, Guid conversationId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<AgentResponse> RunAsync(Guid ownerUserId, Guid agentId, Guid conversationId, IReadOnlyList<ChatMessage> messages, AgentRunOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync(Guid ownerUserId, Guid agentId, Guid conversationId, IReadOnlyList<ChatMessage> messages, AgentRunOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
