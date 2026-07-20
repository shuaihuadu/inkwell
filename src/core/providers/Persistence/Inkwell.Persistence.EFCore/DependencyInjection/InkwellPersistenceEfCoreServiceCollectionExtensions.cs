// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Interceptors;
using Inkwell.Persistence.EFCore.Repositories;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Inkwell.Persistence.EFCore.DependencyInjection;

/// <summary>
/// 注册 EF Core 持久化家族的共享基础服务，不绑定具体数据库提供程序。
/// 最终适配器分别通过对应扩展方法注册数据库上下文选项和初始化器。
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
        services.AddScoped<IAgentVersionRepository, AgentVersionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAgentToolRepository, AgentToolRepository>();
        services.AddScoped<IAgentConversationRepository, AgentConversationRepository>();
        services.AddScoped<IAgentChatMessageRepository, AgentChatMessageRepository>();
        services.AddScoped<IAgentSkillRepository, AgentSkillRepository>();

        return services;
    }
}
