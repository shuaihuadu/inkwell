using System.ComponentModel.DataAnnotations;

namespace Inkwell;

public sealed class AgentOptions
{
    [Range(1, int.MaxValue)]
    public int? MaxAgentsPerOwner { get; set; }

    [Range(1000, 1000000)]
    public int InstructionsWarningThresholdChars { get; set; } = 32000;

    public bool EnableSensitiveDataLogging { get; set; }
}
