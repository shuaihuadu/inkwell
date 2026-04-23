using Microsoft.Extensions.AI;

namespace Inkwell;

/// <summary>
/// AI Chat 模型供应商插件接口
/// </summary>
/// <remarks>
/// 实现类作为 DI Singleton 注册，构造函数可注入 <c>ILogger</c> / <c>IHttpClientFactory</c> / <c>TokenCredential</c> 等依赖。
/// 每种 Provider 从配置字典 <see cref="AIEndpointOptions"/> 中各自取用关心的字段
/// </remarks>
public interface IAIChatProvider
{
    /// <summary>
    /// 获取 Provider 名称，与配置 <see cref="AIEndpointOptions.Provider"/> 字段大小写不敏感匹配
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 根据端点配置创建 <see cref="IChatClient"/> 实例
    /// </summary>
    /// <param name="options">端点配置</param>
    /// <returns>Chat 客户端</returns>
    IChatClient CreateChatClient(AIEndpointOptions options);
}
