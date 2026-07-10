using System.ComponentModel.DataAnnotations;

namespace Inkwell;

public sealed class AgentConversationOptions
{
    [Range(1, int.MaxValue)]
    public int? MaxMessagesPerConversation { get; set; }

    public bool EnableSensitiveDataLogging { get; set; }
}
