using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Inkwell;
using Inkwell.Persistence.EFCore;
using Inkwell.Persistence.EFCore.Postgres.DependencyInjection;

namespace Inkwell.Providers.Contract;

/// <summary>
/// 2026-07-09 回归测试：<c>UsePostgres</c> 内部 <c>BindConfiguration("Inkwell:Persistence:Postgres")</c>
/// 在配置里完全没有该节（真实但为空的 <see cref="IConfiguration"/>）时不应该崩溃，应静默保留
/// <c>PostgresPersistenceOptions</c> 的代码默认值（此前 <c>AddInkwell(Action&lt;InkwellOptions&gt;)</c>
/// 纯程式化重载 + <c>EmptyConfiguration</c> 占位实现踩过这个坑，2026-07-09 决定直接删掉该重载——
/// <c>AddInkwell</c> 现在只接受真实 <see cref="IConfiguration"/> 这一种入口，此测试改为验证"配置节缺失"场景）。
/// </summary>
[TestClass]
public sealed class AddInkwellProgrammaticPathTests
{
    [TestMethod]
    public void UsePostgres_With_Empty_Configuration_Does_Not_Throw_On_DbContext_Resolution()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        IInkwellBuilder builder = services.AddInkwell(new ConfigurationBuilder().Build());

        builder.UsePostgres("Host=localhost;Database=inkwell;Username=postgres;Password=postgres");

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        // 触发 AddDbContext 的 options 工厂 lambda（内部走 BindConfiguration("Inkwell:Persistence:Postgres")）；
        // 不需要真的连上数据库，只要 DI 解析阶段不抛异常即可证明"配置节缺失"场景被正确处理。
        InkwellDbContext dbContext = scope.ServiceProvider.GetRequiredService<InkwellDbContext>();

        Assert.IsNotNull(dbContext);
    }
}
