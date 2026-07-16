// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Data;
using System.Text.Json;
using Inkwell.Persistence.EFCore;
using Inkwell.Persistence.EFCore.Postgres.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Testcontainers.PostgreSql;

namespace Inkwell.Providers.Contract;

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

    /// <summary>验证 Session State 的新增、更新和删除 CRUD。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task SessionStateCrud_AddUpdateDelete_PersistsValuesAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        SeededConversation seeded = await SeedConversationAsync();
        DateTimeOffset now = seeded.CreatedTime.AddMinutes(1);
        await using ServiceProvider provider = BuildServiceProvider();
        IAgentSessionStateRepository states = provider.GetRequiredService<IAgentSessionStateRepository>();
        AgentSessionState firstState = CreateState(seeded.ConversationId, 1, "run-a", now);

        // Act
        await states.AddSessionState(firstState);
        AgentSessionState updatedState = CreateState(seeded.ConversationId, 2, "run-b", now.AddMinutes(1));
        await states.UpdateSessionState(updatedState);
        AgentSessionState? persisted = await states.GetSessionStateOrDefault(seeded.ConversationId);
        bool deleted = await states.DeleteSessionStateByConversation(seeded.ConversationId);

        // Assert
        Assert.IsNotNull(persisted);
        Assert.AreEqual(2L, persisted.Revision);
        Assert.AreEqual("run-b", persisted.LastRunId);
        Assert.IsTrue(deleted);
        Assert.IsNull(await states.GetSessionStateOrDefault(seeded.ConversationId));
    }

    /// <summary>验证跨 Repository CRUD 在事务失败时整体回滚。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task ExecuteInTransaction_CrossRepositoryFailure_RollsBackDeletedDataAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        SeededConversation seeded = await SeedConversationAsync();
        DateTimeOffset now = seeded.CreatedTime.AddMinutes(1);
        await using ServiceProvider provider = BuildServiceProvider();
        IPersistenceProvider persistence = provider.GetRequiredService<IPersistenceProvider>();
        IAgentChatMessageRepository messages = provider.GetRequiredService<IAgentChatMessageRepository>();
        IAgentSessionStateRepository states = provider.GetRequiredService<IAgentSessionStateRepository>();
        _ = await messages.AddMessages([CreateMessage(seeded.ConversationId, "run-a", 0, new ChatMessage(ChatRole.User, "keep"), now)]);
        await states.AddSessionState(CreateState(seeded.ConversationId, 1, "run-a", now));

        // Act
        Task ActAsync() => persistence.ExecuteInTransactionAsync(
            IsolationLevel.Serializable,
            async innerCt =>
            {
                _ = await messages.DeleteMessagesByConversation(seeded.ConversationId, innerCt);
                _ = await states.DeleteSessionStateByConversation(seeded.ConversationId, innerCt);
                throw new InvalidOperationException("Force rollback.");
            });

        // Assert
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(ActAsync);
        PagedResult<AgentChatMessage> remainingMessages = await messages.ListMessagesByConversation(seeded.ConversationId, Pagination.Default);
        Assert.HasCount(1, remainingMessages.Items);
        Assert.IsNotNull(await states.GetSessionStateOrDefault(seeded.ConversationId));
    }

    private static AgentSessionState CreateState(Guid conversationId, long revision, string runId, DateTimeOffset updatedTime)
    {
        using JsonDocument document = JsonDocument.Parse($$"""{"runId":"{{runId}}","revision":{{revision}}}""");
        return new AgentSessionState
        {
            ConversationId = conversationId,
            SerializedState = document.RootElement.Clone(),
            Revision = revision,
            LastRunId = runId,
            UpdatedTime = updatedTime,
        };
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
            "TRUNCATE TABLE agent_chat_messages, agent_session_states, agent_conversations, agent_versions, agents CASCADE;");
    }

    private static async Task<SeededConversation> SeedConversationAsync()
    {
        await using ServiceProvider provider = BuildServiceProvider();
        InkwellDbContext db = provider.GetRequiredService<InkwellDbContext>();
        Guid agentId = Guid.CreateVersion7();
        Guid versionId = Guid.CreateVersion7();
        Guid ownerUserId = Guid.CreateVersion7();
        Guid conversationId = Guid.CreateVersion7();
        DateTimeOffset createdTime = new(2026, 7, 16, 0, 0, 0, TimeSpan.Zero);
        AgentBuildOptions buildOptions = new()
        {
            ModelOptions = new AgentModelOptions { ModelId = "test-model" },
        };
        AgentSnapshot snapshot = new()
        {
            Name = "Conversation agent",
            Instructions = "Test conversation persistence.",
            BuildOptions = buildOptions,
        };
        string buildOptionsJson = JsonSerializer.Serialize(buildOptions);
        string snapshotJson = JsonSerializer.Serialize(snapshot);
        await db.Database.ExecuteSqlInterpolatedAsync($$"""
            INSERT INTO agents
                (id, owner_user_id, name, avatar_uri, description, instructions, build_options,
                 current_published_version_id, latest_published_version_number, is_shared,
                 shared_revoked_by_admin_time, created_time, updated_time)
            VALUES
                ({{agentId}}, {{ownerUserId}}, 'Conversation agent', NULL, NULL,
                 'Test conversation persistence.', CAST({{buildOptionsJson}} AS jsonb), {{versionId}}, 1, FALSE,
                 NULL, {{createdTime}}, {{createdTime}});

            INSERT INTO agent_versions
                (id, agent_id, version_number, snapshot, created_by_user_id, change_summary,
                 created_time, updated_time, published_time)
            VALUES
                ({{versionId}}, {{agentId}}, 1, CAST({{snapshotJson}} AS jsonb), {{ownerUserId}}, NULL,
                 {{createdTime}}, {{createdTime}}, {{createdTime}});

            INSERT INTO agent_conversations
                (id, session_key, agent_id, agent_version_id, owner_user_id, title,
                 last_committed_run_id, last_activity_time, created_time, updated_time)
            VALUES
                ({{conversationId}}, {{conversationId.ToString("D")}}, {{agentId}}, {{versionId}}, {{ownerUserId}}, NULL,
                 NULL, {{createdTime}}, {{createdTime}}, {{createdTime}});
            """);
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
