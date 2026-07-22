using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Domain.Enums;
using ChristianLibrary.Notifications.Rules;
using FluentAssertions;
using Xunit;

namespace ChristianLibrary.UnitTests.Notifications;

public class ReminderRuleEvaluatorTests
{
    private static readonly DateTime Today = new(2026, 1, 15);
    private static readonly ReminderRuleEvaluator Evaluator = new();
    private static readonly IReadOnlyList<ReminderRule> Rules = SystemDefaultReminderRules.All;

    private static ReminderEvaluationContext Context(
        bool borrowerOptedOutOfCourtesy = false,
        bool lenderOptedOutOfCopies = false)
        => new(borrowerOptedOutOfCourtesy, lenderOptedOutOfCopies);

    // daysFromDue uses the evaluator's frame: negative = before due, 0 = on due, positive = overdue.
    private static Loan BuildLoan(
        int daysFromDue,
        LoanStatus status = LoanStatus.Active,
        DateTime? returnedDate = null,
        bool isDeleted = false,
        ReminderCategory? lastReminderCategory = null,
        int? lastReminderOffsetDays = null,
        int createdDaysBeforeDue = 30)
    {
        var due = Today.AddDays(-daysFromDue);
        return new Loan
        {
            Status = status,
            DueDate = due,
            CreatedAt = due.AddDays(-createdDaysBeforeDue),
            ReturnedDate = returnedDate,
            IsDeleted = isDeleted,
            LastReminderCategory = lastReminderCategory,
            LastReminderOffsetDays = lastReminderOffsetDays,
        };
    }

    private static void AssertCategory(ReminderRule? result, ReminderCategory? expected)
    {
        if (expected is null)
            result.Should().BeNull();
        else
            result!.Category.Should().Be(expected.Value);
    }

    // -------------------------------------------------------
    // First-time: which tier fires when nothing has been sent yet
    // -------------------------------------------------------

    [Theory]
    [InlineData(-3, ReminderCategory.DueSoon)]
    [InlineData(0,  ReminderCategory.DueToday)]
    [InlineData(3,  ReminderCategory.Overdue)]
    [InlineData(-5, null)]
    public void FirstTime_SelectsTierForCurrentDay(int daysFromDue, ReminderCategory? expected)
    {
        var loan = BuildLoan(daysFromDue);
        var result = Evaluator.Evaluate(loan, Today, Rules, Context());
        AssertCategory(result, expected);
    }

    // -------------------------------------------------------
    // Already-sent: never repeat a one-shot tier; advance to the next
    // -------------------------------------------------------

    [Theory]
    [InlineData(-2, ReminderCategory.DueSoon,  -3, null)]                       // DueSoon sent, still before due
    [InlineData(0,  ReminderCategory.DueToday, 0,  null)]                       // DueToday sent, still on due date
    [InlineData(0,  ReminderCategory.DueSoon,  -3, ReminderCategory.DueToday)]  // DueSoon sent, now due date
    [InlineData(3,  ReminderCategory.DueToday, 0,  ReminderCategory.Overdue)]   // DueToday sent, now overdue
    public void AlreadySent_DoesNotRepeatTierButAdvances(
        int daysFromDue, ReminderCategory lastCategory, int lastOffset, ReminderCategory? expected)
    {
        var loan = BuildLoan(daysFromDue, lastReminderCategory: lastCategory, lastReminderOffsetDays: lastOffset);
        var result = Evaluator.Evaluate(loan, Today, Rules, Context());
        AssertCategory(result, expected);
    }

    // -------------------------------------------------------
    // Recurring overdue: fires again only after the weekly cadence
    // -------------------------------------------------------

