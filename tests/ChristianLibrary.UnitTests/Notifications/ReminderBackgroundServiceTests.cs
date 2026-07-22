using ChristianLibrary.Data.Context;
using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Domain.Enums;
using ChristianLibrary.Notifications.Interfaces;
using ChristianLibrary.Notifications.Messages;
using ChristianLibrary.Notifications.Rules;
using ChristianLibrary.Services.BackgroundServices;
using ChristianLibrary.Services.Configuration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ChristianLibrary.UnitTests.Notifications;

public class ReminderBackgroundServiceTests
{
    private static readonly DateTime Today = new(2026, 1, 15);

    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ReminderBackgroundService CreateService(INotificationPublisher publisher) =>
        new(
            new Mock<IServiceScopeFactory>().Object,          // unused by ProcessLoansAsync
            new ReminderRuleEvaluator(),
            new LoanReminderNotificationFactory(),
            publisher,
            Options.Create(new ReminderServiceSettings()),
            new Mock<ILogger<ReminderBackgroundService>>().Object);

    // daysFromDue: negative = before due, 0 = on due, positive = overdue.
    private static async Task<Loan> SeedLoanAsync(
        ApplicationDbContext db,
        string key,
        int daysFromDue,
        LoanStatus status = LoanStatus.Active,
        DateTime? returnedDate = null,
        bool borrowerNotifyOnDueDate = true,
        bool lenderNotifyOnLoanReminderCopies = true,
        bool borrowerHasProfile = true)
    {
        var borrower = new ApplicationUser
        {
            Id = $"borrower-{key}",
            UserName = $"borrower-{key}@test.com",
            Email = $"borrower-{key}@test.com",
            Profile = borrowerHasProfile
                ? new UserProfile
                {
                    UserId = $"borrower-{key}", FirstName = "Betty", LastName = "Borrower",
                    NotifyOnDueDate = borrowerNotifyOnDueDate
                }
                : null
        };
        var lender = new ApplicationUser
        {
            Id = $"lender-{key}",
            UserName = $"lender-{key}@test.com",
            Email = $"lender-{key}@test.com",
            Profile = new UserProfile
            {
                UserId = $"lender-{key}", FirstName = "Larry", LastName = "Lender",
                NotifyOnLoanReminderCopies = lenderNotifyOnLoanReminderCopies
            }
        };
        var book = new Book { Title = $"Book {key}", Author = "Author", OwnerId = lender.Id, Owner = lender };

        var loan = new Loan
        {
            BorrowerId = borrower.Id,
            LenderId = lender.Id,
            Borrower = borrower,
            Lender = lender,
            Book = book,
            Status = status,
            DueDate = Today.AddDays(-daysFromDue),
            ReturnedDate = returnedDate,
            CreatedAt = Today.AddDays(-30),
        };

        db.Users.AddRange(borrower, lender);
        db.Books.Add(book);
        db.Loans.Add(loan);
        await db.SaveChangesAsync();
        return loan;
    }

    [Fact]
    public async Task ProcessLoansAsync_PublishesAndStampsDueLoans_AndStaysSilentOnClosedOrEarly()
    {
        // Arrange
        await using var db = CreateInMemoryContext();
        var dueSoon  = await SeedLoanAsync(db, "duesoon", daysFromDue: -3);
        var overdue  = await SeedLoanAsync(db, "overdue", daysFromDue: 3);
        var returned = await SeedLoanAsync(db, "returned", daysFromDue: 3,
                                           status: LoanStatus.Returned, returnedDate: Today.AddDays(-1));
        var tooEarly = await SeedLoanAsync(db, "early", daysFromDue: -10);

        var publisher = new CapturingPublisher();
        var service = CreateService(publisher);

        // Act
        await service.ProcessLoansAsync(db, Today, CancellationToken.None);

        // Assert — two reminders fired: DueSoon + Overdue
        publisher.Published.Should().HaveCount(2);
        publisher.Published.Select(n => n.Category)
            .Should().BeEquivalentTo(new[] { ReminderCategory.DueSoon, ReminderCategory.Overdue });

        // the two due loans got stamped
        var dueSoonReloaded = await db.Loans.FindAsync(dueSoon.Id);
        dueSoonReloaded!.LastReminderCategory.Should().Be(ReminderCategory.DueSoon);
        dueSoonReloaded.RemindersSent.Should().Be(1);
        dueSoonReloaded.LastReminderSentAt.Should().NotBeNull();

        var overdueReloaded = await db.Loans.FindAsync(overdue.Id);
        overdueReloaded!.LastReminderOffsetDays.Should().Be(3);   // actual days-from-due, not nominal

        // closed + too-early loans untouched
        (await db.Loans.FindAsync(returned.Id))!.RemindersSent.Should().Be(0);
        (await db.Loans.FindAsync(tooEarly.Id))!.RemindersSent.Should().Be(0);
    }

    [Fact]
    public async Task ProcessLoansAsync_SuppressesLenderCopy_WhenLenderOptedOut()
    {
        await using var db = CreateInMemoryContext();
        await SeedLoanAsync(db, "optout", daysFromDue: 3, lenderNotifyOnLoanReminderCopies: false);

        var publisher = new CapturingPublisher();
        var service = CreateService(publisher);

        await service.ProcessLoansAsync(db, Today, CancellationToken.None);

        publisher.Published.Should().ContainSingle();
        publisher.Published[0].Lender.Should().BeNull();       // cc suppressed end-to-end
        publisher.Published[0].Borrower.Should().NotBeNull();  // borrower still notified
    }

    [Fact]
    public async Task ProcessLoansAsync_SkipsLoanMissingProfile_AndProcessesTheRest()
    {
        await using var db = CreateInMemoryContext();
        await SeedLoanAsync(db, "broken", daysFromDue: 3, borrowerHasProfile: false);
        var good = await SeedLoanAsync(db, "good", daysFromDue: 3);

        var publisher = new CapturingPublisher();
        var service = CreateService(publisher);

        // Must not throw despite the incomplete loan.
        await service.ProcessLoansAsync(db, Today, CancellationToken.None);

        publisher.Published.Should().ContainSingle();          // only the good loan
        (await db.Loans.FindAsync(good.Id))!.RemindersSent.Should().Be(1);
    }

    private sealed class CapturingPublisher : INotificationPublisher
    {
        public readonly List<LoanReminderNotification> Published = new();

        public Task PublishAsync(LoanReminderNotification notification, CancellationToken cancellationToken = default)
        {
            Published.Add(notification);
            return Task.CompletedTask;
        }
    }
}