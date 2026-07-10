using System.ComponentModel.DataAnnotations;

namespace Inkwell;

public sealed class QueueOptions
{
    [Range(1, 100)]
    public int MaxDeliveryAttempts { get; set; } = 3;

    [Range(1, 3600)]
    public int VisibilityTimeoutSeconds { get; set; } = 300;

    [Range(1, 168)]
    public int DlqRetentionHours { get; set; } = 24;

    public bool EnableSensitiveDataLogging { get; set; }
}
