using ChristianLibrary.Domain.Enums;
using ChristianLibrary.Notifications;
using ChristianLibrary.Notifications.Implementations;
using ChristianLibrary.Notifications.Messages;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ChristianLibrary.UnitTests.Notifications;

public class InMemoryNotificationPublisherTests
{
    [Theory]
    [InlineData(ReminderCategory.DueSoon,  LogLevel.Information)]
    [InlineData(ReminderCategory.DueToday, LogLevel.Information)]
    [InlineData(ReminderCategory.Overdue,  LogLevel.Warning)]   // late notice = louder
    public async Task PublishAsync_LogsAtSeverityMatchingCategory(ReminderCategory category, LogLevel expected)
    {
        // Arrange
        var logger = new CapturingLogger<InMemoryNotificationPublisher>();
        var publisher = new InMemoryNotificationPublisher(logger);

        // Act
        await publisher.PublishAsync(BuildNotification(category));

        // Assert
        logger.Entries.Should().ContainSingle();
        logger.Entries[0].Level.Should().Be(expected);
    }

    [Fact]
    public async Task PublishAsync_LogMessageCarriesLoanContext()
    {
        // Arrange
        var logger = new CapturingLogger<InMemoryNotificationPublisher>();
        var publisher = new InMemoryNotificationPublisher(logger);

        // Act
        await publisher.PublishAsync(BuildNotification(ReminderCategory.Overdue, loanId: 42, bookTitle: "Mere Christianity"));

        // Assert
        logger.Entries[0].Message.Should().Contain("42").And.Contain("Mere Christianity").And.Contain("Overdue");
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------

    private static LoanReminderNotification BuildNotification(
        ReminderCategory category, int loanId = 1, string bookTitle = "Test Book")
    {
        var borrower = new LoanReminderRecipient("borrower-1", "Borrower User", "borrower@test.com");
        var lender = new LoanReminderRecipient("lender-1", "Lender User", "lender@test.com");
        var context = new LoanReminderContext(loanId, bookTitle, "Author", new DateTime(2026, 1, 15), DaysFromDue: 0);

        return new LoanReminderNotification(
            Guid.NewGuid(), new DateTime(2026, 1, 15), category,
            OffsetDays: 0, NotificationChannels.Email, borrower, lender, context);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public readonly List<(LogLevel Level, string Message)> Entries = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}