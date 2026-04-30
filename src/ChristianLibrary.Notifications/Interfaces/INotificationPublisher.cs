using ChristianLibrary.Notifications.Messages;

namespace ChristianLibrary.Notifications.Interfaces
{
    /// <summary>
    /// Publishes notifications for downstream delivery.
    /// </summary>
    /// <remarks>
    /// US-06.10 ships InMemoryNotificationPublisher, which logs notifications via Serilog.
    /// US-07.01 will add an email-delivering implementation behind this interface.
    /// US-07.08 will add a RabbitMQ-publishing implementation, also behind this interface.
    ///
    /// A batch overload (PublishBatchAsync) may be added in a future story when a concrete
    /// implementation benefits from batching (e.g., MassTransit/RabbitMQ in US-07.08, or
    /// HTTP-based providers in US-07.09). The current interface is intentionally minimal
    /// under YAGNI; the BackgroundService publishes from a collection, so adding a batch
    /// overload later is a small refactor at the call site.
    /// </remarks>
    public interface INotificationPublisher
    {
        Task PublishAsync(LoanReminderNotification notification, CancellationToken cancellationToken = default);
    }
}