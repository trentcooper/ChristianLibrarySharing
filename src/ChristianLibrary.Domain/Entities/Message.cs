using System.ComponentModel.DataAnnotations;

namespace ChristianLibrary.Domain.Entities
{
    /// <summary>
    /// Represents a message between users
    /// Maps to US-06.01: Create messaging data model
    /// </summary>
    public class Message : BaseEntity
    {
        /// <summary>
        /// Foreign key to the sender
        /// </summary>
        [Required]
        public string SenderId { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to the recipient
        /// </summary>
        [Required]
        public string RecipientId { get; set; } = string.Empty;

        /// <summary>
        /// Subject of the message
        /// </summary>
        [MaxLength(200)]
        public string? Subject { get; set; }

        /// <summary>
        /// Content of the message
        /// </summary>
        [Required]
        [MaxLength(5000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Whether the message has been read
        /// </summary>
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// Date when the message was read
        /// </summary>
        public DateTime? ReadAt { get; set; }

        /// <summary>
        /// Optional reference to a book (if message is about a specific book)
        /// </summary>
        public int? BookId { get; set; }

        /// <summary>
        /// Optional reference to a borrow request (if message is about a request)
        /// </summary>
        public int? BorrowRequestId { get; set; }

        /// <summary>
        /// Optional reference to a loan (if message is about a loan)
        /// </summary>
        public int? LoanId { get; set; }

        /// <summary>
        /// Whether the sender has deleted this message
        /// </summary>
        public bool SenderDeleted { get; set; } = false;

        /// <summary>
        /// Whether the recipient has deleted this message
        /// </summary>
        public bool RecipientDeleted { get; set; } = false;

        /// <summary>
        /// Navigation property to the sender
        /// </summary>
        public virtual ApplicationUser Sender { get; set; } = null!;

        /// <summary>
        /// Navigation property to the recipient
        /// </summary>
        public virtual ApplicationUser Recipient { get; set; } = null!;
    }
}