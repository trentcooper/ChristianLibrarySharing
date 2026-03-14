using ChristianLibrary.Domain.Entities;
using ChristianLibrary.Domain.Enums;
using FluentAssertions;

namespace ChristianLibrary.UnitTests.Domain;

public class DomainModelTests
{
    // -------------------------------------------------------
    // BaseEntity Tests
    // -------------------------------------------------------

    [Fact]
    public void BaseEntity_IsDeleted_DefaultsToFalse()
    {
        var book = new Book();
        book.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void BaseEntity_DeletedAt_DefaultsToNull()
    {
        var book = new Book();
        book.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void BaseEntity_UpdatedAt_DefaultsToNull()
    {
        var book = new Book();
        book.UpdatedAt.Should().BeNull();
    }

    // -------------------------------------------------------
    // BorrowRequest Tests
    // -------------------------------------------------------

    [Fact]
    public void BorrowRequest_Status_DefaultsToPending()
    {
        var request = new BorrowRequest();
        request.Status.Should().Be(BorrowRequestStatus.Pending);
    }

    [Fact]
    public void BorrowRequest_BorrowerId_DefaultsToEmptyString()
    {
        var request = new BorrowRequest();
        request.BorrowerId.Should().Be(string.Empty);
    }

    [Fact]
    public void BorrowRequest_LenderId_DefaultsToEmptyString()
    {
        var request = new BorrowRequest();
        request.LenderId.Should().Be(string.Empty);
    }

    [Fact]
    public void BorrowRequest_Message_DefaultsToNull()
    {
        var request = new BorrowRequest();
        request.Message.Should().BeNull();
    }

    [Fact]
    public void BorrowRequest_ResponseMessage_DefaultsToNull()
    {
        var request = new BorrowRequest();
        request.ResponseMessage.Should().BeNull();
    }

    [Fact]
    public void BorrowRequest_RespondedAt_DefaultsToNull()
    {
        var request = new BorrowRequest();
        request.RespondedAt.Should().BeNull();
    }

    // -------------------------------------------------------
    // Loan Tests
    // -------------------------------------------------------

    [Fact]
    public void Loan_Status_DefaultsToActive()
    {
        var loan = new Loan();
        loan.Status.Should().Be(LoanStatus.Active);
    }

    [Fact]
    public void Loan_ExtensionDays_DefaultsToZero()
    {
        var loan = new Loan();
        loan.ExtensionDays.Should().Be(0);
    }

    [Fact]
    public void Loan_ExtensionRequested_DefaultsToFalse()
    {
        var loan = new Loan();
        loan.ExtensionRequested.Should().BeFalse();
    }

    [Fact]
    public void Loan_RemindersSent_DefaultsToZero()
    {
        var loan = new Loan();
        loan.RemindersSent.Should().Be(0);
    }

    [Fact]
    public void Loan_ReturnedDate_DefaultsToNull()
    {
        var loan = new Loan();
        loan.ReturnedDate.Should().BeNull();
    }

    [Fact]
    public void Loan_IsOverdue_ReturnsTrueWhenActiveAndPastDueDate()
    {
        var loan = new Loan
        {
            Status = LoanStatus.Active,
            DueDate = DateTime.UtcNow.AddDays(-1) // yesterday
        };

        loan.IsOverdue.Should().BeTrue();
    }

    [Fact]
    public void Loan_IsOverdue_ReturnsFalseWhenDueDateInFuture()
    {
        var loan = new Loan
        {
            Status = LoanStatus.Active,
            DueDate = DateTime.UtcNow.AddDays(7)
        };

        loan.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void Loan_IsOverdue_ReturnsFalseWhenStatusIsNotActive()
    {
        var loan = new Loan
        {
            Status = LoanStatus.Returned,
            DueDate = DateTime.UtcNow.AddDays(-5) // past due but returned
        };

        loan.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void Loan_DaysUntilDue_ReturnsPositiveWhenDueDateInFuture()
    {
        var loan = new Loan
        {
            DueDate = DateTime.UtcNow.AddDays(7)
        };

        loan.DaysUntilDue.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Loan_DaysUntilDue_ReturnsNegativeWhenOverdue()
    {
        var loan = new Loan
        {
            DueDate = DateTime.UtcNow.AddDays(-3)
        };

        loan.DaysUntilDue.Should().BeLessThan(0);
    }

    // -------------------------------------------------------
    // Message Tests
    // -------------------------------------------------------

    [Fact]
    public void Message_IsRead_DefaultsToFalse()
    {
        var message = new Message();
        message.IsRead.Should().BeFalse();
    }

    [Fact]
    public void Message_ReadAt_DefaultsToNull()
    {
        var message = new Message();
        message.ReadAt.Should().BeNull();
    }

    [Fact]
    public void Message_SenderDeleted_DefaultsToFalse()
    {
        var message = new Message();
        message.SenderDeleted.Should().BeFalse();
    }

    [Fact]
    public void Message_RecipientDeleted_DefaultsToFalse()
    {
        var message = new Message();
        message.RecipientDeleted.Should().BeFalse();
    }

    [Fact]
    public void Message_Content_DefaultsToEmptyString()
    {
        var message = new Message();
        message.Content.Should().Be(string.Empty);
    }

    // -------------------------------------------------------
    // UserProfile Tests
    // -------------------------------------------------------

    [Fact]
    public void UserProfile_FullName_CombinesFirstAndLastName()
    {
        var profile = new UserProfile
        {
            FirstName = "John",
            LastName = "Bunyan"
        };

        profile.FullName.Should().Be("John Bunyan");
    }

    [Fact]
    public void UserProfile_Visibility_DefaultsToPublic()
    {
        var profile = new UserProfile();
        profile.Visibility.Should().Be(ProfileVisibility.Public);
    }

    [Fact]
    public void UserProfile_ShowFullName_DefaultsToTrue()
    {
        var profile = new UserProfile();
        profile.ShowFullName.Should().BeTrue();
    }

    [Fact]
    public void UserProfile_ShowEmail_DefaultsToFalse()
    {
        var profile = new UserProfile();
        profile.ShowEmail.Should().BeFalse();
    }

    [Fact]
    public void UserProfile_ShowPhone_DefaultsToFalse()
    {
        var profile = new UserProfile();
        profile.ShowPhone.Should().BeFalse();
    }

    [Fact]
    public void UserProfile_ShowExactAddress_DefaultsToFalse()
    {
        var profile = new UserProfile();
        profile.ShowExactAddress.Should().BeFalse();
    }

    [Fact]
    public void UserProfile_ShowCityState_DefaultsToTrue()
    {
        var profile = new UserProfile();
        profile.ShowCityState.Should().BeTrue();
    }

    [Fact]
    public void UserProfile_EmailNotifications_DefaultsToTrue()
    {
        var profile = new UserProfile();
        profile.EmailNotifications.Should().BeTrue();
    }

    [Fact]
    public void UserProfile_SmsNotifications_DefaultsToFalse()
    {
        var profile = new UserProfile();
        profile.SmsNotifications.Should().BeFalse();
    }

    [Fact]
    public void UserProfile_PushNotifications_DefaultsToTrue()
    {
        var profile = new UserProfile();
        profile.PushNotifications.Should().BeTrue();
    }

    [Fact]
    public void UserProfile_NotificationFrequency_DefaultsToImmediate()
    {
        var profile = new UserProfile();
        profile.NotificationFrequency.Should().Be(NotificationFrequency.Immediate);
    }

    [Fact]
    public void UserProfile_AllNotifyFlags_DefaultToTrue()
    {
        var profile = new UserProfile();

        profile.NotifyOnBorrowRequest.Should().BeTrue();
        profile.NotifyOnRequestApproval.Should().BeTrue();
        profile.NotifyOnRequestDenial.Should().BeTrue();
        profile.NotifyOnDueDate.Should().BeTrue();
        profile.NotifyOnReturn.Should().BeTrue();
        profile.NotifyOnNewMessage.Should().BeTrue();
    }
}