using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Notifications.Rules;

namespace ChristianLibrary.Notifications.Messages
{
    /// <summary>
    /// Builds a <see cref="LoanReminderNotification"/> from a loan and the reminder rule that fired.
    /// The one place loan/user/book entities are mapped into the self-contained message DTO, and the
    /// one place the lender-copy opt-out is applied (a suppressed lender becomes a null recipient).
    /// </summary>
    /// <remarks>
    /// The behavioural fields — category, offset, channels, recipients, loan context — are a
    /// deterministic function of the inputs. The only non-deterministic values are the message's own
    /// identity/timestamp (MessageId, CreatedAtUtc), which a factory is expected to stamp. Requires the
    /// loan's Borrower, Lender, and Book navigations (with profiles) loaded.
    /// </remarks>
    public sealed class LoanReminderNotificationFactory
    {
        public LoanReminderNotification Create(
            Loan loan,
            ReminderRule firedRule,
            DateTime today,
            ReminderEvaluationContext context)
        {
            var daysFromDue = (today.Date - loan.DueDate.Date).Days;

            var borrower = new LoanReminderRecipient(
                loan.BorrowerId,
                loan.Borrower.Profile!.FullName,
                loan.Borrower.Email ?? string.Empty);

            LoanReminderRecipient? lender = context.LenderOptedOutOfReminderCopies
                ? null
                : new LoanReminderRecipient(
                    loan.LenderId,
                    loan.Lender.Profile!.FullName,
                    loan.Lender.Email ?? string.Empty);

            var loanContext = new LoanReminderContext(
                loan.Id,
                loan.Book.Title,
                loan.Book.Author,
                loan.DueDate,
                daysFromDue);

            return new LoanReminderNotification(
                MessageId: Guid.NewGuid(),
                CreatedAtUtc: DateTime.UtcNow,
                Category: firedRule.Category,
                OffsetDays: firedRule.OffsetDays,
                Channels: firedRule.Channels,
                Borrower: borrower,
                Lender: lender,
                LoanContext: loanContext);
        }
    }
}