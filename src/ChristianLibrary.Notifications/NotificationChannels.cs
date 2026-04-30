namespace ChristianLibrary.Notifications
{
    /// <summary>
    /// Channels through which a notification may be delivered.
    /// Flags enum allows a single notification to target multiple channels simultaneously.
    /// In US-06.10 only Email is supported; Sms and Push delivery arrive in US-07.09.
    /// </summary>
    [Flags]
    public enum NotificationChannels
    {
        None  = 0,
        Email = 1 << 0,  // 1
        Sms   = 1 << 1,  // 2
        Push  = 1 << 2   // 4
    }
}