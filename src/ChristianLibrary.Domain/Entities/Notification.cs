using System.ComponentModel.DataAnnotations;
using ChristianLibrary.Domain.Enums;

namespace ChristianLibrary.Domain.Entities
{
    /// <summary>
    /// Represents a notification for a user
    /// Maps to US-07.01: Create notification data model
    /// </summary>
    public class Notification : BaseEntity
    {
        /// <summary>
        /// Foreign key to the user receiving the notification
        /// </summary>
        [Required]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Type/category of the notification
        /// </summary>
        public NotificationType Type { get; set; }

        /// <summary>
        /// Title of the notification
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Content/message of the notification
        /// </summary>
        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Whether the notification has been read
        /// </summary>
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// Date when the notification was read
        /// </summary>
        public DateTime? ReadAt { get; set; }

        /// <summary>
        /// Optional URL or action link
        /// </summary>
        [MaxLength(500)]
        public string? ActionUrl { get; set; }

        /// <summary>
        /// Optional reference to a book
        /// </summary>
        public int? BookId { get; set; }

        /// <summary>
        /// Optional reference to a borrow request
        /// </summary>
        public int? BorrowRequestId { get; set; }

        /// <summary>
        /// Optional reference to a loan
        /// </summary>
        public int? LoanId { get; set; }

        /// <summary>
        /// Optional reference to a message
        /// </summary>
        public int? MessageId { get; set; }

        /// <summary>
        /// Priority level (1=low, 2=normal, 3=high)
        /// </summary>
        public int Priority { get; set; } = 2;

        /// <summary>
        /// Whether an email notification was sent
        /// </summary>
        public bool EmailSent { get; set; } = false;

        /// <summary>
        /// Whether an SMS notification was sent
        /// </summary>
        public bool SmsSent { get; set; } = false;

        /// <summary>
        /// Whether a push notification was sent
        /// </summary>
        public bool PushSent { get; set; } = false;

        /// <summary>
        /// Date when the notification expires (for temporary notifications)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Navigation property to the user
        /// </summary>
        public virtual ApplicationUser User { get; set; } = null!;
    }
}