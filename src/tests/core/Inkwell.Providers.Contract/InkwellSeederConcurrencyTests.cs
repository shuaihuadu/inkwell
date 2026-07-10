using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Testcontainers.PostgreSql;
using Inkwell;
using Inkwell.Persistence.EFCore;
using Inkwell.Persistence.EFCore.Entities;
using Inkwell.Persistence.EFCore.Postgres.DependencyInjection;

namespace Inkwell.Providers.Contract;

/// <summary>
/// 验证 ADR-024 §幂等性保证要求的加固：两个 <see cref="InkwellSeeder"/> 实例并发跑同一段 Seed
/// （模拟 <c>Inkwell.Migrator</c> Job 极端情况下短暂并发）时，不应有任一方抛异常失败，且最终
/// 数据库里只有一条 <c>admin</c> 用户记录。
/// </summary>
[TestClass]
public sealed class InkwellSeederConcurrencyTests
{
    private static PostgreSqlContainer? s_container;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext _)
    {
        s_container = new PostgreSqlBuilder("postgres:17-alpine").Build();

        await s_container.StartAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        if (s_container is not null)
        {
            await s_container.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task Concurrent_SeedAsync_Does_Not_Throw_And_Inserts_Exactly_One_Admin()
    {
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

        // 两边都跑同一段 Seed（无 Id 依赖、纯按 Username 唯一键判定），模拟两个 Migrator Job 实例
        // 短暂并发执行的极端场景；两边都不应该向上抛异常。
        await Task.WhenAll(seederA.SeedAsync(), seederB.SeedAsync());

        await using AsyncServiceScope verifyScope = providerA.CreateAsyncScope();
        InkwellDbContext db = verifyScope.ServiceProvider.GetRequiredService<InkwellDbContext>();

        int adminCount = await db.Set<UserEntity>().CountAsync(x => x.Username == "admin");

        Assert.AreEqual(1, adminCount, "并发 Seed 之后应该只有一条 admin 用户记录，不能重复插入。");
    }

    private static ServiceProvider BuildServiceProvider()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        IInkwellBuilder builder = services.AddInkwell(new ConfigurationBuilder().Build());

        builder.UsePostgres(s_container!.GetConnectionString());

        return builder.Services.BuildServiceProvider();
    }
}
