// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence;
using Inkwell.Persistence.EFCore;
using Inkwell.Persistence.EFCore.Postgres.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Inkwell.Providers.Contract;

/// <summary>
/// 验证 PostgreSQL Agent Version Repository 的快照持久化契约。
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class AgentVersionPostgresRepositoryTests
{
    private static PostgreSqlContainer? container;

    /// <summary>
    /// 启动真实 PostgreSQL 测试容器。
    /// </summary>
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

    /// <summary>
    /// 释放 PostgreSQL 测试容器。
    /// </summary>
    /// <returns>表示异步清理操作的任务。</returns>
    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
        }
    }

    /// <summary>
    /// 验证版本按降序读取，并完整保留 Tool 参数、Skill 内容和 Chat History 配置。
    /// </summary>
    [TestMethod]
    public async Task VersionCrud_AddListAndFind_PreservesCompleteSnapshotsAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        Guid ownerUserId = Guid.CreateVersion7();
        Guid agentId = Guid.CreateVersion7();
        Guid toolId = Guid.CreateVersion7();
        Guid skillId = Guid.CreateVersion7();
        DateTimeOffset createdTime = new(2026, 8, 24, 0, 0, 0, TimeSpan.Zero);
        await using ServiceProvider provider = BuildServiceProvider();
        IUserRepository users = provider.GetRequiredService<IUserRepository>();
        IAgentRepository agents = provider.GetRequiredService<IAgentRepository>();
        IAgentVersionRepository versions = provider.GetRequiredService<IAgentVersionRepository>();
        _ = await users.AddUser(CreateUser(ownerUserId, createdTime));
        _ = await agents.AddAgent(CreateAgent(agentId, ownerUserId, createdTime));
        AgentVersion versionOne = CreateVersion(agentId, ownerUserId, 1, "short", toolId, skillId, createdTime);
        AgentVersion versionTwo = CreateVersion(agentId, ownerUserId, 2, "long", toolId, skillId, createdTime.AddMinutes(1));

        // Act
        _ = await versions.AddVersionAsync(versionOne);
        _ = await versions.AddVersionAsync(versionTwo);
        IReadOnlyList<AgentVersion> listed = await versions.ListVersionsByAgentAsync(agentId);
        AgentVersion loaded = await versions.GetVersionAsync(versionTwo.Id);
        IReadOnlyDictionary<Guid, AgentVersion> found = await versions.FindVersionsByIdsAsync([versionOne.Id, versionTwo.Id]);

        // Assert
        CollectionAssert.AreEqual(new[] { 2, 1 }, listed.Select(version => version.VersionNumber).ToArray());
        Assert.HasCount(2, found);
        Assert.AreEqual("{\"format\":\"long\"}", loaded.Snapshot.BuildOptions.ToolBindings.Single().ParametersJson);
        AgentSkillDefinition skill = loaded.Snapshot.BuildOptions.Skills.Single();
        Assert.AreEqual("Review the release.", skill.Content);
        CollectionAssert.AreEqual(
            new[] { new Uri("inkwell://skills/references/guide.md") },
            skill.ReferenceFileUris.ToArray());
        Assert.AreEqual(24, loaded.Snapshot.BuildOptions.ChatHistoryOptions?.MaxMessages);
    }

    private static User CreateUser(Guid ownerUserId, DateTimeOffset createdTime) => new()
    {
        Id = ownerUserId,
        Username = $"owner-{ownerUserId:N}"[..20],
        PasswordHash = "hash",
        CreatedTime = createdTime,
        UpdatedTime = createdTime,
    };

    private static AgentDefinition CreateAgent(Guid agentId, Guid ownerUserId, DateTimeOffset createdTime) => new()
    {
        Id = agentId,
        OwnerUserId = ownerUserId,
        Name = "Version contract agent",
        Instructions = "Current draft",
        BuildOptions = new AgentBuildOptions
        {
            ModelOptions = new AgentModelOptions { ModelId = "test-model" },
        },
        CreatedTime = createdTime,
        UpdatedTime = createdTime,
    };

    private static AgentVersion CreateVersion(
        Guid agentId,
        Guid ownerUserId,
        int versionNumber,
        string format,
        Guid toolId,
        Guid skillId,
        DateTimeOffset createdTime) => new()
        {
            Id = Guid.CreateVersion7(),
            AgentId = agentId,
            VersionNumber = versionNumber,
            Snapshot = new AgentSnapshot
            {
                Name = "Version contract agent",
                Instructions = $"Published version {versionNumber}",
                BuildOptions = new AgentBuildOptions
                {
                    ModelOptions = new AgentModelOptions
                    {
                        ModelId = "test-model",
                        Temperature = 0.2,
                        MaxTokens = 2048,
                    },
                    ChatHistoryOptions = new AgentChatHistoryOptions
                    {
                        MaxMessages = 24,
                        ReducerType = "message-count",
                        MaxMessagesToRetrieve = 48,
                    },
                    ToolBindings = [new AgentToolBinding(toolId, $"{{\"format\":\"{format}\"}}")],
                    Skills =
                [
                    new AgentSkillDefinition
                    {
                        Id = skillId,
                        OwnerUserId = ownerUserId,
                        Name = "release-review",
                        Description = "Review release content.",
                        Content = "Review the release.",
                        ReferenceFileUris = [new Uri("inkwell://skills/references/guide.md")],
                        CreatedTime = createdTime,
                        UpdatedTime = createdTime,
                    },
                ],
                },
            },
            OwnerUserId = ownerUserId,
            ChangeSummary = $"Version {versionNumber}",
            CreatedTime = createdTime,
            UpdatedTime = createdTime,
            PublishedTime = createdTime,
        };

    private static async Task ResetDatabaseAsync()
    {
        await using ServiceProvider provider = BuildServiceProvider();
        InkwellDbContext db = provider.GetRequiredService<InkwellDbContext>();
        _ = await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE agent_chat_messages, agent_session_states, agent_conversations, agent_versions, agents, users CASCADE;");
    }

    private static ServiceProvider BuildServiceProvider()
    {
        ServiceCollection services = new();
        services.AddLogging();
        IInkwellBuilder builder = services.AddInkwell(new ConfigurationBuilder().Build());
        builder.UsePostgres(container!.GetConnectionString());
        return services.BuildServiceProvider();
    }
}
