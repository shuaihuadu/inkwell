// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Interceptors;
using Inkwell.Persistence.EFCore.Repositories;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Inkwell.Persistence.EFCore.DependencyInjection;

/// <summary>
/// 注册 EFCore family shared base 服务；不绑定 Provider（final adapter csproj 各自 <c>Use*</c> 扩展方法
/// 注册 <c>DbContext</c> 选项 + <see cref="IDbContextInitializer"/>）。
/// </summary>
internal static class InkwellPersistenceEfCoreServiceCollectionExtensions
{
    internal static IServiceCollection AddEfCorePersistenceBase(this IServiceCollection services)
    {
        services.AddOptions<PersistenceOptions>().BindConfiguration("Inkwell:Persistence");

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IPersistenceProvider, EfCorePersistenceProvider>();
        services.AddSingleton<ISaveChangesInterceptor, AuditingSaveChangesInterceptor>();
        services.AddScoped<InkwellSeeder>();
        services.AddScoped<MigrationRunner>();

        services.AddScoped<IAgentRepository, AgentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAgentToolRepository, AgentToolRepository>();
        services.AddScoped<IAgentConversationRepository, AgentConversationRepository>();
        services.AddScoped<IAgentConversationMessageRepository, AgentConversationMessageRepository>();
        services.AddScoped<IAgentSkillRepository, AgentSkillRepository>();

        return services;
    }
}
