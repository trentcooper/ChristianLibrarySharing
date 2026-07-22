using ChristianLibrary.Domain.Enums;

namespace ChristianLibrary.Notifications.Rules
{
    /// <summary>
    /// The system-default reminder schedule for US-06.10: the four-tier cadence expressed as
    /// <see cref="ReminderRule"/>s — the same shape US-03.07 will use for per-user rules, so the
    /// evaluator is rule-driven from day one.
    /// </summary>
    /// <remarks>
    /// Tiers: DueSoon at 3 days before due, DueToday on the due date, Overdue at 3 days past due
    /// and weekly thereafter (Recurring). Email-only in US-06.10; SMS/Push arrive in US-07.09.
    /// </remarks>
    public static class SystemDefaultReminderRules
    {
        public static IReadOnlyList<ReminderRule> All { get; } = new[]
        {
            new ReminderRule(ReminderCategory.DueSoon,  -3, NotificationChannels.Email, recurring: false),
            new ReminderRule(ReminderCategory.DueToday,  0, NotificationChannels.Email, recurring: false),
            new ReminderRule(ReminderCategory.Overdue,   3, NotificationChannels.Email, recurring: true),
        };
    }
}