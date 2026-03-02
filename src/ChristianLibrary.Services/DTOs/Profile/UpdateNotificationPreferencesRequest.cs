using ChristianLibrary.Domain.Enums;

namespace ChristianLibrary.Services.DTOs.Profile;

/// <summary>
/// Request to update user notification preferences
/// All fields are optional - only update what is provided
/// </summary>
public class UpdateNotificationPreferencesRequest
{
    // General Notification Settings

    /// <summary>
    /// Enable/disable email notifications
    /// </summary>
    public bool? EmailNotifications { get; set; }

    /// <summary>
    /// Enable/disable SMS notifications
    /// </summary>
    public bool? SmsNotifications { get; set; }

    /// <summary>
    /// Enable/disable push notifications
    /// </summary>
    public bool? PushNotifications { get; set; }

    /// <summary>
    /// Notification frequency: Immediate (1), Daily (2), Weekly (3)
    /// </summary>
    public NotificationFrequency? NotificationFrequency { get; set; }

    // Specific Event Notifications

    /// <summary>
    /// Notify when someone requests to borrow your book
    /// </summary>
    public bool? NotifyOnBorrowRequest { get; set; }

    /// <summary>
    /// Notify when your borrow request is approved
    /// </summary>
    public bool? NotifyOnRequestApproval { get; set; }

    /// <summary>
    /// Notify when your borrow request is denied
    /// </summary>
    public bool? NotifyOnRequestDenial { get; set; }

    /// <summary>
    /// Notify on book due date reminders
    /// </summary>
    public bool? NotifyOnDueDate { get; set; }

    /// <summary>
    /// Notify when a borrowed book is returned
    /// </summary>
    public bool? NotifyOnReturn { get; set; }

    /// <summary>
    /// Notify on new messages
    /// </summary>
    public bool? NotifyOnNewMessage { get; set; }
}