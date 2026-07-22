namespace ChristianLibrary.Services.Configuration
{
    /// <summary>
    /// Configuration for the reminder background service (US-06.10), bound from the
    /// "ReminderService" section of appsettings.json via the Options pattern.
    /// </summary>
    public sealed class ReminderServiceSettings
    {
        /// <summary>Master on/off switch for the reminder job.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Hour of day (0-23) at which the daily reminder pass runs. Default 8am.</summary>
        public int RunHour { get; set; } = 8;
    }
}