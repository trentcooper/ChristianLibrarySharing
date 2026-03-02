using System.ComponentModel.DataAnnotations;
using ChristianLibrary.Domain.Enums;

namespace ChristianLibrary.Domain.Entities
{
    /// <summary>
    /// Represents a request to borrow a book
    /// Maps to US-05.01: Create borrow request data model
    /// </summary>
    public class BorrowRequest : BaseEntity
    {
        /// <summary>
        /// Foreign key to the book being requested
        /// </summary>
        [Required]
        public int BookId { get; set; }

        /// <summary>
        /// Foreign key to the user requesting to borrow (borrower)
        /// </summary>
        [Required]
        public string BorrowerId { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to the book owner (lender)
        /// </summary>
        [Required]
        public string LenderId { get; set; } = string.Empty;

        /// <summary>
        /// Current status of the borrow request
        /// </summary>
        public BorrowRequestStatus Status { get; set; } = BorrowRequestStatus.Pending;

        /// <summary>
        /// Requested borrow start date
        /// </summary>
        public DateTime RequestedStartDate { get; set; }

        /// <summary>
        /// Requested borrow end date
        /// </summary>
        public DateTime RequestedEndDate { get; set; }

        /// <summary>
        /// Message from borrower to lender
        /// </summary>
        [MaxLength(1000)]
        public string? Message { get; set; }

        /// <summary>
        /// Response message from lender
        /// </summary>
        [MaxLength(1000)]
        public string? ResponseMessage { get; set; }

        /// <summary>
        /// Date when the request was responded to (approved/declined)
        /// </summary>
        public DateTime? RespondedAt { get; set; }

        /// <summary>
        /// Date when the request expires if not responded to
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Navigation property to the book
        /// </summary>
        public virtual Book Book { get; set; } = null!;

        /// <summary>
        /// Navigation property to the borrower
        /// </summary>
        public virtual ApplicationUser Borrower { get; set; } = null!;

        /// <summary>
        /// Navigation property to the lender
        /// </summary>
        public virtual ApplicationUser Lender { get; set; } = null!;

        /// <summary>
        /// Navigation property to the loan (if approved and converted to loan)
        /// </summary>
        public virtual Loan? Loan { get; set; }
    }
}