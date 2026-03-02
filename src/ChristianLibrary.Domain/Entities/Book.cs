using System.ComponentModel.DataAnnotations;
using ChristianLibrary.Domain.Enums;

namespace ChristianLibrary.Domain.Entities
{
    /// <summary>
    /// Represents a book in the Christian Library system
    /// Maps to US-04.01: Create book data model
    /// </summary>
    public class Book : BaseEntity
    {
        /// <summary>
        /// Book title (required)
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Book author (required)
        /// </summary>
        [Required]
        [MaxLength(300)]
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// ISBN (International Standard Book Number)
        /// </summary>
        [MaxLength(20)]
        public string? ISBN { get; set; }

        /// <summary>
        /// Publisher name
        /// </summary>
        [MaxLength(200)]
        public string? Publisher { get; set; }

        /// <summary>
        /// Publication year
        /// </summary>
        public int? PublicationYear { get; set; }

        /// <summary>
        /// Book genre or category
        /// </summary>
        [MaxLength(100)]
        public string? Genre { get; set; }

        /// <summary>
        /// Physical condition of the book
        /// </summary>
        public BookCondition Condition { get; set; } = BookCondition.Good;

        /// <summary>
        /// Detailed description of the book
        /// </summary>
        [MaxLength(2000)]
        public string? Description { get; set; }

        /// <summary>
        /// Book cover image URL or path
        /// </summary>
        [MaxLength(500)]
        public string? CoverImageUrl { get; set; }

        /// <summary>
        /// Number of pages
        /// </summary>
        public int? PageCount { get; set; }

        /// <summary>
        /// Language of the book
        /// </summary>
        [MaxLength(50)]
        public string? Language { get; set; }

        /// <summary>
        /// Whether the book is currently available for borrowing
        /// </summary>
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// Whether the book is visible in searches
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Foreign key to the owner (ApplicationUser)
        /// </summary>
        [Required]
        public string OwnerId { get; set; } = string.Empty;

        /// <summary>
        /// Additional notes from the owner
        /// </summary>
        [MaxLength(1000)]
        public string? OwnerNotes { get; set; }

        /// <summary>
        /// Number of times this book has been borrowed
        /// </summary>
        public int BorrowCount { get; set; } = 0;

        /// <summary>
        /// Average rating (for future reviews feature)
        /// </summary>
        public decimal? AverageRating { get; set; }

        /// <summary>
        /// Navigation property to the book owner
        /// </summary>
        public virtual ApplicationUser Owner { get; set; } = null!;

        /// <summary>
        /// Navigation property to borrow requests for this book
        /// </summary>
        public virtual ICollection<BorrowRequest> BorrowRequests { get; set; } = new List<BorrowRequest>();

        /// <summary>
        /// Navigation property to loans for this book
        /// </summary>
        public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }
}