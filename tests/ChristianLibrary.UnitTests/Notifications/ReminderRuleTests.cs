using ChristianLibrary.Domain.Enums;
using ChristianLibrary.Notifications;
using ChristianLibrary.Notifications.Rules;
using FluentAssertions;
using Xunit;

namespace ChristianLibrary.UnitTests.Notifications;

public class ReminderRuleTests
{
    // -------------------------------------------------------
    // Valid construction
    // -------------------------------------------------------

    [Fact]
    public void ValidRule_ConstructsAndExposesValues()
    {
        var rule = new ReminderRule(ReminderCategory.Overdue, 3, NotificationChannels.Email, recurring: true);

        rule.Category.Should().Be(ReminderCategory.Overdue);
        rule.OffsetDays.Should().Be(3);
        rule.Channels.Should().Be(NotificationChannels.Email);
        rule.Recurring.Should().BeTrue();
    }

    [Theory]
    [InlineData(ReminderCategory.DueSoon,  -3)]
    [InlineData(ReminderCategory.DueSoon,  -1)]
    [InlineData(ReminderCategory.DueToday,  0)]
    [InlineData(ReminderCategory.Overdue,   1)]
    [InlineData(ReminderCategory.Overdue,   3)]
    public void SignMatchingCategory_DoesNotThrow(ReminderCategory category, int offsetDays)
    {
        var act = () => new ReminderRule(category, offsetDays, NotificationChannels.Email, recurring: false);
        act.Should().NotThrow();
    }

    // -------------------------------------------------------
    // Invariants
    // -------------------------------------------------------

    [Fact]
    public void NoChannel_Throws()
    {
        var act = () => new ReminderRule(ReminderCategory.DueSoon, -3, NotificationChannels.None, recurring: false);

        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("channels");
    }

    [Fact]
    public void UndefinedCategory_Throws()
    {
        var act = () => new ReminderRule((ReminderCategory)99, 0, NotificationChannels.Email, recurring: false);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be("category");
    }

    [Theory]
    [InlineData(ReminderCategory.DueSoon,   0)]   // before-due tier must be negative
    [InlineData(ReminderCategory.DueSoon,   3)]
    [InlineData(ReminderCategory.DueToday, -1)]   // due-day tier must be zero
    [InlineData(ReminderCategory.DueToday,  1)]
    [InlineData(ReminderCategory.Overdue,   0)]   // overdue tier must be positive
    [InlineData(ReminderCategory.Overdue,  -3)]
    public void SignMismatchingCategory_Throws(ReminderCategory category, int offsetDays)
    {
        var act = () => new ReminderRule(category, offsetDays, NotificationChannels.Email, recurring: false);

        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("offsetDays");
    }
}