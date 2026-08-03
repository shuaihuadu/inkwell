// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Text.Json.Nodes;
using Inkwell.Persistence;
using Inkwell.Persistence.EFCore;
using Inkwell.Persistence.EFCore.Postgres.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Inkwell.Providers.Contract;

/// <summary>验证 PostgreSQL Session State Repository 的持久化契约与外键级联行为。</summary>
[TestClass]
[DoNotParallelize]
public sealed class AgentSessionStatePostgresRepositoryTests
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

    /// <summary>验证状态行新增后可按会话标识原样读回 JSON 内容。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task AddSessionState_ThenFindByConversation_RoundtripsContentAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        SeededConversation seeded = await SeedConversationAsync();
        await using ServiceProvider provider = BuildServiceProvider();
        IAgentSessionStateRepository sessionStates = provider.GetRequiredService<IAgentSessionStateRepository>();
        const string Content = """{"checkpoint":"round-1","messages":[{"role":"user"}]}""";

        // Act
        _ = await sessionStates.AddSessionState(CreateState(seeded.ConversationId, Content, seeded.CreatedTime));
        AgentSessionState? found = await sessionStates.FindSessionStateByConversation(seeded.ConversationId);

        // Assert
        Assert.IsNotNull(found);
        Assert.AreEqual(seeded.ConversationId, found.ConversationId);
        AssertJsonEquivalent(Content, found.SessionState);
    }

    /// <summary>验证会话标识不存在时查询返回空而不是抛出。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task FindSessionStateByConversation_WithUnknownId_ReturnsNullAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using ServiceProvider provider = BuildServiceProvider();
        IAgentSessionStateRepository sessionStates = provider.GetRequiredService<IAgentSessionStateRepository>();

        // Act
        AgentSessionState? found = await sessionStates.FindSessionStateByConversation(Guid.CreateVersion7());

        // Assert
        Assert.IsNull(found);
    }

    /// <summary>验证覆盖写入会替换内容并刷新更新时间，同时保留创建时间。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task UpdateSessionState_WithExistingConversation_OverwritesContentAndUpdatedTimeAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        SeededConversation seeded = await SeedConversationAsync();
        await using ServiceProvider provider = BuildServiceProvider();
        IAgentSessionStateRepository sessionStates = provider.GetRequiredService<IAgentSessionStateRepository>();
        _ = await sessionStates.AddSessionState(CreateState(seeded.ConversationId, AgentSessionState.Empty, seeded.CreatedTime));
        DateTimeOffset updatedTime = seeded.CreatedTime.AddMinutes(5);
        const string Content = """{"checkpoint":"round-2"}""";

        // Act
        bool updated = await sessionStates.UpdateSessionState(seeded.ConversationId, Content, updatedTime);
        AgentSessionState? found = await sessionStates.FindSessionStateByConversation(seeded.ConversationId);

        // Assert
        Assert.IsTrue(updated);
        Assert.IsNotNull(found);
        AssertJsonEquivalent(Content, found.SessionState);
        Assert.AreEqual(updatedTime, found.UpdatedTime);
        Assert.AreEqual(seeded.CreatedTime, found.CreatedTime);
    }

    /// <summary>验证覆盖写入未命中任何行时返回 false，供上层回退到新增路径。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task UpdateSessionState_WithUnknownConversation_ReturnsFalseAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using ServiceProvider provider = BuildServiceProvider();
        IAgentSessionStateRepository sessionStates = provider.GetRequiredService<IAgentSessionStateRepository>();

        // Act
        bool updated = await sessionStates.UpdateSessionState(
            Guid.CreateVersion7(),
            AgentSessionState.Empty,
            DateTimeOffset.UnixEpoch);

        // Assert
        Assert.IsFalse(updated);
    }

    /// <summary>验证删除会移除状态行，且不影响所属会话本身。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task DeleteSessionState_WithExistingConversation_RemovesRowAndKeepsConversationAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        SeededConversation seeded = await SeedConversationAsync();
        await using ServiceProvider provider = BuildServiceProvider();
        IAgentSessionStateRepository sessionStates = provider.GetRequiredService<IAgentSessionStateRepository>();
        IAgentConversationRepository conversations = provider.GetRequiredService<IAgentConversationRepository>();
        _ = await sessionStates.AddSessionState(CreateState(seeded.ConversationId, """{"checkpoint":"round-1"}""", seeded.CreatedTime));

        // Act
        bool deleted = await sessionStates.DeleteSessionState(seeded.ConversationId);

        // Assert
        Assert.IsTrue(deleted);
        Assert.IsNull(await sessionStates.FindSessionStateByConversation(seeded.ConversationId));
        Assert.IsNotNull(await conversations.GetConversation(seeded.ConversationId));
    }

    /// <summary>验证删除未命中任何行时返回 false 而不是抛出。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task DeleteSessionState_WithUnknownConversation_ReturnsFalseAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using ServiceProvider provider = BuildServiceProvider();
        IAgentSessionStateRepository sessionStates = provider.GetRequiredService<IAgentSessionStateRepository>();

        // Act
        bool deleted = await sessionStates.DeleteSessionState(Guid.CreateVersion7());

        // Assert
        Assert.IsFalse(deleted);
    }

    /// <summary>验证删除会话时状态行随外键级联删除，不残留孤儿行。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task DeleteConversation_CascadesToSessionStateAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        SeededConversation seeded = await SeedConversationAsync();
        await using ServiceProvider provider = BuildServiceProvider();
        IAgentSessionStateRepository sessionStates = provider.GetRequiredService<IAgentSessionStateRepository>();
        IAgentConversationRepository conversations = provider.GetRequiredService<IAgentConversationRepository>();
        _ = await sessionStates.AddSessionState(CreateState(seeded.ConversationId, """{"checkpoint":"round-1"}""", seeded.CreatedTime));

        // Act
        bool deleted = await conversations.DeleteConversation(seeded.ConversationId);

        // Assert
        Assert.IsTrue(deleted);
        Assert.IsNull(await sessionStates.FindSessionStateByConversation(seeded.ConversationId));
    }

    /// <summary>验证不存在的会话标识因外键约束无法写入状态行。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task AddSessionState_WithoutConversation_ThrowsOnForeignKeyAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using ServiceProvider provider = BuildServiceProvider();
        IAgentSessionStateRepository sessionStates = provider.GetRequiredService<IAgentSessionStateRepository>();
        AgentSessionState orphan = CreateState(Guid.CreateVersion7(), AgentSessionState.Empty, DateTimeOffset.UnixEpoch);

        // Act
        Task ActAsync() => sessionStates.AddSessionState(orphan);

        // Assert
        _ = await Assert.ThrowsExactlyAsync<DbUpdateException>(ActAsync);
    }

    /// <summary>
    /// 按 JSON 语义比较两段内容。PostgreSQL jsonb 列在写入时会重排对象成员并规范化空白，
    /// 读回的字符串与写入的原文不逐字节相同，只保证 JSON 语义等价。
    /// </summary>
    /// <param name="expected">期望的 JSON 内容。</param>
    /// <param name="actual">实际读回的 JSON 内容。</param>
    private static void AssertJsonEquivalent(string expected, string actual)
        => Assert.IsTrue(
            JsonNode.DeepEquals(JsonNode.Parse(expected), JsonNode.Parse(actual)),
            $"Expected JSON <{expected}> to be equivalent to <{actual}>.");

    private static AgentSessionState CreateState(Guid conversationId, string sessionState, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        ConversationId = conversationId,
        SessionState = sessionState,
        CreatedTime = now,
        UpdatedTime = now,
    };

    private static async Task ResetDatabaseAsync()
    {
        await using ServiceProvider provider = BuildServiceProvider();
        InkwellDbContext db = provider.GetRequiredService<InkwellDbContext>();
        _ = await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE agent_session_states, agent_conversations, agent_versions, agents, users CASCADE;");
    }

    private static async Task<SeededConversation> SeedConversationAsync()
    {
        await using ServiceProvider provider = BuildServiceProvider();
        IUserRepository users = provider.GetRequiredService<IUserRepository>();
        IAgentRepository agents = provider.GetRequiredService<IAgentRepository>();
        IAgentVersionRepository versions = provider.GetRequiredService<IAgentVersionRepository>();
        IAgentConversationRepository conversations = provider.GetRequiredService<IAgentConversationRepository>();

        DateTimeOffset createdTime = new(2026, 7, 26, 0, 0, 0, TimeSpan.Zero);
        Guid ownerUserId = Guid.CreateVersion7();
        Guid agentId = Guid.CreateVersion7();
        Guid versionId = Guid.CreateVersion7();
        Guid conversationId = Guid.CreateVersion7();
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
            Name = "Session state agent",
            Instructions = "Test session state persistence.",
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
                Name = "Session state agent",
                Instructions = "Test session state persistence.",
                BuildOptions = buildOptions,
            },
            OwnerUserId = ownerUserId,
            CreatedTime = createdTime,
            UpdatedTime = createdTime,
            PublishedTime = createdTime,
        });
        AgentConversation conversation = await conversations.AddConversation(new AgentConversation
        {
            Id = conversationId,
            AgentId = agentId,
            AgentVersionId = versionId,
            OwnerUserId = ownerUserId,
            LastActivityTime = createdTime,
            CreatedTime = createdTime,
            UpdatedTime = createdTime,
        });

        return new SeededConversation(conversation.Id, createdTime);
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
