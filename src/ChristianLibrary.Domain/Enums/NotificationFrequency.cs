namespace ChristianLibrary.Domain.Enums;

/// <summary>
/// Defines how frequently notifications are delivered
/// </summary>
public enum NotificationFrequency
{
    /// <summary>
    /// Send notifications immediately when events occur
    /// </summary>
    Immediate = 1,

    /// <summary>
    /// Batch notifications and send once daily
    /// </summary>
    Daily = 2,

    /// <summary>
    /// Batch notifications and send once weekly
    /// </summary>
    Weekly = 3
}