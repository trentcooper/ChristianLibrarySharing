using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Domain.Enums;
using ChristianLibrary.Notifications;
using ChristianLibrary.Notifications.Messages;
using ChristianLibrary.Notifications.Rules;
using FluentAssertions;
using Xunit;

namespace ChristianLibrary.UnitTests.Notifications;

public class LoanReminderNotificationFactoryTests
{
    private static readonly DateTime Today = new(2026, 1, 15);
    private static readonly LoanReminderNotificationFactory Factory = new();

    private static readonly ReminderRule OverdueRule =
        new(ReminderCategory.Overdue, 3, NotificationChannels.Email, recurring: true);

    private static Loan BuildLoan()
    {
        var borrower = new ApplicationUser
        {
            Id = "borrower-1",
            Email = "borrower@test.com",
            Profile = new UserProfile { UserId = "borrower-1", FirstName = "Betty", LastName = "Borrower" }
        };
        var lender = new ApplicationUser
        {
            Id = "lender-1",
            Email = "lender@test.com",
            Profile = new UserProfile { UserId = "lender-1", FirstName = "Larry", LastName = "Lender" }
        };
        var book = new Book { Title = "Mere Christianity", Author = "C.S. Lewis", OwnerId = "lender-1", Owner = lender };

        return new Loan
        {
            Id = 42,
            BorrowerId = "borrower-1",
            LenderId = "lender-1",
            Borrower = borrower,
            Lender = lender,
            Book = book,
            DueDate = Today.AddDays(-3),   // 3 days overdue → daysFromDue = +3
            Status = LoanStatus.Overdue,
        };
    }

    private static ReminderEvaluationContext Context(bool lenderOptedOut) =>
        new(BorrowerOptedOutOfCourtesyReminders: false, LenderOptedOutOfReminderCopies: lenderOptedOut);

    [Fact]
    public void Create_IncludesLender_WhenNotOptedOut()
    {
        var n = Factory.Create(BuildLoan(), OverdueRule, Today, Context(lenderOptedOut: false));

        n.Lender.Should().NotBeNull();
        n.Lender!.UserId.Should().Be("lender-1");
        n.Lender.DisplayName.Should().Be("Larry Lender");
        n.Lender.EmailAddress.Should().Be("lender@test.com");
    }

    [Fact]
    public void Create_OmitsLender_WhenOptedOut()
    {
        var n = Factory.Create(BuildLoan(), OverdueRule, Today, Context(lenderOptedOut: true));
        n.Lender.Should().BeNull();
    }

    [Fact]
    public void Create_AlwaysIncludesBorrower()
    {
        var n = Factory.Create(BuildLoan(), OverdueRule, Today, Context(lenderOptedOut: true));

        n.Borrower.UserId.Should().Be("borrower-1");
        n.Borrower.DisplayName.Should().Be("Betty Borrower");
        n.Borrower.EmailAddress.Should().Be("borrower@test.com");
    }

    [Fact]
    public void Create_CarriesRuleAndLoanContext()
    {
        var n = Factory.Create(BuildLoan(), OverdueRule, Today, Context(lenderOptedOut: false));

        n.Category.Should().Be(ReminderCategory.Overdue);
        n.OffsetDays.Should().Be(3);
        n.Channels.Should().Be(NotificationChannels.Email);

        n.LoanContext.LoanId.Should().Be(42);
        n.LoanContext.BookTitle.Should().Be("Mere Christianity");
        n.LoanContext.BookAuthor.Should().Be("C.S. Lewis");
        n.LoanContext.DueDate.Should().Be(Today.AddDays(-3));
        n.LoanContext.DaysFromDue.Should().Be(3);
    }
}