using ChristianLibrary.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ChristianLibrary.Domain.Entities;

/// <summary>
/// Represents extended profile information for a user
/// Maps to US-03.01: Create user profile data model
/// </summary>
public class UserProfile : BaseEntity
{
    /// <summary>
    /// Foreign key to ApplicationUser
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    // Basic Information

    /// <summary>
    /// User's first name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User's bio or about me section
    /// </summary>
    [MaxLength(1000)]
    public string? Bio { get; set; }

    /// <summary>
    /// Phone number
    /// </summary>
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Date of birth (optional)
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Profile picture URL or path
    /// </summary>
    [MaxLength(500)]
    public string? ProfilePictureUrl { get; set; }
    
    /// <summary>
    /// Profile picture thumbnail URL or path (US-03.04)
    /// </summary>
    [MaxLength(500)]
    public string? ProfilePictureThumbnailUrl { get; set; }

    // Location/Address Information (US-03.02)

    /// <summary>
    /// Street address
    /// </summary>
    [MaxLength(200)]
    public string? Street { get; set; }

    /// <summary>
    /// City
    /// </summary>
    [MaxLength(100)]
    public string? City { get; set; }

    /// <summary>
    /// State or province
    /// </summary>
    [MaxLength(100)]
    public string? State { get; set; }

    /// <summary>
    /// Postal/ZIP code
    /// </summary>
    [MaxLength(20)]
    public string? ZipCode { get; set; }

    /// <summary>
    /// Country
    /// </summary>
    [MaxLength(100)]
    public string? Country { get; set; }

    /// <summary>
    /// Latitude for geocoding (US-03.02)
    /// </summary>
    public decimal? Latitude { get; set; }

    /// <summary>
    /// Longitude for geocoding (US-03.02)
    /// </summary>
    public decimal? Longitude { get; set; }

    // Church/Community Affiliation (US-03.04 - Post-MVP)

    /// <summary>
    /// Church name or affiliation (US-03.04)
    /// </summary>
    [MaxLength(200)]
    public string? ChurchName { get; set; }

    /// <summary>
    /// Church location
    /// </summary>
    [MaxLength(200)]
    public string? ChurchLocation { get; set; }

    // Profile Visibility Settings (US-03.03)

    /// <summary>
    /// Overall profile visibility level
    /// </summary>
    public ProfileVisibility Visibility { get; set; } = ProfileVisibility.Public;

    /// <summary>
    /// Whether to show full name publicly
    /// </summary>
    public bool ShowFullName { get; set; } = true;

    /// <summary>
    /// Whether to show email publicly
    /// </summary>
    public bool ShowEmail { get; set; } = false;

    /// <summary>
    /// Whether to show phone number publicly
    /// </summary>
    public bool ShowPhone { get; set; } = false;

    /// <summary>
    /// Whether to show exact address publicly
    /// </summary>
    public bool ShowExactAddress { get; set; } = false;

    /// <summary>
    /// Whether to show city/state only
    /// </summary>
    public bool ShowCityState { get; set; } = true;

    /// <summary>
    /// Whether to show date of birth publicly
    /// </summary>
    public bool ShowDateOfBirth { get; set; } = false;

    // Notification Preferences (US-03.05)

    /// <summary>
    /// Whether to receive email notifications
    /// </summary>
    public bool EmailNotifications { get; set; } = true;

    /// <summary>
    /// Whether to receive SMS notifications
    /// </summary>
    public bool SmsNotifications { get; set; } = false;

    /// <summary>
    /// Whether to receive push notifications
    /// </summary>
    public bool PushNotifications { get; set; } = true;

    /// <summary>
    /// Notification frequency: Immediate, Daily, Weekly
    /// </summary>
    public NotificationFrequency NotificationFrequency { get; set; } = NotificationFrequency.Immediate;

    /// <summary>
    /// Notify on new borrow requests
    /// </summary>
    public bool NotifyOnBorrowRequest { get; set; } = true;

    /// <summary>
    /// Notify when request is approved
    /// </summary>
    public bool NotifyOnRequestApproval { get; set; } = true;

    /// <summary>
    /// Notify when request is denied
    /// </summary>
    public bool NotifyOnRequestDenial { get; set; } = true;

    /// <summary>
    /// Notify on book due date reminders
    /// </summary>
    public bool NotifyOnDueDate { get; set; } = true;

    /// <summary>
    /// Notify when borrowed book is returned
    /// </summary>
    public bool NotifyOnReturn { get; set; } = true;

    /// <summary>
    /// Notify on new messages
    /// </summary>
    public bool NotifyOnNewMessage { get; set; } = true;

    // Navigation Properties

    /// <summary>
    /// Navigation property to ApplicationUser
    /// </summary>
    public virtual ApplicationUser User { get; set; } = null!;

    // Computed Properties

    /// <summary>
    /// Computed property for full name
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";
}