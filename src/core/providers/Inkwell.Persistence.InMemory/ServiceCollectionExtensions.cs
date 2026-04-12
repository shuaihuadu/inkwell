using Inkwell;
using Inkwell.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Inkwell.Persistence.InMemory;

/// <summary>
/// InMemory 持久化扩展方法
/// </summary>
public static class InkwellCoreBuilderInMemoryExtensions
{
    /// <summary>
    /// 使用 InMemory 数据库作为持久化存储（适用于开发调试）
    /// </summary>
    /// <param name="builder">Inkwell 核心构建器</param>
    /// <param name="databaseName">内存数据库名称</param>
    /// <returns>Inkwell 核心构建器</returns>
    public static InkwellCoreBuilder UseInMemoryDatabase(this InkwellCoreBuilder builder, string databaseName = "InkwellDb")
    {
        builder.Services.AddDbContext<InkwellDbContext>(options =>
        {
            options.UseInMemoryDatabase(databaseName);
        });

        builder.Services.AddInkwellEfCorePersistence();

        return builder;
    }
}
