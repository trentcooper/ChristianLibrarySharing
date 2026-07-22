using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Domain.Enums;

namespace ChristianLibrary.Notifications.Rules
{
    /// <summary>
    /// Pure decision function: given a loan, the current day, the active rules, and the
    /// borrower/lender preference context, returns the single reminder rule that should fire
    /// right now — or null to send nothing. No clock, no I/O, no state. Same inputs → same output.
    /// </summary>
    public sealed class ReminderRuleEvaluator
    {
        private const int WeeklyCadenceDays = 7;

        public ReminderRule? Evaluate(
            Loan loan,
            DateTime today,
            IReadOnlyList<ReminderRule> rules,
            ReminderEvaluationContext context)
        {
            // 1. Silent for closed or removed loans.
            if (loan.IsDeleted || loan.Status == LoanStatus.Returned || loan.ReturnedDate is not null)
                return null;

            // 2. Whole-day position relative to the due date (server clock supplied as `today`).
            int daysFromDue = (today.Date - loan.DueDate.Date).Days;

            // 3. Most-recent-applicable rule: the latest tier we've reached.
            ReminderRule? candidate = rules
                .Where(r => daysFromDue >= r.OffsetDays)
                .OrderByDescending(r => r.OffsetDays)
                .FirstOrDefault();

            if (candidate is null)
                return null;                                 // too early — before the first tier

            // 4. Create-date floor: never fire a reminder dated before the loan existed.
            DateTime tierDate = loan.DueDate.Date.AddDays(candidate.OffsetDays);
            if (tierDate < loan.CreatedAt.Date)
                return null;

            // 5. Preference gating: courtesy honors borrower opt-out; overdue always fires.
            bool isCourtesy = candidate.Category is ReminderCategory.DueSoon or ReminderCategory.DueToday;
            if (isCourtesy && context.BorrowerOptedOutOfCourtesyReminders)
                return null;

            // 6. Don't repeat a tier; for the recurring overdue tier, honor the weekly cadence.
            if (loan.LastReminderCategory is ReminderCategory last)
            {
                if (candidate.Category < last)
                    return null;                             // already past this tier

                if (candidate.Category == last)
                {
                    if (!candidate.Recurring)
                        return null;                         // one-shot tier already sent

                    int daysSinceLast = daysFromDue - (loan.LastReminderOffsetDays ?? daysFromDue);
                    if (daysSinceLast < WeeklyCadenceDays)
                        return null;                         // not a week yet
                }
                // candidate.Category > last -> advanced to a new tier -> fall through and send
            }

            // 7. Fire: the matched rule IS the decision.
            return candidate;
        }
    }
}
