using System.ComponentModel.DataAnnotations;
using ChristianLibrary.Domain.Enums;

namespace ChristianLibrary.Domain.Entities
{
    /// <summary>
    /// Represents an active loan of a book
    /// Maps to US-06.01: Create borrow request data model (loan tracking)
    /// </summary>
    public class Loan : BaseEntity
    {
        /// <summary>
        /// Foreign key to the book being loaned
        /// </summary>
        [Required]
        public int BookId { get; set; }

        /// <summary>
        /// Foreign key to the borrower
        /// </summary>
        [Required]
        public string BorrowerId { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to the lender (book owner)
        /// </summary>
        [Required]
        public string LenderId { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to the original borrow request
        /// </summary>
        public int? BorrowRequestId { get; set; }

        /// <summary>
        /// Current status of the loan
        /// </summary>
        public LoanStatus Status { get; set; } = LoanStatus.Active;

        /// <summary>
        /// Date when the loan started (book handed over)
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Date when the book is due to be returned
        /// </summary>
        public DateTime DueDate { get; set; }

        /// <summary>
        /// Actual date when the book was returned
        /// </summary>
        public DateTime? ReturnedDate { get; set; }

        /// <summary>
        /// Number of days the loan has been extended
        /// </summary>
        public int ExtensionDays { get; set; } = 0;

        /// <summary>
        /// Whether an extension has been requested
        /// </summary>
        public bool ExtensionRequested { get; set; } = false;

        /// <summary>
        /// Number of times reminders have been sent
        /// </summary>
        public int RemindersSent { get; set; } = 0;

        /// <summary>
        /// Notes from lender at loan start
        /// </summary>
        [MaxLength(1000)]
        public string? LenderNotes { get; set; }

        /// <summary>
        /// Notes from borrower at return
        /// </summary>
        [MaxLength(1000)]
        public string? BorrowerNotes { get; set; }
        
        /// <summary>
        /// New due date proposed by the borrower in a pending extension request.
        /// Populated when an extension is requested; remains as audit breadcrumb after decision.
        /// Maps to US-06.11
        /// </summary>
        public DateTime? RequestedExtensionDate { get; set; }

        /// <summary>
        /// Optional message from the borrower accompanying an extension request.
        /// Maps to US-06.11
        /// </summary>
        [MaxLength(1000)]
        public string? ExtensionRequestMessage { get; set; }
        
        
        // ── Condition Tracking ─────────────────────────────────────────────────

        /// <summary>
        /// Condition of book at checkout (recorded by lender at pickup)
        /// </summary>
        public BookCondition? ConditionAtCheckout { get; set; }

        /// <summary>
        /// Condition of book at return (recorded by lender at return)
        /// Maps to US-06.07
        /// </summary>
        public BookCondition? ConditionAtReturn { get; set; }
        
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
        /// Navigation property to the original borrow request
        /// </summary>
        public virtual BorrowRequest? BorrowRequest { get; set; }

        /// <summary>
        /// Computed property: whether the loan is overdue
        /// </summary>
        public bool IsOverdue => Status == LoanStatus.Active && DateTime.UtcNow > DueDate;

        /// <summary>
        /// Computed property: days until due (negative if overdue)
        /// </summary>
        public int DaysUntilDue => (DueDate - DateTime.UtcNow).Days;
        
        // ── Reminder Tracking ──────────────────────────────────────────────────

        /// <summary>
        /// Category of the most recent reminder fired for this loan.
        /// Null if no reminder has ever fired.
        /// Maps to US-06.10
        /// </summary>
        public ReminderCategory? LastReminderCategory { get; set; }

        /// <summary>
        /// Offset in days (relative to DueDate) of the most recent reminder fired.
        /// Negative = before due, zero = on due, positive = overdue.
        /// Null if no reminder has ever fired.
        /// Maps to US-06.10
        /// </summary>
        public int? LastReminderOffsetDays { get; set; }

        /// <summary>
        /// UTC timestamp of when the most recent reminder fired.
        /// Null if no reminder has ever fired.
        /// Maps to US-06.10
        /// </summary>
        public DateTime? LastReminderSentAt { get; set; }
    }
}