using ChristianLibrary.Domain.Enums;

namespace ChristianLibrary.Notifications.Rules
{
    /// <summary>
    /// A single reminder rule: "for this <see cref="ReminderCategory"/>, fire a notification
    /// <see cref="OffsetDays"/> days relative to the loan's due date, over these
    /// <see cref="Channels"/>, optionally repeating on a cadence."
    /// </summary>
    /// <remarks>
    /// Invariants are enforced at construction so an invalid rule cannot exist:
    /// <list type="bullet">
    ///   <item><description><see cref="Channels"/> must target at least one channel
    ///     (never <see cref="NotificationChannels.None"/>).</description></item>
    ///   <item><description><see cref="OffsetDays"/> must agree in sign with <see cref="Category"/>:
    ///     DueSoon is before due (negative), DueToday is on the due date (zero),
    ///     Overdue is after due (positive).</description></item>
    /// </list>
    /// The sign convention matches LoanReminderContext.DaysFromDue: negative = before due,
    /// zero = on due, positive = overdue.
    ///
    /// In US-06.10 the only source of rules is SystemDefaultReminderRules. In US-03.07 the same
    /// shape carries per-user rules and per-loan overrides — which is why the invariants live here
    /// on the rule itself, not in the system-defaults factory.
    ///
    /// <see cref="Recurring"/> marks a rule that repeats after its first firing (the weekly overdue
    /// drumbeat). The cadence is a convention owned by the evaluator in US-06.10; a later story may
    /// promote it to an explicit field.
    /// </remarks>
    public sealed record ReminderRule
    {
        public ReminderCategory Category { get; }
        public int OffsetDays { get; }
        public NotificationChannels Channels { get; }
        public bool Recurring { get; }

        public ReminderRule(
            ReminderCategory category,
            int offsetDays,
            NotificationChannels channels,
            bool recurring)
        {
            if (!Enum.IsDefined(typeof(ReminderCategory), category))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(category), category, "Unknown reminder category.");
            }

            if (channels == NotificationChannels.None)
            {
                throw new ArgumentException(
                    "A reminder rule must target at least one channel.", nameof(channels));
            }

            var signMatchesCategory = category switch
            {
                ReminderCategory.DueSoon  => offsetDays < 0,
                ReminderCategory.DueToday => offsetDays == 0,
                ReminderCategory.Overdue  => offsetDays > 0,
                _ => false
            };

            if (!signMatchesCategory)
            {
                throw new ArgumentException(
                    $"OffsetDays {offsetDays} is invalid for category {category}: " +
                    "DueSoon must be negative, DueToday zero, Overdue positive.",
                    nameof(offsetDays));
            }

            Category = category;
            OffsetDays = offsetDays;
            Channels = channels;
            Recurring = recurring;
        }
    }
}
