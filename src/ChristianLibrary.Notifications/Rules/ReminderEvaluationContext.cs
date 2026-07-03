namespace ChristianLibrary.Notifications.Rules
{
    /// <summary>
    /// Carries borrower and lender preference flags into the reminder rule evaluator.
    /// Separates the "who prefers what" from the loan itself, keeping preference state
    /// off the Loan entity.
    /// </summary>
    /// <remarks>
    /// In US-06.10 these flags are sourced from the existing per-user notification
    /// preferences (US-03.05). In US-03.07 this context will expand to carry
    /// per-channel preferences and per-loan overrides.
    ///
    /// Semantic note: <see cref="LenderOptedOutOfReminderCopies"/> does NOT suppress
    /// whether a reminder fires — it only affects whether the lender appears as a
    /// recipient on the resulting notification message.
    /// </remarks>
    public sealed record ReminderEvaluationContext(
        bool BorrowerOptedOutOfCourtesyReminders,
        bool LenderOptedOutOfReminderCopies);
}

