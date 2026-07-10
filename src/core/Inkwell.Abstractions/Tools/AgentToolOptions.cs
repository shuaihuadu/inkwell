using System.ComponentModel.DataAnnotations;

namespace Inkwell;

public sealed class AgentToolOptions
{
    [Range(1, int.MaxValue)]
    public int? MaxToolsPerAgent { get; set; }

    public bool EnableSensitiveDataLogging { get; set; }
}