    [Theory]
    [InlineData(10, 3,  ReminderCategory.Overdue)]  // exactly a week since +3 -> fire
    [InlineData(8,  3,  null)]                        // 5 days since +3 -> wait
    [InlineData(9,  3,  null)]                        // 6 days since +3 -> wait
    [InlineData(17, 10, ReminderCategory.Overdue)]  // a week since +10 -> fire
    [InlineData(24, 3,  ReminderCategory.Overdue)]  // long gap -> still just one overdue (no flood)
    public void RecurringOverdue_HonorsWeeklyCadence(
        int daysFromDue, int lastOverdueOffset, ReminderCategory? expected)
    {
        var loan = BuildLoan(
            daysFromDue,
            lastReminderCategory: ReminderCategory.Overdue,
            lastReminderOffsetDays: lastOverdueOffset);
        var result = Evaluator.Evaluate(loan, Today, Rules, Context());
        AssertCategory(result, expected);
    }

    // -------------------------------------------------------
    // Preference gating: courtesy honors opt-out; overdue always fires
    // -------------------------------------------------------

    [Theory]
    [InlineData(-3, null)]                      // DueSoon suppressed
    [InlineData(0,  null)]                      // DueToday suppressed
    [InlineData(3,  ReminderCategory.Overdue)]  // overdue ignores the preference
    public void CourtesyOptOut_SuppressesCourtesyButNotOverdue(int daysFromDue, ReminderCategory? expected)
    {
        var loan = BuildLoan(daysFromDue);
        var result = Evaluator.Evaluate(loan, Today, Rules, Context(borrowerOptedOutOfCourtesy: true));
        AssertCategory(result, expected);
    }

    [Fact]
    public void LenderOptOut_DoesNotSuppressFiring()
    {
        // Lender opting out only drops the cc downstream; it must not change whether a reminder fires.
        var loan = BuildLoan(-3);
        var result = Evaluator.Evaluate(loan, Today, Rules, Context(lenderOptedOutOfCopies: true));
        AssertCategory(result, ReminderCategory.DueSoon);
    }

    // -------------------------------------------------------
    // Closed / removed loans: always silent, even when overdue
    // -------------------------------------------------------

    [Fact]
    public void ReturnedStatus_SendsNothing_EvenWhenOverdue()
    {
        var loan = BuildLoan(3, status: LoanStatus.Returned);
        Evaluator.Evaluate(loan, Today, Rules, Context()).Should().BeNull();
    }

    [Fact]
    public void ReturnedDateSet_SendsNothing_EvenWhenOverdue()
    {
        var loan = BuildLoan(3, returnedDate: Today.AddDays(-1));
        Evaluator.Evaluate(loan, Today, Rules, Context()).Should().BeNull();
    }

    [Fact]
    public void SoftDeleted_SendsNothing_EvenWhenOverdue()
    {
        var loan = BuildLoan(3, isDeleted: true);
        Evaluator.Evaluate(loan, Today, Rules, Context()).Should().BeNull();
    }

    // -------------------------------------------------------
    // Create-date floor: never fire a tier dated before the loan existed
    // -------------------------------------------------------

    [Theory]
    [InlineData(-1, null)]                        // DueSoon tier predates a 2-day loan -> suppressed
    [InlineData(0,  ReminderCategory.DueToday)]  // DueToday still lands after creation -> fires
    public void CreateDateFloor_SuppressesTiersBeforeLoanExisted(int daysFromDue, ReminderCategory? expected)
    {
        var loan = BuildLoan(daysFromDue, createdDaysBeforeDue: 2);
        var result = Evaluator.Evaluate(loan, Today, Rules, Context());
        AssertCategory(result, expected);
    }

    // -------------------------------------------------------
    // Extension pending is still an open, past-due loan -> late notices continue
    // -------------------------------------------------------

    [Fact]
    public void ExtensionRequested_PastDue_StillFiresOverdue()
    {
        var loan = BuildLoan(3, status: LoanStatus.ExtensionRequested);
        var result = Evaluator.Evaluate(loan, Today, Rules, Context());
        AssertCategory(result, ReminderCategory.Overdue);
    }
}