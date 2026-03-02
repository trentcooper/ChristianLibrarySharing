using ChristianLibrary.Domain.Enums;

namespace ChristianLibrary.Services.DTOs.Profile;

/// <summary>
/// Response containing user's notification preferences
/// </summary>
public class NotificationPreferencesResponse
{
    // General Notification Settings
    public bool EmailNotifications { get; set; }
    public bool SmsNotifications { get; set; }
    public bool PushNotifications { get; set; }
    
    /// <summary>
    /// Notification frequency enum
    /// </summary>
    public NotificationFrequency NotificationFrequency { get; set; }
    
    /// <summary>
    /// Human-readable notification frequency text
    /// </summary>
    public string NotificationFrequencyText => NotificationFrequency switch
    {
        NotificationFrequency.Immediate => "Immediate - Notifications sent as events occur",
        NotificationFrequency.Daily => "Daily - Notifications batched once per day",
        NotificationFrequency.Weekly => "Weekly - Notifications batched once per week",
        _ => "Unknown"
    };

    // Specific Event Notifications
    public bool NotifyOnBorrowRequest { get; set; }
    public bool NotifyOnRequestApproval { get; set; }
    public bool NotifyOnRequestDenial { get; set; }
    public bool NotifyOnDueDate { get; set; }
    public bool NotifyOnReturn { get; set; }
    public bool NotifyOnNewMessage { get; set; }

    // Computed Properties
    
    /// <summary>
    /// Count of active notification channels (email, SMS, push)
    /// </summary>
    public int ActiveChannels
    {
        get
        {
            var count = 0;
            if (EmailNotifications) count++;
            if (SmsNotifications) count++;
            if (PushNotifications) count++;
            return count;
        }
    }

    /// <summary>
    /// List of enabled notification types
    /// </summary>
    public List<string> EnabledNotifications
    {
        get
        {
            var notifications = new List<string>();
            
            if (NotifyOnBorrowRequest) notifications.Add("Borrow Requests");
            if (NotifyOnRequestApproval) notifications.Add("Request Approvals");
            if (NotifyOnRequestDenial) notifications.Add("Request Denials");
            if (NotifyOnDueDate) notifications.Add("Due Date Reminders");
            if (NotifyOnReturn) notifications.Add("Book Returns");
            if (NotifyOnNewMessage) notifications.Add("New Messages");
            
            return notifications;
        }
    }

    /// <summary>
    /// Summary of notification preferences
    /// </summary>
    public string Summary
    {
        get
        {
            var channels = new List<string>();
            if (EmailNotifications) channels.Add("Email");
            if (SmsNotifications) channels.Add("SMS");
            if (PushNotifications) channels.Add("Push");
            
            var channelText = channels.Any() 
                ? string.Join(", ", channels) 
                : "No channels";

            var frequencyText = NotificationFrequency switch
            {
                NotificationFrequency.Immediate => "Immediate",
                NotificationFrequency.Daily => "Daily",
                NotificationFrequency.Weekly => "Weekly",
                _ => "Unknown"
            };

            return $"{channelText} | {frequencyText} | {EnabledNotifications.Count} event types";
        }
    }
}