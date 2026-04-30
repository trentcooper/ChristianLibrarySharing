using ChristianLibrary.Notifications.Interfaces;
using ChristianLibrary.Notifications.Messages;

namespace ChristianLibrary.Notifications.Implementations
{
    /// <summary>
    /// In-memory notification publisher that logs notifications via Serilog.
    /// Used in US-06.10 for development and testing; replaced by an SMTP-delivering
    /// implementation in US-07.01 and a RabbitMQ-publishing implementation in US-07.08.
    /// </summary>
    public sealed class InMemoryNotificationPublisher : INotificationPublisher
    {
        public Task PublishAsync(
            LoanReminderNotification notification,
            CancellationToken cancellationToken = default)
        {
            // Implementation deferred to Step 5 (US-06.10).
            // The real body will log a structured Serilog event with the full notification context.
            return Task.CompletedTask;
        }
    }
}