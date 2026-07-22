using ChristianLibrary.Domain.Enums;
using ChristianLibrary.Notifications.Interfaces;
using ChristianLibrary.Notifications.Messages;
using Microsoft.Extensions.Logging;

namespace ChristianLibrary.Notifications.Implementations
{
    /// <summary>
    /// US-06.10 notification publisher: it does not deliver anything, it records each notification
    /// as a structured log event. It logs through the Microsoft.Extensions.Logging abstraction
    /// (<c>ILogger&lt;T&gt;</c>), which the host routes to Serilog as the configured provider —
    /// so the event lands in Serilog with its named properties (Category, LoanId, ...) intact.
    /// Real delivery arrives behind this same interface — SMTP in US-07.01, RabbitMQ in US-07.08.
    /// </summary>
    /// <remarks>
    /// Severity mirrors the domain distinction: courtesy reminders (DueSoon / DueToday) log at
    /// Information; late notices (Overdue) log at Warning, so an overdue book stands out in the logs.
    /// Stateless and thread-safe → registered as a singleton.
    /// </remarks>
    public sealed class InMemoryNotificationPublisher : INotificationPublisher
    {
        private readonly ILogger<InMemoryNotificationPublisher> _logger;

        public InMemoryNotificationPublisher(ILogger<InMemoryNotificationPublisher> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync(
            LoanReminderNotification notification,
            CancellationToken cancellationToken = default)
        {
            var level = notification.Category == ReminderCategory.Overdue
                ? LogLevel.Warning
                : LogLevel.Information;

            _logger.Log(
                level,
                "Loan reminder published: {Category} for loan {LoanId} ({BookTitle}) — " +
                "borrower {BorrowerId}, due {DueDate:yyyy-MM-dd}, {DaysFromDue} day(s) from due, " +
                "channels {Channels}, message {MessageId}",
                notification.Category,
                notification.LoanContext.LoanId,
                notification.LoanContext.BookTitle,
                notification.Borrower.UserId,
                notification.LoanContext.DueDate,
                notification.LoanContext.DaysFromDue,
                notification.Channels,
                notification.MessageId);

            return Task.CompletedTask;
        }
    }
}