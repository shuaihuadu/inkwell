using Microsoft.Extensions.DependencyInjection;

namespace Inkwell;

/// <summary>
/// IServiceCollection 内省扩展
/// 用于在 DI 容器构建期（Build 之前）提前拿到已注册的 Keyed Singleton 实例
/// </summary>
public static class ServiceCollectionIntrospectionExtensions
{
    /// <summary>
    /// 在 IServiceCollection 中查找指定 Keyed Singleton 实例
    /// 仅匹配 KeyedImplementationInstance 形式的注册（即 AddKeyedSingleton(key, instance)）
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="key">服务 Key（区分大小写比较，因 Key 可为任意 object）</param>
    /// <returns>已注册的实例，未找到时返回 null</returns>
    public static T? FindKeyedSingletonInstance<T>(this IServiceCollection services, object key) where T : class
    {
        foreach (ServiceDescriptor descriptor in services)
        {
            if (descriptor.ServiceType == typeof(T)
                && descriptor.IsKeyedService
                && Equals(descriptor.ServiceKey, key)
                && descriptor.KeyedImplementationInstance is T instance)
            {
                return instance;
            }
        }

        return null;
    }

    /// <summary>
    /// 在 IServiceCollection 中查找已注册为单例实例的服务
    /// 仅匹配 ImplementationInstance 形式的注册（即 AddSingleton(instance)）
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>已注册的实例，未找到时返回 null</returns>
    public static T? FindSingletonInstance<T>(this IServiceCollection services) where T : class
    {
        foreach (ServiceDescriptor descriptor in services)
        {
            if (descriptor.ServiceType == typeof(T)
                && !descriptor.IsKeyedService
                && descriptor.ImplementationInstance is T instance)
            {
                return instance;
            }
        }

        return null;
    }
}
