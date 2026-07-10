using Microsoft.Extensions.AI;

namespace Inkwell;

/// <summary>
/// <see cref="AgentChatMessage"/>（Inkwell 自有 DTO）与 <see cref="ChatMessage"/>（<c>Microsoft.Extensions.AI</c>）
/// 之间的双向映射。供 <see cref="AzureOpenAIAgentRuntime"/> 与 <see cref="DatabaseChatHistoryProvider"/> 共用，
/// 避免同一段映射逻辑在两处重复维护。
/// </summary>
internal static class AgentChatMessageMapper
{
    /// <summary>将 Inkwell 自有的 <see cref="AgentChatMessage"/> 转换为 MAF 的 <see cref="ChatMessage"/>。</summary>
    /// <param name="message">要转换的消息。</param>
    /// <returns>转换后的 <see cref="ChatMessage"/>。</returns>
    public static ChatMessage ToChatMessage(AgentChatMessage message)
    {
        ChatMessage chatMessage = new(message.Role, [.. message.Content]);

        if (message.AuthorName is not null)
        {
            chatMessage.AuthorName = message.AuthorName;
        }

        return chatMessage;
    }

    /// <summary>将 MAF 的 <see cref="ChatMessage"/> 转换为 Inkwell 自有的 <see cref="AgentChatMessage"/>。</summary>
    /// <param name="message">要转换的消息。</param>
    /// <returns>转换后的 <see cref="AgentChatMessage"/>。</returns>
    public static AgentChatMessage ToAgentChatMessage(ChatMessage message) => new()
    {
        Role = message.Role,
        Content = [.. message.Contents.Where(c => c is TextContent or UriContent or DataContent)],
        AuthorName = message.AuthorName,
    };
}
