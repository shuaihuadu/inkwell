using Microsoft.Agents.AI;

namespace Inkwell;

/// <summary>
/// <see cref="RoutingAgent"/> 自身持有的会话占位对象。
/// </summary>
/// <remarks>
/// 真正的对话历史读写发生在 <see cref="AzureOpenAIAgentRuntime"/> 内部（<see cref="DatabaseChatHistoryProvider"/>
/// 按 <c>ConversationId</c> 自动拉取/写回），<see cref="RoutingAgent"/> 这一层路由壳不需要自己管理会话状态，
/// 这里只是满足 <see cref="AIAgent"/> 契约要求返回一个 <see cref="AgentSession"/> 实例的占位类型。
/// </remarks>
internal sealed class RoutingAgentSession : AgentSession;
