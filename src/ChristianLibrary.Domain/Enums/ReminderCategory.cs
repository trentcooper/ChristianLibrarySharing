namespace ChristianLibrary.Domain.Enums
{
    /// <summary>
    /// Categorizes a reminder by its temporal relationship to the loan due date.
    /// The actual offset in days is carried separately on the rule and on the loan record.
    /// Maps to US-06.10
    /// </summary>
    public enum ReminderCategory
    {
        /// <summary>
        /// Reminder fired before the due date.
        /// Considered a courtesy and may be gated by user preferences.
        /// </summary>
        DueSoon = 1,

        /// <summary>
        /// Reminder fired on the due date itself.
        /// Considered a courtesy and may be gated by user preferences.
        /// </summary>
        DueToday = 2,

        /// <summary>
        /// Reminder fired after the due date.
        /// Always sent regardless of user preferences.
        /// </summary>
        Overdue = 3
    }
}