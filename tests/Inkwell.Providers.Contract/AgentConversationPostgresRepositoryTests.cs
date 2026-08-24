// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Data;
using Inkwell.Persistence;
using Inkwell.Persistence.EFCore;
using Inkwell.Persistence.EFCore.Postgres.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Testcontainers.PostgreSql;

namespace Inkwell.Providers.Contract;

#pragma warning disable MEAI001

/// <summary>验证 PostgreSQL Conversation Repository 的事务与持久化契约。</summary>
[TestClass]
[DoNotParallelize]
public sealed class AgentConversationPostgresRepositoryTests
{
    private static PostgreSqlContainer? container;

    /// <summary>启动真实 PostgreSQL 测试容器。</summary>
    /// <param name="_">MSTest 测试上下文。</param>
    /// <returns>表示异步初始化操作的任务。</returns>
    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext _)
    {
        container = new PostgreSqlBuilder(ContainerImageConfiguration.GetRequired("Tests:Postgres")).Build();
        await container.StartAsync();
        await using ServiceProvider provider = BuildServiceProvider();
        InkwellDbContext db = provider.GetRequiredService<InkwellDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    /// <summary>释放 PostgreSQL 测试容器。</summary>
    /// <returns>表示异步清理操作的任务。</returns>
    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
        }
    }

    /// <summary>验证消息批量新增后按 Run 和会话序号稳定读取。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task MessageCrud_AddAndList_PreservesRunAndConversationOrderAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        SeededConversation seeded = await SeedConversationAsync();
        DateTimeOffset now = seeded.CreatedTime.AddMinutes(1);
        await using ServiceProvider provider = BuildServiceProvider();
        IAgentChatMessageRepository messages = provider.GetRequiredService<IAgentChatMessageRepository>();
        IReadOnlyList<AgentChatMessage> batch =
        [
            CreateMessage(seeded.ConversationId, "run-a", 0, new ChatMessage(ChatRole.User, "stable title"), now),
            CreateMessage(seeded.ConversationId, "run-a", 1, new ChatMessage(ChatRole.Assistant, "answer"), now),
        ];

        // Act
        IReadOnlyList<AgentChatMessage> added = await messages.AddMessages(batch);
        IReadOnlyList<AgentChatMessage> byRun = await messages.ListMessagesByRun(seeded.ConversationId, "run-a");
        IReadOnlyList<AgentChatMessage> all = await messages.ListAllMessagesByConversation(seeded.ConversationId);

        // Assert
        Assert.HasCount(2, added);
        Assert.HasCount(2, byRun);
        Assert.HasCount(2, all);
        CollectionAssert.AreEqual(new[] { 1, 2 }, all.Select(message => message.SequenceNumber).ToArray());
        CollectionAssert.AreEqual(new int?[] { 0, 1 }, byRun.Select(message => message.RunMessageIndex).ToArray());
    }

    /// <summary>验证同一会话、Run 和消息索引不能重复持久化。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task MessageCrud_AddDuplicateRunMessage_ThrowsDatabaseUpdateExceptionAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        SeededConversation seeded = await SeedConversationAsync();
        DateTimeOffset now = seeded.CreatedTime.AddMinutes(1);
        await using ServiceProvider provider = BuildServiceProvider();
        IAgentChatMessageRepository messages = provider.GetRequiredService<IAgentChatMessageRepository>();
        _ = await messages.AddMessages(
            [CreateMessage(seeded.ConversationId, "run-a", 0, new ChatMessage(ChatRole.User, "first"), now)]);

        // Act
        Task ActAsync() => messages.AddMessages(
            [CreateMessage(seeded.ConversationId, "run-a", 0, new ChatMessage(ChatRole.User, "duplicate"), now)]);

        // Assert
        _ = await Assert.ThrowsExactlyAsync<DbUpdateException>(ActAsync);
    }

    /// <summary>验证 Token 用量只覆盖目标消息并完整保留 Provider 报告字段。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task MessageCrud_UpdateUsage_PreservesAllFieldsAndTargetsExactMessageAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        SeededConversation seeded = await SeedConversationAsync();
        DateTimeOffset createdTime = seeded.CreatedTime.AddMinutes(1);
        DateTimeOffset updatedTime = createdTime.AddMinutes(1);
        await using ServiceProvider provider = BuildServiceProvider();
        IAgentChatMessageRepository messages = provider.GetRequiredService<IAgentChatMessageRepository>();
        IReadOnlyList<AgentChatMessage> added = await messages.AddMessages(
        [
            CreateMessage(seeded.ConversationId, "run-usage", 0, new ChatMessage(ChatRole.User, "question"), createdTime),
            CreateMessage(seeded.ConversationId, "run-usage", 1, new ChatMessage(ChatRole.Assistant, "answer"), createdTime),
        ]);
        AgentChatMessage target = added[1];
        UsageDetails usage = new()
        {
            InputTokenCount = 10,
            OutputTokenCount = 20,
            TotalTokenCount = 30,
            CachedInputTokenCount = 4,
            ReasoningTokenCount = 5,
            InputAudioTokenCount = 6,
            InputTextTokenCount = 7,
            OutputAudioTokenCount = 8,
            OutputTextTokenCount = 9,
            AdditionalCounts = new() { ["providerCount"] = 11 },
        };

        // Act
        bool updated = await messages.UpdateMessageUsage(seeded.ConversationId, target.Id, usage, updatedTime);
        bool wrongConversationUpdated = await messages.UpdateMessageUsage(Guid.CreateVersion7(), target.Id, usage, updatedTime);
        bool wrongMessageUpdated = await messages.UpdateMessageUsage(seeded.ConversationId, Guid.CreateVersion7(), usage, updatedTime);
        IReadOnlyList<AgentChatMessage> persisted = await messages.ListMessagesByRun(seeded.ConversationId, "run-usage");

        // Assert
        Assert.IsTrue(updated);
        Assert.IsFalse(wrongConversationUpdated);
        Assert.IsFalse(wrongMessageUpdated);
        Assert.IsNull(persisted[0].Usage);
        UsageDetails persistedUsage = persisted[1].Usage!;
        Assert.AreEqual(10, persistedUsage.InputTokenCount);
        Assert.AreEqual(20, persistedUsage.OutputTokenCount);
        Assert.AreEqual(30, persistedUsage.TotalTokenCount);
        Assert.AreEqual(4, persistedUsage.CachedInputTokenCount);
        Assert.AreEqual(5, persistedUsage.ReasoningTokenCount);
        Assert.AreEqual(6, persistedUsage.InputAudioTokenCount);
        Assert.AreEqual(7, persistedUsage.InputTextTokenCount);
        Assert.AreEqual(8, persistedUsage.OutputAudioTokenCount);
        Assert.AreEqual(9, persistedUsage.OutputTextTokenCount);
        Assert.AreEqual(11, persistedUsage.AdditionalCounts!["providerCount"]);
        Assert.AreEqual(updatedTime, persisted[1].UpdatedTime);
    }

    /// <summary>验证 Repository CRUD 在事务失败时整体回滚。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task ExecuteInTransaction_RepositoryFailure_RollsBackDeletedDataAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        SeededConversation seeded = await SeedConversationAsync();
        DateTimeOffset now = seeded.CreatedTime.AddMinutes(1);
        await using ServiceProvider provider = BuildServiceProvider();
        IPersistenceProvider persistence = provider.GetRequiredService<IPersistenceProvider>();
        IAgentChatMessageRepository messages = provider.GetRequiredService<IAgentChatMessageRepository>();
        _ = await messages.AddMessages([CreateMessage(seeded.ConversationId, "run-a", 0, new ChatMessage(ChatRole.User, "keep"), now)]);

        // Act
        Task ActAsync() => persistence.ExecuteInTransactionAsync(
            IsolationLevel.Serializable,
            async innerCt =>
            {
                _ = await messages.DeleteMessagesByConversation(seeded.ConversationId, innerCt);
                throw new InvalidOperationException("Force rollback.");
            });

        // Assert
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(ActAsync);
        PagedResult<AgentChatMessage> remainingMessages = await messages.ListMessagesByConversation(seeded.ConversationId, Pagination.Default);
        Assert.HasCount(1, remainingMessages.Items);
    }

    private static AgentChatMessage CreateMessage(Guid conversationId, string runId, int runMessageIndex, ChatMessage message, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        ConversationId = conversationId,
        RunId = runId,
        RunMessageIndex = runMessageIndex,
        Message = message,
        SequenceNumber = 0,
        CreatedTime = now,
        UpdatedTime = now,
    };

    private static async Task ResetDatabaseAsync()
    {
        await using ServiceProvider provider = BuildServiceProvider();
        InkwellDbContext db = provider.GetRequiredService<InkwellDbContext>();
        _ = await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE agent_chat_messages, agent_session_states, agent_conversations, agent_versions, agents, users CASCADE;");
    }

    private static async Task<SeededConversation> SeedConversationAsync()
    {
        await using ServiceProvider provider = BuildServiceProvider();
        IUserRepository users = provider.GetRequiredService<IUserRepository>();
        IAgentRepository agents = provider.GetRequiredService<IAgentRepository>();
        IAgentVersionRepository versions = provider.GetRequiredService<IAgentVersionRepository>();
        IAgentConversationRepository conversations = provider.GetRequiredService<IAgentConversationRepository>();

        Guid agentId = Guid.CreateVersion7();
        Guid versionId = Guid.CreateVersion7();
        Guid ownerUserId = Guid.CreateVersion7();
        Guid conversationId = Guid.CreateVersion7();
        DateTimeOffset createdTime = new(2026, 7, 16, 0, 0, 0, TimeSpan.Zero);
        AgentBuildOptions buildOptions = new()
        {
            ModelOptions = new AgentModelOptions { ModelId = "test-model" },
        };

        _ = await users.AddUser(new User
        {
            Id = ownerUserId,
            Username = $"owner-{ownerUserId:N}"[..20],
            PasswordHash = "hash",
            CreatedTime = createdTime,
            UpdatedTime = createdTime,
        });
        _ = await agents.AddAgent(new AgentDefinition
        {
            Id = agentId,
            OwnerUserId = ownerUserId,
            Name = "Conversation agent",
            Instructions = "Test conversation persistence.",
            BuildOptions = buildOptions,
            CurrentPublishedVersionId = versionId,
            LatestPublishedVersionNumber = 1,
            CreatedTime = createdTime,
            UpdatedTime = createdTime,
        });
        _ = await versions.AddVersionAsync(new AgentVersion
        {
            Id = versionId,
            AgentId = agentId,
            VersionNumber = 1,
            Snapshot = new AgentSnapshot
            {
                Name = "Conversation agent",
                Instructions = "Test conversation persistence.",
                BuildOptions = buildOptions,
            },
            OwnerUserId = ownerUserId,
            CreatedTime = createdTime,
            UpdatedTime = createdTime,
            PublishedTime = createdTime,
        });
        _ = await conversations.AddConversation(new AgentConversation
        {
            Id = conversationId,
            AgentId = agentId,
            AgentVersionId = versionId,
            OwnerUserId = ownerUserId,
            LastActivityTime = createdTime,
            CreatedTime = createdTime,
            UpdatedTime = createdTime,
        });

        return new SeededConversation(conversationId, createdTime);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        ServiceCollection services = new();
        services.AddLogging();
        IInkwellBuilder builder = services.AddInkwell(new ConfigurationBuilder().Build());
        builder.UsePostgres(container!.GetConnectionString());
        return services.BuildServiceProvider();
    }

    private sealed record SeededConversation(Guid ConversationId, DateTimeOffset CreatedTime);
}

#pragma warning restore MEAI001
