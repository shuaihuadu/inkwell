using Inkwell;
using Inkwell.Persistence.InMemory;

namespace Inkwell.WebApi;

/// <summary>
/// Inkwell Web API 入口
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // 注册 Controller
        builder.Services.AddControllers();

        // 注册 Inkwell 核心服务 + 持久化
        // 方式一：Fluent API（显式指定）
        builder.Services.AddInkwellCore().UseInMemoryDatabase();

        // 方式二：配置文件驱动（从 appsettings.json 的 Persistence 节点读取）
        // builder.Services.AddInkwellCore().UseConfiguredPersistence(builder.Configuration);

        // 配置 CORS（允许前端开发服务器访问）
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        WebApplication app = builder.Build();

        app.UseCors();
        app.MapControllers();

        app.Run();
    }
}
