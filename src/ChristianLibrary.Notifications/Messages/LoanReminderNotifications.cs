using ChristianLibrary.Domain.Enums;

namespace ChristianLibrary.Notifications.Messages
{
    /// <summary>
    /// A notification message indicating a reminder should be delivered for a specific loan.
    /// Self-contained DTO — does not reference any EF entities. Safe for serialization
    /// and for cross-process delivery via a message broker (US-07.08).
    /// Maps to US-06.10
    /// </summary>
    public sealed record LoanReminderNotification(
        Guid MessageId,
        DateTime CreatedAtUtc,
        ReminderCategory Category,
        int OffsetDays,
        NotificationChannels Channels,
        LoanReminderRecipient Borrower,
        LoanReminderRecipient Lender,
        LoanReminderContext LoanContext);

    /// <summary>
    /// Identifies a recipient of a loan reminder notification.
    /// </summary>
    public sealed record LoanReminderRecipient(
        string UserId,
        string DisplayName,
        string EmailAddress);

    /// <summary>
    /// Loan-specific context attached to a reminder notification.
    /// All fields are denormalized onto the message so consumers don't need
    /// to query the loan record or perform date arithmetic.
    /// </summary>
    public sealed record LoanReminderContext(
        int LoanId,
        string BookTitle,
        string BookAuthor,
        DateTime DueDate,
        int DaysFromDue);  // negative = before due, zero = on due, positive = overdue
}