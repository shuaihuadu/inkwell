// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore;
using Inkwell.Persistence.EFCore.Postgres.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Inkwell.Providers.Contract;

/// <summary>
/// design-review-report.md §21 B20 / HD-012 标注的"未验证假设"验证 spike：
/// <c>PostgresRowVersionInterceptor</c>（手工 8 字节大端计数器模拟 RowVersion）在真实 PostgreSQL 上
/// 是否正确工作——(1) 每次写入 RowVersion 是否真的递增并持久化，(2) 并发写入陈旧 RowVersion 是否被
/// EF Core 正确识别为 <see cref="DbUpdateConcurrencyException"/>。跑一次即可归档结论，不是长期回归测试。
/// </summary>
[TestClass]
public sealed class PostgresRowVersionSpikeTests
{
    private static PostgreSqlContainer? container;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext _)
    {
        container = new PostgreSqlBuilder("postgres:17-alpine")
            .Build();

        await container.StartAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task RowVersion_Increments_And_Persists_On_Each_UpdateAsync()
    {
        ServiceProvider provider = BuildServiceProvider();

        await using (AsyncServiceScope scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<InkwellDbContext>().Database.EnsureCreatedAsync();
        }

        Guid agentId = Guid.CreateVersion7();
        Guid ownerUserId = Guid.CreateVersion7();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await using (AsyncServiceScope scope = provider.CreateAsyncScope())
        {
            IAgentRepository agents = scope.ServiceProvider.GetRequiredService<IAgentRepository>();

            await agents.AddAgent(new AgentDefinition
            {
                Id = agentId,
                OwnerUserId = ownerUserId,
                CreatedTime = now,
                UpdatedTime = now,
            });
        }

        byte[] rowVersionAfterInsert;

        await using (AsyncServiceScope scope = provider.CreateAsyncScope())
        {
            IAgentRepository agents = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
            AgentDefinition agent = await agents.GetAgent(agentId);

            rowVersionAfterInsert = agent.RowVersion;

            Assert.IsNotNull(rowVersionAfterInsert);
            Assert.AreEqual(8, rowVersionAfterInsert.Length, "PostgresRowVersionInterceptor 约定 8 字节大端计数器。");

            await agents.UpdateAgent(agent with { IsShared = true });
        }

        await using (AsyncServiceScope scope = provider.CreateAsyncScope())
        {
            IAgentRepository agents = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
            AgentDefinition agent = await agents.GetAgent(agentId);

            CollectionAssert.AreNotEqual(rowVersionAfterInsert, agent.RowVersion, "Update 之后 RowVersion 应该递增，不能保持不变。");
        }
    }

    [TestMethod]
    public async Task Concurrent_Update_With_Stale_RowVersion_Throws_ConcurrencyConflictAsync()
    {
        ServiceProvider provider = BuildServiceProvider();

        await using (AsyncServiceScope scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<InkwellDbContext>().Database.EnsureCreatedAsync();
        }

        Guid agentId = Guid.CreateVersion7();
        Guid ownerUserId = Guid.CreateVersion7();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await using (AsyncServiceScope scope = provider.CreateAsyncScope())
        {
            IAgentRepository agents = scope.ServiceProvider.GetRequiredService<IAgentRepository>();

            await agents.AddAgent(new AgentDefinition
            {
                Id = agentId,
                OwnerUserId = ownerUserId,
                CreatedTime = now,
                UpdatedTime = now,
            });
        }

        // 模拟两个并发 actor：各自独立 scope 读同一行，各自持有各自读到的 RowVersion 快照。
        await using AsyncServiceScope scopeA = provider.CreateAsyncScope();
        await using AsyncServiceScope scopeB = provider.CreateAsyncScope();

        IAgentRepository agentsA = scopeA.ServiceProvider.GetRequiredService<IAgentRepository>();
        IAgentRepository agentsB = scopeB.ServiceProvider.GetRequiredService<IAgentRepository>();

        AgentDefinition agentSeenByA = await agentsA.GetAgent(agentId);
        AgentDefinition agentSeenByB = await agentsB.GetAgent(agentId);

        // A 先提交成功，RowVersion 在 DB 里递增。
        await agentsA.UpdateAgent(agentSeenByA with { IsShared = true });

        // B 仍拿着旧的 RowVersion 提交，理论上应该被识别为并发冲突。
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => agentsB.UpdateAgent(agentSeenByB with { SharedRevokedByAdminTime = DateTimeOffset.UtcNow }));
    }

    private static ServiceProvider BuildServiceProvider()
    {
        ServiceCollection services = new();
        services.AddLogging();

        // AddInkwell(Action<InkwellOptions>) 纯程式化重载已于 2026-07-09 删除（唯一真实调用方 WebApi/Worker
        // 都是走真实 IConfiguration 这条路，没必要再维护一条零配置的入口）；这里统一用真实但为空的 IConfiguration。
        IInkwellBuilder builder = services.AddInkwell(new ConfigurationBuilder().Build());

        builder.UsePostgres(container!.GetConnectionString());

        return builder.Services.BuildServiceProvider();
    }
}
