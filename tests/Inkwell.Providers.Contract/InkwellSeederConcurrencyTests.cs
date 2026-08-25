// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Security.Cryptography;
using System.Text.Json;
using Inkwell.Persistence.EFCore;
using Inkwell.Persistence.EFCore.Entities;
using Inkwell.Persistence.EFCore.Postgres.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Inkwell.Providers.Contract;

/// <summary>
/// 验证 ADR-024 §幂等性保证要求的加固：两个 <see cref="InkwellSeeder"/> 实例并发跑同一段 Seed
/// （模拟 <c>Inkwell.Migrator</c> Job 极端情况下短暂并发）时，不应有任一方抛异常失败，且最终
/// 数据库里只有一条 <c>admin</c> 用户记录、一条内置工具记录和一套示例 Agent 发布数据。
/// </summary>
[TestClass]
public sealed class InkwellSeederConcurrencyTests
{
    private const string ConfiguredAdminPassword = "configured-admin-password";
    private static PostgreSqlContainer? postgresContainer;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext _)
    {
        postgresContainer = new PostgreSqlBuilder(ContainerImageConfiguration.GetRequired("Tests:Postgres")).Build();

        await postgresContainer.StartAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        if (postgresContainer is not null)
        {
            await postgresContainer.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task Concurrent_SeedAsync_Does_Not_Throw_And_Inserts_Exactly_One_Seed_Data_SetAsync()
    {
        // Arrange
        ServiceProvider providerA = BuildServiceProvider();
        ServiceProvider providerB = BuildServiceProvider();

        await using (AsyncServiceScope scope = providerA.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<InkwellDbContext>().Database.EnsureCreatedAsync();
        }

        await using AsyncServiceScope scopeA = providerA.CreateAsyncScope();
        await using AsyncServiceScope scopeB = providerB.CreateAsyncScope();

        InkwellSeeder seederA = scopeA.ServiceProvider.GetRequiredService<InkwellSeeder>();
        InkwellSeeder seederB = scopeB.ServiceProvider.GetRequiredService<InkwellSeeder>();

        // Act
        // 两边都跑同一段 Seed（无 Id 依赖、纯按 Username 唯一键判定），模拟两个 Migrator Job 实例
        // 短暂并发执行的极端场景；两边都不应该向上抛异常。
        await Task.WhenAll(seederA.SeedAsync(), seederB.SeedAsync());

        await using AsyncServiceScope verifyScope = providerA.CreateAsyncScope();
        InkwellDbContext db = verifyScope.ServiceProvider.GetRequiredService<InkwellDbContext>();

        UserEntity admin = await db.Set<UserEntity>().SingleAsync(x => x.Username == "admin");
        AgentToolEntity currentDateTimeTool = await db.Set<AgentToolEntity>().SingleAsync(x => x.Name == "get_current_datetime");
        AgentEntity sampleAgent = await db.Set<AgentEntity>().SingleAsync(x => x.Id == Guid.Parse("00000000-0000-0000-0000-000000000301"));
        AgentVersionEntity sampleAgentVersion = await db.Set<AgentVersionEntity>().SingleAsync(x => x.AgentId == sampleAgent.Id);
        AgentBuildOptions sampleBuildOptions = JsonSerializer.Deserialize<AgentBuildOptions>(sampleAgent.BuildOptions)!;
        AgentSnapshot sampleSnapshot = JsonSerializer.Deserialize<AgentSnapshot>(sampleAgentVersion.Snapshot)!;
        string[] hashParts = admin.PasswordHash.Split('$');
        byte[] salt = Convert.FromBase64String(hashParts[2]);
        byte[] expectedHash = Convert.FromBase64String(hashParts[3]);
        byte[] configuredPasswordHash = Rfc2898DeriveBytes.Pbkdf2(ConfiguredAdminPassword, salt, int.Parse(hashParts[1]), HashAlgorithmName.SHA256, expectedHash.Length);
        byte[] defaultPasswordHash = Rfc2898DeriveBytes.Pbkdf2("admin", salt, int.Parse(hashParts[1]), HashAlgorithmName.SHA256, expectedHash.Length);

        // Assert
        Assert.AreEqual(4, hashParts.Length);
        Assert.AreEqual("PBKDF2", hashParts[0]);
        Assert.IsTrue(CryptographicOperations.FixedTimeEquals(configuredPasswordHash, expectedHash));
        Assert.IsFalse(CryptographicOperations.FixedTimeEquals(defaultPasswordHash, expectedHash));
        Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000101"), currentDateTimeTool.Id);
        Assert.AreEqual(admin.Id, sampleAgent.OwnerUserId);
        Assert.AreEqual("Inkwell 小助手", sampleAgent.Name);
        Assert.IsTrue(sampleAgent.IsShared);
        Assert.AreEqual(1, sampleAgent.LatestPublishedVersionNumber);
        Assert.AreEqual(sampleAgentVersion.Id, sampleAgent.CurrentPublishedVersionId);
        Assert.AreEqual(1, sampleAgentVersion.VersionNumber);
        Assert.AreEqual(sampleAgent.Name, sampleSnapshot.Name);
        Assert.AreEqual(sampleAgent.Description, sampleSnapshot.Description);
        Assert.AreEqual(sampleAgent.Instructions, sampleSnapshot.Instructions);
        Assert.AreEqual("gpt-5.4", sampleBuildOptions.ModelOptions.ModelId);
        Assert.AreEqual(0.2, sampleBuildOptions.ModelOptions.Temperature);
        Assert.AreEqual(0.9, sampleBuildOptions.ModelOptions.TopP);
        Assert.AreEqual(4096, sampleBuildOptions.ModelOptions.MaxTokens);
        Assert.HasCount(1, sampleBuildOptions.ToolBindings);
        Assert.AreEqual(currentDateTimeTool.Id, sampleBuildOptions.ToolBindings[0].ToolId);
        Assert.IsNull(sampleBuildOptions.ToolBindings[0].ParametersJson);
        Assert.IsEmpty(sampleBuildOptions.Skills);
        Assert.IsTrue(JsonElement.DeepEquals(
            JsonSerializer.SerializeToElement(sampleBuildOptions),
            JsonSerializer.SerializeToElement(sampleSnapshot.BuildOptions)));
        StringAssert.Contains(sampleAgent.Instructions, "保存草稿不会影响当前正式对话");
        StringAssert.Contains(sampleAgent.Instructions, "不把规划、设计或原型中的功能说成已经可用");
        Assert.IsEmpty(
            JsonDocument.Parse(currentDateTimeTool.ParametersJsonSchema)
                .RootElement
                .GetProperty("properties")
                .EnumerateObject()
                .ToArray());

        await db.Set<AgentToolEntity>()
            .Where(tool => tool.Name == "get_current_datetime")
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                tool => tool.ParametersJsonSchema,
                "{\"type\":\"object\",\"properties\":{\"timeZoneId\":{\"type\":\"string\"}}}"));

        await using AsyncServiceScope refreshScope = providerA.CreateAsyncScope();
        await refreshScope.ServiceProvider.GetRequiredService<InkwellSeeder>().SeedAsync();
        InkwellDbContext refreshedDb = refreshScope.ServiceProvider.GetRequiredService<InkwellDbContext>();
        string refreshedSchema = await refreshedDb.Set<AgentToolEntity>()
            .Where(tool => tool.Name == "get_current_datetime")
            .Select(tool => tool.ParametersJsonSchema)
            .SingleAsync();

        Assert.IsEmpty(
            JsonDocument.Parse(refreshedSchema)
                .RootElement
                .GetProperty("properties")
                .EnumerateObject()
                .ToArray());

        await refreshedDb.Set<AgentVersionEntity>()
            .Where(version => version.AgentId == sampleAgent.Id)
            .ExecuteDeleteAsync();
        await refreshedDb.Set<AgentEntity>()
            .Where(agent => agent.Id == sampleAgent.Id)
            .ExecuteDeleteAsync();

        await using AsyncServiceScope restoreScope = providerA.CreateAsyncScope();
        await restoreScope.ServiceProvider.GetRequiredService<InkwellSeeder>().SeedAsync();
        InkwellDbContext restoredDb = restoreScope.ServiceProvider.GetRequiredService<InkwellDbContext>();

        Assert.AreEqual(1, await restoredDb.Set<AgentEntity>().CountAsync(agent => agent.Id == sampleAgent.Id));
        Assert.AreEqual(1, await restoredDb.Set<AgentVersionEntity>().CountAsync(version => version.AgentId == sampleAgent.Id));
    }

    private static ServiceProvider BuildServiceProvider()
    {
        ServiceCollection services = new();
        services.AddLogging();

        Dictionary<string, string?> configurationValues = new()
        {
            ["Inkwell:Persistence:Seed:AdminPassword"] = ConfiguredAdminPassword,
            ["Inkwell:Persistence:Seed:SampleDataEnabled"] = "true",
            ["Inkwell:Persistence:Seed:AgentModelId"] = "gpt-5.4",
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();
        IInkwellBuilder builder = services.AddInkwell(configuration);

        builder.UsePostgres(postgresContainer!.GetConnectionString());

        return builder.Services.BuildServiceProvider();
    }
}
